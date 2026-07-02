using Application.Abstractions;
using Application.Common;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class InventoryService(
    IProductRepository productRepository,
    IInventoryMovementRepository movementRepository,
    IUnitOfWork unitOfWork) : IInventoryService
{
    public async Task<PagedResult<Product>> GetInventoryAsync(InventoryQueryModel query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var filteredProducts = await productRepository.GetFilteredAsync(
            query.Query,
            query.Category,
            query.Warehouse,
            cancellationToken);

        var normalizedStockLevel = query.StockLevel?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedStockLevel))
        {
            filteredProducts = filteredProducts
                .Where(p => StockLevelHelper.GetStockLevel(p.Stock, p.MaxStock).StockLevel == normalizedStockLevel)
                .ToList();
        }

        var totalCount = filteredProducts.Count;
        var items = filteredProducts
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
        var products = await productRepository.GetAllAsync(cancellationToken);
        return new InventoryStatsModel
        {
            TotalItems = products.Count,
            TotalUnits = products.Sum(p => p.Stock),
            TotalValue = products.Sum(p => p.Price * p.Stock),
            LowStockCount = products.Count(p => StockLevelHelper.GetStockLevel(p.Stock, p.MaxStock).StockLevel is "low" or "critical"),
            OutOfStockCount = products.Count(p => p.Stock <= 0),
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

    public async Task<InventoryMovement> CreateAdjustmentAsync(AdjustmentModel request, CancellationToken cancellationToken = default)
    {
        if (request.QuantityChange == 0)
        {
            throw new InvalidOperationException("El ajuste no puede ser cero.");
        }

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

        var resultingStock = product.Stock + request.QuantityChange;
        if (resultingStock < 0)
        {
            throw new InvalidOperationException("El ajuste deja el stock en negativo.");
        }

        product.Stock = resultingStock;
        product.UpdatedAt = DateTime.UtcNow;
        if (product.Stock <= 0)
        {
            product.Status = ProductStatus.OutOfStock;
        }
        else if (product.Status == ProductStatus.OutOfStock)
        {
            product.Status = ProductStatus.Active;
        }

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Type = StockMovementType.Adjustment,
            QuantityChange = request.QuantityChange,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Ajuste manual" : request.Reason.Trim(),
            Detail = string.IsNullOrWhiteSpace(request.Detail) ? "Ajuste realizado desde API" : request.Detail.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        movementRepository.Add(movement);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return movement;
    }

    public Task<List<Product>> GetInventoryForExportAsync(CancellationToken cancellationToken = default) =>
        productRepository.GetAllOrderedByCodeAsync(cancellationToken);

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
