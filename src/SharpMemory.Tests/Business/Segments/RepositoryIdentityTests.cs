using FluentAssertions;
using SharpMemory.Core.Business.Segments;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Business.Segments;

[TestFixture]
public sealed class RepositoryIdentityTests
{
    [Test]
    public void CreateId_NormalizesTrailingDirectorySeparators()
    {
        using var temp = new TempDirectory();

        var withoutTrailingSeparator = RepositoryIdentity.CreateId(temp.Path);
        var withTrailingSeparator = RepositoryIdentity.CreateId(temp.Path + Path.DirectorySeparatorChar);

        withTrailingSeparator.Should().Be(withoutTrailingSeparator);
        withTrailingSeparator.Should().HaveLength(16);
    }

    [Test]
    public void CreateName_ReturnsRepositoryDirectoryName()
    {
        using var temp = new TempDirectory();

        var name = RepositoryIdentity.CreateName(temp.Path);

        name.Should().Be(Path.GetFileName(temp.Path));
    }
}
