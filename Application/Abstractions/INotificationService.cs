using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions;

public interface INotificationService
{
    Task NotifySaleFulfillmentChangeAsync(
        Sale sale,
        FulfillmentStatus newStatus,
        FulfillmentNotificationRecipient recipient = FulfillmentNotificationRecipient.Customer,
        CancellationToken cancellationToken = default);
    Task<NotificationListModel> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
}
