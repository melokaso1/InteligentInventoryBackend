namespace Api.Dtos;

public sealed class InvoiceDto
{
    public Guid Id { get; set; }
    public string Client { get; set; } = string.Empty;
    public string ClientInitials { get; set; } = string.Empty;
    public string BillingNote { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "draft";
    public List<InvoiceLineItemDto> LineItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid? SaleId { get; set; }
}

public sealed class InvoiceLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class InvoiceStatsDto
{
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public int DraftInvoices { get; set; }
    public decimal TotalBilledAmount { get; set; }
}
