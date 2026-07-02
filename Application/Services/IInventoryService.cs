using Application.Models;
using Domain.Entities;

namespace Application.Services;

public interface IInventoryService
{
    Task<PagedResult<Product>> GetInventoryAsync(InventoryQueryModel query, CancellationToken cancellationToken = default);
    Task<InventoryStatsModel> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryMovement>> GetMovementsAsync(InventoryMovementQueryModel query, CancellationToken cancellationToken = default);
    Task<InventoryMovement> CreateAdjustmentAsync(AdjustmentModel request, CancellationToken cancellationToken = default);
    Task<List<Product>> GetInventoryForExportAsync(CancellationToken cancellationToken = default);
}
