using Microsoft.Data.Sqlite;

namespace SharpMemory.Core.Storage.Sqlite;

public sealed class SqliteConnectionFactory(SqliteStorageOptions options)
{
    public SqliteConnection Open()
    {
        var databasePath = options.ResolveDatabasePath();
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;
            """;
        pragma.ExecuteNonQuery();

        SqliteSchema.EnsureCreated(connection);
        return connection;
    }
}
