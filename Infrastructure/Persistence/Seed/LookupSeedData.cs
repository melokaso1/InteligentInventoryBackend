using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Persistence.Seed;

internal static class LookupSeedData
{
    internal static IEnumerable<SaleStatusLookup> SaleStatuses() =>
    [
        new() { Id = (int)SaleStatus.Invoiced, Name = "invoiced" },
        new() { Id = (int)SaleStatus.Pending, Name = "pending" },
        new() { Id = (int)SaleStatus.Confirmed, Name = "confirmed" },
        new() { Id = (int)SaleStatus.Cancelled, Name = "cancelled" },
    ];

    internal static IEnumerable<SaleOriginLookup> SaleOrigins() =>
    [
        new() { Id = (int)SaleOrigin.Manual, Name = "manual" },
        new() { Id = (int)SaleOrigin.Chatbot, Name = "chatbot" },
    ];

    internal static IEnumerable<InvoiceStatusLookup> InvoiceStatuses() =>
    [
        new() { Id = (int)InvoiceStatus.Paid, Name = "paid" },
        new() { Id = (int)InvoiceStatus.Pending, Name = "pending" },
        new() { Id = (int)InvoiceStatus.Overdue, Name = "overdue" },
        new() { Id = (int)InvoiceStatus.Draft, Name = "draft" },
    ];

    internal static IEnumerable<MovementTypeLookup> MovementTypes() =>
    [
        new() { Id = (int)StockMovementType.Inbound, Name = "inbound" },
        new() { Id = (int)StockMovementType.Adjustment, Name = "adjustment" },
        new() { Id = (int)StockMovementType.Outbound, Name = "outbound" },
    ];
}
