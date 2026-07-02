namespace Core.Enums;

public enum ProductStatus
{
    Active,
    Inactive,
    OutOfStock,
    Archived
}

public enum StockMovementType
{
    Inbound,
    Adjustment,
    Outbound
}

public enum SaleStatus
{
    Invoiced,
    Pending,
    Confirmed,
    Cancelled
}

public enum SaleOrigin
{
    Manual,
    Chatbot
}

public enum InvoiceStatus
{
    Paid,
    Pending,
    Overdue,
    Draft
}
