namespace Application.Models;

public sealed class InventoryQueryModel
{
    public string? Query { get; init; }
    public string? Category { get; init; }
    public string? Warehouse { get; init; }
    public string? StockLevel { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class InventoryStatsModel
{
    public int TotalItems { get; init; }
    public decimal TotalUnits { get; init; }
    public decimal TotalValue { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }
}

public sealed class InventoryMovementQueryModel
{
    public string? Type { get; init; }
    public string? ProductCode { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class AdjustmentModel
{
    public Guid? ProductId { get; init; }
    public string? ProductCode { get; init; }
    public decimal QuantityChange { get; init; }
    public string? Reason { get; init; }
    public string? Detail { get; init; }
}
