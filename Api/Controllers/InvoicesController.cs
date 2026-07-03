using System.Security.Claims;
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
            return Ok(result.ToPagedDto(i => i.ToInvoiceDto()));
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

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await invoiceService.CreateAsync(
                new CreateInvoiceModel
                {
                    Client = request.Client,
                    BillingNote = request.BillingNote,
                    Date = request.Date,
                    DueDate = request.DueDate,
                    LineItems = request.LineItems
                        .Select(li => new CreateInvoiceLineItemModel
                        {
                            Description = li.Description,
                            Quantity = li.Quantity,
                            UnitPrice = li.UnitPrice,
                        })
                        .ToList(),
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice.ToInvoiceDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deprecated: use POST /api/sales (manual sale) then POST /api/sales/{id}/invoice instead.
    /// </summary>
    [HttpPost("manual")]
    public async Task<ActionResult<InvoiceDto>> CreateManual(
        [FromBody] CreateManualInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await invoiceService.CreateManualAsync(
                new CreateManualInvoiceModel
                {
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    BillingNote = request.BillingNote,
                    LineItems = request.LineItems
                        .Select(li => new CreateManualInvoiceLineItemModel
                        {
                            ProductId = li.ProductId,
                            ProductCode = li.ProductCode,
                            Quantity = li.Quantity,
                        })
                        .ToList(),
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice.ToInvoiceDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
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
            return File(Encoding.UTF8.GetBytes(content), "text/plain; charset=utf-8", $"factura-{id}.txt");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<InvoiceDto>> PayInvoice(
        Guid id,
        [FromBody] PayInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var invoice = await invoiceService.PayInvoiceAsync(
                id,
                new PayInvoiceModel { PaymentMethod = request.PaymentMethod },
                userId,
                isAdmin: true,
                cancellationToken);

            return Ok(invoice.ToInvoiceDto());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
