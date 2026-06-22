using FluentAssertions;
using SharpMemory.Core.Business.Queries;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Business.Queries;

[TestFixture]
public sealed class QueryServiceTests
{
    [Test]
    public async Task Search_WhenNoActiveSnapshot_ThrowsClearError()
    {
        var service = new SegmentQueryService(
            new FakeActiveSnapshotProvider(null),
            new FakeSegmentStore([]),
            new FakeSnapshotStatusStore());

        var act = () => service.Search("Worker", 10, null, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No active Ready snapshot is available.");
    }

    [Test]
    public async Task GetFileContext_ReturnsFileSegmentsAndOutgoingRelationships()
    {
        var snapshot = ReadySnapshot();
        var fileSegment = SegmentFactory.Create(SegmentKind.Class, "Worker");
        var target = SegmentFactory.Create(SegmentKind.Method, "Run");
        var relationship = Relationship(fileSegment, target, RelationshipType.Contains);
        var segmentStore = new FakeSegmentStore([fileSegment, target]);
        var relationshipStore = new FakeRelationshipStore([relationship]);
        var service = new FileContextQueryService(
            new FakeActiveSnapshotProvider(snapshot),
            segmentStore,
            relationshipStore);

        var context = await service.GetFileContext(fileSegment.RepoId, fileSegment.FilePath.Replace('/', '\\'));

        context.Segments.Should().ContainSingle(s => s.SegmentId == fileSegment.SegmentId);
        context.Relationships.Should().ContainSingle(r => r.RelationshipId == relationship.RelationshipId);
    }

    [Test]
    public async Task GetNeighbors_FollowsIncomingAndOutgoingEdgesUpToDepth()
    {
        var snapshot = ReadySnapshot();
        var root = SegmentFactory.Create(SegmentKind.Method, "Root");
        var caller = SegmentFactory.Create(SegmentKind.Method, "Caller");
        var callee = SegmentFactory.Create(SegmentKind.Method, "Callee");
        var farNode = SegmentFactory.Create(SegmentKind.Method, "FarNode");
        var incoming = Relationship(caller, root, RelationshipType.Calls);
        var outgoing = Relationship(root, callee, RelationshipType.Calls);
        var far = Relationship(callee, farNode, RelationshipType.Calls);
        var service = new GraphQueryService(
            new FakeActiveSnapshotProvider(snapshot),
            new FakeSegmentStore([root, caller, callee, farNode]),
            new FakeRelationshipStore([incoming, outgoing, far]));

        var neighbors = await service.GetNeighbors(root.SegmentId, maxDepth: 1);

        neighbors.Nodes.Should().BeEquivalentTo(
            [root, caller, callee],
            options => options.ComparingByMembers<MemorySegment>());
        neighbors.Edges.Should().BeEquivalentTo(
            [incoming, outgoing],
            options => options.ComparingByMembers<MemoryRelationship>());
    }

    [Test]
    public async Task FindCallers_ReturnsDistinctCallerSegments()
    {
        var snapshot = ReadySnapshot();
        var target = SegmentFactory.Create(SegmentKind.Method, "Target");
        var caller = SegmentFactory.Create(SegmentKind.Method, "Caller");
        var relationship = Relationship(caller, target, RelationshipType.Calls);
        var service = new RelationshipQueryService(
            new FakeActiveSnapshotProvider(snapshot),
            new FakeSegmentStore([target, caller]),
            new FakeRelationshipStore([relationship, relationship]));

        var callers = await service.FindCallers("Target");

        callers.Should().ContainSingle(s => s.SegmentId == caller.SegmentId);
    }

    private static SnapshotMetadata ReadySnapshot() =>
        new()
        {
            SnapshotId = "snapshot",
            Status = SnapshotStatus.Ready,
        };

    private static MemoryRelationship Relationship(
        MemorySegment from,
        MemorySegment to,
        RelationshipType type) =>
        new()
        {
            RelationshipId = $"{type}:{from.SegmentId}:{to.SegmentId}",
            FromSegmentId = from.SegmentId,
            ToSegmentId = to.SegmentId,
            FromStableKey = from.StableKey,
            ToStableKey = to.StableKey,
            Type = type,
        };

    private sealed class FakeActiveSnapshotProvider(SnapshotMetadata? snapshot) : IActiveSnapshotProvider
    {
        public Task<SnapshotMetadata?> GetActive(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);
    }

    private sealed class FakeSegmentStore(IReadOnlyList<MemorySegment> segments) : ISegmentStore
    {
        public Task<IReadOnlyList<SegmentSearchResult>> Search(
            string snapshotId,
            SegmentSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var results = segments
                .Where(segment => !request.Kind.HasValue || segment.Kind == request.Kind.Value)
                .Where(segment => string.IsNullOrWhiteSpace(request.Query)
                    || segment.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                .Select(segment => new SegmentSearchResult { Segment = segment })
                .ToList();

            return Task.FromResult<IReadOnlyList<SegmentSearchResult>>(results);
        }

        public Task<MemorySegment?> GetById(
            string snapshotId,
            string segmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(segments.FirstOrDefault(segment => segment.SegmentId == segmentId));

        public Task<MemorySegment?> GetByStableKey(
            string snapshotId,
            string stableKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(segments.FirstOrDefault(segment => segment.StableKey == stableKey));

        public Task<IReadOnlyList<MemorySegment>> GetByFile(
            string snapshotId,
            string repoId,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var fileSegments = segments
                .Where(segment => segment.RepoId == repoId && segment.FilePath == filePath)
                .ToList();
            return Task.FromResult<IReadOnlyList<MemorySegment>>(fileSegments);
        }
    }

    private sealed class FakeRelationshipStore(IReadOnlyList<MemoryRelationship> relationships) : IRelationshipStore
    {
        public Task<IReadOnlyList<MemoryRelationship>> GetAll(
            string snapshotId,
            string? from = null,
            string? to = null,
            RelationshipType? type = null,
            CancellationToken cancellationToken = default)
        {
            var results = relationships
                .Where(r => from is null || r.FromSegmentId == from || r.FromStableKey.Contains(from, StringComparison.Ordinal))
                .Where(r => to is null || r.ToSegmentId == to || r.ToStableKey.Contains(to, StringComparison.Ordinal))
                .Where(r => !type.HasValue || r.Type == type.Value)
                .ToList();

            return Task.FromResult<IReadOnlyList<MemoryRelationship>>(results);
        }

        public Task<IReadOnlyList<MemoryRelationship>> GetOutgoing(
            string snapshotId,
            string segmentId,
            RelationshipType? type = null,
            CancellationToken cancellationToken = default)
        {
            var results = relationships
                .Where(r => r.FromSegmentId == segmentId)
                .Where(r => !type.HasValue || r.Type == type.Value)
                .ToList();
            return Task.FromResult<IReadOnlyList<MemoryRelationship>>(results);
        }

        public Task<IReadOnlyList<MemoryRelationship>> GetIncoming(
            string snapshotId,
            string segmentId,
            RelationshipType? type = null,
            CancellationToken cancellationToken = default)
        {
            var results = relationships
                .Where(r => r.ToSegmentId == segmentId)
                .Where(r => !type.HasValue || r.Type == type.Value)
                .ToList();
            return Task.FromResult<IReadOnlyList<MemoryRelationship>>(results);
        }
    }

    private sealed class FakeSnapshotStatusStore : ISnapshotStatusStore
    {
        public Task<SnapshotMetadata?> GetSnapshot(
            string snapshotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SnapshotMetadata?>(null);

        public Task<IReadOnlyList<SnapshotRepositoryInfo>> GetRepositories(
            string snapshotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SnapshotRepositoryInfo>>([]);

        public Task<IReadOnlyList<SnapshotDiagnostic>> GetDiagnostics(
            string snapshotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SnapshotDiagnostic>>([]);
    }
}
