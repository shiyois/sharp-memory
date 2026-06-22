using SharpMemory.Core.Storage.Models;

namespace SharpMemory.App.Application.Snapshots.Models;

public sealed record SnapshotRefreshStatusDto
{
    public bool IsRunning { get; init; }

    public string? RunningSnapshotId { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public SnapshotMetadata? LastCompletedSnapshot { get; init; }

    public string? LastError { get; init; }
}
