namespace Application.Common;

/// <summary>
/// Colombia (COT, UTC-5, no DST) calendar-day boundaries for API date filters.
/// </summary>
public static class ColombiaTimeHelper
{
    private const int ColombiaUtcOffsetHours = 5;

    /// <summary>Start of calendar day in Colombia, as UTC (00:00:00 COT).</summary>
    public static DateTime ToUtcStartOfColombiaDay(DateTime date) =>
        DateTime.SpecifyKind(date.Date.AddHours(ColombiaUtcOffsetHours), DateTimeKind.Utc);

    /// <summary>End of calendar day in Colombia inclusive, as UTC (23:59:59.999 COT).</summary>
    public static DateTime ToUtcEndOfColombiaDay(DateTime date)
    {
        var endLocal = date.Date.AddDays(1).AddMilliseconds(-1);
        return DateTime.SpecifyKind(endLocal.AddHours(ColombiaUtcOffsetHours), DateTimeKind.Utc);
    }

    /// <summary>Current calendar date in Colombia (COT).</summary>
    public static DateTime TodayInColombia() =>
        DateTime.UtcNow.AddHours(-ColombiaUtcOffsetHours).Date;

    /// <summary>UTC [from, to) for the current Colombia calendar day.</summary>
    public static (DateTime FromInclusiveUtc, DateTime ToExclusiveUtc) GetUtcRangeForColombiaToday()
    {
        var today = TodayInColombia();
        return (ToUtcStartOfColombiaDay(today), ToUtcStartOfColombiaDay(today.AddDays(1)));
    }
}
