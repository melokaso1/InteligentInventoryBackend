using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Removes legacy QA catalog rows (e.g. TEST-001 / Hardware / retired PLZ-ALC-* alcohol SKUs).
/// Safe to run on every startup; uses bulk deletes so FK restrictions cannot block removal.
/// </summary>
internal static class QaCatalogCleanup
{
    private static readonly string[] OrphanCategoryNames = ["Hardware", "Alcohol"];

    /// <summary>
    /// Removes the legacy demo cliente user (cliente@elplonsazo.com), its customer row,
    /// and any sales, invoices, or chat sessions tied to that account.
    /// </summary>
    internal static async Task RemoveLegacyDemoClienteDataAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var legacyEmail = UserSeedData.LegacyDemoClienteEmail.ToLowerInvariant();

        var demoCustomerIds = await context.Customers
            .Where(c => c.Email.ToLower() == legacyEmail)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var demoUserIds = await context.Users
            .Where(u => u.Email.ToLower() == legacyEmail)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (demoCustomerIds.Count == 0 && demoUserIds.Count == 0)
        {
            return;
        }

        if (demoCustomerIds.Count > 0)
        {
            var demoInvoiceIds = await context.Invoices
                .Where(i => i.CustomerId != null && demoCustomerIds.Contains(i.CustomerId.Value))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            if (demoInvoiceIds.Count > 0)
            {
                await context.InvoiceLineItems
                    .Where(ili => demoInvoiceIds.Contains(ili.InvoiceId))
                    .ExecuteDeleteAsync(cancellationToken);

                await context.Invoices
                    .Where(i => demoInvoiceIds.Contains(i.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var demoSaleIds = await context.Sales
                .Where(s => s.CustomerId != null && demoCustomerIds.Contains(s.CustomerId.Value))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            if (demoSaleIds.Count > 0)
            {
                await context.SaleLineItems
                    .Where(sli => demoSaleIds.Contains(sli.SaleId))
                    .ExecuteDeleteAsync(cancellationToken);

                await context.Sales
                    .Where(s => demoSaleIds.Contains(s.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await context.ChatSessions
                .Where(cs =>
                    (cs.CustomerId != null && demoCustomerIds.Contains(cs.CustomerId.Value)) ||
                    (cs.UserId != null && demoUserIds.Contains(cs.UserId.Value)))
                .ExecuteDeleteAsync(cancellationToken);
        }
        else if (demoUserIds.Count > 0)
        {
            await context.ChatSessions
                .Where(cs => cs.UserId != null && demoUserIds.Contains(cs.UserId.Value))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var usersDeleted = await context.Users
            .Where(u => u.Email.ToLower() == legacyEmail)
            .ExecuteDeleteAsync(cancellationToken);

        var customersDeleted = demoCustomerIds.Count > 0
            ? await context.Customers
                .Where(c => demoCustomerIds.Contains(c.Id))
                .ExecuteDeleteAsync(cancellationToken)
            : 0;

        logger.LogInformation(
            "Limpieza demo cliente: eliminados {UsersDeleted} usuario(s), {CustomersDeleted} cliente(s), " +
            "ventas/facturas/sesiones asociadas al correo {Email}.",
            usersDeleted,
            customersDeleted,
            UserSeedData.LegacyDemoClienteEmail);
    }

    internal static async Task<int> RemoveLegacyQaDataAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var alcoholCategoryIds = await context.Categories
            .Where(c => c.Name.ToUpper() == "ALCOHOL")
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var qaProductIds = await context.Products
            .Where(p =>
                p.Code.ToUpper() == "TEST-001" ||
                p.Code.ToUpper() == "EP-001" ||
                p.Code.ToUpper().StartsWith("TEST-") ||
                p.Code.ToUpper().Contains("COPY-TEST") ||
                p.Name.ToUpper().Contains("PRUEBA QA") ||
                p.Name.ToUpper().Contains("PRODUCTO DE PRUEBA") ||
                (p.Category.Name.ToUpper() == "HARDWARE" && !p.Code.ToUpper().StartsWith("PLZ-")) ||
                p.Code.ToUpper().StartsWith("PLZ-ALC-") ||
                alcoholCategoryIds.Contains(p.CategoryId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (qaProductIds.Count == 0)
        {
            await RemoveOrphanCategoriesAsync(context, logger, cancellationToken);
            return 0;
        }

        var codes = await context.Products
            .Where(p => qaProductIds.Contains(p.Id))
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);

        var deleted = await DeleteProductsByIdsAsync(context, qaProductIds, cancellationToken);

        logger.LogInformation(
            "Limpieza QA: eliminados {Count} producto(s) de prueba/retirados ({Codes}).",
            deleted,
            string.Join(", ", codes));

        await RemoveOrphanCategoriesAsync(context, logger, cancellationToken);
        return deleted;
    }

    /// <summary>
    /// Deletes PLZ-* products that are no longer present in <see cref="ProductSeedData"/>.
    /// Catches retired SKUs (e.g. PLZ-ALC-*) even if they slip past legacy filters.
    /// </summary>
    internal static async Task<int> RemoveRetiredSeedProductsAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!await context.Warehouses.AnyAsync(cancellationToken))
        {
            return 0;
        }

        var validCodes = ProductSeedData.ValidSeedProductCodes();

        var retiredIds = await context.Products
            .Where(p => p.Code.ToUpper().StartsWith("PLZ-") && !validCodes.Contains(p.Code.ToUpper()))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (retiredIds.Count == 0)
        {
            return 0;
        }

        var codes = await context.Products
            .Where(p => retiredIds.Contains(p.Id))
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);

        var deleted = await DeleteProductsByIdsAsync(context, retiredIds, cancellationToken);

        logger.LogInformation(
            "Limpieza catálogo: eliminados {Count} producto(s) PLZ-* retirados del seed ({Codes}).",
            deleted,
            string.Join(", ", codes));

        await RemoveOrphanCategoriesAsync(context, logger, cancellationToken);
        return deleted;
    }

    private static async Task<int> DeleteProductsByIdsAsync(
        AppDbContext context,
        List<Guid> productIds,
        CancellationToken cancellationToken)
    {
        await context.InventoryMovements
            .Where(m => productIds.Contains(m.ProductId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.SaleLineItems
            .Where(sli => sli.ProductId != null && productIds.Contains(sli.ProductId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(sli => sli.ProductId, (Guid?)null), cancellationToken);

        await context.InvoiceLineItems
            .Where(ili => ili.ProductId != null && productIds.Contains(ili.ProductId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(ili => ili.ProductId, (Guid?)null), cancellationToken);

        await context.Inventories
            .Where(i => productIds.Contains(i.ProductId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.ProductEmbeddings
            .Where(pe => productIds.Contains(pe.ProductId))
            .ExecuteDeleteAsync(cancellationToken);

        return await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);
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
