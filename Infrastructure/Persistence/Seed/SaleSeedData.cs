using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Persistence.Seed;

internal static class SaleSeedData
{
    internal static IEnumerable<Sale> Create(
        IReadOnlyDictionary<string, Product> products,
        IReadOnlyDictionary<string, Customer> customers)
    {
        var laptop = products["PLZ-LAP-001"];
        var keyboard = products["PLZ-KBD-001"];
        var mouse = products["PLZ-MSE-001"];
        var monitor = products["PLZ-MON-001"];
        var hub = products["PLZ-HUB-001"];
        var chair = products["PLZ-CHR-001"];

        var carolina = customers["carolina.mendez@techsolutions.co"];
        var andres = customers["andres.vargas@grupoandina.co"];
        var laura = customers["laura.herrera@logisticaexpress.co"];
        var miguel = customers["miguel.torres@constructorametro.co"];

        return
        [
            Sale("ORD-94210", carolina, SaleOrigin.Chatbot, SaleStatus.Invoiced,
                new DateTime(2025, 6, 24, 14, 22, 0, DateTimeKind.Utc), 21_250_000, 1_700_000, 22_950_000,
                [Line(laptop, 5)]),
            Sale("ORD-94211", andres, SaleOrigin.Manual, SaleStatus.Pending,
                new DateTime(2025, 6, 24, 15, 5, 0, DateTimeKind.Utc), 1_980_000, 158_400, 2_138_400,
                [Line(keyboard, 2), Line(mouse, 3)]),
            Sale("ORD-94212", laura, SaleOrigin.Chatbot, SaleStatus.Confirmed,
                new DateTime(2025, 6, 23, 9, 12, 0, DateTimeKind.Utc), 3_400_000, 272_000, 3_672_000,
                [Line(monitor, 2), Line(hub, 4)]),
            Sale("ORD-94213", miguel, SaleOrigin.Manual, SaleStatus.Cancelled,
                new DateTime(2025, 6, 23, 11, 30, 0, DateTimeKind.Utc), 1_780_000, 142_400, 1_922_400,
                [Line(chair, 2)]),
        ];
    }

    private static SaleLineItem Line(Product p, int qty) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = p.Id,
        Description = $"{p.Name} ({p.Code})",
        Quantity = qty,
        UnitPrice = p.Price,
    };

    private static Sale Sale(
        string orderNumber,
        Customer customer,
        SaleOrigin origin,
        SaleStatus status,
        DateTime createdAt,
        decimal subtotal,
        decimal tax,
        decimal total,
        List<SaleLineItem> lines) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = orderNumber,
        CustomerName = customer.FullName,
        CustomerEmail = customer.Email,
        Origin = origin,
        Status = status,
        Subtotal = subtotal,
        Tax = tax,
        Total = total,
        CreatedAt = createdAt,
        LineItems = lines,
    };
}
