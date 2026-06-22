using FluentAssertions;
using SharpMemory.Core.Business.Segments.Extraction;
using SharpMemory.Core.Business.Segments.Models;
using SharpMemory.Core.Common.Models;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Business.Segments;

[TestFixture]
public sealed class ProjectAndSolutionExtractorTests
{
    [Test]
    public void DotNetProjectExtractor_ReadsAssemblyTargetFrameworkPackagesAndProjectReferences()
    {
        using var temp = new TempDirectory();
        var projectPath = temp.WriteFile(
            "src/App/App.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>Demo.App</AssemblyName>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.9" />
                <ProjectReference Include="..\Domain\Domain.csproj" />
              </ItemGroup>
            </Project>
            """);
        var file = CreateScannedFile(projectPath, "src/App/App.csproj", ".csproj");

        var segment = new DotNetProjectSegmentExtractor().Extract(file).Single();

        segment.Kind.Should().Be(SegmentKind.Project);
        segment.Name.Should().Be("Demo.App");
        segment.ProjectName.Should().Be("Demo.App");
        segment.Metadata.Should().ContainKey("target_framework").WhoseValue.Should().Be("net10.0");
        segment.Metadata.Should().ContainKey("package_refs").WhoseValue.Should().Contain("Microsoft.Extensions.Hosting 10.0.9");
        segment.Metadata.Should().ContainKey("project_refs").WhoseValue.Should().Be("Domain");
    }

    [Test]
    public void SolutionExtractor_ReadsSlnxProjectReferences()
    {
        using var temp = new TempDirectory();
        var solutionPath = temp.WriteFile(
            "SharpMemory.slnx",
            """
            <Solution>
              <Project Path="src/App/App.csproj" />
              <Project Path="src/Core/Core.csproj" />
            </Solution>
            """);
        var file = CreateScannedFile(solutionPath, "SharpMemory.slnx", ".slnx");

        var segment = new SolutionSegmentExtractor().Extract(file).Single();

        segment.Kind.Should().Be(SegmentKind.Solution);
        segment.Name.Should().Be("SharpMemory");
        segment.Metadata.Should().ContainKey("project_refs").WhoseValue.Should().Be("App, Core");
    }

    [Test]
    public void SolutionExtractor_ReturnsNoSegmentsWhenSolutionHasNoProjects()
    {
        using var temp = new TempDirectory();
        var solutionPath = temp.WriteFile("Empty.slnx", "<Solution />");
        var file = CreateScannedFile(solutionPath, "Empty.slnx", ".slnx");

        var segments = new SolutionSegmentExtractor().Extract(file);

        segments.Should().BeEmpty();
    }

    private static ScannedFile CreateScannedFile(string fullPath, string relativePath, string extension) =>
        new()
        {
            RepoId = "repo",
            FullPath = fullPath,
            RelativePath = relativePath,
            Extension = extension,
            ContentHash = "hash",
        };
}
