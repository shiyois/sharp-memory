using System.Text;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Sqlite;

public sealed class SqliteSegmentStore(SqliteConnectionFactory connectionFactory) : ISegmentStore
{
    public Task<IReadOnlyList<SegmentSearchResult>> Search(
        string snapshotId,
        SegmentSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();

        var where = new StringBuilder("s.snapshot_id = $snapshotId");
        command.Parameters.AddWithValue("$snapshotId", snapshotId);

        if (!string.IsNullOrWhiteSpace(request.RepoId))
        {
            where.Append(" AND s.repo_id = $repoId");
            command.Parameters.AddWithValue("$repoId", request.RepoId);
        }

        if (!string.IsNullOrWhiteSpace(request.ProjectName))
        {
            where.Append(" AND s.project_name = $projectName");
            command.Parameters.AddWithValue("$projectName", request.ProjectName);
        }

        if (request.Kind.HasValue)
        {
            where.Append(" AND s.kind = $kind");
            command.Parameters.AddWithValue("$kind", request.Kind.Value.ToString());
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            command.CommandText = $"""
                SELECT s.segment_json, 0.0 AS score, s.preview
                FROM segments s
                WHERE {where}
                ORDER BY s.repo_id, s.project_name, s.file_path, s.start_line
                LIMIT $topN;
                """;
        }
        else
        {
            command.CommandText = $"""
                SELECT s.segment_json, bm25(segment_fts) AS score, s.preview
                FROM segment_fts f
                JOIN segments s
                  ON s.snapshot_id = f.snapshot_id
                 AND s.segment_id = f.segment_id
                WHERE segment_fts MATCH $query
                  AND {where}
                ORDER BY score
                LIMIT $topN;
                """;
            command.Parameters.AddWithValue("$query", BuildFtsQuery(request.Query));
        }

        command.Parameters.AddWithValue("$topN", Math.Clamp(request.TopN, 1, 100));

        var results = new List<SegmentSearchResult>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var segment = SqliteJson.Deserialize<MemorySegment>(reader.GetString(0));
            if (segment is null)
            {
                continue;
            }

            results.Add(
                new SegmentSearchResult
                {
                    Segment = segment,
                    Score = reader.GetDouble(1),
                    Preview = reader.GetString(2),
                });
        }

        return Task.FromResult<IReadOnlyList<SegmentSearchResult>>(results);
    }

    public Task<MemorySegment?> GetById(
        string snapshotId,
        string segmentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT segment_json
            FROM segments
            WHERE snapshot_id = $snapshotId
              AND segment_id = $segmentId;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId);
        command.Parameters.AddWithValue("$segmentId", segmentId);

        var json = command.ExecuteScalar() as string;
        var segment = json is null ? null : SqliteJson.Deserialize<MemorySegment>(json);
        return Task.FromResult(segment);
    }

    public Task<MemorySegment?> GetByStableKey(
        string snapshotId,
        string stableKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT segment_json
            FROM segments
            WHERE snapshot_id = $snapshotId
              AND stable_key = $stableKey;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId);
        command.Parameters.AddWithValue("$stableKey", stableKey);

        var json = command.ExecuteScalar() as string;
        var segment = json is null ? null : SqliteJson.Deserialize<MemorySegment>(json);
        return Task.FromResult(segment);
    }

    public Task<IReadOnlyList<MemorySegment>> GetByFile(
        string snapshotId,
        string repoId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT segment_json
            FROM segments
            WHERE snapshot_id = $snapshotId
              AND repo_id = $repoId
              AND file_path = $filePath
            ORDER BY start_line, end_line;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId);
        command.Parameters.AddWithValue("$repoId", repoId);
        command.Parameters.AddWithValue("$filePath", filePath);

        var segments = new List<MemorySegment>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var segment = SqliteJson.Deserialize<MemorySegment>(reader.GetString(0));
            if (segment is not null)
            {
                segments.Add(segment);
            }
        }

        return Task.FromResult<IReadOnlyList<MemorySegment>>(segments);
    }

    private static string BuildFtsQuery(string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
        {
            return "\"\"";
        }

        return string.Join(" ", terms.Select(static term => $"\"{term.Replace("\"", "\"\"")}\""));
    }
}
