using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Interfaces;

public interface IActiveSnapshotProvider
{
    Task<SnapshotMetadata?> GetActive(CancellationToken cancellationToken = default);
}
