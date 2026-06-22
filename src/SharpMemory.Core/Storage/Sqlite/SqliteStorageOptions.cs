namespace SharpMemory.Core.Storage.Sqlite;

public sealed record SqliteStorageOptions
{
    public string StorageRoot { get; init; } = Environment.CurrentDirectory;

    public string DatabasePath { get; init; } = string.Empty;

    public string ResolveDatabasePath()
    {
        if (!string.IsNullOrWhiteSpace(DatabasePath))
        {
            return Path.GetFullPath(DatabasePath);
        }

        return Path.Combine(Path.GetFullPath(StorageRoot), ".sharp-memory", "index.db");
    }
}
