using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.HostedServices;

/// <summary>
/// Dev helper: spawns the FastAPI chatbot (uvicorn) when <c>Chatbot:AutoStart</c> is enabled.
/// Skips startup when port 8000 is already in use so manual <c>python run.py</c> keeps working.
/// </summary>
public sealed class ChatbotHostedService(
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<ChatbotHostedService> logger) : IHostedService
{
    private Process? _process;
    private CancellationTokenSource? _outputPumpCts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Chatbot:AutoStart", false))
        {
            logger.LogDebug("Chatbot:AutoStart desactivado; no se iniciará FastAPI.");
            return Task.CompletedTask;
        }

        var port = configuration.GetValue("Chatbot:Port", 8000);
        if (!IsPortAvailable(port))
        {
            logger.LogInformation(
                "Puerto {Port} ya en uso; se asume que el chatbot FastAPI ya está en ejecución (p. ej. python run.py).",
                port);
            return Task.CompletedTask;
        }

        var projectPath = ResolveProjectPath();
        if (!Directory.Exists(projectPath))
        {
            logger.LogWarning(
                "No se encontró LLMChatBot en {Path}. Omite AutoStart del chatbot.",
                projectPath);
            return Task.CompletedTask;
        }

        var python = ResolvePythonExecutable();
        if (python is null)
        {
            logger.LogWarning(
                "No se encontró Python en PATH. Inicia el chatbot manualmente: cd LLMChatBot && python run.py");
            return Task.CompletedTask;
        }

        var host = configuration["Chatbot:Host"] ?? "127.0.0.1";

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = python,
                Arguments = $"-m uvicorn app.main:app --host {host} --port {port}",
                WorkingDirectory = projectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            startInfo.Environment["PYTHONUTF8"] = "1";

            _process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            _process.Exited += (_, _) =>
            {
                if (_process.ExitCode != 0)
                {
                    logger.LogWarning("Proceso del chatbot terminó con código {ExitCode}", _process.ExitCode);
                }
            };

            if (!_process.Start())
            {
                logger.LogWarning("No se pudo iniciar el proceso del chatbot FastAPI.");
                _process.Dispose();
                _process = null;
                return Task.CompletedTask;
            }

            _outputPumpCts = new CancellationTokenSource();
            PumpStream(_process.StandardOutput, LogLevel.Information, _outputPumpCts.Token);
            PumpStream(_process.StandardError, LogLevel.Warning, _outputPumpCts.Token);

            logger.LogInformation(
                "Chatbot FastAPI iniciado ({Python}) en http://{Host}:{Port} desde {Path}",
                python,
                host,
                port,
                projectPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo iniciar el chatbot FastAPI.");
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _outputPumpCts?.Cancel();
        _outputPumpCts?.Dispose();
        _outputPumpCts = null;

        if (_process is not { HasExited: false })
        {
            DisposeProcess();
            return;
        }

        logger.LogInformation("Deteniendo chatbot FastAPI (pid {Pid})...", _process.Id);

        try
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Error al detener el proceso del chatbot.");
        }
        finally
        {
            DisposeProcess();
        }
    }

    private string ResolveProjectPath()
    {
        var configured = configuration["Chatbot:ProjectPath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        return Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "..", "..", "LLMChatBot"));
    }

    private string? ResolvePythonExecutable()
    {
        var configured = configuration["Chatbot:PythonExecutable"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (Path.IsPathRooted(configured) && !File.Exists(configured))
            {
                logger.LogWarning("Chatbot:PythonExecutable no existe: {Path}", configured);
                return null;
            }

            return CanRunPython(configured) ? configured : null;
        }

        var candidates = OperatingSystem.IsWindows()
            ? new[] { "python", "py", "python3" }
            : new[] { "python3", "python" };

        foreach (var candidate in candidates)
        {
            if (CanRunPython(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool CanRunPython(string executable)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
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

    private void PumpStream(StreamReader reader, LogLevel level, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line is null)
                    {
                        break;
                    }

                    logger.Log(level, "[chatbot] {Line}", line);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Fin de lectura de salida del chatbot.");
            }
        }, cancellationToken);
    }

    private void DisposeProcess()
    {
        if (_process is null)
        {
            return;
        }

        _process.Dispose();
        _process = null;
    }
}
