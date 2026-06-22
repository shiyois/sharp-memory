namespace SharpMemory.Core.Storage.Models;

public sealed record SnapshotMetadata
{
    public const int CurrentSchemaVersion = 1;

    public string SnapshotId { get; init; } = string.Empty;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public SnapshotStatus Status { get; init; } = SnapshotStatus.Building;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FinishedAt { get; init; }

    public string ConfigHash { get; init; } = string.Empty;

    public int RepositoryCount { get; init; }

    public int FileCount { get; init; }

    public int SegmentCount { get; init; }

    public int RelationshipCount { get; init; }

    public string? ErrorMessage { get; init; }
}
