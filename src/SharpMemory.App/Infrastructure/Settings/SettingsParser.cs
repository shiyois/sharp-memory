using System.Text.Json;

namespace SharpMemory.App.Infrastructure.Settings;

public static class SettingsParser
{
    public static Settings Parse(string[] args)
    {
        var cliRepoPaths = new List<string>();
        var storageRoot = FindProjectRoot(Environment.CurrentDirectory);
        var useStdio = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--stdio", StringComparison.OrdinalIgnoreCase))
            {
                useStdio = true;
            }
            else if (args[i].Equals("--repo", StringComparison.OrdinalIgnoreCase)
                     && i + 1 < args.Length)
            {
                cliRepoPaths.Add(Path.GetFullPath(args[++i]));
            }
        }

        var repoPaths = cliRepoPaths.Count > 0
            ? cliRepoPaths
            : LoadRepositoryPathsFromSettings(storageRoot);

        if (repoPaths.Count == 0)
        {
            repoPaths.Add(storageRoot);
        }

        return new Settings
        {
            UseStdio = useStdio,
            StorageRoot = storageRoot,
            RepositoryPaths = repoPaths.ToArray(),
        };
    }

    private static string FindProjectRoot(string startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SharpMemory.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(startPath);
    }

    private static List<string> LoadRepositoryPathsFromSettings(string storageRoot)
    {
        var settingsPath = Path.Combine(storageRoot, "settings.json");
        if (!File.Exists(settingsPath))
        {
            return [];
        }

        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
        if (!document.RootElement.TryGetProperty("repositories", out var repositories)
            || repositories.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var repoPaths = new List<string>();
        foreach (var repository in repositories.EnumerateArray())
        {
            var path = repository.ValueKind switch
            {
                JsonValueKind.String => repository.GetString(),
                JsonValueKind.Object when repository.TryGetProperty("path", out var pathElement)
                                            && pathElement.ValueKind == JsonValueKind.String => pathElement.GetString(),
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(path))
            {
                repoPaths.Add(Path.GetFullPath(path));
            }
        }

        return repoPaths;
    }
}
