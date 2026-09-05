using System;
using System.IO;
using System.Linq;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void LuoyangCitywideBuildingLanguage_ClassifiesFormalFacilitiesDeterministically()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            var stack = new CountyMapPresentationStack(source.LayoutPackage,
                source.Prototype.Partition);
            var worldPlan = new CountyWorldSpacePresentationPlan(
                source.LayoutPackage, source.Prototype.Partition, stack);
            var fingerprint = source.LayoutPackage.DeclaredLayoutFingerprint;

            CountyCitywideBuildingLanguagePlan Build() => new
                CountyCitywideBuildingLanguagePlan(source.LayoutPackage,
                    worldPlan.FarLandmarks.Select(item => item.FacilityId));
            var first = Build();
            var second = Build();
            var expected = source.LayoutPackage.Facilities.Count(
                CountyCitywideBuildingLanguagePlan
                    .IsBuildingLanguageCandidate);

            Assert.That(first.SourceFacilityCount, Is.EqualTo(2084));
            Assert.That(first.Entries.Count, Is.EqualTo(expected));
            Assert.That(first.Entries.Count, Is.EqualTo(1056));
            Assert.That(first.Entries.Select(item => item.FacilityId)
                .Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(first.Entries.Count));
            Assert.That(first.FacilityCountByProfile.Count, Is.EqualTo(5));
            Assert.That(first.FacilityCountByProfile.Values.Sum(),
                Is.EqualTo(first.Entries.Count));
            Assert.That(first.FacilityCountByProfile.Values,
                Has.All.GreaterThan(0));
            Assert.That(first.ContextEntries.All(item =>
                !item.PreservesFormalModelIdentity), Is.True);
            Assert.That(first.Entries.Where(item =>
                    item.PreservesFormalModelIdentity)
                .Select(item => item.FacilityId), Is.EquivalentTo(
                worldPlan.FarLandmarks.Where(
                        CountyCitywideBuildingLanguagePlan
                            .IsBuildingLanguageCandidate)
                    .Select(item => item.FacilityId)));
            Assert.That(first.ModuleCount,
                Is.GreaterThan(first.ContextEntries.Count * 3));
            Assert.That(first.StableSignature,
                Is.EqualTo(second.StableSignature));
            Assert.That(first.IsDerivedPresentationOnly, Is.True);
            Assert.That(first.CreatesWorldFacts, Is.False);
            Assert.That(source.LayoutPackage.DeclaredLayoutFingerprint,
                Is.EqualTo(fingerprint));
        }

        [Test]
        public void LuoyangCitywideBuildingLanguage_MapsKnownDefinitionsToFiveFamilies()
        {
            var catalog = CountyBuildingPresentationProfileCatalog
                .HanLuoyangV2;

            Assert.That(catalog.Resolve(
                    "facility.residential.rural_hamlet", "residential")
                .Archetype, Is.EqualTo(
                CountyGoldenBlockArchetype.ResidenceCourtyard));
            Assert.That(catalog.Resolve("facility.service.inn", "service")
                .Archetype, Is.EqualTo(
                CountyGoldenBlockArchetype.MarketFrontage));
            Assert.That(catalog.Resolve("facility.industry.brewery",
                    "industry").Archetype, Is.EqualTo(
                CountyGoldenBlockArchetype.WorkshopYard));
            Assert.That(catalog.Resolve("facility.public.granary", "public")
                .Archetype, Is.EqualTo(
                CountyGoldenBlockArchetype.WarehouseCompound));
            Assert.That(catalog.Resolve("facility.service.school", "service")
                .Archetype, Is.EqualTo(
                CountyGoldenBlockArchetype.CivicCourtyard));
            Assert.That(catalog.Resolve(CountyFarAggregateKind.Storage)
                .Archetype, Is.EqualTo(
                CountyGoldenBlockArchetype.WarehouseCompound));
        }
    }
}
