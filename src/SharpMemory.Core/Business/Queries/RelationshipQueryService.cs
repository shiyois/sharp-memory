using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Queries;

public sealed class RelationshipQueryService(
    IActiveSnapshotProvider activeSnapshotProvider,
    ISegmentStore segmentStore,
    IRelationshipStore relationshipStore)
{
    public async Task<IReadOnlyList<MemoryRelationship>> GetRelationships(
        string? from = null,
        string? to = null,
        RelationshipType? type = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        return await relationshipStore.GetAll(snapshot.SnapshotId, from, to, type, cancellationToken);
    }

    public async Task<IReadOnlyList<MemorySegment>> FindCallers(
        string methodName,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        var methods = await segmentStore.Search(
            snapshot.SnapshotId,
            new SegmentSearchRequest
            {
                Query = methodName,
                TopN = 25,
                Kind = SegmentKind.Method,
            },
            cancellationToken);

        var callers = new List<MemorySegment>();
        foreach (var method in methods.Select(static m => m.Segment))
        {
            var incoming = await relationshipStore.GetIncoming(
                snapshot.SnapshotId,
                method.SegmentId,
                RelationshipType.Calls,
                cancellationToken);

            foreach (var relationship in incoming)
            {
                var caller = await segmentStore.GetById(
                    snapshot.SnapshotId,
                    relationship.FromSegmentId,
                    cancellationToken);
                if (caller is not null)
                {
                    callers.Add(caller);
                }
            }
        }

        return callers.DistinctBy(static s => s.SegmentId).ToList();
    }

    public async Task<IReadOnlyList<MemorySegment>> FindImplementations(
        string interfaceName,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        var interfaces = await segmentStore.Search(
            snapshot.SnapshotId,
            new SegmentSearchRequest
            {
                Query = interfaceName,
                TopN = 25,
                Kind = SegmentKind.Interface,
            },
            cancellationToken);

        var implementations = new List<MemorySegment>();
        foreach (var iface in interfaces.Select(static m => m.Segment))
        {
            var incoming = await relationshipStore.GetIncoming(
                snapshot.SnapshotId,
                iface.SegmentId,
                RelationshipType.Implements,
                cancellationToken);

            foreach (var relationship in incoming)
            {
                var implementation = await segmentStore.GetById(
                    snapshot.SnapshotId,
                    relationship.FromSegmentId,
                    cancellationToken);
                if (implementation is not null)
                {
                    implementations.Add(implementation);
                }
            }
        }

        return implementations.DistinctBy(static s => s.SegmentId).ToList();
    }

    private async Task<SnapshotMetadata> RequireActiveSnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await activeSnapshotProvider.GetActive(cancellationToken);
        return snapshot ?? throw new InvalidOperationException("No active Ready snapshot is available.");
    }
}
