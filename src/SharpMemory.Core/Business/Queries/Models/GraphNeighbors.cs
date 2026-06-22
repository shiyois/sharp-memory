using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Queries.Models;

public sealed record GraphNeighbors
{
    public IReadOnlyList<MemorySegment> Nodes { get; init; } = [];

    public IReadOnlyList<MemoryRelationship> Edges { get; init; } = [];
}
