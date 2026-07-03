using Domain.Entities;

namespace Application.Abstractions;

public interface INotificationRepository
{
    Task<List<Notification>> GetForUserAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<int> CountUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Notification notification);
}
