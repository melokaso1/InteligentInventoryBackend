using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions;

public interface IInvoiceRepository
{
    Task<PagedResult<Invoice>> GetPagedAsync(int page, int pageSize, InvoiceStatus? status, CancellationToken cancellationToken = default);
    Task<PagedResult<Invoice>> GetPagedForUserAsync(
        Guid userId,
        string userEmail,
        Guid? customerId,
        int page,
        int pageSize,
        InvoiceStatus? status,
        CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdWithLineItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdTrackedForPaymentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UserOwnsInvoiceAsync(Guid invoiceId, Guid userId, string userEmail, Guid? customerId, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default);
    Task<decimal> SumTotalAsync(CancellationToken cancellationToken = default);
    void Add(Invoice entity);
}
