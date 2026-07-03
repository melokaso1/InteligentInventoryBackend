using Domain.Entities;

namespace Application.Models;

public sealed class StockDeductionLine
{
    public required Product Product { get; init; }
    public required decimal Quantity { get; init; }
    public string? MeasureUnit { get; init; }
}
