using Mandate.Domain;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void M26P5AEntry_RecommendedMerchantHasProminentTownEntry()
        {
            var world = CreateRecommendedMerchantWorld();

            var entry = TownNavigationPresentation.Build(
                world,
                world.PlayerPersonId,
                false);

            Assert.That(entry.LocationId, Is.EqualTo("location.zhongshan"));
            Assert.That(entry.VisibleFacilityCount, Is.EqualTo(7));
            Assert.That(entry.CanEnter, Is.True);
            Assert.That(entry.ButtonLabel, Does.Contain("进入中山"));
            Assert.That(entry.ButtonLabel, Does.Contain("7处建筑"));
            Assert.That(entry.Guidance, Does.Contain("直接进入城镇"));
        }

        [Test]
        public void M26P5AEntry_TravelingDisablesTownEntryWithReason()
        {
            var world = CreateRecommendedMerchantWorld();

            var entry = TownNavigationPresentation.Build(
                world,
                world.PlayerPersonId,
                true);

            Assert.That(entry.VisibleFacilityCount, Is.EqualTo(7));
            Assert.That(entry.CanEnter, Is.False);
            Assert.That(entry.Guidance, Does.Contain("旅途中"));
        }

        [Test]
        public void M26P5AEntry_LocationWithoutBuildingsPointsToZhongshan()
        {
            var world = new NewGameSetupService().CreateExisting184World(
                "person.liu_bei",
                184);

            var entry = TownNavigationPresentation.Build(
                world,
                world.PlayerPersonId,
                false);

            Assert.That(entry.VisibleFacilityCount, Is.EqualTo(0));
            Assert.That(entry.CanEnter, Is.False);
            Assert.That(entry.Guidance, Does.Contain("中山"));
        }

        [Test]
        public void M26P5AVisual_AllOpeningFacilityKindsHaveDistinctSeals()
        {
            var kinds = new[]
            {
                TownFacilityKindIds.Market,
                TownFacilityKindIds.MerchantHall,
                TownFacilityKindIds.Warehouse,
                TownFacilityKindIds.Inn,
                TownFacilityKindIds.VehicleYard,
                TownFacilityKindIds.GuildHall,
                TownFacilityKindIds.GovernmentOffice
            };
            var seals = new System.Collections.Generic.HashSet<string>();

            for (var i = 0; i < kinds.Length; i++)
            {
                var visual = TownVisualPresentation.Describe(kinds[i]);
                Assert.That(visual.Seal, Is.Not.Empty);
                Assert.That(visual.Category, Is.Not.Empty);
                Assert.That(seals.Add(visual.Seal), Is.True);
            }

            Assert.That(
                TownVisualPresentation.OverviewResourcePath(
                    "location.zhongshan"),
                Is.EqualTo(
                    TownVisualPresentation.ZhongshanOverviewResourcePath));
            Assert.That(
                TownVisualPresentation.OverviewResourcePath(
                    "location.luoyang"),
                Is.Empty);
        }

        [Test]
        public void M26P5B_TownInspectionProjectsPersistentMapPlacement()
        {
            var world = CreateRecommendedMerchantWorld();
            var town = new MerchantTownOperationSystem().InspectTown(
                world,
                world.PlayerPersonId,
                "location.zhongshan");

            Assert.That(town.Facilities.Count, Is.EqualTo(7));
            for (var i = 0; i < town.Facilities.Count; i++)
            {
                Assert.That(town.Facilities[i].HasMapPlacement, Is.True);
                Assert.That(town.Facilities[i].DistrictId, Is.Not.Empty);
            }
            Assert.That(
                town.Facilities[0].MapYBasisPoints,
                Is.LessThanOrEqualTo(
                    town.Facilities[town.Facilities.Count - 1]
                        .MapYBasisPoints));
        }

        private static WorldState CreateRecommendedMerchantWorld() =>
            new NewGameSetupService().CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "沈衡",
                    Age = 24,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Merchant,
                    BackgroundId = StartingBackgroundIds.LocalHousehold,
                    StartingLocationId = "location.zhongshan"
                },
                184);
    }
}
