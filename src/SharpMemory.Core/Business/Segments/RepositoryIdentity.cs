using System.Security.Cryptography;
using System.Text;

namespace SharpMemory.Core.Business.Segments;

public static class RepositoryIdentity
{
    public static string CreateId(string rootPath)
    {
        var normalized = Path.GetFullPath(rootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace('\\', '/')
            .ToUpperInvariant();

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    public static string CreateName(string rootPath) =>
        Path.GetFileName(
            Path.GetFullPath(rootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
