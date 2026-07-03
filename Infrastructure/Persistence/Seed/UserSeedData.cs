namespace Infrastructure.Persistence.Seed;

public static class UserSeedData
{
    public const string AdminEmail = "admin@elplonsazo.com";
    public const string AdminPassword = "Admin123!";
    public const string AdminFullName = "Jhon Alejandro Escobar Lozada";

    /// <summary>Legacy demo cliente removed from seed; used only to purge existing DB rows on startup.</summary>
    internal const string LegacyDemoClienteEmail = "cliente@elplonsazo.com";
}
