using System.Text.RegularExpressions;

namespace Application.Validation;

public static class PasswordValidator
{
    private static readonly Regex UppercaseRegex = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new("[0-9]", RegexOptions.Compiled);
    private static readonly Regex SymbolRegex = new(@"[^A-Za-z0-9]", RegexOptions.Compiled);

    public static void Validate(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            throw new InvalidOperationException("Longitud insuficiente: la contraseña debe tener al menos 8 caracteres.");
        }

        if (!UppercaseRegex.IsMatch(password))
        {
            throw new InvalidOperationException("Falta mayúscula: la contraseña debe incluir al menos una letra mayúscula.");
        }

        if (!LowercaseRegex.IsMatch(password))
        {
            throw new InvalidOperationException("Falta minúscula: la contraseña debe incluir al menos una letra minúscula.");
        }

        if (!DigitRegex.IsMatch(password))
        {
            throw new InvalidOperationException("Falta número: la contraseña debe incluir al menos un dígito.");
        }

        if (!SymbolRegex.IsMatch(password))
        {
            throw new InvalidOperationException("Falta símbolo: la contraseña debe incluir al menos un carácter especial.");
        }
    }
}
