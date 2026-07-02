using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class SaleRepository(AppDbContext context) : ISaleRepository
{
    public async Task<PagedResult<Sale>> GetPagedAsync(
        DateTime? from,
        DateTime? to,
        SaleOrigin? origin,
        SaleStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildBaseQuery();

        if (from.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= to.Value);
        }

        if (origin.HasValue)
        {
            query = query.Where(s => s.Origin == origin.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Sale> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public Task<Sale?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        BuildBaseQuery().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Sale?> GetByIdTrackedWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Sales
            .Include(s => s.LineItems)
            .ThenInclude(li => li.Product)
            .Include(s => s.Invoice)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<List<Sale>> GetRecentAsync(int limit, CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().OrderByDescending(s => s.CreatedAt).Take(limit).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountByOriginAsync(SaleOrigin origin, CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().CountAsync(s => s.Origin == origin, cancellationToken);

    public Task<int> CountByStatusAsync(SaleStatus status, CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().CountAsync(s => s.Status == status, cancellationToken);

    public async Task<decimal> SumTotalAsync(CancellationToken cancellationToken = default) =>
        await context.Sales.AsNoTracking().SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;

    public async Task<decimal> SumTotalByDateRangeAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default) =>
        await context.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromInclusive && s.CreatedAt < toExclusive)
            .SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;

    public void Add(Sale entity) => context.Sales.Add(entity);

    private IQueryable<Sale> BuildBaseQuery() =>
        context.Sales
            .AsNoTracking()
            .Include(s => s.LineItems)
            .ThenInclude(li => li.Product)
            .Include(s => s.Invoice)
            .AsQueryable();
}
