using Application.Abstractions;
using Application.Common;
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
        var query = ApplyListFilters(BuildBaseQuery(), from, to, origin, status);

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
            .Include(s => s.ChatSession)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<List<Sale>> GetRecentAsync(int limit, CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().OrderByDescending(s => s.CreatedAt).Take(limit).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountFilteredAsync(
        DateTime? from,
        DateTime? to,
        SaleOrigin? origin,
        SaleStatus? status,
        CancellationToken cancellationToken = default) =>
        ApplyListFilters(context.Sales.AsNoTracking(), from, to, origin, status).CountAsync(cancellationToken);

    public Task<int> CountByOriginAsync(SaleOrigin origin, CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().CountAsync(s => s.Origin == origin, cancellationToken);

    public Task<int> CountByStatusAsync(SaleStatus status, CancellationToken cancellationToken = default) =>
        context.Sales.AsNoTracking().CountAsync(s => s.Status == status, cancellationToken);

    public async Task<decimal> SumTotalAsync(CancellationToken cancellationToken = default) =>
        await context.Sales.AsNoTracking().SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;

    public async Task<decimal> SumTotalFilteredAsync(
        DateTime? from,
        DateTime? to,
        SaleOrigin? origin,
        SaleStatus? status,
        CancellationToken cancellationToken = default) =>
        await ApplyListFilters(context.Sales.AsNoTracking(), from, to, origin, status)
            .SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;

    public async Task<decimal> SumTotalByDateRangeAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default) =>
        await context.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromInclusive && s.CreatedAt < toExclusive)
            .SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;

    public void Add(Sale entity) => context.Sales.Add(entity);

    public async Task<PagedResult<Sale>> GetPagedForDispatchAsync(
        FulfillmentStatus? fulfillmentStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDispatchQuery();

        if (fulfillmentStatus.HasValue)
        {
            query = query.Where(s => s.FulfillmentStatus == fulfillmentStatus.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.DeliveredAt ?? s.ShippedAt ?? s.PreparingSince ?? s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Sale> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<Sale>> GetPagedForUserAsync(
        Guid userId,
        string userEmail,
        Guid? customerId,
        FulfillmentStatus? fulfillmentStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyUserOwnershipFilter(BuildDispatchQuery(), userId, userEmail, customerId);

        if (fulfillmentStatus.HasValue)
        {
            query = query.Where(s => s.FulfillmentStatus == fulfillmentStatus.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Sale> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public Task<bool> UserOwnsSaleAsync(
        Guid saleId,
        Guid userId,
        string userEmail,
        Guid? customerId,
        CancellationToken cancellationToken = default) =>
        ApplyUserOwnershipFilter(
            context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.ChatSession),
            userId,
            userEmail,
            customerId)
            .AnyAsync(s => s.Id == saleId, cancellationToken);

    private static IQueryable<Sale> ApplyUserOwnershipFilter(
        IQueryable<Sale> query,
        Guid userId,
        string userEmail,
        Guid? customerId)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        return query.Where(s =>
            (customerId != null && s.CustomerId == customerId) ||
            (s.Customer != null && s.Customer.Email.ToLower() == normalizedEmail) ||
            (s.ChatSession != null && s.ChatSession.UserId == userId) ||
            s.CustomerEmail.ToLower() == normalizedEmail);
    }

    private static IQueryable<Sale> ApplyListFilters(
        IQueryable<Sale> query,
        DateTime? from,
        DateTime? to,
        SaleOrigin? origin,
        SaleStatus? status)
    {
        if (from.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= ColombiaTimeHelper.ToUtcStartOfColombiaDay(from.Value));
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= ColombiaTimeHelper.ToUtcEndOfColombiaDay(to.Value));
        }

        if (origin.HasValue)
        {
            query = query.Where(s => s.Origin == origin.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return query;
    }

    private IQueryable<Sale> BuildBaseQuery() =>
        context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.LineItems)
            .ThenInclude(li => li.Product)
            .Include(s => s.Invoice)
            .Include(s => s.ChatSession)
            .AsQueryable();

    private IQueryable<Sale> BuildDispatchQuery() =>
        BuildBaseQuery()
            .Where(s => s.Status != SaleStatus.Cancelled)
            .Where(s => s.Invoice != null && s.Invoice.Status == InvoiceStatus.Paid)
            .Where(s => s.LineItems.Any());

}
