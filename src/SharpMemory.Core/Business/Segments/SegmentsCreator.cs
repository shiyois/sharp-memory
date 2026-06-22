using SharpMemory.Core.Business.Segments.Extensions;
using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Business.Segments;

public sealed class SegmentsCreator(RepositoryScanner scanner, FileSegmenter segmenter)
{
    public async IAsyncEnumerable<MemorySegment> Create(string rootPath, string repoId)
    {
        await foreach (var filePath in scanner.Scan(rootPath))
        {
            var relativePath = Path.GetRelativePath(rootPath, filePath).ToUnixPath();

            await foreach (var segment in segmenter.Segment(filePath, relativePath, rootPath, repoId))
            {
                yield return segment;
            }
        }
    }
}
