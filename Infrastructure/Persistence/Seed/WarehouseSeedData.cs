using Domain.Constants;
using Domain.Entities;

namespace Infrastructure.Persistence.Seed;

internal static class WarehouseSeedData
{
    internal static List<Warehouse> Create() =>
    [
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = WarehouseNames.CentralBogota,
            Location = "Bogotá D.C.",
            IsActive = true,
            IsDefault = true,
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = WarehouseNames.AlmacenNorte,
            Location = "Medellín",
            IsActive = true,
            IsDefault = false,
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Name = WarehouseNames.BodegaSur,
            Location = "Cali",
            IsActive = true,
            IsDefault = false,
        },
    ];
}
