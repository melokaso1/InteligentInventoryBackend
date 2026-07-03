using Domain.Enums;

namespace Domain.Entities;

public class InvoiceLineItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public SaleMeasureUnit MeasureUnit { get; set; } = SaleMeasureUnit.Unit;
    public decimal UnitPrice { get; set; }
}
