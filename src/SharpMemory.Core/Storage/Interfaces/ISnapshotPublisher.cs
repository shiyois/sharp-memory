namespace SharpMemory.Core.Storage.Interfaces;

public interface ISnapshotPublisher
{
    Task Publish(string snapshotId, CancellationToken cancellationToken = default);
}
