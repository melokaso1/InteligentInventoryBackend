namespace Application.Models;

public sealed class DispatchQueryModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? FulfillmentStatus { get; set; }
}

public sealed class MyOrdersQueryModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? FulfillmentStatus { get; set; }
}

public sealed class NotificationListModel
{
    public int UnreadCount { get; set; }
    public List<NotificationItemModel> Items { get; set; } = [];
}

public sealed class NotificationItemModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? SaleId { get; set; }
    public bool IsRead { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
