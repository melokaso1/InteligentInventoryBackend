using Core.Enums;

namespace Core.Entities;

public class Sale
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public SaleOrigin Origin { get; set; }
    public SaleStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SaleLineItem> LineItems { get; set; } = [];
    public Invoice? Invoice { get; set; }
}
