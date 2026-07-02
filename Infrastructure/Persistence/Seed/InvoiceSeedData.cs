using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Persistence.Seed;

internal static class InvoiceSeedData
{
    internal static IEnumerable<Invoice> Create(
        IReadOnlyDictionary<string, Product> products,
        IReadOnlyDictionary<string, Customer> customers)
    {
        var laptop = products["PLZ-LAP-001"];
        var monitor = products["PLZ-MON-001"];
        var keyboard = products["PLZ-KBD-001"];
        var ssd = products["PLZ-HDD-001"];
        var ups = products["PLZ-UPS-001"];
        var toner = products["PLZ-PRN-002"];
        var paper = products["PLZ-PAP-001"];
        var webcam = products["PLZ-WEB-001"];
        var desk = products["PLZ-DSK-001"];

        return
        [
            Inv("INV-2025-001", customers["carolina.mendez@techsolutions.co"], "TC", InvoiceStatus.Paid,
                new DateTime(2025, 6, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 12, 0, 0, 0, DateTimeKind.Utc),
                22_700_000, 1_816_000, 24_516_000,
                "Equipamiento de estaciones de trabajo — Q2",
                [Line(laptop, 5), Line(monitor, 1)]),
            Inv("INV-2025-002", customers["andres.vargas@grupoandina.co"], "GA", InvoiceStatus.Pending,
                new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                3_960_000, 316_800, 4_276_800,
                "Periféricos y almacenamiento para oficina Medellín",
                [Line(keyboard, 4), Line(ssd, 6)]),
            Inv("INV-2025-003", customers["laura.herrera@logisticaexpress.co"], "LE", InvoiceStatus.Overdue,
                new DateTime(2025, 6, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 18, 0, 0, 0, DateTimeKind.Utc),
                4_740_000, 379_200, 5_119_200,
                "Respaldo eléctrico para sala de servidores",
                [Line(ups, 3)]),
            Inv("INV-2025-004", customers["miguel.torres@constructorametro.co"], "CM", InvoiceStatus.Draft,
                new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc),
                8_400_000, 672_000, 9_072_000,
                "Mobiliario para nueva sede administrativa",
                [Line(desk, 5)]),
            Inv("INV-2025-005", customers["edu@edutech.co"], "EI", InvoiceStatus.Paid,
                new DateTime(2025, 6, 22, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 22, 0, 0, 0, DateTimeKind.Utc),
                2_455_000, 196_400, 2_651_400,
                "Consumibles y videoconferencia — aula virtual",
                [Line(toner, 5), Line(paper, 20), Line(webcam, 3)]),
            Inv("INV-2025-006", customers["oficinas@valle.co"], "OV", InvoiceStatus.Pending,
                new DateTime(2025, 6, 25, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 25, 0, 0, 0, DateTimeKind.Utc),
                5_710_000, 456_800, 6_166_800,
                "Renovación de monitores sala de juntas",
                [Line(monitor, 3), Line(products["PLZ-MON-002"], 2)]),
        ];
    }

    private static InvoiceLineItem Line(Product p, int qty) => new()
    {
        Id = Guid.NewGuid(),
        Description = $"{p.Name} ({p.Code})",
        Quantity = qty,
        UnitPrice = p.Price,
    };

    private static Invoice Inv(
        string number,
        Customer customer,
        string initials,
        InvoiceStatus status,
        DateTime issue,
        DateTime due,
        decimal subtotal,
        decimal tax,
        decimal total,
        string billingNote,
        List<InvoiceLineItem> lines) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = number,
        ClientName = customer.FullName,
        ClientInitials = initials,
        BillingNote = billingNote,
        Status = status,
        IssueDate = issue,
        DueDate = due,
        Subtotal = subtotal,
        Tax = tax,
        Total = total,
        LineItems = lines,
    };
}
