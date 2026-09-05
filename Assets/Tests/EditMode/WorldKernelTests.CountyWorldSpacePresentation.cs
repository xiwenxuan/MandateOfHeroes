using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void CountyWorldSpacePresentation_IndexesFormalLayoutWithoutMutation()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            var layout = source.LayoutPackage;
            var stack = new CountyMapPresentationStack(layout,
                source.Prototype.Partition);
            var plan = new CountyWorldSpacePresentationPlan(layout,
                source.Prototype.Partition, stack);
            var fingerprint = layout.DeclaredLayoutFingerprint;

            var first = plan.CreateSummary();
            var second = plan.CreateSummary();

            Assert.That(first.TerrainChunkCount, Is.EqualTo(50));
            Assert.That(first.FacilityCount, Is.EqualTo(2084));
            Assert.That(first.RoadSegmentCount, Is.EqualTo(
                Luoyang50mCountyLayoutIds.RoadEdgeCount));
            Assert.That(first.CanalSegmentCount, Is.EqualTo(
                Luoyang50mCountyLayoutIds.CanalEdgeCount));
            Assert.That(first.FortificationSegmentCount, Is.EqualTo(
                Luoyang50mCountyLayoutIds.FortificationEdgeCount));
            Assert.That(first.PlanningCellGameObjectCount, Is.Zero);
            Assert.That(first.MaximumLocalPlanningGridCellCount,
                Is.EqualTo(625));
            Assert.That(first.UrbanCandidateHullVisibleByDefault, Is.False);
            Assert.That(first.IsDerivedPresentationOnly, Is.True);
            Assert.That(first.DeterministicSignature,
                Is.EqualTo(second.DeterministicSignature));
            Assert.That(layout.DeclaredLayoutFingerprint,
                Is.EqualTo(fingerprint));
        }

        [Test]
        public void CountyWorldSpacePresentation_UsesLocalGridAndTerrainSampling()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            var plan = new CountyWorldSpacePresentationPlan(
                source.LayoutPackage, source.Prototype.Partition,
                new CountyMapPresentationStack(source.LayoutPackage,
                    source.Prototype.Partition));

            var cells = plan.LocalPlanningGrid(160, 320);
            Assert.That(cells.Count, Is.EqualTo(625));
            Assert.That(plan.LocalPlanningGrid(0, 0).Count,
                Is.LessThan(625));
            Assert.That(plan.SurfaceHeight(100.5f, 100.5f),
                Is.EqualTo(plan.SurfaceHeight(100.5f, 100.5f)));
            Assert.That(CountyWorldSpacePresentationPlan.StableModulo(
                127, 281, 7), Is.EqualTo(
                CountyWorldSpacePresentationPlan.StableModulo(
                    127, 281, 7)));
        }

        [Test]
        public void CountyStrategicSandboxV2_FarAggregatesCoverOrdinaryFacilitiesOnce()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            var stack = new CountyMapPresentationStack(source.LayoutPackage,
                source.Prototype.Partition);
            var plan = new CountyWorldSpacePresentationPlan(
                source.LayoutPackage, source.Prototype.Partition, stack);
            var landmarkIds = new HashSet<string>(plan.FarLandmarks.Select(
                item => item.FacilityId), StringComparer.Ordinal);
            var expected = source.LayoutPackage.Facilities.Where(item =>
                    !CountyWorldSpacePresentationPlan
                        .IsSpecializedInfrastructure(item.DefinitionId) &&
                    !CountyWorldSpacePresentationPlan
                        .IsAgriculturalFacility(item) &&
                    !landmarkIds.Contains(item.FacilityId))
                .Select(item => item.FacilityId)
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();
            var actual = plan.FarAggregates.SelectMany(item =>
                    item.FacilityIds)
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();

            Assert.That(plan.FarAggregates, Is.Not.Empty);
            Assert.That(plan.FarAggregates.Count,
                Is.LessThan(expected.Length));
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(actual.Length));
            Assert.That(plan.FarLandmarks.Select(item => item.FacilityId),
                Is.EquivalentTo(stack.FarFacilities.Select(item =>
                    item.Facility.FacilityId)));
            Assert.That(plan.CreateSummary()
                    .FarSuppressedOrdinaryFacilityCount,
                Is.EqualTo(expected.Length));
        }

        [Test]
        public void CountyStrategicSandboxV2_AggregationAndTerrainWindingAreStable()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            CountyWorldSpacePresentationPlan CreatePlan() =>
                new CountyWorldSpacePresentationPlan(source.LayoutPackage,
                    source.Prototype.Partition,
                    new CountyMapPresentationStack(source.LayoutPackage,
                        source.Prototype.Partition));
            var first = CreatePlan();
            var second = CreatePlan();
            var triangles = new List<int>();

            CountyWorldSpacePresentationPlan
                .AppendUpwardTerrainQuadTriangles(triangles, 0, 2);

            Assert.That(triangles, Is.EqualTo(new[] { 0, 1, 3, 1, 4, 3 }));
            Assert.That(first.FarAggregates.Select(item =>
                    item.StableSignature),
                Is.EqualTo(second.FarAggregates.Select(item =>
                    item.StableSignature)));
            Assert.That(first.FarAggregates.Select(item => item.Kind)
                    .Distinct().Count(), Is.GreaterThanOrEqualTo(4));
            Assert.That(first.CreateSummary().PresentationVersion,
                Is.EqualTo(CountyWorldSpacePresentationPlan.Version));
        }
    }
}
