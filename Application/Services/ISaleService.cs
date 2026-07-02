using Application.Models;
using Domain.Entities;

namespace Application.Services;

public interface ISaleService
{
    Task<PagedResult<Sale>> GetSalesAsync(SalesQueryModel query, CancellationToken cancellationToken = default);
    Task<SaleMetricsModel> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sale> CreateManualSaleAsync(CreateSaleModel request, CancellationToken cancellationToken = default);
    Task<Invoice> CreateInvoiceAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task<ChatbotSaleResult> CreateSaleFromChatbotAsync(string productCode, int quantity, string customerName, string customerEmail, string? sessionId = null, CancellationToken cancellationToken = default);
}
