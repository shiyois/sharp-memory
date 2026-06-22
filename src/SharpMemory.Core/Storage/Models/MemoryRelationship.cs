namespace SharpMemory.Core.Storage.Models;

public sealed record MemoryRelationship
{
    public string RelationshipId { get; init; } = string.Empty;

    public string FromSegmentId { get; init; } = string.Empty;

    public string ToSegmentId { get; init; } = string.Empty;

    public string FromStableKey { get; init; } = string.Empty;

    public string ToStableKey { get; init; } = string.Empty;

    public RelationshipType Type { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}
