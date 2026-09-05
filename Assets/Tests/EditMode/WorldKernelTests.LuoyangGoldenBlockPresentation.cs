using System;
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
        public void LuoyangGoldenBlock_IsDeterministicAuditableAndPresentationOnly()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            var fingerprint = source.LayoutPackage.DeclaredLayoutFingerprint;
            var first = new CountyGoldenBlockPresentationPlan(
                source.LayoutPackage);
            var second = new CountyGoldenBlockPresentationPlan(
                source.LayoutPackage);
            Console.WriteLine("GOLDEN_BLOCK rows=" + first.MinimumRow +
                              ".." + first.MaximumRow + " columns=" +
                              first.MinimumColumn + ".." +
                              first.MaximumColumn + " signature=" +
                              first.StableSignature);
            var formalIds = source.LayoutPackage.Facilities.Select(item =>
                item.FacilityId).ToHashSet(StringComparer.Ordinal);

            Assert.That(first.IsDerivedPresentationOnly, Is.True);
            Assert.That(first.MaximumRow - first.MinimumRow + 1,
                Is.EqualTo(CountyGoldenBlockPresentationPlan.BlockSizeCells));
            Assert.That(first.MaximumColumn - first.MinimumColumn + 1,
                Is.EqualTo(CountyGoldenBlockPresentationPlan.BlockSizeCells));
            Assert.That(first.Lots.Count,
                Is.EqualTo(CountyGoldenBlockPresentationPlan.LotCount));
            Assert.That(first.Lots.Select(item => item.Archetype).Distinct(),
                Is.EquivalentTo(Enum.GetValues(
                    typeof(CountyGoldenBlockArchetype))));
            Assert.That(first.Lots.All(item =>
                    item.IsDerivedPresentationOnly &&
                    !string.IsNullOrWhiteSpace(
                        item.PresentationProfileId) &&
                    item.ModulePlan != null &&
                    item.CenterRow >= first.MinimumRow &&
                    item.CenterRow <= first.MaximumRow + 1 &&
                    item.CenterColumn >= first.MinimumColumn &&
                    item.CenterColumn <= first.MaximumColumn + 1), Is.True);
            Assert.That(first.SourceFacilityIds, Is.Not.Empty);
            Assert.That(first.SourceFacilityIds.All(formalIds.Contains),
                Is.True);
            Assert.That(first.Lots.Where(item =>
                    !string.IsNullOrEmpty(item.SourceFacilityId)).All(item =>
                    formalIds.Contains(item.SourceFacilityId)), Is.True);
            Assert.That(first.StableSignature, Is.EqualTo(
                second.StableSignature));
            Assert.That(first.Lots.Select(item => new
                {
                    item.CenterRow, item.CenterColumn, item.Archetype,
                    item.RotationQuarterTurns, item.Variant,
                    item.PresentationProfileId,
                    ModuleSignature = item.ModulePlan.StableSignature,
                    item.SourceFacilityId
                }), Is.EqualTo(second.Lots.Select(item => new
                {
                    item.CenterRow, item.CenterColumn, item.Archetype,
                    item.RotationQuarterTurns, item.Variant,
                    item.PresentationProfileId,
                    ModuleSignature = item.ModulePlan.StableSignature,
                    item.SourceFacilityId
                })));
            Assert.That(source.LayoutPackage.DeclaredLayoutFingerprint,
                Is.EqualTo(fingerprint));
        }

        [Test]
        public void LuoyangGoldenBlockV2_ProfilesAreDistinctAndStable()
        {
            var catalog = CountyBuildingPresentationProfileCatalog
                .HanLuoyangV2;

            Assert.That(catalog.Profiles.Count, Is.EqualTo(5));
            Assert.That(catalog.Profiles.Select(item => item.Archetype),
                Is.EquivalentTo(Enum.GetValues(
                    typeof(CountyGoldenBlockArchetype))));
            Assert.That(catalog.Profiles.Select(item => item.RoofFamily)
                .Distinct().Count(), Is.EqualTo(5));
            Assert.That(catalog.Profiles.All(item =>
                item.Modules.Count >= 5 &&
                item.RoofVariationSet.Count == 3 &&
                !string.IsNullOrWhiteSpace(item.StableVariationRule) &&
                item.FarPresentationMode == "aggregate-silhouette" &&
                item.MidPresentationMode == "compound-readable" &&
                item.NearPresentationMode == "compound-modules"), Is.True);
            foreach (var profile in catalog.Profiles)
            {
                var first = profile.Resolve("facility.test.stable", 3);
                var second = profile.Resolve("facility.test.stable", 3);
                Assert.That(first.StableSignature,
                    Is.EqualTo(second.StableSignature), profile.ProfileId);
                Assert.That(first.Modules.Select(item => item.ModuleId),
                    Is.EqualTo(second.Modules.Select(item => item.ModuleId)),
                    profile.ProfileId);
            }
            Assert.That(WorldState.CurrentSchemaVersion, Is.EqualTo(79));
        }

        [Test]
        public void LuoyangGoldenBlockV2_UsesSixtyFourFormalCellsNotLots()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            var plan = new CountyGoldenBlockPresentationPlan(
                source.LayoutPackage);
            var formalCells = Enumerable.Range(plan.MinimumRow,
                    CountyGoldenBlockPresentationPlan.BlockSizeCells)
                .SelectMany(row => Enumerable.Range(plan.MinimumColumn,
                    CountyGoldenBlockPresentationPlan.BlockSizeCells)
                    .Select(column => new PlanningCellCoord(row, column)))
                .ToArray();

            Assert.That(formalCells.Length, Is.EqualTo(64));
            Assert.That(plan.Lots.Count, Is.EqualTo(16));
            Assert.That(formalCells.Distinct().Count(), Is.EqualTo(64));
            Assert.That(CountyGoldenBlockPresentationPlan.BlockSizeCells *
                DualScaleCountySpatialContractV1.PlanningCellSizeMetres,
                Is.EqualTo(400));
        }
    }
}
