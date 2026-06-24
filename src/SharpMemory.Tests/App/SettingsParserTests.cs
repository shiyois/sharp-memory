using FluentAssertions;
using SharpMemory.App.Infrastructure.Settings;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.App;

[TestFixture]
[NonParallelizable]
public sealed class SettingsParserTests
{
    [Test]
    public void Parse_UsesCliRepositoriesBeforeSettingsFile()
    {
        using var workspace = new TempDirectory();
        using var home = new TempDirectory();
        var settingsRepo = workspace.CreateDirectory("settings-repo");
        var cliRepo = workspace.CreateDirectory("cli-repo");
        workspace.WriteFile("SharpMemory.slnx", "<Solution />");
        workspace.WriteFile(
            "settings.json",
            $$"""
            {
              "repositories": [
                "{{settingsRepo.Replace("\\", "\\\\")}}"
              ]
            }
            """);

        var settings = ParseFrom(workspace.Path, home.Path, ["--repo", cliRepo]);

        settings.RepositoryPaths.Should().ContainSingle().Which.Should().Be(Path.GetFullPath(cliRepo));
        settings.StorageRoot.Should().Be(Path.GetFullPath(home.Path));
        settings.UseStdio.Should().BeFalse();
    }

    [Test]
    public void Parse_LoadsRepositoryPathsFromSettingsJson()
    {
        using var workspace = new TempDirectory();
        using var home = new TempDirectory();
        var firstRepo = workspace.CreateDirectory("repo-one");
        var secondRepo = workspace.CreateDirectory("repo-two");
        home.WriteFile(
            "settings.json",
            $$"""
            {
              "repositories": [
                "{{firstRepo.Replace("\\", "\\\\")}}",
                "{{secondRepo.Replace("\\", "\\\\")}}"
              ]
            }
            """);

        var settings = ParseFrom(workspace.Path, home.Path, []);

        settings.RepositoryPaths.Should().Equal(
            Path.GetFullPath(firstRepo),
            Path.GetFullPath(secondRepo));
        settings.StorageRoot.Should().Be(Path.GetFullPath(home.Path));
    }

    [Test]
    public void Parse_WhenNoRepositoriesConfigured_ReturnsEmptyRepositoryList()
    {
        using var workspace = new TempDirectory();
        using var home = new TempDirectory();

        var settings = ParseFrom(workspace.Path, home.Path, ["--stdio"]);

        settings.UseStdio.Should().BeTrue();
        settings.StorageRoot.Should().Be(Path.GetFullPath(home.Path));
        settings.RepositoryPaths.Should().BeEmpty();
    }

    [Test]
    public void Parse_UsesCurrentDirectorySettingsForCompatibility()
    {
        using var workspace = new TempDirectory();
        using var home = new TempDirectory();
        var repo = workspace.CreateDirectory("repo");
        workspace.WriteFile(
            "settings.json",
            $$"""
            {
              "repositories": [
                "{{repo.Replace("\\", "\\\\")}}"
              ]
            }
            """);

        var settings = ParseFrom(workspace.Path, home.Path, []);

        settings.RepositoryPaths.Should().ContainSingle().Which.Should().Be(Path.GetFullPath(repo));
        settings.StorageRoot.Should().Be(Path.GetFullPath(home.Path));
    }

    private static Settings ParseFrom(string currentDirectory, string homePath, string[] args)
    {
        var previous = Environment.CurrentDirectory;
        var previousHome = Environment.GetEnvironmentVariable(SharpMemoryPaths.HomeEnvironmentVariable);
        try
        {
            Environment.CurrentDirectory = currentDirectory;
            Environment.SetEnvironmentVariable(SharpMemoryPaths.HomeEnvironmentVariable, homePath);
            return SettingsParser.Parse(args);
        }
        finally
        {
            Environment.CurrentDirectory = previous;
            Environment.SetEnvironmentVariable(SharpMemoryPaths.HomeEnvironmentVariable, previousHome);
        }
    }
}
