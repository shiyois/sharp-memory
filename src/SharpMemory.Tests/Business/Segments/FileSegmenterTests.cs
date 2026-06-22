using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SharpMemory.Core.Business.Segments;
using SharpMemory.Core.Business.Segments.Extraction;
using SharpMemory.Core.Business.Segments.Models;
using SharpMemory.Core.Common.Models;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Business.Segments;

[TestFixture]
public sealed class FileSegmenterTests
{
    [Test]
    public async Task Segment_ResolvesNearestProjectNameForCSharpFile()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("Root.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        temp.WriteFile("src/Nested/Nested.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var filePath = temp.WriteFile("src/Nested/Feature/Worker.cs", "class Worker { }");
        var extractor = new CapturingExtractor(".cs");
        var segmenter = new FileSegmenter([extractor], NullLogger<FileSegmenter>.Instance);

        var segments = new List<MemorySegment>();
        await foreach (var segment in segmenter.Segment(
            filePath,
            "src/Nested/Feature/Worker.cs",
            temp.Path,
            "repo"))
        {
            segments.Add(segment);
        }

        segments.Should().ContainSingle();
        extractor.CapturedFile.Should().NotBeNull();
        extractor.CapturedFile!.ProjectName.Should().Be("Nested");
        extractor.CapturedFile.RelativePath.Should().Be("src/Nested/Feature/Worker.cs");
        extractor.CapturedFile.ContentHash.Should().NotBeEmpty();
    }

    private sealed class CapturingExtractor(string extension) : ISegmentExtractor
    {
        public ScannedFile? CapturedFile { get; private set; }

        public bool CanExtract(string requestedExtension) =>
            requestedExtension.Equals(extension, StringComparison.OrdinalIgnoreCase);

        public IEnumerable<MemorySegment> Extract(ScannedFile file)
        {
            CapturedFile = file;
            yield return new MemorySegment
            {
                SegmentId = "segment",
                StableKey = "stable",
                RepoId = file.RepoId,
                Kind = SegmentKind.File,
                Name = Path.GetFileName(file.RelativePath),
                FilePath = file.RelativePath,
                ContentHash = file.ContentHash,
                ProjectName = file.ProjectName,
            };
        }
    }
}
