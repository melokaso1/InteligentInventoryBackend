namespace Application.Models;

public sealed class ProductQueryModel
{
    public string? Query { get; init; }
    public string? Category { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class CreateProductModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public int MaxStock { get; init; }
    public string? Status { get; init; }
    public required string Icon { get; init; }
    public required string Description { get; init; }
    public required string Warehouse { get; init; }
}

public sealed class UpdateProductModel : CreateProductModel
{
}

public sealed class ProductStatsModel
{
    public int TotalProducts { get; init; }
    public int ActiveProducts { get; init; }
    public int InactiveProducts { get; init; }
    public int OutOfStockProducts { get; init; }
    public int ArchivedProducts { get; init; }
    public decimal TotalInventoryValue { get; init; }
}
