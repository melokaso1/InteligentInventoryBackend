using Domain.Entities;

namespace Infrastructure.Persistence.Seed;

// Catálogo ficticio de taller El Plonsazo. Tras cambiar categorías/productos: docker compose down -v
// o reiniciar la API (upsert añade categorías/productos PLZ-* faltantes y sincroniza nombres en existentes).
public static class CategorySeedData
{
    public static int ExpectedCategoryCount => DefaultCategoryNames.Length;

    public static readonly string[] DefaultCategoryNames =
    [
        "Alucinógenos",
        "Estimulantes",
        "Inhalantes",
        "Disociativos",
        "Depresores",
        "Opioides",
        "Cannabis",
        "Cannabinoides sintéticos",
        "Drogas sintéticas",
        "Nicotina",
        "Esteroides anabólicos",
        "Medicamentos con prescripción",
        "Benzodiacepinas",
        "Antitusivos",
    ];

    public static List<Category> Create() =>
        DefaultCategoryNames
            .Select(name => new Category
            {
                Name = name,
                Description = $"Categoría {name} — catálogo ficticio El Plonsazo (taller).",
            })
            .ToList();
}
