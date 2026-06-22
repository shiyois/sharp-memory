using FluentAssertions;
using SharpMemory.Core.Business.Segments.Relationships;
using SharpMemory.Core.Common.Models;
using SharpMemory.Core.Storage.Models;
using SharpMemory.Tests.TestInfrastructure;

namespace SharpMemory.Tests.Business.Segments;

[TestFixture]
public sealed class SegmentRelationshipBuilderTests
{
    [Test]
    public void Build_CreatesProjectTypeAndCallRelationships()
    {
        var project = SegmentFactory.Create(SegmentKind.Project, "App", projectName: "App");
        var contract = SegmentFactory.Create(SegmentKind.Interface, "IService", projectName: "App");
        var worker = SegmentFactory.Create(
            SegmentKind.Class,
            "Worker",
            projectName: "App",
            metadata: new Dictionary<string, string> { ["baseTypes"] = "IService" });
        var helper = SegmentFactory.Create(SegmentKind.Method, "Helper", projectName: "App", containerName: "Worker");
        var run = SegmentFactory.Create(
            SegmentKind.Method,
            "Run",
            projectName: "App",
            containerName: "Worker",
            metadata: new Dictionary<string, string> { ["called_methods"] = "Helper" });

        var relationships = new SegmentRelationshipBuilder()
            .Build([project, contract, worker, helper, run]);

        relationships.Should().Contain(r =>
            r.FromSegmentId == worker.SegmentId
            && r.ToSegmentId == project.SegmentId
            && r.Type == RelationshipType.DefinedIn);
        relationships.Should().Contain(r =>
            r.FromSegmentId == worker.SegmentId
            && r.ToSegmentId == contract.SegmentId
            && r.Type == RelationshipType.Implements);
        relationships.Should().Contain(r =>
            r.FromSegmentId == run.SegmentId
            && r.ToSegmentId == helper.SegmentId
            && r.Type == RelationshipType.Calls);
    }
}
