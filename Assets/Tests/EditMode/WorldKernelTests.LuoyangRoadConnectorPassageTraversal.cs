using System;
using System.Collections.Generic;
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
        public void FreightCellRoute_GateWaitSaveLoadAndRecoveryIsConservative()
        {
            var fixture = PrepareCivilianFreightWorld(25_901, 12);
            var passagePlan = BuildLuoyangPassagePlan();
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                passagePlan);
            passageSystem.RegisterHandlers(runtime);
            Assert.That(passageSystem.EnsureInitialized(
                fixture.World, runtime), Is.True);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var gateId = fixture.World.LuoyangPassageTraversals.First(item =>
                !string.Equals(item.FacilityDefinitionId,
                    "facility.public.bridge", StringComparison.Ordinal))
                .FacilityId;
            var cellPlan = BuildFreightGateCellPlan(gateId,
                out var originCellId64, out var targetCellId64);
            fixture.FreightSystem = new CivilianFreightSystem(
                fixture.World.MasterSeed, fixture.Content, cellPlan);
            fixture.Request.OriginCellId64 = originCellId64;
            fixture.Request.TargetCellId64 = targetCellId64;
            fixture.Request.MovementCapabilityId =
                MovementCapabilityIds.PackAnimal;
            var openingAudit = new FormalFoodConservationAuditor().Audit(
                fixture.World, fixture.Content);
            Assert.That(openingAudit.Balanced, Is.True);

            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            Assert.That(freight.UsesCellRoute, Is.True);
            Assert.That(freight.CellRouteSegments.Any(item =>
                item.TraversalConditionId ==
                    CellTraversalIds.FormalPassageConditionId &&
                item.FormalWorldObjectId == gateId), Is.True);
            Assert.That(passageSystem.EnqueueTransition(
                fixture.World,
                runtime,
                gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.freight-cell-route-close.v1",
                "person.freight-cell-route-controller"), Is.True);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);

            var travel = new TravelSystem();
            for (var segment = 0;
                 segment < 8 && !freight.CellRouteWaiting;
                 segment++)
                travel.AdvanceJourneysOneSegment(fixture.World);
            Assert.That(freight.CellRouteWaiting, Is.True);
            Assert.That(freight.CellRouteWaitingOnFormalWorldObjectId,
                Is.EqualTo(gateId));
            Assert.That(freight.DeliveredQuantity, Is.Zero);
            var catchment = new LuoyangSupplyCatchmentSelection
            {
                CellIds = cellPlan.Profiles.Select(item => item.CellId64)
                    .ToList(),
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
                },
                FacilityIds = new List<string>
                {
                    fixture.SellerStorage.Id,
                    fixture.BuyerStorage.Id
                }
            };
            var projectionSystem = new LuoyangSupplyProjectionSystem(
                fixture.Content);
            var catchmentAudit = projectionSystem.AuditCatchment(
                fixture.World, catchment, cellPlan);
            Assert.That(catchmentAudit.Passed, Is.True);
            Assert.That(catchmentAudit.TraversalCoveredCellCount,
                Is.EqualTo(catchmentAudit.CellCount));
            Assert.That(catchmentAudit.PermanentPersonCount,
                Is.GreaterThan(0));
            Assert.That(catchmentAudit.HouseholdCount,
                Is.GreaterThan(0));
            Assert.That(catchmentAudit.StorageFacilityCount,
                Is.EqualTo(3));
            var blockedProjection = projectionSystem.BuildCityProjection(
                fixture.World, catchment);
            Assert.That(blockedProjection.IncomingFreightQuantity,
                Is.EqualTo(freight.RemainingCargoQuantity));
            Assert.That(blockedProjection.BlockedFreightCount, Is.EqualTo(1));
            Assert.That(blockedProjection.DelayedFreightCount, Is.EqualTo(1));
            Assert.That(blockedProjection.DailyFoodDemandNutritionBasisUnits,
                Is.GreaterThan(0));
            var waitingJson = WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content);
            var loaded = WorldSnapshotSerializer.Deserialize(
                waitingJson, fixture.Content);
            var loadedFreight = loaded.CivilianFreights.Single(item =>
                item.Id == freight.Id);
            Assert.That(loadedFreight.CellRouteWaiting, Is.True);
            Assert.That(WorldSnapshotSerializer.Serialize(
                loaded, fixture.Content), Is.EqualTo(waitingJson));

            var resumedRuntime = new WorldCommandRuntime();
            var resumedPassageSystem = new LuoyangPassageWorldCommandSystem(
                passagePlan);
            resumedPassageSystem.RegisterHandlers(resumedRuntime);
            Assert.That(resumedPassageSystem.EnqueueTransition(
                loaded,
                resumedRuntime,
                gateId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.freight-cell-route-reopen.v1",
                "person.freight-cell-route-controller"), Is.True);
            resumedRuntime.ProcessDue(loaded);
            resumedRuntime.DispatchPublishedEvents(loaded);
            var resumedFreightSystem = new CivilianFreightSystem(
                loaded.MasterSeed, fixture.Content, cellPlan);
            var resumedTravel = new TravelSystem();
            for (var segment = 0;
                 segment < 32 && loadedFreight.Status !=
                    CivilianFreightStatus.Completed;
                 segment++)
            {
                resumedTravel.AdvanceJourneysOneSegment(loaded);
                resumedFreightSystem.ResolveArrivals(loaded);
            }
            Assert.That(loadedFreight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(loadedFreight.DeliveredQuantity,
                Is.EqualTo(loadedFreight.DispatchedQuantity));
            Assert.That(loadedFreight.NaturalLossQuantity, Is.Zero);
            Assert.That(loadedFreight.CellRouteCurrentCellId64,
                Is.EqualTo(targetCellId64));
            var closingAudit = new FormalFoodConservationAuditor().Audit(
                loaded, fixture.Content);
            Assert.That(closingAudit.Balanced, Is.True);
            Assert.That(closingAudit.Difference, Is.Zero);
            var recoveredProjection = new LuoyangSupplyProjectionSystem(
                fixture.Content).BuildCityProjection(loaded, catchment);
            Assert.That(recoveredProjection.IncomingFreightQuantity, Is.Zero);
            Assert.That(recoveredProjection.BlockedFreightCount, Is.Zero);
            Assert.That(recoveredProjection.CurrentUsableFoodStock,
                Is.GreaterThanOrEqualTo(loadedFreight.DeliveredQuantity));
            Assert.That(recoveredProjection.DaysOfSupply,
                Is.GreaterThan(0d));
            loaded.Validate();
        }

        [Test]
        public void FoodSupplyVerticalSlice_HarvestMarketFreightGateAndConsumptionBalance()
        {
            var fixture = PrepareCivilianFreightWorld(25_902, 100);
            fixture.Seller.FarmlandUnits = 1;
            fixture.Seller.SeedGrain = 100;
            var field = new VillageFacilityState
            {
                Id = "facility.freight_origin_farmland",
                VillageId = "village.freight_origin",
                Kind = VillageFacilityKind.Farmland,
                OwnerFamilyId = fixture.Seller.Id,
                ManagerPersonId = fixture.Seller.HeadPersonId,
                Capacity = 1
            };
            fixture.World.VillageFacilities.Add(field);
            var agriculture = new AgricultureProductionSystem(
                fixture.World.MasterSeed, fixture.Content);
            var order = agriculture.CreateOrder(
                fixture.World,
                "village.freight_origin",
                fixture.Seller.Id,
                field.Id,
                fixture.SellerStorage.Id,
                fixture.Seller.HeadPersonId,
                CoreProductionContent.WheatCropId,
                CoreProductionContent.PrototypeNorthernWheatVarietyId,
                CoreProductionContent.GrowWheatRecipeId,
                CoreProductionContent.PrototypeDrylandMethodId,
                ProductionControlMode.TargetInstruction,
                1,
                new[] { fixture.Seller.HeadPersonId },
                fixture.World.AbsoluteDay + 180);
            fixture.World.AbsoluteDay = order.HarvestDay;
            agriculture.ResolveDueOrders(
                fixture.World, "village.freight_origin");
            var harvestBatch = fixture.World.ProductBatches.Single(item =>
                item.SourceWorkOrderId == order.Id);
            Assert.That(harvestBatch.Quantity, Is.GreaterThan(0));
            Assert.That(harvestBatch.StorageFacilityId,
                Is.EqualTo(fixture.SellerStorage.Id));

            var market = new FormalCountyMarketSystem(fixture.Content);
            var sell = market.CreateSellOrder(
                fixture.World,
                "county_governance.freight_origin",
                fixture.Seller.Id,
                fixture.SellerStorage.Id,
                harvestBatch.ProductDefinitionId,
                checked((int)harvestBatch.Quantity),
                2,
                checked((int)fixture.World.AbsoluteDay),
                checked((int)fixture.World.AbsoluteDay + 10));
            var buy = market.CreateBuyOrder(
                fixture.World,
                "county_governance.freight_destination",
                fixture.Buyer.Id,
                fixture.BuyerStorage.Id,
                harvestBatch.ProductDefinitionId,
                checked((int)harvestBatch.Quantity),
                3,
                checked((int)fixture.World.AbsoluteDay),
                checked((int)fixture.World.AbsoluteDay + 10));
            var passagePlan = BuildLuoyangPassagePlan();
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                passagePlan);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var gateId = fixture.World.LuoyangPassageTraversals.First(item =>
                !string.Equals(item.FacilityDefinitionId,
                    "facility.public.bridge", StringComparison.Ordinal))
                .FacilityId;
            var cellPlan = BuildFreightGateCellPlan(
                gateId, out var originCellId64, out var targetCellId64);
            fixture.FreightSystem = new CivilianFreightSystem(
                fixture.World.MasterSeed, fixture.Content, cellPlan);
            fixture.Request.BuyOrderId = buy.Id;
            fixture.Request.SellOrderId = sell.Id;
            fixture.Request.Quantity = harvestBatch.Quantity;
            fixture.Request.OriginCellId64 = originCellId64;
            fixture.Request.TargetCellId64 = targetCellId64;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            Assert.That(fixture.World.ProductBatches.Any(item =>
                item.InventoryContainerId == fixture.Transport.Id &&
                item.SourceWorkOrderId == order.Id), Is.True);
            var travel = new TravelSystem();
            for (var segment = 0;
                 segment < 32 && freight.Status !=
                    CivilianFreightStatus.Completed;
                 segment++)
            {
                travel.AdvanceJourneysOneSegment(fixture.World);
                fixture.FreightSystem.ResolveArrivals(fixture.World);
            }
            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(freight.CellRouteSegments.Any(item =>
                item.FormalWorldObjectId == gateId), Is.True);
            var deliveredBatch = fixture.World.ProductBatches.Single(item =>
                item.OwnerFamilyId == fixture.Buyer.Id &&
                item.StorageFacilityId == fixture.BuyerStorage.Id &&
                item.SourceWorkOrderId == order.Id);
            var consumption = new FoodInventorySystem(fixture.Content)
                .ConsumeFamilyFood(
                    fixture.World,
                    fixture.Buyer.Id,
                    fixture.BuyerStorage.Id,
                    fixture.Buyer.HeadPersonId,
                    Math.Min(10_000L,
                        deliveredBatch.Quantity * 10_000L));
            Assert.That(consumption.ConsumedPhysicalQuantity,
                Is.GreaterThan(0));
            Assert.That(fixture.World.InventoryTransactions.Single(item =>
                    item.Id == consumption.InventoryTransactionId).Type,
                Is.EqualTo(InventoryTransactionType.FoodConsumed));
            var conservation = new FormalFoodConservationAuditor().Audit(
                fixture.World, fixture.Content);
            Assert.That(conservation.Balanced, Is.True);
            Assert.That(conservation.Difference, Is.Zero);
            fixture.World.Validate();
        }

        [Test]
        public void WoodFreightCellRoute_UsesSameCarrierGateAndInventoryAuthority()
        {
            var fixture = PrepareCivilianFreightWorld(
                25_903,
                12,
                CoreProductionContent.TimberMaterialProductId);
            var passagePlan = BuildLuoyangPassagePlan();
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                passagePlan);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var gateId = fixture.World.LuoyangPassageTraversals.First(item =>
                !string.Equals(item.FacilityDefinitionId,
                    "facility.public.bridge", StringComparison.Ordinal))
                .FacilityId;
            var cellPlan = BuildFreightGateCellPlan(gateId,
                out var originCellId64, out var targetCellId64);
            fixture.FreightSystem = new CivilianFreightSystem(
                fixture.World.MasterSeed, fixture.Content, cellPlan);
            fixture.Request.OriginCellId64 = originCellId64;
            fixture.Request.TargetCellId64 = targetCellId64;
            fixture.Request.MovementCapabilityId =
                MovementCapabilityIds.PackAnimal;
            var loggingStorage = new VillageFacilityState
            {
                Id = "facility.freight_origin_logging_camp",
                VillageId = "village.freight_origin",
                Kind = VillageFacilityKind.HouseholdGranary,
                OwnerFamilyId = fixture.Seller.Id,
                ManagerPersonId = fixture.Seller.HeadPersonId,
                Capacity = 20_000,
                CapabilityTags = new List<string>
                {
                    CoreProductionContent.LoggingFacilityTag
                }
            };
            fixture.World.VillageFacilities.Add(loggingStorage);
            var forest = new ResourceBodyState
            {
                Id = "resource_body.freight_origin.forest.v1",
                ResourceKindId = "resource_kind.temperate_forest_stand",
                OutputProductDefinitionId =
                    CoreProductionContent.TimberMaterialProductId,
                LocationId = "location.freight_origin_village",
                Provenance = "gameplay_reconstruction",
                GenerationRuleVersion = "resource_rules.outer_supply.v1",
                RequiredFacilityTag =
                    CoreProductionContent.LoggingFacilityTag,
                InitialQuantity = 100,
                RemainingQuantity = 100,
                QualityBasisPoints = 8_000,
                ExtractionDifficultyBasisPoints = 8_000
            };
            fixture.World.ResourceBodies.Add(forest);
            var openingWood = fixture.World.ProductBatches.Where(item =>
                item.ProductDefinitionId ==
                    CoreProductionContent.TimberMaterialProductId).Sum(item =>
                item.Quantity) + forest.RemainingQuantity;
            var extraction = new UpstreamResourceProductionSystem(
                fixture.Content);
            var extractionOrder = extraction.CreateFamilyOrder(
                fixture.World,
                forest.Id,
                fixture.Seller.Id,
                loggingStorage.Id,
                fixture.Seller.HeadPersonId,
                new[] { fixture.Seller.HeadPersonId },
                ProductionControlMode.WorkOrder,
                12);
            fixture.World.AbsoluteDay = extractionOrder.FinishDay;
            extraction.ResolveDueOrders(fixture.World);
            var extractedBatch = fixture.World.ProductBatches.Single(item =>
                item.SourceWorkOrderId == extractionOrder.Id);
            var market = new FormalCountyMarketSystem(fixture.Content);
            var sell = market.CreateSellOrder(
                fixture.World,
                "county_governance.freight_origin",
                fixture.Seller.Id,
                loggingStorage.Id,
                CoreProductionContent.TimberMaterialProductId,
                checked((int)extractedBatch.Quantity),
                2,
                checked((int)fixture.World.AbsoluteDay),
                checked((int)fixture.World.AbsoluteDay + 10));
            var buy = market.CreateBuyOrder(
                fixture.World,
                "county_governance.freight_destination",
                fixture.Buyer.Id,
                fixture.BuyerStorage.Id,
                CoreProductionContent.TimberMaterialProductId,
                checked((int)extractedBatch.Quantity),
                3,
                checked((int)fixture.World.AbsoluteDay),
                checked((int)fixture.World.AbsoluteDay + 10));
            fixture.Request.SellOrderId = sell.Id;
            fixture.Request.BuyOrderId = buy.Id;
            fixture.Request.Quantity = extractedBatch.Quantity;

            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            Assert.That(fixture.World.ProductBatches.Any(item =>
                item.InventoryContainerId == fixture.Transport.Id &&
                item.SourceWorkOrderId == extractionOrder.Id), Is.True);
            var travel = new TravelSystem();
            for (var segment = 0;
                 segment < 32 && freight.Status !=
                    CivilianFreightStatus.Completed;
                 segment++)
            {
                travel.AdvanceJourneysOneSegment(fixture.World);
                fixture.FreightSystem.ResolveArrivals(fixture.World);
            }
            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(freight.ProductDefinitionId,
                Is.EqualTo(CoreProductionContent.TimberMaterialProductId));
            Assert.That(freight.DeliveredQuantity, Is.EqualTo(12));
            Assert.That(freight.NaturalLossQuantity, Is.Zero);
            var closingWood = fixture.World.ProductBatches.Where(item =>
                item.ProductDefinitionId ==
                    CoreProductionContent.TimberMaterialProductId).Sum(item =>
                item.Quantity) + forest.RemainingQuantity;
            Assert.That(closingWood, Is.EqualTo(openingWood));
            Assert.That(fixture.World.ProductBatches.Where(item =>
                item.OwnerFamilyId == fixture.Buyer.Id &&
                item.StorageFacilityId == fixture.BuyerStorage.Id &&
                item.ProductDefinitionId ==
                    CoreProductionContent.TimberMaterialProductId).Sum(item =>
                item.Quantity), Is.EqualTo(12));
            Assert.That(fixture.World.ProductBatches.Any(item =>
                item.OwnerFamilyId == fixture.Buyer.Id &&
                item.SourceWorkOrderId == extractionOrder.Id), Is.True);
            Assert.That(freight.CellRouteCurrentCellId64,
                Is.EqualTo(targetCellId64));
            fixture.World.Validate();
        }

        [Test]
        public void FreightCellRoute_BridgeClosureWaitsAndReopenResumes()
        {
            var fixture = PrepareCivilianFreightWorld(25_904, 12);
            var passagePlan = BuildLuoyangPassagePlan();
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                passagePlan);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var bridgeId = fixture.World.LuoyangPassageTraversals.First(item =>
                string.Equals(item.FacilityDefinitionId,
                    "facility.public.bridge", StringComparison.Ordinal))
                .FacilityId;
            var cellPlan = BuildFreightPassageCellPlan(
                bridgeId,
                FacilitySpatialCapabilityIds.Bridge,
                out var originCellId64,
                out var targetCellId64);
            fixture.FreightSystem = new CivilianFreightSystem(
                fixture.World.MasterSeed, fixture.Content, cellPlan);
            fixture.Request.OriginCellId64 = originCellId64;
            fixture.Request.TargetCellId64 = targetCellId64;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);

            Assert.That(passageSystem.EnqueueTransition(
                fixture.World,
                runtime,
                bridgeId,
                LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                "passage.reason.freight-bridge-destroyed.v1",
                "person.freight-cell-route-controller"), Is.True);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var travel = new TravelSystem();
            for (var segment = 0;
                 segment < 8 && !freight.CellRouteWaiting;
                 segment++)
                travel.AdvanceJourneysOneSegment(fixture.World);
            Assert.That(freight.CellRouteWaiting, Is.True);
            Assert.That(freight.CellRouteWaitingOnFormalWorldObjectId,
                Is.EqualTo(bridgeId));
            Assert.That(freight.DeliveredQuantity, Is.Zero);

            Assert.That(passageSystem.EnqueueTransition(
                fixture.World,
                runtime,
                bridgeId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.freight-bridge-repaired.v1",
                "person.freight-cell-route-controller"), Is.True);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            for (var segment = 0;
                 segment < 32 && freight.Status !=
                    CivilianFreightStatus.Completed;
                 segment++)
            {
                travel.AdvanceJourneysOneSegment(fixture.World);
                fixture.FreightSystem.ResolveArrivals(fixture.World);
            }
            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(freight.CellRouteCurrentCellId64,
                Is.EqualTo(targetCellId64));
            Assert.That(new FormalFoodConservationAuditor().Audit(
                fixture.World, fixture.Content).Difference, Is.Zero);
            fixture.World.Validate();
        }

        [Test]
        public void FoodSupplyInterruptionTests_GateBlockCreatesShortfallAndRecovery()
        {
            var fixture = PrepareCivilianFreightWorld(25_910, 12);
            new ProductInventorySystem(fixture.Content)
                .CreateFamilyOpeningBatch(
                    fixture.World,
                    fixture.Buyer.Id,
                    fixture.BuyerStorage.Id,
                    fixture.Buyer.HeadPersonId,
                    CoreProductionContent.WheatGrainProductId,
                    2);
            var passagePlan = BuildLuoyangPassagePlan();
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                passagePlan);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var gateId = fixture.World.LuoyangPassageTraversals.First(item =>
                !string.Equals(item.FacilityDefinitionId,
                    "facility.public.bridge", StringComparison.Ordinal))
                .FacilityId;
            var cellPlan = BuildFreightGateCellPlan(
                gateId, out var originCellId64, out var targetCellId64);
            fixture.FreightSystem = new CivilianFreightSystem(
                fixture.World.MasterSeed, fixture.Content, cellPlan);
            fixture.Request.OriginCellId64 = originCellId64;
            fixture.Request.TargetCellId64 = targetCellId64;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            passageSystem.EnqueueTransition(fixture.World, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.supply-interruption-close.v1",
                "person.freight-cell-route-controller");
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var travel = new TravelSystem();
            for (var segment = 0;
                 segment < 8 && !freight.CellRouteWaiting;
                 segment++)
                travel.AdvanceJourneysOneSegment(fixture.World);
            Assert.That(freight.CellRouteWaiting, Is.True);

            fixture.World.AbsoluteDay = 30;
            var life = new VillageLifeSystem(
                fixture.World.MasterSeed, fixture.Content);
            var shortfall = life.ResolveFormalFoodMonthly(
                fixture.World,
                "village.freight_destination",
                fixture.World.AbsoluteDay);
            Assert.That(shortfall.HasShortfall, Is.True);
            Assert.That(shortfall.ShortfallFamilyIds,
                Does.Contain(fixture.Buyer.Id));
            var selection = new LuoyangSupplyCatchmentSelection
            {
                CityLocationIds = new List<string>
                {
                    "location.freight_destination_village"
                }
            };
            var blocked = new LuoyangSupplyProjectionSystem(fixture.Content)
                .BuildCityProjection(fixture.World, selection);
            Assert.That(blocked.CurrentUsableFoodStock, Is.Zero);
            Assert.That(blocked.BlockedFreightCount, Is.EqualTo(1));
            Assert.That(blocked.HouseholdShortfallCount, Is.EqualTo(1));

            passageSystem.EnqueueTransition(fixture.World, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.supply-interruption-reopen.v1",
                "person.freight-cell-route-controller");
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            for (var segment = 0;
                 segment < 32 && freight.Status !=
                    CivilianFreightStatus.Completed;
                 segment++)
            {
                travel.AdvanceJourneysOneSegment(fixture.World);
                fixture.FreightSystem.ResolveArrivals(fixture.World);
            }
            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            fixture.World.AbsoluteDay = 60;
            var recovered = life.ResolveFormalFoodMonthly(
                fixture.World,
                "village.freight_destination",
                fixture.World.AbsoluteDay);
            Assert.That(recovered.HasShortfall, Is.False);
            var recoveredProjection =
                new LuoyangSupplyProjectionSystem(fixture.Content)
                    .BuildCityProjection(fixture.World, selection);
            Assert.That(recoveredProjection.BlockedFreightCount, Is.Zero);
            Assert.That(recoveredProjection.CurrentUsableFoodStock,
                Is.GreaterThan(0));
            Assert.That(recoveredProjection.HouseholdShortfallCount,
                Is.Zero);
            Assert.That(new FormalFoodConservationAuditor().Audit(
                fixture.World, fixture.Content).Difference, Is.Zero);
            fixture.World.Validate();
        }

        [Test]
        public void FreightCellRoute_RoadBlockReroutesPackAnimalOffRoadButNotCart()
        {
            var pack = CreateRoadRerouteFreightFixture(
                25_905, MovementCapabilityIds.PackAnimal);
            var packFreight = pack.Fixture.FreightSystem.Dispatch(
                pack.Fixture.World, pack.Fixture.Request);
            Assert.That(packFreight.CellRouteSegments.Any(item =>
                item.FormalWorldObjectId == pack.RoadEdgeId), Is.True);
            pack.Fixture.World.LuoyangRoadOperationalSegments.Single().StatusId =
                LuoyangFormalPlayerMovementIds.DestroyedRoadStatusId;
            var travel = new TravelSystem();
            travel.AdvanceJourneysOneSegment(pack.Fixture.World);
            Assert.That(packFreight.CellRouteWaiting, Is.True);
            Assert.That(pack.Fixture.FreightSystem.TryRerouteCellFreight(
                pack.Fixture.World, packFreight), Is.True);
            Assert.That(packFreight.CellRouteRevision, Is.EqualTo(1));
            Assert.That(packFreight.CellRouteSegments.Any(item =>
                item.FormalWorldObjectId == pack.RoadEdgeId), Is.False);
            Assert.That(packFreight.CellRouteSegments.Any(item =>
                item.TraversalCostPermille > 1_000), Is.True);
            for (var segment = 0;
                 segment < 48 && packFreight.Status !=
                    CivilianFreightStatus.Completed;
                 segment++)
            {
                travel.AdvanceJourneysOneSegment(pack.Fixture.World);
                pack.Fixture.FreightSystem.ResolveArrivals(
                    pack.Fixture.World);
            }
            Assert.That(packFreight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            pack.Fixture.World.Validate();

            var cart = CreateRoadRerouteFreightFixture(
                25_906, MovementCapabilityIds.Cart);
            var cartFreight = cart.Fixture.FreightSystem.Dispatch(
                cart.Fixture.World, cart.Fixture.Request);
            cart.Fixture.World.LuoyangRoadOperationalSegments.Single().StatusId =
                LuoyangFormalPlayerMovementIds.DestroyedRoadStatusId;
            travel.AdvanceJourneysOneSegment(cart.Fixture.World);
            Assert.That(cartFreight.CellRouteWaiting, Is.True);
            Assert.That(cart.Fixture.FreightSystem.TryRerouteCellFreight(
                cart.Fixture.World, cartFreight), Is.False);
            Assert.That(cartFreight.CellRouteRevision, Is.Zero);
            Assert.That(cartFreight.DeliveredQuantity, Is.Zero);
            cart.Fixture.World.Validate();
        }

        [Test]
        public void SupplyReplayTests_ThreeGateInterruptionRunsAreByteIdentical()
        {
            string expected = null;
            for (var run = 0; run < 3; run++)
            {
                var actual = RunDeterministicFreightGateReplay();
                if (expected == null) expected = actual;
                else Assert.That(actual, Is.EqualTo(expected));
            }
        }

        [Test]
        public void OuterSupplyCatchmentDataAudit_ReferencesOneWorldAndReportsTargetGap()
        {
            var worldMapRoot = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var reader = new LuoyangOuterSupplyCatchmentV1Reader(
                Path.Combine(worldMapRoot,
                    "LuoyangOuterSupplyCatchmentV1"));
            var audit = reader.Audit();
            var local = new LuoyangHumanScaleLocalMapPlanSource(worldMapRoot);
            var traversal = LuoyangCellTraversalRules.CreatePlan(
                local.Plan, local.StrategicRoads);
            timer.Stop();
            Assert.That(audit.CriticalReferencesPassed, Is.True,
                string.Join(",", audit.CriticalReferenceErrors));
            Assert.That(reader.Manifest.IsProjectionOnly, Is.True);
            Assert.That(reader.Manifest.AdministrativeEffect,
                Is.EqualTo("none"));
            Assert.That(audit.CellCount, Is.EqualTo(869));
            Assert.That(audit.FacilityCount, Is.EqualTo(854));
            Assert.That(audit.SettlementCount, Is.EqualTo(33));
            Assert.That(audit.AgricultureUnitCount, Is.EqualTo(135));
            Assert.That(audit.StorageFacilityCount, Is.EqualTo(22));
            Assert.That(audit.RoadFacilityCount, Is.EqualTo(267));
            Assert.That(audit.MaterializedOuterPopulation,
                Is.EqualTo(130_000));
            Assert.That(audit.MaterializedOuterHouseholds,
                Is.EqualTo(26_907));
            Assert.That(audit.MaterializedWorldPopulation,
                Is.EqualTo(400_000));
            Assert.That(audit.InclusivePopulationTarget,
                Is.EqualTo(700_000));
            Assert.That(audit.UnmaterializedPopulationGap,
                Is.EqualTo(300_000));
            Assert.That(audit.PopulationTargetMaterialized, Is.False);
            Assert.That(reader.Definition.CellIds.All(cellId =>
                traversal.ProfilesByCellId.ContainsKey(cellId)), Is.True);
            Assert.That(reader.Definition.FoodProductDefinitionIds,
                Does.Contain("product.food.wheat_grain"));
            Assert.That(reader.Definition.WoodProductDefinitionIds,
                Does.Contain(CoreProductionContent.TimberMaterialProductId));
            Assert.That(reader.Definition.ContentIdCrosswalks.Single(item =>
                    item.SourceId == "product.food.wheat_grain").FormalId,
                Is.EqualTo(CoreProductionContent.WheatGrainProductId));
            Assert.That(audit.FormalContentBridgeComplete, Is.False);
            Assert.That(audit.UnresolvedContentDefinitionIds,
                Is.EquivalentTo(new[]
                {
                    "product.food.bean",
                    "product.food.broomcorn_grain",
                    "product.food.millet_grain"
                }));
            Assert.That(timer.ElapsedMilliseconds, Is.LessThan(10_000));
            Console.WriteLine(
                "OUTER_SUPPLY_PERF init_and_traversal_ms=" +
                timer.ElapsedMilliseconds + " cells=" + audit.CellCount +
                " facilities=" + audit.FacilityCount +
                " population_gap=" + audit.UnmaterializedPopulationGap);
        }

        [Test]
        public void FreightOriginInsufficientAndCarrierUnavailable_DoNotMutateWorld()
        {
            var insufficient = PrepareCivilianFreightWorld(25_911, 12);
            insufficient.Request.Quantity = 13;
            var insufficientBefore = WorldSnapshotSerializer.Serialize(
                insufficient.World, insufficient.Content);

            Assert.Throws<InvalidOperationException>(() =>
                insufficient.FreightSystem.Dispatch(
                    insufficient.World, insufficient.Request));
            Assert.That(WorldSnapshotSerializer.Serialize(
                insufficient.World, insufficient.Content),
                Is.EqualTo(insufficientBefore));

            var unavailable = PrepareCivilianFreightWorld(25_912, 12);
            unavailable.Carrier.LocationId =
                "location.freight_destination_village";
            var unavailableBefore = WorldSnapshotSerializer.Serialize(
                unavailable.World, unavailable.Content);

            Assert.Throws<InvalidOperationException>(() =>
                unavailable.FreightSystem.Dispatch(
                    unavailable.World, unavailable.Request));
            Assert.That(WorldSnapshotSerializer.Serialize(
                unavailable.World, unavailable.Content),
                Is.EqualTo(unavailableBefore));
        }

        [Test]
        public void FreightDestinationFull_SaveLoadThenCapacityRecoveryCompletesOnce()
        {
            var fixture = PrepareCivilianFreightWorld(25_913, 12);
            fixture.BuyerStorage.Capacity = 0;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);

            new WorldSimulator(fixture.World.MasterSeed, fixture.Content)
                .AdvanceSegments(fixture.World, 5);

            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.AwaitingReceipt));
            Assert.That(freight.DeliveredQuantity, Is.Zero);
            var waitingJson = WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content);
            var loaded = WorldSnapshotSerializer.Deserialize(
                waitingJson, fixture.Content);
            Assert.That(WorldSnapshotSerializer.Serialize(
                loaded, fixture.Content), Is.EqualTo(waitingJson));
            var loadedFreight = loaded.CivilianFreights.Single(item =>
                item.Id == freight.Id);
            loaded.VillageFacilities.Single(item =>
                item.Id == fixture.BuyerStorage.Id).Capacity = 20_000;

            var resumed = new CivilianFreightSystem(
                loaded.MasterSeed, fixture.Content);
            resumed.ResolveArrivals(loaded);
            resumed.ResolveArrivals(loaded);

            Assert.That(loadedFreight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(loadedFreight.RemainingCargoQuantity, Is.Zero);
            Assert.That(loadedFreight.DispatchedQuantity, Is.EqualTo(
                loadedFreight.DeliveredQuantity +
                loadedFreight.NaturalLossQuantity));
            Assert.That(new FormalFoodConservationAuditor().Audit(
                loaded, fixture.Content).Difference, Is.Zero);
            loaded.Validate();
        }

        [Test]
        public void LuoyangPassageWorldState_CommandRoundTripPreservesStateAndAudit()
        {
            var plan = BuildLuoyangPassagePlan();
            var world = WorldState.Create(184);
            var runtime = new WorldCommandRuntime();
            var system = new LuoyangPassageWorldCommandSystem(plan);
            system.RegisterHandlers(runtime);

            Assert.That(system.EnsureInitialized(world, runtime), Is.True);
            var initialization = runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(initialization.ProcessedCommands, Is.EqualTo(1));
            Assert.That(initialization.CommittedTransactions, Is.EqualTo(1));
            Assert.That(initialization.PublishedEvents, Is.EqualTo(1));
            Assert.That(world.LuoyangPassageTraversals, Has.Count.EqualTo(20));
            Assert.That(world.PersistentWorldCommands.Single(item =>
                    item.Id == LuoyangPassageTraversalWorldContractIds
                        .InitializationCommandId).Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));

            var gateId = plan.PassageFacilityIds.First();
            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.core-persisted-close.v1",
                "person.core-test-issuer"), Is.True);
            var transition = runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(transition.ProcessedCommands, Is.EqualTo(1));
            var current = world.LuoyangPassageTraversals.Single(item =>
                item.FacilityId == gateId);
            Assert.That(current.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId));
            Assert.That(current.Revision, Is.EqualTo(1));
            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.core-repeat.v1",
                "person.core-test-issuer"), Is.False);

            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);
            Assert.That(WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(json));
            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.WorldEventOutbox, Has.Count.EqualTo(2));
            Assert.That(loaded.WorldEventOutbox.All(item =>
                item.DispatchStatus == WorldEventDispatchStatus.Dispatched),
                Is.True);
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateSessionFromWorldState(plan, loaded);
            Assert.That(session.PersistsAcrossSave, Is.True);
            Assert.That(session.ChangesSaveSchema, Is.True);
            Assert.That(session.IsWorldStateProjection, Is.True);
            Assert.That(session.Get(gateId).CanTraverse, Is.False);
            Assert.Throws<System.InvalidOperationException>(() =>
                session.SetStatus(gateId,
                    LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                    1, "passage.reason.invalid-direct-write.v1"));
        }

        [Test]
        public void LuoyangPassageWorldState_V73MigrationIsEmptyAndInvalidVersionsReject()
        {
            var legacy = WorldState.Create(184);
            legacy.SchemaVersion = 73;
            legacy.LuoyangPassageTraversals = null;
            var migrated = WorldSnapshotMigrator.MigrateToCurrent(legacy);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.LuoyangPassageTraversals, Is.Empty);
            Assert.That(migrated.PersistentWorldCommands, Is.Empty);
            Assert.That(migrated.WorldEventOutbox, Is.Empty);
            migrated.Validate();

            var zero = WorldState.Create(184);
            zero.SchemaVersion = 0;
            Assert.Throws<System.InvalidOperationException>(() =>
                WorldSnapshotMigrator.MigrateToCurrent(zero));
            var future = WorldState.Create(184);
            future.SchemaVersion = WorldState.CurrentSchemaVersion + 1;
            Assert.Throws<System.InvalidOperationException>(() =>
                WorldSnapshotMigrator.MigrateToCurrent(future));
        }

        [Test]
        public void LuoyangPassageWorldState_RejectsTamperAndConflictingBatch()
        {
            var plan = BuildLuoyangPassagePlan();
            var world = WorldState.Create(184);
            var runtime = new WorldCommandRuntime();
            var system = new LuoyangPassageWorldCommandSystem(plan);
            system.RegisterHandlers(runtime);
            system.EnsureInitialized(world, runtime);
            runtime.ProcessDue(world);
            var gateId = plan.PassageFacilityIds.First();

            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.core-conflict-close.v1",
                "person.core-test-issuer"), Is.True);
            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                "passage.reason.core-conflict-damage.v1",
                "person.core-test-issuer"), Is.True);
            Assert.Throws<System.InvalidOperationException>(() =>
                runtime.ProcessDue(world));
            var unchanged = world.LuoyangPassageTraversals.Single(item =>
                item.FacilityId == gateId);
            Assert.That(unchanged.Revision, Is.Zero);
            Assert.That(unchanged.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId));
            Assert.That(world.WorldCommandBatchResults.Last().Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            world.Validate();

            unchanged.LastReasonId = "passage.reason.tampered.v1";
            Assert.Throws<System.InvalidOperationException>(() =>
                world.Validate());
        }

        [Test]
        public void LuoyangPassageOperations_GuardDamageRepairAndReopenAreAuditable()
        {
            var fixture = CreateLuoyangPassageOperationsFixture();
            var world = fixture.World;
            var runtime = fixture.Runtime;
            var system = fixture.System;

            Assert.That(system.EnqueueGuardAssignment(world, runtime,
                fixture.FacilityId, fixture.GuardArmyId,
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var control = world.LuoyangPassageOperationalControls.Single();
            Assert.That(control.GuardPersonIds,
                Is.EqualTo(new[] { fixture.GuardCommanderPersonId }));
            Assert.That(control.CurrentConditionBasisPoints, Is.EqualTo(10_000));
            Assert.Throws<System.InvalidOperationException>(() =>
                system.EnqueueTransition(world, runtime, fixture.FacilityId,
                    LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                    "passage.reason.presentation-direct-close.v1",
                    "person.presentation.map"));

            Assert.That(system.EnqueueTransition(world, runtime,
                fixture.FacilityId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.guard-close.v1",
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(system.EnqueueTransition(world, runtime,
                fixture.FacilityId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.guard-open.v1",
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);

            Assert.That(system.EnqueueBattleDamage(world, runtime,
                fixture.FacilityId, fixture.BattleId, 4_000,
                "passage.reason.test-battle-damage.v1",
                fixture.AttackerCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var passage = world.LuoyangPassageTraversals.Single(item =>
                item.FacilityId == fixture.FacilityId);
            Assert.That(passage.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId));
            Assert.That(control.CurrentConditionBasisPoints, Is.EqualTo(6_000));
            Assert.That(world.Facilities.Single(item =>
                item.Id == fixture.FacilityId).ConditionBasisPoints,
                Is.EqualTo(6_000));
            Assert.That(world.LuoyangPassageDamageRecords, Has.Count.EqualTo(1));

            Assert.That(system.EnqueueStartRepair(world, runtime,
                fixture.FacilityId, fixture.GuardCommanderPersonId,
                fixture.GuardCommanderPersonId, fixture.InventoryContainerId),
                Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var repair = world.LuoyangPassageRepairOrders.Single();
            var project = world.FacilityConstructionProjects.Single(item =>
                item.Id == repair.FacilityConstructionProjectId);
            Assert.That(project.Kind,
                Is.EqualTo(FacilityConstructionProjectKind.Repair));
            Assert.That(project.Materials.Sum(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.TimberMaterialProductId
                        ? item.ReservedQuantity : 0),
                Is.EqualTo(LuoyangPassageOperationsContractIds
                    .GateRequiredTimberUnits));
            Assert.That(project.Materials.Sum(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.IronMaterialProductId
                        ? item.ReservedQuantity : 0),
                Is.EqualTo(LuoyangPassageOperationsContractIds
                    .GateRequiredIronUnits));
            Assert.That(world.InventoryTransactions.Count(item =>
                    item.SourceFacilityConstructionProjectId == project.Id &&
                    item.Type == InventoryTransactionType
                        .FacilityConstructionMaterialReserved),
                Is.EqualTo(1));

            system.ContributeRepairLabor(world, repair.Id,
                fixture.GuardCommanderPersonId, 480);
            world.AbsoluteDay = 1;
            system.ContributeRepairLabor(world, repair.Id,
                fixture.GuardCommanderPersonId, 480);
            world.AbsoluteDay = project.EarliestCompletionDay;
            Assert.That(system.EnqueueCompleteRepair(world, runtime, repair.Id,
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);

            Assert.That(repair.Status,
                Is.EqualTo(LuoyangPassageRepairStatus.Completed));
            Assert.That(project.Status,
                Is.EqualTo(FacilityConstructionStatus.Completed));
            Assert.That(control.CurrentConditionBasisPoints, Is.EqualTo(10_000));
            Assert.That(control.ActiveRepairOrderId, Is.Empty);
            Assert.That(passage.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId));
            Assert.That(world.ProductBatches.Single(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.TimberMaterialProductId).Quantity,
                Is.EqualTo(12));
            Assert.That(world.ProductBatches.Single(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.IronMaterialProductId).Quantity,
                Is.EqualTo(3));
            Assert.That(world.InventoryTransactions.Count(item =>
                    item.SourceFacilityConstructionProjectId == project.Id &&
                    item.Type == InventoryTransactionType
                        .FacilityConstructionMaterialConsumed),
                Is.EqualTo(1));

            Assert.That(system.EnqueueTransition(world, runtime,
                fixture.FacilityId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.guard-reopen-after-repair.v1",
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(passage.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId));
            world.Validate();

            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);
            Assert.That(WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(json));
            Assert.That(loaded.LuoyangPassageOperationalControls,
                Has.Count.EqualTo(1));
            Assert.That(loaded.LuoyangPassageDamageRecords,
                Has.Count.EqualTo(1));
            Assert.That(loaded.LuoyangPassageRepairOrders,
                Has.Count.EqualTo(1));
            loaded.LuoyangPassageRepairOrders.Single().CompletionEventId =
                "luoyang.passage.event.tampered";
            Assert.Throws<System.InvalidOperationException>(() =>
                loaded.Validate());
        }

        [Test]
        public void LuoyangPassageOperations_RejectsFalseAuthorityAndMaterialShortage()
        {
            var fixture = CreateLuoyangPassageOperationsFixture();
            var world = fixture.World;
            var runtime = fixture.Runtime;
            var system = fixture.System;
            Assert.That(system.EnqueueGuardAssignment(world, runtime,
                fixture.FacilityId, fixture.GuardArmyId,
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);

            Assert.Throws<System.InvalidOperationException>(() =>
                system.EnqueueBattleDamage(world, runtime,
                    fixture.FacilityId, fixture.BattleId, 1_000,
                    "passage.reason.false-attacker.v1",
                    fixture.GuardCommanderPersonId));
            Assert.That(system.EnqueueBattleDamage(world, runtime,
                fixture.FacilityId, fixture.BattleId, 4_000,
                "passage.reason.real-attacker.v1",
                fixture.AttackerCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);

            var timber = world.ProductBatches.Single(item =>
                item.ProductDefinitionId ==
                    CoreProductionContent.TimberMaterialProductId);
            var opening = world.InventoryTransactions.Single(item =>
                item.Id == timber.SourceTransactionId).Lines.Single();
            timber.Quantity = 7;
            opening.QuantityDelta = 7;
            world.Validate();
            Assert.That(system.EnqueueStartRepair(world, runtime,
                fixture.FacilityId, fixture.GuardCommanderPersonId,
                fixture.GuardCommanderPersonId, fixture.InventoryContainerId),
                Is.True);
            Assert.Throws<System.InvalidOperationException>(() =>
                runtime.ProcessDue(world));
            Assert.That(world.LuoyangPassageRepairOrders, Is.Empty);
            Assert.That(world.FacilityConstructionProjects, Is.Empty);
            Assert.That(world.ProductBatches.All(item =>
                item.ReservedQuantity == 0), Is.True);
            Assert.That(world.WorldCommandBatchResults.Last().Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            world.Validate();
        }

        [Test]
        public void LuoyangPassageOperations_V74MigrationIsEmptyAndNormalizesInventoryProvenance()
        {
            var legacy = WorldState.Create(184);
            legacy.SchemaVersion = 74;
            legacy.LuoyangPassageOperationalControls = null;
            legacy.LuoyangPassageDamageRecords = null;
            legacy.LuoyangPassageRepairOrders = null;
            legacy.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = "inventory_transaction.v74.provenance-probe",
                SourceFacilityConstructionProjectId = null
            });

            var migrated = WorldSnapshotMigrator.MigrateToCurrent(legacy);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.LuoyangPassageOperationalControls, Is.Empty);
            Assert.That(migrated.LuoyangPassageDamageRecords, Is.Empty);
            Assert.That(migrated.LuoyangPassageRepairOrders, Is.Empty);
            Assert.That(migrated.InventoryTransactions.Single()
                .SourceFacilityConstructionProjectId, Is.Empty);
            migrated.InventoryTransactions.Clear();
            migrated.Validate();
        }

        [Test]
        public void LuoyangRoadConnectorPassageTraversal_AuthorsAndBlocksPassages()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Assets",
                "StreamingAssets", "WorldMap");
            var coverage = new LuoyangFacilityModelCoverageSource(root);
            var production = new LuoyangProductionBuildingKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var performance = new LuoyangBuildingPerformancePlanSource(root,
                coverage.Bindings, coverage.CombinedCatalog).Plan;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                root, coverage.CombinedCatalog, performance).Catalog;
            var defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                root, coverage.CombinedCatalog, gates, performance).Catalog;
            var resources = new LuoyangResourceAgricultureProductionKitSource(
                root, coverage.CombinedCatalog, performance).Catalog;
            var civic = new LuoyangFinalCivicRitualMedicalProductionKitSource(
                root, coverage.CombinedCatalog, landmarks, performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(root,
                production, landmarks, gates, fabric, infrastructure, defense,
                resources, civic, performance).Plan;
            var composition = LuoyangWholeCityCompositionRules.CreatePlan(
                performance, review);
            var interaction = LuoyangFacilityInteractionNavigationRules
                .CreatePlan(performance, composition);
            var plan = LuoyangRoadConnectorPassageTraversalRules.CreatePlan(
                interaction);
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateInitialSession(plan);

            Assert.That(plan.ModeledConnectors.Count, Is.EqualTo(28));
            Assert.That(plan.NavigationEdges.Count, Is.EqualTo(402));
            Assert.That(session.Records.Count, Is.EqualTo(20));
            var gate = plan.PassageFacilityIds.First();
            Assert.That(LuoyangRoadConnectorPassageTraversalRules
                .FindFacilityPath(plan, session, gate, gate).Count,
                Is.EqualTo(1));
            session.SetStatus(gate,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                1, "passage.reason.core-test.v1");
            Assert.That(LuoyangRoadConnectorPassageTraversalRules
                .FindFacilityPath(plan, session, gate, gate), Is.Empty);
        }

        [Test]
        public void LuoyangPassagePedestrianPresentation_ProjectsDeterministicBlockingWithoutPersistence()
        {
            var plan = BuildLuoyangPassagePlan();
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateInitialSession(plan);
            var gateId = plan.PassageFacilityIds.First(item =>
                !string.Equals(plan.NavigationNodesByFacilityId[item]
                        .FacilityDefinitionId, "facility.public.bridge",
                    System.StringComparison.Ordinal));
            var bridgeId = plan.PassageFacilityIds.First(item =>
                string.Equals(plan.NavigationNodesByFacilityId[item]
                        .FacilityDefinitionId, "facility.public.bridge",
                    System.StringComparison.Ordinal));

            var opening = LuoyangPassagePedestrianPresentationRules.CreatePlan(
                plan, session);
            Assert.That(opening.States.Count, Is.EqualTo(20));
            Assert.That(opening.ChangesSaveSchema, Is.False);
            Assert.That(opening.PersistsAcrossSave, Is.False);
            Assert.That(opening.IsWorldStateProjection, Is.False);
            Assert.That(opening.States.All(item =>
                !item.BlocksPedestrianTraversal &&
                item.VisualStateId ==
                    LuoyangPassagePedestrianPresentationIds.OpenVisualStateId),
                Is.True);

            session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                1, "passage.reason.pedestrian-closed.v1");
            session.SetStatus(bridgeId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                1, "passage.reason.pedestrian-damaged.v1");
            var first = LuoyangPassagePedestrianPresentationRules.CreatePlan(
                plan, session);
            var second = LuoyangPassagePedestrianPresentationRules.CreatePlan(
                plan, session);
            Assert.That(first.Get(gateId).BlocksPedestrianTraversal, Is.True);
            Assert.That(first.Get(gateId).VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds.ClosedVisualStateId));
            Assert.That(first.Get(bridgeId).BlocksPedestrianTraversal,
                Is.False);
            Assert.That(first.Get(bridgeId).ConditionBasisPoints,
                Is.EqualTo(5_000));
            Assert.That(second.States.Select(item => string.Join("|",
                    item.FacilityId, item.TraversalStatusId,
                    item.VisualStateId, item.BlocksPedestrianTraversal,
                    item.ConditionBasisPoints, item.PassageRevision)).ToArray(),
                Is.EqualTo(first.States.Select(item => string.Join("|",
                    item.FacilityId, item.TraversalStatusId,
                    item.VisualStateId, item.BlocksPedestrianTraversal,
                    item.ConditionBasisPoints, item.PassageRevision)).ToArray()));

            session.SetStatus(bridgeId,
                LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                2, "passage.reason.pedestrian-destroyed.v1");
            var destroyed = LuoyangPassagePedestrianPresentationRules
                .CreatePlan(plan, session).Get(bridgeId);
            Assert.That(destroyed.BlocksPedestrianTraversal, Is.True);
            Assert.That(destroyed.ConditionBasisPoints, Is.Zero);
            Assert.That(destroyed.VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds.DestroyedVisualStateId));
        }

        [Test]
        public void LuoyangClickToWalkPedestrian_UsesStableWidthsCostsAndDynamicPassageRules()
        {
            var plan = BuildLuoyangPassagePlan();
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateInitialSession(plan);
            var gateId = plan.PassageFacilityIds.First(item =>
                !string.Equals(plan.NavigationNodesByFacilityId[item]
                        .FacilityDefinitionId, "facility.public.bridge",
                    System.StringComparison.Ordinal));
            var bridgeId = plan.PassageFacilityIds.First(item =>
                string.Equals(plan.NavigationNodesByFacilityId[item]
                        .FacilityDefinitionId, "facility.public.bridge",
                    System.StringComparison.Ordinal));
            var gateNode = plan.NavigationNodesByFacilityId[gateId];
            var bridgeNode = plan.NavigationNodesByFacilityId[bridgeId];
            var nodeById = plan.NavigationNodes.ToDictionary(item =>
                item.NodeId, System.StringComparer.Ordinal);
            var gateRoadIds = plan.NavigationEdges.Where(item =>
                    item.EdgeProfileId ==
                        LuoyangRoadConnectorPassageTraversalIds
                            .PassageApproachEdgeProfileId &&
                    (item.FromNodeId == gateNode.NodeId ||
                     item.ToNodeId == gateNode.NodeId))
                .Select(item => item.FromNodeId == gateNode.NodeId
                    ? nodeById[item.ToNodeId].FacilityId
                    : nodeById[item.FromNodeId].FacilityId)
                .OrderBy(item => item, System.StringComparer.Ordinal).ToArray();
            var gateRoadId = gateRoadIds[0];
            var bridgeRoadId = plan.NavigationEdges.Where(item =>
                    item.EdgeProfileId ==
                        LuoyangRoadConnectorPassageTraversalIds
                            .PassageApproachEdgeProfileId &&
                    (item.FromNodeId == bridgeNode.NodeId ||
                     item.ToNodeId == bridgeNode.NodeId))
                .Select(item => item.FromNodeId == bridgeNode.NodeId
                    ? nodeById[item.ToNodeId].FacilityId
                    : nodeById[item.FromNodeId].FacilityId).First();

            const string actorId = "person.luoyang.walking-core-test";
            var open = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadId, gateId);
            var repeated = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadId, gateId);
            Assert.That(open.ContractId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.ContractId));
            Assert.That(open.StatusId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.StatusId));
            Assert.That(open.CanWalk, Is.True);
            Assert.That(open.CreatesPermanentPerson, Is.False);
            Assert.That(open.ChangesSaveSchema, Is.False);
            Assert.That(open.PersistsAcrossSave, Is.False);
            Assert.That(open.FacilityIds, Is.EqualTo(repeated.FacilityIds));
            Assert.That(open.Segments.Select(item => string.Join("|",
                    item.EdgeId, item.WidthProfileId, item.WidthMetres,
                    item.LateralOffsetMetres)).ToArray(),
                Is.EqualTo(repeated.Segments.Select(item => string.Join("|",
                    item.EdgeId, item.WidthProfileId, item.WidthMetres,
                    item.LateralOffsetMetres)).ToArray()));
            Assert.That(open.Segments.Single().WidthProfileId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.GateWidthProfileId));
            Assert.That(open.Segments.Single().WidthMetres, Is.EqualTo(12f));
            Assert.That(open.Segments.Single().UsesPassage, Is.True);
            var crossing = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadIds[0], gateRoadIds[1]);
            Assert.That(crossing.CanWalk, Is.True);
            Assert.That(crossing.FacilityIds, Does.Contain(gateId));

            session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId, 1,
                "passage.reason.walking-core-damaged.v1");
            var damaged = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadId, gateId);
            Assert.That(damaged.CanWalk, Is.True);
            Assert.That(damaged.UsesDamagedPassage, Is.True);
            Assert.That(damaged.WeightedDistanceMetres,
                Is.GreaterThan(open.WeightedDistanceMetres));
            Assert.That(damaged.EstimatedDurationSeconds,
                Is.GreaterThan(open.EstimatedDurationSeconds));

            session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId, 2,
                "passage.reason.walking-core-closed.v1");
            var blocked = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadId, gateId);
            Assert.That(blocked.CanWalk, Is.False);
            Assert.That(blocked.FailureReasonId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.BlockedPassageReasonId));

            var bridge = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, bridgeRoadId, bridgeId);
            Assert.That(bridge.CanWalk, Is.True);
            Assert.That(bridge.Segments.Single().WidthProfileId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.BridgeWidthProfileId));
            Assert.That(bridge.Segments.Single().WidthMetres, Is.EqualTo(8f));

            var connectorWalk = plan.ModeledConnectors.Select(connector =>
                LuoyangClickToWalkPedestrianRules.CreatePlan(plan, session,
                    actorId, nodeById[connector.FromNodeId].FacilityId,
                    nodeById[connector.ToNodeId].FacilityId)).First(item =>
                item.CanWalk && item.UsesModeledConnector);
            Assert.That(connectorWalk.Segments.First(item =>
                    item.UsesModeledConnector).WidthProfileId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds
                    .ModeledConnectorWidthProfileId));
            Assert.That(connectorWalk.Segments.First(item =>
                    item.UsesModeledConnector).WidthMetres, Is.EqualTo(12f));
        }

        [Test]
        public void LuoyangPassagePedestrianPresentation_UsesV75IntegrityAndActiveRepairReadOnly()
        {
            var fixture = CreateLuoyangPassageOperationsFixture();
            var world = fixture.World;
            var runtime = fixture.Runtime;
            var system = fixture.System;
            system.EnqueueGuardAssignment(world, runtime, fixture.FacilityId,
                fixture.GuardArmyId, fixture.GuardCommanderPersonId);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            system.EnqueueBattleDamage(world, runtime, fixture.FacilityId,
                fixture.BattleId, 4_000,
                "passage.reason.pedestrian-projection-damage.v1",
                fixture.AttackerCommanderPersonId);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            system.EnqueueStartRepair(world, runtime, fixture.FacilityId,
                fixture.GuardCommanderPersonId,
                fixture.GuardCommanderPersonId,
                fixture.InventoryContainerId);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var serializedBefore = WorldSnapshotSerializer.Serialize(world);
            var passagePlan = BuildLuoyangPassagePlan();
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateSessionFromWorldState(passagePlan, world);

            var presentation = LuoyangPassagePedestrianPresentationRules
                .CreatePlan(passagePlan, session, world);
            var state = presentation.Get(fixture.FacilityId);
            Assert.That(presentation.IsWorldStateProjection, Is.True);
            Assert.That(state.ConditionBasisPoints, Is.EqualTo(6_000));
            Assert.That(state.IntegrityRevision, Is.EqualTo(1));
            Assert.That(state.IsRepairing, Is.True);
            Assert.That(state.BlocksPedestrianTraversal, Is.False);
            Assert.That(state.VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds
                    .RepairingVisualStateId));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(serializedBefore));
        }

        [Test]
        public void MovePersonCommandTests_FormalSessionCreatesPersistentAction()
        {
            var fixture = CreateFormalMovementFixture();
            var target = FindFarReachableTarget(fixture);
            Assert.That(fixture.Service.TryRequest(fixture.World, target,
                out var movement, out var plan, out var failure), Is.True,
                failure);
            Assert.That(new PlayerSession(fixture.World).ControlledPersonId,
                Is.EqualTo(FormalPlayerPersonId));
            Assert.That(movement.PersonId, Is.EqualTo(FormalPlayerPersonId));
            Assert.That(movement.Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Active));
            Assert.That(movement.Segments, Has.Count.EqualTo(
                plan.Segments.Count));
            Assert.That(fixture.World.PersistentWorldCommands.Single(item =>
                    item.Id == movement.RequestCommandId).CommandTypeId,
                Is.EqualTo(LuoyangFormalPlayerMovementIds.MoveCommandTypeId));
            fixture.World.Validate();
        }

        [Test]
        public void MovementValidationTests_RejectMissingInvalidAndStaleOrigins()
        {
            var fixture = CreateFormalMovementFixture();
            Assert.That(fixture.System.TryCreatePlan(fixture.World,
                    "facility.missing.target", out _, out var invalidTarget),
                Is.False);
            Assert.That(invalidTarget, Is.Not.Empty);
            fixture.World.People.Single(item => item.Id ==
                FormalPlayerPersonId).IsAlive = false;
            Assert.That(fixture.System.TryCreatePlan(fixture.World,
                    FindAnyOtherFacility(fixture), out _, out var deadReason),
                Is.False);
            Assert.That(deadReason, Is.EqualTo(
                "movement.rejection.person-cannot-act.v1"));

            var stale = CreateFormalMovementFixture();
            var commandId = stale.System.EnqueueMove(stale.World,
                stale.Runtime, FindFarReachableTarget(stale));
            var command = stale.World.PersistentWorldCommands.Single(item =>
                item.Id == commandId);
            command.Arguments.Single(item => item.Key ==
                "origin_facility_id").Value = FindAnyOtherFacility(stale);
            Assert.Throws<System.InvalidOperationException>(() =>
                stale.Runtime.ProcessDue(stale.World));
            Assert.That(stale.World.WorldCommandBatchResults.Last().Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
        }

        [Test]
        public void MovementValidationTests_RejectsInsufficientStaminaAndFood()
        {
            var stamina = CreateFormalMovementFixture();
            var target = FindFarReachableTarget(stamina);
            new PlayerSession(stamina.World).ControlledPerson
                .StaminaBasisPoints = 0;
            Assert.That(stamina.System.TryCreatePlan(stamina.World, target,
                    out _, out var staminaReason), Is.False);
            Assert.That(staminaReason, Is.EqualTo(
                LuoyangFormalPlayerMovementIds.InsufficientStaminaReasonId));

            var food = CreateFormalMovementFixture();
            var foodTarget = FindFoodCostTarget(food);
            new PlayerSession(food.World).ControlledPerson.Provisions = 0;
            Assert.That(food.System.TryCreatePlan(food.World, foodTarget,
                    out _, out var foodReason), Is.False);
            Assert.That(foodReason, Is.EqualTo(
                LuoyangFormalPlayerMovementIds.InsufficientFoodReasonId));
        }

        [Test]
        public void MovementCostCalculatorTests_FixedInputsAreExactAndDataDriven()
        {
            var calculator = new LuoyangMovementCostCalculator(
                new LuoyangMovementCostPolicy(80, 20, 360));
            var cost = calculator.CalculateSegment(2_000d, 3_600d, 2_000);
            Assert.That(cost.DistanceMetres, Is.EqualTo(2_000));
            Assert.That(cost.WeightedDistanceMetres, Is.EqualTo(3_960));
            Assert.That(cost.DurationMinutes, Is.EqualTo(50));
            Assert.That(cost.StaminaCostBasisPoints, Is.EqualTo(198));
            Assert.That(calculator.CalculateFoodCost(359), Is.Zero);
            Assert.That(calculator.CalculateFoodCost(360), Is.EqualTo(1));
            Assert.That(calculator.CalculateWorldSegments(361), Is.EqualTo(2));
        }

        [Test]
        public void PersonLocationUpdateTests_SettlesTimeStaminaFoodAndLocation()
        {
            var fixture = CreateFormalMovementFixture();
            var person = new PlayerSession(fixture.World).ControlledPerson;
            var openingDay = fixture.World.AbsoluteDay;
            var openingSegment = fixture.World.Segment;
            var openingStamina = person.StaminaBasisPoints;
            var openingFood = person.Provisions;
            var target = FindFarReachableTarget(fixture);
            fixture.Service.TryRequest(fixture.World, target,
                out var movement, out _, out _);
            fixture.Service.Complete(fixture.World, movement.Id);
            Assert.That(movement.Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Completed));
            Assert.That(person.CurrentFacilityId, Is.EqualTo(target));
            Assert.That(person.CurrentCellId64,
                Is.EqualTo(movement.TargetCellId64));
            Assert.That(openingStamina - person.StaminaBasisPoints,
                Is.EqualTo(movement.ExpectedStaminaCostBasisPoints));
            Assert.That(openingFood - person.Provisions,
                Is.EqualTo(movement.ExpectedFoodCost));
            Assert.That(fixture.World.AbsoluteDay > openingDay ||
                        fixture.World.Segment > openingSegment, Is.True);
            Assert.That(fixture.World.WorldEventOutbox.Any(item =>
                item.EventTypeId == LuoyangFormalPlayerMovementIds
                    .MovementCompletedEventTypeId), Is.True);
            Assert.That(fixture.World.WorldEventOutbox.Any(item =>
                item.EventTypeId == LuoyangFormalPlayerMovementIds
                    .LocationChangedEventTypeId), Is.True);
        }

        [Test]
        public void RoutePassabilityTests_RoadBlockDestroyAndRepairAreAuthoritative()
        {
            var fixture = CreateFormalMovementFixture();
            var start = new PlayerSession(fixture.World).ControlledPerson
                .CurrentFacilityId;
            var road = fixture.World.LuoyangRoadOperationalSegments.First(item =>
                item.FromFacilityId == start || item.ToFacilityId == start);
            var target = road.FromFacilityId == start
                ? road.ToFacilityId
                : road.FromFacilityId;
            fixture.System.EnqueueRoadTransition(fixture.World,
                fixture.Runtime, road.EdgeId,
                LuoyangFormalPlayerMovementIds.DestroyedRoadStatusId,
                "road.reason.test-destroyed.v1");
            fixture.Runtime.ProcessDue(fixture.World);
            fixture.Runtime.DispatchPublishedEvents(fixture.World);
            Assert.That(fixture.System.TryCreatePlan(fixture.World, target,
                out _, out _), Is.False);
            fixture.System.EnqueueRoadTransition(fixture.World,
                fixture.Runtime, road.EdgeId,
                LuoyangFormalPlayerMovementIds.OpenRoadStatusId,
                "road.reason.test-repaired.v1");
            fixture.Runtime.ProcessDue(fixture.World);
            fixture.Runtime.DispatchPublishedEvents(fixture.World);
            Assert.That(fixture.System.TryCreatePlan(fixture.World, target,
                out var reopened, out _), Is.True);
            Assert.That(reopened.CanWalk, Is.True);
        }

        [Test]
        public void GateBridgePassabilityTests_ReadsPersistedPassageState()
        {
            foreach (var bridge in new[] { false, true })
            {
                var open = CreatePassageMovementFixture(bridge);
                Assert.That(open.Service.TryRequest(open.World,
                    open.TargetFacilityId, out var movement, out _, out _),
                    Is.True);
                open.Service.Complete(open.World, movement.Id);
                Assert.That(movement.Status,
                    Is.EqualTo(LuoyangFormalMovementStatus.Completed));
                var blocked = CreatePassageMovementFixture(bridge);
                blocked.PassageSystem.EnqueueTransition(blocked.World,
                    blocked.Runtime, blocked.PassageFacilityId,
                    LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                    "passage.reason.movement-test-destroyed.v1",
                    FormalPlayerPersonId);
                blocked.Runtime.ProcessDue(blocked.World);
                blocked.Runtime.DispatchPublishedEvents(blocked.World);
                Assert.That(blocked.System.TryCreatePlan(blocked.World,
                    blocked.PassageFacilityId, out _, out _), Is.False);
            }
        }

        [Test]
        public void RouteInvalidationTests_StopsAtBoundaryWhenGateCloses()
        {
            var fixture = CreatePassageMovementFixture(false);
            Assert.That(fixture.Service.TryRequest(fixture.World,
                fixture.TargetFacilityId, out var movement, out _, out _),
                Is.True);
            fixture.PassageSystem.EnqueueTransition(fixture.World,
                fixture.Runtime, fixture.PassageFacilityId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.movement-test-closed.v1",
                FormalPlayerPersonId);
            fixture.Runtime.ProcessDue(fixture.World);
            fixture.Runtime.DispatchPublishedEvents(fixture.World);
            fixture.Service.AdvanceNextSegment(fixture.World, movement.Id);
            Assert.That(movement.Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Interrupted));
            Assert.That(movement.FailureReasonId, Is.EqualTo(
                LuoyangFormalPlayerMovementIds.InvalidRouteReasonId));
            Assert.That(fixture.World.WorldEventOutbox.Any(item =>
                item.EventTypeId == LuoyangFormalPlayerMovementIds
                    .RouteInvalidatedEventTypeId), Is.True);
        }

        [Test]
        public void MovementSaveLoadTests_ActiveSegmentBoundaryResumesExactly()
        {
            var fixture = CreateFormalMovementFixture();
            var target = FindFarReachableTarget(fixture);
            fixture.Service.TryRequest(fixture.World, target,
                out var movement, out _, out _);
            Assert.That(movement.Segments.Count, Is.GreaterThan(1));
            fixture.Service.AdvanceNextSegment(fixture.World, movement.Id);
            var boundarySnapshot = WorldSnapshotSerializer.Serialize(
                fixture.World);
            fixture.Service.Complete(fixture.World, movement.Id);
            var continuousHash = MovementStateHash(fixture.World);
            var loaded = WorldSnapshotSerializer.Deserialize(boundarySnapshot);
            var resumed = CreateFormalMovementRuntime(loaded, fixture.Plan);
            resumed.Service.Complete(loaded, movement.Id);
            Assert.That(MovementStateHash(loaded), Is.EqualTo(continuousHash));
            Assert.That(loaded.LuoyangFormalPlayerMovements.Single(item =>
                    item.Id == movement.Id).Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Completed));
        }

        [Test]
        public void MovementSaveLoadTests_V75MigratesWithoutInventingLocalFacts()
        {
            var legacy = WorldState.Create(184);
            legacy.SchemaVersion = 75;
            legacy.LuoyangLocalNavigationLocations = null;
            legacy.LuoyangRoadOperationalSegments = null;
            legacy.LuoyangFormalPlayerMovements = null;
            var migrated = WorldSnapshotMigrator.MigrateToCurrent(legacy);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.LuoyangLocalNavigationLocations, Is.Empty);
            Assert.That(migrated.LuoyangRoadOperationalSegments, Is.Empty);
            Assert.That(migrated.LuoyangFormalPlayerMovements, Is.Empty);
            migrated.Validate();
        }

        [Test]
        public void MovementReplayTests_ThreeRunsProduceIdenticalStateHash()
        {
            var hashes = new string[3];
            for (var run = 0; run < hashes.Length; run++)
            {
                var fixture = CreateFormalMovementFixture();
                var target = FindFarReachableTarget(fixture);
                fixture.Service.TryRequest(fixture.World, target,
                    out var movement, out _, out _);
                fixture.Service.Complete(fixture.World, movement.Id);
                hashes[run] = MovementStateHash(fixture.World);
                System.Console.WriteLine("REPLAY_RUN_" + (run + 1) +
                    "_HASH=" + hashes[run]);
            }
            Assert.That(hashes[1], Is.EqualTo(hashes[0]));
            Assert.That(hashes[2], Is.EqualTo(hashes[0]));
        }

        private const string FormalPlayerPersonId =
            "person.m26.formal-player";

        private static FormalMovementFixture CreateFormalMovementFixture(
            string initialFacilityId = null)
        {
            var plan = BuildLuoyangPassagePlan();
            var roadNodes = plan.NavigationNodes.Where(item =>
                    item.FacilityDefinitionId == "facility.public.road")
                .OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId,
                    System.StringComparer.Ordinal).ToArray();
            initialFacilityId ??= roadNodes[0].FacilityId;
            var world = WorldState.Create(184);
            const string locationId = "location.luoyang.formal-movement";
            world.Locations.Add(new LocationState
            {
                Id = locationId,
                DisplayName = "洛阳",
                Kind = LocationKind.RegionalSeat,
                Terrain = TerrainKind.Plains,
                Features = LocationFeature.Government |
                    LocationFeature.Market,
                Population = 1
            });
            world.People.Add(new PersonState
            {
                Id = FormalPlayerPersonId,
                DisplayName = "M26 正式玩家",
                LocationId = locationId,
                BirthLocationId = locationId,
                StaminaBasisPoints = 10_000,
                Provisions = 100
            });
            world.PlayerPersonId = FormalPlayerPersonId;
            world.PopulationStorage.SynchronizeInlineCounts(world.People);
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(plan);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(world, runtime);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var system = new LuoyangFormalPlayerMovementSystem(plan);
            system.RegisterHandlers(runtime);
            system.EnsureInitialized(world, runtime, initialFacilityId);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var fixture = CreateFormalMovementRuntime(world, plan, runtime,
                passageSystem, system);
            world.Validate();
            return fixture;
        }

        private static FormalMovementFixture CreateFormalMovementRuntime(
            WorldState world, LuoyangRoadTraversalRefinementPlan plan,
            WorldCommandRuntime runtime = null,
            LuoyangPassageWorldCommandSystem passageSystem = null,
            LuoyangFormalPlayerMovementSystem system = null)
        {
            runtime ??= new WorldCommandRuntime();
            passageSystem ??= new LuoyangPassageWorldCommandSystem(plan);
            passageSystem.RegisterHandlers(runtime);
            system ??= new LuoyangFormalPlayerMovementSystem(plan);
            system.RegisterHandlers(runtime);
            var simulator = new WorldSimulator(world.MasterSeed, null,
                new WorldStatePersonRepository(world), runtime);
            var service = new LuoyangFormalPlayerMovementService(system,
                runtime, simulator);
            return new FormalMovementFixture(world, plan, runtime,
                passageSystem, system, service);
        }

        private static PassageMovementFixture CreatePassageMovementFixture(
            bool bridge)
        {
            var plan = BuildLuoyangPassagePlan();
            var passage = plan.PassageFacilityIds.Select(item =>
                    plan.NavigationNodesByFacilityId[item])
                .First(item => (item.FacilityDefinitionId ==
                    "facility.public.bridge") == bridge);
            var nodeById = plan.NavigationNodes.ToDictionary(item =>
                item.NodeId, System.StringComparer.Ordinal);
            var approaches = plan.NavigationEdges.Where(item =>
                    item.EdgeProfileId ==
                    LuoyangRoadConnectorPassageTraversalIds
                        .PassageApproachEdgeProfileId &&
                    (item.FromNodeId == passage.NodeId ||
                     item.ToNodeId == passage.NodeId))
                .Select(item => nodeById[item.FromNodeId == passage.NodeId
                    ? item.ToNodeId
                    : item.FromNodeId].FacilityId)
                .OrderBy(item => item, System.StringComparer.Ordinal).ToArray();
            var fixture = CreateFormalMovementFixture(approaches[0]);
            return new PassageMovementFixture(fixture,
                passage.FacilityId, approaches[1]);
        }

        private static string FindFarReachableTarget(
            FormalMovementFixture fixture)
        {
            var start = new PlayerSession(fixture.World).ControlledPerson
                .CurrentFacilityId;
            foreach (var node in fixture.Plan.NavigationNodes.Where(item =>
                         item.FacilityDefinitionId == "facility.public.road" &&
                         item.FacilityId != start).OrderByDescending(item =>
                         item.CellId64).ThenByDescending(item => item.FacilityId,
                         System.StringComparer.Ordinal))
                if (fixture.System.TryCreatePlan(fixture.World,
                        node.FacilityId, out var plan, out _) &&
                    plan.Segments.Count > 1)
                    return node.FacilityId;
            throw new System.InvalidOperationException(
                "No multi-segment movement target exists.");
        }

        private static string FindAnyOtherFacility(
            FormalMovementFixture fixture) => fixture.Plan.NavigationNodes
            .First(item => item.FacilityId != new PlayerSession(fixture.World)
                .ControlledPerson.CurrentFacilityId).FacilityId;

        private static string FindFoodCostTarget(FormalMovementFixture fixture)
        {
            var calculator = new LuoyangMovementCostCalculator();
            var start = new PlayerSession(fixture.World).ControlledPerson
                .CurrentFacilityId;
            foreach (var node in fixture.Plan.NavigationNodes.Where(item =>
                         item.FacilityId != start).OrderByDescending(item =>
                         item.CellId64))
                if (fixture.System.TryCreatePlan(fixture.World,
                        node.FacilityId, out var plan, out _))
                {
                    var minutes = plan.Segments.Sum(item => calculator
                        .CalculateSegment(item.DistanceMetres,
                            item.WeightedDistanceMetres).DurationMinutes);
                    if (calculator.CalculateFoodCost(minutes) > 0)
                        return node.FacilityId;
                }
            throw new System.InvalidOperationException(
                "No route reaches the feeding interval.");
        }

        private static string MovementStateHash(WorldState world)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(
                WorldSnapshotSerializer.Serialize(world));
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(bytes).Select(item =>
                item.ToString("x2",
                    System.Globalization.CultureInfo.InvariantCulture)));
        }

        private static LuoyangRoadTraversalRefinementPlan
            BuildLuoyangPassagePlan()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Assets",
                "StreamingAssets", "WorldMap");
            var coverage = new LuoyangFacilityModelCoverageSource(root);
            var production = new LuoyangProductionBuildingKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var performance = new LuoyangBuildingPerformancePlanSource(root,
                coverage.Bindings, coverage.CombinedCatalog).Plan;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                root, coverage.CombinedCatalog, performance).Catalog;
            var defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                root, coverage.CombinedCatalog, gates, performance).Catalog;
            var resources = new LuoyangResourceAgricultureProductionKitSource(
                root, coverage.CombinedCatalog, performance).Catalog;
            var civic = new LuoyangFinalCivicRitualMedicalProductionKitSource(
                root, coverage.CombinedCatalog, landmarks, performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(root,
                production, landmarks, gates, fabric, infrastructure, defense,
                resources, civic, performance).Plan;
            var composition = LuoyangWholeCityCompositionRules.CreatePlan(
                performance, review);
            var interaction = LuoyangFacilityInteractionNavigationRules
                .CreatePlan(performance, composition);
            return LuoyangRoadConnectorPassageTraversalRules.CreatePlan(
                interaction);
        }

        private static CellTraversalPlan BuildFreightGateCellPlan(
            string gateFacilityId,
            out ulong originCellId64,
            out ulong targetCellId64)
        {
            return BuildFreightPassageCellPlan(
                gateFacilityId,
                FacilitySpatialCapabilityIds.Gate,
                out originCellId64,
                out targetCellId64);
        }

        private static CellTraversalPlan BuildFreightPassageCellPlan(
            string passageFacilityId,
            string facilityCapabilityId,
            out ulong originCellId64,
            out ulong targetCellId64)
        {
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            originCellId64 = grid.ToCellId(1_200, 2_000).Value;
            var gateCellId64 = grid.ToCellId(1_200, 2_001).Value;
            targetCellId64 = grid.ToCellId(1_200, 2_002).Value;
            var origin = FreightCellProfile(originCellId64, string.Empty,
                string.Empty);
            var gate = FreightCellProfile(gateCellId64, passageFacilityId,
                facilityCapabilityId);
            var target = FreightCellProfile(targetCellId64, string.Empty,
                string.Empty);
            EnableFreightPort(origin, CellTraversalDirection.East,
                CellTraversalIds.StaticConditionId, string.Empty);
            EnableFreightPort(gate, CellTraversalDirection.West,
                CellTraversalIds.FormalPassageConditionId,
                passageFacilityId);
            EnableFreightPort(gate, CellTraversalDirection.East,
                CellTraversalIds.FormalPassageConditionId,
                passageFacilityId);
            EnableFreightPort(target, CellTraversalDirection.West,
                CellTraversalIds.StaticConditionId, string.Empty);
            return new CellTraversalPlan(
                new[] { origin, gate, target }, new string('a', 64));
        }

        private static RoadRerouteFreightFixture
            CreateRoadRerouteFreightFixture(
                ulong seed, string movementCapabilityId)
        {
            const string roadEdgeId = "road.edge.freight-reroute.v1";
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var originCellId64 = grid.ToCellId(1_210, 2_000).Value;
            var roadCellId64 = grid.ToCellId(1_210, 2_001).Value;
            var targetCellId64 = grid.ToCellId(1_210, 2_002).Value;
            var alternateWestCellId64 = grid.ToCellId(1_209, 2_000).Value;
            var alternateMiddleCellId64 = grid.ToCellId(1_209, 2_001).Value;
            var alternateEastCellId64 = grid.ToCellId(1_209, 2_002).Value;
            var origin = FreightCellProfile(originCellId64, string.Empty,
                string.Empty);
            var road = FreightCellProfile(roadCellId64, string.Empty,
                FacilitySpatialCapabilityIds.Road);
            var target = FreightCellProfile(targetCellId64, string.Empty,
                string.Empty);
            var alternateWest = FreightCellProfile(
                alternateWestCellId64, string.Empty, string.Empty);
            var alternateMiddle = FreightCellProfile(
                alternateMiddleCellId64, string.Empty, string.Empty);
            var alternateEast = FreightCellProfile(
                alternateEastCellId64, string.Empty, string.Empty);
            foreach (var profile in new[]
                     {
                         origin, road, target, alternateWest,
                         alternateMiddle, alternateEast
                     })
                profile.InternalTopology = CellInternalTopology.OpenArea;
            origin.TraversalCostPermilleByCapability[
                MovementCapabilityIds.Cart] = 1_000;
            road.TraversalCostPermilleByCapability[
                MovementCapabilityIds.Cart] = 1_000;
            target.TraversalCostPermilleByCapability[
                MovementCapabilityIds.Cart] = 1_000;
            EnableFreightPortForCapabilities(origin,
                CellTraversalDirection.East,
                CellTraversalIds.FormalRoadConditionId,
                roadEdgeId,
                MovementCapabilityIds.PackAnimal,
                MovementCapabilityIds.Cart);
            EnableFreightPortForCapabilities(road,
                CellTraversalDirection.West,
                CellTraversalIds.FormalRoadConditionId,
                roadEdgeId,
                MovementCapabilityIds.PackAnimal,
                MovementCapabilityIds.Cart);
            EnableFreightPortForCapabilities(road,
                CellTraversalDirection.East,
                CellTraversalIds.FormalRoadConditionId,
                roadEdgeId,
                MovementCapabilityIds.PackAnimal,
                MovementCapabilityIds.Cart);
            EnableFreightPortForCapabilities(target,
                CellTraversalDirection.West,
                CellTraversalIds.FormalRoadConditionId,
                roadEdgeId,
                MovementCapabilityIds.PackAnimal,
                MovementCapabilityIds.Cart);

            foreach (var profile in new[]
                     {
                         alternateWest, alternateMiddle, alternateEast
                     })
                profile.TraversalCostPermilleByCapability[
                    MovementCapabilityIds.PackAnimal] = 1_500;
            EnablePackAnimalPort(origin, CellTraversalDirection.North);
            EnablePackAnimalPort(alternateWest,
                CellTraversalDirection.South);
            EnablePackAnimalPort(alternateWest,
                CellTraversalDirection.East);
            EnablePackAnimalPort(alternateMiddle,
                CellTraversalDirection.West);
            EnablePackAnimalPort(alternateMiddle,
                CellTraversalDirection.East);
            EnablePackAnimalPort(alternateEast,
                CellTraversalDirection.West);
            EnablePackAnimalPort(alternateEast,
                CellTraversalDirection.South);
            EnablePackAnimalPort(target, CellTraversalDirection.North);
            var plan = new CellTraversalPlan(new[]
            {
                origin, road, target, alternateWest, alternateMiddle,
                alternateEast
            }, new string('b', 64));
            var fixture = PrepareCivilianFreightWorld(seed, 12);
            fixture.World.LuoyangLocalNavigationLocations.Add(
                new LuoyangLocalNavigationLocationState
                {
                    Id = "local-navigation.freight.origin.v1",
                    FacilityId = "facility.freight.origin",
                    FacilityDefinitionId = "facility.public.road",
                    SettlementLocationId =
                        "location.freight_origin_village",
                    CellId64 = originCellId64,
                    GridColumn = 2_000,
                    GridRow = 1_210
                });
            fixture.World.LuoyangLocalNavigationLocations.Add(
                new LuoyangLocalNavigationLocationState
                {
                    Id = "local-navigation.freight.destination.v1",
                    FacilityId = "facility.freight.destination",
                    FacilityDefinitionId = "facility.public.road",
                    SettlementLocationId =
                        "location.freight_destination_village",
                    CellId64 = targetCellId64,
                    GridColumn = 2_002,
                    GridRow = 1_210
                });
            fixture.World.LuoyangRoadOperationalSegments.Add(
                new LuoyangRoadOperationalSegmentState
                {
                    Id = "road.operational.freight-reroute.v1",
                    EdgeId = roadEdgeId,
                    FromFacilityId = "facility.freight.origin",
                    ToFacilityId = "facility.freight.destination",
                    StatusId = LuoyangFormalPlayerMovementIds
                        .OpenRoadStatusId,
                    LastChangedDay = fixture.World.AbsoluteDay,
                    LastChangedSegment = fixture.World.Segment,
                    LastReasonId = "road.reason.initialized.v1",
                    LastCommandId = "command.road.initialized.v1",
                    LastEventId = "event.road.initialized.v1"
                });
            fixture.FreightSystem = new CivilianFreightSystem(
                fixture.World.MasterSeed, fixture.Content, plan);
            fixture.Request.OriginCellId64 = originCellId64;
            fixture.Request.TargetCellId64 = targetCellId64;
            fixture.Request.MovementCapabilityId = movementCapabilityId;
            return new RoadRerouteFreightFixture
            {
                Fixture = fixture,
                RoadEdgeId = roadEdgeId
            };
        }

        private static string RunDeterministicFreightGateReplay()
        {
            var fixture = PrepareCivilianFreightWorld(25_907, 12);
            var passagePlan = BuildLuoyangPassagePlan();
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                passagePlan);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(fixture.World, runtime);
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var gateId = fixture.World.LuoyangPassageTraversals.First(item =>
                !string.Equals(item.FacilityDefinitionId,
                    "facility.public.bridge", StringComparison.Ordinal))
                .FacilityId;
            var cellPlan = BuildFreightGateCellPlan(
                gateId, out var originCellId64, out var targetCellId64);
            fixture.FreightSystem = new CivilianFreightSystem(
                fixture.World.MasterSeed, fixture.Content, cellPlan);
            fixture.Request.OriginCellId64 = originCellId64;
            fixture.Request.TargetCellId64 = targetCellId64;
            var freight = fixture.FreightSystem.Dispatch(
                fixture.World, fixture.Request);
            passageSystem.EnqueueTransition(fixture.World, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.freight-replay-close.v1",
                "person.freight-cell-route-controller");
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            var travel = new TravelSystem();
            for (var segment = 0;
                 segment < 8 && !freight.CellRouteWaiting;
                 segment++)
                travel.AdvanceJourneysOneSegment(fixture.World);
            passageSystem.EnqueueTransition(fixture.World, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.freight-replay-open.v1",
                "person.freight-cell-route-controller");
            runtime.ProcessDue(fixture.World);
            runtime.DispatchPublishedEvents(fixture.World);
            for (var segment = 0;
                 segment < 32 && freight.Status !=
                    CivilianFreightStatus.Completed;
                 segment++)
            {
                travel.AdvanceJourneysOneSegment(fixture.World);
                fixture.FreightSystem.ResolveArrivals(fixture.World);
            }
            Assert.That(freight.Status,
                Is.EqualTo(CivilianFreightStatus.Completed));
            Assert.That(new FormalFoodConservationAuditor().Audit(
                fixture.World, fixture.Content).Difference, Is.Zero);
            fixture.World.Validate();
            return WorldSnapshotSerializer.Serialize(
                fixture.World, fixture.Content);
        }

        private static CellTraversalProfile FreightCellProfile(
            ulong cellId64, string facilityId, string capabilityId)
        {
            var profile = new CellTraversalProfile
            {
                CellId64 = cellId64,
                TerrainCapabilityId = "terrain.capability.plains.v1",
                FacilityId = facilityId,
                FacilityDefinitionId = string.IsNullOrEmpty(facilityId)
                    ? string.Empty
                    : "facility.public.gate",
                FacilityCapabilityId = capabilityId,
                AccessRequirementId = FacilityAccessRequirementIds.Optional,
                PassThroughAllowed = true,
                InternalTopology = CellInternalTopology.Straight,
                TraversalDistanceCentimetres = 800_000,
                TraversalCostPermilleByCapability =
                    new Dictionary<string, int>(StringComparer.Ordinal)
                    {
                        { MovementCapabilityIds.PackAnimal, 1_000 },
                        { MovementCapabilityIds.Foot, 1_000 }
                    }
            };
            foreach (var direction in CellTraversalDirections.All)
                profile.Ports.Add(new CellTraversalPort
                {
                    Direction = direction,
                    Enabled = false,
                    AllowsEntry = false,
                    AllowsExit = false,
                    RoleId = CellTraversalPortRoleIds.Blocked,
                    AccessPolicyId = FacilityAccessRequirementIds.Optional,
                    TraversalConditionId = CellTraversalIds.StaticConditionId,
                    MovementCapabilityIds = new List<string>()
                });
            return profile;
        }

        private static void EnablePackAnimalPort(
            CellTraversalProfile profile,
            CellTraversalDirection direction)
        {
            EnableFreightPortForCapabilities(
                profile,
                direction,
                CellTraversalIds.StaticConditionId,
                string.Empty,
                MovementCapabilityIds.PackAnimal);
        }

        private static void EnableFreightPortForCapabilities(
            CellTraversalProfile profile,
            CellTraversalDirection direction,
            string conditionId,
            string formalWorldObjectId,
            params string[] capabilities)
        {
            var port = profile.Port(direction);
            port.Enabled = true;
            port.AllowsEntry = true;
            port.AllowsExit = true;
            port.RoleId = string.Equals(conditionId,
                    CellTraversalIds.FormalPassageConditionId,
                    StringComparison.Ordinal)
                ? CellTraversalPortRoleIds.Passage
                : CellTraversalPortRoleIds.RoadConnection;
            port.TraversalConditionId = conditionId;
            port.FormalWorldObjectId = formalWorldObjectId;
            port.WidthCentimetres = 400;
            port.CapacityClass = 1;
            port.MovementCapabilityIds.AddRange(capabilities);
        }

        private static void EnableFreightPort(
            CellTraversalProfile profile,
            CellTraversalDirection direction,
            string conditionId,
            string formalWorldObjectId)
        {
            EnableFreightPortForCapabilities(
                profile,
                direction,
                conditionId,
                formalWorldObjectId,
                MovementCapabilityIds.PackAnimal,
                MovementCapabilityIds.Foot);
        }

        private sealed class RoadRerouteFreightFixture
        {
            public CivilianFreightFixture Fixture;
            public string RoadEdgeId;
        }

        private static LuoyangPassageOperationsFixture
            CreateLuoyangPassageOperationsFixture()
        {
            const string locationId = "location.luoyang.passage_test";
            const string guardOrganizationId =
                "organization.luoyang.passage_guard";
            const string attackerOrganizationId =
                "organization.luoyang.passage_attacker";
            const string guardCommanderPersonId =
                "person.luoyang.passage_guard_commander";
            const string attackerCommanderPersonId =
                "person.luoyang.passage_attacker_commander";
            const string guardArmyId = "army.luoyang.passage_guard";
            const string attackerArmyId = "army.luoyang.passage_attacker";
            const string inventoryContainerId =
                "inventory_container.luoyang.passage_repair";
            const string battleId = "battle.luoyang.passage_test";
            const ulong cellId64 = 900001;

            var plan = BuildLuoyangPassagePlan();
            var world = WorldState.Create(184);
            var runtime = new WorldCommandRuntime();
            var system = new LuoyangPassageWorldCommandSystem(plan);
            system.RegisterHandlers(runtime);
            system.EnsureInitialized(world, runtime);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var passage = world.LuoyangPassageTraversals.First(item =>
                !string.Equals(item.FacilityDefinitionId,
                    "facility.public.bridge", System.StringComparison.Ordinal));

            world.Locations.Add(new LocationState
            {
                Id = locationId,
                DisplayName = "洛阳关隘测试区",
                Kind = LocationKind.RegionalSeat,
                Terrain = TerrainKind.Plains,
                Features = LocationFeature.Garrison |
                    LocationFeature.Fortification,
                Population = 2
            });
            world.People.Add(new PersonState
            {
                Id = guardCommanderPersonId,
                DisplayName = "守关校尉",
                LocationId = locationId,
                BirthLocationId = locationId
            });
            world.People.Add(new PersonState
            {
                Id = attackerCommanderPersonId,
                DisplayName = "攻方主将",
                LocationId = locationId,
                BirthLocationId = locationId
            });
            world.Organizations.Add(new OrganizationState
            {
                Id = guardOrganizationId,
                DisplayName = "洛阳守关组织",
                Type = OrganizationType.Military,
                HeadquartersLocationId = locationId,
                LeaderPersonId = guardCommanderPersonId,
                Treasury = 1_000
            });
            world.Organizations.Add(new OrganizationState
            {
                Id = attackerOrganizationId,
                DisplayName = "洛阳攻方组织",
                Type = OrganizationType.Military,
                HeadquartersLocationId = locationId,
                LeaderPersonId = attackerCommanderPersonId,
                Treasury = 1_000
            });
            world.Armies.Add(new ArmyState
            {
                Id = guardArmyId,
                DisplayName = "洛阳守关军",
                OrganizationId = guardOrganizationId,
                CommanderPersonId = guardCommanderPersonId,
                LocationId = locationId,
                Troops = 1,
                MaximumTroops = 1,
                Provisions = 100
            });
            world.Armies.Add(new ArmyState
            {
                Id = attackerArmyId,
                DisplayName = "洛阳攻方军",
                OrganizationId = attackerOrganizationId,
                CommanderPersonId = attackerCommanderPersonId,
                LocationId = locationId,
                Troops = 1,
                MaximumTroops = 1,
                Provisions = 100
            });
            world.MilitaryFormations.Add(new MilitaryFormationState
            {
                Id = "formation.luoyang.passage_guard.root",
                ArmyId = guardArmyId,
                ParentFormationId = string.Empty,
                DisplayName = "洛阳守关军本阵",
                Kind = MilitaryFormationKind.Army,
                CommanderPersonId = guardCommanderPersonId,
                AuthorizedStrength = 1,
                DisplayOrder = 0
            });
            world.MilitaryFormations.Add(new MilitaryFormationState
            {
                Id = "formation.luoyang.passage_attacker.root",
                ArmyId = attackerArmyId,
                ParentFormationId = string.Empty,
                DisplayName = "洛阳攻方军本阵",
                Kind = MilitaryFormationKind.Army,
                CommanderPersonId = attackerCommanderPersonId,
                AuthorizedStrength = 1,
                DisplayOrder = 0
            });
            world.MilitaryServices.Add(new MilitaryServiceState
            {
                Id = "military_service.luoyang.passage_guard_commander",
                PersonId = guardCommanderPersonId,
                ArmyId = guardArmyId,
                FormationId = "formation.luoyang.passage_guard.root",
                Role = MilitaryServiceRole.Commander,
                Rank = 10,
                Status = MilitaryServiceStatus.Active,
                EnlistedDay = 0,
                LastStatusChangeDay = 0
            });
            world.MilitaryServices.Add(new MilitaryServiceState
            {
                Id = "military_service.luoyang.passage_attacker_commander",
                PersonId = attackerCommanderPersonId,
                ArmyId = attackerArmyId,
                FormationId = "formation.luoyang.passage_attacker.root",
                Role = MilitaryServiceRole.Commander,
                Rank = 10,
                Status = MilitaryServiceStatus.Active,
                EnlistedDay = 0,
                LastStatusChangeDay = 0
            });
            world.MilitaryServiceInitialized = true;
            new PropertyConstructionSystem().GrantOpeningProperty(world,
                cellId64, locationId, guardOrganizationId,
                guardOrganizationId);
            world.FacilityDefinitions.Add(new FacilityDefinitionState
            {
                Id = passage.FacilityDefinitionId,
                DisplayName = "洛阳关隘测试定义",
                CategoryId = "facility.category.fortification"
            });
            world.Facilities.Add(new FacilityState
            {
                Id = passage.FacilityId,
                DisplayName = "洛阳关隘测试设施",
                DefinitionId = passage.FacilityDefinitionId,
                CellId64 = cellId64,
                OwnerId = guardOrganizationId,
                ControllerId = guardOrganizationId,
                AdministrativeControllerId = guardOrganizationId,
                SettlementId = locationId,
                HistoricalConfidence =
                    HistoricalConfidenceLevel.GameplayReconstruction,
                SpatialPrecision = HistoricalSpatialPrecision.Confirmed,
                SourceNote = "Deterministic passage operations fixture."
            });
            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = inventoryContainerId,
                KindId = "inventory_container.military_construction_store",
                OwnerOrganizationId = guardOrganizationId,
                LocationId = locationId,
                CapacityWeight = 1_000
            });
            AddPassageRepairOpeningBatch(world,
                "product_batch.luoyang.passage_repair.timber",
                CoreProductionContent.TimberMaterialProductId, 20,
                guardOrganizationId, inventoryContainerId, locationId,
                guardCommanderPersonId);
            AddPassageRepairOpeningBatch(world,
                "product_batch.luoyang.passage_repair.iron",
                CoreProductionContent.IronMaterialProductId, 5,
                guardOrganizationId, inventoryContainerId, locationId,
                guardCommanderPersonId);
            world.Battles.Add(new BattleRecordState
            {
                Id = battleId,
                Day = 0,
                LocationId = locationId,
                AttackerArmyId = attackerArmyId,
                DefenderArmyId = guardArmyId,
                AttackerInitialTroops = 1,
                DefenderInitialTroops = 1,
                AttackerCasualties = 0,
                DefenderCasualties = 0,
                AttackerWounded = 0,
                DefenderWounded = 0,
                AttackerEquipmentReadinessBasisPoints = 10_000,
                DefenderEquipmentReadinessBasisPoints = 10_000,
                Result = BattleResultType.Stalemate,
                WinnerArmyId = string.Empty,
                Summary = "关隘战损权威测试战斗。"
            });
            world.Validate();
            return new LuoyangPassageOperationsFixture(world, runtime, system,
                passage.FacilityId, guardArmyId, guardCommanderPersonId,
                attackerCommanderPersonId, inventoryContainerId, battleId);
        }

        private static void AddPassageRepairOpeningBatch(
            WorldState world,
            string batchId,
            string productDefinitionId,
            long quantity,
            string ownerOrganizationId,
            string inventoryContainerId,
            string locationId,
            string actorPersonId)
        {
            var product = ProductionContentRegistry.CreateCore().GetProduct(
                productDefinitionId);
            var transactionId = "inventory_transaction." + batchId +
                ".opening";
            var batch = new ProductBatchState
            {
                Id = batchId,
                ProductDefinitionId = product.Id,
                OwnerOrganizationId = ownerOrganizationId,
                InventoryContainerId = inventoryContainerId,
                OriginLocationId = locationId,
                SourceTransactionId = transactionId,
                UnitId = product.UnitId,
                UnitWeight = product.BaseWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = quantity,
                QualityBasisPoints = 8_500,
                FreshnessBasisPoints = 9_500,
                QualityDimensions = ProductQualityRules.CreateUniform(
                    product, 8_500)
            };
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = transactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = actorPersonId,
                Summary = "Passage repair test opening balance.",
                Lines =
                {
                    new InventoryTransactionLineState
                    {
                        BatchId = batch.Id,
                        ProductDefinitionId = batch.ProductDefinitionId,
                        OwnerOrganizationId = batch.OwnerOrganizationId,
                        InventoryContainerId = batch.InventoryContainerId,
                        UnitId = batch.UnitId,
                        QuantityDelta = quantity
                    }
                }
            });
        }

        private class FormalMovementFixture
        {
            public FormalMovementFixture(WorldState world,
                LuoyangRoadTraversalRefinementPlan plan,
                WorldCommandRuntime runtime,
                LuoyangPassageWorldCommandSystem passageSystem,
                LuoyangFormalPlayerMovementSystem system,
                LuoyangFormalPlayerMovementService service)
            {
                World = world;
                Plan = plan;
                Runtime = runtime;
                PassageSystem = passageSystem;
                System = system;
                Service = service;
            }

            public WorldState World { get; }
            public LuoyangRoadTraversalRefinementPlan Plan { get; }
            public WorldCommandRuntime Runtime { get; }
            public LuoyangPassageWorldCommandSystem PassageSystem { get; }
            public LuoyangFormalPlayerMovementSystem System { get; }
            public LuoyangFormalPlayerMovementService Service { get; }
        }

        private sealed class PassageMovementFixture : FormalMovementFixture
        {
            public PassageMovementFixture(FormalMovementFixture source,
                string passageFacilityId, string targetFacilityId)
                : base(source.World, source.Plan, source.Runtime,
                    source.PassageSystem, source.System, source.Service)
            {
                PassageFacilityId = passageFacilityId;
                TargetFacilityId = targetFacilityId;
            }

            public string PassageFacilityId { get; }
            public string TargetFacilityId { get; }
        }

        private sealed class LuoyangPassageOperationsFixture
        {
            public LuoyangPassageOperationsFixture(
                WorldState world,
                WorldCommandRuntime runtime,
                LuoyangPassageWorldCommandSystem system,
                string facilityId,
                string guardArmyId,
                string guardCommanderPersonId,
                string attackerCommanderPersonId,
                string inventoryContainerId,
                string battleId)
            {
                World = world;
                Runtime = runtime;
                System = system;
                FacilityId = facilityId;
                GuardArmyId = guardArmyId;
                GuardCommanderPersonId = guardCommanderPersonId;
                AttackerCommanderPersonId = attackerCommanderPersonId;
                InventoryContainerId = inventoryContainerId;
                BattleId = battleId;
            }

            public WorldState World { get; }
            public WorldCommandRuntime Runtime { get; }
            public LuoyangPassageWorldCommandSystem System { get; }
            public string FacilityId { get; }
            public string GuardArmyId { get; }
            public string GuardCommanderPersonId { get; }
            public string AttackerCommanderPersonId { get; }
            public string InventoryContainerId { get; }
            public string BattleId { get; }
        }
    }
}
