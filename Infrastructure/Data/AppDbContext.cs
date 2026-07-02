using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLineItem> SaleLineItems => Set<SaleLineItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.Embedding).HasColumnType("vector(1536)");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasIndex(s => s.OrderNumber).IsUnique();
            entity.Property(s => s.Subtotal).HasPrecision(18, 2);
            entity.Property(s => s.Tax).HasPrecision(18, 2);
            entity.Property(s => s.Total).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SaleLineItem>(entity =>
        {
            entity.Property(li => li.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(i => i.InvoiceNumber).IsUnique();
            entity.Property(i => i.Subtotal).HasPrecision(18, 2);
            entity.Property(i => i.Tax).HasPrecision(18, 2);
            entity.Property(i => i.Total).HasPrecision(18, 2);
            entity.HasOne(i => i.Sale)
                .WithOne(s => s.Invoice)
                .HasForeignKey<Invoice>(i => i.SaleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.Property(li => li.UnitPrice).HasPrecision(18, 2);
        });
    }
}
