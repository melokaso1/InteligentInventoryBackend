using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ChatSessionRepository(AppDbContext context) : IChatSessionRepository
{
    public Task<ChatSession?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken = default) =>
        context.ChatSessions
            .AsNoTracking()
            .Include(cs => cs.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(cs => cs.SessionToken == sessionToken, cancellationToken);

    public Task<ChatSession?> GetByTokenTrackedAsync(string sessionToken, CancellationToken cancellationToken = default) =>
        context.ChatSessions
            .Include(cs => cs.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(cs => cs.SessionToken == sessionToken, cancellationToken);

    public Task<List<ChatMessage>> GetMessagesAsync(Guid chatSessionId, CancellationToken cancellationToken = default) =>
        context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatSessionId == chatSessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(ChatSession entity) => context.ChatSessions.Add(entity);

    public void AddMessage(ChatMessage entity) => context.ChatMessages.Add(entity);
}
