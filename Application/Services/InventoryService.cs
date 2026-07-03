using Application.Abstractions;
using Application.Common;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;

namespace Application.Services;

public sealed class InventoryService(
    IInventoryRepository inventoryRepository,
    IProductRepository productRepository,
    IWarehouseRepository warehouseRepository,
    IInventoryMovementRepository movementRepository,
    IUnitOfWork unitOfWork) : IInventoryService
{
    private const int MaxPageSize = 200;
    private const int MaxExportPageSize = 500;

    public async Task<PagedResult<Product>> GetInventoryAsync(InventoryQueryModel query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxExportPageSize);

        var inventories = await inventoryRepository.GetFilteredAsync(
            query.Query,
            query.Category,
            query.Warehouse,
            cancellationToken);

        var products = inventories
            .Select(i =>
            {
                i.Product.Inventories = [i];
                return i.Product;
            })
            .ToList();

        var normalizedStockLevel = query.StockLevel?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedStockLevel))
        {
            products = products
                .Where(p => StockLevelHelper.GetStockLevel(p.GetStock(), p.GetMaxStock()).StockLevel == normalizedStockLevel)
                .ToList();
        }

        var totalCount = products.Count;
        var items = products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<InventoryStatsModel> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var inventories = await inventoryRepository.GetAllWithDetailsAsync(cancellationToken);
        return new InventoryStatsModel
        {
            TotalItems = inventories.Select(i => i.ProductId).Distinct().Count(),
            TotalUnits = inventories.Sum(i => i.CurrentStock),
            TotalValue = inventories.Sum(i => i.Product.Price * i.CurrentStock),
            LowStockCount = inventories.Count(i =>
                StockLevelHelper.GetStockLevel(i.CurrentStock, i.MaxStock).StockLevel is "low" or "critical"),
            OutOfStockCount = inventories.Count(i => i.CurrentStock <= 0),
        };
    }

    public async Task<PagedResult<InventoryMovement>> GetMovementsAsync(
        InventoryMovementQueryModel query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var type = TryParseMovementType(query.Type);

        return await movementRepository.GetPagedAsync(
            type,
            query.ProductCode,
            query.From,
            query.To,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<AdjustmentResultModel> CreateAdjustmentAsync(AdjustmentModel request, CancellationToken cancellationToken = default)
    {
        Product? product = null;
        if (request.ProductId.HasValue)
        {
            product = await productRepository.GetByIdTrackedAsync(request.ProductId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            var normalizedCode = request.ProductCode.Trim().ToUpperInvariant();
            product = await productRepository.GetByCodeAsync(normalizedCode, cancellationToken);
        }

        if (product is null)
        {
            throw new KeyNotFoundException("Producto no encontrado para ajustar inventario.");
        }

        var defaultWarehouse = await warehouseRepository.GetDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No hay almacén configurado.");

        var inventory = product.Inventories.FirstOrDefault(i => i.WarehouseId == defaultWarehouse.Id);
        if (inventory is null)
        {
            inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                WarehouseId = defaultWarehouse.Id,
                CurrentStock = 0,
                MinStock = 0,
                MaxStock = request.MaxStock ?? 100,
                UpdatedAt = DateTime.UtcNow,
            };
            inventoryRepository.Add(inventory);
            product.Inventories.Add(inventory);
        }

        var previousMaxStock = inventory.MaxStock;
        var maxStockChanged = false;
        if (request.MaxStock.HasValue)
        {
            if (request.MaxStock.Value < 0)
            {
                throw new InvalidOperationException("El límite máximo no puede ser negativo.");
            }

            maxStockChanged = request.MaxStock.Value != previousMaxStock;
            inventory.MaxStock = request.MaxStock.Value;
            inventory.MinStock = Math.Max(1, request.MaxStock.Value / 4);
        }

        var targetStock = inventory.CurrentStock + request.QuantityChange;
        var newStock = StockLevelHelper.ClampStock(targetStock, inventory.MaxStock);
        var actualChange = newStock - inventory.CurrentStock;
        var stockCapped = newStock < targetStock;
        var needsCapOnlyFix = !maxStockChanged
            && request.QuantityChange == 0
            && inventory.CurrentStock > inventory.MaxStock;

        if (!maxStockChanged && request.QuantityChange == 0 && !needsCapOnlyFix)
        {
            throw new InvalidOperationException("El ajuste no puede ser cero.");
        }

        if (actualChange == 0 && !maxStockChanged && !needsCapOnlyFix)
        {
            throw new InvalidOperationException("El ajuste no puede ser cero.");
        }

        inventory.CurrentStock = newStock;
        inventory.UpdatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        if (inventory.CurrentStock <= 0)
        {
            product.Status = ProductStatus.OutOfStock;
        }
        else if (product.Status == ProductStatus.OutOfStock)
        {
            product.Status = ProductStatus.Active;
        }

        InventoryMovement? movement = null;
        if (actualChange != 0)
        {
            movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                InventoryId = inventory.Id,
                Inventory = inventory,
                Type = StockMovementType.Adjustment,
                QuantityChange = actualChange,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Ajuste manual" : request.Reason.Trim(),
                Detail = string.IsNullOrWhiteSpace(request.Detail) ? "Ajuste realizado desde API" : request.Detail.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            movementRepository.Add(movement);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        movement ??= new InventoryMovement
        {
            Id = Guid.Empty,
            ProductId = product.Id,
            Product = product,
            Type = StockMovementType.Adjustment,
            QuantityChange = 0,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Ajuste manual" : request.Reason.Trim(),
            Detail = "Límite máximo actualizado",
            CreatedAt = DateTime.UtcNow,
        };

        return new AdjustmentResultModel
        {
            Movement = movement,
            ResultingStock = newStock,
            MaxStock = inventory.MaxStock,
            StockCapped = stockCapped,
        };
    }

    public Task<List<Product>> GetInventoryForExportAsync(CancellationToken cancellationToken = default) =>
        productRepository.GetAllOrderedByCodeAsync(cancellationToken);

    public Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        productRepository.GetDistinctCategoriesAsync(cancellationToken);

    private static StockMovementType? TryParseMovementType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        return type.Trim().ToLowerInvariant() switch
        {
            "inbound" => StockMovementType.Inbound,
            "adjustment" => StockMovementType.Adjustment,
            "outbound" => StockMovementType.Outbound,
            _ => null,
        };
    }
}
