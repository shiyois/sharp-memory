using FluentAssertions;
using SharpMemory.Core.Business.Segments;
using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Business.Segments;

[TestFixture]
public sealed class RepositoryScannerTests
{
    [Test]
    public async Task Scan_UsesRepositoryGitIgnoreAndSkipsServiceDirectories()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(
            ".gitignore",
            """
            ignored/
            *.tmp
            """);
        temp.WriteFile("src/Program.cs", "class Program { }");
        temp.WriteFile("ignored/Ignored.cs", "class Ignored { }");
        temp.WriteFile("src/cache.tmp", "temp");
        temp.WriteFile("bin/Generated.cs", "class Generated { }");
        temp.WriteFile("node_modules/pkg/index.js", "console.log('skip');");

        var scanner = new RepositoryScanner();

        var files = new List<string>();
        await foreach (var file in scanner.Scan(temp.Path))
        {
            files.Add(Path.GetRelativePath(temp.Path, file).ToUnixPath());
        }

        files.Should().Contain("src/Program.cs");
        files.Should().NotContain("ignored/Ignored.cs");
        files.Should().NotContain("src/cache.tmp");
        files.Should().NotContain("bin/Generated.cs");
        files.Should().NotContain("node_modules/pkg/index.js");
    }
}
