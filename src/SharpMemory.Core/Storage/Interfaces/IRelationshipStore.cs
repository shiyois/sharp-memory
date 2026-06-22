using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Interfaces;

public interface IRelationshipStore
{
    Task<IReadOnlyList<MemoryRelationship>> GetAll(
        string snapshotId,
        string? from = null,
        string? to = null,
        RelationshipType? type = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryRelationship>> GetOutgoing(
        string snapshotId,
        string segmentId,
        RelationshipType? type = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryRelationship>> GetIncoming(
        string snapshotId,
        string segmentId,
        RelationshipType? type = null,
        CancellationToken cancellationToken = default);
}
