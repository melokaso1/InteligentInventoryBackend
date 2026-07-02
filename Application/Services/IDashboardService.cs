using Application.Models;

namespace Application.Services;

public interface IDashboardService
{
    Task<List<DashboardKpiModel>> GetKpisAsync(CancellationToken cancellationToken = default);
    Task<List<LowStockItemModel>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<List<ActivityItemModel>> GetActivityAsync(int limit, CancellationToken cancellationToken = default);
}
