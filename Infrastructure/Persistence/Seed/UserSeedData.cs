using Application.Abstractions;

using Domain.Constants;

using Domain.Entities;



namespace Infrastructure.Persistence.Seed;



public static class UserSeedData

{

    public const string DefaultAdminEmail = "admin@elplonsazo.com";

    public const string DefaultAdminPassword = "Admin123!";

    public const string DefaultAdminName = "Administrador";



    public static User CreateDefaultAdmin(IPasswordHasher passwordHasher) => new()

    {

        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),

        FullName = DefaultAdminName,

        Email = DefaultAdminEmail,

        PasswordHash = passwordHasher.Hash(DefaultAdminPassword),

        RoleId = RoleIds.Admin,

        IsActive = true,

        CreatedAt = DateTime.UtcNow,

        UpdatedAt = DateTime.UtcNow,

    };

}

