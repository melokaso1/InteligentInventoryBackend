using System.Security.Claims;
using Api.Dtos;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private static object ErrorPayload(string message) => new { message };

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await authService.LoginAsync(
                new LoginModel { Email = request.Email, Password = request.Password },
                cancellationToken);

            return Ok(ToDto(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            // Keep error payload consistent for frontend parsing.
            return Unauthorized(ErrorPayload(ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await authService.RegisterAsync(
                new RegisterModel
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = request.Password,
                },
                cancellationToken);

            return Ok(ToDto(result));
        }
        catch (InvalidOperationException ex)
        {
            if (IsWeakPasswordError(ex.Message))
            {
                return BadRequest(ErrorPayload(ex.Message));
            }

            // Keep error payload consistent for frontend parsing.
            return Conflict(ErrorPayload(ex.Message));
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        try
        {
            await authService.ChangePasswordAsync(
                new ChangePasswordModel
                {
                    UserId = id,
                    CurrentPassword = request.CurrentPassword,
                    NewPassword = request.NewPassword,
                },
                cancellationToken);

            return Ok(new { message = "Contraseña actualizada correctamente." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ErrorPayload(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ErrorPayload(ex.Message));
        }
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<AuthResponseDto>> UpdateProfile(
        [FromBody] UpdateProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        try
        {
            var result = await authService.UpdateProfileAsync(
                new UpdateProfileModel
                {
                    UserId = id,
                    Name = request.Name,
                },
                cancellationToken);

            return Ok(ToDto(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ErrorPayload(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ErrorPayload(ex.Message));
        }
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<AuthUserDto> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;
        var role = User.IsInRole("Admin") ? "admin" : "cliente";

        if (userId is null || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        return Ok(new AuthUserDto
        {
            Id = id,
            Name = name,
            Email = email,
            Role = role,
        });
    }

    private static AuthResponseDto ToDto(AuthResultModel result) => new()
    {
        Token = result.Token,
        User = new AuthUserDto
        {
            Id = result.User.Id,
            Name = result.User.Name,
            Email = result.User.Email,
            Role = result.User.Role,
        },
    };

    private static bool IsWeakPasswordError(string message) =>
        message.StartsWith("Longitud insuficiente", StringComparison.Ordinal)
        || message.StartsWith("Falta mayúscula", StringComparison.Ordinal)
        || message.StartsWith("Falta minúscula", StringComparison.Ordinal)
        || message.StartsWith("Falta número", StringComparison.Ordinal)
        || message.StartsWith("Falta símbolo", StringComparison.Ordinal);
}
