using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void PlayableFormalWorldMap_PlannedRouteUsesR003WithoutMutation()
        {
            var world = CreateM26ProductWorld();
            var before = WorldSnapshotSerializer.Serialize(world);
            var projection = PlayableWorldMapProjectionSystem.Build(
                world,
                world.PlayerPersonId,
                MerchantHouseholdContentRegistry.CreateCore(),
                new HanWorldStrategicCellRouteProvider(
                    WorldPackageRoot()));

            Assert.That(projection.HasRoute, Is.True);
            Assert.That(projection.AssetRouteId, Is.EqualTo("R003"));
            Assert.That(projection.FormalWorldRouteId,
                Is.EqualTo("route.zhuo_zhongshan"));
            Assert.That(projection.Status,
                Is.EqualTo(PlayableWorldMapRouteStatus.Planned));
            Assert.That(projection.OriginCellId64,
                Is.EqualTo(3_352_589UL));
            Assert.That(projection.TargetCellId64,
                Is.EqualTo(3_160_413UL));
            Assert.That(projection.CurrentCellId64,
                Is.EqualTo(projection.OriginCellId64));
            Assert.That(projection.CellIds.Count, Is.GreaterThan(59));
            Assert.That(projection.TotalWeightedCentimetres,
                Is.GreaterThan(0));
            Assert.That(projection.RemainingWeightedCentimetres,
                Is.EqualTo(projection.TotalWeightedCentimetres));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));

            using (var reader = new WorldMapDataReader(WorldPackageRoot()))
            {
                for (var i = 1; i < projection.CellIds.Count; i++)
                {
                    Assert.That(reader.Grid.TryDecode(
                        new WorldMapCellId(projection.CellIds[i - 1]),
                        out var fromRow, out var fromColumn), Is.True);
                    Assert.That(reader.Grid.TryDecode(
                        new WorldMapCellId(projection.CellIds[i]),
                        out var toRow, out var toColumn), Is.True);
                    Assert.That(Math.Abs(fromRow - toRow) +
                        Math.Abs(fromColumn - toColumn), Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void PlayableFormalWorldMap_DepartureReadsFreightProgress()
        {
            var world = PreparedCaravan();
            Execute(world, PlayerActionIds.MerchantStartJourney);
            Execute(world, PlayerActionIds.Rest);
            var freight = world.CivilianFreights.Single(item =>
                item.CarrierPersonId == world.PlayerPersonId &&
                item.PurposeId ==
                    CivilianFreightPurposeIds.MerchantOwnerCarriage);
            var before = WorldSnapshotSerializer.Serialize(world);

            var projection = PlayableWorldMapProjectionSystem.Build(
                world,
                world.PlayerPersonId,
                MerchantHouseholdContentRegistry.CreateCore(),
                new HanWorldStrategicCellRouteProvider(
                    WorldPackageRoot()));

            Assert.That(projection.HasRoute, Is.True);
            Assert.That(projection.Status,
                Is.EqualTo(freight.CellRouteWaiting
                    ? PlayableWorldMapRouteStatus.Waiting
                    : PlayableWorldMapRouteStatus.InTransit));
            Assert.That(projection.PlanVersionId,
                Is.EqualTo(freight.CellRoutePlanVersionId));
            Assert.That(projection.AssetHash,
                Is.EqualTo(freight.CellRouteAssetHash));
            Assert.That(projection.CurrentCellId64,
                Is.EqualTo(freight.CellRouteCurrentCellId64));
            Assert.That(projection.CurrentCellSequence,
                Is.EqualTo(freight.CurrentCellRouteSegmentIndex));
            Assert.That(projection.RemainingWeightedCentimetres,
                Is.EqualTo(freight.CellRouteRemainingWeightedCentimetres));
            Assert.That(projection.CellIds.Count,
                Is.EqualTo(freight.CellRouteSegments.Count + 1));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }
    }
}
