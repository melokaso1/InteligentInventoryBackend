using Domain.Entities;

namespace Application.Abstractions;

public interface ITokenService
{
    string GenerateToken(User user);
}
