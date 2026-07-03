using System.Text.Json;
using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public interface IChatService
{
    Task<ChatMessageResult> SendMessageAsync(
        ChatMessageRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatHistoryMessage>> GetHistoryAsync(
        string sessionId,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<ChatRetentionResult> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}

public sealed class ChatRetentionResult
{
    public int DeletedMessages { get; set; }
    public int DeletedSessions { get; set; }
}

public sealed class ChatService(
    IChatbotGateway chatbotGateway,
    IChatSessionRepository chatSessionRepository,
    IUnitOfWork unitOfWork) : IChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ChatMessageResult> SendMessageAsync(
        ChatMessageRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var sessionToken = request.SessionId.Trim();
        var session = await chatSessionRepository.GetByTokenTrackedAsync(sessionToken, cancellationToken);

        if (session is null)
        {
            session = new ChatSession
            {
                Id = Guid.NewGuid(),
                SessionToken = sessionToken,
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            chatSessionRepository.Add(session);
        }
        else
        {
            EnsureSessionAccess(session, userId);
        }

        var stateJson = !string.IsNullOrWhiteSpace(request.StateJson)
            ? request.StateJson
            : session.CurrentStateJson;

        chatSessionRepository.AddMessage(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            SenderType = ChatSenderType.User,
            MessageText = request.Message.Trim(),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await chatbotGateway.SendMessageAsync(
            new ChatMessageRequest
            {
                SessionId = sessionToken,
                Message = request.Message,
                StateJson = stateJson,
            },
            cancellationToken);

        session.CurrentStateJson = result.StateJson ?? session.CurrentStateJson;
        session.UpdatedAt = DateTime.UtcNow;

        chatSessionRepository.AddMessage(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            SenderType = ChatSenderType.Bot,
            MessageText = result.Response,
            MetadataJson = BuildBotMetadata(result),
            CreatedAt = DateTime.UtcNow,
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<ChatHistoryMessage>> GetHistoryAsync(
        string sessionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var session = await chatSessionRepository.GetByTokenAsync(sessionId.Trim(), cancellationToken);
        if (session is null)
        {
            return [];
        }

        if (session.UserId is not null && session.UserId != userId)
        {
            throw new UnauthorizedAccessException("No tiene acceso a esta sesión de chat.");
        }

        var messages = await chatSessionRepository.GetMessagesAsync(session.Id, cancellationToken);
        return messages
            .Select(m => new ChatHistoryMessage
            {
                SenderType = m.SenderType.ToString().ToLowerInvariant(),
                MessageText = m.MessageText,
                CreatedAt = m.CreatedAt.ToString("O"),
                MetadataJson = m.MetadataJson,
            })
            .ToList();
    }

    public async Task<ChatRetentionResult> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-5);
        var deletedMessages = await chatSessionRepository.DeleteMessagesOlderThanAsync(cutoff, cancellationToken);
        var deletedSessions = await chatSessionRepository.DeleteEmptySessionsAsync(cancellationToken);
        return new ChatRetentionResult
        {
            DeletedMessages = deletedMessages,
            DeletedSessions = deletedSessions,
        };
    }

    private static void EnsureSessionAccess(ChatSession session, Guid userId)
    {
        if (session.UserId is null)
        {
            session.UserId = userId;
            return;
        }

        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException("No tiene acceso a esta sesión de chat.");
        }
    }

    private static string? BuildBotMetadata(ChatMessageResult result)
    {
        var metadata = new
        {
            result.State,
            result.InvoiceNumber,
            result.Chips,
            result.OperationSummary,
            result.Offers,
            result.OffersTotalCount,
        };

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }
}
