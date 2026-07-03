using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Removes legacy QA catalog rows (e.g. TEST-001 / Hardware) left from manual testing.
/// Safe to run on every startup; only targets known test SKUs and non-PLZ Hardware products.
/// </summary>
internal static class QaCatalogCleanup
{
    private static readonly string[] OrphanCategoryNames = ["Hardware", "Alcohol"];

    internal static async Task<int> RemoveLegacyQaDataAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var qaProducts = await context.Products
            .Include(p => p.Category)
            .Include(p => p.Movements)
            .Where(p =>
                p.Code.ToUpper() == "TEST-001" ||
                p.Code.ToUpper() == "EP-001" ||
                p.Code.ToUpper().StartsWith("TEST-") ||
                p.Code.ToUpper().Contains("COPY-TEST") ||
                p.Name.ToUpper().Contains("PRUEBA QA") ||
                p.Name.ToUpper().Contains("PRODUCTO DE PRUEBA") ||
                (p.Category.Name.ToUpper() == "HARDWARE" && !p.Code.ToUpper().StartsWith("PLZ-")) ||
                p.Code.ToUpper().StartsWith("PLZ-ALC-") ||
                p.Category.Name.ToUpper() == "ALCOHOL")
            .ToListAsync(cancellationToken);

        if (qaProducts.Count == 0)
        {
            await RemoveOrphanCategoriesAsync(context, logger, cancellationToken);
            return 0;
        }

        var qaIds = qaProducts.Select(p => p.Id).ToList();
        var codes = string.Join(", ", qaProducts.Select(p => p.Code));

        var movements = qaProducts.SelectMany(p => p.Movements).ToList();
        if (movements.Count > 0)
        {
            context.InventoryMovements.RemoveRange(movements);
        }

        await context.SaleLineItems
            .Where(sli => sli.ProductId != null && qaIds.Contains(sli.ProductId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(sli => sli.ProductId, (Guid?)null), cancellationToken);

        context.Products.RemoveRange(qaProducts);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Limpieza QA: eliminados {Count} producto(s) de prueba ({Codes}).",
            qaProducts.Count,
            codes);

        await RemoveOrphanCategoriesAsync(context, logger, cancellationToken);
        return qaProducts.Count;
    }

    private static async Task RemoveOrphanCategoriesAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var name in OrphanCategoryNames)
        {
            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToUpper() == name.ToUpper(), cancellationToken);

            if (category is null)
            {
                continue;
            }

            var hasProducts = await context.Products.AnyAsync(p => p.CategoryId == category.Id, cancellationToken);
            if (hasProducts)
            {
                continue;
            }

            context.Categories.Remove(category);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Limpieza QA: eliminada categoría huérfana «{Category}».", name);
        }
    }
}
