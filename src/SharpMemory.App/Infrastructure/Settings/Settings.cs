namespace SharpMemory.App.Infrastructure.Settings;

public sealed record Settings
{
    public bool UseStdio { get; init; }

    public string StorageRoot { get; init; } = Environment.CurrentDirectory;

    public string[] RepositoryPaths { get; init; } = [];
}
