using FluentAssertions;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Integration;

[TestFixture]
public sealed class SnapshotQueryFlowTests
{
    [Test]
    public async Task BuildSnapshot_PublishesReadySnapshotAndSearchesSegments()
    {
        using var repo = CreateRepository();
        using var storage = new TempDirectory();
        var harness = new SnapshotTestHarness(storage.Path, [repo.Path]);

        var snapshot = await harness.Builder.BuildSnapshot();
        var repositories = await harness.Queries.ListRepositories();
        var searchResults = await harness.Queries.Search(
            "Worker",
            topN: 10,
            project: "Demo.App",
            repository: null,
            kind: SegmentKind.Class);

        snapshot.Status.Should().Be(SnapshotStatus.Ready);
        snapshot.RepositoryCount.Should().Be(1);
        snapshot.SegmentCount.Should().BeGreaterThan(0);
        repositories.Should().ContainSingle(r => r.RepoName == Path.GetFileName(repo.Path));
        searchResults.Should().ContainSingle(r =>
            r.Segment.Kind == SegmentKind.Class
            && r.Segment.Name == "Worker"
            && r.Segment.ProjectName == "Demo.App");
    }

    [Test]
    public async Task BuildSnapshot_StoresRelationshipsAndFileContext()
    {
        using var repo = CreateRepository();
        using var storage = new TempDirectory();
        var harness = new SnapshotTestHarness(storage.Path, [repo.Path]);

        await harness.Builder.BuildSnapshot();
        var repository = (await harness.Queries.ListRepositories()).Single();
        var callers = await harness.Queries.FindCallers("Helper");
        var fileContext = await harness.Queries.GetFileContext(repository.RepoId, "src/Worker.cs");
        var relationships = await harness.Queries.GetRelationships(type: RelationshipType.Calls);

        callers.Should().ContainSingle(s => s.Kind == SegmentKind.Method && s.Name == "Run");
        fileContext.Segments.Should().Contain(s => s.Kind == SegmentKind.Class && s.Name == "Worker");
        relationships.Should().ContainSingle(r =>
            r.FromStableKey.Contains(".Run(", StringComparison.Ordinal)
            && r.ToStableKey.Contains(".Helper(", StringComparison.Ordinal));
    }

    private static TempDirectory CreateRepository()
    {
        var repo = new TempDirectory();
        repo.WriteFile(
            ".gitignore",
            """
            obj/
            """);
        repo.WriteFile(
            "Demo.App.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>Demo.App</AssemblyName>
              </PropertyGroup>
            </Project>
            """);
        repo.WriteFile(
            "src/Worker.cs",
            """
            namespace Demo.App;

            public class Worker
            {
                public void Run()
                {
                    Helper();
                }

                private void Helper()
                {
                }
            }
            """);
        repo.WriteFile("obj/Ignored.cs", "class Ignored { }");
        return repo;
    }
}
