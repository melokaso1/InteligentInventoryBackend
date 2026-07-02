using System.Text;
using Api.Dtos;
using Api.Mapping;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!EntityMappers.TryParseInvoiceStatus(status, out var parsedStatus))
            {
                return BadRequest("Estado inválido. Valores permitidos: paid, pending, overdue, draft.");
            }

            query = query.Where(i => i.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var invoices = await query
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Items = invoices.Select(i => i.ToInvoiceDto()).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<InvoiceStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var invoices = context.Invoices.AsNoTracking();
        var dto = new InvoiceStatsDto
        {
            TotalInvoices = await invoices.CountAsync(cancellationToken),
            PaidInvoices = await invoices.CountAsync(i => i.Status == InvoiceStatus.Paid, cancellationToken),
            PendingInvoices = await invoices.CountAsync(i => i.Status == InvoiceStatus.Pending, cancellationToken),
            OverdueInvoices = await invoices.CountAsync(i => i.Status == InvoiceStatus.Overdue, cancellationToken),
            DraftInvoices = await invoices.CountAsync(i => i.Status == InvoiceStatus.Draft, cancellationToken),
            TotalBilledAmount = await invoices.SumAsync(i => i.Total, cancellationToken),
        };

        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is null)
        {
            return NotFound("Factura no encontrada.");
        }

        return Ok(invoice.ToInvoiceDto());
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is null)
        {
            return NotFound("Factura no encontrada.");
        }

        var content = new StringBuilder();
        content.AppendLine($"Factura: {invoice.InvoiceNumber}");
        content.AppendLine($"Cliente: {invoice.ClientName}");
        content.AppendLine($"Fecha: {invoice.IssueDate:yyyy-MM-dd}");
        content.AppendLine($"Vencimiento: {invoice.DueDate:yyyy-MM-dd}");
        content.AppendLine($"Estado: {EntityMappers.ToFrontendInvoiceStatus(invoice.Status)}");
        content.AppendLine($"Subtotal: COP {invoice.Subtotal:0.00}");
        content.AppendLine($"IVA: COP {invoice.Tax:0.00}");
        content.AppendLine($"Total: COP {invoice.Total:0.00}");
        content.AppendLine();
        content.AppendLine("Items:");

        foreach (var item in invoice.LineItems)
        {
            content.AppendLine($"- {item.Description} x{item.Quantity} @ COP {item.UnitPrice:0.00}");
        }

        return File(Encoding.UTF8.GetBytes(content.ToString()), "application/pdf", $"{invoice.InvoiceNumber}.pdf");
    }
}
