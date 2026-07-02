using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Text;

using Application.Abstractions;

using Domain.Entities;

using Microsoft.Extensions.Configuration;

using Microsoft.IdentityModel.Tokens;



namespace Infrastructure.Security;



public sealed class JwtTokenService(IConfiguration configuration) : ITokenService

{

    public string GenerateToken(User user)

    {

        var secret = configuration["Jwt:Secret"]

            ?? throw new InvalidOperationException("JWT secret is not configured.");

        var issuer = configuration["Jwt:Issuer"] ?? "ElPlonsazo";

        var audience = configuration["Jwt:Audience"] ?? "ElPlonsazoApp";

        var expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var minutes) ? minutes : 480;



        var roleName = user.Role?.Name ?? "Cliente";

        var claims = new List<Claim>

        {

            new(ClaimTypes.NameIdentifier, user.Id.ToString()),

            new(ClaimTypes.Email, user.Email),

            new(ClaimTypes.Name, user.FullName),

            new(ClaimTypes.Role, roleName),

        };



        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);



        var token = new JwtSecurityToken(

            issuer,

            audience,

            claims,

            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),

            signingCredentials: credentials);



        return new JwtSecurityTokenHandler().WriteToken(token);

    }

}

