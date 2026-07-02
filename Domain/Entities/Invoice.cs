using Domain.Enums;

namespace Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientInitials { get; set; } = string.Empty;
    public string BillingNote { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = [];
}
