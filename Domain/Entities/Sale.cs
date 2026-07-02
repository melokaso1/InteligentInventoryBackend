using Domain.Enums;

namespace Domain.Entities;

public class Sale
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public SaleOrigin Origin { get; set; }
    public SaleStatus Status { get; set; }
    public Guid? ChatSessionId { get; set; }
    public ChatSession? ChatSession { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SaleLineItem> LineItems { get; set; } = [];
    public Invoice? Invoice { get; set; }
}
