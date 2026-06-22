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
        var settingsRepo = workspace.CreateDirectory("settings-repo");
        var cliRepo = workspace.CreateDirectory("cli-repo");
        workspace.WriteFile("SharpMemory.slnx", "<Solution />");
        workspace.WriteFile(
            "settings.json",
            $$"""
            {
              "repositories": [
                { "path": "{{settingsRepo.Replace("\\", "\\\\")}}" }
              ]
            }
            """);

        var settings = ParseFrom(workspace.Path, ["--repo", cliRepo]);

        settings.RepositoryPaths.Should().ContainSingle().Which.Should().Be(Path.GetFullPath(cliRepo));
        settings.StorageRoot.Should().Be(Path.GetFullPath(workspace.Path));
        settings.UseStdio.Should().BeFalse();
    }

    [Test]
    public void Parse_LoadsRepositoryPathsFromSettingsJson()
    {
        using var workspace = new TempDirectory();
        var firstRepo = workspace.CreateDirectory("repo-one");
        var secondRepo = workspace.CreateDirectory("repo-two");
        workspace.WriteFile("SharpMemory.slnx", "<Solution />");
        workspace.WriteFile(
            "settings.json",
            $$"""
            {
              "repositories": [
                "{{firstRepo.Replace("\\", "\\\\")}}",
                { "path": "{{secondRepo.Replace("\\", "\\\\")}}" }
              ]
            }
            """);

        var settings = ParseFrom(workspace.Path, []);

        settings.RepositoryPaths.Should().Equal(
            Path.GetFullPath(firstRepo),
            Path.GetFullPath(secondRepo));
    }

    [Test]
    public void Parse_FallsBackToStorageRootWhenNoRepositoriesConfigured()
    {
        using var workspace = new TempDirectory();
        workspace.WriteFile("SharpMemory.slnx", "<Solution />");

        var settings = ParseFrom(workspace.Path, ["--stdio"]);

        settings.UseStdio.Should().BeTrue();
        settings.RepositoryPaths.Should().ContainSingle().Which.Should().Be(Path.GetFullPath(workspace.Path));
    }

    private static Settings ParseFrom(string currentDirectory, string[] args)
    {
        var previous = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = currentDirectory;
            return SettingsParser.Parse(args);
        }
        finally
        {
            Environment.CurrentDirectory = previous;
        }
    }
}
