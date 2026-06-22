using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Business.Segments.Models;
using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Business.Segments.Extraction;

public sealed class SolutionSegmentExtractor : ISegmentExtractor
{
    private static readonly Regex SlnProjectLine = new(
        @"Project\(""\{[^}]+\}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+\.csproj)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanExtract(string extension) =>
        extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<MemorySegment> Extract(ScannedFile file)
    {
        var projects = file.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            ? ExtractFromSlnx(file)
            : ExtractFromSln(file);

        if (projects.Count == 0)
        {
            yield break;
        }

        var solutionName = Path.GetFileNameWithoutExtension(file.FullPath);
        var stableKey = $"solution:{file.RepoId}:{file.RelativePath}";

        yield return new MemorySegment
        {
            SegmentId = stableKey.ToSegmentId(),
            StableKey = stableKey,
            RepoId = file.RepoId,
            Kind = SegmentKind.Solution,
            Name = solutionName,
            FilePath = file.RelativePath,
            StartLine = 1,
            ContentHash = file.ContentHash,
            Metadata = new Dictionary<string, string>
            {
                ["project_refs"] = string.Join(", ", projects),
            },
        };
    }

    private static IReadOnlyList<string> ExtractFromSln(ScannedFile file)
    {
        return File.ReadAllLines(file.FullPath)
            .Select(line => SlnProjectLine.Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToList();
    }

    private static IReadOnlyList<string> ExtractFromSlnx(ScannedFile file)
    {
        XDocument xml;
        try
        {
            xml = XDocument.Load(file.FullPath);
        }
        catch (XmlException ex)
        {
            Console.Error.WriteLine($"[extraction] failed to parse {file.RelativePath}: {ex.Message}");
            return [];
        }

        return xml.Descendants("Project")
            .Select(e => e.Attribute("Path")?.Value ?? string.Empty)
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(projectName => projectName.Length > 0)
            .ToList();
    }
}
