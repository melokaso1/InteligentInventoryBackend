using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    private IQueryable<Product> BaseQuery() =>
        context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .ThenInclude(i => i.Warehouse);

    public async Task<PagedResult<Product>> GetPagedAsync(
        string? query,
        string? category,
        ProductStatus? status,
        int page,
        int pageSize,
        bool availableForSaleOnly = false,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = BaseQuery();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(p => p.Code.ToLower().Contains(term) || p.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(p => p.Category.Name.ToLower() == normalized);
        }

        if (status is not null)
        {
            dbQuery = dbQuery.Where(p => p.Status == status);
        }

        if (availableForSaleOnly)
        {
            dbQuery = dbQuery
                .Where(p => p.Status == ProductStatus.Active)
                .Where(p => p.Inventories.Any(i => i.CurrentStock > 0));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<List<Product>> GetFilteredAsync(
        string? query,
        string? category,
        string? warehouse,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = BaseQuery();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(p => p.Code.ToLower().Contains(term) || p.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(p => p.Category.Name.ToLower() == normalized);
        }

        if (!string.IsNullOrWhiteSpace(warehouse))
        {
            var normalized = warehouse.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(p => p.Inventories.Any(i => i.Warehouse.Name.ToLower() == normalized));
        }

        return await dbQuery.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        BaseQuery().ToListAsync(cancellationToken);

    public Task<List<Product>> GetAllOrderedByCodeAsync(CancellationToken cancellationToken = default) =>
        BaseQuery().OrderBy(p => p.Code).ToListAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        BaseQuery().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .ThenInclude(i => i.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .ThenInclude(i => i.Warehouse)
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(p => p.Code == code, cancellationToken);

    public Task<bool> ExistsByCodeExceptIdAsync(Guid id, string code, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(p => p.Id != id && p.Code == code, cancellationToken);

    public async Task<Dictionary<Guid, Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default) =>
        await context.Products
            .Include(p => p.Inventories)
            .ThenInclude(i => i.Warehouse)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountByStatusAsync(ProductStatus status, CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().CountAsync(p => p.Status == status, cancellationToken);

    public Task<decimal> SumInventoryValueAsync(CancellationToken cancellationToken = default) =>
        context.Inventories.AsNoTracking().SumAsync(i => i.Product.Price * i.CurrentStock, cancellationToken);

    public Task<List<string>> GetDistinctCategoriesAsync(CancellationToken cancellationToken = default) =>
        context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

    public void Add(Product entity) => context.Products.Add(entity);

    public void Remove(Product entity) => context.Products.Remove(entity);
}
