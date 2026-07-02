using Application.Abstractions;
using Application.Models;

namespace Application.Services;

public interface IChatService
{
    Task<ChatMessageResult> SendMessageAsync(ChatMessageRequest request, CancellationToken cancellationToken = default);
}

public sealed class ChatService(IChatbotGateway chatbotGateway) : IChatService
{
    public Task<ChatMessageResult> SendMessageAsync(
        ChatMessageRequest request,
        CancellationToken cancellationToken = default) =>
        chatbotGateway.SendMessageAsync(request, cancellationToken);
}
