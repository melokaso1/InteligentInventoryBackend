namespace Application.Models;

public sealed class ChatMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ChatOperationSummary
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
}

public sealed class ChatMessageResult
{
    public string Response { get; set; } = string.Empty;
    public string State { get; set; } = "idle";
    public string? InvoiceNumber { get; set; }
    public IReadOnlyList<string>? Chips { get; set; }
    public ChatOperationSummary? OperationSummary { get; set; }
}
