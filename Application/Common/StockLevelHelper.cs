namespace Application.Common;

public static class StockLevelHelper
{
    public static decimal ClampStock(decimal stock, decimal maxStock) =>
        maxStock <= 0 ? 0m : Math.Max(0m, Math.Min(stock, maxStock));

    public static (string StockLevel, decimal StockPercent) GetStockLevel(decimal stock, decimal maxStock)
    {
        if (stock <= 0)
        {
            return ("out_of_stock", 0m);
        }

        if (maxStock <= 0)
        {
            return ("critical", 0m);
        }

        var percent = Math.Round((stock * 100m) / maxStock, 2, MidpointRounding.AwayFromZero);
        if (percent <= 10m)
        {
            return ("critical", Math.Max(0m, percent));
        }

        if (percent <= 30m)
        {
            return ("low", percent);
        }

        if (percent <= 70m)
        {
            return ("medium", percent);
        }

        return ("high", percent);
    }

    public static string GetLowStockStatus(decimal stock, decimal maxStock)
    {
        if (stock <= 0)
        {
            return "out_of_stock";
        }

        var (stockLevel, _) = GetStockLevel(stock, maxStock);
        return stockLevel == "critical" ? "critical" : "low_stock";
    }

    public static bool IsOutOfStock(decimal stock) => stock <= 0;

    public static bool MatchesStockLevelFilter(string filter, decimal stock, decimal maxStock)
    {
        var normalized = filter.Trim().ToLowerInvariant();
        if (normalized is "out_of_stock" or "zero")
        {
            return IsOutOfStock(stock);
        }

        var (stockLevel, _) = GetStockLevel(stock, maxStock);
        return stockLevel == normalized;
    }
}
