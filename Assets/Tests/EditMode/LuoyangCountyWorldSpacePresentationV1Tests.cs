using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangCountyWorldSpacePresentationV1Tests
    {
        [Test]
        [Timeout(300_000)]
        public void CountyRenderer_BuildsLayeredWorldSpaceCacheWithoutCellObjects()
        {
            var root = new GameObject("County World-Space EditMode Test");
            var cameraObject = new GameObject("County Camera");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                var planning = root.AddComponent<
                    LuoyangCountyPlanningPresentationController>();
                Assert.That(planning.Begin(
                    Luoyang50mCountySpatialPrototypeIds.CountyId,
                    CountySubViewMode.Overview), Is.True,
                    planning.LastError);
                Assert.That(planning.EnsureWorldSpacePresentation(camera),
                    Is.True, planning.LastError);
                var world = planning.WorldSpacePresentation;
                Assert.That(world.IsBuilt, Is.True);
                Assert.That(world.DebugVisible, Is.False);
                Assert.That(world.Summary.TerrainChunkCount, Is.EqualTo(50));
                Assert.That(world.Summary.FacilityCount, Is.EqualTo(2084));
                Assert.That(world.Summary.RoadSegmentCount, Is.EqualTo(334));
                Assert.That(world.Summary.CanalSegmentCount, Is.EqualTo(17));
                Assert.That(world.Summary.FortificationSegmentCount,
                    Is.EqualTo(144));
                Assert.That(world.Summary.PresentationVersion,
                    Is.EqualTo(CountyWorldSpacePresentationPlan.Version));
                Assert.That(world.Summary.FarAggregateCount,
                    Is.GreaterThan(0).And.LessThan(
                        world.Summary.FarSuppressedOrdinaryFacilityCount));
                Assert.That(world.Summary.FarLandmarkCount,
                    Is.GreaterThan(0));
                Assert.That(world.FarOrdinaryFacilityDetailObjectCount,
                    Is.Zero);
                Assert.That(world.FarAggregateRendererCount,
                    Is.GreaterThan(0).And.LessThan(256));
                Assert.That(world.RendererCount,
                    Is.GreaterThan(20).And.LessThan(1000));
                Assert.That(world.WorldRoot.Find("Terrain"), Is.Not.Null);
                Assert.That(world.WorldRoot.Find("Water"), Is.Not.Null);
                Assert.That(world.WorldRoot.Find("Roads"), Is.Not.Null);
                Assert.That(world.WorldRoot.Find("Facilities"), Is.Not.Null);
                Assert.That(world.WorldRoot.Find("Planning Overlay"),
                    Is.Not.Null);
                Assert.That(planning.PlanningCellGameObjectCount, Is.Zero);

                var terrain = world.WorldRoot.Find("Terrain")
                    .GetComponentsInChildren<MeshFilter>(true);
                Assert.That(terrain, Is.Not.Empty);
                Assert.That(terrain.SelectMany(item =>
                        item.sharedMesh.normals).Average(item => item.y),
                    Is.GreaterThan(0.25f),
                    "V2 county terrain must be front-facing and lit.");
                var aggregateRoot = world.WorldRoot.Find(
                    "Urban Fabric/Far Aggregates");
                Assert.That(aggregateRoot, Is.Not.Null);
                Assert.That(aggregateRoot.childCount,
                    Is.GreaterThan(1).And.LessThan(256));
                Assert.That(aggregateRoot.Cast<Transform>().All(item =>
                    item.name.StartsWith("Far Urban Chunk ")),
                    Is.True, "Far aggregates must be chunk-batched renderers.");

                planning.SetPresentationMode(CountySubViewMode.Planning);
                world.Show(new Rect(0f, 0f, 1280f, 720f));
                Assert.That(world.PlanningGridGameObjectCount,
                    Is.LessThanOrEqualTo(1));
                Assert.That(world.DetailedFacilityObjectCount,
                    Is.LessThanOrEqualTo(
                        CountyWorldSpacePresentationPlan
                            .MaximumNearDetailedFacilities));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
