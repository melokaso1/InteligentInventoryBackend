using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository(AppDbContext context) : IInvoiceRepository
{
    public async Task<PagedResult<Invoice>> GetPagedAsync(
        int page,
        int pageSize,
        InvoiceStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Invoice> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public Task<Invoice?> GetByIdWithLineItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<List<Invoice>> GetRecentAsync(int limit, CancellationToken cancellationToken = default) =>
        context.Invoices.AsNoTracking().OrderByDescending(i => i.IssueDate).Take(limit).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Invoices.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default) =>
        context.Invoices.AsNoTracking().CountAsync(i => i.Status == status, cancellationToken);

    public async Task<decimal> SumTotalAsync(CancellationToken cancellationToken = default) =>
        await context.Invoices.AsNoTracking().SumAsync(i => (decimal?)i.Total, cancellationToken) ?? 0m;

    public void Add(Invoice entity) => context.Invoices.Add(entity);
}
