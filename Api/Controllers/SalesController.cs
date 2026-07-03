using Api.Caching;
using Api.Dtos;
using Api.Filters;
using Api.Mapping;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/sales")]
public sealed class SalesController(ISaleService saleService, IMemoryCache cache) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? origin,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"sales:list:{from:O}:{to:O}:{origin}:{status}:{page}:{pageSize}";
            var result = await EndpointCache.GetOrCreateAsync(
                cache,
                cacheKey,
                async ct => await saleService.GetSalesAsync(
                    new SalesQueryModel
                    {
                        From = from,
                        To = to,
                        Origin = origin,
                        Status = status,
                        Page = page,
                        PageSize = pageSize,
                    },
                    ct),
                cancellationToken);

            return Ok(result.ToPagedDto(s => s.ToSaleDto()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<SaleMetricsDto>> GetMetrics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? origin,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"sales:metrics:{from:O}:{to:O}:{origin}:{status}";
            var metrics = await EndpointCache.GetOrCreateAsync(
                cache,
                cacheKey,
                async ct => await saleService.GetMetricsAsync(
                    new SalesQueryModel
                    {
                        From = from,
                        To = to,
                        Origin = origin,
                        Status = status,
                    },
                    ct),
                cancellationToken);

            return Ok(
                new SaleMetricsDto
                {
                    TotalSales = metrics.TotalSales,
                    TotalRevenue = metrics.TotalRevenue,
                    ChatbotSales = metrics.ChatbotSales,
                    ManualSales = metrics.ManualSales,
                    PendingSales = metrics.PendingSales,
                    InvoicedSales = metrics.InvoicedSales,
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await saleService.GetByIdAsync(id, cancellationToken);

        if (sale is null)
        {
            return NotFound("Venta no encontrada.");
        }

        return Ok(sale.ToSaleDto());
    }

    [HttpPost]
    public async Task<ActionResult<SaleDto>> CreateManualSale(
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sale = await saleService.CreateManualSaleAsync(
                new CreateSaleModel
                {
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    Origin = request.Origin,
                    Status = request.Status,
                    LineItems = request.LineItems
                        .Select(li => new CreateSaleLineItemModel
                        {
                            ProductId = li.ProductId,
                            Quantity = li.Quantity,
                            MeasureUnit = li.MeasureUnit,
                        })
                        .ToList(),
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale.ToSaleDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/invoice")]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await saleService.CreateInvoiceAsync(id, cancellationToken);
            return Ok(invoice.ToInvoiceDto());
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

    [AllowAnonymous]
    [ChatbotApiKey]
    [HttpPost("from-chatbot")]
    public async Task<ActionResult<CreateSaleFromChatbotResponse>> CreateSaleFromChatbot(
        [FromBody] CreateSaleFromChatbotRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lineItems = (request.LineItems?.Count > 0
                    ? request.LineItems
                    :
                    [
                        new CreateSaleFromChatbotLineItemRequest
                        {
                            ProductCode = request.ProductCode,
                            Quantity = request.Quantity,
                            MeasureUnit = request.MeasureUnit,
                        },
                    ])
                .Select(li => new ChatbotSaleLineItemModel
                {
                    ProductCode = li.ProductCode,
                    Quantity = li.Quantity,
                    MeasureUnit = li.MeasureUnit,
                })
                .ToList();

            var result = await saleService.CreateSaleFromChatbotAsync(
                lineItems,
                request.CustomerName,
                request.CustomerEmail,
                request.SessionId,
                request.DeliveryAddress,
                request.DeliveryCity,
                cancellationToken);

            return Ok(
                new CreateSaleFromChatbotResponse
                {
                    SaleId = result.Sale.Id,
                    OrderNumber = result.Sale.OrderNumber,
                    InvoiceNumber = result.InvoiceNumber,
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

}
