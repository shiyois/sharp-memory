using System.Xml;
using System.Xml.Linq;
using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Business.Segments.Models;
using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Business.Segments.Extraction;

public sealed class DotNetProjectSegmentExtractor : ISegmentExtractor
{
    public bool CanExtract(string extension) =>
        extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<MemorySegment> Extract(ScannedFile file)
    {
        XDocument xml;
        try
        {
            xml = XDocument.Load(file.FullPath);
        }
        catch (XmlException ex)
        {
            Console.Error.WriteLine($"[extraction] failed to parse {file.RelativePath}: {ex.Message}");
            yield break;
        }

        XNamespace ns = xml.Root?.Name.NamespaceName ?? string.Empty;

        var targetFramework =
            xml.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value
            ?? xml.Descendants(ns + "TargetFrameworks").FirstOrDefault()?.Value
            ?? string.Empty;

        var assemblyName =
            xml.Descendants(ns + "AssemblyName").FirstOrDefault()?.Value
            ?? Path.GetFileNameWithoutExtension(file.FullPath);

        var packageRefs = xml.Descendants(ns + "PackageReference")
            .Select(e =>
            {
                var packageName = e.Attribute("Include")?.Value ?? string.Empty;
                var version = e.Attribute("Version")?.Value ?? e.Element(ns + "Version")?.Value ?? string.Empty;
                return (packageName, version);
            })
            .Where(r => r.packageName.Length > 0)
            .Select(r => r.version.Length > 0 ? $"{r.packageName} {r.version}" : r.packageName)
            .ToList();

        var projectRefs = xml.Descendants(ns + "ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")?.Value ?? string.Empty))
            .Where(projectName => projectName.Length > 0)
            .ToList();

        var stableKey = $"project:{file.RepoId}:{file.RelativePath}";

        yield return new MemorySegment
        {
            SegmentId = stableKey.ToSegmentId(),
            StableKey = stableKey,
            RepoId = file.RepoId,
            Kind = SegmentKind.Project,
            Name = assemblyName,
            ProjectName = assemblyName,
            FilePath = file.RelativePath,
            StartLine = 1,
            ContentHash = file.ContentHash,
            Metadata = new Dictionary<string, string>
            {
                ["target_framework"] = targetFramework,
                ["package_refs"] = string.Join(", ", packageRefs),
                ["project_refs"] = string.Join(", ", projectRefs),
            },
        };
    }
}
