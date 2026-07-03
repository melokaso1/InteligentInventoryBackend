using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class DispatchService(
    ISaleRepository saleRepository,
    IUserRepository userRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork) : IDispatchService
{
    public async Task<PagedResult<Sale>> GetDispatchSalesAsync(
        DispatchQueryModel query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var status = ParseFulfillmentStatus(query.FulfillmentStatus);

        return await saleRepository.GetPagedForDispatchAsync(status, page, pageSize, cancellationToken);
    }

    public async Task<Sale> UpdateFulfillmentStatusAsync(
        Guid saleId,
        FulfillmentStatus status,
        CancellationToken cancellationToken = default)
    {
        var sale = await saleRepository.GetByIdTrackedWithDetailsAsync(saleId, cancellationToken)
            ?? throw new KeyNotFoundException("Pedido no encontrado.");

        ValidateAdminTransition(sale.FulfillmentStatus, status);

        ApplyFulfillmentStatusChange(sale, status);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var recipient = status switch
        {
            FulfillmentStatus.Shipped => FulfillmentNotificationRecipient.Customer,
            FulfillmentStatus.Delivered => FulfillmentNotificationRecipient.None,
            _ => FulfillmentNotificationRecipient.None,
        };

        await notificationService.NotifySaleFulfillmentChangeAsync(sale, status, recipient, cancellationToken);

        return sale;
    }

    public async Task<PagedResult<Sale>> GetMyOrdersAsync(
        Guid userId,
        MyOrdersQueryModel query,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var status = ParseFulfillmentStatus(query.FulfillmentStatus);

        return await saleRepository.GetPagedForUserAsync(
            userId,
            user.Email,
            user.CustomerId,
            status,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<Sale> ConfirmDeliveryAsync(
        Guid saleId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

        var ownsSale = await saleRepository.UserOwnsSaleAsync(
            saleId,
            userId,
            user.Email,
            user.CustomerId,
            cancellationToken);

        if (!ownsSale)
        {
            throw new UnauthorizedAccessException("No tienes permiso para confirmar este pedido.");
        }

        var sale = await saleRepository.GetByIdTrackedWithDetailsAsync(saleId, cancellationToken)
            ?? throw new KeyNotFoundException("Pedido no encontrado.");

        if (sale.FulfillmentStatus != FulfillmentStatus.Shipped)
        {
            throw new InvalidOperationException("Solo puedes confirmar la entrega cuando el pedido está enviado.");
        }

        ApplyFulfillmentStatusChange(sale, FulfillmentStatus.Delivered);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notificationService.NotifySaleFulfillmentChangeAsync(
            sale,
            FulfillmentStatus.Delivered,
            FulfillmentNotificationRecipient.Admins,
            cancellationToken);

        return sale;
    }

    private static void ApplyFulfillmentStatusChange(Sale sale, FulfillmentStatus status)
    {
        var now = DateTime.UtcNow;
        sale.FulfillmentStatus = status;

        switch (status)
        {
            case FulfillmentStatus.Preparing when sale.PreparingSince is null:
                sale.PreparingSince = now;
                break;
            case FulfillmentStatus.Shipped:
                sale.ShippedAt = now;
                sale.PreparingSince ??= now;
                break;
            case FulfillmentStatus.Delivered:
                sale.DeliveredAt = now;
                sale.ShippedAt ??= now;
                sale.PreparingSince ??= now;
                break;
        }
    }

    private static void ValidateAdminTransition(FulfillmentStatus current, FulfillmentStatus next)
    {
        var valid = (current, next) switch
        {
            (FulfillmentStatus.Preparing, FulfillmentStatus.Shipped) => true,
            (FulfillmentStatus.Shipped, FulfillmentStatus.Delivered) => true,
            (FulfillmentStatus.Preparing, FulfillmentStatus.Delivered) => true,
            _ when current == next => true,
            _ => false,
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                $"No se puede cambiar el estado de despacho de '{ToApiValue(current)}' a '{ToApiValue(next)}'.");
        }
    }

    private static FulfillmentStatus? ParseFulfillmentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "preparing" => FulfillmentStatus.Preparing,
            "shipped" => FulfillmentStatus.Shipped,
            "delivered" => FulfillmentStatus.Delivered,
            _ => throw new InvalidOperationException("Estado de despacho inválido. Valores: preparing, shipped, delivered."),
        };
    }

    private static string ToApiValue(FulfillmentStatus status) => status switch
    {
        FulfillmentStatus.Preparing => "preparing",
        FulfillmentStatus.Shipped => "shipped",
        FulfillmentStatus.Delivered => "delivered",
        _ => "preparing",
    };
}
