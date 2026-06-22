using Microsoft.Data.Sqlite;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Sqlite;

internal static class SqliteStorageMapper
{
    public const int PreviewMaxChars = 800;
    public const int SearchTextMaxChars = 12_000;

    public static SnapshotMetadata ReadSnapshot(SqliteDataReader reader) =>
        new()
        {
            SnapshotId = reader.GetString(0),
            SchemaVersion = reader.GetInt32(1),
            Status = Enum.Parse<SnapshotStatus>(reader.GetString(2)),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(3)),
            FinishedAt = reader.IsDBNull(4) ? null : DateTimeOffset.Parse(reader.GetString(4)),
            ConfigHash = reader.GetString(5),
            RepositoryCount = reader.GetInt32(6),
            FileCount = reader.GetInt32(7),
            SegmentCount = reader.GetInt32(8),
            RelationshipCount = reader.GetInt32(9),
            ErrorMessage = reader.IsDBNull(10) ? null : reader.GetString(10),
        };

    public static SnapshotRepositoryInfo ReadRepository(SqliteDataReader reader) =>
        new()
        {
            SnapshotId = reader.GetString(0),
            RepoId = reader.GetString(1),
            RepoName = reader.GetString(2),
            RepoPath = reader.GetString(3),
            CommitSha = reader.IsDBNull(4) ? null : reader.GetString(4),
            IndexedAt = DateTimeOffset.Parse(reader.GetString(5)),
            FileCount = reader.GetInt32(6),
            SegmentCount = reader.GetInt32(7),
        };

    public static SnapshotDiagnostic ReadDiagnostic(SqliteDataReader reader) =>
        new()
        {
            SnapshotId = reader.GetString(0),
            RepoId = reader.GetString(1),
            FilePath = reader.IsDBNull(2) ? null : reader.GetString(2),
            Severity = Enum.Parse<SnapshotDiagnosticSeverity>(reader.GetString(3)),
            Code = reader.GetString(4),
            Message = reader.GetString(5),
        };

    public static MemoryRelationship ReadRelationship(SqliteDataReader reader) =>
        new()
        {
            RelationshipId = reader.GetString(0),
            FromSegmentId = reader.GetString(1),
            ToSegmentId = reader.GetString(2),
            FromStableKey = reader.GetString(3),
            ToStableKey = reader.GetString(4),
            Type = Enum.Parse<RelationshipType>(reader.GetString(5)),
            Metadata = SqliteJson.Deserialize<Dictionary<string, string>>(reader.GetString(6))
                ?? new Dictionary<string, string>(),
        };

    public static string BuildSearchText(MemorySegment segment)
    {
        var metadata = string.Join(
            ' ',
            segment.Metadata.Select(static item => $"{item.Key} {item.Value}"));

        return Truncate(
            $"{segment.Name} {segment.ContainerName} {segment.ProjectName} {segment.FilePath} {segment.Kind} {metadata}",
            SearchTextMaxChars);
    }

    public static string BuildPreview(MemorySegment segment)
    {
        if (segment.Metadata.TryGetValue("signature", out var signature)
            && !string.IsNullOrWhiteSpace(signature))
        {
            return Truncate(signature, PreviewMaxChars);
        }

        return Truncate($"{segment.Kind} {segment.ContainerName}/{segment.Name}", PreviewMaxChars);
    }

    public static string Truncate(string value, int maxChars)
    {
        if (value.Length <= maxChars)
        {
            return value;
        }

        return value[..maxChars];
    }
}
