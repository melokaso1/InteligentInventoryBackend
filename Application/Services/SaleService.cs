using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class SaleService(
    IProductRepository productRepository,
    ISaleRepository saleRepository,
    IInvoiceRepository invoiceRepository,
    IInventoryMovementRepository movementRepository,
    IUnitOfWork unitOfWork) : ISaleService
{
    private const decimal TaxRate = 0.08m;

    public async Task<PagedResult<Sale>> GetSalesAsync(SalesQueryModel query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var origin = ParseSaleOrigin(query.Origin);
        var status = ParseSaleStatus(query.Status);

        return await saleRepository.GetPagedAsync(
            query.From,
            query.To,
            origin,
            status,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<SaleMetricsModel> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        return new SaleMetricsModel
        {
            TotalSales = await saleRepository.CountAsync(cancellationToken),
            TotalRevenue = await saleRepository.SumTotalAsync(cancellationToken),
            ChatbotSales = await saleRepository.CountByOriginAsync(SaleOrigin.Chatbot, cancellationToken),
            ManualSales = await saleRepository.CountByOriginAsync(SaleOrigin.Manual, cancellationToken),
            PendingSales = await saleRepository.CountByStatusAsync(SaleStatus.Pending, cancellationToken),
            InvoicedSales = await saleRepository.CountByStatusAsync(SaleStatus.Invoiced, cancellationToken),
        };
    }

    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        saleRepository.GetByIdWithDetailsAsync(id, cancellationToken);

    public async Task<Sale> CreateManualSaleAsync(CreateSaleModel request, CancellationToken cancellationToken = default)
    {
        if (request.LineItems.Count == 0)
        {
            throw new InvalidOperationException("Debe enviar al menos un producto para crear la venta.");
        }

        var origin = ParseSaleOrigin(request.Origin)
            ?? throw new InvalidOperationException("Origen inválido. Valores permitidos: manual, chatbot.");
        var status = ParseSaleStatus(request.Status)
            ?? throw new InvalidOperationException("Estado inválido. Valores permitidos: invoiced, pending, confirmed, cancelled.");

        var productIds = request.LineItems.Select(li => li.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOperationException("Uno o más productos no existen.");
        }

        foreach (var line in request.LineItems)
        {
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("La cantidad de cada línea debe ser mayor a cero.");
            }

            var product = products[line.ProductId];
            if (product.Stock < line.Quantity)
            {
                throw new InvalidOperationException($"Stock insuficiente para el producto {product.Code}.");
            }
        }

        Sale? created = null;
        await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
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

                var movements = new List<InventoryMovement>();
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

                    movements.Add(
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

                movementRepository.AddRange(movements);
                saleRepository.Add(sale);
                await unitOfWork.SaveChangesAsync(ct);
                created = sale;
            },
            cancellationToken);

        return created!;
    }

    public async Task<Invoice> CreateInvoiceAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var sale = await saleRepository.GetByIdTrackedWithDetailsAsync(saleId, cancellationToken)
            ?? throw new KeyNotFoundException("Venta no encontrada.");

        if (sale.Invoice is not null)
        {
            throw new InvalidOperationException("La venta ya tiene factura asociada.");
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
        invoiceRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invoice;
    }

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

        var product = await productRepository.GetByCodeAsync(productCode.Trim(), cancellationToken);

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

        Sale? sale = null;
        Invoice? invoice = null;
        await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var now = DateTime.UtcNow;
                var subtotal = decimal.Round(product.Price * quantity, 2, MidpointRounding.AwayFromZero);
                var tax = decimal.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
                var total = subtotal + tax;
                var orderNumber = $"ORD-{now:yyyyMMddHHmmssfff}";
                var invoiceNumber = $"INV-{now:yyyyMMddHHmmssfff}";

                product.Stock -= quantity;
                product.UpdatedAt = now;
                product.Status = product.Stock <= 0 ? ProductStatus.OutOfStock : ProductStatus.Active;

                sale = new Sale
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
                            Product = product,
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
                    Product = product,
                    Type = StockMovementType.Outbound,
                    QuantityChange = -quantity,
                    Reason = "Venta chatbot",
                    Detail = $"Pedido {orderNumber}",
                    CreatedAt = now,
                };

                invoice = new Invoice
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

                movementRepository.Add(movement);
                saleRepository.Add(sale);
                invoiceRepository.Add(invoice);

                await unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        sale!.Invoice = invoice;
        return new ChatbotSaleResult { Sale = sale, InvoiceNumber = invoice!.InvoiceNumber };
    }

    private static SaleOrigin? ParseSaleOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return null;
        }

        return origin.Trim().ToLowerInvariant() switch
        {
            "manual" => SaleOrigin.Manual,
            "chatbot" => SaleOrigin.Chatbot,
            _ => throw new InvalidOperationException("Origen inválido. Valores permitidos: manual, chatbot."),
        };
    }

    private static SaleStatus? ParseSaleStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "invoiced" => SaleStatus.Invoiced,
            "pending" => SaleStatus.Pending,
            "confirmed" => SaleStatus.Confirmed,
            "cancelled" => SaleStatus.Cancelled,
            _ => throw new InvalidOperationException("Estado inválido. Valores permitidos: invoiced, pending, confirmed, cancelled."),
        };
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
