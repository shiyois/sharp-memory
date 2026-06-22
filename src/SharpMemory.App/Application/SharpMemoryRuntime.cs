using System.Text.Json.Serialization;
using SharpMemory.App.Http;
using SharpMemory.App.Infrastructure.DI;
using SharpMemory.App.Infrastructure.Settings;

namespace SharpMemory.App.Application;

public static class SharpMemoryRuntime
{
    public static async Task<int> Run(string[] args, CancellationToken cancellationToken = default)
    {
        var runtimeOptions = SettingsParser.Parse(args);

        if (runtimeOptions.UseStdio)
        {
            var hostBuilder = Host.CreateApplicationBuilder(args);
            hostBuilder.Logging.ClearProviders();
            hostBuilder.Services
                .AddSharpMemory(runtimeOptions)
                .AddSharpMemoryMcpStdio();

            await hostBuilder.Build().RunAsync(cancellationToken);
            return 0;
        }

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services
            .AddSharpMemory(runtimeOptions)
            .AddSharpMemoryMcpHttp();

        var app = builder.Build();

        app.MapMemoryApi();
        app.MapMcp();

        await app.RunAsync(cancellationToken);
        return 0;
    }
}
