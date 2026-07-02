using Api.Dtos;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost("message")]
    public async Task<ActionResult<ChatMessageResponseDto>> SendMessage(
        [FromBody] ChatMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await chatService.SendMessageAsync(
                new ChatMessageRequest
                {
                    SessionId = request.SessionId,
                    Message = request.Message,
                },
                cancellationToken);

            return Ok(
                new ChatMessageResponseDto
                {
                    Response = result.Response,
                    State = result.State,
                    InvoiceNumber = result.InvoiceNumber,
                    Chips = result.Chips,
                    OperationSummary = result.OperationSummary is null
                        ? null
                        : new ChatOperationSummaryDto
                        {
                            TransactionId = result.OperationSummary.TransactionId,
                            Status = result.OperationSummary.Status,
                            ProductCode = result.OperationSummary.ProductCode,
                            ProductName = result.OperationSummary.ProductName,
                            Quantity = result.OperationSummary.Quantity,
                            UnitPrice = result.OperationSummary.UnitPrice,
                            Subtotal = result.OperationSummary.Subtotal,
                            Tax = result.OperationSummary.Tax,
                            Total = result.OperationSummary.Total,
                        },
                });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }
}
