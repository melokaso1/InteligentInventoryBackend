using Domain.Enums;

namespace Domain.Entities;

public class SaleLineItem
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public SaleMeasureUnit MeasureUnit { get; set; } = SaleMeasureUnit.Unit;
    public decimal UnitPrice { get; set; }
}
