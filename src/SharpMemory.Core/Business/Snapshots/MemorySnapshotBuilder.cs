using Microsoft.Extensions.Logging;
using SharpMemory.Core.Business.Repositories;
using SharpMemory.Core.Business.Repositories.Models;
using SharpMemory.Core.Business.Segments.Relationships;
using SharpMemory.Core.Business.Snapshots.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Snapshots;

public sealed class MemorySnapshotBuilder(
    RepositorySnapshotBuilder repositoryBuilder,
    SegmentRelationshipBuilder relationshipBuilder,
    SnapshotLifecycleService snapshotLifecycle,
    ISnapshotWriter snapshotWriter,
    MemorySnapshotOptions options,
    ILogger<MemorySnapshotBuilder> logger)
{
    public async Task<SnapshotMetadata> BuildSnapshot(CancellationToken cancellationToken = default)
    {
        var repoPaths = ResolveRepositoryPaths();
        var metadata = snapshotLifecycle.CreateBuildingSnapshot(ComputeConfigHash(repoPaths));
        await snapshotLifecycle.Begin(metadata, cancellationToken);

        try
        {
            var repositories = await BuildRepositorySnapshots(
                metadata.SnapshotId,
                repoPaths,
                cancellationToken);
            var allSegments = repositories.SelectMany(static r => r.Segments).ToList();
            var relationships = relationshipBuilder.Build(allSegments);
            await snapshotWriter.WriteRelationships(
                metadata.SnapshotId,
                relationships,
                cancellationToken);

            var readySnapshot = await snapshotLifecycle.Publish(
                metadata,
                repoPaths.Count,
                allSegments,
                relationships.Count,
                cancellationToken);

            logger.LogInformation(
                "Published snapshot {SnapshotId}: {SegmentCount} segments, {RelationshipCount} relationships",
                metadata.SnapshotId,
                allSegments.Count,
                relationships.Count);

            return readySnapshot;
        }
        catch (Exception ex)
        {
            await snapshotLifecycle.MarkFailed(
                metadata.SnapshotId,
                ex.Message,
                CancellationToken.None);
            throw;
        }
    }

    private async Task<IReadOnlyList<IndexedRepository>> BuildRepositorySnapshots(
        string snapshotId,
        IReadOnlyList<string> repoPaths,
        CancellationToken cancellationToken)
    {
        var repositories = new List<IndexedRepository>();
        foreach (var repoPath in repoPaths)
        {
            var repository = await repositoryBuilder.BuildSnapshot(
                snapshotId,
                repoPath,
                cancellationToken);
            repositories.Add(repository);
        }

        return repositories;
    }

    private IReadOnlyList<string> ResolveRepositoryPaths() =>
        options.RepositoryPaths;

    private static string ComputeConfigHash(IReadOnlyList<string> repoPaths) =>
        string.Join('|', repoPaths.Select(static path => Path.GetFullPath(path)));
}
