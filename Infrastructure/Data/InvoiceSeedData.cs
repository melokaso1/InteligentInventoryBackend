using Core.Entities;
using Core.Enums;

namespace Infrastructure.Data;

internal static class InvoiceSeedData
{
    internal static IEnumerable<Invoice> Create(IReadOnlyDictionary<string, Product> products)
    {
        var mj = products["PLZ-MJ-001"];
        var popper = products["PLZ-POP-007"];
        var lsd = products["PLZ-LSD-042"];
        var tussi = products["PLZ-TUS-015"];
        var gel = products["PLZ-LSD-044"];
        var ket = products["PLZ-KET-021"];
        var dmt = products["PLZ-DMT-012"];

        return
        [
            Inv("INV-2024-001", "Manolo \"El Fumeta\" García", "MG", InvoiceStatus.Paid,
                new DateTime(2024, 10, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 12, 0, 0, 0, DateTimeKind.Utc),
                2_706_000, 216_480, 2_922_480,
                [Line(mj, 50), Line(popper, 6)]),
            Inv("INV-2024-002", "Paca \"La Pastillera\" Jiménez", "PJ", InvoiceStatus.Pending,
                new DateTime(2024, 10, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 15, 0, 0, 0, DateTimeKind.Utc),
                3_420_000, 273_600, 3_693_600,
                [Line(lsd, 40), Line(tussi, 8), Line(gel, 5)]),
            Inv("INV-2024-003", "Vicente \"K-Hole\" Martínez", "VM", InvoiceStatus.Overdue,
                new DateTime(2024, 10, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 18, 0, 0, 0, DateTimeKind.Utc),
                1_092_000, 87_360, 1_179_360,
                [Line(ket, 2), Line(dmt, 1)]),
            Inv("INV-2024-004", "Rosario \"La Chalota\" Delgado", "RD", InvoiceStatus.Draft,
                new DateTime(2024, 10, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 20, 0, 0, 0, DateTimeKind.Utc),
                5_160_000, 412_800, 5_572_800,
                [Line(mj, 80), Line(products["PLZ-MDM-088"], 10)]),
            Inv("INV-2024-005", "Fermín \"El Perlador\" Iglesias", "FI", InvoiceStatus.Paid,
                new DateTime(2024, 10, 22, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 22, 0, 0, 0, DateTimeKind.Utc),
                4_736_000, 378_880, 5_114_880,
                [Line(products["PLZ-COC-099"], 8), Line(products["PLZ-HNG-034"], 4)]),
            Inv("INV-2024-006", "Encarna \"Popper Queen\" Torres", "ET", InvoiceStatus.Pending,
                new DateTime(2024, 10, 25, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 25, 0, 0, 0, DateTimeKind.Utc),
                2_558_000, 204_640, 2_762_640,
                [Line(popper, 20), Line(products["PLZ-POP-008"], 15)]),
        ];
    }

    private static InvoiceLineItem Line(Product p, int qty) => new()
    {
        Id = Guid.NewGuid(),
        Description = $"{p.Name} ({p.Code})",
        Quantity = qty,
        UnitPrice = p.Price,
    };

    private static Invoice Inv(string number, string client, string initials, InvoiceStatus status,
        DateTime issue, DateTime due, decimal subtotal, decimal tax, decimal total, List<InvoiceLineItem> lines) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = number,
        ClientName = client,
        ClientInitials = initials,
        BillingNote = "Factura El Plonsazo — datos de demostración.",
        Status = status,
        IssueDate = issue,
        DueDate = due,
        Subtotal = subtotal,
        Tax = tax,
        Total = total,
        LineItems = lines,
    };
}
