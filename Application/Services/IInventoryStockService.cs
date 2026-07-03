using Application.Models;
using Domain.Entities;

namespace Application.Services;

public interface IInventoryStockService
{
    void ValidateSufficientStock(IReadOnlyList<StockDeductionLine> lines);

    Task<IReadOnlyList<InventoryMovement>> DeductStockAsync(
        IReadOnlyList<StockDeductionLine> lines,
        string reason,
        string detail,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);
}
