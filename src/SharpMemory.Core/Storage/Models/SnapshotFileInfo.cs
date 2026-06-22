namespace SharpMemory.Core.Storage.Models;

public sealed record SnapshotFileInfo
{
    public string SnapshotId { get; init; } = string.Empty;

    public string RepoId { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    public string ContentHash { get; init; } = string.Empty;

    public string ProjectName { get; init; } = string.Empty;

    public int SegmentCount { get; init; }
}
