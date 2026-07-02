namespace Application.Models;

public sealed class SalesQueryModel
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Origin { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class CreateSaleModel
{
    public required string CustomerName { get; init; }
    public required string CustomerEmail { get; init; }
    public required string Origin { get; init; }
    public required string Status { get; init; }
    public required List<CreateSaleLineItemModel> LineItems { get; init; }
}

public sealed class CreateSaleLineItemModel
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public sealed class SaleMetricsModel
{
    public int TotalSales { get; init; }
    public decimal TotalRevenue { get; init; }
    public int ChatbotSales { get; init; }
    public int ManualSales { get; init; }
    public int PendingSales { get; init; }
    public int InvoicedSales { get; init; }
}
