using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Queries;

public sealed class SegmentQueryService(
    IActiveSnapshotProvider activeSnapshotProvider,
    ISegmentStore segmentStore,
    ISnapshotStatusStore statusStore)
{
    public async Task<IReadOnlyList<SegmentSearchResult>> Search(
        string query,
        int topN,
        string? project,
        string? repository,
        SegmentKind? kind,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        return await segmentStore.Search(
            snapshot.SnapshotId,
            new SegmentSearchRequest
            {
                Query = query,
                TopN = topN,
                ProjectName = project,
                RepoId = repository,
                Kind = kind,
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<SnapshotRepositoryInfo>> ListRepositories(
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        return await statusStore.GetRepositories(snapshot.SnapshotId, cancellationToken);
    }

    public async Task<MemorySegment?> GetSegment(
        string stableKey,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        return await segmentStore.GetByStableKey(snapshot.SnapshotId, stableKey, cancellationToken);
    }

    private async Task<SnapshotMetadata> RequireActiveSnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await activeSnapshotProvider.GetActive(cancellationToken);
        return snapshot ?? throw new InvalidOperationException("No active Ready snapshot is available.");
    }
}
