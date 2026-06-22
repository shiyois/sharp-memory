using SharpMemory.Core.Business.Queries.Models;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Queries;

public sealed class MemoryQueryService(
    SegmentQueryService segments,
    FileContextQueryService fileContext,
    RelationshipQueryService relationships,
    GraphQueryService graph)
{
    public Task<IReadOnlyList<SegmentSearchResult>> Search(
        string query,
        int topN,
        string? project,
        string? repository,
        SegmentKind? kind,
        CancellationToken cancellationToken = default) =>
        segments.Search(query, topN, project, repository, kind, cancellationToken);

    public Task<IReadOnlyList<SnapshotRepositoryInfo>> ListRepositories(
        CancellationToken cancellationToken = default) =>
        segments.ListRepositories(cancellationToken);

    public Task<MemorySegment?> GetSegment(
        string stableKey,
        CancellationToken cancellationToken = default) =>
        segments.GetSegment(stableKey, cancellationToken);

    public Task<FileContext> GetFileContext(
        string repoId,
        string filePath,
        CancellationToken cancellationToken = default) =>
        fileContext.GetFileContext(repoId, filePath, cancellationToken);

    public Task<IReadOnlyList<MemoryRelationship>> GetRelationships(
        string? from = null,
        string? to = null,
        RelationshipType? type = null,
        CancellationToken cancellationToken = default) =>
        relationships.GetRelationships(from, to, type, cancellationToken);

    public Task<IReadOnlyList<MemorySegment>> FindCallers(
        string methodName,
        CancellationToken cancellationToken = default) =>
        relationships.FindCallers(methodName, cancellationToken);

    public Task<IReadOnlyList<MemorySegment>> FindImplementations(
        string interfaceName,
        CancellationToken cancellationToken = default) =>
        relationships.FindImplementations(interfaceName, cancellationToken);

    public Task<GraphNeighbors> GetAffected(
        string segmentId,
        int maxDepth,
        CancellationToken cancellationToken = default) =>
        graph.GetAffected(segmentId, maxDepth, cancellationToken);

    public Task<GraphNeighbors> GetNeighbors(
        string segmentId,
        int maxDepth,
        CancellationToken cancellationToken = default) =>
        graph.GetNeighbors(segmentId, maxDepth, cancellationToken);
}
