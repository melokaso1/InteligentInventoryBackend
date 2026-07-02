using System.Globalization;
using Application.Abstractions;
using Application.Common;
using Application.Models;
using Domain.Enums;
using Domain.Extensions;

namespace Application.Services;

public sealed class DashboardService(
    IProductRepository productRepository,
    ISaleRepository saleRepository,
    IInventoryMovementRepository movementRepository,
    IInvoiceRepository invoiceRepository) : IDashboardService
{
    public async Task<List<DashboardKpiModel>> GetKpisAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var culture = CultureInfo.GetCultureInfo("es-CO");

        var productCount = await productRepository.CountAsync(cancellationToken);
        var todaySales = await saleRepository.SumTotalByDateRangeAsync(today, tomorrow, cancellationToken);
        var allProducts = await productRepository.GetAllAsync(cancellationToken);
        var lowStockCount = allProducts.Count(
            p => StockLevelHelper.GetStockLevel(p.GetStock(), p.GetMaxStock()).StockLevel is "critical" or "low");
        var chatbotSalesCount = await saleRepository.CountByOriginAsync(SaleOrigin.Chatbot, cancellationToken);

        return
        [
            new DashboardKpiModel
            {
                Id = "products",
                Label = "Productos",
                Value = productCount.ToString(culture),
                Change = "Catálogo actual",
                ChangeType = "neutral",
                Icon = "inventory_2",
                IconBg = "bg-blue-100",
                IconColor = "text-blue-700",
            },
            new DashboardKpiModel
            {
                Id = "sales_today",
                Label = "Ventas de hoy",
                Value = todaySales.ToString("C0", culture),
                Change = "Actualizado en tiempo real",
                ChangeType = "positive",
                Icon = "payments",
                IconBg = "bg-emerald-100",
                IconColor = "text-emerald-700",
            },
            new DashboardKpiModel
            {
                Id = "low_stock",
                Label = "Stock bajo",
                Value = lowStockCount.ToString(culture),
                Change = "Requiere reposición",
                ChangeType = "warning",
                Icon = "warning",
                IconBg = "bg-amber-100",
                IconColor = "text-amber-700",
            },
            new DashboardKpiModel
            {
                Id = "chatbot_sales",
                Label = "Ventas chatbot",
                Value = chatbotSalesCount.ToString(culture),
                Change = "Pedidos automatizados",
                ChangeType = "positive",
                Icon = "smart_toy",
                IconBg = "bg-purple-100",
                IconColor = "text-purple-700",
            },
        ];
    }

    public async Task<List<LowStockItemModel>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        var products = (await productRepository.GetAllAsync(cancellationToken))
            .Where(p => StockLevelHelper.GetStockLevel(p.GetStock(), p.GetMaxStock()).StockLevel is "critical" or "low")
            .OrderBy(p => p.GetStock())
            .ToList();

        return products
            .Select(
                p => new LowStockItemModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Code,
                    CurrentStock = p.GetStock(),
                    ReorderLevel = p.GetMaxStock(),
                    Status = StockLevelHelper.GetLowStockStatus(p.GetStock(), p.GetMaxStock()),
                })
            .ToList();
    }

    public async Task<List<ActivityItemModel>> GetActivityAsync(int limit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var sales = await saleRepository.GetRecentAsync(limit, cancellationToken);
        var movements = await movementRepository.GetRecentWithProductAsync(limit, cancellationToken);
        var invoices = await invoiceRepository.GetRecentAsync(limit, cancellationToken);

        var events = new List<(DateTime Date, ActivityItemModel Item)>();
        events.AddRange(sales.Select(s => (s.CreatedAt, ToActivityItem(s))));
        events.AddRange(movements.Select(m => (m.CreatedAt, ToActivityItem(m))));
        events.AddRange(invoices.Select(i => (i.IssueDate, ToActivityItem(i))));

        return events
            .OrderByDescending(e => e.Date)
            .Take(limit)
            .Select(e => e.Item)
            .ToList();
    }

    private static ActivityItemModel ToActivityItem(Domain.Entities.Sale sale) =>
        new()
        {
            Id = sale.Id.ToString(),
            Title = $"Venta {sale.OrderNumber}",
            Description = $"{sale.CustomerName} - {ToFrontendSaleOrigin(sale.Origin)}",
            Time = sale.CreatedAt.ToString("O"),
            DotBg = "bg-blue-100",
            DotBorder = "border-blue-300",
        };

    private static ActivityItemModel ToActivityItem(Domain.Entities.InventoryMovement movement) =>
        new()
        {
            Id = movement.Id.ToString(),
            Title = $"Movimiento de stock {movement.Product?.Code}",
            Description = movement.Detail,
            Time = movement.CreatedAt.ToString("O"),
            DotBg = "bg-amber-100",
            DotBorder = "border-amber-300",
        };

    private static ActivityItemModel ToActivityItem(Domain.Entities.Invoice invoice) =>
        new()
        {
            Id = invoice.Id.ToString(),
            Title = $"Factura {invoice.InvoiceNumber}",
            Description = $"{invoice.ClientName} - {ToFrontendInvoiceStatus(invoice.Status)}",
            Time = invoice.IssueDate.ToString("O"),
            DotBg = "bg-emerald-100",
            DotBorder = "border-emerald-300",
        };

    private static string ToFrontendSaleOrigin(SaleOrigin origin) => origin switch
    {
        SaleOrigin.Manual => "manual",
        SaleOrigin.Chatbot => "chatbot",
        _ => "manual",
    };

    private static string ToFrontendInvoiceStatus(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Paid => "paid",
        InvoiceStatus.Pending => "pending",
        InvoiceStatus.Overdue => "overdue",
        InvoiceStatus.Draft => "draft",
        _ => "draft",
    };
}
