using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Sqlite;

public sealed class SqliteSnapshotStatusStore(SqliteConnectionFactory connectionFactory)
    : ISnapshotStatusStore
{
    public Task<SnapshotMetadata?> GetSnapshot(
        string snapshotId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT snapshot_id, schema_version, status, created_at, finished_at,
                   config_hash, repository_count, file_count, segment_count,
                   relationship_count, error_message
            FROM snapshots
            WHERE snapshot_id = $snapshotId;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId);

        using var reader = command.ExecuteReader();
        var snapshot = reader.Read() ? SqliteStorageMapper.ReadSnapshot(reader) : null;
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<SnapshotRepositoryInfo>> GetRepositories(
        string snapshotId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT snapshot_id, repo_id, repo_name, repo_path, commit_sha,
                   indexed_at, file_count, segment_count
            FROM snapshot_repositories
            WHERE snapshot_id = $snapshotId
            ORDER BY repo_name;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId);

        var repositories = new List<SnapshotRepositoryInfo>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            repositories.Add(SqliteStorageMapper.ReadRepository(reader));
        }

        return Task.FromResult<IReadOnlyList<SnapshotRepositoryInfo>>(repositories);
    }

    public Task<IReadOnlyList<SnapshotDiagnostic>> GetDiagnostics(
        string snapshotId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT snapshot_id, repo_id, file_path, severity, code, message
            FROM snapshot_diagnostics
            WHERE snapshot_id = $snapshotId
            ORDER BY id;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId);

        var diagnostics = new List<SnapshotDiagnostic>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            diagnostics.Add(SqliteStorageMapper.ReadDiagnostic(reader));
        }

        return Task.FromResult<IReadOnlyList<SnapshotDiagnostic>>(diagnostics);
    }
}
