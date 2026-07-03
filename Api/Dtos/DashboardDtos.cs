namespace Api.Dtos;

public sealed class DashboardKpiDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Change { get; set; } = string.Empty;
    public string ChangeType { get; set; } = "neutral";
    public string Icon { get; set; } = string.Empty;
    public string IconBg { get; set; } = string.Empty;
    public string IconColor { get; set; } = string.Empty;
}

public sealed class LowStockItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public string Status { get; set; } = "low_stock";
}

public sealed class ActivityItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string DotBg { get; set; } = string.Empty;
    public string DotBorder { get; set; } = string.Empty;
}
