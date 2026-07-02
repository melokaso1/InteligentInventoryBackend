using System.Text;
using Api.Dtos;
using Api.Mapping;
using Core.Entities;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? q,
        [FromQuery] string? category,
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

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = entities.Select(p => p.ToProductDto()).ToList();

        return Ok(new { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ProductStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var products = context.Products.AsNoTracking();
        var dto = new ProductStatsDto
        {
            TotalProducts = await products.CountAsync(cancellationToken),
            ActiveProducts = await products.CountAsync(p => p.Status == ProductStatus.Active, cancellationToken),
            InactiveProducts = await products.CountAsync(p => p.Status == ProductStatus.Inactive, cancellationToken),
            OutOfStockProducts = await products.CountAsync(p => p.Status == ProductStatus.OutOfStock, cancellationToken),
            ArchivedProducts = await products.CountAsync(p => p.Status == ProductStatus.Archived, cancellationToken),
            TotalInventoryValue = await products.SumAsync(p => p.Price * p.Stock, cancellationToken),
        };

        return Ok(dto);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken = default)
    {
        var products = await context.Products.AsNoTracking().OrderBy(p => p.Code).ToListAsync(cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("id,code,name,category,price,stock,maxStock,status,icon,description");

        foreach (var product in products)
        {
            csv.AppendLine(
                $"{product.Id},{Escape(product.Code)},{Escape(product.Name)},{Escape(product.Category)},{product.Price:0.00},{product.Stock},{product.MaxStock},{EntityMappers.ToFrontendProductStatus(product.Status)},{Escape(product.Icon)},{Escape(product.Description)}");
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "productos.csv");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound("Producto no encontrado.");
        }

        return Ok(product.ToProductDto());
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("El código y nombre son obligatorios.");
        }

        if (request.Price < 0 || request.Stock < 0 || request.MaxStock < 0)
        {
            return BadRequest("Los valores numéricos no pueden ser negativos.");
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await context.Products.AnyAsync(p => p.Code == normalizedCode, cancellationToken))
        {
            return BadRequest("Ya existe un producto con ese código.");
        }

        if (!TryGetProductStatus(request.Status, request.Stock, out var status, out var error))
        {
            return BadRequest(error);
        }

        var now = DateTime.UtcNow;
        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero),
            Stock = request.Stock,
            MaxStock = request.MaxStock,
            Status = status,
            Icon = request.Icon.Trim(),
            Description = request.Description.Trim(),
            Warehouse = request.Warehouse.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.Products.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity.ToProductDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound("Producto no encontrado.");
        }

        if (request.Price < 0 || request.Stock < 0 || request.MaxStock < 0)
        {
            return BadRequest("Los valores numéricos no pueden ser negativos.");
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var duplicateCode = await context.Products.AnyAsync(
            p => p.Id != id && p.Code == normalizedCode,
            cancellationToken);

        if (duplicateCode)
        {
            return BadRequest("Ya existe otro producto con ese código.");
        }

        if (!TryGetProductStatus(request.Status, request.Stock, out var status, out var error))
        {
            return BadRequest(error);
        }

        entity.Code = normalizedCode;
        entity.Name = request.Name.Trim();
        entity.Category = request.Category.Trim();
        entity.Price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero);
        entity.Stock = request.Stock;
        entity.MaxStock = request.MaxStock;
        entity.Status = status;
        entity.Icon = request.Icon.Trim();
        entity.Description = request.Description.Trim();
        entity.Warehouse = request.Warehouse.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToProductDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound("Producto no encontrado.");
        }

        context.Products.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ProductDto>> Duplicate(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (source is null)
        {
            return NotFound("Producto no encontrado.");
        }

        var baseCode = $"{source.Code}-COPY";
        var duplicatedCode = baseCode;
        var suffix = 1;
        while (await context.Products.AnyAsync(p => p.Code == duplicatedCode, cancellationToken))
        {
            duplicatedCode = $"{baseCode}-{suffix}";
            suffix++;
        }

        var now = DateTime.UtcNow;
        var duplicate = new Product
        {
            Id = Guid.NewGuid(),
            Code = duplicatedCode,
            Name = $"{source.Name} (Copia)",
            Category = source.Category,
            Price = source.Price,
            Stock = source.Stock,
            MaxStock = source.MaxStock,
            Status = source.Stock <= 0 ? ProductStatus.OutOfStock : source.Status,
            Icon = source.Icon,
            Description = source.Description,
            Warehouse = source.Warehouse,
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.Products.Add(duplicate);
        await context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = duplicate.Id }, duplicate.ToProductDto());
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ProductDto>> PatchStatus(
        Guid id,
        [FromBody] ProductStatusPatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound("Producto no encontrado.");
        }

        if (!EntityMappers.TryParseProductStatus(request.Status, out var parsedStatus))
        {
            return BadRequest("Estado inválido. Valores permitidos: active, inactive, out_of_stock, archived.");
        }

        entity.Status = parsedStatus;
        entity.UpdatedAt = DateTime.UtcNow;
        if (parsedStatus == ProductStatus.OutOfStock)
        {
            entity.Stock = 0;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToProductDto());
    }

    private static bool TryGetProductStatus(
        string? requestedStatus,
        int stock,
        out ProductStatus status,
        out string error)
    {
        if (stock <= 0)
        {
            status = ProductStatus.OutOfStock;
            error = string.Empty;
            return true;
        }

        if (string.IsNullOrWhiteSpace(requestedStatus))
        {
            status = ProductStatus.Active;
            error = string.Empty;
            return true;
        }

        if (!EntityMappers.TryParseProductStatus(requestedStatus, out status))
        {
            error = "Estado inválido. Valores permitidos: active, inactive, out_of_stock, archived.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
