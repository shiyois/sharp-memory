namespace SharpMemory.App.Infrastructure.Settings;

public static class SharpMemoryPaths
{
    public const string HomeEnvironmentVariable = "SHARPMEMORY_HOME";
    public const string SettingsFileName = "settings.json";
    public const string DatabaseFileName = "sharp-memory.db";

    public static string ResolveHomePath()
    {
        var configuredHome = Environment.GetEnvironmentVariable(HomeEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredHome))
        {
            return Path.GetFullPath(configuredHome);
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            userProfile = Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetEnvironmentVariable("USERPROFILE")
                ?? Environment.CurrentDirectory;
        }

        return Path.Combine(Path.GetFullPath(userProfile), ".sharp-memory");
    }

    public static string ResolveSettingsPath() =>
        Path.Combine(ResolveHomePath(), SettingsFileName);

    public static string ResolveDatabasePath() =>
        Path.Combine(ResolveHomePath(), DatabaseFileName);
}
