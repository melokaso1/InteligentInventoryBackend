using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<PagedResult<Product>> GetPagedAsync(
        string? query,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            dbQuery = dbQuery.Where(p => p.Code.ToLower().Contains(term) || p.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim().ToLower();
            dbQuery = dbQuery.Where(p => p.Category.ToLower() == normalized);
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
        var dbQuery = context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            dbQuery = dbQuery.Where(p => p.Code.ToLower().Contains(term) || p.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim().ToLower();
            dbQuery = dbQuery.Where(p => p.Category.ToLower() == normalized);
        }

        if (!string.IsNullOrWhiteSpace(warehouse))
        {
            var normalized = warehouse.Trim().ToLower();
            dbQuery = dbQuery.Where(p => p.Warehouse.ToLower() == normalized);
        }

        return await dbQuery.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().ToListAsync(cancellationToken);

    public Task<List<Product>> GetAllOrderedByCodeAsync(CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().OrderBy(p => p.Code).ToListAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(p => p.Code == code, cancellationToken);

    public Task<bool> ExistsByCodeExceptIdAsync(Guid id, string code, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(p => p.Id != id && p.Code == code, cancellationToken);

    public Task<Dictionary<Guid, Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default) =>
        context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountByStatusAsync(ProductStatus status, CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().CountAsync(p => p.Status == status, cancellationToken);

    public Task<decimal> SumInventoryValueAsync(CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().SumAsync(p => p.Price * p.Stock, cancellationToken);

    public void Add(Product entity) => context.Products.Add(entity);

    public void Remove(Product entity) => context.Products.Remove(entity);
}
