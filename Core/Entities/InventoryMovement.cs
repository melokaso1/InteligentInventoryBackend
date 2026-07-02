using Core.Enums;

namespace Core.Entities;

public class InventoryMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public StockMovementType Type { get; set; }
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
