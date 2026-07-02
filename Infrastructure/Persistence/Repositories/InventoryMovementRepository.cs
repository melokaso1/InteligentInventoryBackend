using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class InventoryMovementRepository(AppDbContext context) : IInventoryMovementRepository
{
    public async Task<PagedResult<InventoryMovement>> GetPagedAsync(
        StockMovementType? type,
        string? productCode,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(m => m.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(productCode))
        {
            var normalized = productCode.Trim().ToLower();
            query = query.Where(m => m.Product.Code.ToLower() == normalized);
        }

        if (from.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryMovement> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public Task<List<InventoryMovement>> GetRecentWithProductAsync(int limit, CancellationToken cancellationToken = default) =>
        context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(InventoryMovement entity) => context.InventoryMovements.Add(entity);

    public void AddRange(IEnumerable<InventoryMovement> entities) => context.InventoryMovements.AddRange(entities);
}
