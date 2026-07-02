using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ChatbotApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Chatbot-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["Chatbot:ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            context.Result = new ObjectResult(new { message = "Integración chatbot no configurada." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
            || !string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "API key del chatbot inválida." });
        }
    }
}
