using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Segments.Relationships;

public sealed class SegmentRelationshipBuilder
{
    public IReadOnlyList<MemoryRelationship> Build(IReadOnlyList<MemorySegment> segments)
    {
        var relationships = new List<MemoryRelationship>();
        var byProject = segments
            .Where(static s => s.Kind == SegmentKind.Project)
            .GroupBy(static s => s.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static g => g.Key, static g => g.First(), StringComparer.OrdinalIgnoreCase);

        var methodsByName = segments
            .Where(static s => s.Kind is SegmentKind.Method or SegmentKind.Constructor)
            .GroupBy(static s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static g => g.Key, static g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var typesByName = segments
            .Where(static s => s.Kind is SegmentKind.Class
                or SegmentKind.Interface
                or SegmentKind.Record
                or SegmentKind.Struct)
            .GroupBy(static s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static g => g.Key, static g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var segment in segments)
        {
            if (!string.IsNullOrWhiteSpace(segment.ProjectName)
                && byProject.TryGetValue(segment.ProjectName, out var project)
                && segment.SegmentId != project.SegmentId
                && segment.Kind != SegmentKind.Project)
            {
                relationships.Add(Create(segment, project, RelationshipType.DefinedIn));
            }

            if (segment.Kind == SegmentKind.Solution
                && segment.Metadata.TryGetValue("project_refs", out var solutionProjects))
            {
                foreach (var projectName in SplitCsv(solutionProjects))
                {
                    if (byProject.TryGetValue(projectName, out var target))
                    {
                        relationships.Add(Create(segment, target, RelationshipType.Contains));
                    }
                }
            }

            if (segment.Kind == SegmentKind.Project
                && segment.Metadata.TryGetValue("project_refs", out var projectRefs))
            {
                foreach (var projectName in SplitCsv(projectRefs))
                {
                    if (byProject.TryGetValue(projectName, out var target))
                    {
                        relationships.Add(Create(segment, target, RelationshipType.DependsOn));
                    }
                }
            }

            if (segment.Kind is SegmentKind.Class or SegmentKind.Record or SegmentKind.Struct
                && segment.Metadata.TryGetValue("baseTypes", out var baseTypes))
            {
                foreach (var baseType in SplitCsv(baseTypes).Select(NormalizeTypeName))
                {
                    if (!typesByName.TryGetValue(baseType, out var candidates))
                    {
                        continue;
                    }

                    foreach (var target in candidates
                        .Where(s => s.SegmentId != segment.SegmentId)
                        .Take(5))
                    {
                        var type = target.Kind == SegmentKind.Interface
                            ? RelationshipType.Implements
                            : RelationshipType.Extends;

                        relationships.Add(Create(segment, target, type));
                    }
                }
            }

            if (segment.Kind is SegmentKind.Method or SegmentKind.Constructor
                && segment.Metadata.TryGetValue("called_methods", out var calledMethods))
            {
                foreach (var calledName in SplitCsv(calledMethods))
                {
                    if (!methodsByName.TryGetValue(calledName, out var candidates))
                    {
                        continue;
                    }

                    foreach (var target in candidates
                        .Where(s => s.SegmentId != segment.SegmentId)
                        .Take(5))
                    {
                        relationships.Add(Create(segment, target, RelationshipType.Calls));
                    }
                }
            }
        }

        return relationships
            .DistinctBy(static r => r.RelationshipId, StringComparer.Ordinal)
            .ToList();
    }

    private static MemoryRelationship Create(
        MemorySegment from,
        MemorySegment to,
        RelationshipType type)
    {
        var stableKey = $"rel:{type}:{from.SegmentId}:{to.SegmentId}";
        return new MemoryRelationship
        {
            RelationshipId = stableKey.ToSegmentId(),
            FromSegmentId = from.SegmentId,
            ToSegmentId = to.SegmentId,
            FromStableKey = from.StableKey,
            ToStableKey = to.StableKey,
            Type = type,
        };
    }

    private static IEnumerable<string> SplitCsv(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static item => item.Length > 0);

    private static string NormalizeTypeName(string typeName)
    {
        var withoutGeneric = typeName.Split('<', 2)[0];
        var parts = withoutGeneric.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? withoutGeneric : parts[^1];
    }
}
