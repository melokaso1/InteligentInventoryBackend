namespace Application.Models;

public sealed class ChatMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StateJson { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
}

public sealed class ChatOperationSummary
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string MeasureUnit { get; set; } = "unit";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
}

public sealed class ChatProductOffer
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Stock { get; set; }
    public string SaleUnit { get; set; } = "unit";
}

public sealed class ChatMessageResult
{
    public string Response { get; set; } = string.Empty;
    public string State { get; set; } = "idle";
    public string? StateJson { get; set; }
    public string? InvoiceNumber { get; set; }
    public IReadOnlyList<string>? Chips { get; set; }
    public ChatOperationSummary? OperationSummary { get; set; }
    public IReadOnlyList<ChatProductOffer>? Offers { get; set; }
    public int? OffersTotalCount { get; set; }
}

public sealed class ChatHistoryMessage
{
    public string SenderType { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}

public sealed class ChatbotHealthStatus
{
    public string Status { get; set; } = "ok";
    public string Chatbot { get; set; } = "available";
}
