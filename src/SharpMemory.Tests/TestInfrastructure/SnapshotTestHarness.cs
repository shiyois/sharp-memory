using Microsoft.Extensions.Logging.Abstractions;
using SharpMemory.Core.Business.Queries;
using SharpMemory.Core.Business.Repositories;
using SharpMemory.Core.Business.Segments;
using SharpMemory.Core.Business.Segments.Extraction;
using SharpMemory.Core.Business.Segments.Relationships;
using SharpMemory.Core.Business.Snapshots;
using SharpMemory.Core.Business.Snapshots.Models;
using SharpMemory.Core.Storage.Sqlite;

namespace SharpMemory.Tests.TestInfrastructure;

internal sealed class SnapshotTestHarness
{
    private readonly SqliteConnectionFactory connectionFactory;

    public SnapshotTestHarness(string storageRoot, IReadOnlyList<string> repositoryPaths)
    {
        connectionFactory = new SqliteConnectionFactory(
            new SqliteStorageOptions { StorageRoot = storageRoot });

        var writer = new SqliteSnapshotWriter(connectionFactory);
        var publisher = new SqliteSnapshotPublisher(connectionFactory);
        var lifecycle = new SnapshotLifecycleService(writer, publisher);
        var scanner = new RepositoryScanner();
        var segmenter = new FileSegmenter(
            [
                new CSharpSegmentExtractor(),
                new DotNetProjectSegmentExtractor(),
                new SolutionSegmentExtractor(),
            ],
            NullLogger<FileSegmenter>.Instance);
        var segmentsCreator = new SegmentsCreator(scanner, segmenter);
        var repositoryBuilder = new RepositorySnapshotBuilder(
            segmentsCreator,
            writer,
            NullLogger<RepositorySnapshotBuilder>.Instance);

        Builder = new MemorySnapshotBuilder(
            repositoryBuilder,
            new SegmentRelationshipBuilder(),
            lifecycle,
            writer,
            new MemorySnapshotOptions { RepositoryPaths = [.. repositoryPaths] },
            NullLogger<MemorySnapshotBuilder>.Instance);

        var activeSnapshotProvider = new SqliteActiveSnapshotProvider(connectionFactory);
        var segmentStore = new SqliteSegmentStore(connectionFactory);
        var relationshipStore = new SqliteRelationshipStore(connectionFactory);
        var statusStore = new SqliteSnapshotStatusStore(connectionFactory);

        Queries = new MemoryQueryService(
            new SegmentQueryService(activeSnapshotProvider, segmentStore, statusStore),
            new FileContextQueryService(activeSnapshotProvider, segmentStore, relationshipStore),
            new RelationshipQueryService(activeSnapshotProvider, segmentStore, relationshipStore),
            new GraphQueryService(activeSnapshotProvider, segmentStore, relationshipStore));
    }

    public MemorySnapshotBuilder Builder { get; }

    public MemoryQueryService Queries { get; }
}
