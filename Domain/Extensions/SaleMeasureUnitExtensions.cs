using Domain.Entities;
using Domain.Enums;

namespace Domain.Extensions;

public static class SaleMeasureUnitExtensions
{
    public static bool AllowsFractional(this SaleMeasureUnit unit) => unit is not SaleMeasureUnit.Unit;

    public static bool IsWeightUnit(this SaleMeasureUnit unit) =>
        unit is SaleMeasureUnit.Gram or SaleMeasureUnit.Kilogram or SaleMeasureUnit.Milligram;

    public static bool IsVolumeUnit(this SaleMeasureUnit unit) =>
        unit is SaleMeasureUnit.Milliliter or SaleMeasureUnit.Liter;

    public static string ToApiValue(this SaleMeasureUnit unit) => unit switch
    {
        SaleMeasureUnit.Unit => "unit",
        SaleMeasureUnit.Gram => "gram",
        SaleMeasureUnit.Kilogram => "kilogram",
        SaleMeasureUnit.Milligram => "milligram",
        SaleMeasureUnit.Milliliter => "milliliter",
        SaleMeasureUnit.Liter => "liter",
        _ => "unit",
    };

    public static string ToDisplayLabel(this SaleMeasureUnit unit, bool plural = false) => unit switch
    {
        SaleMeasureUnit.Unit => plural ? "unidades" : "unidad",
        SaleMeasureUnit.Gram => plural ? "gramos" : "gramo",
        SaleMeasureUnit.Kilogram => plural ? "kilogramos" : "kilogramo",
        SaleMeasureUnit.Milligram => plural ? "miligramos" : "miligramo",
        SaleMeasureUnit.Milliliter => plural ? "mililitros" : "mililitro",
        SaleMeasureUnit.Liter => plural ? "litros" : "litro",
        _ => plural ? "unidades" : "unidad",
    };

    public static string ToShortLabel(this SaleMeasureUnit unit) => unit switch
    {
        SaleMeasureUnit.Unit => "u.",
        SaleMeasureUnit.Gram => "g",
        SaleMeasureUnit.Kilogram => "kg",
        SaleMeasureUnit.Milligram => "mg",
        SaleMeasureUnit.Milliliter => "ml",
        SaleMeasureUnit.Liter => "L",
        _ => "u.",
    };

    public static string? ToUnitContentLabel(decimal? amount, SaleMeasureUnit? measure)
    {
        if (amount is null or <= 0 || measure is null)
        {
            return null;
        }

        var formattedAmount = amount.Value % 1m == 0m
            ? amount.Value.ToString("0")
            : amount.Value.ToString("0.##");

        return $"{formattedAmount} {measure.Value.ToShortLabel()} por unidad";
    }

    public static bool TryParse(string? value, out SaleMeasureUnit unit)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            unit = SaleMeasureUnit.Unit;
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        unit = normalized switch
        {
            "unit" or "units" or "unidad" or "unidades" or "u" => SaleMeasureUnit.Unit,
            "gram" or "grams" or "gramo" or "gramos" or "g" => SaleMeasureUnit.Gram,
            "kilogram" or "kilograms" or "kilogramo" or "kilogramos" or "kilo" or "kilos" or "kg" => SaleMeasureUnit.Kilogram,
            "milligram" or "milligrams" or "miligramo" or "miligramos" or "mg" => SaleMeasureUnit.Milligram,
            "milliliter" or "milliliters" or "mililitro" or "mililitros" or "ml" => SaleMeasureUnit.Milliliter,
            "liter" or "liters" or "litro" or "litros" or "l" => SaleMeasureUnit.Liter,
            _ => SaleMeasureUnit.Unit,
        };

        return normalized is
            "unit" or "units" or "unidad" or "unidades" or "u" or
            "gram" or "grams" or "gramo" or "gramos" or "g" or
            "kilogram" or "kilograms" or "kilogramo" or "kilogramos" or "kilo" or "kilos" or "kg" or
            "milligram" or "milligrams" or "miligramo" or "miligramos" or "mg" or
            "milliliter" or "milliliters" or "mililitro" or "mililitros" or "ml" or
            "liter" or "liters" or "litro" or "litros" or "l";
    }

    public static bool IsCompatibleWith(this SaleMeasureUnit requested, SaleMeasureUnit productUnit)
    {
        if (requested == productUnit)
        {
            return true;
        }

        return productUnit switch
        {
            SaleMeasureUnit.Gram => requested.IsWeightUnit(),
            SaleMeasureUnit.Kilogram => requested.IsWeightUnit(),
            SaleMeasureUnit.Milligram => requested.IsWeightUnit(),
            SaleMeasureUnit.Milliliter => requested.IsVolumeUnit(),
            SaleMeasureUnit.Liter => requested.IsVolumeUnit(),
            SaleMeasureUnit.Unit => requested == SaleMeasureUnit.Unit,
            _ => false,
        };
    }

    public static decimal ConvertTo(decimal quantity, SaleMeasureUnit fromUnit, SaleMeasureUnit toUnit)
    {
        if (fromUnit == toUnit)
        {
            return quantity;
        }

        if (!fromUnit.IsCompatibleWith(toUnit))
        {
            throw new InvalidOperationException(
                $"No se puede convertir de {fromUnit.ToDisplayLabel()} a {toUnit.ToDisplayLabel()}.");
        }

        var inGrams = fromUnit switch
        {
            SaleMeasureUnit.Gram => quantity,
            SaleMeasureUnit.Kilogram => quantity * 1000m,
            SaleMeasureUnit.Milligram => quantity / 1000m,
            _ => quantity,
        };

        var inMilliliters = fromUnit switch
        {
            SaleMeasureUnit.Milliliter => quantity,
            SaleMeasureUnit.Liter => quantity * 1000m,
            _ => quantity,
        };

        return toUnit switch
        {
            SaleMeasureUnit.Gram => inGrams,
            SaleMeasureUnit.Kilogram => inGrams / 1000m,
            SaleMeasureUnit.Milligram => inGrams * 1000m,
            SaleMeasureUnit.Milliliter => inMilliliters,
            SaleMeasureUnit.Liter => inMilliliters / 1000m,
            SaleMeasureUnit.Unit => quantity,
            _ => quantity,
        };
    }

    public static decimal ResolveSaleQuantity(
        Product product,
        decimal quantity,
        string? measureUnit,
        out SaleMeasureUnit resolvedUnit)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("La cantidad debe ser mayor a cero.");
        }

        var requestedUnit = SaleMeasureUnitExtensions.TryParse(measureUnit, out var parsed)
            ? parsed
            : product.SaleUnit;

        if (!requestedUnit.IsCompatibleWith(product.SaleUnit))
        {
            throw new InvalidOperationException(
                $"Este producto se vende por {product.SaleUnit.ToDisplayLabel(plural: true)}; " +
                $"no acepta medida en {requestedUnit.ToDisplayLabel(plural: true)}.");
        }

        var normalized = ConvertTo(quantity, requestedUnit, product.SaleUnit);
        normalized = decimal.Round(normalized, 4, MidpointRounding.AwayFromZero);

        if (!product.SaleUnit.AllowsFractional() && normalized != decimal.Truncate(normalized))
        {
            throw new InvalidOperationException(
                $"Este producto solo se vende en {product.SaleUnit.ToDisplayLabel(plural: true)} enteras.");
        }

        resolvedUnit = product.SaleUnit;
        return normalized;
    }
}
