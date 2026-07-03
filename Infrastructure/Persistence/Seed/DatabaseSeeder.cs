using Application.Abstractions;
using Domain.Constants;
using Domain.Entities;
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

        await FulfillmentBackfill.BackfillPaidSalesAsync(context, logger, cancellationToken);

        await SeedWarehousesAsync(context, logger, cancellationToken);
        await QaCatalogCleanup.RemoveLegacyDemoClienteDataAsync(context, logger, cancellationToken);
        await SeedUsersAsync(scope.ServiceProvider, context, logger, cancellationToken);
        await QaCatalogCleanup.RemoveLegacyQaDataAsync(context, logger, cancellationToken);

        var hadProducts = await context.Products.AnyAsync(cancellationToken);

        if (!hadProducts)
        {
            logger.LogInformation("Sembrando catálogo ficticio de El Plonsazo (base vacía)...");
        }

        var upsert = await CatalogSeedUpsert.UpsertMissingAsync(context, logger, cancellationToken);

        await LogCatalogSeedStatusAsync(context, logger, upsert, hadProducts, cancellationToken);
    }

    private static async Task LogCatalogSeedStatusAsync(
        AppDbContext context,
        ILogger logger,
        CatalogSeedUpsert.UpsertResult upsert,
        bool hadProducts,
        CancellationToken cancellationToken)
    {
        var expectedCategories = CategorySeedData.ExpectedCategoryCount;
        var expectedProducts = ProductSeedData.ExpectedProductCount;

        var categoryCount = await context.Categories.CountAsync(cancellationToken);
        var productCount = await context.Products
            .Where(p => p.Code.ToUpper().StartsWith("PLZ-"))
            .CountAsync(cancellationToken);

        logger.LogInformation(
            "Seed: {CategoryCount} categorías, {ProductCount} productos (esperado: {ExpectedCategories} categorías, {ExpectedProducts} productos).",
            categoryCount,
            productCount,
            expectedCategories,
            expectedProducts);

        if (categoryCount < expectedCategories || productCount < expectedProducts)
        {
            logger.LogWarning(
                "Catálogo incompleto: faltan {MissingCategories} categoría(s) y/o {MissingProducts} producto(s) PLZ-*. " +
                "Reinicia la API o ejecuta docker compose down -v && docker compose up -d para re-sembrar.",
                Math.Max(0, expectedCategories - categoryCount),
                Math.Max(0, expectedProducts - productCount));
        }

        if (!hadProducts)
        {
            logger.LogInformation(
                "Seed completado: {ProductCount} producto(s) con {Stock} unidad(es) de stock por almacén.",
                productCount,
                ProductSeedData.DefaultSeedStock);
            return;
        }

        if (upsert.CategoriesAdded > 0 || upsert.ProductsAdded > 0 || upsert.ProductsSynced > 0)
        {
            logger.LogInformation(
                "Upsert catálogo en DB existente: {CategoriesAdded} categoría(s) añadida(s), " +
                "{ProductsAdded} producto(s) añadido(s), {ProductsSynced} producto(s) sincronizado(s). " +
                "El stock previo no se modifica.",
                upsert.CategoriesAdded,
                upsert.ProductsAdded,
                upsert.ProductsSynced);
            return;
        }

        logger.LogInformation(
            "Catálogo al día. Stock refleja ventas reales (no se resetea al reiniciar). " +
            "Para re-sembrar desde cero: docker compose down -v && docker compose up -d, luego reiniciar la API.");
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

    private static async Task SeedUsersAsync(
        IServiceProvider services,
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var customerRepository = services.GetRequiredService<ICustomerRepository>();
        var now = DateTime.UtcNow;

        var adminCustomer = await customerRepository.GetOrCreateAsync(
            UserSeedData.AdminFullName,
            UserSeedData.AdminEmail,
            cancellationToken);

        context.Users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Email = UserSeedData.AdminEmail,
                FullName = UserSeedData.AdminFullName,
                PasswordHash = passwordHasher.Hash(UserSeedData.AdminPassword),
                RoleId = RoleIds.Admin,
                CustomerId = adminCustomer.Id,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Usuario administrador sembrado: {AdminEmail} ({AdminName}).",
            UserSeedData.AdminEmail,
            UserSeedData.AdminFullName);
    }
}
