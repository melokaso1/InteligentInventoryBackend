namespace Application.Common;

public static class StockLevelHelper
{
    public static (string StockLevel, decimal StockPercent) GetStockLevel(decimal stock, decimal maxStock)
    {
        if (maxStock <= 0)
        {
            return ("critical", 0m);
        }

        var percent = Math.Round((stock * 100m) / maxStock, 2, MidpointRounding.AwayFromZero);
        if (stock <= 0 || percent <= 10m)
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
}
