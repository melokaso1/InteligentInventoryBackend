using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;

namespace Application.Common;

public static class ProductStatusHelper
{
    public static bool IsAvailableForSale(Product product) =>
        product.Status == ProductStatus.Active && product.GetStock() > 0;

    public static void ApplyStockChange(Product product, decimal newStock)
    {
        if (newStock <= 0)
        {
            product.Status = ProductStatus.OutOfStock;
            return;
        }

        if (product.Status == ProductStatus.OutOfStock)
        {
            product.Status = ProductStatus.Active;
        }
    }
}
