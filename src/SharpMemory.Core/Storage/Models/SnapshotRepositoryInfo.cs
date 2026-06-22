namespace SharpMemory.Core.Storage.Models;

public sealed record SnapshotRepositoryInfo
{
    public string SnapshotId { get; init; } = string.Empty;

    public string RepoId { get; init; } = string.Empty;

    public string RepoName { get; init; } = string.Empty;

    public string RepoPath { get; init; } = string.Empty;

    public string? CommitSha { get; init; }

    public DateTimeOffset IndexedAt { get; init; } = DateTimeOffset.UtcNow;

    public int FileCount { get; init; }

    public int SegmentCount { get; init; }
}
