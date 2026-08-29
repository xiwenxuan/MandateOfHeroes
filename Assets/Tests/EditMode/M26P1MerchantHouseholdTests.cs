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
        public void M26P1_ContentAndMerchantStartExposeGroundedGoal()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var goal = content.GetGoal(MerchantHouseholdContentIds.FirstGoal);
            var world = CreateM26P1MerchantWorld(content);
            var service = CreateM26P1Actions(world, content);
            var view = service.InspectMerchantGoal(world, world.PlayerPersonId);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var family = world.Families.Find(item => item.Id == player.FamilyId);

            Assert.That(view.IsAvailable, Is.True);
            Assert.That(view.GoalId, Is.EqualTo(goal.Id));
            Assert.That(view.Phase, Is.EqualTo(0));
            Assert.That(view.MarketOpportunity.SourceName, Is.Not.Empty);
            Assert.That(view.MarketOpportunity.ReliabilityBasisPoints,
                Is.InRange(1, 9_999));
            Assert.That(family.Debt, Is.EqualTo(goal.InitialFamilyDebt));
            Assert.That(world.Tasks.Count(item =>
                    item.DefinitionId ==
                    MerchantHouseholdGameplayService.PrimaryTaskDefinitionId),
                Is.EqualTo(1));
            Assert.That(service.QueryActions(world, world.PlayerPersonId).Any(
                item => item.Id == PlayerActionIds.WorkTask), Is.False);
        }

        [Test]
        public void M26P1_CapitalChoicesUseRealLedgersAndCannotRepeat()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var goal = content.GetGoal(MerchantHouseholdContentIds.FirstGoal);
            var ownWorld = CreateM26P1MerchantWorld(content);
            var ownActions = CreateM26P1Actions(ownWorld, content);
            var ownPlayer = ownWorld.People.Find(item =>
                item.Id == ownWorld.PlayerPersonId);
            var ownFamily = ownWorld.Families.Find(item =>
                item.Id == ownPlayer.FamilyId);
            var conserved = ownPlayer.Wealth + ownFamily.Wealth;

            var own = ownActions.Execute(ownWorld, ownPlayer.Id,
                PlayerActionIds.MerchantUseOwnCapital);

            Assert.That(own.Success, Is.True);
            Assert.That(ownPlayer.Wealth + ownFamily.Wealth,
                Is.EqualTo(conserved));
            Assert.That(ownFamily.Debt, Is.EqualTo(goal.InitialFamilyDebt));
            var afterFirst = WorldSnapshotSerializer.Serialize(ownWorld);
            var duplicate = ownActions.Execute(ownWorld, ownPlayer.Id,
                PlayerActionIds.MerchantUseOwnCapital);
            Assert.That(duplicate.Success, Is.False);
            Assert.That(WorldSnapshotSerializer.Serialize(ownWorld),
                Is.EqualTo(afterFirst));

            var creditWorld = CreateM26P1MerchantWorld(content);
            var creditActions = CreateM26P1Actions(creditWorld, content);
            var creditPlayer = creditWorld.People.Find(item =>
                item.Id == creditWorld.PlayerPersonId);
            var creditFamily = creditWorld.Families.Find(item =>
                item.Id == creditPlayer.FamilyId);
            var guild = creditWorld.Organizations.Find(item =>
                item.Id == goal.IssuerOrganizationId);
            var creditConserved = creditPlayer.Wealth + guild.Treasury;

            var credit = creditActions.Execute(creditWorld, creditPlayer.Id,
                PlayerActionIds.MerchantTakeGuildAdvance);

            Assert.That(credit.Success, Is.True);
            Assert.That(creditPlayer.Wealth + guild.Treasury,
                Is.EqualTo(creditConserved));
            Assert.That(creditFamily.Debt,
                Is.EqualTo(goal.InitialFamilyDebt + goal.GuildAdvanceDebt));
        }

        [Test]
        public void M26P1_FullRouteCommitsTradeEventDebtAndFollowup()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var goal = content.GetGoal(MerchantHouseholdContentIds.FirstGoal);
            var world = CreateM26P1MerchantWorld(content);
            var actions = CreateM26P1Actions(world, content);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var family = world.Families.Find(item => item.Id == player.FamilyId);
            var startingDebt = family.Debt;

            AssertSuccess(actions, world, PlayerActionIds.MerchantUseOwnCapital);
            AssertSuccess(actions, world, PlayerActionIds.MerchantBuyJourneyCargo);
            Assert.That(world.TradeRecords.Count, Is.EqualTo(1));
            AssertSuccess(actions, world, PlayerActionIds.MerchantStartJourney);
            AdvanceM26P1ToEvent(actions, world);
            var eventResult = AssertSuccess(
                actions, world, PlayerActionIds.MerchantEventHelp);
            Assert.That(eventResult.DaysAdvanced, Is.EqualTo(1));
            AdvanceM26P1ToArrival(actions, world);
            Assert.That(player.LocationId, Is.EqualTo(goal.TargetLocationId));

            AssertSuccess(actions, world, PlayerActionIds.MerchantDeliverCargo);
            Assert.That(world.TradeRecords.Count, Is.EqualTo(2));
            Assert.That(actions.InspectMerchantGoal(world, player.Id).Phase,
                Is.EqualTo(4));
            AssertSuccess(actions, world, PlayerActionIds.MerchantRepayFamilyDebt);

            var primary = world.Tasks.Find(item =>
                item.DefinitionId ==
                MerchantHouseholdGameplayService.PrimaryTaskDefinitionId);
            Assert.That(primary.Status, Is.EqualTo(TaskStatus.Completed));
            Assert.That(family.Debt,
                Is.EqualTo(System.Math.Max(0,
                    startingDebt - goal.DebtRepayment)));
            Assert.That(world.Tasks.Any(item =>
                item.Id.StartsWith("task.m26p1.followup.") &&
                item.Status == TaskStatus.Active), Is.True);
            Assert.That(world.LifeEvents.Any(item =>
                item.Id.StartsWith("life_event.m26p1.event_help.")), Is.True);
            world.Validate();
        }

        [Test]
        public void M26P1_TravelChoiceIsDeterministicAndSurvivesSnapshot()
        {
            var first = ResolveM26P1GuardChoice(724_111UL);
            var second = ResolveM26P1GuardChoice(724_111UL);
            Assert.That(WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(first)));

            var roundTrip = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(first));
            var player = roundTrip.People.Find(item =>
                item.Id == roundTrip.PlayerPersonId);
            var task = roundTrip.Tasks.Find(item =>
                item.DefinitionId ==
                MerchantHouseholdGameplayService.PrimaryTaskDefinitionId);
            Assert.That(task.Progress, Is.EqualTo(3));
            Assert.That(roundTrip.Journeys.Any(item =>
                item.PersonId == player.Id), Is.True);
            Assert.That(roundTrip.LifeEvents.Any(item =>
                item.Id.StartsWith("life_event.m26p1.event_guard.")), Is.True);
            roundTrip.Validate();
        }

        [Test]
        public void M26P1_CartEndingPreservesDebtAndRaisesCapacity()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var goal = content.GetGoal(MerchantHouseholdContentIds.FirstGoal);
            var world = CreateM26P1MerchantWorld(content);
            var actions = CreateM26P1Actions(world, content);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            var family = world.Families.Find(item => item.Id == player.FamilyId);
            CompleteM26P1Delivery(actions, world,
                PlayerActionIds.MerchantEventRefuse);
            var debtBefore = family.Debt;
            var capacityBefore = player.CargoCapacity;

            AssertSuccess(actions, world, PlayerActionIds.MerchantInvestCart);

            Assert.That(family.Debt, Is.EqualTo(debtBefore));
            Assert.That(player.CargoCapacity,
                Is.EqualTo(capacityBefore + goal.CartCapacityGain));
            Assert.That(world.Tasks.Any(item =>
                item.DefinitionId == goal.CartFollowupDefinitionId &&
                item.Status == TaskStatus.Active), Is.True);
        }

        [Test]
        public void M26P1_UnavailablePurchaseDoesNotMutateWorld()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var world = CreateM26P1MerchantWorld(content);
            var actions = CreateM26P1Actions(world, content);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            AssertSuccess(actions, world, PlayerActionIds.MerchantUseOwnCapital);
            player.Wealth = 0;
            var before = WorldSnapshotSerializer.Serialize(world);

            var result = actions.Execute(
                world, player.Id, PlayerActionIds.MerchantBuyJourneyCargo);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Summary, Is.Not.Empty);
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void M26P1_CargoLossCreatesPartialDeliveryAndProratedCommission()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var goal = content.GetGoal(MerchantHouseholdContentIds.FirstGoal);
            var travelEvent = content.GetTravelEvent(goal.TravelEventId);
            ulong lossSeed = 0;
            for (ulong candidate = 1; candidate <= 1_000; candidate++)
            {
                if (new NamedRandom(candidate).Range(
                        "m26p1_travel_event",
                        new StableId(NewGameSetupService.CustomPlayerPersonId),
                        0,
                        travelEvent.Id + ".help",
                        0,
                        10_000) <
                    travelEvent.HelpCargoLossChanceBasisPoints)
                {
                    lossSeed = candidate;
                    break;
                }
            }
            Assert.That(lossSeed, Is.Not.EqualTo(0));

            var world = CreateM26P1MerchantWorld(content, lossSeed);
            var actions = CreateM26P1Actions(world, content);
            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            AssertSuccess(actions, world, PlayerActionIds.MerchantUseOwnCapital);
            AssertSuccess(actions, world, PlayerActionIds.MerchantBuyJourneyCargo);
            AssertSuccess(actions, world, PlayerActionIds.MerchantStartJourney);
            AdvanceM26P1ToEvent(actions, world);
            AssertSuccess(actions, world, PlayerActionIds.MerchantEventHelp);
            Assert.That(new TradingSystem().GetQuantity(
                world, player.Id, goal.CommodityId),
                Is.EqualTo(goal.CargoQuantity - 1));
            AdvanceM26P1ToArrival(actions, world);
            var guild = world.Organizations.Find(item =>
                item.Id == goal.IssuerOrganizationId);
            var guildBefore = guild.Treasury;

            AssertSuccess(actions, world, PlayerActionIds.MerchantDeliverCargo);

            var expectedCommission =
                goal.DeliveryCommission * (goal.CargoQuantity - 1) /
                goal.CargoQuantity;
            Assert.That(guildBefore - guild.Treasury,
                Is.EqualTo(expectedCommission));
            Assert.That(world.LifeEvents.Any(item =>
                item.Id.StartsWith("life_event.m26p1.partial_delivery.")),
                Is.True);
        }

        [Test]
        public void M26P2_ClothPurchaseCreatesFormalBatchInMovingCaravan()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var world = CreateM26P1MerchantWorld(content);
            var actions = CreateM26P1Actions(world, content);
            var player = world.People.Find(item =>
                item.Id == world.PlayerPersonId);
            var listing = world.MarketListings.Find(item =>
                item.LocationId == player.LocationId &&
                item.CommodityId == "commodity.cloth");
            var stockBefore = listing.Stock;

            AssertSuccess(actions, world, PlayerActionIds.MerchantUseOwnCapital);
            AssertSuccess(actions, world, PlayerActionIds.MerchantBuyJourneyCargo);

            var container = world.InventoryContainers.Single(item =>
                item.CarrierPersonId == player.Id &&
                item.KindId == "inventory_container.merchant_caravan");
            var batch = world.ProductBatches.Single(item =>
                item.InventoryContainerId == container.Id &&
                item.ProductDefinitionId ==
                    CoreProductionContent.PlainClothProductId);
            Assert.That(batch.Quantity, Is.EqualTo(6));
            Assert.That(batch.OwnerFamilyId, Is.EqualTo(player.FamilyId));
            Assert.That(listing.Stock, Is.EqualTo(stockBefore - 6));
            Assert.That(world.Inventories.Any(item =>
                item.OwnerPersonId == player.Id &&
                item.CommodityId == "commodity.cloth"), Is.False);
            Assert.That(world.InventoryTransactions.Any(item =>
                item.Type == InventoryTransactionType.MerchantMarketPurchased &&
                item.Lines.Any(line => line.BatchId == batch.Id &&
                    line.QuantityDelta == 6)), Is.True);

            AssertSuccess(actions, world, PlayerActionIds.MerchantStartJourney);
            AdvanceM26P1ToEvent(actions, world);
            Assert.That(container.LocationId, Is.EqualTo("location.zhongshan"));
            AssertSuccess(actions, world, PlayerActionIds.MerchantEventGuard);
            AdvanceM26P1ToArrival(actions, world);
            Assert.That(container.LocationId, Is.EqualTo("location.zhuo"));
            Assert.That(batch.Quantity, Is.EqualTo(6));
            world.Validate();
        }

        [Test]
        public void M26P2_FormalCaravanCargoSurvivesSnapshotAndAuditsLossAndSale()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var world = CreateM26P1MerchantWorld(content);
            var actions = CreateM26P1Actions(world, content);
            AssertSuccess(actions, world, PlayerActionIds.MerchantUseOwnCapital);
            AssertSuccess(actions, world, PlayerActionIds.MerchantBuyJourneyCargo);

            world = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            actions = CreateM26P1Actions(world, content);
            AssertSuccess(actions, world, PlayerActionIds.MerchantStartJourney);
            var trading = new TradingSystem();
            Assert.That(trading.LoseCargo(
                world, world.PlayerPersonId, "commodity.cloth", 1), Is.True);
            Assert.That(trading.GetQuantity(
                world, world.PlayerPersonId, "commodity.cloth"), Is.EqualTo(5));
            Assert.That(world.InventoryTransactions.Any(item =>
                item.Type == InventoryTransactionType.MerchantCargoDamaged &&
                item.Lines.Sum(line => line.QuantityDelta) == -1), Is.True);

            var player = world.People.Find(item =>
                item.Id == world.PlayerPersonId);
            AdvanceM26P1ToEvent(actions, world);
            AssertSuccess(actions, world, PlayerActionIds.MerchantEventRefuse);
            AdvanceM26P1ToArrival(actions, world);
            var stockBefore = world.MarketListings.Find(item =>
                item.LocationId == "location.zhuo" &&
                item.CommodityId == "commodity.cloth").Stock;
            var sale = trading.Sell(
                world,
                new StableId(player.Id),
                new StableId("commodity.cloth"),
                5);

            Assert.That(sale.Success, Is.True, sale.Message);
            Assert.That(trading.GetQuantity(
                world, player.Id, "commodity.cloth"), Is.Zero);
            Assert.That(world.MarketListings.Find(item =>
                item.LocationId == "location.zhuo" &&
                item.CommodityId == "commodity.cloth").Stock,
                Is.EqualTo(stockBefore + 5));
            Assert.That(world.InventoryTransactions.Any(item =>
                item.Type == InventoryTransactionType.MerchantMarketSold &&
                item.Lines.Sum(line => line.QuantityDelta) == -5), Is.True);
            WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world)).Validate();
        }

        [Test]
        public void Snapshot_MigratesVersionSixtyFourToDataDrivenCommodityProduct()
        {
            var world = PrototypeWorldFactory.Create184World();
            var json = WorldSnapshotSerializer.Serialize(world)
                .Replace(
                    "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                    "\"SchemaVersion\": 64")
                .Replace("\"Version\": \"11.1.0\"", "\"Version\": \"10.0.0\"");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.Commodities.Find(item =>
                item.Id == "commodity.cloth").ProductDefinitionId,
                Is.EqualTo(CoreProductionContent.PlainClothProductId));
            Assert.That(loaded.ProductionContentManifest.Packages[0].Version,
                Is.EqualTo("11.1.0"));
        }

        [Test]
        public void M26P2_RejectsMerchantBatchCarriedByUnrelatedPerson()
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var world = CreateM26P1MerchantWorld(content);
            var actions = CreateM26P1Actions(world, content);
            AssertSuccess(actions, world, PlayerActionIds.MerchantUseOwnCapital);
            AssertSuccess(actions, world, PlayerActionIds.MerchantBuyJourneyCargo);
            var container = world.InventoryContainers.Single(item =>
                item.CarrierPersonId == world.PlayerPersonId &&
                item.KindId == "inventory_container.merchant_caravan");

            container.CarrierPersonId = "person.su_shuang";

            Assert.Throws<System.InvalidOperationException>(
                () => world.Validate());
        }

        private static WorldState ResolveM26P1GuardChoice(ulong seed)
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            var world = CreateM26P1MerchantWorld(content, seed);
            var actions = CreateM26P1Actions(world, content);
            AssertSuccess(actions, world, PlayerActionIds.MerchantUseOwnCapital);
            AssertSuccess(actions, world, PlayerActionIds.MerchantBuyJourneyCargo);
            AssertSuccess(actions, world, PlayerActionIds.MerchantStartJourney);
            AdvanceM26P1ToEvent(actions, world);
            AssertSuccess(actions, world, PlayerActionIds.MerchantEventGuard);
            return world;
        }

        private static void CompleteM26P1Delivery(
            PlayerActionService actions,
            WorldState world,
            string eventActionId)
        {
            AssertSuccess(actions, world, PlayerActionIds.MerchantUseOwnCapital);
            AssertSuccess(actions, world, PlayerActionIds.MerchantBuyJourneyCargo);
            AssertSuccess(actions, world, PlayerActionIds.MerchantStartJourney);
            AdvanceM26P1ToEvent(actions, world);
            AssertSuccess(actions, world, eventActionId);
            AdvanceM26P1ToArrival(actions, world);
            AssertSuccess(actions, world, PlayerActionIds.MerchantDeliverCargo);
        }

        private static void AdvanceM26P1ToEvent(
            PlayerActionService actions,
            WorldState world)
        {
            for (var day = 0; day < 20; day++)
            {
                if (actions.QueryActions(world, world.PlayerPersonId).Any(item =>
                        item.Id == PlayerActionIds.MerchantEventHelp))
                {
                    return;
                }
                AssertSuccess(actions, world, PlayerActionIds.Rest);
            }
            Assert.Fail("M26-P1 travel event did not become available.");
        }

        private static void AdvanceM26P1ToArrival(
            PlayerActionService actions,
            WorldState world)
        {
            for (var day = 0; day < 20; day++)
            {
                if (!world.Journeys.Any(item =>
                        item.PersonId == world.PlayerPersonId))
                {
                    return;
                }
                AssertSuccess(actions, world, PlayerActionIds.Rest);
            }
            Assert.Fail("M26-P1 caravan did not arrive.");
        }

        private static PlayerActionResult AssertSuccess(
            PlayerActionService actions,
            WorldState world,
            string actionId)
        {
            var result = actions.Execute(world, world.PlayerPersonId, actionId);
            Assert.That(result.Success, Is.True,
                "Action failed: " + actionId + " / " + result.Summary);
            return result;
        }

        private static PlayerActionService CreateM26P1Actions(
            WorldState world,
            MerchantHouseholdContentRegistry content) =>
            new PlayerActionService(
                new WorldSimulator(world.MasterSeed),
                content);

        private static WorldState CreateM26P1MerchantWorld(
            MerchantHouseholdContentRegistry content,
            ulong seed = 184_001UL) =>
            new NewGameSetupService(content).CreateCustom184World(
                new NewGameCharacterRequest
                {
                    DisplayName = "沈衡",
                    Age = 24,
                    Gender = PersonGender.Male,
                    Identity = StartingIdentity.Merchant,
                    BackgroundId = StartingBackgroundIds.LocalHousehold,
                    StartingLocationId = "location.zhongshan"
                },
                seed);
    }
}
