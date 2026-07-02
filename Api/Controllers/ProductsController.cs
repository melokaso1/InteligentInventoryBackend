using System.Text;
using Api.Dtos;
using Api.Mapping;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await productService.GetProductsAsync(
            new ProductQueryModel { Query = q, Category = category, Page = page, PageSize = pageSize },
            cancellationToken);

        return Ok(new
        {
            Items = result.Items.Select(p => p.ToProductDto()).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize,
        });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ProductStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var stats = await productService.GetStatsAsync(cancellationToken);
        return Ok(
            new ProductStatsDto
            {
                TotalProducts = stats.TotalProducts,
                ActiveProducts = stats.ActiveProducts,
                InactiveProducts = stats.InactiveProducts,
                OutOfStockProducts = stats.OutOfStockProducts,
                ArchivedProducts = stats.ArchivedProducts,
                TotalInventoryValue = stats.TotalInventoryValue,
            });
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken = default)
    {
        var products = await productService.GetProductsForExportAsync(cancellationToken);
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
        var product = await productService.GetByIdAsync(id, cancellationToken);
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
        try
        {
            var entity = await productService.CreateAsync(
                new CreateProductModel
                {
                    Code = request.Code,
                    Name = request.Name,
                    Category = request.Category,
                    Price = request.Price,
                    Stock = request.Stock,
                    MaxStock = request.MaxStock,
                    Status = request.Status,
                    Icon = request.Icon,
                    Description = request.Description,
                    Warehouse = request.Warehouse,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity.ToProductDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await productService.UpdateAsync(
                id,
                new UpdateProductModel
                {
                    Code = request.Code,
                    Name = request.Name,
                    Category = request.Category,
                    Price = request.Price,
                    Stock = request.Stock,
                    MaxStock = request.MaxStock,
                    Status = request.Status,
                    Icon = request.Icon,
                    Description = request.Description,
                    Warehouse = request.Warehouse,
                },
                cancellationToken);
            return Ok(entity.ToProductDto());
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await productService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ProductDto>> Duplicate(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var duplicate = await productService.DuplicateAsync(id, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = duplicate.Id }, duplicate.ToProductDto());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ProductDto>> PatchStatus(
        Guid id,
        [FromBody] ProductStatusPatchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await productService.PatchStatusAsync(id, request.Status, cancellationToken);
            return Ok(entity.ToProductDto());
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

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
