using Microsoft.Data.Sqlite;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Sqlite;

public sealed class SqliteSnapshotWriter(SqliteConnectionFactory connectionFactory) : ISnapshotWriter
{
    public Task Begin(
        SnapshotMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        MarkBuildingSnapshotsAbandoned(connection);

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO snapshots (
                snapshot_id, schema_version, status, created_at, finished_at, config_hash,
                repository_count, file_count, segment_count, relationship_count, error_message
            )
            VALUES (
                $snapshotId, $schemaVersion, $status, $createdAt, $finishedAt, $configHash,
                $repositoryCount, $fileCount, $segmentCount, $relationshipCount, $errorMessage
            );
            """;
        command.Parameters.AddWithValue("$snapshotId", metadata.SnapshotId);
        command.Parameters.AddWithValue("$schemaVersion", metadata.SchemaVersion);
        command.Parameters.AddWithValue("$status", SnapshotStatus.Building.ToString());
        command.Parameters.AddWithValue("$createdAt", metadata.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$finishedAt", DBNull.Value);
        command.Parameters.AddWithValue("$configHash", metadata.ConfigHash);
        command.Parameters.AddWithValue("$repositoryCount", metadata.RepositoryCount);
        command.Parameters.AddWithValue("$fileCount", metadata.FileCount);
        command.Parameters.AddWithValue("$segmentCount", metadata.SegmentCount);
        command.Parameters.AddWithValue("$relationshipCount", metadata.RelationshipCount);
        command.Parameters.AddWithValue("$errorMessage", DBNull.Value);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task WriteRepository(
        SnapshotRepositoryInfo repository,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO snapshot_repositories (
                snapshot_id, repo_id, repo_name, repo_path, commit_sha,
                indexed_at, file_count, segment_count
            )
            VALUES (
                $snapshotId, $repoId, $repoName, $repoPath, $commitSha,
                $indexedAt, $fileCount, $segmentCount
            );
            """;
        command.Parameters.AddWithValue("$snapshotId", repository.SnapshotId);
        command.Parameters.AddWithValue("$repoId", repository.RepoId);
        command.Parameters.AddWithValue("$repoName", repository.RepoName);
        command.Parameters.AddWithValue("$repoPath", repository.RepoPath);
        command.Parameters.AddWithValue(
            "$commitSha",
            repository.CommitSha is null ? DBNull.Value : repository.CommitSha);
        command.Parameters.AddWithValue("$indexedAt", repository.IndexedAt.ToString("O"));
        command.Parameters.AddWithValue("$fileCount", repository.FileCount);
        command.Parameters.AddWithValue("$segmentCount", repository.SegmentCount);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task WriteFile(SnapshotFileInfo file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO snapshot_files (
                snapshot_id, repo_id, file_path, content_hash, project_name, segment_count
            )
            VALUES ($snapshotId, $repoId, $filePath, $contentHash, $projectName, $segmentCount);
            """;
        command.Parameters.AddWithValue("$snapshotId", file.SnapshotId);
        command.Parameters.AddWithValue("$repoId", file.RepoId);
        command.Parameters.AddWithValue("$filePath", file.FilePath);
        command.Parameters.AddWithValue("$contentHash", file.ContentHash);
        command.Parameters.AddWithValue("$projectName", file.ProjectName);
        command.Parameters.AddWithValue("$segmentCount", file.SegmentCount);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task WriteSegments(
        string snapshotId,
        IReadOnlyList<MemorySegment> segments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var transaction = connection.BeginTransaction();

        foreach (var segment in segments)
        {
            WriteSegment(connection, transaction, snapshotId, segment);
        }

        transaction.Commit();
        return Task.CompletedTask;
    }

    public Task WriteRelationships(
        string snapshotId,
        IReadOnlyList<MemoryRelationship> relationships,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var transaction = connection.BeginTransaction();

        foreach (var relationship in relationships)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR REPLACE INTO relationships (
                    snapshot_id, relationship_id, from_segment_id, to_segment_id,
                    from_stable_key, to_stable_key, type, metadata_json
                )
                VALUES (
                    $snapshotId, $relationshipId, $fromSegmentId, $toSegmentId,
                    $fromStableKey, $toStableKey, $type, $metadataJson
                );
                """;
            command.Parameters.AddWithValue("$snapshotId", snapshotId);
            command.Parameters.AddWithValue("$relationshipId", relationship.RelationshipId);
            command.Parameters.AddWithValue("$fromSegmentId", relationship.FromSegmentId);
            command.Parameters.AddWithValue("$toSegmentId", relationship.ToSegmentId);
            command.Parameters.AddWithValue("$fromStableKey", relationship.FromStableKey);
            command.Parameters.AddWithValue("$toStableKey", relationship.ToStableKey);
            command.Parameters.AddWithValue("$type", relationship.Type.ToString());
            command.Parameters.AddWithValue("$metadataJson", SqliteJson.Serialize(relationship.Metadata));
            command.ExecuteNonQuery();
        }

        transaction.Commit();
        return Task.CompletedTask;
    }

    public Task WriteDiagnostic(
        SnapshotDiagnostic diagnostic,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO snapshot_diagnostics (
                snapshot_id, repo_id, file_path, severity, code, message
            )
            VALUES ($snapshotId, $repoId, $filePath, $severity, $code, $message);
            """;
        command.Parameters.AddWithValue("$snapshotId", diagnostic.SnapshotId);
        command.Parameters.AddWithValue("$repoId", diagnostic.RepoId);
        command.Parameters.AddWithValue(
            "$filePath",
            diagnostic.FilePath is null ? DBNull.Value : diagnostic.FilePath);
        command.Parameters.AddWithValue("$severity", diagnostic.Severity.ToString());
        command.Parameters.AddWithValue("$code", diagnostic.Code);
        command.Parameters.AddWithValue("$message", diagnostic.Message);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task MarkFailed(
        string snapshotId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE snapshots
            SET status = $status,
                finished_at = $finishedAt,
                error_message = $errorMessage
            WHERE snapshot_id = $snapshotId;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId);
        command.Parameters.AddWithValue("$status", SnapshotStatus.Failed.ToString());
        command.Parameters.AddWithValue("$finishedAt", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$errorMessage", errorMessage);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    private static void WriteSegment(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string snapshotId,
        MemorySegment segment)
    {
        var searchText = SqliteStorageMapper.BuildSearchText(segment);
        var preview = SqliteStorageMapper.BuildPreview(segment);

        using var segmentCommand = connection.CreateCommand();
        segmentCommand.Transaction = transaction;
        segmentCommand.CommandText = """
            INSERT OR REPLACE INTO segments (
                snapshot_id, segment_id, stable_key, repo_id, kind, name, container_name,
                project_name, file_path, start_line, end_line, content_hash,
                search_text, preview, segment_json
            )
            VALUES (
                $snapshotId, $segmentId, $stableKey, $repoId, $kind, $name, $containerName,
                $projectName, $filePath, $startLine, $endLine, $contentHash,
                $searchText, $preview, $segmentJson
            );
            """;
        segmentCommand.Parameters.AddWithValue("$snapshotId", snapshotId);
        segmentCommand.Parameters.AddWithValue("$segmentId", segment.SegmentId);
        segmentCommand.Parameters.AddWithValue("$stableKey", segment.StableKey);
        segmentCommand.Parameters.AddWithValue("$repoId", segment.RepoId);
        segmentCommand.Parameters.AddWithValue("$kind", segment.Kind.ToString());
        segmentCommand.Parameters.AddWithValue("$name", segment.Name);
        segmentCommand.Parameters.AddWithValue("$containerName", segment.ContainerName);
        segmentCommand.Parameters.AddWithValue("$projectName", segment.ProjectName);
        segmentCommand.Parameters.AddWithValue("$filePath", segment.FilePath);
        segmentCommand.Parameters.AddWithValue("$startLine", segment.StartLine);
        segmentCommand.Parameters.AddWithValue("$endLine", segment.EndLine);
        segmentCommand.Parameters.AddWithValue("$contentHash", segment.ContentHash);
        segmentCommand.Parameters.AddWithValue("$searchText", searchText);
        segmentCommand.Parameters.AddWithValue("$preview", preview);
        segmentCommand.Parameters.AddWithValue("$segmentJson", SqliteJson.Serialize(segment));
        segmentCommand.ExecuteNonQuery();

        using var ftsCommand = connection.CreateCommand();
        ftsCommand.Transaction = transaction;
        ftsCommand.CommandText = """
            INSERT INTO segment_fts (
                snapshot_id, segment_id, name, container_name, project_name,
                file_path, kind, search_text
            )
            VALUES (
                $snapshotId, $segmentId, $name, $containerName, $projectName,
                $filePath, $kind, $searchText
            );
            """;
        ftsCommand.Parameters.AddWithValue("$snapshotId", snapshotId);
        ftsCommand.Parameters.AddWithValue("$segmentId", segment.SegmentId);
        ftsCommand.Parameters.AddWithValue("$name", segment.Name);
        ftsCommand.Parameters.AddWithValue("$containerName", segment.ContainerName);
        ftsCommand.Parameters.AddWithValue("$projectName", segment.ProjectName);
        ftsCommand.Parameters.AddWithValue("$filePath", segment.FilePath);
        ftsCommand.Parameters.AddWithValue("$kind", segment.Kind.ToString());
        ftsCommand.Parameters.AddWithValue("$searchText", searchText);
        ftsCommand.ExecuteNonQuery();
    }

    private static void MarkBuildingSnapshotsAbandoned(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE snapshots
            SET status = $abandoned,
                finished_at = $finishedAt
            WHERE status = $building;
            """;
        command.Parameters.AddWithValue("$abandoned", SnapshotStatus.Abandoned.ToString());
        command.Parameters.AddWithValue("$building", SnapshotStatus.Building.ToString());
        command.Parameters.AddWithValue("$finishedAt", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }
}
