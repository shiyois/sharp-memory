using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Snapshots;

public sealed class SnapshotLifecycleService(
    ISnapshotWriter snapshotWriter,
    ISnapshotPublisher snapshotPublisher)
{
    public SnapshotMetadata CreateBuildingSnapshot(string configHash) =>
        new()
        {
            SnapshotId = $"snapshot-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}",
            ConfigHash = configHash,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public Task Begin(SnapshotMetadata metadata, CancellationToken cancellationToken = default) =>
        snapshotWriter.Begin(metadata, cancellationToken);

    public async Task<SnapshotMetadata> Publish(
        SnapshotMetadata metadata,
        int repositoryCount,
        IReadOnlyList<MemorySegment> segments,
        int relationshipCount,
        CancellationToken cancellationToken = default)
    {
        await snapshotPublisher.Publish(metadata.SnapshotId, cancellationToken);

        return metadata with
        {
            Status = SnapshotStatus.Ready,
            FinishedAt = DateTimeOffset.UtcNow,
            RepositoryCount = repositoryCount,
            FileCount = segments.Select(static s => (s.RepoId, s.FilePath)).Distinct().Count(),
            SegmentCount = segments.Count,
            RelationshipCount = relationshipCount,
        };
    }

    public Task MarkFailed(
        string snapshotId,
        string errorMessage,
        CancellationToken cancellationToken = default) =>
        snapshotWriter.MarkFailed(snapshotId, errorMessage, cancellationToken);
}
