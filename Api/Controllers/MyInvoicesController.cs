using System.Security.Claims;
using Api.Dtos;
using Api.Mapping;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/my/invoices")]
public sealed class MyInvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await invoiceService.GetMyInvoicesAsync(
                userId,
                new InvoiceQueryModel { Page = page, PageSize = pageSize, Status = status },
                cancellationToken);

            return Ok(result.ToPagedDto(i => i.ToInvoiceDto()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
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
                isAdmin: false,
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
