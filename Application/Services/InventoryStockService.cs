using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;

namespace Application.Services;

public sealed class InventoryStockService(
    IInventoryRepository inventoryRepository,
    IWarehouseRepository warehouseRepository) : IInventoryStockService
{
    public void ValidateSufficientStock(IReadOnlyList<StockDeductionLine> lines)
    {
        foreach (var line in lines)
        {
            var saleQuantity = ResolveQuantity(line);
            if (line.Product.GetStock() < saleQuantity)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente para el producto {line.Product.Code}. " +
                    $"Disponible: {line.Product.GetStock():0.####} {line.Product.SaleUnit.ToShortLabel()}.");
            }
        }
    }

    public async Task<IReadOnlyList<InventoryMovement>> DeductStockAsync(
        IReadOnlyList<StockDeductionLine> lines,
        string reason,
        string detail,
        DateTime occurredAt,
        CancellationToken cancellationToken = default)
    {
        var defaultWarehouse = await warehouseRepository.GetDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No hay almacén configurado.");

        var movements = new List<InventoryMovement>();
        foreach (var line in lines)
        {
            var saleQuantity = ResolveQuantity(line);
            var inventory = await GetOrCreateInventoryAsync(line.Product, defaultWarehouse.Id, cancellationToken);
            inventory.CurrentStock -= saleQuantity;
            inventory.UpdatedAt = occurredAt;
            line.Product.Status = inventory.CurrentStock <= 0 ? ProductStatus.OutOfStock : ProductStatus.Active;
            line.Product.UpdatedAt = occurredAt;

            movements.Add(
                new InventoryMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = line.Product.Id,
                    InventoryId = inventory.Id,
                    Product = line.Product,
                    Type = StockMovementType.Outbound,
                    QuantityChange = -saleQuantity,
                    Reason = reason,
                    Detail = detail,
                    CreatedAt = occurredAt,
                });
        }

        return movements;
    }

    private static decimal ResolveQuantity(StockDeductionLine line) =>
        SaleMeasureUnitExtensions.ResolveSaleQuantity(
            line.Product,
            line.Quantity,
            line.MeasureUnit,
            out _);

    private async Task<Inventory> GetOrCreateInventoryAsync(
        Product product,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var inventory = product.Inventories.FirstOrDefault(i => i.WarehouseId == warehouseId);
        if (inventory is not null)
        {
            return inventory;
        }

        inventory = await inventoryRepository.GetByProductAndWarehouseTrackedAsync(
            product.Id,
            warehouseId,
            cancellationToken);

        if (inventory is not null)
        {
            product.Inventories.Add(inventory);
            return inventory;
        }

        inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            WarehouseId = warehouseId,
            CurrentStock = product.GetStock(),
            MinStock = 0,
            MaxStock = product.GetMaxStock(),
            UpdatedAt = DateTime.UtcNow,
        };
        inventoryRepository.Add(inventory);
        product.Inventories.Add(inventory);
        return inventory;
    }
}
