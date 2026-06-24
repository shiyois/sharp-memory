using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Business.Segments.Validation;

namespace SharpMemory.Core.Business.Segments;

public sealed class RepositoryScanner
{
    private readonly Func<string, IEnumerable<string>> enumerateDirectories;
    private readonly Func<string, IEnumerable<string>> enumerateFiles;

    private static readonly HashSet<string> SkippedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".hg",
        ".svn",
        ".sharp-memory",
        ".vs",
        ".idea",
        "bin",
        "obj",
        "node_modules",
    };

    public RepositoryScanner()
        : this(Directory.EnumerateDirectories, Directory.EnumerateFiles)
    {
    }

    internal RepositoryScanner(
        Func<string, IEnumerable<string>> enumerateDirectories,
        Func<string, IEnumerable<string>> enumerateFiles)
    {
        this.enumerateDirectories = enumerateDirectories;
        this.enumerateFiles = enumerateFiles;
    }

    public async IAsyncEnumerable<string> Scan(string rootPath)
    {
        var gitIgnore = GitIgnoreMatcher.Load(rootPath) ?? GitIgnoreMatcher.Empty;
        var stack = new Stack<string>([rootPath]);

        while (stack.TryPop(out var directory))
        {
            foreach (var subdirectory in Enumerate(enumerateDirectories, directory))
            {
                if (SkippedDirectoryNames.Contains(Path.GetFileName(subdirectory)))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(rootPath, subdirectory).ToUnixPath();

                if (!gitIgnore.IsIgnored(relative, true))
                {
                    stack.Push(subdirectory);
                }
            }

            foreach (var filePath in Enumerate(enumerateFiles, directory))
            {
                var relative = Path.GetRelativePath(rootPath, filePath).ToUnixPath();

                if (!gitIgnore.IsIgnored(relative, false))
                {
                    yield return filePath;
                }
            }
        }

        await Task.CompletedTask;
    }

    private static IReadOnlyList<string> Enumerate(
        Func<string, IEnumerable<string>> enumerate,
        string directory)
    {
        try
        {
            return enumerate(directory).ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }
}
