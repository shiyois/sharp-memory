using FluentAssertions;
using SharpMemory.Core.Business.Segments.Extraction;
using SharpMemory.Core.Business.Segments.Models;
using SharpMemory.Core.Common.Models;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Business.Segments;

[TestFixture]
public sealed class CSharpSegmentExtractorTests
{
    [Test]
    public void Extract_ReturnsStructuralSegmentsWithMetadata()
    {
        using var temp = new TempDirectory();
        var filePath = temp.WriteFile(
            "src/Worker.cs",
            """
            namespace Demo.App;

            public interface IService
            {
                void Execute();
            }

            public class Worker : IService
            {
                private readonly int count;

                public string Name { get; }

                public Worker()
                {
                    Execute();
                }

                public void Execute()
                {
                    Helper();
                }

                private void Helper()
                {
                }
            }
            """);
        var file = new ScannedFile
        {
            RepoId = "repo",
            FullPath = filePath,
            RelativePath = "src/Worker.cs",
            Extension = ".cs",
            ContentHash = "hash",
            ProjectName = "App",
        };

        var segments = new CSharpSegmentExtractor().Extract(file).ToList();

        segments.Should().ContainSingle(s => s.Kind == SegmentKind.Interface && s.Name == "IService");
        segments.Should().ContainSingle(s => s.Kind == SegmentKind.Class && s.Name == "Worker");
        segments.Should().ContainSingle(s => s.Kind == SegmentKind.Field && s.Name == "count");
        segments.Should().ContainSingle(s => s.Kind == SegmentKind.Property && s.Name == "Name");
        segments.Should().ContainSingle(s => s.Kind == SegmentKind.Constructor && s.Name == "Worker");
        segments.Should().Contain(s => s.Kind == SegmentKind.Method && s.Name == "Execute");
        segments.Should().Contain(s => s.Kind == SegmentKind.Method && s.Name == "Helper");

        var worker = segments.Single(s => s.Kind == SegmentKind.Class && s.Name == "Worker");
        worker.ContainerName.Should().Be("Demo.App");
        worker.ProjectName.Should().Be("App");
        worker.Metadata.Should().ContainKey("baseTypes").WhoseValue.Should().Be("IService");

        var execute = segments.Single(s =>
            s.Kind == SegmentKind.Method
            && s.Name == "Execute"
            && s.ContainerName == "Worker");
        execute.ContainerName.Should().Be("Worker");
        execute.Metadata.Should().ContainKey("called_methods").WhoseValue.Should().Be("Helper");
        execute.StableKey.Should().Contain("repo:App:Method:Demo.App.Worker.Execute");
    }
}
