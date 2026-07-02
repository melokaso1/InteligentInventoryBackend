using Application.Abstractions;
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
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await context.Database.MigrateAsync(cancellationToken);

        await SeedRolesAsync(context, logger, cancellationToken);
        await SeedLookupsAsync(context, logger, cancellationToken);
        await SeedWarehousesAsync(context, logger, cancellationToken);
        await SeedAdminUserAsync(context, passwordHasher, logger, cancellationToken);

        if (await context.Products.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Base de datos ya contiene datos; se omite el seed de catálogo.");
            return;
        }

        logger.LogInformation("Sembrando catálogo inicial de SmartInventory AI...");

        await SeedCategoriesAsync(context, cancellationToken);
        var categoryByName = await context.Categories.ToDictionaryAsync(c => c.Name, cancellationToken);
        var warehouseByName = await context.Warehouses.ToDictionaryAsync(w => w.Name, cancellationToken);

        var customers = CustomerSeedData.Create();
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync(cancellationToken);
        var customerByEmail = customers.ToDictionary(c => c.Email);

        var products = ProductSeedData.Create(categoryByName, warehouseByName);
        context.Products.AddRange(products);
        await context.SaveChangesAsync(cancellationToken);

        var productByCode = products.ToDictionary(p => p.Code);

        context.Sales.AddRange(SaleSeedData.Create(productByCode, customerByEmail));
        await context.SaveChangesAsync(cancellationToken);

        context.Invoices.AddRange(InvoiceSeedData.Create(productByCode, customerByEmail));
        await context.SaveChangesAsync(cancellationToken);

        var inventoryByProduct = await context.Inventories
            .ToDictionaryAsync(i => i.ProductId, cancellationToken);

        context.InventoryMovements.AddRange(
        [
            Movement(productByCode["PLZ-LAP-001"], inventoryByProduct, StockMovementType.Outbound, -5, "Venta chatbot", "Pedido ORD-94210"),
            Movement(productByCode["PLZ-HDD-002"], inventoryByProduct, StockMovementType.Inbound, 20, "Reposición", "Lote Almacén Norte"),
            Movement(productByCode["PLZ-UPS-001"], inventoryByProduct, StockMovementType.Adjustment, -1, "Ajuste inventario", "Conteo físico mensual"),
            Movement(productByCode["PLZ-KBD-001"], inventoryByProduct, StockMovementType.Outbound, -2, "Venta manual", "Pedido ORD-94211"),
            Movement(productByCode["PLZ-MON-002"], inventoryByProduct, StockMovementType.Inbound, 15, "Entrada almacén", "Recepción proveedor Samsung"),
        ]);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seed completado: {ProductCount} productos.", products.Count);
    }

    private static async Task SeedRolesAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        context.Roles.AddRange(RoleSeedData.Create());
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Roles sembrados: Admin, Cliente.");
    }

    private static async Task SeedLookupsAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.SaleStatuses.AnyAsync(cancellationToken))
        {
            return;
        }

        context.SaleStatuses.AddRange(LookupSeedData.SaleStatuses());
        context.SaleOrigins.AddRange(LookupSeedData.SaleOrigins());
        context.InvoiceStatuses.AddRange(LookupSeedData.InvoiceStatuses());
        context.MovementTypes.AddRange(LookupSeedData.MovementTypes());
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Lookups sembrados: estados de venta, origen, factura y movimiento.");
    }

    private static async Task SeedWarehousesAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Warehouses.AnyAsync(cancellationToken))
        {
            return;
        }

        context.Warehouses.AddRange(WarehouseSeedData.Create());
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Almacenes sembrados: Central Bogotá, Almacén Norte, Bodega Sur.");
    }

    private static async Task SeedCategoriesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        context.Categories.AddRange(CategorySeedData.Create());
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminUserAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Email == UserSeedData.DefaultAdminEmail, cancellationToken))
        {
            return;
        }

        context.Users.Add(UserSeedData.CreateDefaultAdmin(passwordHasher));
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Usuario administrador creado: {Email} / contraseña: {Password}",
            UserSeedData.DefaultAdminEmail,
            UserSeedData.DefaultAdminPassword);
    }

    private static InventoryMovement Movement(
        Product product,
        IReadOnlyDictionary<Guid, Inventory> inventoryByProduct,
        StockMovementType type,
        int change,
        string reason,
        string detail)
    {
        inventoryByProduct.TryGetValue(product.Id, out var inventory);
        return new InventoryMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            InventoryId = inventory?.Id,
            Type = type,
            QuantityChange = change,
            Reason = reason,
            Detail = detail,
            CreatedAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 72)),
        };
    }
}
