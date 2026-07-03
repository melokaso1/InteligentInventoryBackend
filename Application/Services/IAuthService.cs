using Application.Models;

namespace Application.Services;

public interface IAuthService
{
    Task<AuthResultModel> LoginAsync(LoginModel model, CancellationToken cancellationToken = default);
    Task<AuthResultModel> RegisterAsync(RegisterModel model, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordModel model, CancellationToken cancellationToken = default);
}
