namespace Domain.Enums;

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

public enum ChatSenderType
{
    User,
    Bot,
    System
}

public enum SaleMeasureUnit
{
    Unit = 0,
    Gram = 1,
    Kilogram = 2,
    Milligram = 3,
    Milliliter = 4,
    Liter = 5,
}
