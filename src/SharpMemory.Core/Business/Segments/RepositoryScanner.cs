using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Business.Segments.Validation;

namespace SharpMemory.Core.Business.Segments;

public sealed class RepositoryScanner
{
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

    public async IAsyncEnumerable<string> Scan(string rootPath)
    {
        var gitIgnore = GitIgnoreMatcher.Load(rootPath) ?? GitIgnoreMatcher.Empty;
        var stack = new Stack<string>([rootPath]);

        while (stack.TryPop(out var directory))
        {
            foreach (var subdirectory in Directory.EnumerateDirectories(directory))
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

            foreach (var filePath in Directory.EnumerateFiles(directory))
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
}
