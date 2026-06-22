using FluentAssertions;
using SharpMemory.Core.Business.Segments.Validation;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Business.Segments;

[TestFixture]
public sealed class GitIgnoreMatcherTests
{
    [Test]
    public void IsIgnored_AppliesLastMatchingRule()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(
            ".gitignore",
            """
            bin/
            *.secret
            !keep.secret
            """);

        var matcher = GitIgnoreMatcher.Load(temp.Path);

        matcher.Should().NotBeNull();
        matcher!.IsIgnored("src/bin", isDirectory: true).Should().BeTrue();
        matcher.IsIgnored("src/app.secret", isDirectory: false).Should().BeTrue();
        matcher.IsIgnored("src/keep.secret", isDirectory: false).Should().BeFalse();
        matcher.IsIgnored("src/Program.cs", isDirectory: false).Should().BeFalse();
    }
}
