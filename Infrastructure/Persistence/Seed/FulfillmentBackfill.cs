using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

internal static class FulfillmentBackfill
{
    /// <summary>
    /// Ensures paid sales that were never shipped appear in dispatch (Preparando).
    /// Idempotent: leaves shipped/delivered sales unchanged.
    /// </summary>
    public static async Task BackfillPaidSalesAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var updated = await context.Sales
            .Where(s =>
                s.Status != SaleStatus.Cancelled
                && s.Invoice != null
                && s.Invoice.Status == InvoiceStatus.Paid
                && s.FulfillmentStatus != FulfillmentStatus.Shipped
                && s.FulfillmentStatus != FulfillmentStatus.Delivered)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.FulfillmentStatus, FulfillmentStatus.Preparing),
                cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation(
                "Backfill despacho: {Count} venta(s) pagada(s) actualizada(s) a estado Preparando.",
                updated);
        }
    }
}
