namespace Api.Dtos;

public sealed class ChatMessageRequestDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ChatOperationSummaryDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
}

public sealed class ChatProductOfferDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Stock { get; set; }
    public string SaleUnit { get; set; } = "unit";
}

public sealed class ChatMessageResponseDto
{
    public string Response { get; set; } = string.Empty;
    public string State { get; set; } = "idle";
    public string? InvoiceNumber { get; set; }
    public IReadOnlyList<string>? Chips { get; set; }
    public ChatOperationSummaryDto? OperationSummary { get; set; }
    public IReadOnlyList<ChatProductOfferDto>? Offers { get; set; }
    public int? OffersTotalCount { get; set; }
}

public sealed class ChatHistoryMessageDto
{
    public string SenderType { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}
