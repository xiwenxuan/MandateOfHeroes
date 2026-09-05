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
        public void MerchantGameplayEntryTests_RecommendedMerchantHasPlayableView()
        {
            var world = CreateM26ProductWorld();
            var view = Actions(world).InspectMerchantGoal(
                world, world.PlayerPersonId);

            Assert.That(view.IsAvailable, Is.True);
            Assert.That(view.ProductReadiness, Is.Not.Null);
            Assert.That(view.ProductReadiness.PlayerName, Is.EqualTo("沈衡"));
            Assert.That(view.ProductReadiness.CurrentLocationName,
                Does.Contain("中山"));
        }

        [Test]
        public void MerchantGoalVisibilityTests_FirstDecisionAnswersWhyAndNext()
        {
            var world = CreateM26ProductWorld();
            var view = Inspect(world);

            Assert.That(view.FamilySituation, Does.Contain("债务"));
            Assert.That(view.CurrentObjective, Is.Not.Empty);
            Assert.That(view.ProductReadiness.PlayerCash, Is.GreaterThan(0));
            Assert.That(view.ProductReadiness.PressureSummary,
                Does.Contain("债务"));
            Assert.That(view.ProductReadiness.RecommendedNextStep,
                Does.Contain("行动页"));
        }

        [Test]
        public void MerchantMarketKnowledgeTests_QuoteHasProductPlaceSourceAndConfidence()
        {
            var quote = Inspect(CreateM26ProductWorld()).MarketOpportunity;

            Assert.That(quote.ProductName, Is.EqualTo("布帛"));
            Assert.That(quote.OriginLocationName, Does.Contain("中山"));
            Assert.That(quote.TargetLocationName, Does.Contain("涿县"));
            Assert.That(quote.SourceName, Is.Not.Empty);
            Assert.That(quote.LearnedDay, Is.GreaterThanOrEqualTo(0));
            Assert.That(quote.ReliabilityBasisPoints,
                Is.InRange(1, 10_000));
            Assert.That(quote.ReliabilityLabel, Is.Not.Empty);
        }

        [Test]
        public void MerchantQuoteFreshnessTests_OldQuoteIsClearlyMarked()
        {
            var world = CreateM26ProductWorld();
            new WorldSimulator(world.MasterSeed).AdvanceDays(world, 6);

            var quote = Inspect(world).MarketOpportunity;

            Assert.That(quote.AgeDays, Is.EqualTo(6));
            Assert.That(quote.FreshnessLabel, Does.Contain("较旧"));
        }

        [Test]
        public void MerchantPurchasePreviewTests_UsesLivePriceCashAndCapacity()
        {
            var world = CreateM26ProductWorld();
            Execute(world, PlayerActionIds.MerchantUseOwnCapital);
            var view = Inspect(world).ProductReadiness.Purchase;
            var listing = world.MarketListings.Single(item =>
                item.LocationId == "location.zhongshan" &&
                item.CommodityId == "commodity.cloth");

            Assert.That(view.CurrentUnitPrice, Is.EqualTo(listing.Price));
            Assert.That(view.PlannedQuantity, Is.EqualTo(6));
            Assert.That(view.TotalCost,
                Is.EqualTo((long)listing.Price * 6));
            Assert.That(view.CashAfter,
                Is.EqualTo(view.CashBefore - view.TotalCost));
            Assert.That(view.CargoWeightAfter,
                Is.EqualTo(view.CurrentCargoWeight + view.AddedCargoWeight));
            Assert.That(view.CanPurchase, Is.True);
        }

        [Test]
        public void MerchantPurchaseFailureTests_StockFailureDoesNotMutateWorld()
        {
            var world = CreateM26ProductWorld();
            Execute(world, PlayerActionIds.MerchantUseOwnCapital);
            world.MarketListings.Single(item =>
                item.LocationId == "location.zhongshan" &&
                item.CommodityId == "commodity.cloth").Stock = 1;
            var before = WorldSnapshotSerializer.Serialize(world);

            var result = Actions(world).Execute(
                world, world.PlayerPersonId,
                PlayerActionIds.MerchantBuyJourneyCargo);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Summary, Does.Contain("只剩1匹"));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MerchantCarrierCapacityTests_OverloadIsVisibleBeforePurchase()
        {
            var world = CreateM26ProductWorld();
            Execute(world, PlayerActionIds.MerchantUseOwnCapital);
            world.People.Single(item =>
                item.Id == world.PlayerPersonId).CargoCapacity = 1;

            var purchase = Inspect(world).ProductReadiness.Purchase;
            var option = Actions(world).QueryActions(
                world, world.PlayerPersonId).Single(item =>
                    item.Id == PlayerActionIds.MerchantBuyJourneyCargo);

            Assert.That(purchase.CanPurchase, Is.False);
            Assert.That(purchase.Blocker, Does.Contain("超载"));
            Assert.That(purchase.RecoveryHint, Is.Not.Empty);
            Assert.That(option.IsAvailable, Is.False);
        }

        [Test]
        public void MerchantRoutePreviewTests_UsesExistingRouteFacts()
        {
            var world = PreparedCaravan();
            var route = world.Routes.Single(item =>
                item.Id == "route.zhuo_zhongshan");

            var preview = Inspect(world).ProductReadiness.Journey;

            Assert.That(preview.RouteDistanceKilometers,
                Is.EqualTo(route.DistanceKilometers));
            Assert.That(preview.RouteSecurityBasisPoints,
                Is.EqualTo(route.SecurityBasisPoints));
            Assert.That(preview.RequiredProvisions, Is.GreaterThan(0));
            Assert.That(preview.CanDepart, Is.True);
        }

        [Test]
        public void MerchantRouteFailureTests_MissingRouteIsReadableAndSafe()
        {
            var world = PreparedCaravan();
            world.Routes.RemoveAll(item =>
                item.Id == "route.zhuo_zhongshan");
            var before = WorldSnapshotSerializer.Serialize(world);

            var view = Inspect(world).ProductReadiness.Journey;
            var result = Actions(world).Execute(
                world, world.PlayerPersonId,
                PlayerActionIds.MerchantStartJourney);

            Assert.That(view.CanDepart, Is.False);
            Assert.That(view.Blocker, Does.Contain("道路"));
            Assert.That(view.RecoveryHint, Is.Not.Empty);
            Assert.That(result.Success, Is.False);
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MerchantTravelWorldTimeTests_TravelAdvancesWorldAndPosition()
        {
            var world = PreparedCaravan();
            Execute(world, PlayerActionIds.MerchantStartJourney);
            var journey = world.Journeys.Single(item =>
                item.PersonId == world.PlayerPersonId);
            var day = world.AbsoluteDay;
            var remaining = journey.RemainingKilometers;

            Execute(world, PlayerActionIds.Rest);

            Assert.That(world.AbsoluteDay, Is.EqualTo(day + 1));
            Assert.That(journey.RemainingKilometers, Is.LessThan(remaining));
            Assert.That(Inspect(world).ProductReadiness.Journey.IsInTransit,
                Is.True);
        }

        [Test]
        public void MerchantFormalCellRouteTests_DepartureUsesR003FreightAuthority()
        {
            var world = PreparedCaravan();
            Execute(world, PlayerActionIds.MerchantStartJourney);
            var freight = world.CivilianFreights.Single(item =>
                item.CarrierPersonId == world.PlayerPersonId &&
                item.PurposeId ==
                    CivilianFreightPurposeIds.MerchantOwnerCarriage);
            var dispatch = world.InventoryTransactions.Single(item =>
                item.Id == freight.DispatchInventoryTransactionId);

            Assert.That(freight.UsesCellRoute, Is.True);
            Assert.That(freight.CellRoutePlanVersionId,
                Is.EqualTo(HanWorldStrategicCellRouteProvider.PlanVersionId));
            Assert.That(freight.CellRouteAssetHash.Length, Is.EqualTo(64));
            Assert.That(freight.CellRouteOriginCellId64,
                Is.EqualTo(3_352_589UL));
            Assert.That(freight.CellRouteTargetCellId64,
                Is.EqualTo(3_160_413UL));
            Assert.That(freight.CellRouteSegments.Count,
                Is.GreaterThan(58));
            Assert.That(freight.CellRouteSegments.All(item =>
                item.TraversalConditionId ==
                    CellTraversalIds.FormalRoadConditionId &&
                item.FormalWorldObjectId == "route.zhuo_zhongshan"),
                Is.True);
            using (var reader = new WorldMapDataReader(WorldPackageRoot()))
            {
                foreach (var segment in freight.CellRouteSegments)
                {
                    Assert.That(reader.Grid.TryDecode(
                        new WorldMapCellId(segment.FromCellId64),
                        out var fromRow, out var fromColumn), Is.True);
                    Assert.That(reader.Grid.TryDecode(
                        new WorldMapCellId(segment.ToCellId64),
                        out var toRow, out var toColumn), Is.True);
                    Assert.That(Math.Abs(fromRow - toRow) +
                        Math.Abs(fromColumn - toColumn), Is.EqualTo(1));
                }
            }
            Assert.That(dispatch.Type, Is.EqualTo(
                InventoryTransactionType.CivilianFreightDispatched));
            Assert.That(dispatch.SourceCivilianFreightId,
                Is.EqualTo(freight.Id));
            Assert.That(dispatch.Lines.Sum(item => item.QuantityDelta),
                Is.Zero);
            Assert.That(dispatch.Lines.Any(item =>
                item.QuantityDelta < 0), Is.True);
            Assert.That(dispatch.Lines.Any(item =>
                item.QuantityDelta > 0), Is.True);
            world.Validate();
        }

        [Test]
        public void MerchantCellRouteSaveContinuationTests_MidRouteResumesSameCellLedger()
        {
            var world = PreparedCaravan();
            Execute(world, PlayerActionIds.MerchantStartJourney);
            Execute(world, PlayerActionIds.Rest);
            var before = world.CivilianFreights.Single(item =>
                item.PurposeId ==
                    CivilianFreightPurposeIds.MerchantOwnerCarriage);
            var cellBefore = before.CellRouteCurrentCellId64;
            var remainingBefore = before.CellRouteRemainingWeightedCentimetres;
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var loadedFreight = loaded.CivilianFreights.Single(item =>
                item.Id == before.Id);

            Assert.That(loadedFreight.CellRouteCurrentCellId64,
                Is.EqualTo(cellBefore));
            Assert.That(loadedFreight.CellRouteRemainingWeightedCentimetres,
                Is.EqualTo(remainingBefore));
            Execute(loaded, PlayerActionIds.Rest);
            Assert.That(loadedFreight.CellRouteRemainingWeightedCentimetres,
                Is.LessThan(remainingBefore));
            loaded.Validate();
        }

        [Test]
        public void MerchantTravelEventTests_ChoiceChangesFormalWorldAndFeedback()
        {
            var world = PreparedCaravan();
            Execute(world, PlayerActionIds.MerchantStartJourney);
            AdvanceToEvent(world);
            var provisions = Player(world).Provisions;

            var result = Execute(world, PlayerActionIds.MerchantEventGuard);

            Assert.That(Player(world).Provisions, Is.LessThan(provisions));
            Assert.That(result.Summary, Is.Not.Empty);
            Assert.That(world.LifeEvents.Any(item =>
                item.Id.StartsWith("life_event.m26p1.event_guard.",
                    StringComparison.Ordinal)), Is.True);
            Assert.That(world.Relationships.Any(item =>
                item.FromPersonId == world.PlayerPersonId &&
                item.ToPersonId == "person.su_shuang"), Is.True);
        }

        [Test]
        public void MerchantArrivalTests_ShowsArrivalCargoAndCurrentMarketPrice()
        {
            var world = ArrivedCaravan();
            var target = world.MarketListings.Single(item =>
                item.LocationId == "location.zhuo" &&
                item.CommodityId == "commodity.cloth");

            var view = Inspect(world).ProductReadiness;

            Assert.That(view.CurrentLocationName, Is.EqualTo("涿县"));
            Assert.That(view.Journey.IsInTransit, Is.False);
            Assert.That(view.Settlement.SaleQuantity, Is.GreaterThan(0));
            Assert.That(view.Settlement.CurrentSaleUnitPrice,
                Is.EqualTo(target.Price));
        }

        [Test]
        public void MerchantSaleSettlementTests_SaleUsesMarketBatchMoneyAndCommission()
        {
            var world = ArrivedCaravan();
            var player = Player(world);
            var money = player.Wealth;
            var stock = TargetListing(world).Stock;

            Execute(world, PlayerActionIds.MerchantDeliverCargo);
            var settlement = Inspect(world).ProductReadiness.Settlement;

            Assert.That(settlement.HasSale, Is.True);
            Assert.That(settlement.SoldQuantity, Is.EqualTo(6));
            Assert.That(player.Wealth, Is.GreaterThan(money));
            Assert.That(TargetListing(world).Stock, Is.EqualTo(stock + 6));
            Assert.That(world.InventoryTransactions.Any(item =>
                item.Type == InventoryTransactionType.MerchantMarketSold),
                Is.True);
        }

        [Test]
        public void MerchantProfitSummaryTests_ExplainsActualNetResult()
        {
            var world = ArrivedCaravan();
            Execute(world, PlayerActionIds.MerchantDeliverCargo);

            var settlement = Inspect(world).ProductReadiness.Settlement;

            Assert.That(settlement.PurchaseCost, Is.GreaterThan(0));
            Assert.That(settlement.ActualSaleRevenue, Is.GreaterThan(0));
            Assert.That(settlement.ActualCommission, Is.GreaterThan(0));
            Assert.That(settlement.ActualNetResult, Is.EqualTo(
                settlement.ActualSaleRevenue +
                settlement.ActualCommission -
                settlement.PurchaseCost));
        }

        [Test]
        public void MerchantWorldImpactTests_ReportsOnlyCommittedMarketChange()
        {
            var world = ArrivedCaravan();
            var before = Inspect(world).ProductReadiness.Settlement;
            Assert.That(before.WorldImpactSummary, Does.Contain("尚未成交"));

            Execute(world, PlayerActionIds.MerchantDeliverCargo);
            var after = Inspect(world).ProductReadiness.Settlement;

            Assert.That(after.WorldImpactSummary,
                Does.Contain("涿县市场新增6匹"));
            Assert.That(after.WorldImpactSummary, Does.Contain("成交价"));
            Assert.That(after.WorldImpactSummary,
                Does.Not.Contain("拯救"));
        }

        [Test]
        public void MerchantHouseholdGoalTests_SaleLeadsToRealLongTermChoice()
        {
            var world = ArrivedCaravan();
            Execute(world, PlayerActionIds.MerchantDeliverCargo);
            Assert.That(Inspect(world).ProductReadiness.RecommendedNextStep,
                Does.Contain("偿还家债"));

            Execute(world, PlayerActionIds.MerchantRepayFamilyDebt);
            var view = Inspect(world);

            Assert.That(view.Status, Is.EqualTo(TaskStatus.Completed));
            Assert.That(view.TrackedObjective, Does.Contain("越冬储备"));
            Assert.That(view.ProductReadiness.Organization.LongTermGoal,
                Does.Contain("越冬储备"));
        }

        [Test]
        public void MerchantBusinessOrganizationViewTests_UsesRealMembersAndPositions()
        {
            var world = CreateM26ProductWorld();
            var organization = Inspect(world).ProductReadiness.Organization;

            Assert.That(organization.OrganizationName,
                Is.EqualTo("中山商行"));
            Assert.That(organization.Treasury, Is.GreaterThan(0));
            Assert.That(organization.ManagerName, Is.EqualTo("张世平"));
            Assert.That(organization.PlayerPositionName, Is.Not.Empty);
            Assert.That(organization.Members.Any(item =>
                item.PersonName == "沈衡" && item.IsPlayer), Is.True);
            Assert.That(organization.Members.Any(item =>
                item.PersonName == "张世平"), Is.True);
        }

        [Test]
        public void MerchantStorageViewTests_DistinguishesWarehouseFromCaravan()
        {
            var world = PreparedCaravan();
            var organization = Inspect(world).ProductReadiness.Organization;

            Assert.That(organization.WarehouseCapacity, Is.EqualTo(2_000));
            Assert.That(organization.WarehouseSummary,
                Does.Contain("随身货物不会自动转入"));
            Assert.That(world.ProductBatches.Any(item =>
                item.InventoryContainerId ==
                    MerchantTownOperationSystem.ZhongshanWarehouseContainerId),
                Is.False);
            Assert.That(world.ProductBatches.Any(item =>
                item.InventoryContainerId !=
                    MerchantTownOperationSystem.ZhongshanWarehouseContainerId),
                Is.True);
        }

        [Test]
        public void MerchantFailureRecoveryTests_CashFailureKeepsWorldPlayable()
        {
            var world = CreateM26ProductWorld();
            Execute(world, PlayerActionIds.MerchantUseOwnCapital);
            Player(world).Wealth = 0;
            var before = WorldSnapshotSerializer.Serialize(world);
            var preview = Inspect(world).ProductReadiness.Purchase;

            var result = Actions(world).Execute(
                world, world.PlayerPersonId,
                PlayerActionIds.MerchantBuyJourneyCargo);

            Assert.That(preview.Blocker, Does.Contain("现金不足"));
            Assert.That(preview.RecoveryHint, Is.Not.Empty);
            Assert.That(result.Success, Is.False);
            Assert.That(Actions(world).QueryActions(
                world, world.PlayerPersonId).Count, Is.GreaterThan(0));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MerchantSaveLoadUiStateTests_RebuildsEveryCriticalProjection()
        {
            var world = PreparedCaravan();
            AssertProjectionRoundTrip(world, 2, false, false);

            Execute(world, PlayerActionIds.MerchantStartJourney);
            AssertProjectionRoundTrip(world, 2, true, false);
            AdvanceToEvent(world);
            Execute(world, PlayerActionIds.MerchantEventRefuse);
            AssertProjectionRoundTrip(world, 3, true, false);
            AdvanceToArrival(world);
            AssertProjectionRoundTrip(world, 3, false, false);
            Execute(world, PlayerActionIds.MerchantDeliverCargo);
            AssertProjectionRoundTrip(world, 4, false, true);
        }

        [Test]
        public void MerchantReplayTests_SameSeedAndChoicesProduceSameWorld()
        {
            var first = CompleteRoute(184_001UL);
            var second = CompleteRoute(184_001UL);
            var third = CompleteRoute(184_001UL);
            var expected = WorldSnapshotSerializer.Serialize(first);

            Assert.That(WorldSnapshotSerializer.Serialize(second),
                Is.EqualTo(expected));
            Assert.That(WorldSnapshotSerializer.Serialize(third),
                Is.EqualTo(expected));
            Assert.That(Inspect(second).ProductReadiness.Settlement
                .ActualNetResult, Is.EqualTo(Inspect(first).ProductReadiness
                    .Settlement.ActualNetResult));
            Assert.That(Inspect(third).ProductReadiness.Settlement
                .ActualNetResult, Is.EqualTo(Inspect(first).ProductReadiness
                    .Settlement.ActualNetResult));
        }

        [Test]
        public void MerchantPlayerCopyAuditTests_ProjectionHidesInternalNames()
        {
            var departedWorld = PreparedCaravan();
            Execute(departedWorld, PlayerActionIds.MerchantStartJourney);
            var departedView = Inspect(departedWorld);
            var world = ArrivedCaravan();
            Execute(world, PlayerActionIds.MerchantDeliverCargo);
            var view = Inspect(world);
            var text = string.Join("\n", new[]
            {
                view.CurrentObjective,
                view.TrackedObjective,
                view.FamilySituation,
                departedView.LatestImportantResult,
                view.LatestImportantResult,
                view.MarketOpportunity.FreshnessLabel,
                view.ProductReadiness.RecommendedNextStep,
                view.ProductReadiness.Purchase.OwnershipSummary,
                view.ProductReadiness.Journey.RoadStatus,
                view.ProductReadiness.Settlement.WorldImpactSummary,
                view.ProductReadiness.Organization.WarehouseSummary,
                view.ProductReadiness.Organization.LongTermGoal
            });

            var banned = new[]
            {
                "ProductBatchState", "InventoryTransaction",
                "FormalFreight", "CellTraversalEdge", "WorldCommand",
                "ValidationResult", "task_definition.", "location.",
                "commodity.", "civilian_freight.", "TODO", "Debug"
            };
            for (var i = 0; i < banned.Length; i++)
            {
                Assert.That(text, Does.Not.Contain(banned[i]), banned[i]);
            }
            Assert.That(
                departedView.ProductReadiness.Journey.RoadStatus,
                Does.Not.Match(@"\d{7,}"),
                "player-facing route status must not expose raw Cell IDs");
        }

        private static WorldState CreateM26ProductWorld(
            ulong seed = 184_001UL)
        {
            var content = MerchantHouseholdContentRegistry.CreateCore();
            return new NewGameSetupService(content).CreateCustom184World(
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

        private static PlayerActionService Actions(WorldState world) =>
            new PlayerActionService(
                new WorldSimulator(
                    world.MasterSeed,
                    strategicCellRouteProvider:
                        new HanWorldStrategicCellRouteProvider(Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "Assets",
                            "StreamingAssets",
                            "WorldMap",
                            "HanWorldV1"))),
                MerchantHouseholdContentRegistry.CreateCore());

        private static string WorldPackageRoot() => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets",
            "StreamingAssets",
            "WorldMap",
            "HanWorldV1");

        private static MerchantHouseholdGoalView Inspect(WorldState world) =>
            Actions(world).InspectMerchantGoal(
                world, world.PlayerPersonId);

        private static PersonState Player(WorldState world) =>
            world.People.Single(item => item.Id == world.PlayerPersonId);

        private static MarketListingState TargetListing(WorldState world) =>
            world.MarketListings.Single(item =>
                item.LocationId == "location.zhuo" &&
                item.CommodityId == "commodity.cloth");

        private static PlayerActionResult Execute(
            WorldState world, string actionId)
        {
            var result = Actions(world).Execute(
                world, world.PlayerPersonId, actionId);
            Assert.That(result.Success, Is.True,
                actionId + " / " + result.Summary);
            return result;
        }

        private static WorldState PreparedCaravan()
        {
            var world = CreateM26ProductWorld();
            Execute(world, PlayerActionIds.MerchantUseOwnCapital);
            Execute(world, PlayerActionIds.MerchantBuyJourneyCargo);
            return world;
        }

        private static WorldState ArrivedCaravan()
        {
            var world = PreparedCaravan();
            Execute(world, PlayerActionIds.MerchantStartJourney);
            AdvanceToEvent(world);
            Execute(world, PlayerActionIds.MerchantEventRefuse);
            AdvanceToArrival(world);
            return world;
        }

        private static void AdvanceToEvent(WorldState world)
        {
            for (var day = 0; day < 20; day++)
            {
                if (Actions(world).QueryActions(
                        world, world.PlayerPersonId).Any(item =>
                        item.Id == PlayerActionIds.MerchantEventHelp))
                {
                    return;
                }
                Execute(world, PlayerActionIds.Rest);
            }
            Assert.Fail("Merchant event was not reached.");
        }

        private static void AdvanceToArrival(WorldState world)
        {
            for (var day = 0; day < 20; day++)
            {
                if (!world.Journeys.Any(item =>
                        item.PersonId == world.PlayerPersonId))
                {
                    return;
                }
                Execute(world, PlayerActionIds.Rest);
            }
            Assert.Fail("Merchant destination was not reached.");
        }

        private static void AssertProjectionRoundTrip(
            WorldState world, int phase, bool inTransit, bool hasSale)
        {
            var before = Inspect(world).ProductReadiness;
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var afterGoal = Inspect(loaded);
            var after = afterGoal.ProductReadiness;

            Assert.That(afterGoal.Phase, Is.EqualTo(phase));
            Assert.That(after.PlayerCash, Is.EqualTo(before.PlayerCash));
            Assert.That(after.Purchase.OwnershipSummary,
                Is.EqualTo(before.Purchase.OwnershipSummary));
            Assert.That(after.Journey.IsInTransit, Is.EqualTo(inTransit));
            Assert.That(after.Settlement.HasSale, Is.EqualTo(hasSale));
            Assert.That(after.Organization.LongTermGoal,
                Is.EqualTo(before.Organization.LongTermGoal));
        }

        private static WorldState CompleteRoute(ulong seed)
        {
            var world = CreateM26ProductWorld(seed);
            Execute(world, PlayerActionIds.MerchantUseOwnCapital);
            Execute(world, PlayerActionIds.MerchantBuyJourneyCargo);
            Execute(world, PlayerActionIds.MerchantStartJourney);
            AdvanceToEvent(world);
            Execute(world, PlayerActionIds.MerchantEventGuard);
            AdvanceToArrival(world);
            Execute(world, PlayerActionIds.MerchantDeliverCargo);
            Execute(world, PlayerActionIds.MerchantRepayFamilyDebt);
            return world;
        }
    }
}
