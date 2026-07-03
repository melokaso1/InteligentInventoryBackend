using Application.Abstractions;
using Application.Models;
using Application.Validation;
using Domain.Constants;
using Domain.Entities;

namespace Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    ICustomerRepository customerRepository,
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

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("El nombre completo es obligatorio.");
        }

        var email = NormalizeEmail(model.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una cuenta con ese correo electrónico.");
        }

        var customer = await customerRepository.GetOrCreateAsync(name, email, cancellationToken);
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Email = email,
            PasswordHash = passwordHasher.Hash(model.Password),
            RoleId = RoleIds.Cliente,
            CustomerId = customer.Id,
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

    public async Task<AuthResultModel> UpdateProfileAsync(UpdateProfileModel model, CancellationToken cancellationToken = default)
    {
        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("El nombre completo es obligatorio.");
        }

        var user = await userRepository.GetByIdAsync(model.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("La cuenta está desactivada.");
        }

        user.FullName = name;
        user.UpdatedAt = DateTime.UtcNow;

        if (user.CustomerId is not null)
        {
            var customer = await customerRepository.GetByIdTrackedAsync(user.CustomerId.Value, cancellationToken);
            if (customer is not null)
            {
                customer.FullName = name;
            }
        }
        else
        {
            var customer = await customerRepository.GetOrCreateAsync(name, user.Email, cancellationToken);
            user.CustomerId = customer.Id;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await userRepository.GetByIdAsync(model.UserId, cancellationToken)
            ?? throw new InvalidOperationException("No se pudo actualizar el perfil.");

        return BuildAuthResult(updated);
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
