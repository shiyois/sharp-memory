namespace SharpMemory.Core.Business.Segments.Extensions;

public static class PathExtensions
{
    public static string ToUnixPath(this string path) => path.Replace('\\', '/');
}
