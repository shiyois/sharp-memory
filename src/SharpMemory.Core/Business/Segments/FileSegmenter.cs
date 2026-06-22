using Microsoft.Extensions.Logging;
using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Business.Segments.Extraction;
using SharpMemory.Core.Business.Segments.Models;
using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Business.Segments;

public sealed class FileSegmenter(
    IReadOnlyList<ISegmentExtractor> extractors,
    ILogger<FileSegmenter> logger)
{
    public async IAsyncEnumerable<MemorySegment> Segment(
        string filePath,
        string relativePath,
        string rootPath,
        string repoId)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var extractor = extractors.FirstOrDefault(extractor => extractor.CanExtract(extension));

        if (extractor is null)
        {
            logger.LogInformation("No extractor found for {FilePath}", relativePath);
            yield break;
        }

        var scannedFile = new ScannedFile
        {
            RepoId = repoId,
            FullPath = filePath,
            RelativePath = relativePath,
            Extension = extension,
            ContentHash = await filePath.ComputeHash(),
            ProjectName = ResolveProjectName(filePath, rootPath, extension),
        };

        foreach (var segment in extractor.Extract(scannedFile))
        {
            yield return segment;
        }
    }

    private static string ResolveProjectName(string filePath, string rootPath, string extension)
    {
        if (!extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var root = Path.GetFullPath(rootPath);
        var current = Directory.GetParent(Path.GetFullPath(filePath));

        while (current is not null && IsUnderRoot(current.FullName, root))
        {
            var projectPath = Directory.EnumerateFiles(current.FullName, "*.csproj")
                .Order(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (projectPath is not null)
            {
                return Path.GetFileNameWithoutExtension(projectPath);
            }

            current = current.Parent;
        }

        return string.Empty;
    }

    private static bool IsUnderRoot(string path, string rootPath)
    {
        var relative = Path.GetRelativePath(rootPath, path);
        return relative == "."
            || (!relative.StartsWith("..", StringComparison.Ordinal)
                && !Path.IsPathRooted(relative));
    }
}
