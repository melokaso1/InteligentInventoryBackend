using Domain.Entities;

namespace Application.Abstractions;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetByTokenTrackedAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<List<ChatMessage>> GetMessagesAsync(Guid chatSessionId, CancellationToken cancellationToken = default);
    Task<int> DeleteMessagesOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<int> DeleteEmptySessionsAsync(CancellationToken cancellationToken = default);
    void Add(ChatSession entity);
    void AddMessage(ChatMessage entity);
}
