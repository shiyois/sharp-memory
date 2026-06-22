using SharpMemory.Core.Storage.Interfaces;

namespace SharpMemory.App.Application.Snapshots;

public sealed class SnapshotStartupService(
    SnapshotRefreshCoordinator snapshotRefresh,
    IActiveSnapshotProvider activeSnapshotProvider,
    ILogger<SnapshotStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var activeSnapshot = await activeSnapshotProvider.GetActive(cancellationToken);
            if (activeSnapshot is null)
            {
                logger.LogInformation("No active snapshot found. Building initial snapshot before serving memory tools.");
                await snapshotRefresh.BuildInitialSnapshot(cancellationToken);
                return;
            }

            logger.LogInformation(
                "Active snapshot {SnapshotId} found. Starting background refresh.",
                activeSnapshot.SnapshotId);

            await snapshotRefresh.StartRefreshInBackground(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Initial snapshot build failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
