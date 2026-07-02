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
            // Keep error payload consistent for frontend parsing.
            return Conflict(ErrorPayload(ex.Message));
        }
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<AuthUserDto> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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
}
