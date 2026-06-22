using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Business.Repositories.Models;

public sealed record IndexedRepository
{
    public string RepoId { get; init; } = string.Empty;

    public string RepoName { get; init; } = string.Empty;

    public string RootPath { get; init; } = string.Empty;

    public IReadOnlyList<MemorySegment> Segments { get; init; } = [];
}
