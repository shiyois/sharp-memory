using SharpMemory.App.Application;
using SharpMemory.App.Infrastructure.Settings;

if (args.Length == 0 || IsHelp(args[0]))
{
    WriteUsage();
    return args.Length == 0 ? 2 : 0;
}

if (args[0].Equals("init", StringComparison.OrdinalIgnoreCase))
{
    return InitializeSettings();
}

if (!args[0].Equals("run", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown command '{args[0]}'.");
    WriteUsage();
    return 2;
}

if (args.Length > 1 && IsHelp(args[1]))
{
    WriteUsage();
    return 0;
}

return await SharpMemoryRuntime.Run(args[1..]);

static int InitializeSettings()
{
    var homePath = SharpMemoryPaths.ResolveHomePath();
    var settingsPath = SharpMemoryPaths.ResolveSettingsPath();
    Directory.CreateDirectory(homePath);

    if (File.Exists(settingsPath))
    {
        Console.Error.WriteLine($"Settings already exist: {settingsPath}");
        return 0;
    }

    File.WriteAllText(
        settingsPath,
        """
        {
          "repositories": []
        }

        """);

    Console.Error.WriteLine($"Created settings: {settingsPath}");
    return 0;
}

static bool IsHelp(string value) =>
    value.Equals("--help", StringComparison.OrdinalIgnoreCase)
    || value.Equals("-h", StringComparison.OrdinalIgnoreCase);

static void WriteUsage()
{
    Console.Error.WriteLine(
        """
        SharpMemory

        Usage:
          sharp-memory init
          sharp-memory run
          sharp-memory run --stdio

        Options:
          --repo <path>  Override repositories from settings.json. Can be repeated.

        Environment:
          SHARPMEMORY_HOME  Override the default ~/.sharp-memory home directory.
        """);
}
