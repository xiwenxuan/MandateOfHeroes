using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public void LuoyangIntegratedSupplyBaselineTests_ThirtyDayScaleAndFormalChainRemainConservative()
        {
            var source = new Luoyang184OuterSupplyRemediationPopulationSource(
                RemediationRoot);
            var livingSystem = new Luoyang184LivingWorldSystem(source);
            var runtime = livingSystem.CreateRuntime(42_001UL);
            livingSystem.AdvanceTo(runtime, 30);
            var compactSummary = livingSystem.BuildWorldSummary(runtime);
            Assert.That(runtime.Workforce.Count, Is.EqualTo(700_000));
            Assert.That(runtime.Households.Count, Is.EqualTo(142_980));
            Assert.That(new LuoyangFoodConservationAuditor().Audit(runtime)
                .DifferenceMilliunits, Is.Zero);

            var formal = PrepareCivilianFreightWorld(42_002UL, 20);
            formal.FreightSystem.Dispatch(formal.World, formal.Request);
            new WorldSimulator(formal.World.MasterSeed, formal.Content)
                .AdvanceSegments(formal.World, 5);
            AssertFormalFoodBalanced(formal);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE baseline_30d",
                "scope=split_runtime",
                "persons=" + runtime.Workforce.Count,
                "households=" + runtime.Households.Count,
                "food_stock=" + compactSummary.FoodStockMilliunits,
                "food_consumed=" + compactSummary.FoodConsumptionMilliunits,
                "food_shortage=" + compactSummary.FoodShortageMilliunits,
                "shortfall_households=" +
                    compactSummary.HouseholdShortageCount,
                "compact_difference=" +
                    new LuoyangFoodConservationAuditor().Audit(runtime)
                        .DifferenceMilliunits,
                "formal_fixture_difference=0"));
        }

        [Test]
        public void LuoyangSupplyProjectionTests_ReportsFormalSourcesDemandPriceAndFreight()
        {
            var fixture = PrepareCivilianFreightWorld(42_003UL, 20);
            fixture.FreightSystem.RegisterCarrier(
                fixture.World,
                new CivilianCarrierRegistrationRequest
                {
                    CarrierPersonId = fixture.Carrier.Id,
                    TransportInventoryContainerId = fixture.Transport.Id,
                    BaseFee = 1,
                    FeePerKilometer = 1,
                    FeePerHundredUnits = 1,
                    MaximumDistanceKilometers = 100,
                    RoutePolicyId =
                        CivilianFreightRoutePolicyIds.ShortestKnown,
                    KnownRouteIds = new List<string>
                    {
                        "route.freight_origin_destination"
                    }
                });
            fixture.Buyer.FoodSecurityBasisPoints = 5_000;
            fixture.FreightSystem.Dispatch(fixture.World, fixture.Request);

            var beforeProjection = WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content);
            var projection = new LuoyangSupplyProjectionSystem(
                    fixture.Content)
                .BuildCityProjection(fixture.World,
                    IntegratedSupplySelection());
            Assert.That(WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                Is.EqualTo(beforeProjection),
                "The Supply Projection must remain read-only.");

            Assert.That(projection.IncomingFreightQuantity,
                Is.EqualTo(20));
            Assert.That(projection.ActiveFreightCount, Is.EqualTo(1));
            Assert.That(projection.ActiveCarrierCount, Is.EqualTo(1));
            Assert.That(projection.HouseholdShortfallCount,
                Is.EqualTo(1));
            Assert.That(projection.FoodShortfallPersonCount,
                Is.EqualTo(1));
            Assert.That(projection.DailyFoodDemandNutritionBasisUnits,
                Is.GreaterThan(0));
            Assert.That(projection.SupplySourceCount,
                Is.GreaterThanOrEqualTo(1));
            Assert.That(projection.ProductPrices, Is.Not.Empty);
            Assert.That(projection.ProductPrices[0].ExplanationFactorIds,
                Does.Contain("formal.household-shortfall"));
            AssertFormalFoodBalanced(fixture);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE projection",
                "usable_stock=" + projection.CurrentUsableFoodStock,
                "daily_demand=" +
                    projection.DailyFoodDemandNutritionBasisUnits,
                "days_of_supply=" + projection.DaysOfSupply,
                "incoming=" + projection.IncomingFreightQuantity,
                "shortfall_households=" +
                    projection.HouseholdShortfallCount,
                "shortfall_persons=" + projection.FoodShortfallPersonCount,
                "active_carriers=" + projection.ActiveCarrierCount,
                "sources=" + projection.SupplySourceCount));
        }

        [Test]
        public void LuoyangMarketSupplyPriceTests_FormalQuoteAndTradeExplainShockAndRecovery()
        {
            var fixture = PrepareFormalMarketCommandWorld(42_004UL);
            var governanceId = fixture.World.CountyGovernances[0].Id;
            var productId = fixture.SellOrder.ProductDefinitionId;
            var beforeQuote = WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content);
            var normal = FormalCountyMarketSystem.BuildQuote(
                fixture.World, governanceId, productId);
            Assert.That(WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content),
                Is.EqualTo(beforeQuote),
                "The formal market quote must remain read-only.");
            fixture.Market.CancelOrder(
                fixture.World, fixture.SellOrder.Id, "stress_reset");
            fixture.Market.CancelOrder(
                fixture.World, fixture.BuyOrder.Id, "stress_reset");
            fixture.Buyer.FoodSecurityBasisPoints = 0;
            var shock = FormalCountyMarketSystem.BuildQuote(
                fixture.World, governanceId, productId);
            Assert.That(shock.ReferenceUnitPrice,
                Is.GreaterThan(normal.ReferenceUnitPrice));
            Assert.That(shock.ExplanationFactorIds,
                Does.Contain("formal.household-shortfall"));

            var sell = fixture.Market.CreateSellOrder(
                fixture.World,
                governanceId,
                fixture.Seller.Id,
                fixture.World.VillageFacilities.Find(item =>
                    item.OwnerFamilyId == fixture.Seller.Id &&
                    item.Kind == VillageFacilityKind.HouseholdGranary).Id,
                productId,
                1,
                shock.SuggestedSellMinimumUnitPrice,
                0,
                fixture.World.AbsoluteDay + 5);
            var buy = fixture.Market.CreateBuyOrder(
                fixture.World,
                governanceId,
                fixture.Buyer.Id,
                fixture.World.VillageFacilities.Find(item =>
                    item.OwnerFamilyId == fixture.Buyer.Id &&
                    item.Kind == VillageFacilityKind.HouseholdGranary).Id,
                productId,
                1,
                shock.SuggestedBuyMaximumUnitPrice,
                0,
                fixture.World.AbsoluteDay + 5);
            fixture.Market.ResolveDaily(fixture.World);
            Assert.That(sell.Status, Is.EqualTo(
                FormalMarketOrderStatus.Filled));
            Assert.That(buy.Status, Is.EqualTo(
                FormalMarketOrderStatus.Filled));
            Assert.That(fixture.World.FormalMarketPrices.Find(item =>
                    item.CountyGovernanceId == governanceId &&
                    item.ProductDefinitionId == productId)
                .LastTradeUnitPrice,
                Is.EqualTo(shock.SuggestedSellMinimumUnitPrice));

            fixture.Buyer.FoodSecurityBasisPoints = 10_000;
            var recovered = FormalCountyMarketSystem.BuildQuote(
                fixture.World, governanceId, productId);
            Assert.That(recovered.ReferenceUnitPrice,
                Is.LessThan(shock.ReferenceUnitPrice));
            fixture.World.Validate();
            Console.WriteLine(string.Join(" ",
                "EVIDENCE price_series",
                "normal=" + normal.ReferenceUnitPrice,
                "shock=" + shock.ReferenceUnitPrice,
                "recovered=" + recovered.ReferenceUnitPrice,
                "trade=" +
                    fixture.World.FormalMarketPrices.Find(item =>
                        item.CountyGovernanceId == governanceId &&
                        item.ProductDefinitionId == productId)
                        .LastTradeUnitPrice));
        }

        [Test]
        public void GateIntegratedSupplyShockTests_FormalGateBlocksFreightAndProjection()
        {
            var gate = PrepareIntegratedGateShock(42_005UL);
            var projection = new LuoyangSupplyProjectionSystem(
                    gate.Fixture.Content)
                .BuildCityProjection(gate.Fixture.World,
                    IntegratedSupplySelection());
            Assert.That(gate.Freight.CellRouteWaiting, Is.True);
            Assert.That(projection.BlockedFreightCount, Is.EqualTo(1));
            Assert.That(projection.BlockedFreightQuantity,
                Is.GreaterThan(0));
            Assert.That(projection.ProductPrices[0].ExplanationFactorIds,
                Does.Contain("formal.transport-disruption"));
            AssertFormalFoodBalanced(gate.Fixture);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE gate_shock",
                "gate=" + gate.GateId,
                "blocked_count=" + projection.BlockedFreightCount,
                "blocked_quantity=" + projection.BlockedFreightQuantity,
                "incoming=" + projection.IncomingFreightQuantity));
        }

        [Test]
        public void GateRecoveryMarketTests_ReopenResumesSameFreightOnce()
        {
            var gate = PrepareIntegratedGateShock(42_006UL);
            ReopenAndFinish(gate);
            Assert.That(gate.Freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(gate.Freight.DeliveredQuantity +
                gate.Freight.NaturalLossQuantity,
                Is.EqualTo(gate.Freight.DispatchedQuantity));
            Assert.That(gate.Fixture.World.CivilianFreightLedgerEntries.Count(
                item => item.CivilianFreightId == gate.Freight.Id &&
                    item.Type == CivilianFreightLedgerType.Delivered),
                Is.EqualTo(1));
            AssertFormalFoodBalanced(gate.Fixture);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE gate_recovery",
                "dispatched=" + gate.Freight.DispatchedQuantity,
                "delivered=" + gate.Freight.DeliveredQuantity,
                "loss=" + gate.Freight.NaturalLossQuantity));
        }

        [Test]
        public void MultiGateFreightRerouteTests_RecordsCurrentContinuousLegBoundary()
        {
            var gate = PrepareIntegratedGateShock(42_007UL);
            Assert.That(gate.Fixture.World.LuoyangPassageTraversals.Count(
                item => item.FacilityDefinitionId !=
                    "facility.public.bridge"), Is.GreaterThan(1));
            Assert.That(gate.Freight.CellRouteSegments.Where(item =>
                    item.TraversalConditionId ==
                        CellTraversalIds.FormalPassageConditionId)
                .Select(item => item.FormalWorldObjectId)
                .Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(1),
                "The route may persist several segments through one selected Gate; " +
                "network-level multi-Gate freight choice remains an acceptance boundary.");
        }

        [Test]
        public void RoadBlockSupplyTests_PackAnimalReroutesWithoutDuplicatingCargo()
        {
            var road = CreateRoadRerouteFreightFixture(
                42_008UL, MovementCapabilityIds.PackAnimal);
            var freight = road.Fixture.FreightSystem.Dispatch(
                road.Fixture.World, road.Fixture.Request);
            road.Fixture.World.LuoyangRoadOperationalSegments.Single()
                .StatusId =
                LuoyangFormalPlayerMovementIds.DestroyedRoadStatusId;
            new TravelSystem().AdvanceJourneysOneSegment(
                road.Fixture.World);
            Assert.That(freight.CellRouteWaiting, Is.True);
            Assert.That(road.Fixture.FreightSystem.TryRerouteCellFreight(
                road.Fixture.World, freight), Is.True);
            Assert.That(freight.CellRouteRevision, Is.EqualTo(1));
            AssertFormalFoodBalanced(road.Fixture);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE road_shock",
                "route_revision=" + freight.CellRouteRevision,
                "remaining=" + freight.RemainingCargoQuantity));
        }

        [Test]
        public void ProductionShockSupplyTests_EarlyHarvestReducesFormalBatchYield()
        {
            var content = LoadHanFoodProductionContent();
            // Use the established agriculture fixture seed whose household
            // granary retains capacity for the formal harvest output.
            var early = VillagePrototypeFactory.Create(200, 25_902UL);
            var normal = VillagePrototypeFactory.Create(200, 25_902UL);
            early.ProductionContentManifest = content.CreateManifest();
            normal.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(early);
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(normal);
            var earlyOrder = CreateEarlyHarvestTestOrder(early, content);
            var normalOrder = CreateEarlyHarvestTestOrder(normal, content);
            var earlySystem = new AgricultureProductionSystem(
                early.MasterSeed, content);
            var normalSystem = new AgricultureProductionSystem(
                normal.MasterSeed, content);
            early.AbsoluteDay = earlyOrder.PlantingDay +
                (earlyOrder.HarvestDay - earlyOrder.PlantingDay) * 79 / 100;
            Assert.That(earlySystem.TryHarvestEarly(
                early, earlyOrder.Id), Is.False,
                "Harvest must remain unavailable before 80% maturity.");
            early.AbsoluteDay = earlyOrder.PlantingDay +
                (earlyOrder.HarvestDay - earlyOrder.PlantingDay) * 80 / 100;
            Assert.That(earlySystem.TryHarvestEarly(
                early, earlyOrder.Id), Is.True,
                "Harvest must become available at 80% maturity.");
            normal.AbsoluteDay = normalOrder.HarvestDay;
            normalSystem.ResolveDueOrders(normal, normal.Villages[0].Id);
            Assert.That(earlyOrder.ProducedQuantity,
                Is.LessThan(normalOrder.ProducedQuantity));
            Assert.That(early.ProductBatches.Exists(item =>
                item.SourceWorkOrderId == earlyOrder.Id), Is.True,
                "Early harvest must create a formal batch with work-order provenance.");
            Console.WriteLine(string.Join(" ",
                "EVIDENCE production_shock",
                "early_yield=" + earlyOrder.ProducedQuantity,
                "normal_yield=" + normalOrder.ProducedQuantity,
                "maturity=" + earlyOrder.MaturityBasisPointsAtHarvest));
        }

        [Test]
        public void CarrierShortageSupplyTests_NoCarrierCreatesNoPhantomFreight()
        {
            var fixture = PrepareCivilianFreightWorld(42_010UL, 20);
            Assert.That(fixture.FreightSystem.GenerateDemands(
                fixture.World), Is.EqualTo(1));
            Assert.That(fixture.FreightSystem.GenerateOffers(
                fixture.World), Is.Zero);
            Assert.That(fixture.FreightSystem.DispatchBestOffers(
                fixture.World), Is.Zero);
            Assert.That(fixture.World.CivilianFreights, Is.Empty);
            Assert.That(fixture.World.CivilianFreightDemands,
                Has.Count.EqualTo(1));
            Console.WriteLine(
                "EVIDENCE carrier_shortage active_demand=1 offers=0 freights=0");
        }

        [Test]
        public void StorageBottleneckSupplyTests_WaitsThenReceivesRemainingCargoOnce()
        {
            var fixture = PrepareCivilianFreightWorld(42_011UL, 1_000);
            fixture.BuyerStorage.Capacity = 400;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 5);
            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.AwaitingReceipt));
            Assert.That(freight.RemainingCargoQuantity,
                Is.GreaterThan(0));
            fixture.BuyerStorage.Capacity = 2_000;
            fixture.FreightSystem.ResolveArrivals(fixture.World);
            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            AssertFormalFoodBalanced(fixture);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE storage_bottleneck",
                "dispatched=" + freight.DispatchedQuantity,
                "delivered=" + freight.DeliveredQuantity,
                "loss=" + freight.NaturalLossQuantity,
                "remaining=" + freight.RemainingCargoQuantity));
        }

        [Test]
        public void PublicProcurementIntegratedTests_UsesBudgetSellerBatchAndFormalTrade()
        {
            var fixture = PreparePublicReliefProcurementWorld(
                42_012UL, true);
            fixture.World.AbsoluteDay = 31;
            fixture.World.Segment = (byte)DaySegment.Dawn;
            var scheduler = new PublicReliefProcurementCommandScheduler(
                new PublicReliefProcurementSystem(fixture.Content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            runtime.RegisterEventHandler(
                scheduler.CreateProjectionHandler());
            var before = FormalCashTotal(fixture.World);
            Assert.That(runtime.ProcessDue(fixture.World)
                .CommittedTransactions, Is.EqualTo(1));
            Assert.That(fixture.World.PublicReliefProcurementTrades,
                Has.Count.EqualTo(1));
            Assert.That(FormalCashTotal(fixture.World), Is.EqualTo(before));
            Assert.That(new FormalFoodConservationAuditor().Audit(
                fixture.World, fixture.Content).Difference, Is.Zero);
            var procurement = fixture.World.PublicReliefProcurementTrades[0];
            Console.WriteLine(string.Join(" ",
                "EVIDENCE government_procurement",
                "quantity=" + procurement.Quantity,
                "money=" + procurement.MoneyTransferred,
                "cash_net=0",
                "food_difference=0"));
        }

        [Test]
        public void ReliefIntegratedTests_TransfersAndConsumesTraceablePublicFood()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(42_013UL);
            SeedMonthlyShortfallAndVillageFood(fixture, 2);
            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 1);
            Assert.That(fixture.World.HouseholdReliefPickups.Exists(item =>
                item.DeliveredPhysicalQuantity > 0), Is.True);
            Assert.That(fixture.World.HouseholdReliefConsumptions.Exists(
                item => item.ConsumedPhysicalQuantity > 0), Is.True);
            Assert.That(new FormalFoodConservationAuditor().Audit(
                fixture.World, fixture.Content).Difference, Is.Zero);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE relief",
                "delivered=" +
                    fixture.World.HouseholdReliefPickups.Sum(item =>
                        item.DeliveredPhysicalQuantity),
                "consumed=" +
                    fixture.World.HouseholdReliefConsumptions.Sum(item =>
                        item.ConsumedPhysicalQuantity),
                "food_difference=0"));
        }

        [Test]
        public void HouseholdSupplyDistributionTests_PreservesHouseholdLevelVariation()
        {
            var fixture = PrepareFormalHouseholdFoodCommandWorld(42_014UL);
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(fixture.Scheduler.CreateCommandHandler());
            fixture.Scheduler.EnsureDueCommands(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            Assert.That(fixture.World.HouseholdReliefPickups,
                Is.Not.Empty);
            Assert.That(fixture.World.HouseholdReliefPickups.Select(item =>
                    item.RequestedNutritionBasisUnits).Distinct().Count(),
                Is.GreaterThan(1));
            Assert.That(fixture.World.HouseholdReliefPickups.All(item =>
                item.FamilyId != string.Empty &&
                item.AffectedPersonCountAtAuthorization > 0), Is.True);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE household_distribution",
                "households=" + fixture.World.HouseholdReliefPickups.Count,
                "distinct_demands=" +
                    fixture.World.HouseholdReliefPickups.Select(item =>
                        item.RequestedNutritionBasisUnits).Distinct().Count(),
                "affected_persons=" +
                    fixture.World.HouseholdReliefPickups.Sum(item =>
                        item.AffectedPersonCountAtAuthorization)));
        }

        [Test]
        public void SupplyDemandStormPreventionTests_RepeatedPlanningKeepsOneDemand()
        {
            var fixture = PrepareCivilianFreightWorld(42_015UL, 100);
            for (var day = 0; day < 10; day++)
                fixture.FreightSystem.GenerateDemands(fixture.World);
            Assert.That(fixture.World.CivilianFreightDemands,
                Has.Count.EqualTo(1));
            Assert.That(fixture.World.CivilianFreightDemands[0].Status,
                Is.EqualTo(CivilianFreightDemandStatus.Active));
        }

        [Test]
        public void OutstandingFreightDemandTests_OnlyPlansUncommittedOrderRemainder()
        {
            var fixture = PrepareCivilianFreightWorld(42_016UL, 20);
            fixture.Request.Quantity = 8;
            fixture.FreightSystem.Dispatch(fixture.World, fixture.Request);
            Assert.That(fixture.FreightSystem.GenerateDemands(
                fixture.World), Is.EqualTo(1));
            Assert.That(fixture.World.CivilianFreightDemands.Single()
                .Quantity, Is.EqualTo(12));
        }

        [Test]
        public void PlayerMerchantWorldImpactTests_PlayerDispatchUsesFormalMarketAndFreight()
        {
            var fixture = PrepareCivilianFreightWorld(42_017UL, 20);
            fixture.World.PlayerPersonId = fixture.Carrier.Id;
            var before = FormalCashTotal(fixture.World);
            var result = new LuoyangPlayerSupplyInterventionService()
                .DispatchMarketFreight(
                    fixture.World, fixture.FreightSystem, fixture.Request);
            Assert.That(result.PlayerPersonId,
                Is.EqualTo(fixture.World.PlayerPersonId));
            Assert.That(result.CivilianFreightId, Is.Not.Empty);
            Assert.That(fixture.World.CivilianFreights.Exists(item =>
                item.Id == result.CivilianFreightId), Is.True);
            Assert.That(FormalCashTotal(fixture.World), Is.EqualTo(before));
            AssertFormalFoodBalanced(fixture);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE player_intervention",
                "player=" + result.PlayerPersonId,
                "freight=" + result.CivilianFreightId,
                "quantity=" + result.Quantity,
                "unit_price=" + result.UnitPrice,
                "freight_fee=" + result.FreightFee,
                "cash_net=0",
                "food_difference=0"));
        }

        [Test]
        public void IntegratedFoodConservationTests_DispatchLossReceiptAndConsumptionBalance()
        {
            var fixture = PrepareCivilianFreightWorld(42_018UL, 1_000);
            fixture.FreightSystem.Dispatch(fixture.World, fixture.Request);
            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 5);
            var result = new FoodInventorySystem(fixture.Content)
                .ConsumeFamilyFood(
                    fixture.World,
                    fixture.Buyer.Id,
                    fixture.BuyerStorage.Id,
                    fixture.Buyer.HeadPersonId,
                    10_000L);
            Assert.That(result.ConsumedPhysicalQuantity,
                Is.GreaterThan(0));
            AssertFormalFoodBalanced(fixture);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE food_conservation",
                "consumed=" + result.ConsumedPhysicalQuantity,
                "difference=0"));
        }

        [Test]
        public void IntegratedCashTransferTests_TradeAndFreightAreNetZeroInternalTransfers()
        {
            var fixture = PrepareCivilianFreightWorld(42_019UL, 100);
            var before = FormalCashTotal(fixture.World);
            fixture.FreightSystem.Dispatch(fixture.World, fixture.Request);
            Assert.That(FormalCashTotal(fixture.World), Is.EqualTo(before));
            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 5);
            Assert.That(FormalCashTotal(fixture.World), Is.EqualTo(before));
            Console.WriteLine(
                "EVIDENCE cash_transfer internal_transfer_net=0");
        }

        [Test]
        public void IntegratedSupplySaveLoadTests_ProjectionAndFreightRoundTrip()
        {
            var fixture = PrepareCivilianFreightWorld(42_020UL, 20);
            fixture.FreightSystem.Dispatch(fixture.World, fixture.Request);
            var before = new LuoyangSupplyProjectionSystem(fixture.Content)
                .BuildCityProjection(fixture.World,
                    IntegratedSupplySelection());
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content), fixture.Content);
            var after = new LuoyangSupplyProjectionSystem(fixture.Content)
                .BuildCityProjection(loaded, IntegratedSupplySelection());
            Assert.That(after.IncomingFreightQuantity,
                Is.EqualTo(before.IncomingFreightQuantity));
            Assert.That(after.ActiveFreightCount,
                Is.EqualTo(before.ActiveFreightCount));
            Assert.That(after.ProductPrices.Select(PriceDigest),
                Is.EqualTo(before.ProductPrices.Select(PriceDigest)));
            loaded.Validate();
            Console.WriteLine(string.Join(" ",
                "EVIDENCE save_load",
                "incoming_before=" + before.IncomingFreightQuantity,
                "incoming_after=" + after.IncomingFreightQuantity,
                "freights_before=" + before.ActiveFreightCount,
                "freights_after=" + after.ActiveFreightCount,
                "price_digest_equal=true"));
        }

        [Test]
        public void IntegratedSupplyReplayTests_ThreeFormalRunsAreByteIdentical()
        {
            string expected = null;
            for (var run = 0; run < 3; run++)
            {
                var fixture = PrepareCivilianFreightWorld(42_021UL, 20);
                fixture.World.PlayerPersonId = fixture.Carrier.Id;
                new LuoyangPlayerSupplyInterventionService()
                    .DispatchMarketFreight(
                        fixture.World,
                        fixture.FreightSystem,
                        fixture.Request);
                var actual = WorldSnapshotSerializer.Serialize(
                    fixture.World, fixture.Content);
                if (expected == null) expected = actual;
                else Assert.That(actual, Is.EqualTo(expected));
            }
            Console.WriteLine(
                "EVIDENCE replay identical_runs=3 total_runs=3");
        }

        [Test]
        public void IntegratedOneYearStabilityTests_FormalWorldHasNoEconomicInvariantFailure()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 42_022UL);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);
            new WorldSimulator(world.MasterSeed, content)
                .AdvanceDays(world, 360);
            var audit = new FormalFoodConservationAuditor().Audit(
                world, content);
            Assert.That(audit.Balanced, Is.True);
            Assert.That(audit.Difference, Is.Zero);
            Assert.That(world.ProductBatches.All(item =>
                item.Quantity >= 0 && item.ReservedQuantity >= 0 &&
                item.ReservedQuantity <= item.Quantity), Is.True);
            world.Validate();
            Console.WriteLine(string.Join(" ",
                "EVIDENCE baseline_1y",
                "scope=formal_200_person_fixture",
                "persons=" + world.People.Count,
                "food_difference=" + audit.Difference,
                "balanced=" + audit.Balanced));
        }

        [Test]
        public void IntegratedSupplyPerformanceTests_ActualPopulationRuntimeAndProjectionAreBounded()
        {
            var source = new Luoyang184OuterSupplyRemediationPopulationSource(
                RemediationRoot);
            var system = new Luoyang184LivingWorldSystem(source);
            var watch = Stopwatch.StartNew();
            var runtime = system.CreateRuntime(42_023UL);
            system.AdvanceTo(runtime, 1);
            watch.Stop();
            var compactElapsed = watch.ElapsedMilliseconds;
            Assert.That(runtime.Workforce.Count, Is.EqualTo(700_000));
            Assert.That(runtime.Households.Count, Is.EqualTo(142_980));
            Assert.That(watch.ElapsedMilliseconds,
                Is.LessThan(30_000));

            var formal = PrepareCivilianFreightWorld(42_024UL, 20);
            watch.Restart();
            for (var i = 0; i < 100; i++)
                new LuoyangSupplyProjectionSystem(formal.Content)
                    .BuildCityProjection(formal.World,
                        IntegratedSupplySelection());
            watch.Stop();
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(2_000));
            Console.WriteLine(string.Join(" ",
                "EVIDENCE performance",
                "scope=split_runtime",
                "compact_init_plus_day_ms=" + compactElapsed,
                "compact_init_ms=" +
                    runtime.Performance.InitializationMilliseconds,
                "compact_day_ms=" + runtime.Performance.OneDayMilliseconds,
                "compact_peak_managed_bytes=" +
                    runtime.Performance.PeakManagedMemoryBytes,
                "formal_fixture_projection_100_ms=" +
                    watch.ElapsedMilliseconds));
        }

        private static LuoyangSupplyCatchmentSelection
            IntegratedSupplySelection() =>
            new LuoyangSupplyCatchmentSelection
            {
                SupplyLocationIds = new List<string>
                {
                    "location.freight_origin_village",
                    "location.freight_destination_village"
                },
                CityLocationIds = new List<string>
                {
                    "location.freight_destination_village"
                },
                SettlementIds = new List<string>
                {
                    "village.freight_origin",
                    "village.freight_destination"
                }
            };

        private static void AssertFormalFoodBalanced(
            CivilianFreightFixture fixture)
        {
            var audit = new FormalFoodConservationAuditor().Audit(
                fixture.World, fixture.Content);
            Assert.That(audit.Balanced, Is.True);
            Assert.That(audit.Difference, Is.Zero);
            fixture.World.Validate();
        }

        private static long FormalCashTotal(WorldState world)
        {
            return checked(
                world.Families.Sum(item => item.Wealth) +
                world.Organizations.Sum(item => item.Treasury) +
                world.FormalMarketOrders.Sum(item => item.EscrowMoney) +
                world.CivilianFreights.Sum(item => item.FreightFeeEscrow));
        }

        private static string PriceDigest(
            LuoyangFoodPriceProjection price) => string.Join(":",
            price.ProductDefinitionId,
            price.EquilibriumUnitPrice,
            price.LastTradeUnitPrice,
            price.LastTradeDay,
            price.ActiveBuyDemand,
            price.ActiveSellSupply,
            price.RecentTradeQuantity);

        private static IntegratedGateFixture PrepareIntegratedGateShock(
            ulong seed)
        {
            var fixture = PrepareCivilianFreightWorld(seed, 12);
            var passagePlan = BuildLuoyangPassagePlan();
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                passagePlan);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var gateId = fixture.World.LuoyangPassageTraversals.First(item =>
                item.FacilityDefinitionId != "facility.public.bridge")
                .FacilityId;
            var cellPlan = BuildFreightGateCellPlan(
                gateId, out var originCellId64, out var targetCellId64);
            fixture.FreightSystem = new CivilianFreightSystem(
                fixture.World.MasterSeed, fixture.Content, cellPlan);
            fixture.Request.OriginCellId64 = originCellId64;
            fixture.Request.TargetCellId64 = targetCellId64;
            fixture.Request.MovementCapabilityId =
                MovementCapabilityIds.PackAnimal;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            passageSystem.EnqueueTransition(
                fixture.World,
                runtime,
                gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.integrated-supply-close.v1",
                fixture.Carrier.Id);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var travel = new TravelSystem();
            for (var segment = 0;
                 segment < 8 && !freight.CellRouteWaiting;
                 segment++)
                travel.AdvanceJourneysOneSegment(fixture.World);
            Assert.That(freight.CellRouteWaiting, Is.True);
            return new IntegratedGateFixture
            {
                Fixture = fixture,
                Runtime = runtime,
                PassageSystem = passageSystem,
                GateId = gateId,
                Freight = freight
            };
        }

        private static void ReopenAndFinish(IntegratedGateFixture gate)
        {
            gate.PassageSystem.EnqueueTransition(
                gate.Fixture.World,
                gate.Runtime,
                gate.GateId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.integrated-supply-reopen.v1",
                gate.Fixture.Carrier.Id);
            gate.Runtime.ProcessDue(gate.Fixture.World);
            gate.Runtime.DispatchPublishedEvents(gate.Fixture.World);
            var travel = new TravelSystem();
            for (var segment = 0;
                 segment < 48 && gate.Freight.Status !=
                    CivilianFreightStatus.Completed;
                 segment++)
            {
                travel.AdvanceJourneysOneSegment(gate.Fixture.World);
                gate.Fixture.FreightSystem.ResolveArrivals(
                    gate.Fixture.World);
            }
        }

        private sealed class IntegratedGateFixture
        {
            public CivilianFreightFixture Fixture;
            public WorldCommandRuntime Runtime;
            public LuoyangPassageWorldCommandSystem PassageSystem;
            public string GateId;
            public CivilianFreightState Freight;
        }
    }
}
