using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Sqlite;

public sealed class SqliteRelationshipStore(SqliteConnectionFactory connectionFactory)
    : IRelationshipStore
{
    public Task<IReadOnlyList<MemoryRelationship>> GetAll(
        string snapshotId,
        string? from = null,
        string? to = null,
        RelationshipType? type = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();

        var filters = new List<string> { "snapshot_id = $snapshotId" };
        command.Parameters.AddWithValue("$snapshotId", snapshotId);

        if (!string.IsNullOrWhiteSpace(from))
        {
            filters.Add("(from_segment_id = $from OR from_stable_key LIKE $fromLike)");
            command.Parameters.AddWithValue("$from", from);
            command.Parameters.AddWithValue("$fromLike", $"%{from}%");
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            filters.Add("(to_segment_id = $to OR to_stable_key LIKE $toLike)");
            command.Parameters.AddWithValue("$to", to);
            command.Parameters.AddWithValue("$toLike", $"%{to}%");
        }

        if (type.HasValue)
        {
            filters.Add("type = $type");
            command.Parameters.AddWithValue("$type", type.Value.ToString());
        }

        command.CommandText = $"""
            SELECT relationship_id, from_segment_id, to_segment_id,
                   from_stable_key, to_stable_key, type, metadata_json
            FROM relationships
            WHERE {string.Join(" AND ", filters)}
            ORDER BY type, relationship_id
            LIMIT 500;
            """;

        var relationships = new List<MemoryRelationship>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            relationships.Add(SqliteStorageMapper.ReadRelationship(reader));
        }

        return Task.FromResult<IReadOnlyList<MemoryRelationship>>(relationships);
    }

    public Task<IReadOnlyList<MemoryRelationship>> GetOutgoing(
        string snapshotId,
        string segmentId,
        RelationshipType? type = null,
        CancellationToken cancellationToken = default) =>
        GetRelationships(snapshotId, "from_segment_id", segmentId, type, cancellationToken);

    public Task<IReadOnlyList<MemoryRelationship>> GetIncoming(
        string snapshotId,
        string segmentId,
        RelationshipType? type = null,
        CancellationToken cancellationToken = default) =>
        GetRelationships(snapshotId, "to_segment_id", segmentId, type, cancellationToken);

    private Task<IReadOnlyList<MemoryRelationship>> GetRelationships(
        string snapshotId,
        string segmentColumn,
        string segmentId,
        RelationshipType? type,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = connectionFactory.Open();
        using var command = connection.CreateCommand();

        var typeFilter = type.HasValue ? "AND type = $type" : string.Empty;
        command.CommandText = $"""
            SELECT relationship_id, from_segment_id, to_segment_id,
                   from_stable_key, to_stable_key, type, metadata_json
            FROM relationships
            WHERE snapshot_id = $snapshotId
              AND {segmentColumn} = $segmentId
              {typeFilter}
            ORDER BY type, relationship_id;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId);
        command.Parameters.AddWithValue("$segmentId", segmentId);
        if (type.HasValue)
        {
            command.Parameters.AddWithValue("$type", type.Value.ToString());
        }

        var relationships = new List<MemoryRelationship>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            relationships.Add(SqliteStorageMapper.ReadRelationship(reader));
        }

        return Task.FromResult<IReadOnlyList<MemoryRelationship>>(relationships);
    }
}
