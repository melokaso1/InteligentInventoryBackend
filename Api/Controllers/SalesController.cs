using Api.Dtos;
using Api.Mapping;
using Core.Entities;
using Core.Enums;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(AppDbContext context, SaleService saleService) : ControllerBase
{
    private const decimal TaxRate = 0.08m;

    [HttpGet]
    public async Task<IActionResult> GetSales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? origin,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = context.Sales
            .AsNoTracking()
            .Include(s => s.LineItems)
            .ThenInclude(li => li.Product)
            .Include(s => s.Invoice)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(origin))
        {
            if (!EntityMappers.TryParseSaleOrigin(origin, out var parsedOrigin))
            {
                return BadRequest("Origen inválido. Valores permitidos: manual, chatbot.");
            }

            query = query.Where(s => s.Origin == parsedOrigin);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!EntityMappers.TryParseSaleStatus(status, out var parsedStatus))
            {
                return BadRequest("Estado inválido. Valores permitidos: invoiced, pending, confirmed, cancelled.");
            }

            query = query.Where(s => s.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var sales = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Items = sales.Select(s => s.ToSaleDto()).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<SaleMetricsDto>> GetMetrics(CancellationToken cancellationToken = default)
    {
        var sales = context.Sales.AsNoTracking();
        var dto = new SaleMetricsDto
        {
            TotalSales = await sales.CountAsync(cancellationToken),
            TotalRevenue = await sales.SumAsync(s => s.Total, cancellationToken),
            ChatbotSales = await sales.CountAsync(s => s.Origin == SaleOrigin.Chatbot, cancellationToken),
            ManualSales = await sales.CountAsync(s => s.Origin == SaleOrigin.Manual, cancellationToken),
            PendingSales = await sales.CountAsync(s => s.Status == SaleStatus.Pending, cancellationToken),
            InvoicedSales = await sales.CountAsync(s => s.Status == SaleStatus.Invoiced, cancellationToken),
        };

        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.LineItems)
            .ThenInclude(li => li.Product)
            .Include(s => s.Invoice)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sale is null)
        {
            return NotFound("Venta no encontrada.");
        }

        return Ok(sale.ToSaleDto());
    }

    [HttpPost]
    public async Task<ActionResult<SaleDto>> CreateManualSale(
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.LineItems.Count == 0)
        {
            return BadRequest("Debe enviar al menos un producto para crear la venta.");
        }

        if (!EntityMappers.TryParseSaleOrigin(request.Origin, out var origin))
        {
            return BadRequest("Origen inválido. Valores permitidos: manual, chatbot.");
        }

        if (!EntityMappers.TryParseSaleStatus(request.Status, out var status))
        {
            return BadRequest("Estado inválido. Valores permitidos: invoiced, pending, confirmed, cancelled.");
        }

        var productIds = request.LineItems.Select(li => li.ProductId).Distinct().ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return BadRequest("Uno o más productos no existen.");
        }

        foreach (var line in request.LineItems)
        {
            if (line.Quantity <= 0)
            {
                return BadRequest("La cantidad de cada línea debe ser mayor a cero.");
            }

            var product = products[line.ProductId];
            if (product.Stock < line.Quantity)
            {
                return BadRequest($"Stock insuficiente para el producto {product.Code}.");
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var orderNumber = $"ORD-{now:yyyyMMddHHmmssfff}";

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            Origin = origin,
            Status = status,
            CreatedAt = now,
        };

        foreach (var line in request.LineItems)
        {
            var product = products[line.ProductId];
            product.Stock -= line.Quantity;
            product.UpdatedAt = now;
            if (product.Stock <= 0)
            {
                product.Status = ProductStatus.OutOfStock;
            }

            sale.LineItems.Add(
                new SaleLineItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Product = product,
                    Description = $"{product.Name} ({product.Code})",
                    Quantity = line.Quantity,
                    UnitPrice = product.Price,
                });

            context.InventoryMovements.Add(
                new InventoryMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Type = StockMovementType.Outbound,
                    QuantityChange = -line.Quantity,
                    Reason = "Venta manual",
                    Detail = $"Pedido {orderNumber}",
                    CreatedAt = now,
                });
        }

        sale.Subtotal = decimal.Round(sale.LineItems.Sum(li => li.UnitPrice * li.Quantity), 2, MidpointRounding.AwayFromZero);
        sale.Tax = decimal.Round(sale.Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        sale.Total = sale.Subtotal + sale.Tax;

        context.Sales.Add(sale);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale.ToSaleDto());
    }

    [HttpPost("{id:guid}/invoice")]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await context.Sales
            .Include(s => s.LineItems)
            .ThenInclude(li => li.Product)
            .Include(s => s.Invoice)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sale is null)
        {
            return NotFound("Venta no encontrada.");
        }

        if (sale.Invoice is not null)
        {
            return BadRequest("La venta ya tiene factura asociada.");
        }

        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{now:yyyyMMddHHmmssfff}",
            SaleId = sale.Id,
            ClientName = sale.CustomerName,
            ClientInitials = BuildInitials(sale.CustomerName),
            BillingNote = $"Factura emitida para el pedido {sale.OrderNumber}.",
            Status = InvoiceStatus.Pending,
            Subtotal = sale.Subtotal,
            Tax = sale.Tax,
            Total = sale.Total,
            IssueDate = now,
            DueDate = now.AddDays(30),
            LineItems = sale.LineItems
                .Select(li => new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                })
                .ToList(),
        };

        sale.Status = SaleStatus.Invoiced;
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);

        return Ok(invoice.ToInvoiceDto());
    }

    [HttpPost("from-chatbot")]
    public async Task<ActionResult<CreateSaleFromChatbotResponse>> CreateSaleFromChatbot(
        [FromBody] CreateSaleFromChatbotRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await saleService.CreateSaleFromChatbotAsync(
                request.ProductCode,
                request.Quantity,
                request.CustomerName,
                request.CustomerEmail,
                cancellationToken);

            return Ok(
                new CreateSaleFromChatbotResponse
                {
                    SaleId = result.Sale.Id,
                    OrderNumber = result.Sale.OrderNumber,
                    InvoiceNumber = result.InvoiceNumber,
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static string BuildInitials(string input)
    {
        var initials = input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]))
            .ToArray();

        return initials.Length == 0 ? "NN" : new string(initials);
    }
}
