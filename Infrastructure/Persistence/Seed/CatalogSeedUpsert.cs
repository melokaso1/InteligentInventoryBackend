using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Adds missing seed categories and PLZ-* products without wiping existing catalog or stock.
/// Syncs name/description/category/price/sale-unit metadata for existing PLZ-* SKUs on every startup.
/// Safe to run on every startup after migrations.
/// </summary>
internal static class CatalogSeedUpsert
{
    internal sealed record UpsertResult(int CategoriesAdded, int ProductsAdded, int ProductsSynced);

    internal static async Task<UpsertResult> UpsertMissingAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // Categories must exist before any ProductSeedData.GetAll() that reads from the DB.
        var categoriesAdded = await UpsertCategoriesAsync(context, cancellationToken);
        await QaCatalogCleanup.RemoveRetiredSeedProductsAsync(context, logger, cancellationToken);

        var productsAdded = await UpsertProductsAsync(context, logger, cancellationToken);
        var productsSynced = await SyncExistingProductsAsync(context, logger, cancellationToken);
        return new UpsertResult(categoriesAdded, productsAdded, productsSynced);
    }

    private static async Task<int> UpsertCategoriesAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        var existingNames = await context.Categories
            .Select(c => c.Name.ToUpper())
            .ToListAsync(cancellationToken);

        var missing = CategorySeedData.Create()
            .Where(c => !existingNames.Contains(c.Name.ToUpper()))
            .ToList();

        if (missing.Count == 0)
        {
            return 0;
        }

        context.Categories.AddRange(missing);
        await context.SaveChangesAsync(cancellationToken);
        return missing.Count;
    }

    private static async Task<int> UpsertProductsAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var categoryByName = await context.Categories.ToDictionaryAsync(c => c.Name, cancellationToken);
        var warehouseByName = await context.Warehouses.ToDictionaryAsync(w => w.Name, cancellationToken);

        if (warehouseByName.Count == 0)
        {
            logger.LogWarning("Upsert de catálogo omitido: no hay almacenes sembrados.");
            return 0;
        }

        var seedProducts = ProductSeedData.GetAll(categoryByName, warehouseByName);
        var existingCodes = await context.Products
            .Select(p => p.Code.ToUpper())
            .ToListAsync(cancellationToken);

        var missing = seedProducts
            .Where(p => !existingCodes.Contains(p.Code.ToUpper()))
            .ToList();

        if (missing.Count == 0)
        {
            return 0;
        }

        context.Products.AddRange(missing);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Upsert catálogo: añadidos {Count} producto(s) PLZ-* faltante(s) ({Codes}).",
            missing.Count,
            string.Join(", ", missing.Select(p => p.Code)));

        return missing.Count;
    }

    private static async Task<int> SyncExistingProductsAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var categoryByName = await context.Categories.ToDictionaryAsync(c => c.Name, cancellationToken);
        var warehouseByName = await context.Warehouses.ToDictionaryAsync(w => w.Name, cancellationToken);

        if (warehouseByName.Count == 0)
        {
            return 0;
        }

        var seedByCode = ProductSeedData.GetAll(categoryByName, warehouseByName)
            .ToDictionary(p => p.Code.ToUpper(), p => p);

        var existing = await context.Products
            .Where(p => p.Code.ToUpper().StartsWith("PLZ-"))
            .ToListAsync(cancellationToken);

        var synced = 0;
        var syncedCodes = new List<string>();

        foreach (var product in existing)
        {
            if (!seedByCode.TryGetValue(product.Code.ToUpper(), out var seed))
            {
                continue;
            }

            if (!categoryByName.TryGetValue(seed.Category.Name, out var category))
            {
                logger.LogWarning(
                    "Sync catálogo omitido para {Code}: categoría «{Category}» no encontrada.",
                    product.Code,
                    seed.Category.Name);
                continue;
            }

            var changed = false;

            if (product.Name != seed.Name)
            {
                product.Name = seed.Name;
                changed = true;
            }

            if (product.Description != seed.Description)
            {
                product.Description = seed.Description;
                changed = true;
            }

            if (product.CategoryId != category.Id)
            {
                product.CategoryId = category.Id;
                changed = true;
            }

            if (product.Price != seed.Price)
            {
                product.Price = seed.Price;
                changed = true;
            }

            if (product.Icon != seed.Icon)
            {
                product.Icon = seed.Icon;
                changed = true;
            }

            if (product.SaleUnit != seed.SaleUnit)
            {
                product.SaleUnit = seed.SaleUnit;
                changed = true;
            }

            if (product.UnitContentAmount != seed.UnitContentAmount)
            {
                product.UnitContentAmount = seed.UnitContentAmount;
                changed = true;
            }

            if (product.UnitContentMeasure != seed.UnitContentMeasure)
            {
                product.UnitContentMeasure = seed.UnitContentMeasure;
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            product.UpdatedAt = DateTime.UtcNow;
            synced++;
            syncedCodes.Add(product.Code);
        }

        if (synced == 0)
        {
            return 0;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Sync catálogo: actualizados {Count} producto(s) PLZ-* ({Codes}). Stock no modificado.",
            synced,
            string.Join(", ", syncedCodes));

        return synced;
    }
}
