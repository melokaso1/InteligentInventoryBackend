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



        await SeedWarehousesAsync(context, logger, cancellationToken);

        await QaCatalogCleanup.RemoveLegacyQaDataAsync(context, logger, cancellationToken);



        var hadProducts = await context.Products.AnyAsync(cancellationToken);



        if (!hadProducts)

        {

            logger.LogInformation("Sembrando catálogo ficticio de El Plonsazo (base vacía)...");

            await SeedCategoriesAsync(context, cancellationToken);

        }



        var upsert = await CatalogSeedUpsert.UpsertMissingAsync(

            context,

            logger,

            cancellationToken);



        if (!hadProducts)

        {

            var productCount = await context.Products.CountAsync(cancellationToken);

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



        var existingCount = await context.Products.CountAsync(cancellationToken);

        logger.LogInformation(

            "Catálogo al día: {ProductCount} producto(s). Stock refleja ventas reales (no se resetea al reiniciar). " +

            "Para re-sembrar desde cero: docker compose down -v && docker compose up -d, luego reiniciar la API.",

            existingCount);

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

}

