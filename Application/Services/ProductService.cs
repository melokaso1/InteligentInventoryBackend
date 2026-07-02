using Application.Abstractions;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class ProductService(IProductRepository productRepository, IUnitOfWork unitOfWork) : IProductService
{
    public Task<PagedResult<Product>> GetProductsAsync(ProductQueryModel query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        return productRepository.GetPagedAsync(query.Query, query.Category, page, pageSize, cancellationToken);
    }

    public async Task<ProductStatsModel> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        return new ProductStatsModel
        {
            TotalProducts = await productRepository.CountAsync(cancellationToken),
            ActiveProducts = await productRepository.CountByStatusAsync(ProductStatus.Active, cancellationToken),
            InactiveProducts = await productRepository.CountByStatusAsync(ProductStatus.Inactive, cancellationToken),
            OutOfStockProducts = await productRepository.CountByStatusAsync(ProductStatus.OutOfStock, cancellationToken),
            ArchivedProducts = await productRepository.CountByStatusAsync(ProductStatus.Archived, cancellationToken),
            TotalInventoryValue = await productRepository.SumInventoryValueAsync(cancellationToken),
        };
    }

    public Task<List<Product>> GetProductsForExportAsync(CancellationToken cancellationToken = default) =>
        productRepository.GetAllOrderedByCodeAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        productRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Product> CreateAsync(CreateProductModel request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("El código y nombre son obligatorios.");
        }

        ValidateNumericValues(request.Price, request.Stock, request.MaxStock);

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await productRepository.ExistsByCodeAsync(normalizedCode, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe un producto con ese código.");
        }

        var status = ResolveStatus(request.Status, request.Stock);
        var now = DateTime.UtcNow;
        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero),
            Stock = request.Stock,
            MaxStock = request.MaxStock,
            Status = status,
            Icon = request.Icon.Trim(),
            Description = request.Description.Trim(),
            Warehouse = request.Warehouse.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        productRepository.Add(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Product> UpdateAsync(Guid id, UpdateProductModel request, CancellationToken cancellationToken = default)
    {
        var entity = await productRepository.GetByIdTrackedAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Producto no encontrado.");

        ValidateNumericValues(request.Price, request.Stock, request.MaxStock);

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await productRepository.ExistsByCodeExceptIdAsync(id, normalizedCode, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe otro producto con ese código.");
        }

        var status = ResolveStatus(request.Status, request.Stock);

        entity.Code = normalizedCode;
        entity.Name = request.Name.Trim();
        entity.Category = request.Category.Trim();
        entity.Price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero);
        entity.Stock = request.Stock;
        entity.MaxStock = request.MaxStock;
        entity.Status = status;
        entity.Icon = request.Icon.Trim();
        entity.Description = request.Description.Trim();
        entity.Warehouse = request.Warehouse.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await productRepository.GetByIdTrackedAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Producto no encontrado.");

        productRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Product> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Producto no encontrado.");

        var baseCode = $"{source.Code}-COPY";
        var duplicateCode = baseCode;
        var suffix = 1;
        while (await productRepository.ExistsByCodeAsync(duplicateCode, cancellationToken))
        {
            duplicateCode = $"{baseCode}-{suffix}";
            suffix++;
        }

        var now = DateTime.UtcNow;
        var duplicate = new Product
        {
            Id = Guid.NewGuid(),
            Code = duplicateCode,
            Name = $"{source.Name} (Copia)",
            Category = source.Category,
            Price = source.Price,
            Stock = source.Stock,
            MaxStock = source.MaxStock,
            Status = source.Stock <= 0 ? ProductStatus.OutOfStock : source.Status,
            Icon = source.Icon,
            Description = source.Description,
            Warehouse = source.Warehouse,
            CreatedAt = now,
            UpdatedAt = now,
        };

        productRepository.Add(duplicate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return duplicate;
    }

    public async Task<Product> PatchStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        var entity = await productRepository.GetByIdTrackedAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Producto no encontrado.");

        if (!TryParseProductStatus(status, out var parsedStatus))
        {
            throw new InvalidOperationException("Estado inválido. Valores permitidos: active, inactive, out_of_stock, archived.");
        }

        entity.Status = parsedStatus;
        entity.UpdatedAt = DateTime.UtcNow;
        if (parsedStatus == ProductStatus.OutOfStock)
        {
            entity.Stock = 0;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static void ValidateNumericValues(decimal price, int stock, int maxStock)
    {
        if (price < 0 || stock < 0 || maxStock < 0)
        {
            throw new InvalidOperationException("Los valores numéricos no pueden ser negativos.");
        }
    }

    private static ProductStatus ResolveStatus(string? requestedStatus, int stock)
    {
        if (stock <= 0)
        {
            return ProductStatus.OutOfStock;
        }

        if (string.IsNullOrWhiteSpace(requestedStatus))
        {
            return ProductStatus.Active;
        }

        if (!TryParseProductStatus(requestedStatus, out var parsed))
        {
            throw new InvalidOperationException("Estado inválido. Valores permitidos: active, inactive, out_of_stock, archived.");
        }

        return parsed;
    }

    private static bool TryParseProductStatus(string status, out ProductStatus parsed)
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
}
