using Api.Dtos;
using Api.Mapping;
using Application.Abstractions;
using Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/dispatch")]
public sealed class DispatchController(IDispatchService dispatchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDispatchSales(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? fulfillmentStatus = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await dispatchService.GetDispatchSalesAsync(
                new DispatchQueryModel
                {
                    Page = page,
                    PageSize = pageSize,
                    FulfillmentStatus = fulfillmentStatus,
                },
                cancellationToken);

            return Ok(result.ToPagedDto(s => s.ToSaleDto()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{saleId:guid}/status")]
    public async Task<ActionResult<SaleDto>> UpdateStatus(
        Guid saleId,
        [FromBody] UpdateFulfillmentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!EntityMappers.TryParseFulfillmentStatus(request.Status, out var status))
        {
            return BadRequest("Estado de despacho inválido. Valores: preparing, shipped, delivered.");
        }

        try
        {
            var sale = await dispatchService.UpdateFulfillmentStatusAsync(saleId, status, cancellationToken);
            return Ok(sale.ToSaleDto());
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
}
