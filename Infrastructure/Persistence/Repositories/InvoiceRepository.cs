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
            .Include(i => i.Sale)
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

    public async Task<PagedResult<Invoice>> GetPagedForUserAsync(
        Guid userId,
        string userEmail,
        Guid? customerId,
        int page,
        int pageSize,
        InvoiceStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyUserOwnershipFilter(
            context.Invoices
                .AsNoTracking()
                .Include(i => i.LineItems)
                .Include(i => i.Customer)
                .Include(i => i.Sale)
                    .ThenInclude(s => s!.ChatSession),
            userId,
            userEmail,
            customerId);

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

    public Task<Invoice?> GetByIdTrackedForPaymentAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Customer)
            .Include(i => i.Sale)
                .ThenInclude(s => s!.ChatSession)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<bool> UserOwnsInvoiceAsync(
        Guid invoiceId,
        Guid userId,
        string userEmail,
        Guid? customerId,
        CancellationToken cancellationToken = default) =>
        ApplyUserOwnershipFilter(
            context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Sale)
                    .ThenInclude(s => s!.ChatSession),
            userId,
            userEmail,
            customerId)
            .AnyAsync(i => i.Id == invoiceId, cancellationToken);

    public Task<List<Invoice>> GetRecentAsync(int limit, CancellationToken cancellationToken = default) =>
        context.Invoices.AsNoTracking().OrderByDescending(i => i.IssueDate).Take(limit).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Invoices.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default) =>
        context.Invoices.AsNoTracking().CountAsync(i => i.Status == status, cancellationToken);

    public async Task<decimal> SumTotalAsync(CancellationToken cancellationToken = default) =>
        await context.Invoices.AsNoTracking().SumAsync(i => (decimal?)i.Total, cancellationToken) ?? 0m;

    public void Add(Invoice entity) => context.Invoices.Add(entity);

    private static IQueryable<Invoice> ApplyUserOwnershipFilter(
        IQueryable<Invoice> query,
        Guid userId,
        string userEmail,
        Guid? customerId)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        return query.Where(i =>
            (customerId != null && i.CustomerId == customerId) ||
            (i.Customer != null && i.Customer.Email.ToLower() == normalizedEmail) ||
            (i.Sale != null && i.Sale.ChatSession != null && i.Sale.ChatSession.UserId == userId));
    }
}
