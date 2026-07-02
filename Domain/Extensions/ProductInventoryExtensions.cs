using Domain.Constants;
using Domain.Entities;

namespace Domain.Extensions;

public static class ProductInventoryExtensions
{
    public static Inventory? GetDefaultInventory(this Product product) =>
        product.Inventories.FirstOrDefault(i => i.Warehouse.IsDefault)
        ?? product.Inventories.FirstOrDefault();

    public static Inventory? GetInventoryAt(this Product product, string warehouseName) =>
        product.Inventories.FirstOrDefault(i =>
            string.Equals(i.Warehouse.Name, warehouseName, StringComparison.OrdinalIgnoreCase));

    public static int GetStock(this Product product) =>
        product.GetDefaultInventory()?.CurrentStock ?? 0;

    public static int GetMaxStock(this Product product) =>
        product.GetDefaultInventory()?.MaxStock ?? 0;

    public static string GetWarehouseName(this Product product) =>
        product.GetDefaultInventory()?.Warehouse.Name ?? WarehouseNames.Default;
}
