using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Queries.Models;

public sealed record FileContext
{
    public IReadOnlyList<MemorySegment> Segments { get; init; } = [];

    public IReadOnlyList<MemoryRelationship> Relationships { get; init; } = [];
}
