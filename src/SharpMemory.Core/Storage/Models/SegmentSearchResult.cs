using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Storage.Models;

public sealed record SegmentSearchResult
{
    public MemorySegment Segment { get; init; } = new();

    public double Score { get; init; }

    public string Preview { get; init; } = string.Empty;
}
