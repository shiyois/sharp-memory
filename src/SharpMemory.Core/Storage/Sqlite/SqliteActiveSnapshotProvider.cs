using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Sqlite;

public sealed class SqliteActiveSnapshotProvider(SqliteConnectionFactory connectionFactory)
    : IActiveSnapshotProvider
{
    public Task<SnapshotMetadata?> GetActive(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.snapshot_id, s.schema_version, s.status, s.created_at, s.finished_at,
                   s.config_hash, s.repository_count, s.file_count, s.segment_count,
                   s.relationship_count, s.error_message
            FROM active_snapshot a
            JOIN snapshots s ON s.snapshot_id = a.snapshot_id
            WHERE a.singleton_id = 1
              AND s.status = $ready;
            """;
        command.Parameters.AddWithValue("$ready", SnapshotStatus.Ready.ToString());

        using var reader = command.ExecuteReader();
        var snapshot = reader.Read() ? SqliteStorageMapper.ReadSnapshot(reader) : null;
        return Task.FromResult(snapshot);
    }
}
