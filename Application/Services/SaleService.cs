using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;

namespace Application.Services;

public sealed class SaleService(
    IProductRepository productRepository,
    ICustomerRepository customerRepository,
    IChatSessionRepository chatSessionRepository,
    ISaleRepository saleRepository,
    IInvoiceRepository invoiceRepository,
    IInventoryMovementRepository movementRepository,
    IInventoryStockService inventoryStockService,
    IUnitOfWork unitOfWork) : ISaleService
{
    private const decimal TaxRate = 0.08m;
    private const int MaxPageSize = 200;
    private const int MaxExportPageSize = 500;

    public async Task<PagedResult<Sale>> GetSalesAsync(SalesQueryModel query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var requestedPageSize = Math.Clamp(query.PageSize, 1, MaxExportPageSize);
        var origin = ParseSaleOrigin(query.Origin);
        var status = ParseSaleStatus(query.Status);

        if (requestedPageSize <= MaxPageSize)
        {
            return await saleRepository.GetPagedAsync(
                query.From,
                query.To,
                origin,
                status,
                page,
                requestedPageSize,
                cancellationToken);
        }

        var allItems = new List<Sale>();
        var currentPage = 1;
        var totalCount = 0;

        while (allItems.Count < requestedPageSize)
        {
            var batch = await saleRepository.GetPagedAsync(
                query.From,
                query.To,
                origin,
                status,
                currentPage,
                MaxPageSize,
                cancellationToken);

            totalCount = batch.TotalCount;
            if (batch.Items.Count == 0)
            {
                break;
            }

            allItems.AddRange(batch.Items);
            if (allItems.Count >= totalCount)
            {
                break;
            }

            currentPage++;
        }

        if (allItems.Count > requestedPageSize)
        {
            allItems = allItems.Take(requestedPageSize).ToList();
        }

        return new PagedResult<Sale>
        {
            Items = allItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = allItems.Count,
        };
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

        var deductionLines = request.LineItems
            .Select(line => new StockDeductionLine
            {
                Product = products[line.ProductId],
                Quantity = line.Quantity,
                MeasureUnit = line.MeasureUnit,
            })
            .ToList();
        inventoryStockService.ValidateSufficientStock(deductionLines);

        var customer = await customerRepository.GetOrCreateAsync(
            request.CustomerName,
            request.CustomerEmail,
            cancellationToken);

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
                    CustomerId = customer.Id,
                    Customer = customer,
                    CustomerName = customer.FullName,
                    CustomerEmail = customer.Email,
                    Origin = origin,
                    Status = status,
                    CreatedAt = now,
                };

                var movements = await inventoryStockService.DeductStockAsync(
                    deductionLines,
                    "Venta manual",
                    $"Pedido {orderNumber}",
                    now,
                    ct);

                foreach (var line in request.LineItems)
                {
                    var product = products[line.ProductId];
                    var saleQuantity = SaleMeasureUnitExtensions.ResolveSaleQuantity(
                        product,
                        line.Quantity,
                        line.MeasureUnit,
                        out var measureUnit);

                    sale.LineItems.Add(
                        new SaleLineItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Product = product,
                            Description = $"{product.Name} ({product.Code})",
                            Quantity = saleQuantity,
                            MeasureUnit = measureUnit,
                            UnitPrice = product.Price,
                        });
                }

                sale.Subtotal = decimal.Round(sale.LineItems.Sum(li => li.UnitPrice * li.Quantity), 2, MidpointRounding.AwayFromZero);
                sale.Tax = decimal.Round(sale.Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
                sale.Total = sale.Subtotal + sale.Tax;

                movementRepository.AddRange(movements.ToList());
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
            CustomerId = sale.CustomerId,
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
                    ProductId = li.ProductId,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    MeasureUnit = li.MeasureUnit,
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
        IReadOnlyList<ChatbotSaleLineItemModel> lineItems,
        string customerName,
        string customerEmail,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (lineItems.Count == 0)
        {
            throw new InvalidOperationException("Debe enviar al menos un producto para crear la venta.");
        }

        var resolvedLines = new List<(Product Product, decimal Quantity, SaleMeasureUnit MeasureUnit)>();
        var deductionLines = new List<StockDeductionLine>();
        foreach (var line in lineItems)
        {
            if (string.IsNullOrWhiteSpace(line.ProductCode))
            {
                throw new InvalidOperationException("El código del producto es obligatorio.");
            }

            var product = await productRepository.GetByCodeAsync(line.ProductCode.Trim(), cancellationToken);
            if (product is null)
            {
                throw new InvalidOperationException("Producto no encontrado.");
            }

            if (product.Status is ProductStatus.Inactive or ProductStatus.Archived)
            {
                throw new InvalidOperationException($"El producto {product.Code} no está disponible para venta.");
            }

            var saleQuantity = SaleMeasureUnitExtensions.ResolveSaleQuantity(
                product,
                line.Quantity,
                line.MeasureUnit,
                out var resolvedUnit);

            deductionLines.Add(
                new StockDeductionLine
                {
                    Product = product,
                    Quantity = line.Quantity,
                    MeasureUnit = line.MeasureUnit,
                });
            resolvedLines.Add((product, saleQuantity, resolvedUnit));
        }

        inventoryStockService.ValidateSufficientStock(deductionLines);

        var customer = await customerRepository.GetOrCreateAsync(customerName, customerEmail, cancellationToken);
        ChatSession? chatSession = null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            chatSession = await chatSessionRepository.GetByTokenTrackedAsync(sessionId.Trim(), cancellationToken);
        }

        Sale? sale = null;
        Invoice? invoice = null;
        await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var now = DateTime.UtcNow;
                var orderNumber = $"ORD-{now:yyyyMMddHHmmssfff}";
                var invoiceNumber = $"INV-{now:yyyyMMddHHmmssfff}";

                sale = new Sale
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    CustomerId = customer.Id,
                    Customer = customer,
                    CustomerName = customer.FullName,
                    CustomerEmail = customer.Email,
                    Origin = SaleOrigin.Chatbot,
                    Status = SaleStatus.Invoiced,
                    ChatSessionId = chatSession?.Id,
                    ChatSession = chatSession,
                    CreatedAt = now,
                };

                var movements = await inventoryStockService.DeductStockAsync(
                    deductionLines,
                    "Venta chatbot",
                    $"Pedido {orderNumber}",
                    now,
                    ct);

                foreach (var (product, saleQuantity, resolvedUnit) in resolvedLines)
                {
                    sale.LineItems.Add(
                        new SaleLineItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Product = product,
                            Description = $"{product.Name} ({product.Code})",
                            Quantity = saleQuantity,
                            MeasureUnit = resolvedUnit,
                            UnitPrice = product.Price,
                        });
                }

                sale.Subtotal = decimal.Round(
                    sale.LineItems.Sum(li => li.UnitPrice * li.Quantity),
                    2,
                    MidpointRounding.AwayFromZero);
                sale.Tax = decimal.Round(sale.Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
                sale.Total = sale.Subtotal + sale.Tax;

                invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = invoiceNumber,
                    SaleId = sale.Id,
                    CustomerId = customer.Id,
                    ClientName = sale.CustomerName,
                    ClientInitials = BuildInitials(sale.CustomerName),
                    BillingNote = $"Factura generada automáticamente para pedido {orderNumber}.",
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
                            ProductId = li.ProductId,
                            Description = li.Description,
                            Quantity = li.Quantity,
                            MeasureUnit = li.MeasureUnit,
                            UnitPrice = li.UnitPrice,
                        })
                        .ToList(),
                };

                movementRepository.AddRange(movements.ToList());
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
