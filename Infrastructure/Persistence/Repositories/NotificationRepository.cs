using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public Task<List<Notification>> GetForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default) =>
        context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public Task<Notification?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public void Add(Notification notification) => context.Notifications.Add(notification);
}
