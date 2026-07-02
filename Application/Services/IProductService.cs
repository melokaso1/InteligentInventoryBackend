using Application.Models;
using Domain.Entities;

namespace Application.Services;

public interface IProductService
{
    Task<PagedResult<Product>> GetProductsAsync(ProductQueryModel query, CancellationToken cancellationToken = default);
    Task<ProductStatsModel> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetProductsForExportAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product> CreateAsync(CreateProductModel request, CancellationToken cancellationToken = default);
    Task<Product> UpdateAsync(Guid id, UpdateProductModel request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product> DuplicateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product> PatchStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
}
