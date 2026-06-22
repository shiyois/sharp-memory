using Microsoft.Data.Sqlite;

namespace SharpMemory.Core.Storage.Sqlite;

internal static class SqliteSchema
{
    public static void EnsureCreated(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS snapshots (
                snapshot_id TEXT PRIMARY KEY,
                schema_version INTEGER NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL,
                finished_at TEXT NULL,
                config_hash TEXT NOT NULL,
                repository_count INTEGER NOT NULL DEFAULT 0,
                file_count INTEGER NOT NULL DEFAULT 0,
                segment_count INTEGER NOT NULL DEFAULT 0,
                relationship_count INTEGER NOT NULL DEFAULT 0,
                error_message TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS active_snapshot (
                singleton_id INTEGER PRIMARY KEY CHECK (singleton_id = 1),
                snapshot_id TEXT NOT NULL REFERENCES snapshots(snapshot_id)
            );

            CREATE TABLE IF NOT EXISTS snapshot_repositories (
                snapshot_id TEXT NOT NULL REFERENCES snapshots(snapshot_id) ON DELETE CASCADE,
                repo_id TEXT NOT NULL,
                repo_name TEXT NOT NULL,
                repo_path TEXT NOT NULL,
                commit_sha TEXT NULL,
                indexed_at TEXT NOT NULL,
                file_count INTEGER NOT NULL DEFAULT 0,
                segment_count INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (snapshot_id, repo_id)
            );

            CREATE TABLE IF NOT EXISTS snapshot_files (
                snapshot_id TEXT NOT NULL REFERENCES snapshots(snapshot_id) ON DELETE CASCADE,
                repo_id TEXT NOT NULL,
                file_path TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                project_name TEXT NOT NULL,
                segment_count INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (snapshot_id, repo_id, file_path)
            );

            CREATE TABLE IF NOT EXISTS segments (
                snapshot_id TEXT NOT NULL REFERENCES snapshots(snapshot_id) ON DELETE CASCADE,
                segment_id TEXT NOT NULL,
                stable_key TEXT NOT NULL,
                repo_id TEXT NOT NULL,
                kind TEXT NOT NULL,
                name TEXT NOT NULL,
                container_name TEXT NOT NULL,
                project_name TEXT NOT NULL,
                file_path TEXT NOT NULL,
                start_line INTEGER NOT NULL,
                end_line INTEGER NOT NULL,
                content_hash TEXT NOT NULL,
                search_text TEXT NOT NULL,
                preview TEXT NOT NULL,
                segment_json TEXT NOT NULL,
                PRIMARY KEY (snapshot_id, segment_id)
            );

            CREATE INDEX IF NOT EXISTS idx_segments_stable_key
                ON segments(snapshot_id, stable_key);
            CREATE INDEX IF NOT EXISTS idx_segments_file
                ON segments(snapshot_id, repo_id, file_path);
            CREATE INDEX IF NOT EXISTS idx_segments_scope
                ON segments(snapshot_id, repo_id, project_name, kind);

            CREATE VIRTUAL TABLE IF NOT EXISTS segment_fts USING fts5(
                snapshot_id UNINDEXED,
                segment_id UNINDEXED,
                name,
                container_name,
                project_name,
                file_path,
                kind,
                search_text
            );

            CREATE TABLE IF NOT EXISTS relationships (
                snapshot_id TEXT NOT NULL REFERENCES snapshots(snapshot_id) ON DELETE CASCADE,
                relationship_id TEXT NOT NULL,
                from_segment_id TEXT NOT NULL,
                to_segment_id TEXT NOT NULL,
                from_stable_key TEXT NOT NULL,
                to_stable_key TEXT NOT NULL,
                type TEXT NOT NULL,
                metadata_json TEXT NOT NULL,
                PRIMARY KEY (snapshot_id, relationship_id)
            );

            CREATE INDEX IF NOT EXISTS idx_relationships_from
                ON relationships(snapshot_id, from_segment_id, type);
            CREATE INDEX IF NOT EXISTS idx_relationships_to
                ON relationships(snapshot_id, to_segment_id, type);

            CREATE TABLE IF NOT EXISTS snapshot_diagnostics (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                snapshot_id TEXT NOT NULL REFERENCES snapshots(snapshot_id) ON DELETE CASCADE,
                repo_id TEXT NOT NULL,
                file_path TEXT NULL,
                severity TEXT NOT NULL,
                code TEXT NOT NULL,
                message TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
