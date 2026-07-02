using Domain.Entities;

namespace Application.Abstractions;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<List<Warehouse>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    void Add(Warehouse entity);
}
