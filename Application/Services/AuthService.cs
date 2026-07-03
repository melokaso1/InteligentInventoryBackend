using Application.Abstractions;
using Application.Models;
using Application.Validation;
using Domain.Constants;
using Domain.Entities;

namespace Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork) : IAuthService
{
    public async Task<AuthResultModel> LoginAsync(LoginModel model, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(model.Email);
        var user = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("La cuenta está desactivada.");
        }

        if (!passwordHasher.Verify(model.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        }

        return BuildAuthResult(user);
    }

    public async Task<AuthResultModel> RegisterAsync(RegisterModel model, CancellationToken cancellationToken = default)
    {
        PasswordValidator.Validate(model.Password);

        var email = NormalizeEmail(model.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una cuenta con ese correo electrónico.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = model.Name.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(model.Password),
            RoleId = RoleIds.Cliente,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await userRepository.AddAsync(user, cancellationToken);
        var created = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new InvalidOperationException("No se pudo crear el usuario.");

        return BuildAuthResult(created);
    }

    public async Task ChangePasswordAsync(ChangePasswordModel model, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(model.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("La cuenta está desactivada.");
        }

        if (!passwordHasher.Verify(model.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("La contraseña actual es incorrecta.");
        }

        PasswordValidator.Validate(model.NewPassword);

        user.PasswordHash = passwordHasher.Hash(model.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private AuthResultModel BuildAuthResult(User user) => new()
    {
        Token = tokenService.GenerateToken(user),
        User = new AuthUserModel
        {
            Id = user.Id,
            Name = user.FullName,
            Email = user.Email,
            Role = MapRole(user.Role.Name),
        },
    };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string MapRole(string roleName) => roleName switch
    {
        "Admin" => "admin",
        "Cliente" => "cliente",
        _ => "cliente",
    };
}
