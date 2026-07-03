using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.HostedServices;

public sealed class ChatRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ChatRetentionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
            var result = await chatService.CleanupExpiredAsync(cancellationToken);

            if (result.DeletedMessages > 0 || result.DeletedSessions > 0)
            {
                logger.LogInformation(
                    "Retención de chat: {DeletedMessages} mensajes y {DeletedSessions} sesiones vacías eliminados (más de 5 meses).",
                    result.DeletedMessages,
                    result.DeletedSessions);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al ejecutar la retención de mensajes de chat.");
        }
    }
}
