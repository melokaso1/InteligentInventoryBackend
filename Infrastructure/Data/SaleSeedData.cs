using Core.Entities;
using Core.Enums;

namespace Infrastructure.Data;

internal static class SaleSeedData
{
    internal static IEnumerable<Sale> Create(IReadOnlyDictionary<string, Product> products)
    {
        var mj = products["PLZ-MJ-001"];
        var tussi = products["PLZ-TUS-015"];
        var popper = products["PLZ-POP-007"];
        var mdm = products["PLZ-MDM-088"];
        var gel = products["PLZ-LSD-044"];
        var hongos = products["PLZ-HNG-033"];

        return
        [
            new Sale
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-94210",
                CustomerName = "Jonathan Doe",
                CustomerEmail = "jonathan.doe@corp.com",
                Origin = SaleOrigin.Chatbot,
                Status = SaleStatus.Invoiced,
                Subtotal = 2_250_000,
                Tax = 180_000,
                Total = 2_430_000,
                CreatedAt = new DateTime(2023, 10, 24, 14, 22, 0, DateTimeKind.Utc),
                LineItems =
                [
                    new SaleLineItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = mj.Id,
                        Description = $"{mj.Name} ({mj.Code})",
                        Quantity = 50,
                        UnitPrice = mj.Price,
                    },
                ],
            },
            new Sale
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-94211",
                CustomerName = "Alice Smith",
                CustomerEmail = "alice.smith@corp.com",
                Origin = SaleOrigin.Manual,
                Status = SaleStatus.Pending,
                Subtotal = 508_000,
                Tax = 40_640,
                Total = 548_640,
                CreatedAt = new DateTime(2023, 10, 24, 15, 5, 0, DateTimeKind.Utc),
                LineItems =
                [
                    new SaleLineItem { Id = Guid.NewGuid(), ProductId = tussi.Id, Description = $"{tussi.Name} ({tussi.Code})", Quantity = 2, UnitPrice = tussi.Price },
                    new SaleLineItem { Id = Guid.NewGuid(), ProductId = popper.Id, Description = $"{popper.Name} ({popper.Code})", Quantity = 3, UnitPrice = popper.Price },
                ],
            },
            new Sale
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-94212",
                CustomerName = "Robert King",
                CustomerEmail = "robert.king@corp.com",
                Origin = SaleOrigin.Chatbot,
                Status = SaleStatus.Confirmed,
                Subtotal = 936_000,
                Tax = 74_880,
                Total = 1_010_880,
                CreatedAt = new DateTime(2023, 10, 23, 9, 12, 0, DateTimeKind.Utc),
                LineItems =
                [
                    new SaleLineItem { Id = Guid.NewGuid(), ProductId = mdm.Id, Description = $"{mdm.Name} ({mdm.Code})", Quantity = 2, UnitPrice = mdm.Price },
                    new SaleLineItem { Id = Guid.NewGuid(), ProductId = gel.Id, Description = $"{gel.Name} ({gel.Code})", Quantity = 10, UnitPrice = gel.Price },
                ],
            },
            new Sale
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-94213",
                CustomerName = "Elena Lopez",
                CustomerEmail = "elena.lopez@corp.com",
                Origin = SaleOrigin.Manual,
                Status = SaleStatus.Cancelled,
                Subtotal = 448_000,
                Tax = 35_840,
                Total = 483_840,
                CreatedAt = new DateTime(2023, 10, 23, 11, 30, 0, DateTimeKind.Utc),
                LineItems =
                [
                    new SaleLineItem { Id = Guid.NewGuid(), ProductId = hongos.Id, Description = $"{hongos.Name} ({hongos.Code})", Quantity = 4, UnitPrice = hongos.Price },
                ],
            },
        ];
    }
}
