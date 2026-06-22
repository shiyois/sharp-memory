namespace SharpMemory.Core.Business.Segments.Models;

public sealed record ScannedFile
{
    /// <summary>Repository the file belongs to.</summary>
    public string RepoId { get; init; } = string.Empty;

    /// <summary>Absolute path to the file on disk. Used by extractors to open a read stream.</summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>Repo-relative path, unix slashes, lowercase.</summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>File extension including the dot, e.g. <c>.cs</c>. Used to pick the right indexer.</summary>
    public string Extension { get; init; } = string.Empty;

    /// <summary>Hash of the file content. Used by the incremental indexer to skip unchanged files.</summary>
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>Owning .csproj name. Empty for files outside any project (markdown, config).</summary>
    public string ProjectName { get; init; } = string.Empty;
}
