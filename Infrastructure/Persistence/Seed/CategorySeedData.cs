using Domain.Entities;

namespace Infrastructure.Persistence.Seed;

public static class CategorySeedData
{
    public static readonly string[] DefaultCategoryNames =
    [
        "Electrónica",
        "Periféricos",
        "Accesorios",
        "Almacenamiento",
        "Mobiliario",
        "Oficina",
        "Consumibles",
        "Redes",
    ];

    public static List<Category> Create() =>
        DefaultCategoryNames
            .Select(name => new Category
            {
                Name = name,
                Description = $"Categoría {name}",
            })
            .ToList();
}
