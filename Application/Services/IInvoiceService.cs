using Application.Models;
using Domain.Entities;

namespace Application.Services;

public interface IInvoiceService
{
    Task<PagedResult<Invoice>> GetInvoicesAsync(InvoiceQueryModel query, CancellationToken cancellationToken = default);
    Task<PagedResult<Invoice>> GetMyInvoicesAsync(Guid userId, InvoiceQueryModel query, CancellationToken cancellationToken = default);
    Task<InvoiceStatsModel> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> BuildPdfContentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice> CreateAsync(CreateInvoiceModel request, CancellationToken cancellationToken = default);
    Task<Invoice> CreateManualAsync(CreateManualInvoiceModel request, CancellationToken cancellationToken = default);
    Task<Invoice> PayInvoiceAsync(Guid invoiceId, PayInvoiceModel request, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
}
