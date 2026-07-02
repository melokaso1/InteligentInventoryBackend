namespace Application.Models;

public sealed class DashboardKpiModel
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Value { get; init; }
    public required string Change { get; init; }
    public required string ChangeType { get; init; }
    public required string Icon { get; init; }
    public required string IconBg { get; init; }
    public required string IconColor { get; init; }
}

public sealed class LowStockItemModel
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Sku { get; init; }
    public int CurrentStock { get; init; }
    public int ReorderLevel { get; init; }
    public required string Status { get; init; }
}

public sealed class ActivityItemModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Time { get; init; }
    public required string DotBg { get; init; }
    public required string DotBorder { get; init; }
}
