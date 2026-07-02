using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Persistence.Seed;

internal static class ProductSeedData
{
    internal static List<Product> Create() =>
    [
        P("PLZ-MJ-001", "Marihuana Sativa Indoor Premium", "Alucinógenos", 45000, 142, 200, "eco", "El Plonsazo Norte"),
        P("PLZ-MJ-002", "Marihuana Índica El Plonsazo", "Alucinógenos", 90000, 98, 150, "eco", "El Plonsazo Norte"),
        P("PLZ-MJ-003", "Marihuana Híbrida Blue Dream", "Alucinógenos", 104000, 76, 120, "eco", "El Plonsazo Norte"),
        P("PLZ-MJ-010", "Flores Premium Reserva", "Alucinógenos", 136000, 55, 80, "local_florist", "Montaña Alta"),
        P("PLZ-MJ-011", "Pre-rolled Sativa x6", "Alucinógenos", 72000, 120, 200, "smoking_rooms", "Central-A"),
        P("PLZ-MJ-012", "Aceite CBD 10% — El Plonsazo", "Alucinógenos", 180000, 38, 60, "water_drop", "Central-A"),
        P("PLZ-MJ-013", "Hash Marroquí Premium", "Alucinógenos", 128000, 28, 50, "grain", "Montaña Alta"),
        P("PLZ-LSD-042", "LSD-25 Blotter 200µg", "Alucinógenos", 50000, 8, 50, "science", "Búnker Psicodélico"),
        P("PLZ-LSD-043", "Microdosis LSD 10µg", "Alucinógenos", 32000, 12, 100, "science", "Búnker Psicodélico"),
        P("PLZ-LSD-044", "Gel Tabs LSD 150µg", "Alucinógenos", 60000, 45, 80, "science", "Búnker Psicodélico"),
        P("PLZ-LSD-045", "LSD Líquido 100µg/ml", "Alucinógenos", 220000, 6, 30, "biotech", "Búnker Psicodélico"),
        P("PLZ-POP-007", "Popper Rush XL (Amil)", "Inhalantes", 76000, 67, 100, "air", "Central-A"),
        P("PLZ-POP-008", "Popper Nitrito Isopropílico", "Inhalantes", 66000, 41, 80, "air", "Central-A"),
        P("PLZ-TUS-015", "Tussi Rosa 2C-B", "Estimulantes", 140000, 22, 80, "medication", "Central-A"),
        P("PLZ-TUS-016", "Tussi Champagne", "Estimulantes", 154000, 35, 80, "medication", "Central-A"),
        P("PLZ-HNG-033", "Hongos Psilocybe Cubensis", "Alucinógenos", 112000, 45, 60, "spa", "Montaña Alta"),
        P("PLZ-HNG-034", "Trufas Mágicas Holandesas", "Alucinógenos", 168000, 18, 40, "spa", "Montaña Alta"),
        P("PLZ-MDM-088", "MDMA Cristal Europa", "Estimulantes", 168000, 156, 200, "diamond", "Central-A"),
        P("PLZ-MDM-089", "MDMA Pastillas Red Bull", "Estimulantes", 72000, 89, 150, "medication", "Central-A"),
        P("PLZ-KET-021", "Ketamina Líquida 50ml", "Disociativos", 356000, 3, 40, "vaccines", "Búnker Psicodélico"),
        P("PLZ-COC-099", "Cocaína Perlada — Polvo", "Estimulantes", 480000, 18, 50, "grain", "Central-A"),
        P("PLZ-COC-100", "Crack El Plonsazo (Ficción)", "Estimulantes", 340000, 5, 25, "grain", "Central-A"),
        P("PLZ-EXT-056", "Éxtasis Tesla 300mg", "Estimulantes", 60000, 0, 120, "medication", "Central-A", ProductStatus.OutOfStock),
        P("PLZ-DMT-012", "DMT Cristalizado", "Alucinógenos", 380000, 1, 25, "biotech", "Búnker Psicodélico"),
    ];

    private static Product P(
        string code, string name, string category, decimal price, int stock, int maxStock,
        string icon, string warehouse, ProductStatus status = ProductStatus.Active) =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Category = category,
            Price = price,
            Stock = stock,
            MaxStock = maxStock,
            Status = status,
            Icon = icon,
            Warehouse = warehouse,
            Description = $"Producto del catálogo El Plonsazo — {name}.",
        };
}
