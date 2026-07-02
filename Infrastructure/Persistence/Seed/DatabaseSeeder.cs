using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Products.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Base de datos ya contiene datos; se omite el seed.");
            return;
        }

        logger.LogInformation("Sembrando datos iniciales de El Plonsazo...");

        var products = ProductSeedData.Create();
        context.Products.AddRange(products);
        await context.SaveChangesAsync(cancellationToken);

        var productByCode = products.ToDictionary(p => p.Code);

        context.Sales.AddRange(SaleSeedData.Create(productByCode));
        await context.SaveChangesAsync(cancellationToken);

        context.Invoices.AddRange(InvoiceSeedData.Create(productByCode));
        await context.SaveChangesAsync(cancellationToken);

        var movements = new[]
        {
            Movement(products[0], StockMovementType.Outbound, -12, "Venta manual", "Pedido ORD-94210"),
            Movement(products[7], StockMovementType.Inbound, 50, "Reposición", "Lote Búnker Psicodélico"),
            Movement(products[19], StockMovementType.Adjustment, -2, "Ajuste inventario", "Conteo físico mensual"),
            Movement(products[13], StockMovementType.Outbound, -5, "Venta chatbot", "Pedido chatbot #9021"),
            Movement(products[4], StockMovementType.Inbound, 30, "Entrada almacén", "Central-A recepción"),
        };
        context.InventoryMovements.AddRange(movements);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seed completado: {ProductCount} productos.", products.Count);
    }

    private static InventoryMovement Movement(Product p, StockMovementType type, int change, string reason, string detail) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = p.Id,
        Type = type,
        QuantityChange = change,
        Reason = reason,
        Detail = detail,
        CreatedAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 72)),
    };
}
