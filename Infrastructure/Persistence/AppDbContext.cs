using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLineItem> SaleLineItems => Set<SaleLineItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SaleStatusLookup> SaleStatuses => Set<SaleStatusLookup>();
    public DbSet<SaleOriginLookup> SaleOrigins => Set<SaleOriginLookup>();
    public DbSet<InvoiceStatusLookup> InvoiceStatuses => Set<InvoiceStatusLookup>();
    public DbSet<MovementTypeLookup> MovementTypes => Set<MovementTypeLookup>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ProductEmbedding> ProductEmbeddings => Set<ProductEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        ConfigureLookups(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureWarehouse(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureInventoryMovement(modelBuilder);
        ConfigureSale(modelBuilder);
        ConfigureInvoice(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureChat(modelBuilder);
        ConfigureProductEmbedding(modelBuilder);
    }

    private static void ConfigureLookups(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SaleStatusLookup>(entity =>
        {
            entity.Property(l => l.Id).ValueGeneratedNever();
            entity.Property(l => l.Name).HasMaxLength(50);
            entity.HasIndex(l => l.Name).IsUnique();
        });

        modelBuilder.Entity<SaleOriginLookup>(entity =>
        {
            entity.Property(l => l.Id).ValueGeneratedNever();
            entity.Property(l => l.Name).HasMaxLength(50);
            entity.HasIndex(l => l.Name).IsUnique();
        });

        modelBuilder.Entity<InvoiceStatusLookup>(entity =>
        {
            entity.Property(l => l.Id).ValueGeneratedNever();
            entity.Property(l => l.Name).HasMaxLength(50);
            entity.HasIndex(l => l.Name).IsUnique();
        });

        modelBuilder.Entity<MovementTypeLookup>(entity =>
        {
            entity.Property(l => l.Id).ValueGeneratedNever();
            entity.Property(l => l.Name).HasMaxLength(50);
            entity.HasIndex(l => l.Name).IsUnique();
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(500);
        });
    }

    private static void ConfigureWarehouse(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(w => w.Name).IsUnique();
            entity.Property(w => w.Name).HasMaxLength(100);
            entity.Property(w => w.Location).HasMaxLength(200);
        });
    }

    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Email);
            entity.Property(c => c.FullName).HasMaxLength(200);
            entity.Property(c => c.Email).HasMaxLength(256);
            entity.Property(c => c.Phone).HasMaxLength(50);
            entity.Property(c => c.DocumentNumber).HasMaxLength(50);
        });
    }

    private static void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.HasIndex(p => p.CategoryId);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasIndex(i => new { i.ProductId, i.WarehouseId }).IsUnique();
            entity.HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Warehouse)
                .WithMany(w => w.Inventories)
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInventoryMovement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.Property(m => m.Type).HasConversion<int>();

            entity.HasOne(m => m.Product)
                .WithMany(p => p.Movements)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Inventory)
                .WithMany(i => i.Movements)
                .HasForeignKey(m => m.InventoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureSale(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasIndex(s => s.OrderNumber).IsUnique();
            entity.HasIndex(s => s.CreatedAt);
            entity.Property(s => s.Subtotal).HasPrecision(18, 2);
            entity.Property(s => s.Tax).HasPrecision(18, 2);
            entity.Property(s => s.Total).HasPrecision(18, 2);
            entity.Property(s => s.Status).HasConversion<int>();
            entity.Property(s => s.Origin).HasConversion<int>();

            entity.HasOne(s => s.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.ChatSession)
                .WithMany(cs => cs.Sales)
                .HasForeignKey(s => s.ChatSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureInvoice(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(i => i.InvoiceNumber).IsUnique();
            entity.Property(i => i.Subtotal).HasPrecision(18, 2);
            entity.Property(i => i.Tax).HasPrecision(18, 2);
            entity.Property(i => i.Total).HasPrecision(18, 2);
            entity.Property(i => i.Status).HasConversion<int>();

            entity.HasOne(i => i.Sale)
                .WithOne(s => s.Invoice)
                .HasForeignKey<Invoice>(i => i.SaleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.Property(li => li.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(li => li.Product)
                .WithMany()
                .HasForeignKey(li => li.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.Name).HasMaxLength(50);
            entity.Property(r => r.Description).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.FullName).HasMaxLength(200);
            entity.Property(u => u.PasswordHash).HasMaxLength(256);

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<User>(u => u.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureChat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasIndex(cs => cs.SessionToken).IsUnique();
            entity.Property(cs => cs.SessionToken).HasMaxLength(128);
            entity.Property(cs => cs.CurrentStateJson).HasColumnType("jsonb");

            entity.HasOne(cs => cs.Customer)
                .WithMany(c => c.ChatSessions)
                .HasForeignKey(cs => cs.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cs => cs.User)
                .WithMany()
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(cm => cm.ChatSessionId);
            entity.HasIndex(cm => cm.CreatedAt);
            entity.Property(cm => cm.MessageText).HasMaxLength(4000);
            entity.Property(cm => cm.MetadataJson).HasColumnType("jsonb");

            entity.HasOne(cm => cm.ChatSession)
                .WithMany(cs => cs.Messages)
                .HasForeignKey(cm => cm.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductEmbedding(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEmbedding>(entity =>
        {
            entity.HasIndex(pe => pe.ProductId);
            entity.Property(pe => pe.Embedding).HasColumnType("vector(1536)");
            entity.Property(pe => pe.ContentText).HasMaxLength(4000);

            entity.HasOne(pe => pe.Product)
                .WithMany(p => p.Embeddings)
                .HasForeignKey(pe => pe.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
