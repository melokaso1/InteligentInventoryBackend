using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Middleware;

public static class GlobalExceptionMiddleware
{
    public static void UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;

                // TaskCanceledException derives from OperationCanceledException (client abort or HttpClient timeout).
                if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
                {
                    // Client aborted the request (navigation, F5, tab close). Not a server fault.
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 499;
                    }

                    return;
                }

                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                if (exception is not null)
                {
                    var root = Unwrap(exception);
                    logger.LogError(
                        exception,
                        "Unhandled exception processing {Method} {Path}. Root: {RootType}: {RootMessage}",
                        context.Request.Method,
                        context.Request.Path,
                        root.GetType().Name,
                        root.Message);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";

                var payload = new Dictionary<string, string?>
                {
                    ["error"] = "Error al procesar la solicitud.",
                };

                if (app.Environment.IsDevelopment() && exception is not null)
                {
                    payload["detail"] = GetClientDetailMessage(Unwrap(exception));
                }

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    }));
            });
        });
    }

    private static Exception Unwrap(Exception exception)
    {
        var current = exception;

        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }

    private static string GetClientDetailMessage(Exception exception) => Unwrap(exception).Message;
}
