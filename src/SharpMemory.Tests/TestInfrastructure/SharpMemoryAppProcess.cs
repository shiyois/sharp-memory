using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SharpMemory.Tests.TestInfrastructure;

internal sealed class SharpMemoryAppProcess : IAsyncDisposable
{
    private readonly Process process;
    private readonly StringBuilder output = new();

    private SharpMemoryAppProcess(Process process, Uri baseAddress)
    {
        this.process = process;
        BaseAddress = baseAddress;
    }

    public Uri BaseAddress { get; }

    public static async Task<SharpMemoryAppProcess> Start(
        string workingDirectory,
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        var port = GetAvailablePort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}");
        var appDll = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "SharpMemory.App",
            "bin",
            "Debug",
            "net10.0",
            "SharpMemory.App.dll");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{appDll}\" --repo \"{repositoryPath}\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        process.StartInfo.Environment["ASPNETCORE_URLS"] = baseAddress.ToString();

        var app = new SharpMemoryAppProcess(process, baseAddress);
        process.OutputDataReceived += (_, e) => app.AppendOutput(e.Data);
        process.ErrorDataReceived += (_, e) => app.AppendOutput(e.Data);

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start SharpMemory.App process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await app.WaitUntilHealthy(cancellationToken);
        return app;
    }

    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        process.Dispose();
    }

    private async Task WaitUntilHealthy(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { BaseAddress = BaseAddress };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        while (!linked.Token.IsCancellationRequested)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"SharpMemory.App exited with code {process.ExitCode}.{Environment.NewLine}{output}");
            }

            try
            {
                using var response = await client.GetAsync("/health", linked.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
            }

            await Task.Delay(100, linked.Token);
        }

        throw new TimeoutException($"SharpMemory.App did not become healthy.{Environment.NewLine}{output}");
    }

    private void AppendOutput(string? line)
    {
        if (line is not null)
        {
            output.AppendLine(line);
        }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SharpMemory.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find SharpMemory.slnx.");
    }
}
