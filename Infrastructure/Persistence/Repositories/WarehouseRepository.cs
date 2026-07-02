using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository(AppDbContext context) : IWarehouseRepository
{
    public Task<Warehouse?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        context.Warehouses.AsNoTracking().FirstOrDefaultAsync(
            w => w.Name == name,
            cancellationToken);

    public async Task<Warehouse?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        var defaultWarehouse = await context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.IsDefault, cancellationToken);

        return defaultWarehouse
            ?? await context.Warehouses.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<Warehouse>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        context.Warehouses.AsNoTracking().Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync(cancellationToken);

    public void Add(Warehouse entity) => context.Warehouses.Add(entity);
}
