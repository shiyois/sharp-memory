using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Sqlite;

public sealed class SqliteSnapshotPublisher(SqliteConnectionFactory connectionFactory) : ISnapshotPublisher
{
    public Task Publish(string snapshotId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var transaction = connection.BeginTransaction();

        var counts = ReadCounts(connection, transaction, snapshotId);

        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE snapshots
            SET status = $ready,
                finished_at = $finishedAt,
                repository_count = $repositoryCount,
                file_count = $fileCount,
                segment_count = $segmentCount,
                relationship_count = $relationshipCount
            WHERE snapshot_id = $snapshotId
              AND status = $building;
            """;
        update.Parameters.AddWithValue("$snapshotId", snapshotId);
        update.Parameters.AddWithValue("$ready", SnapshotStatus.Ready.ToString());
        update.Parameters.AddWithValue("$building", SnapshotStatus.Building.ToString());
        update.Parameters.AddWithValue("$finishedAt", DateTimeOffset.UtcNow.ToString("O"));
        update.Parameters.AddWithValue("$repositoryCount", counts.RepositoryCount);
        update.Parameters.AddWithValue("$fileCount", counts.FileCount);
        update.Parameters.AddWithValue("$segmentCount", counts.SegmentCount);
        update.Parameters.AddWithValue("$relationshipCount", counts.RelationshipCount);

        if (update.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException(
                $"Snapshot '{snapshotId}' is not in Building state and cannot be published.");
        }

        using var active = connection.CreateCommand();
        active.Transaction = transaction;
        active.CommandText = """
            INSERT INTO active_snapshot (singleton_id, snapshot_id)
            VALUES (1, $snapshotId)
            ON CONFLICT(singleton_id) DO UPDATE SET snapshot_id = excluded.snapshot_id;
            """;
        active.Parameters.AddWithValue("$snapshotId", snapshotId);
        active.ExecuteNonQuery();

        CleanupOldSnapshots(connection, transaction, snapshotId);

        transaction.Commit();
        return Task.CompletedTask;
    }

    private static SnapshotCounts ReadCounts(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Microsoft.Data.Sqlite.SqliteTransaction transaction,
        string snapshotId)
    {
        var repositoryCount = Count(connection, transaction, "snapshot_repositories", snapshotId);
        var fileCount = Count(connection, transaction, "snapshot_files", snapshotId);
        var segmentCount = Count(connection, transaction, "segments", snapshotId);
        var relationshipCount = Count(connection, transaction, "relationships", snapshotId);
        return new SnapshotCounts(repositoryCount, fileCount, segmentCount, relationshipCount);
    }

    private static int Count(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Microsoft.Data.Sqlite.SqliteTransaction transaction,
        string table,
        string snapshotId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE snapshot_id = $snapshotId";
        command.Parameters.AddWithValue("$snapshotId", snapshotId);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void CleanupOldSnapshots(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Microsoft.Data.Sqlite.SqliteTransaction transaction,
        string activeSnapshotId)
    {
        using var previousReady = connection.CreateCommand();
        previousReady.Transaction = transaction;
        previousReady.CommandText = """
            SELECT snapshot_id
            FROM snapshots
            WHERE status = $ready
              AND snapshot_id <> $activeSnapshotId
            ORDER BY finished_at DESC
            LIMIT 1;
            """;
        previousReady.Parameters.AddWithValue("$ready", SnapshotStatus.Ready.ToString());
        previousReady.Parameters.AddWithValue("$activeSnapshotId", activeSnapshotId);
        var previousSnapshotId = previousReady.ExecuteScalar() as string;

        using var latestFailed = connection.CreateCommand();
        latestFailed.Transaction = transaction;
        latestFailed.CommandText = """
            SELECT snapshot_id
            FROM snapshots
            WHERE status = $failed
            ORDER BY finished_at DESC
            LIMIT 1;
            """;
        latestFailed.Parameters.AddWithValue("$failed", SnapshotStatus.Failed.ToString());
        var failedSnapshotId = latestFailed.ExecuteScalar() as string;

        using var cleanup = connection.CreateCommand();
        cleanup.Transaction = transaction;
        cleanup.CommandText = """
            DELETE FROM snapshots
            WHERE snapshot_id <> $activeSnapshotId
              AND ($previousSnapshotId IS NULL OR snapshot_id <> $previousSnapshotId)
              AND ($failedSnapshotId IS NULL OR snapshot_id <> $failedSnapshotId);
            """;
        cleanup.Parameters.AddWithValue("$activeSnapshotId", activeSnapshotId);
        cleanup.Parameters.AddWithValue(
            "$previousSnapshotId",
            previousSnapshotId is null ? DBNull.Value : previousSnapshotId);
        cleanup.Parameters.AddWithValue(
            "$failedSnapshotId",
            failedSnapshotId is null ? DBNull.Value : failedSnapshotId);
        cleanup.ExecuteNonQuery();
    }

    private sealed record SnapshotCounts(
        int RepositoryCount,
        int FileCount,
        int SegmentCount,
        int RelationshipCount);
}
