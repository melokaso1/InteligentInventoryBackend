namespace Domain.Entities;

public class Inventory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int CurrentStock { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InventoryMovement> Movements { get; set; } = [];
}
