using Domain.Constants;
using Domain.Entities;

namespace Infrastructure.Persistence.Seed;

public static class RoleSeedData
{
    public static List<Role> Create() =>
    [
        new()
        {
            Id = RoleIds.Admin,
            Name = "Admin",
            Description = "Administrador del sistema con acceso completo",
        },
        new()
        {
            Id = RoleIds.Cliente,
            Name = "Cliente",
            Description = "Usuario cliente con acceso al chatbot y soporte",
        },
    ];
}
