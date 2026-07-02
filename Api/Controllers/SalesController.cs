using Api.Dtos;
using Api.Mapping;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(ISaleService saleService) : ControllerBase
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
            var result = await saleService.GetSalesAsync(
                new SalesQueryModel
                {
                    From = from,
                    To = to,
                    Origin = origin,
                    Status = status,
                    Page = page,
                    PageSize = pageSize,
                },
                cancellationToken);

            return Ok(result.ToPagedDto(s => s.ToSaleDto()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<SaleMetricsDto>> GetMetrics(CancellationToken cancellationToken = default)
    {
        var metrics = await saleService.GetMetricsAsync(cancellationToken);
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
                        .Select(li => new CreateSaleLineItemModel { ProductId = li.ProductId, Quantity = li.Quantity })
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

    [HttpPost("from-chatbot")]
    public async Task<ActionResult<CreateSaleFromChatbotResponse>> CreateSaleFromChatbot(
        [FromBody] CreateSaleFromChatbotRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await saleService.CreateSaleFromChatbotAsync(
                request.ProductCode,
                request.Quantity,
                request.CustomerName,
                request.CustomerEmail,
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
