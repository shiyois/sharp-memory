using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Interfaces;

public interface ISnapshotWriter
{
    Task Begin(SnapshotMetadata metadata, CancellationToken cancellationToken = default);

    Task WriteRepository(
        SnapshotRepositoryInfo repository,
        CancellationToken cancellationToken = default);

    Task WriteFile(SnapshotFileInfo file, CancellationToken cancellationToken = default);

    Task WriteSegments(
        string snapshotId,
        IReadOnlyList<MemorySegment> segments,
        CancellationToken cancellationToken = default);

    Task WriteRelationships(
        string snapshotId,
        IReadOnlyList<MemoryRelationship> relationships,
        CancellationToken cancellationToken = default);

    Task WriteDiagnostic(
        SnapshotDiagnostic diagnostic,
        CancellationToken cancellationToken = default);

    Task MarkFailed(
        string snapshotId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
