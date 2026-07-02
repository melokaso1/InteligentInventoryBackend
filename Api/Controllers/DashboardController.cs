using System.Globalization;
using Api.Dtos;
using Api.Helpers;
using Api.Mapping;
using Core.Entities;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(AppDbContext context) : ControllerBase
{
    [HttpGet("kpis")]
    public async Task<ActionResult<List<DashboardKpiDto>>> GetKpis(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var culture = CultureInfo.GetCultureInfo("es-CO");

        var productCount = await context.Products.AsNoTracking().CountAsync(cancellationToken);
        var todaySales = await context.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= today && s.CreatedAt < tomorrow)
            .SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;
        var allProducts = await context.Products.AsNoTracking().ToListAsync(cancellationToken);
        var lowStockCount = allProducts.Count(
            p => StockLevelHelper.GetStockLevel(p.Stock, p.MaxStock).StockLevel is "critical" or "low");
        var chatbotSalesCount = await context.Sales
            .AsNoTracking()
            .CountAsync(s => s.Origin == SaleOrigin.Chatbot, cancellationToken);

        var data = new List<DashboardKpiDto>
        {
            new()
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
            new()
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
            new()
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
            new()
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
        };

        return Ok(data);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<LowStockItemDto>>> GetLowStock(CancellationToken cancellationToken = default)
    {
        var products = (await context.Products.AsNoTracking().ToListAsync(cancellationToken))
            .Where(p => StockLevelHelper.GetStockLevel(p.Stock, p.MaxStock).StockLevel is "critical" or "low")
            .OrderBy(p => p.Stock)
            .ToList();

        var items = products
            .Select(
                p => new LowStockItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Code,
                    CurrentStock = p.Stock,
                    ReorderLevel = p.MaxStock,
                    Status = StockLevelHelper.GetLowStockStatus(p.Stock, p.MaxStock),
                })
            .ToList();

        return Ok(items);
    }

    [HttpGet("activity")]
    public async Task<ActionResult<List<ActivityItemDto>>> GetActivity(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var sales = await context.Sales
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var movements = await context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var invoices = await context.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.IssueDate)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var events = new List<(DateTime Date, ActivityItemDto Item)>();
        events.AddRange(sales.Select(s => (s.CreatedAt, s.ToActivityItemDto())));
        events.AddRange(movements.Select(m => (m.CreatedAt, m.ToActivityItemDto())));
        events.AddRange(invoices.Select(i => (i.IssueDate, i.ToActivityItemDto())));

        var result = events
            .OrderByDescending(e => e.Date)
            .Take(limit)
            .Select(e => e.Item)
            .ToList();

        return Ok(result);
    }
}
