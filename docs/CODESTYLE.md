# SharpMemory Code Style

## Naming

- Do not use `Async` suffix for SharpMemory methods, local variables, fields, properties, parameters, or tuple members.
- The only allowed `Async` names are external framework/API members we implement or call, such as `IHostedService.StartAsync`, `IHostedService.StopAsync`, `Task.WaitAsync`, or `File.ReadAllBytesAsync`.
- Name methods and variables by their domain role, not by the implementation detail that they return or await a `Task`.

Prefer:

```csharp
var refreshTask = snapshotRefresh.StartRefreshInBackground(cancellationToken);
var snapshot = await snapshotBuilder.BuildSnapshot(cancellationToken);
```

Avoid:

```csharp
var refreshAsync = snapshotRefresh.StartRefreshInBackground(cancellationToken);
var snapshotAsync = snapshotBuilder.BuildSnapshot(cancellationToken);
```

## Domain Language

- Prefer `snapshot`, `segment`, `relationship`, `repository`, and `search` in business code.
- Avoid using `indexing` or `reindex` for the memory-building flow. A rebuild produces a new snapshot; search indexes are implementation details inside storage.
- Keep App code focused on hosting, HTTP/MCP transport, settings, and runtime orchestration.
- Keep Core code focused on building, storing, and querying memory snapshots.
