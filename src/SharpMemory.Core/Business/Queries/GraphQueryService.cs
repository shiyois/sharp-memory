using SharpMemory.Core.Business.Queries.Models;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Queries;

public sealed class GraphQueryService(
    IActiveSnapshotProvider activeSnapshotProvider,
    ISegmentStore segmentStore,
    IRelationshipStore relationshipStore)
{
    public async Task<GraphNeighbors> GetAffected(
        string segmentId,
        int maxDepth,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var frontier = new Queue<(string SegmentId, int Depth)>();
        var edges = new List<MemoryRelationship>();
        var nodes = new List<MemorySegment>();

        frontier.Enqueue((segmentId, 0));
        visited.Add(segmentId);

        while (frontier.Count > 0)
        {
            var (current, depth) = frontier.Dequeue();
            var node = await segmentStore.GetById(snapshot.SnapshotId, current, cancellationToken);
            if (node is not null)
            {
                nodes.Add(node);
            }

            if (depth >= maxDepth)
            {
                continue;
            }

            var incoming = await relationshipStore.GetIncoming(
                snapshot.SnapshotId,
                current,
                cancellationToken: cancellationToken);

            foreach (var edge in incoming)
            {
                edges.Add(edge);
                if (visited.Add(edge.FromSegmentId))
                {
                    frontier.Enqueue((edge.FromSegmentId, depth + 1));
                }
            }
        }

        return new GraphNeighbors
        {
            Nodes = nodes.DistinctBy(static s => s.SegmentId).ToList(),
            Edges = edges.DistinctBy(static e => e.RelationshipId).ToList(),
        };
    }

    public async Task<GraphNeighbors> GetNeighbors(
        string segmentId,
        int maxDepth,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await RequireActiveSnapshot(cancellationToken);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var frontier = new Queue<(string SegmentId, int Depth)>();
        var edges = new List<MemoryRelationship>();
        var nodes = new List<MemorySegment>();

        frontier.Enqueue((segmentId, 0));
        visited.Add(segmentId);

        while (frontier.Count > 0)
        {
            var (current, depth) = frontier.Dequeue();
            var node = await segmentStore.GetById(snapshot.SnapshotId, current, cancellationToken);
            if (node is not null)
            {
                nodes.Add(node);
            }

            if (depth >= maxDepth)
            {
                continue;
            }

            var outgoing = await relationshipStore.GetOutgoing(
                snapshot.SnapshotId,
                current,
                cancellationToken: cancellationToken);
            var incoming = await relationshipStore.GetIncoming(
                snapshot.SnapshotId,
                current,
                cancellationToken: cancellationToken);

            foreach (var edge in outgoing.Concat(incoming))
            {
                edges.Add(edge);
                var next = edge.FromSegmentId == current ? edge.ToSegmentId : edge.FromSegmentId;
                if (visited.Add(next))
                {
                    frontier.Enqueue((next, depth + 1));
                }
            }
        }

        return new GraphNeighbors
        {
            Nodes = nodes.DistinctBy(static s => s.SegmentId).ToList(),
            Edges = edges.DistinctBy(static e => e.RelationshipId).ToList(),
        };
    }

    private async Task<SnapshotMetadata> RequireActiveSnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await activeSnapshotProvider.GetActive(cancellationToken);
        return snapshot ?? throw new InvalidOperationException("No active Ready snapshot is available.");
    }
}
