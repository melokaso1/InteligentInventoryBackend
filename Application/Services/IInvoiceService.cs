using Application.Models;
using Domain.Entities;

namespace Application.Services;

public interface IInvoiceService
{
    Task<PagedResult<Invoice>> GetInvoicesAsync(InvoiceQueryModel query, CancellationToken cancellationToken = default);
    Task<InvoiceStatsModel> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> BuildPdfContentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice> CreateAsync(CreateInvoiceModel request, CancellationToken cancellationToken = default);
}
