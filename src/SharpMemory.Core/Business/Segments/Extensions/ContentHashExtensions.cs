using System.Security.Cryptography;
using System.Text;

namespace SharpMemory.Core.Business.Segments.Extensions;

public static class ContentHashExtensions
{
    public static async Task<string> ComputeHash(this string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await MD5.HashDataAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ToSegmentId(this string stableKey)
    {
        var bytes = Encoding.UTF8.GetBytes(stableKey);
        return Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();
    }
}
