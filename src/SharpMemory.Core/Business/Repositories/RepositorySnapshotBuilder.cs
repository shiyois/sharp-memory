using Microsoft.Extensions.Logging;
using SharpMemory.Core.Business.Repositories.Models;
using SharpMemory.Core.Business.Segments;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Interfaces;
using SharpMemory.Core.Storage.Models;

namespace SharpMemory.Core.Business.Repositories;

public sealed class RepositorySnapshotBuilder(
    SegmentsCreator segmentsCreator,
    ISnapshotWriter snapshotWriter,
    ILogger<RepositorySnapshotBuilder> logger)
{
    public async Task<IndexedRepository> BuildSnapshot(
        string snapshotId,
        string repoPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rootPath = Path.GetFullPath(repoPath);
        var repoId = RepositoryIdentity.CreateId(rootPath);
        var repoName = RepositoryIdentity.CreateName(rootPath);

        logger.LogInformation("Building repository snapshot {RepoName} at {RootPath}", repoName, rootPath);

        var segments = await BuildSegments(rootPath, repoId, cancellationToken);

        await snapshotWriter.WriteRepository(
            BuildRepositoryInfo(snapshotId, repoId, repoName, rootPath, segments),
            cancellationToken);

        foreach (var fileInfo in BuildFileInfos(snapshotId, repoId, segments))
        {
            await snapshotWriter.WriteFile(fileInfo, cancellationToken);
        }

        await snapshotWriter.WriteSegments(snapshotId, segments, cancellationToken);

        return new IndexedRepository
        {
            RepoId = repoId,
            RepoName = repoName,
            RootPath = rootPath,
            Segments = segments,
        };
    }

    private async Task<IReadOnlyList<MemorySegment>> BuildSegments(
        string rootPath,
        string repoId,
        CancellationToken cancellationToken)
    {
        var segments = new List<MemorySegment>();
        await foreach (var segment in segmentsCreator.Create(rootPath, repoId)
            .WithCancellation(cancellationToken))
        {
            segments.Add(segment);
        }

        return segments;
    }

    private static SnapshotRepositoryInfo BuildRepositoryInfo(
        string snapshotId,
        string repoId,
        string repoName,
        string rootPath,
        IReadOnlyList<MemorySegment> segments) =>
        new()
        {
            SnapshotId = snapshotId,
            RepoId = repoId,
            RepoName = repoName,
            RepoPath = rootPath,
            FileCount = segments.Select(static s => s.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            SegmentCount = segments.Count,
            IndexedAt = DateTimeOffset.UtcNow,
        };

    private static IEnumerable<SnapshotFileInfo> BuildFileInfos(
        string snapshotId,
        string repoId,
        IReadOnlyList<MemorySegment> segments)
    {
        foreach (var fileGroup in segments.GroupBy(static s => s.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            var first = fileGroup.First();
            yield return new SnapshotFileInfo
            {
                SnapshotId = snapshotId,
                RepoId = repoId,
                FilePath = fileGroup.Key,
                ContentHash = first.ContentHash,
                ProjectName = first.ProjectName,
                SegmentCount = fileGroup.Count(),
            };
        }
    }
}
