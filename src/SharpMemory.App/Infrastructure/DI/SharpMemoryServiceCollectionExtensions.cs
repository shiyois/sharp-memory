using SharpMemory.App.Application.Snapshots;
using SharpMemory.Core.Business.Queries;
using SharpMemory.Core.Business.Repositories;
using SharpMemory.Core.Business.Segments;
using SharpMemory.Core.Business.Segments.Extraction;
using SharpMemory.Core.Business.Segments.Relationships;
using SharpMemory.Core.Business.Snapshots;
using SharpMemory.Core.Business.Snapshots.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Sqlite;
using SharpMemoryPaths = SharpMemory.App.Infrastructure.Settings.SharpMemoryPaths;
using SharpMemorySettings = SharpMemory.App.Infrastructure.Settings.Settings;

namespace SharpMemory.App.Infrastructure.DI;

public static class SharpMemoryServiceCollectionExtensions
{
    public static IServiceCollection AddSharpMemory(
        this IServiceCollection services,
        SharpMemorySettings runtimeOptions)
    {
        services.AddSingleton(runtimeOptions);
        services.AddSingleton(
            new MemorySnapshotOptions
            {
                RepositoryPaths = runtimeOptions.RepositoryPaths,
            });
        services.AddSingleton(
            new SqliteStorageOptions
            {
                StorageRoot = runtimeOptions.StorageRoot,
                DatabasePath = Path.Combine(runtimeOptions.StorageRoot, SharpMemoryPaths.DatabaseFileName),
            });
        services.AddSingleton<SqliteConnectionFactory>();

        services.AddSingleton<ISnapshotWriter, SqliteSnapshotWriter>();
        services.AddSingleton<ISnapshotPublisher, SqliteSnapshotPublisher>();
        services.AddSingleton<IActiveSnapshotProvider, SqliteActiveSnapshotProvider>();
        services.AddSingleton<ISegmentStore, SqliteSegmentStore>();
        services.AddSingleton<IRelationshipStore, SqliteRelationshipStore>();
        services.AddSingleton<ISnapshotStatusStore, SqliteSnapshotStatusStore>();

        services.AddSingleton<RepositoryScanner>();
        services.AddSingleton<IReadOnlyList<ISegmentExtractor>>(
            [
                new CSharpSegmentExtractor(),
                new DotNetProjectSegmentExtractor(),
                new SolutionSegmentExtractor(),
            ]);
        services.AddSingleton<FileSegmenter>();
        services.AddSingleton<SegmentsCreator>();
        services.AddSingleton<SegmentRelationshipBuilder>();
        services.AddSingleton<RepositorySnapshotBuilder>();
        services.AddSingleton<SnapshotLifecycleService>();
        services.AddSingleton<MemorySnapshotBuilder>();
        services.AddSingleton<SnapshotRefreshCoordinator>();
        services.AddSingleton<SegmentQueryService>();
        services.AddSingleton<FileContextQueryService>();
        services.AddSingleton<RelationshipQueryService>();
        services.AddSingleton<GraphQueryService>();
        services.AddSingleton<MemoryQueryService>();
        services.AddHostedService<SnapshotStartupService>();

        return services;
    }
}
