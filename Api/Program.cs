using Api.Middleware;
using Api.Startup;
using Application;
using Infrastructure;
using Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

DotEnvLoader.Load(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT secret is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ElPlonsazo";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ElPlonsazoApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(IsAllowedCorsOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseGlobalExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    DevServerStartupGuard.EnsureHttpPortsAvailable(app.Configuration, app.Environment);
}

try
{
    await DatabaseSeeder.SeedAsync(app.Services);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    startupLogger.LogCritical(
        ex,
        "Falló la migración o el seed de la base de datos. Verifica PostgreSQL (docker compose up -d) y la cadena en appsettings.Development.json.");
    throw;
}

app.Run();

static bool IsAllowedCorsOrigin(string origin)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    if (uri.Host is "localhost" or "127.0.0.1")
    {
        return true;
    }

    return uri.Host.EndsWith(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase)
        || uri.Host.EndsWith(".ngrok.io", StringComparison.OrdinalIgnoreCase)
        || uri.Host.EndsWith(".ngrok.app", StringComparison.OrdinalIgnoreCase);
}
