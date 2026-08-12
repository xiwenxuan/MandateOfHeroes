using System;
using System.Collections.Generic;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void M26P5A_PrototypeCreatesPersistentZhongshanTownAndBranch()
        {
            var world = PrototypeWorldFactory.Create184World();
            var branch = world.MerchantBranches.Find(item =>
                item.Id == MerchantTownOperationSystem.ZhongshanBranchId);

            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.IsHeadquarters, Is.True);
            Assert.That(branch.FacilityIds.Count, Is.EqualTo(3));
            Assert.That(world.TownFacilities.Count, Is.EqualTo(7));
            Assert.That(
                world.InventoryContainers.Exists(item =>
                    item.Id == MerchantTownOperationSystem
                        .ZhongshanWarehouseContainerId &&
                    item.OwnerOrganizationId ==
                        MerchantTownOperationSystem.ZhongshanOrganizationId),
                Is.True);
            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26P5A_LocalMerchantCanEnterHallWarehouseAndMarket()
        {
            var world = PrototypeWorldFactory.Create184World();
            var system = new MerchantTownOperationSystem();
            var town = system.InspectTown(
                world,
                "person.zhang_shiping",
                "location.zhongshan");

            Assert.That(town.CanEnterTown, Is.True);
            Assert.That(town.Facilities.Count, Is.EqualTo(7));
            Assert.That(
                system.EnterFacility(
                    world,
                    "person.zhang_shiping",
                    "town_facility.zhongshan.merchant_hall").OperationIds,
                Does.Contain(TownFacilityOperationIds.PrepareCaravan));
            Assert.That(
                system.EnterFacility(
                    world,
                    "person.zhang_shiping",
                    "town_facility.zhongshan.warehouse").InventoryContainerId,
                Is.EqualTo(MerchantTownOperationSystem
                    .ZhongshanWarehouseContainerId));
            Assert.That(
                system.EnterFacility(
                    world,
                    "person.zhang_shiping",
                    "town_facility.zhongshan.market").OperationIds,
                Does.Contain(TownFacilityOperationIds.InspectMarket));
        }

        [Test]
        public void M26P5A_TownAndOrganizationAccessAreEnforced()
        {
            var world = PrototypeWorldFactory.Create184World();
            var system = new MerchantTownOperationSystem();

            Assert.Throws<InvalidOperationException>(() =>
                system.EnterFacility(
                    world,
                    "person.liu_bei",
                    "town_facility.zhongshan.market"));

            var liuBei = world.People.Find(item => item.Id == "person.liu_bei");
            liuBei.LocationId = "location.zhongshan";
            Assert.DoesNotThrow(() => system.EnterFacility(
                world,
                liuBei.Id,
                "town_facility.zhongshan.market"));
            Assert.Throws<InvalidOperationException>(() =>
                system.EnterFacility(
                    world,
                    liuBei.Id,
                    "town_facility.zhongshan.warehouse"));
        }

        [Test]
        public void M26P5A_SnapshotRoundTripPreservesTownFacilitiesAndBranch()
        {
            var world = PrototypeWorldFactory.Create184World();

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.TownFacilities.Count, Is.EqualTo(7));
            Assert.That(loaded.MerchantBranches.Count, Is.EqualTo(1));
            Assert.That(
                loaded.MerchantBranches[0].InventoryContainerId,
                Is.EqualTo(MerchantTownOperationSystem
                    .ZhongshanWarehouseContainerId));
        }

        [Test]
        public void M26P5A_MigratesVersionSixtySixWithoutInventingTownFacts()
        {
            var world = PrototypeWorldFactory.Create184World();
            world.TownFacilities.Clear();
            world.MerchantBranches.Clear();
            world.InventoryContainers.RemoveAll(item =>
                item.Id == MerchantTownOperationSystem
                    .ZhongshanWarehouseContainerId);
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 66");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.TownFacilities, Is.Empty);
            Assert.That(loaded.MerchantBranches, Is.Empty);
            Assert.That(
                loaded.InventoryContainers.Exists(item =>
                    item.Id == MerchantTownOperationSystem
                        .ZhongshanWarehouseContainerId),
                Is.False);
        }

        [Test]
        public void M26P5B_PrototypeTownHasPersistentUniqueSpatialLayout()
        {
            var world = PrototypeWorldFactory.Create184World();
            var coordinates = new HashSet<string>(StringComparer.Ordinal);

            Assert.That(world.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(world.TownFacilities.Count, Is.EqualTo(7));
            for (var i = 0; i < world.TownFacilities.Count; i++)
            {
                var facility = world.TownFacilities[i];
                Assert.That(facility.HasMapPlacement, Is.True, facility.Id);
                Assert.That(facility.DistrictId, Is.Not.Empty, facility.Id);
                Assert.That(facility.MapXBasisPoints, Is.InRange(1, 9_999));
                Assert.That(facility.MapYBasisPoints, Is.InRange(1, 9_999));
                Assert.That(
                    coordinates.Add(
                        facility.MapXBasisPoints + "|" +
                        facility.MapYBasisPoints),
                    Is.True,
                    facility.Id);
            }

            Assert.DoesNotThrow(world.Validate);
        }

        [Test]
        public void M26P5B_SnapshotRoundTripPreservesFacilityPlacement()
        {
            var world = PrototypeWorldFactory.Create184World();
            var expected = world.TownFacilities.Find(item =>
                item.Id == "town_facility.zhongshan.market");

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var actual = loaded.TownFacilities.Find(item =>
                item.Id == expected.Id);

            Assert.That(actual.HasMapPlacement, Is.True);
            Assert.That(actual.DistrictId, Is.EqualTo(expected.DistrictId));
            Assert.That(actual.MapXBasisPoints,
                Is.EqualTo(expected.MapXBasisPoints));
            Assert.That(actual.MapYBasisPoints,
                Is.EqualTo(expected.MapYBasisPoints));
            Assert.That(actual.FootprintWidthBasisPoints,
                Is.EqualTo(expected.FootprintWidthBasisPoints));
            Assert.That(actual.FootprintHeightBasisPoints,
                Is.EqualTo(expected.FootprintHeightBasisPoints));
        }

        [Test]
        public void M26P5B_MigratesV67KnownBuildingsWithoutCreatingFacilities()
        {
            var world = PrototypeWorldFactory.Create184World();
            for (var i = 0; i < world.TownFacilities.Count; i++)
            {
                ClearPlacement(world.TownFacilities[i]);
            }
            world.TownFacilities.Add(new TownFacilityState
            {
                Id = "town_facility.zhongshan.legacy_shop",
                KindId = TownFacilityKindIds.Residence,
                DisplayName = "旧铺",
                LocationId = "location.zhongshan",
                AccessPolicyId = TownFacilityAccessPolicyIds.Public,
                IsPubliclyVisible = true,
                IsOperational = true
            });
            var expectedCount = world.TownFacilities.Count;
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 67");

            var loaded = WorldSnapshotSerializer.Deserialize(json);
            var legacy = loaded.TownFacilities.Find(item =>
                item.Id == "town_facility.zhongshan.legacy_shop");

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.TownFacilities.Count, Is.EqualTo(expectedCount));
            Assert.That(
                loaded.TownFacilities.FindAll(item => item.HasMapPlacement).Count,
                Is.EqualTo(7));
            Assert.That(legacy, Is.Not.Null);
            Assert.That(legacy.HasMapPlacement, Is.False);
            Assert.That(legacy.DistrictId, Is.Null.Or.Empty);
        }

        [Test]
        public void M26P5B_InvalidPlacementIsRejected()
        {
            var world = PrototypeWorldFactory.Create184World();
            world.TownFacilities[0].MapXBasisPoints = 10_000;

            Assert.Throws<InvalidOperationException>(world.Validate);
        }

        private static void ClearPlacement(TownFacilityState facility)
        {
            facility.HasMapPlacement = false;
            facility.DistrictId = string.Empty;
            facility.MapXBasisPoints = 0;
            facility.MapYBasisPoints = 0;
            facility.FootprintWidthBasisPoints = 0;
            facility.FootprintHeightBasisPoints = 0;
        }
    }
}
