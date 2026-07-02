using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository(AppDbContext context) : IInventoryRepository
{
    private IQueryable<Inventory> BaseQuery() =>
        context.Inventories
            .AsNoTracking()
            .Include(i => i.Product)
            .ThenInclude(p => p.Category)
            .Include(i => i.Warehouse);

    public Task<Inventory?> GetByProductAndWarehouseAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        BaseQuery().FirstOrDefaultAsync(
            i => i.ProductId == productId && i.WarehouseId == warehouseId,
            cancellationToken);

    public Task<Inventory?> GetByProductAndWarehouseTrackedAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        context.Inventories
            .Include(i => i.Product)
            .ThenInclude(p => p.Category)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(
                i => i.ProductId == productId && i.WarehouseId == warehouseId,
                cancellationToken);

    public Task<Inventory?> GetDefaultByProductIdTrackedAsync(Guid productId, CancellationToken cancellationToken = default) =>
        context.Inventories
            .Include(i => i.Product)
            .ThenInclude(p => p.Category)
            .Include(i => i.Warehouse)
            .Where(i => i.ProductId == productId)
            .OrderByDescending(i => i.Warehouse.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<Inventory>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
        BaseQuery().OrderBy(i => i.Product.Code).ToListAsync(cancellationToken);

    public async Task<List<Inventory>> GetFilteredAsync(
        string? query,
        string? category,
        string? warehouse,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = BaseQuery();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(i =>
                i.Product.Code.ToLower().Contains(term) ||
                i.Product.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(i => i.Product.Category.Name.ToLower() == normalized);
        }

        if (!string.IsNullOrWhiteSpace(warehouse))
        {
            var normalized = warehouse.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(i => i.Warehouse.Name.ToLower() == normalized);
        }

        return await dbQuery.OrderBy(i => i.Product.Name).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Inventories.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> SumTotalUnitsAsync(CancellationToken cancellationToken = default) =>
        context.Inventories.AsNoTracking().SumAsync(i => i.CurrentStock, cancellationToken);

    public Task<decimal> SumInventoryValueAsync(CancellationToken cancellationToken = default) =>
        context.Inventories.AsNoTracking().SumAsync(i => i.Product.Price * i.CurrentStock, cancellationToken);

    public void Add(Inventory entity) => context.Inventories.Add(entity);
}
