namespace Api.Dtos;

public sealed class InventoryItemDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string StockLevel { get; set; } = "high";
    public decimal StockPercent { get; set; }
    public string Icon { get; set; } = "inventory_2";
}

public sealed class InventoryStatsDto
{
    public int TotalItems { get; set; }
    public decimal TotalUnits { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}

public sealed class StockMovementDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "adjustment";
    public string Sku { get; set; } = string.Empty;
    public string Change { get; set; } = "0";
    public string Timestamp { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class AdjustmentRequest
{
    public Guid? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public decimal QuantityChange { get; set; }
    public string Reason { get; set; } = "Ajuste manual";
    public string Detail { get; set; } = string.Empty;
}
