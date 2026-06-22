namespace SharpMemory.Core.Storage.Models;

public sealed record SnapshotDiagnostic
{
    public string SnapshotId { get; init; } = string.Empty;

    public string RepoId { get; init; } = string.Empty;

    public string? FilePath { get; init; }

    public SnapshotDiagnosticSeverity Severity { get; init; } = SnapshotDiagnosticSeverity.Info;

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
