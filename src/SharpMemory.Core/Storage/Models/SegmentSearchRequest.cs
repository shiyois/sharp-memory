using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Storage.Models;

public sealed record SegmentSearchRequest
{
    public string Query { get; init; } = string.Empty;

    public int TopN { get; init; } = 10;

    public string? RepoId { get; init; }

    public string? ProjectName { get; init; }

    public SegmentKind? Kind { get; init; }
}
