using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions;

public interface IInventoryMovementRepository
{
    Task<PagedResult<InventoryMovement>> GetPagedAsync(StockMovementType? type, string? productCode, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<InventoryMovement>> GetRecentWithProductAsync(int limit, CancellationToken cancellationToken = default);
    void Add(InventoryMovement entity);
    void AddRange(IEnumerable<InventoryMovement> entities);
}
