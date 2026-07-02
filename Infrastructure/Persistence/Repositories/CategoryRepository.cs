using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == normalized, cancellationToken);
    }

    public async Task<Category> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        var existing = await GetByNameAsync(trimmed, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var category = new Category
        {
            Name = trimmed,
            Description = $"Categoría {trimmed}",
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public Task<List<string>> GetAllNamesAsync(CancellationToken cancellationToken = default) =>
        context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);
}
