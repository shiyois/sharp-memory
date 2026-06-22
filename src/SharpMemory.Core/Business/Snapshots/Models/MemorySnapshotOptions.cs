namespace SharpMemory.Core.Business.Snapshots.Models;

public sealed record MemorySnapshotOptions
{
    public string[] RepositoryPaths { get; init; } = [];
}
