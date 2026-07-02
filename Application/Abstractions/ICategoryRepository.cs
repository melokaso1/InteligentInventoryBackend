using Domain.Entities;

namespace Application.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Category> GetOrCreateAsync(string name, CancellationToken cancellationToken = default);
    Task<List<string>> GetAllNamesAsync(CancellationToken cancellationToken = default);
}
