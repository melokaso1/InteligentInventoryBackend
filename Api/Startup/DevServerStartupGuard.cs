using System.Net;
using System.Net.Sockets;

namespace Api.Startup;

/// <summary>
/// Fails fast in Development when Kestrel's HTTP port is already taken (e.g. Api.exe + dotnet run).
/// </summary>
public static class DevServerStartupGuard
{
    private const int DefaultDevHttpPort = 5151;

    public static void EnsureHttpPortsAvailable(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var ports = ResolveHttpPorts(configuration);
        foreach (var port in ports)
        {
            if (!IsPortAvailable(port))
            {
                throw new InvalidOperationException(
                    $"El puerto {port} ya está en uso. Solo puede haber una instancia de la API en desarrollo. " +
                    "Detén Visual Studio / otra terminal con dotnet run, o ejecuta: Stop-Process -Name Api -Force -ErrorAction SilentlyContinue");
            }
        }
    }

    private static IReadOnlyCollection<int> ResolveHttpPorts(IConfiguration configuration)
    {
        var urls = configuration["ASPNETCORE_URLS"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

        if (string.IsNullOrWhiteSpace(urls))
        {
            return [DefaultDevHttpPort];
        }

        var ports = new HashSet<int>();
        foreach (var segment in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(segment, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (uri.Scheme is "http" or "https")
            {
                ports.Add(uri.Port);
            }
        }

        return ports.Count > 0 ? ports : [DefaultDevHttpPort];
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
