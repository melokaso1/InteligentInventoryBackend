namespace Application.Models;

public sealed class InvoiceQueryModel
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Status { get; init; }
}

public sealed class InvoiceStatsModel
{
    public int TotalInvoices { get; init; }
    public int PaidInvoices { get; init; }
    public int PendingInvoices { get; init; }
    public int OverdueInvoices { get; init; }
    public int DraftInvoices { get; init; }
    public decimal TotalBilledAmount { get; init; }
}
