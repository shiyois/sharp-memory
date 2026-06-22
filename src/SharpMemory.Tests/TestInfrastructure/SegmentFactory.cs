using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Common.Models;

namespace SharpMemory.Tests.TestInfrastructure;

internal static class SegmentFactory
{
    public static MemorySegment Create(
        SegmentKind kind,
        string name,
        string? projectName = "App",
        string? repoId = "repo",
        string? containerName = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var stableKey = $"{repoId}:{projectName}:{kind}:{containerName}.{name}:{Guid.NewGuid():N}";
        return new MemorySegment
        {
            SegmentId = stableKey.ToSegmentId(),
            StableKey = stableKey,
            RepoId = repoId ?? string.Empty,
            Kind = kind,
            Name = name,
            ContainerName = containerName ?? string.Empty,
            ProjectName = projectName ?? string.Empty,
            FilePath = $"{name}.cs",
            StartLine = 1,
            EndLine = 1,
            ContentHash = "hash",
            Metadata = metadata ?? new Dictionary<string, string>(),
        };
    }
}
