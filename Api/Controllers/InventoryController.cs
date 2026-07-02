using System.Text;
using Api.Dtos;
using Api.Mapping;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(IInventoryService inventoryService) : ControllerBase
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
        var result = await inventoryService.GetInventoryAsync(
            new InventoryQueryModel
            {
                Query = q,
                Category = category,
                Warehouse = warehouse,
                StockLevel = stockLevel,
                Page = page,
                PageSize = pageSize,
            },
            cancellationToken);

        return Ok(result.ToPagedDto(p => p.ToInventoryItemDto()));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken cancellationToken = default)
    {
        var categories = await inventoryService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<InventoryStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var stats = await inventoryService.GetStatsAsync(cancellationToken);
        return Ok(
            new InventoryStatsDto
            {
                TotalItems = stats.TotalItems,
                TotalUnits = stats.TotalUnits,
                TotalValue = stats.TotalValue,
                LowStockCount = stats.LowStockCount,
                OutOfStockCount = stats.OutOfStockCount,
            });
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
        var result = await inventoryService.GetMovementsAsync(
            new InventoryMovementQueryModel
            {
                Type = type,
                ProductCode = productCode,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize,
            },
            cancellationToken);

        return Ok(result.ToPagedDto(m => m.ToStockMovementDto()));
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult<StockMovementDto>> CreateAdjustment(
        [FromBody] AdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var movement = await inventoryService.CreateAdjustmentAsync(
                new AdjustmentModel
                {
                    ProductId = request.ProductId,
                    ProductCode = request.ProductCode,
                    QuantityChange = request.QuantityChange,
                    Reason = request.Reason,
                    Detail = request.Detail,
                },
                cancellationToken);
            return Ok(movement.ToStockMovementDto());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken = default)
    {
        var products = await inventoryService.GetInventoryForExportAsync(cancellationToken);
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
