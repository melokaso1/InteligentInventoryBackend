using Domain.Enums;
using Pgvector;

namespace Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int MaxStock { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public string Icon { get; set; } = "inventory_2";
    public string Description { get; set; } = string.Empty;
    public string Warehouse { get; set; } = "El Plonsazo Norte";
    public Vector? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InventoryMovement> Movements { get; set; } = [];
    public ICollection<SaleLineItem> SaleLineItems { get; set; } = [];
}
