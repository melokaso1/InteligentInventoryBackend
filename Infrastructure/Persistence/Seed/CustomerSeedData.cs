using Domain.Entities;

namespace Infrastructure.Persistence.Seed;

internal static class CustomerSeedData
{
    internal static List<Customer> Create() =>
    [
        C("Carolina Méndez", "carolina.mendez@techsolutions.co"),
        C("Andrés Vargas", "andres.vargas@grupoandina.co"),
        C("Laura Herrera", "laura.herrera@logisticaexpress.co"),
        C("Miguel Torres", "miguel.torres@constructorametro.co"),
        C("TechSolutions Colombia S.A.S.", "tech@techsolutions.co"),
        C("Grupo Andina Digital Ltda.", "contacto@grupoandina.co"),
        C("Logística Express S.A.S.", "facturacion@logisticaexpress.co"),
        C("Constructora Metro S.A.", "compras@constructorametro.co"),
        C("EduTech Innovación S.A.S.", "edu@edutech.co"),
        C("Oficinas del Valle Ltda.", "oficinas@valle.co"),
    ];

    private static Customer C(string fullName, string email) => new()
    {
        Id = Guid.NewGuid(),
        FullName = fullName,
        Email = email,
        CreatedAt = DateTime.UtcNow,
    };
}
