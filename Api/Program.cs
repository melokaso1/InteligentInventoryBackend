using Application;
using Infrastructure;
using Infrastructure.Persistence.Seed;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseCors();
app.UseAuthorization();
app.MapControllers();

await DatabaseSeeder.SeedAsync(app.Services);

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
