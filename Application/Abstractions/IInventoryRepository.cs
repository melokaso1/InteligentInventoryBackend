using Domain.Entities;

namespace Application.Abstractions;

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<Inventory?> GetByProductAndWarehouseTrackedAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<Inventory?> GetDefaultByProductIdTrackedAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<List<Inventory>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<List<Inventory>> GetFilteredAsync(string? query, string? category, string? warehouse, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> SumTotalUnitsAsync(CancellationToken cancellationToken = default);
    Task<decimal> SumInventoryValueAsync(CancellationToken cancellationToken = default);
    void Add(Inventory entity);
}
