namespace Api.Dtos;

public sealed class UpdateFulfillmentStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? SaleId { get; set; }
    public Guid? InvoiceId { get; set; }
    public bool IsRead { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public sealed class NotificationListDto
{
    public int UnreadCount { get; set; }
    public List<NotificationDto> Items { get; set; } = [];
}
