using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Interfaces;

public interface ISnapshotStatusStore
{
    Task<SnapshotMetadata?> GetSnapshot(
        string snapshotId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnapshotRepositoryInfo>> GetRepositories(
        string snapshotId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnapshotDiagnostic>> GetDiagnostics(
        string snapshotId,
        CancellationToken cancellationToken = default);
}
