using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions;

public interface ISaleRepository
{
    Task<PagedResult<Sale>> GetPagedAsync(DateTime? from, DateTime? to, SaleOrigin? origin, SaleStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdTrackedWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Sale>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountByOriginAsync(SaleOrigin origin, CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(SaleStatus status, CancellationToken cancellationToken = default);
    Task<decimal> SumTotalAsync(CancellationToken cancellationToken = default);
    Task<decimal> SumTotalByDateRangeAsync(DateTime fromInclusive, DateTime toExclusive, CancellationToken cancellationToken = default);
    void Add(Sale entity);
}
