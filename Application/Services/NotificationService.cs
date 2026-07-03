using Application.Abstractions;
using Application.Models;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class NotificationService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : INotificationService
{
    public async Task NotifySaleFulfillmentChangeAsync(
        Sale sale,
        FulfillmentStatus newStatus,
        FulfillmentNotificationRecipient recipient = FulfillmentNotificationRecipient.Customer,
        CancellationToken cancellationToken = default)
    {
        if (recipient == FulfillmentNotificationRecipient.None)
        {
            return;
        }

        var (title, message, type) = BuildNotificationContent(sale.OrderNumber, newStatus, recipient);
        if (type is null)
        {
            return;
        }

        var recipients = recipient switch
        {
            FulfillmentNotificationRecipient.Admins =>
                await userRepository.GetByRoleIdAsync(RoleIds.Admin, cancellationToken),
            _ => await ResolveCustomerRecipientsAsync(sale, cancellationToken),
        };

        if (recipients.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var user in recipients)
        {
            notificationRepository.Add(
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    SaleId = sale.Id,
                    IsRead = false,
                    CreatedAt = now,
                });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationListModel> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var unreadCount = await notificationRepository.CountUnreadForUserAsync(userId, cancellationToken);
        var items = await notificationRepository.GetForUserAsync(userId, 50, cancellationToken);

        return new NotificationListModel
        {
            UnreadCount = unreadCount,
            Items = items
                .Select(n => new NotificationItemModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    SaleId = n.SaleId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("O"),
                })
                .ToList(),
        };
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await notificationRepository.GetByIdTrackedAsync(notificationId, cancellationToken)
            ?? throw new KeyNotFoundException("Notificación no encontrada.");

        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException("No tienes permiso para marcar esta notificación.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<User>> ResolveCustomerRecipientsAsync(
        Sale sale,
        CancellationToken cancellationToken)
    {
        var user = await ResolveUserForSaleAsync(sale, cancellationToken);
        return user is null ? [] : [user];
    }

    private async Task<User?> ResolveUserForSaleAsync(Sale sale, CancellationToken cancellationToken)
    {
        if (sale.CustomerId.HasValue)
        {
            var byCustomer = await userRepository.GetByCustomerIdAsync(sale.CustomerId.Value, cancellationToken);
            if (byCustomer is not null)
            {
                return byCustomer;
            }
        }

        if (!string.IsNullOrWhiteSpace(sale.CustomerEmail))
        {
            return await userRepository.GetByEmailAsync(sale.CustomerEmail.Trim(), cancellationToken);
        }

        return null;
    }

    private static (string Title, string Message, string? Type) BuildNotificationContent(
        string orderNumber,
        FulfillmentStatus status,
        FulfillmentNotificationRecipient recipient) => (status, recipient) switch
    {
        (FulfillmentStatus.Shipped, FulfillmentNotificationRecipient.Customer) => (
            "Pedido enviado",
            $"Tu pedido {orderNumber} fue enviado y está en camino.",
            "order_shipped"),
        (FulfillmentStatus.Delivered, FulfillmentNotificationRecipient.Admins) => (
            "Entrega confirmada",
            $"El cliente confirmó la entrega del pedido {orderNumber}.",
            "order_delivered"),
        _ => (string.Empty, string.Empty, null),
    };
}
