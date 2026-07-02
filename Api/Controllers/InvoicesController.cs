using System.Text;
using Api.Dtos;
using Api.Mapping;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await invoiceService.GetInvoicesAsync(
                new InvoiceQueryModel { Page = page, PageSize = pageSize, Status = status },
                cancellationToken);
            return Ok(
                new
                {
                    Items = result.Items.Select(i => i.ToInvoiceDto()).ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<InvoiceStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var stats = await invoiceService.GetStatsAsync(cancellationToken);
        return Ok(
            new InvoiceStatsDto
            {
                TotalInvoices = stats.TotalInvoices,
                PaidInvoices = stats.PaidInvoices,
                PendingInvoices = stats.PendingInvoices,
                OverdueInvoices = stats.OverdueInvoices,
                DraftInvoices = stats.DraftInvoices,
                TotalBilledAmount = stats.TotalBilledAmount,
            });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceService.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound("Factura no encontrada.");
        }

        return Ok(invoice.ToInvoiceDto());
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await invoiceService.GetByIdAsync(id, cancellationToken);
            if (invoice is null)
            {
                return NotFound("Factura no encontrada.");
            }

            var content = await invoiceService.BuildPdfContentAsync(id, cancellationToken);
            return File(Encoding.UTF8.GetBytes(content), "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
