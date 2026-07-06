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
        Guid? userId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatHistoryMessage>> GetHistoryAsync(
        string sessionId,
        Guid? userId,
        CancellationToken cancellationToken = default);
    Task AttachSessionToUserAsync(
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
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ChatMessageResult> SendMessageAsync(
        ChatMessageRequest request,
        Guid? userId,
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

        User? user = null;
        if (userId is not null)
        {
            user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        }

        var result = EnrichOperationSummary(
            await chatbotGateway.SendMessageAsync(
                new ChatMessageRequest
                {
                    SessionId = sessionToken,
                    Message = request.Message,
                    StateJson = stateJson,
                    CustomerName = user?.FullName,
                    CustomerEmail = user?.Email,
                },
                cancellationToken));

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
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var session = await chatSessionRepository.GetByTokenAsync(sessionId.Trim(), cancellationToken);
        if (session is null)
        {
            return [];
        }

        EnsureHistoryAccess(session, userId);

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

    public async Task AttachSessionToUserAsync(
        string sessionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var session = await chatSessionRepository.GetByTokenTrackedAsync(sessionId.Trim(), cancellationToken)
            ?? throw new KeyNotFoundException("Sesión de chat no encontrada.");

        if (session.UserId is not null && session.UserId != userId)
        {
            throw new UnauthorizedAccessException("No tiene acceso a esta sesión de chat.");
        }

        session.UserId = userId;
        session.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureSessionAccess(ChatSession session, Guid? userId)
    {
        if (userId is null)
        {
            if (session.UserId is not null)
            {
                throw new UnauthorizedAccessException("Debe iniciar sesión para continuar esta conversación.");
            }

            return;
        }

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

    private static void EnsureHistoryAccess(ChatSession session, Guid? userId)
    {
        if (session.UserId is null)
        {
            if (userId is not null)
            {
                return;
            }

            return;
        }

        if (userId is null || session.UserId != userId)
        {
            throw new UnauthorizedAccessException("No tiene acceso a esta sesión de chat.");
        }
    }

    private static ChatMessageResult EnrichOperationSummary(ChatMessageResult result)
    {
        if (result.OperationSummary is null)
        {
            return result;
        }

        if (result.OperationSummary.LineItems is { Count: > 0 })
        {
            return result;
        }

        var lineItems = ExtractCartLineItemsFromStateJson(result.StateJson);
        if (lineItems is null or { Count: 0 })
        {
            return result;
        }

        return new ChatMessageResult
        {
            Response = result.Response,
            State = result.State,
            StateJson = result.StateJson,
            InvoiceNumber = result.InvoiceNumber,
            Chips = result.Chips,
            OperationSummary = new ChatOperationSummary
            {
                TransactionId = result.OperationSummary.TransactionId,
                Status = result.OperationSummary.Status,
                ProductCode = result.OperationSummary.ProductCode,
                ProductName = result.OperationSummary.ProductName,
                Quantity = result.OperationSummary.Quantity,
                MeasureUnit = result.OperationSummary.MeasureUnit,
                UnitPrice = result.OperationSummary.UnitPrice,
                Subtotal = result.OperationSummary.Subtotal,
                Tax = result.OperationSummary.Tax,
                Total = result.OperationSummary.Total,
                LineItems = lineItems,
            },
            Offers = result.Offers,
            OffersTotalCount = result.OffersTotalCount,
        };
    }

    private static IReadOnlyList<ChatCartLineItem>? ExtractCartLineItemsFromStateJson(string? stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(stateJson);
            var root = doc.RootElement;

            var fromCart = ParseCartLineItems(root, "cart");
            if (fromCart is { Count: > 0 })
            {
                return fromCart;
            }

            if (TryGetProperty(root, "operation_summary", out var summaryEl) ||
                TryGetProperty(root, "operationSummary", out summaryEl))
            {
                var fromSummary = ParseCartLineItems(summaryEl, "lineItems")
                    ?? ParseCartLineItems(summaryEl, "LineItems");
                if (fromSummary is { Count: > 0 })
                {
                    return fromSummary;
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<ChatCartLineItem>? ParseCartLineItems(JsonElement container, string arrayProperty)
    {
        if (!TryGetProperty(container, arrayProperty, out var arrayEl) ||
            arrayEl.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var items = new List<ChatCartLineItem>();
        foreach (var item in arrayEl.EnumerateArray())
        {
            var parsed = ParseCartLineItem(item);
            if (parsed is not null)
            {
                items.Add(parsed);
            }
        }

        return items.Count > 0 ? items : null;
    }

    private static ChatCartLineItem? ParseCartLineItem(JsonElement item)
    {
        var code = GetStringProperty(item, "productCode", "ProductCode");
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return new ChatCartLineItem
        {
            ProductCode = code,
            ProductName = GetStringProperty(item, "productName", "ProductName") ?? code,
            Quantity = GetDecimalProperty(item, "quantity", "Quantity"),
            MeasureUnit = GetStringProperty(item, "measureUnit", "MeasureUnit"),
            UnitPrice = GetDecimalProperty(item, "unitPrice", "UnitPrice"),
            Subtotal = GetDecimalProperty(item, "subtotal", "Subtotal"),
        };
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value) =>
        element.TryGetProperty(propertyName, out value);

    private static string? GetStringProperty(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static decimal GetDecimalProperty(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }
        }

        return 0m;
    }

    private static string? BuildBotMetadata(ChatMessageResult result)
    {
        var metadata = new
        {
            result.State,
            result.InvoiceNumber,
            result.Chips,
            result.OperationSummary,
            cart = result.OperationSummary?.LineItems,
            result.Offers,
            result.OffersTotalCount,
        };

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }
}
