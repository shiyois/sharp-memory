using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Storage.Interfaces;

public interface ISegmentStore
{
    Task<IReadOnlyList<SegmentSearchResult>> Search(
        string snapshotId,
        SegmentSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<MemorySegment?> GetById(
        string snapshotId,
        string segmentId,
        CancellationToken cancellationToken = default);

    Task<MemorySegment?> GetByStableKey(
        string snapshotId,
        string stableKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemorySegment>> GetByFile(
        string snapshotId,
        string repoId,
        string filePath,
        CancellationToken cancellationToken = default);
}
