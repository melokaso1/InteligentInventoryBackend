using System.Text;
using Api.Dtos;
using Api.Helpers;
using Api.Mapping;
using Core.Entities;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInventory(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? warehouse,
        [FromQuery] string? stockLevel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = context.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => p.Code.ToLower().Contains(term) || p.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim().ToLower();
            query = query.Where(p => p.Category.ToLower() == normalized);
        }

        if (!string.IsNullOrWhiteSpace(warehouse))
        {
            var normalized = warehouse.Trim().ToLower();
            query = query.Where(p => p.Warehouse.ToLower() == normalized);
        }

        var allFiltered = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        var normalizedStockLevel = stockLevel?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedStockLevel))
        {
            allFiltered = allFiltered
                .Where(p => StockLevelHelper.GetStockLevel(p.Stock, p.MaxStock).StockLevel == normalizedStockLevel)
                .ToList();
        }

        var totalCount = allFiltered.Count;
        var items = allFiltered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.ToInventoryItemDto())
            .ToList();

        return Ok(new { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<InventoryStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var products = await context.Products.AsNoTracking().ToListAsync(cancellationToken);
        var dto = new InventoryStatsDto
        {
            TotalItems = products.Count,
            TotalUnits = products.Sum(p => p.Stock),
            TotalValue = products.Sum(p => p.Price * p.Stock),
            LowStockCount = products.Count(p => StockLevelHelper.GetStockLevel(p.Stock, p.MaxStock).StockLevel is "low" or "critical"),
            OutOfStockCount = products.Count(p => p.Stock <= 0),
        };

        return Ok(dto);
    }

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] string? type,
        [FromQuery] string? productCode,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalized = type.Trim().ToLowerInvariant();
            query = normalized switch
            {
                "inbound" => query.Where(m => m.Type == StockMovementType.Inbound),
                "adjustment" => query.Where(m => m.Type == StockMovementType.Adjustment),
                "outbound" => query.Where(m => m.Type == StockMovementType.Outbound),
                _ => query,
            };
        }

        if (!string.IsNullOrWhiteSpace(productCode))
        {
            var normalized = productCode.Trim().ToLower();
            query = query.Where(m => m.Product.Code.ToLower() == normalized);
        }

        if (from.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Items = items.Select(m => m.ToStockMovementDto()).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult<StockMovementDto>> CreateAdjustment(
        [FromBody] AdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QuantityChange == 0)
        {
            return BadRequest("El ajuste no puede ser cero.");
        }

        Product? product = null;
        if (request.ProductId.HasValue)
        {
            product = await context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            var normalizedCode = request.ProductCode.Trim().ToUpperInvariant();
            product = await context.Products.FirstOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken);
        }

        if (product is null)
        {
            return NotFound("Producto no encontrado para ajustar inventario.");
        }

        var resultingStock = product.Stock + request.QuantityChange;
        if (resultingStock < 0)
        {
            return BadRequest("El ajuste deja el stock en negativo.");
        }

        product.Stock = resultingStock;
        product.UpdatedAt = DateTime.UtcNow;
        if (product.Stock <= 0)
        {
            product.Status = ProductStatus.OutOfStock;
        }
        else if (product.Status == ProductStatus.OutOfStock)
        {
            product.Status = ProductStatus.Active;
        }

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Type = StockMovementType.Adjustment,
            QuantityChange = request.QuantityChange,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Ajuste manual" : request.Reason.Trim(),
            Detail = string.IsNullOrWhiteSpace(request.Detail) ? "Ajuste realizado desde API" : request.Detail.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        context.InventoryMovements.Add(movement);
        await context.SaveChangesAsync(cancellationToken);

        return Ok(movement.ToStockMovementDto());
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken = default)
    {
        var products = await context.Products.AsNoTracking().OrderBy(p => p.Code).ToListAsync(cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("sku,name,category,warehouse,quantity,unitPrice,stockLevel,stockPercent");

        foreach (var item in products.Select(p => p.ToInventoryItemDto()))
        {
            csv.AppendLine(
                $"{Escape(item.Sku)},{Escape(item.Name)},{Escape(item.Category)},{Escape(item.Warehouse)},{item.Quantity},{item.UnitPrice:0.00},{item.StockLevel},{item.StockPercent:0.##}");
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "inventario.csv");
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
