namespace Api.Dtos;

public sealed class ProductDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SaleUnit { get; set; } = "unit";
    public bool AllowsFractional { get; set; }
    public decimal Stock { get; set; }
    public decimal MaxStock { get; set; }
    public string Status { get; set; } = "active";
    public string Icon { get; set; } = "inventory_2";
    public string Description { get; set; } = string.Empty;
    public string? UnitContentLabel { get; set; }
}

public sealed class CreateProductRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int MaxStock { get; set; }
    public string? Status { get; set; }
    public string Icon { get; set; } = "inventory_2";
    public string Description { get; set; } = string.Empty;
    public string Warehouse { get; set; } = "El Plonsazo Norte";
}

public sealed class UpdateProductRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int MaxStock { get; set; }
    public string? Status { get; set; }
    public string Icon { get; set; } = "inventory_2";
    public string Description { get; set; } = string.Empty;
    public string Warehouse { get; set; } = "El Plonsazo Norte";
}

public sealed class ProductStatsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int ArchivedProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

public sealed class ProductStatusPatchRequest
{
    public string Status { get; set; } = string.Empty;
}
