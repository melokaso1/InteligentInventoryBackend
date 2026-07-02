using Api.Dtos;
using Application.Abstractions;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/chat")]
public sealed class ChatController(IChatService chatService, IChatbotGateway chatbotGateway) : ControllerBase
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

            return Ok(ToDto(result));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken = default)
    {
        var healthy = await chatbotGateway.PingHealthAsync(cancellationToken);
        if (!healthy)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "El servicio FastAPI del chatbot no está disponible. Verifica que esté en ejecución en http://localhost:8000.");
        }

        return Ok(new { status = "ok", chatbot = "available" });
    }

    [HttpGet("sessions/{token}/history")]
    public async Task<ActionResult<IReadOnlyList<ChatHistoryMessageDto>>> GetHistory(
        string token,
        CancellationToken cancellationToken = default)
    {
        var history = await chatService.GetHistoryAsync(token, cancellationToken);
        return Ok(history.Select(h => new ChatHistoryMessageDto
        {
            SenderType = h.SenderType,
            MessageText = h.MessageText,
            CreatedAt = h.CreatedAt,
        }).ToList());
    }

    private static ChatMessageResponseDto ToDto(ChatMessageResult result) =>
        new()
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
        };
}
