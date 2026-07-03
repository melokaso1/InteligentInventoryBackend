namespace Api.Dtos;

public sealed class SaleDto
{
    public Guid Id { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Origin { get; set; } = "manual";
    public string Date { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = "pending";
    public string? TaxId { get; set; }
    public List<SaleLineItemDto> LineItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
}

public sealed class SaleLineItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "inventory_2";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string MeasureUnit { get; set; } = "unit";
}

public sealed class SaleMetricsDto
{
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ChatbotSales { get; set; }
    public int ManualSales { get; set; }
    public int PendingSales { get; set; }
    public int InvoicedSales { get; set; }
}

public sealed class CreateSaleRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Origin { get; set; } = "manual";
    public string Status { get; set; } = "confirmed";
    public List<CreateSaleLineItemRequest> LineItems { get; set; } = [];
}

public sealed class CreateSaleLineItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? MeasureUnit { get; set; }
}

public sealed class CreateSaleFromChatbotLineItemRequest
{
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? MeasureUnit { get; set; }
}

public sealed class CreateSaleFromChatbotRequest
{
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? MeasureUnit { get; set; }
    public List<CreateSaleFromChatbotLineItemRequest>? LineItems { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

public sealed class CreateSaleFromChatbotResponse
{
    public Guid SaleId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
}
