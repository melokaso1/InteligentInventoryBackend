using Api.Dtos;
using Application.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;

namespace Api.Mapping;

public static class EntityMappers
{
    public static ProductDto ToProductDto(this Product product) =>
        new()
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Category = product.Category.Name,
            Price = product.Price,
            SaleUnit = product.SaleUnit.ToApiValue(),
            AllowsFractional = product.SaleUnit.AllowsFractional(),
            Stock = product.GetStock(),
            MaxStock = product.GetMaxStock(),
            Status = ToFrontendProductStatus(product.Status),
            Icon = product.Icon,
            Description = product.Description,
            UnitContentLabel = SaleMeasureUnitExtensions.ToUnitContentLabel(
                product.UnitContentAmount,
                product.UnitContentMeasure),
        };

    public static InventoryItemDto ToInventoryItemDto(this Product product)
    {
        var (stockLevel, stockPercent) = StockLevelHelper.GetStockLevel(product.GetStock(), product.GetMaxStock());
        return new InventoryItemDto
        {
            Id = product.Id,
            Sku = product.Code,
            Name = product.Name,
            Category = product.Category.Name,
            Warehouse = product.GetWarehouseName(),
            Quantity = product.GetStock(),
            UnitPrice = product.Price,
            StockLevel = stockLevel,
            StockPercent = stockPercent,
            Icon = product.Icon,
        };
    }

    public static StockMovementDto ToStockMovementDto(this InventoryMovement movement) =>
        new()
        {
            Id = movement.Id,
            Type = ToFrontendStockMovementType(movement.Type),
            Sku = movement.Product?.Code ?? string.Empty,
            Change = movement.QuantityChange > 0 ? $"+{movement.QuantityChange}" : movement.QuantityChange.ToString(),
            Timestamp = movement.CreatedAt.ToString("O"),
            Detail = movement.Detail,
        };

    public static SaleDto ToSaleDto(this Sale sale) =>
        new()
        {
            Id = sale.Id,
            Customer = sale.Customer?.FullName ?? sale.CustomerName,
            Email = sale.Customer?.Email ?? sale.CustomerEmail,
            Origin = ToFrontendSaleOrigin(sale.Origin),
            Date = sale.CreatedAt.ToString("O"),
            Total = sale.Total,
            Status = ToFrontendSaleStatus(sale.Status),
            LineItems = sale.LineItems.Select(ToSaleLineItemDto).ToList(),
            Subtotal = sale.Subtotal,
            Tax = sale.Tax,
            GrandTotal = sale.Total,
            OrderNumber = sale.OrderNumber,
            InvoiceNumber = sale.Invoice?.InvoiceNumber,
        };

    public static SaleLineItemDto ToSaleLineItemDto(this SaleLineItem lineItem) =>
        new()
        {
            Id = lineItem.Id,
            Name = lineItem.Product?.Name ?? lineItem.Description,
            Icon = lineItem.Product?.Icon ?? "receipt_long",
            Quantity = lineItem.Quantity,
            UnitPrice = lineItem.UnitPrice,
            MeasureUnit = lineItem.MeasureUnit.ToApiValue(),
        };

    public static InvoiceDto ToInvoiceDto(this Invoice invoice) =>
        new()
        {
            Id = invoice.Id,
            Client = invoice.ClientName,
            ClientInitials = invoice.ClientInitials,
            BillingNote = invoice.BillingNote,
            Date = invoice.IssueDate.ToString("yyyy-MM-dd"),
            DueDate = invoice.DueDate.ToString("yyyy-MM-dd"),
            Amount = invoice.Total,
            Status = ToFrontendInvoiceStatus(invoice.Status),
            LineItems = invoice.LineItems.Select(ToInvoiceLineItemDto).ToList(),
            Subtotal = invoice.Subtotal,
            Tax = invoice.Tax,
            Total = invoice.Total,
            InvoiceNumber = invoice.InvoiceNumber,
            SaleId = invoice.SaleId,
            Source = ToInvoiceSource(invoice),
        };

    public static InvoiceLineItemDto ToInvoiceLineItemDto(this InvoiceLineItem lineItem) =>
        new()
        {
            Description = lineItem.Description,
            Quantity = lineItem.Quantity,
            UnitPrice = lineItem.UnitPrice,
            MeasureUnit = lineItem.MeasureUnit.ToApiValue(),
        };

    public static ActivityItemDto ToActivityItemDto(this Sale sale) =>
        new()
        {
            Id = sale.Id.ToString(),
            Title = $"Venta {sale.OrderNumber}",
            Description = $"{sale.CustomerName} - {ToFrontendSaleOrigin(sale.Origin)}",
            Time = sale.CreatedAt.ToString("O"),
            DotBg = "bg-blue-100",
            DotBorder = "border-blue-300",
        };

    public static ActivityItemDto ToActivityItemDto(this InventoryMovement movement) =>
        new()
        {
            Id = movement.Id.ToString(),
            Title = $"Movimiento de stock {movement.Product?.Code}",
            Description = movement.Detail,
            Time = movement.CreatedAt.ToString("O"),
            DotBg = "bg-amber-100",
            DotBorder = "border-amber-300",
        };

    public static ActivityItemDto ToActivityItemDto(this Invoice invoice) =>
        new()
        {
            Id = invoice.Id.ToString(),
            Title = $"Factura {invoice.InvoiceNumber}",
            Description = $"{invoice.ClientName} - {ToFrontendInvoiceStatus(invoice.Status)}",
            Time = invoice.IssueDate.ToString("O"),
            DotBg = "bg-emerald-100",
            DotBorder = "border-amber-300",
        };

    public static string ToFrontendProductStatus(ProductStatus status) => status switch
    {
        ProductStatus.Active => "active",
        ProductStatus.Inactive => "inactive",
        ProductStatus.OutOfStock => "out_of_stock",
        ProductStatus.Archived => "archived",
        _ => "active",
    };

    public static string ToFrontendStockMovementType(StockMovementType type) => type switch
    {
        StockMovementType.Inbound => "inbound",
        StockMovementType.Adjustment => "adjustment",
        StockMovementType.Outbound => "outbound",
        _ => "adjustment",
    };

    public static string ToFrontendSaleStatus(SaleStatus status) => status switch
    {
        SaleStatus.Invoiced => "invoiced",
        SaleStatus.Pending => "pending",
        SaleStatus.Confirmed => "confirmed",
        SaleStatus.Cancelled => "cancelled",
        _ => "pending",
    };

    public static string ToFrontendSaleOrigin(SaleOrigin origin) => origin switch
    {
        SaleOrigin.Manual => "manual",
        SaleOrigin.Chatbot => "chatbot",
        _ => "manual",
    };

    public static string ToFrontendInvoiceStatus(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Paid => "paid",
        InvoiceStatus.Pending => "pending",
        InvoiceStatus.Overdue => "overdue",
        InvoiceStatus.Draft => "draft",
        _ => "draft",
    };

    public static string ToInvoiceSource(Invoice invoice) => invoice.SaleId switch
    {
        null => "manual",
        _ when invoice.Sale?.Origin == SaleOrigin.Chatbot => "chatbot",
        _ => "sale",
    };

    public static bool TryParseProductStatus(string status, out ProductStatus parsed)
    {
        var normalized = status.Trim().ToLowerInvariant();
        parsed = normalized switch
        {
            "active" => ProductStatus.Active,
            "inactive" => ProductStatus.Inactive,
            "out_of_stock" => ProductStatus.OutOfStock,
            "archived" => ProductStatus.Archived,
            _ => ProductStatus.Active,
        };

        return normalized is "active" or "inactive" or "out_of_stock" or "archived";
    }

    public static bool TryParseSaleStatus(string status, out SaleStatus parsed)
    {
        var normalized = status.Trim().ToLowerInvariant();
        parsed = normalized switch
        {
            "invoiced" => SaleStatus.Invoiced,
            "pending" => SaleStatus.Pending,
            "confirmed" => SaleStatus.Confirmed,
            "cancelled" => SaleStatus.Cancelled,
            _ => SaleStatus.Pending,
        };

        return normalized is "invoiced" or "pending" or "confirmed" or "cancelled";
    }

    public static bool TryParseSaleOrigin(string origin, out SaleOrigin parsed)
    {
        var normalized = origin.Trim().ToLowerInvariant();
        parsed = normalized switch
        {
            "manual" => SaleOrigin.Manual,
            "chatbot" => SaleOrigin.Chatbot,
            _ => SaleOrigin.Manual,
        };

        return normalized is "manual" or "chatbot";
    }

    public static bool TryParseInvoiceStatus(string status, out InvoiceStatus parsed)
    {
        var normalized = status.Trim().ToLowerInvariant();
        parsed = normalized switch
        {
            "paid" => InvoiceStatus.Paid,
            "pending" => InvoiceStatus.Pending,
            "overdue" => InvoiceStatus.Overdue,
            "draft" => InvoiceStatus.Draft,
            _ => InvoiceStatus.Draft,
        };

        return normalized is "paid" or "pending" or "overdue" or "draft";
    }
}
