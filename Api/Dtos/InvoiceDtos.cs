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
    public string Source { get; set; } = "manual";
}

public sealed class CreateManualInvoiceRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? BillingNote { get; set; }
    public List<CreateManualInvoiceLineItemRequest> LineItems { get; set; } = [];
}

public sealed class CreateManualInvoiceLineItemRequest
{
    public Guid? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class InvoiceLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string MeasureUnit { get; set; } = "unit";
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

public sealed class CreateInvoiceRequest
{
    public string Client { get; set; } = string.Empty;
    public string BillingNote { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = [];
}

public sealed class CreateInvoiceLineItemRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class PayInvoiceRequest
{
    public string PaymentMethod { get; set; } = string.Empty;
}
