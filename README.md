# SharpMemory

> This repository is under active development. Breaking changes are expected.

SharpMemory is an experimental local memory layer for coding agents, with a current focus on .NET projects.

The project focuses on helping an agent understand a repository through structural code segments, relationships, snapshots, and search.
The current direction is local-first: run it on your machine, index repositories you control, and expose memory to local tools such as MCP or HTTP clients.

Install:
```bash
dotnet tool install -g sharp-memory
sharp-memory run
```

Configure repositories in `settings.json`.

This is not yet a stable product, hosted service, or public network API. Treat it as a local developer tool while the design is still moving.
