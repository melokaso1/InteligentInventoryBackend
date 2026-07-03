using Application.Models;

namespace Application.Abstractions;

public interface IChatbotGateway
{
    Task<ChatMessageResult> SendMessageAsync(ChatMessageRequest request, CancellationToken cancellationToken = default);
    Task<bool> PingHealthAsync(CancellationToken cancellationToken = default);
    Task<ChatbotHealthStatus?> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}
