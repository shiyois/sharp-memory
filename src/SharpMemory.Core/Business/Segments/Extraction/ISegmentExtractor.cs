using SharpMemory.Core.Business.Segments.Models;
using SharpMemory.Core.Common.Models;

namespace SharpMemory.Core.Business.Segments.Extraction;

public interface ISegmentExtractor
{
    /// <summary>Returns true if this extractor can process the given file extension (e.g. <c>.cs</c>).</summary>
    bool CanExtract(string extension);

    /// <summary>Produces segments from a file. Use <see cref="ScannedFile.FullPath"/> to open a read stream.</summary>
    IEnumerable<MemorySegment> Extract(ScannedFile file);
}
