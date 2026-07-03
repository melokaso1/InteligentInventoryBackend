using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions;

public interface IDispatchService
{
    Task<PagedResult<Sale>> GetDispatchSalesAsync(DispatchQueryModel query, CancellationToken cancellationToken = default);
    Task<Sale> UpdateFulfillmentStatusAsync(Guid saleId, FulfillmentStatus status, CancellationToken cancellationToken = default);
    Task<PagedResult<Sale>> GetMyOrdersAsync(Guid userId, MyOrdersQueryModel query, CancellationToken cancellationToken = default);
    Task<Sale> ConfirmDeliveryAsync(Guid saleId, Guid userId, CancellationToken cancellationToken = default);
}
