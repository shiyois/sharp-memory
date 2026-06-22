using System.Net;
using System.Text.Json;
using FluentAssertions;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Integration;

[TestFixture]
public sealed class SharpMemoryServiceHttpTests
{
    [Test]
    public async Task Service_ReturnsRepositoriesAndSearchResultsFromHttpApi()
    {
        using var repo = CreateRepository();
        using var storage = new TempDirectory();
        await using var app = await SharpMemoryAppProcess.Start(storage.Path, repo.Path);
        using var client = new HttpClient { BaseAddress = app.BaseAddress };

        using var repositoriesResponse = await client.GetAsync("/api/memory/repositories");
        using var searchResponse = await client.GetAsync("/api/memory/search?query=Worker&project=Demo.Service&topN=10");

        repositoriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var repositories = JsonDocument.Parse(await repositoriesResponse.Content.ReadAsStringAsync());
        repositories.RootElement.GetArrayLength().Should().Be(1);
        repositories.RootElement[0].GetProperty("repoName").GetString().Should().Be(Path.GetFileName(repo.Path));

        using var search = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
        search.RootElement.GetArrayLength().Should().BeGreaterThan(0);
        var workerResult = search.RootElement.EnumerateArray()
            .FirstOrDefault(result =>
                result.GetProperty("segment").GetProperty("name").GetString() == "Worker");

        workerResult.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        workerResult.GetProperty("segment").GetProperty("kind").GetString().Should().Be("Class");
        workerResult.GetProperty("segment").GetProperty("projectName").GetString().Should().Be("Demo.Service");
    }

    [Test]
    public async Task Service_RejectsEmptySearchAndExposesRefreshStatus()
    {
        using var repo = CreateRepository();
        using var storage = new TempDirectory();
        await using var app = await SharpMemoryAppProcess.Start(storage.Path, repo.Path);
        using var client = new HttpClient { BaseAddress = app.BaseAddress };

        using var badSearch = await client.GetAsync("/api/memory/search?query=");
        using var statusResponse = await client.GetAsync("/api/memory/snapshot/refresh/status");

        badSearch.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var status = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync());
        status.RootElement.GetProperty("isRunning").GetBoolean().Should().BeFalse();
        status.RootElement.GetProperty("lastCompletedSnapshot").ValueKind.Should().Be(JsonValueKind.Object);
    }

    private static TempDirectory CreateRepository()
    {
        var repo = new TempDirectory();
        repo.WriteFile(
            "Demo.Service.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>Demo.Service</AssemblyName>
              </PropertyGroup>
            </Project>
            """);
        repo.WriteFile(
            "src/Worker.cs",
            """
            namespace Demo.Service;

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

        return repo;
    }
}
