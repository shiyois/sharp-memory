using SharpMemory.App.Application.Snapshots.Models;
using SharpMemory.Core.Business.Snapshots;

namespace SharpMemory.App.Application.Snapshots;

public sealed class SnapshotRefreshCoordinator(
    MemorySnapshotBuilder snapshotBuilder,
    ILogger<SnapshotRefreshCoordinator> logger)
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private SnapshotRefreshStatusDto status = new();

    public SnapshotRefreshStatusDto GetStatus() => status;

    public async Task<(bool Started, SnapshotRefreshStatusDto Status)> StartRefreshInBackground(
        CancellationToken cancellationToken = default)
    {
        if (!await semaphore.WaitAsync(0, cancellationToken))
        {
            return (false, status);
        }

        status = status with
        {
            IsRunning = true,
            RunningSnapshotId = null,
            StartedAt = DateTimeOffset.UtcNow,
            FinishedAt = null,
            LastError = null,
        };

        _ = RefreshSnapshot();
        return (true, status);
    }

    public async Task BuildInitialSnapshot(CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            status = status with
            {
                IsRunning = true,
                RunningSnapshotId = null,
                StartedAt = DateTimeOffset.UtcNow,
                FinishedAt = null,
                LastError = null,
            };

            var snapshot = await snapshotBuilder.BuildSnapshot(cancellationToken);
            status = status with
            {
                IsRunning = false,
                RunningSnapshotId = null,
                FinishedAt = DateTimeOffset.UtcNow,
                LastCompletedSnapshot = snapshot,
            };

            return;
        }
        catch (Exception ex)
        {
            status = status with
            {
                IsRunning = false,
                RunningSnapshotId = null,
                FinishedAt = DateTimeOffset.UtcNow,
                LastError = ex.Message,
            };
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task RefreshSnapshot()
    {
        try
        {
            var snapshot = await snapshotBuilder.BuildSnapshot(CancellationToken.None);
            status = status with
            {
                IsRunning = false,
                RunningSnapshotId = null,
                FinishedAt = DateTimeOffset.UtcNow,
                LastCompletedSnapshot = snapshot,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background snapshot refresh failed");
            status = status with
            {
                IsRunning = false,
                RunningSnapshotId = null,
                FinishedAt = DateTimeOffset.UtcNow,
                LastError = ex.Message,
            };
        }
        finally
        {
            semaphore.Release();
        }
    }
}
