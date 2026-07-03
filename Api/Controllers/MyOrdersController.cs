using System.Security.Claims;
using Api.Dtos;
using Api.Mapping;
using Application.Abstractions;
using Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/my/orders")]
public sealed class MyOrdersController(IDispatchService dispatchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? fulfillmentStatus = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await dispatchService.GetMyOrdersAsync(
                userId,
                new MyOrdersQueryModel
                {
                    Page = page,
                    PageSize = pageSize,
                    FulfillmentStatus = fulfillmentStatus,
                },
                cancellationToken);

            return Ok(result.ToPagedDto(s => s.ToSaleDto()));
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

    [HttpPost("{saleId:guid}/confirm-delivery")]
    public async Task<ActionResult<SaleDto>> ConfirmDelivery(
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var sale = await dispatchService.ConfirmDeliveryAsync(saleId, userId, cancellationToken);
            return Ok(sale.ToSaleDto());
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
