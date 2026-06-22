namespace SharpMemory.Core.Common.Models;

public sealed record MemorySegment
{
    /// <summary>MD5 of <see cref="StableKey"/>. Used as a compact surrogate key in storage and graph edges.</summary>
    public string SegmentId { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable deterministic key that <see cref="SegmentId"/> is derived from.
    /// Format by kind:
    /// <list type="bullet">
    ///   <item>C# symbols: <c>{repoId}:{project}:{Kind}:{fully.qualified.name}({signature})[:{partialOrdinal}]</c></item>
    ///   <item>Files: <c>file:{repoId}:{normalized-lowercase-path}</c></item>
    ///   <item>Directories: <c>directory:{repoId}:{normalized-lowercase-path}</c></item>
    /// </list>
    /// C# keys are Roslyn metadata-name based, so they survive file renames and line shifts.
    /// </summary>
    public string StableKey { get; init; } = string.Empty;

    /// <summary>Isolates segments when multiple repositories share the same index.</summary>
    public string RepoId { get; init; } = string.Empty;

    /// <summary>Type of the language or structural element.</summary>
    public SegmentKind Kind { get; init; }

    /// <summary>Short unqualified name. Not unique — uniqueness is guaranteed by <see cref="StableKey"/>.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Immediate parent name: namespace for a class, class for a method, file title for a markdown section.
    /// Used when building <see cref="EmbeddingText"/> and displaying search results.
    /// </summary>
    public string ContainerName { get; init; } = string.Empty;

    /// <summary>
    /// Owning .csproj name. Disambiguates symbols with the same name across projects in the same repo.
    /// Empty for markdown and directory segments.
    /// </summary>
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>
    /// Normalized repo-relative file path (unix slashes, lowercase).
    /// For display and navigation only — not part of <see cref="StableKey"/> for C# symbols,
    /// so renaming a file does not invalidate the key.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>First line in the file, 1-based. Navigation only, not used for identity.</summary>
    public int StartLine { get; init; }

    /// <summary>Last line in the file, 1-based, inclusive.</summary>
    public int EndLine { get; init; }

    /// <summary>
    /// Hash of the file content at index time. The incremental indexer uses this to skip
    /// files that have not changed since the last run.
    /// </summary>
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>
    /// Kind-specific extra data. Examples: <c>status</c> / <c>isSuperseded</c> for ADR,
    /// <c>partialOrdinal</c> for partial class declarations spread across multiple files.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}
