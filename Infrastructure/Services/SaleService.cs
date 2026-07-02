using Core.Entities;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class SaleService(AppDbContext context)
{
    private const decimal TaxRate = 0.08m;

    public async Task<ChatbotSaleResult> CreateSaleFromChatbotAsync(
        string productCode,
        int quantity,
        string customerName,
        string customerEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new InvalidOperationException("El código del producto es obligatorio.");
        }

        if (quantity <= 0)
        {
            throw new InvalidOperationException("La cantidad debe ser mayor a cero.");
        }

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Code == productCode.Trim(), cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Producto no encontrado.");
        }

        if (product.Status is ProductStatus.Inactive or ProductStatus.Archived)
        {
            throw new InvalidOperationException("El producto no está disponible para venta.");
        }

        if (product.Stock < quantity)
        {
            throw new InvalidOperationException("Stock insuficiente para completar la venta.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var subtotal = decimal.Round(product.Price * quantity, 2, MidpointRounding.AwayFromZero);
        var tax = decimal.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + tax;
        var orderNumber = $"ORD-{now:yyyyMMddHHmmssfff}";
        var invoiceNumber = $"INV-{now:yyyyMMddHHmmssfff}";

        product.Stock -= quantity;
        product.UpdatedAt = now;
        product.Status = product.Stock <= 0 ? ProductStatus.OutOfStock : ProductStatus.Active;

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail.Trim(),
            Origin = SaleOrigin.Chatbot,
            Status = SaleStatus.Invoiced,
            Subtotal = subtotal,
            Tax = tax,
            Total = total,
            CreatedAt = now,
            LineItems =
            [
                new SaleLineItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Description = $"{product.Name} ({product.Code})",
                    Quantity = quantity,
                    UnitPrice = product.Price,
                },
            ],
        };

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Type = StockMovementType.Outbound,
            QuantityChange = -quantity,
            Reason = "Venta chatbot",
            Detail = $"Pedido {orderNumber}",
            CreatedAt = now,
        };

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            SaleId = sale.Id,
            ClientName = sale.CustomerName,
            ClientInitials = BuildInitials(sale.CustomerName),
            BillingNote = $"Factura generada automáticamente para pedido {orderNumber}.",
            Status = InvoiceStatus.Pending,
            Subtotal = subtotal,
            Tax = tax,
            Total = total,
            IssueDate = now,
            DueDate = now.AddDays(30),
            LineItems =
            [
                new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = $"{product.Name} ({product.Code})",
                    Quantity = quantity,
                    UnitPrice = product.Price,
                },
            ],
        };

        context.InventoryMovements.Add(movement);
        context.Sales.Add(sale);
        context.Invoices.Add(invoice);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        sale.Invoice = invoice;

        return new ChatbotSaleResult { Sale = sale, InvoiceNumber = invoice.InvoiceNumber };
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

public sealed class ChatbotSaleResult
{
    public Sale Sale { get; set; } = new();
    public string InvoiceNumber { get; set; } = string.Empty;
}
