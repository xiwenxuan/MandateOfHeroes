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
        public void PlayableDirectLuoyangWorld_CoversFormalMapAndRoundTrips()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var world = PlayableLuoyangWorldFactory.Create(
                source.Plan, source.Performance, 184_001UL);

            Assert.That(world.PlayerPersonId,
                Is.EqualTo(PlayableLuoyangWorldContractIds.PlayerPersonId));
            Assert.That(world.People.Count, Is.EqualTo(1));
            Assert.That(world.Locations.Count, Is.EqualTo(1));
            Assert.That(world.Locations[0].Id,
                Is.EqualTo(LuoyangHumanScaleLocalMapIds
                    .SettlementLocationId));
            Assert.That(world.Facilities.Count,
                Is.EqualTo(source.Plan.FacilityCapabilities.Count));
            Assert.That(world.Facilities.Count, Is.EqualTo(2_084));
            Assert.That(world.Facilities.Select(item => item.Id),
                Is.EquivalentTo(source.Plan.FacilityCapabilities.Select(
                    item => item.FacilityId)));
            Assert.That(world.Facilities.All(item => item.CellId64 != 0UL),
                Is.True);
            Assert.That(world.Facilities.All(item => item.SourceNote ==
                PlayableLuoyangWorldContractIds.ContractId), Is.True);
            Assert.That(world.Facilities.Single(item => item.Id ==
                    PlayableLuoyangWorldContractIds.MarketFacilityId)
                .DisplayName, Is.EqualTo("市场"));
            Assert.That(world.Families.Single().HeadPersonId,
                Is.EqualTo(world.PlayerPersonId));
            Assert.That(world.MarketListings.Single().LocationId,
                Is.EqualTo(LuoyangHumanScaleLocalMapIds
                    .SettlementLocationId));
            Assert.That(world.TaskDefinitions.Single().Id,
                Is.EqualTo(PlayableLuoyangWorldContractIds
                    .LocalTaskDefinitionId));

            world.Validate();
            var snapshot = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(snapshot);
            loaded.Validate();
            Assert.That(WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(snapshot));
        }

        [Test]
        public void PlayableDirectLuoyangWorld_IsDeterministicAndCanRest()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var first = PlayableLuoyangWorldFactory.Create(source.Plan, 184UL);
            var second = PlayableLuoyangWorldFactory.Create(source.Plan, 184UL);
            Assert.That(WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(first)));

            var actionService = new PlayerActionService(
                new WorldSimulator(first.MasterSeed));
            var result = actionService.Execute(first, first.PlayerPersonId,
                PlayerActionIds.Rest);

            Assert.That(result.Success, Is.True, result.Detail);
            Assert.That(result.DaysAdvanced, Is.EqualTo(1));
            Assert.That(first.AbsoluteDay, Is.EqualTo(1));
            Assert.That(first.PlayerPersonId,
                Is.EqualTo(PlayableLuoyangWorldContractIds.PlayerPersonId));
        }

        [Test]
        public void PlayableDirectLuoyangWorld_ProvidesTradeAndLocalTaskLoop()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var world = PlayableLuoyangWorldFactory.Create(
                source.Plan, source.Performance, 184_001UL);
            var actions = new PlayerActionService(
                new WorldSimulator(world.MasterSeed));

            var trade = actions.QueryActions(world, world.PlayerPersonId)
                .Single(item => item.Id == PlayerActionIds.TradeBuy);
            Assert.That(trade.IsAvailable, Is.True,
                trade.UnavailableReason);
            var bought = actions.Execute(world, world.PlayerPersonId,
                PlayerActionIds.TradeBuy);
            Assert.That(bought.Success, Is.True, bought.Detail);
            Assert.That(world.TradeRecords.Count, Is.EqualTo(1));

            var accepted = actions.Execute(world, world.PlayerPersonId,
                PlayerActionIds.AcceptTask);
            Assert.That(accepted.Success, Is.True, accepted.Detail);
            Assert.That(world.Tasks.Single().DefinitionId,
                Is.EqualTo(PlayableLuoyangWorldContractIds
                    .LocalTaskDefinitionId));
            world.Validate();
        }

        [Test]
        public void PlayableDirectLuoyangWorld_GameplayFacilitiesShareWalkableNetwork()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var world = PlayableLuoyangWorldFactory.Create(
                source.Plan, source.Performance, 184_001UL);
            var initialFacilityId = PlayableLuoyangWorldContractIds
                .StartingFacilityId;
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                source.StrategicRoads);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(world, runtime);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var movementSystem = new LuoyangFormalPlayerMovementSystem(
                source.StrategicRoads, null, source.Plan);
            movementSystem.RegisterHandlers(runtime);
            movementSystem.EnsureInitialized(world, runtime,
                initialFacilityId);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var planner = new LuoyangHumanScaleLocalRoutePlanner(source.Plan);
            var markets = source.Plan.FacilityCapabilities
                .Where(item => item.FacilityDefinitionId ==
                    "facility.commercial.market").ToArray();
            Assert.That(markets.Select(item => item.FacilityId),
                Does.Contain(PlayableLuoyangWorldContractIds
                    .MarketFacilityId));
            Assert.That(new PlayerSession(world).ControlledPerson
                    .CurrentFacilityId,
                Is.EqualTo(PlayableLuoyangWorldContractIds
                    .StartingFacilityId));
            Assert.That(planner.TryFindRoute(world,
                    PlayableLuoyangWorldContractIds.StartingFacilityId,
                    PlayableLuoyangWorldContractIds.MarketFacilityId,
                    out var route, out var failureReasonId), Is.True,
                failureReasonId);
            Assert.That(route.Points.Count, Is.GreaterThan(1));
            Assert.That(PlayableLuoyangWorldContractIds.OfficeFacilityId,
                Is.EqualTo(PlayableLuoyangWorldContractIds
                    .MarketFacilityId),
                "The first local task is issued by the market's 市曹 desk.");
        }

        [Test]
        public void LuoyangThreeLevelView_TransitionsUseOneNonPersistentState()
        {
            var view = new LuoyangPlayableViewState();
            Assert.That(view.Mode, Is.EqualTo(LuoyangPlayableViewMode.Person));
            view.ShowCounty(Luoyang50mCountySpatialPrototypeIds.CountyId,
                CountySubViewMode.UrbanArea,
                PlayableLuoyangWorldContractIds.MarketFacilityId);
            Assert.That(view.Mode, Is.EqualTo(LuoyangPlayableViewMode.County));
            Assert.That(view.FocusFacilityId, Is.EqualTo(
                PlayableLuoyangWorldContractIds.MarketFacilityId));
            view.ShowWorld();
            Assert.That(view.Mode, Is.EqualTo(LuoyangPlayableViewMode.World));
            view.ShowCounty(Luoyang50mCountySpatialPrototypeIds.CountyId);
            Assert.That(view.Mode, Is.EqualTo(LuoyangPlayableViewMode.County));
            view.ShowPlayer();
            Assert.That(view.Mode, Is.EqualTo(LuoyangPlayableViewMode.Person));
            Assert.That(view.FollowsPlayer, Is.True);

            Assert.That(LuoyangPlayableViewCommandBindings.TryResolve('M',
                out var worldCommand), Is.True);
            Assert.That(worldCommand,
                Is.EqualTo(LuoyangPlayableViewCommand.ShowWorld));
            Assert.That(LuoyangPlayableViewCommandBindings.TryResolve('C',
                out var cityCommand), Is.True);
            Assert.That(cityCommand,
                Is.EqualTo(LuoyangPlayableViewCommand.ShowCounty));
            Assert.That(LuoyangPlayableViewCommandBindings.TryResolve('F',
                out var personCommand), Is.True);
            Assert.That(personCommand,
                Is.EqualTo(LuoyangPlayableViewCommand.ShowPerson));
        }

        [Test]
        public void LuoyangThreeLevelView_DoesNotMutateWorldOrTeleportPlayer()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var world = PlayableLuoyangWorldFactory.Create(
                source.Plan, source.Performance, 184_001UL);
            var before = WorldSnapshotSerializer.Serialize(world);
            var player = new PlayerSession(world).ControlledPerson;
            var playerFacilityId = player.CurrentFacilityId;
            var playerCellId = player.CurrentCellId64;
            var view = new LuoyangPlayableViewState();

            view.ShowCounty(Luoyang50mCountySpatialPrototypeIds.CountyId,
                CountySubViewMode.UrbanArea,
                PlayableLuoyangWorldContractIds.MarketFacilityId);
            view.ShowWorld();
            view.ShowCounty(Luoyang50mCountySpatialPrototypeIds.CountyId,
                CountySubViewMode.Planning,
                PlayableLuoyangWorldContractIds.MarketFacilityId);
            view.ObserveFacility(
                PlayableLuoyangWorldContractIds.MarketFacilityId);

            Assert.That(view.Mode, Is.EqualTo(LuoyangPlayableViewMode.Person));
            Assert.That(view.FocusFacilityId, Is.EqualTo(
                PlayableLuoyangWorldContractIds.MarketFacilityId));
            Assert.That(new PlayerSession(world).ControlledPerson
                .CurrentFacilityId, Is.EqualTo(playerFacilityId));
            Assert.That(new PlayerSession(world).ControlledPerson
                .CurrentCellId64, Is.EqualTo(playerCellId));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void LuoyangCityProjection_CoversFormalCityAndKeyStructures()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var projection = LuoyangCityViewProjection.Create(
                source.Performance, source.Composition, source.Plan);

            Assert.That(projection.FacilityCount, Is.EqualTo(2_084));
            Assert.That(projection.AssetVariantCount, Is.EqualTo(54));
            Assert.That(projection.DistrictCount, Is.EqualTo(6));
            Assert.That(projection.CityGateCount, Is.EqualTo(12));
            Assert.That(projection.HasWallNetwork, Is.True);
            Assert.That(projection.HasNorthPalace, Is.True);
            Assert.That(projection.HasSouthPalace, Is.True);
            Assert.That(projection.HasMarket, Is.True);
            Assert.That(projection.HasGovernment, Is.True);
            Assert.That(projection.HasStateStorage, Is.True);
            Assert.That(projection.HasSouthernRitualArea, Is.True);
            Assert.That(projection.TryGet(
                PlayableLuoyangWorldContractIds.MarketFacilityId,
                out var market), Is.True);
            Assert.That(market.FacilityId, Is.EqualTo(
                PlayableLuoyangWorldContractIds.MarketFacilityId));

            var rebuilt = LuoyangCityViewProjection.Create(
                source.Performance, source.Composition, source.Plan);
            Assert.That(rebuilt.StableSummary,
                Is.EqualTo(projection.StableSummary));
        }

        [Test]
        public void LuoyangNearfieldVisualProfile_IsStableAndKeepsFormalId()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var facilityIds = new[]
            {
                PlayableLuoyangWorldContractIds.StartingFacilityId,
                PlayableLuoyangWorldContractIds.MarketFacilityId,
                "facility.instance.luoyang.184.gate.guangyangmen",
                "facility.instance.luoyang.184.arsenal"
            };
            foreach (var facilityId in facilityIds.Where(id => source.Plan
                         .FacilityCapabilitiesByFacilityId.ContainsKey(id)))
            {
                var first = LuoyangNearfieldVisualProfileResolver.Resolve(
                    source.Plan, facilityId);
                var second = LuoyangNearfieldVisualProfileResolver.Resolve(
                    source.Plan, facilityId);
                Assert.That(first.FacilityId, Is.EqualTo(facilityId));
                Assert.That(second.ProfileId, Is.EqualTo(first.ProfileId));
                Assert.That(second.ClusterHookId,
                    Is.EqualTo(first.ClusterHookId));
                Assert.That(second.StableVariantIndex,
                    Is.EqualTo(first.StableVariantIndex));
                Assert.That(second.HeightCentimetres,
                    Is.EqualTo(first.HeightCentimetres));
            }
        }

        [Test]
        public void LuoyangNearfieldUrbanContext_IsCompactStableAndUsesFormalIds()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var first = LuoyangNearfieldUrbanContextProjection.Create(
                source.Plan,
                PlayableLuoyangWorldContractIds.MarketFacilityId);
            var second = LuoyangNearfieldUrbanContextProjection.Create(
                source.Plan,
                PlayableLuoyangWorldContractIds.MarketFacilityId);

            Assert.That(first.FocusFacilityId, Is.EqualTo(
                PlayableLuoyangWorldContractIds.MarketFacilityId));
            Assert.That(first.Facilities.Count, Is.EqualTo(9));
            Assert.That(first.Facilities[0].IsFocusFacility, Is.True);
            Assert.That(first.Facilities[0].VisualEastUnityUnits,
                Is.EqualTo(0d));
            Assert.That(first.Facilities[0].VisualNorthUnityUnits,
                Is.EqualTo(0d));
            Assert.That(first.Facilities.Select(item => item.FacilityId)
                .Distinct().Count(), Is.EqualTo(9));
            Assert.That(first.Facilities.All(item => source.Plan
                .FacilityCapabilitiesByFacilityId.ContainsKey(
                    item.FacilityId)), Is.True);
            Assert.That(first.Facilities.Max(item => System.Math.Abs(
                    item.VisualEastUnityUnits)), Is.LessThanOrEqualTo(8.5d));
            Assert.That(first.Facilities.Max(item => System.Math.Abs(
                    item.VisualNorthUnityUnits)), Is.LessThanOrEqualTo(7.5d));
            Assert.That(second.StableSummary,
                Is.EqualTo(first.StableSummary));
            Assert.That(second.Facilities.Select(item => item.FacilityId),
                Is.EqualTo(first.Facilities.Select(item => item.FacilityId)));
        }

        private static string DirectLuoyangWorldMapRoot()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Assets",
                "StreamingAssets", "WorldMap");
        }
    }
}
