using SharpMemory.App.Mcp;

namespace SharpMemory.App.Infrastructure.DI;

public static class SharpMemoryMcpServiceCollectionExtensions
{
    public static IServiceCollection AddSharpMemoryMcpHttp(this IServiceCollection services)
    {
        services
            .AddMcpServer()
            .WithHttpTransport(options => options.EnableLegacySse = true)
            .WithTools<MemoryTools>();

        return services;
    }

    public static IServiceCollection AddSharpMemoryMcpStdio(this IServiceCollection services)
    {
        services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<MemoryTools>();

        return services;
    }
}
