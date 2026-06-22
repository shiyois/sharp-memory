using SharpMemory.Core.Business.Queries.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Queries;

public sealed class FileContextQueryService(
    IActiveSnapshotProvider activeSnapshotProvider,
    ISegmentStore segmentStore,
    IRelationshipStore relationshipStore)
{
    public async Task<FileContext> GetFileContext(
        string repoId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        var segments = await segmentStore.GetByFile(
            snapshot.SnapshotId,
            repoId,
            filePath.Replace('\\', '/'),
            cancellationToken);

        var relationships = new List<MemoryRelationship>();
        foreach (var segment in segments)
        {
            relationships.AddRange(
                await relationshipStore.GetOutgoing(
                    snapshot.SnapshotId,
                    segment.SegmentId,
                    cancellationToken: cancellationToken));
        }

        return new FileContext
        {
            Segments = segments,
            Relationships = relationships,
        };
    }

    private async Task<SnapshotMetadata> RequireActiveSnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await activeSnapshotProvider.GetActive(cancellationToken);
        return snapshot ?? throw new InvalidOperationException("No active Ready snapshot is available.");
    }
}
