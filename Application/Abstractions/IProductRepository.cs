using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetPagedAsync(string? query, string? category, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Product>> GetFilteredAsync(string? query, string? category, string? warehouse, CancellationToken cancellationToken = default);
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetAllOrderedByCodeAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeExceptIdAsync(Guid id, string code, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, Product>> GetByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(ProductStatus status, CancellationToken cancellationToken = default);
    Task<decimal> SumInventoryValueAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctCategoriesAsync(CancellationToken cancellationToken = default);
    void Add(Product entity);
    void Remove(Product entity);
}
