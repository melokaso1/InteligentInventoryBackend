using Domain.Enums;

namespace Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public decimal Price { get; set; }
    public SaleMeasureUnit SaleUnit { get; set; } = SaleMeasureUnit.Unit;
    public decimal? UnitContentAmount { get; set; }
    public SaleMeasureUnit? UnitContentMeasure { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public string Icon { get; set; } = "inventory_2";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Inventory> Inventories { get; set; } = [];
    public ICollection<ProductEmbedding> Embeddings { get; set; } = [];
    public ICollection<InventoryMovement> Movements { get; set; } = [];
    public ICollection<SaleLineItem> SaleLineItems { get; set; } = [];
}
