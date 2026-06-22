using System.ComponentModel;
using ModelContextProtocol.Server;
using SharpMemory.App.Application.Snapshots;
using SharpMemory.App.Application.Snapshots.Models;
using SharpMemory.Core.Business.Queries;
using SharpMemory.Core.Business.Queries.Models;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.App.Mcp;

[McpServerToolType]
public sealed class MemoryTools(MemoryQueryService queries, SnapshotRefreshCoordinator snapshotRefresh)
{
    [McpServerTool]
    [Description("Search indexed .NET memory using SQLite FTS5/BM25 keyword retrieval.")]
    public Task<IReadOnlyList<SegmentSearchResult>> MemorySearch(
        [Description("Search query")] string query,
        [Description("Maximum number of results")] int topN = 10,
        [Description("Optional project filter")] string? project = null,
        [Description("Optional repository id filter")] string? repository = null,
        CancellationToken ct = default) =>
        queries.Search(query, topN, project, repository, null, ct);

    [McpServerTool]
    [Description("List indexed repositories for the active snapshot.")]
    public Task<IReadOnlyList<SnapshotRepositoryInfo>> MemoryListRepositories(CancellationToken ct = default) =>
        queries.ListRepositories(ct);

    [McpServerTool]
    [Description("Get a segment by stable key.")]
    public Task<MemorySegment?> MemoryGetSegment(string stableKey, CancellationToken ct = default) =>
        queries.GetSegment(stableKey, ct);

    [McpServerTool]
    [Description("Return segments in a file and outgoing relationships from those segments.")]
    public Task<FileContext> MemoryFileContext(string repoId, string filePath, CancellationToken ct = default) =>
        queries.GetFileContext(repoId, filePath, ct);

    [McpServerTool]
    [Description("Return relationships from the active snapshot.")]
    public Task<IReadOnlyList<MemoryRelationship>> MemoryGetRelationships(
        string? from = null,
        string? to = null,
        RelationshipType? type = null,
        CancellationToken ct = default) =>
        queries.GetRelationships(from, to, type, ct);

    [McpServerTool]
    [Description("Find callers of methods matching the given method name.")]
    public Task<IReadOnlyList<MemorySegment>> MemoryFindCallers(
        string methodName,
        CancellationToken ct = default) =>
        queries.FindCallers(methodName, ct);

    [McpServerTool]
    [Description("Find class, record, or struct segments implementing interfaces matching the given name.")]
    public Task<IReadOnlyList<MemorySegment>> MemoryFindImplementations(
        string interfaceName,
        CancellationToken ct = default) =>
        queries.FindImplementations(interfaceName, ct);

    [McpServerTool]
    [Description("Return graph neighbors for a segment id.")]
    public Task<GraphNeighbors> MemoryGraphNeighbors(
        string segmentId,
        int maxDepth = 1,
        CancellationToken ct = default) =>
        queries.GetNeighbors(segmentId, Math.Clamp(maxDepth, 1, 3), ct);

    [McpServerTool]
    [Description("Return reverse dependency graph for a segment id: callers, implementers, containers, and dependents.")]
    public Task<GraphNeighbors> MemoryGraphAffected(
        string segmentId,
        int maxDepth = 1,
        CancellationToken ct = default) =>
        queries.GetAffected(segmentId, Math.Clamp(maxDepth, 1, 3), ct);

    [McpServerTool]
    [Description("Start a background snapshot refresh. Existing MCP queries keep using the previous ready snapshot until the new one is published.")]
    public async Task<SnapshotRefreshStatusDto> MemoryRefreshSnapshot(CancellationToken ct = default)
    {
        var (_, status) = await snapshotRefresh.StartRefreshInBackground(ct);
        return status;
    }

    [McpServerTool]
    [Description("Return current snapshot refresh status.")]
    public SnapshotRefreshStatusDto MemorySnapshotRefreshStatus() => snapshotRefresh.GetStatus();
}
