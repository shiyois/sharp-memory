using SharpMemory.App.Application.Snapshots;
using SharpMemory.Core.Business.Queries;

namespace SharpMemory.App.Http;

public static class MemoryApiEndpoints
{
    public static WebApplication MapMemoryApi(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.MapGet("/api/memory/repositories", async (MemoryQueryService queries, CancellationToken ct) =>
            Results.Ok(await queries.ListRepositories(ct)));

        app.MapGet(
            "/api/memory/search",
            async (
                string query,
                MemoryQueryService queries,
                int topN = 10,
                string? project = null,
                string? repository = null,
                CancellationToken ct = default) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Results.BadRequest(new { error = "query is required" });
                }

                return Results.Ok(await queries.Search(query, topN, project, repository, null, ct));
            });

        app.MapGet("/api/memory/snapshot/refresh/status", (SnapshotRefreshCoordinator snapshotRefresh) =>
            Results.Ok(snapshotRefresh.GetStatus()));

        app.MapPost("/api/memory/snapshot/refresh", async (SnapshotRefreshCoordinator snapshotRefresh, CancellationToken ct) =>
        {
            var result = await snapshotRefresh.StartRefreshInBackground(ct);
            return result.Started
                ? Results.Accepted(value: result.Status)
                : Results.Conflict(result.Status);
        });

        return app;
    }
}
