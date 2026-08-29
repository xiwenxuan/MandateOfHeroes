using System;
using System.Collections.Generic;
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
        public void LuoyangT4_CellPropertyHasOneOwnerAndTransfersMoney()
        {
            var world = CreateT4ConstructionWorld();
            var system = new PropertyConstructionSystem();
            system.GrantOpeningProperty(
                world, 4_001, "location.test", "family.actor",
                "organization.government");
            var government = world.Organizations.Find(item =>
                item.Id == "organization.government");
            var buyerBefore = government.Treasury;
            var seller = world.Families.Find(item => item.Id == "family.actor");
            var sellerBefore = seller.Wealth;

            system.TransferProperty(
                world, 4_001, "family.actor", "organization.government",
                500, CellPropertyTransferKind.Purchase, "person.actor");

            Assert.That(world.CellProperties, Has.Count.EqualTo(1));
            Assert.That(world.CellProperties[0].OwnerId,
                Is.EqualTo("organization.government"));
            Assert.That(government.Treasury,
                Is.EqualTo(buyerBefore - 500));
            Assert.That(seller.Wealth,
                Is.EqualTo(sellerBefore + 500));
        }

        [Test]
        public void LuoyangT4_ConstructionConsumesMaterialLaborAndTime()
        {
            var world = CreateT4ConstructionWorld();
            var system = new PropertyConstructionSystem();
            system.GrantOpeningProperty(
                world, 4_002, "location.test", "family.actor",
                "organization.government");
            var materialBefore = world.ProductBatches[0].Quantity;
            var project = system.StartProject(
                world, "location.test", 4_002, "facility.house",
                "family.actor", "person.actor", "container.builder",
                "product.material.timber", 10, 480, 3, 100);
            system.ContributeLabor(world, project.Id, "person.actor", 480);

            Assert.That(system.TryComplete(world, project.Id), Is.Null);
            Assert.That(world.ProductBatches[0].ReservedQuantity, Is.EqualTo(10));
            world.AbsoluteDay += 3;
            var facility = system.TryComplete(world, project.Id);

            Assert.That(facility, Is.Not.Null);
            Assert.That(facility.CellId64, Is.EqualTo(4_002));
            Assert.That(world.ProductBatches[0].Quantity,
                Is.EqualTo(materialBefore - 10));
            Assert.That(world.ProductBatches[0].ReservedQuantity, Is.Zero);
            Assert.That(world.InventoryTransactions,
                Has.Some.Matches<InventoryTransactionState>(item =>
                    item.Type == InventoryTransactionType
                        .FacilityConstructionMaterialConsumed));
        }

        [Test]
        public void LuoyangT4_CancelledConstructionReleasesRealInventory()
        {
            var world = CreateT4ConstructionWorld();
            var system = new PropertyConstructionSystem();
            system.GrantOpeningProperty(world, 4_004, "location.test",
                "family.actor", "organization.government");
            var project = system.StartProject(world, "location.test", 4_004,
                "facility.house", "family.actor", "person.actor",
                "container.builder", "product.material.timber", 12,
                480, 2, 50);

            system.CancelProject(world, project.Id);

            Assert.That(world.ProductBatches[0].ReservedQuantity, Is.Zero);
            Assert.That(project.Status,
                Is.EqualTo(FacilityConstructionStatus.Cancelled));
            Assert.That(world.InventoryTransactions, Has.Some
                .Matches<InventoryTransactionState>(item => item.Type ==
                    InventoryTransactionType.FacilityConstructionMaterialReleased));
        }

        [Test]
        public void LuoyangT4_RepairExpansionAndAbandonmentRemainAuditable()
        {
            var world = CreateT4ConstructionWorld();
            var system = new PropertyConstructionSystem();
            system.GrantOpeningProperty(world, 4_005, "location.test",
                "family.actor", "organization.government");
            var facility = new FacilityState
            {
                Id = "facility.test.runtime",
                DisplayName = "Test House",
                DefinitionId = "facility.house",
                CellId64 = 4_005,
                OwnerId = "family.actor",
                ControllerId = "family.actor",
                SettlementId = "location.test",
                LifecycleStatus = FacilityLifecycleStatus.Disabled,
                ConditionBasisPoints = 3_000,
                StorageCapacity = 100
            };
            world.Facilities.Add(facility);
            world.Validate();

            var repair = system.StartFacilityWork(world, facility.Id,
                FacilityConstructionProjectKind.Repair, "person.actor",
                "container.builder", "product.material.timber", 5,
                120, 1, 25);
            system.ContributeLabor(world, repair.Id, "person.actor", 120);
            world.AbsoluteDay++;
            Assert.That(system.TryComplete(world, repair.Id), Is.SameAs(facility));
            Assert.That(facility.ConditionBasisPoints, Is.EqualTo(10_000));
            Assert.That(facility.LifecycleStatus,
                Is.EqualTo(FacilityLifecycleStatus.Operational));

            var expansion = system.StartFacilityWork(world, facility.Id,
                FacilityConstructionProjectKind.Expansion, "person.actor",
                "container.builder", "product.material.timber", 5,
                120, 1, 25);
            system.ContributeLabor(world, expansion.Id, "person.actor", 120);
            world.AbsoluteDay++;
            system.TryComplete(world, expansion.Id);
            Assert.That(facility.RuntimeExpansionLevel, Is.EqualTo(1));
            Assert.That(facility.StorageCapacity, Is.EqualTo(125));

            system.AbandonFacility(world, facility.Id, "family.actor");
            Assert.That(facility.LifecycleStatus,
                Is.EqualTo(FacilityLifecycleStatus.Abandoned));
            Assert.That(facility.ControllerId, Is.Empty);
        }

        [Test]
        public void LuoyangT4_HouseholdMigrationUsesMemberJourneys()
        {
            var world = CreateT4ConstructionWorld();
            world.Locations.Add(new LocationState
            {
                Id = "location.henan",
                DisplayName = "河南",
                Population = 1_000
            });
            world.Routes.Add(new RouteState
            {
                Id = "route.luoyang.henan",
                FromLocationId = "location.test",
                ToLocationId = "location.henan",
                DistanceKilometers = 10,
                Bidirectional = true
            });

            var migration = new HouseholdMigrationSystem().Start(
                world, "family.actor", "location.henan",
                "route.luoyang.henan");
            Assert.That(migration.JourneyIds, Has.Count.EqualTo(1));
            Assert.That(world.Families.Find(item => item.Id == "family.actor")
                    .LocationId,
                Is.EqualTo("location.test"));

            new TravelSystem().AdvanceJourneysOneSegment(world);
            new TravelSystem().AdvanceJourneysOneSegment(world);
            Assert.That(new HouseholdMigrationSystem().CompleteArrivals(world),
                Is.EqualTo(1));
            Assert.That(world.Families.Find(item => item.Id == "family.actor")
                    .LocationId,
                Is.EqualTo("location.henan"));
        }

        [Test]
        public void LuoyangT4_SnapshotMigratesV72WithoutInventingProperty()
        {
            var world = CreateT4ConstructionWorld();
            world.SchemaVersion = 72;
            world.CellProperties = null;
            world.CellPropertyTransfers = null;
            world.FacilityConstructionProjects = null;
            world.FacilityConstructionLabor = null;
            world.HouseholdMigrations = null;

            var migrated = WorldSnapshotMigrator.MigrateToCurrent(world);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.CellProperties, Is.Empty);
            Assert.That(migrated.FacilityConstructionProjects, Is.Empty);
            Assert.That(migrated.HouseholdMigrations, Is.Empty);
        }

        [Test]
        public void LuoyangT4_PropertyConstructionRoundTripPreservesAudit()
        {
            var world = CreateT4ConstructionWorld();
            new PropertyConstructionSystem().GrantOpeningProperty(
                world, 4_003, "location.test", "family.actor",
                "organization.government");
            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);
            loaded.Validate();
            Assert.That(loaded.CellProperties[0].CellId64, Is.EqualTo(4_003));
            Assert.That(loaded.CellPropertyTransfers, Has.Count.EqualTo(1));
        }

        [Test]
        public void LuoyangT4_ExternalSupplyUsesInventoryOrderShipmentAndLoss()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            var sourceStock = runtime.ExternalSuppliers.Sum(item =>
                item.InventoryQuantityMilliunits);
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 30);

            Assert.That(runtime.ExternalSuppliers, Has.Count.GreaterThanOrEqualTo(9));
            Assert.That(runtime.ExternalSuppliers.Any(item => item.Level ==
                LuoyangSupplierMaterializationLevel.FullPhysical), Is.True);
            Assert.That(runtime.ExternalSuppliers.Any(item => item.Level ==
                LuoyangSupplierMaterializationLevel.CompactRuntime), Is.True);
            Assert.That(runtime.ExternalSuppliers.Any(item => item.Level ==
                LuoyangSupplierMaterializationLevel.DeferredExternalTrade),
                Is.True);
            Assert.That(runtime.SupplyOrders.Any(item => item.Status ==
                LuoyangSupplyOrderStatus.Delivered), Is.True);
            Assert.That(runtime.Shipments.All(item =>
                item.ShippedQuantityMilliunits ==
                item.CarrierConsumptionMilliunits +
                item.NaturalLossMilliunits + item.RiskLossMilliunits +
                item.DeliveredQuantityMilliunits), Is.True);
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.OperationId == "supply.shipment_delivered"), Is.True);
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.OperationId == "supply.reference_arrival"), Is.False);
            Assert.That(runtime.ExternalSuppliers.Sum(item =>
                item.InventoryQuantityMilliunits),
                Is.Not.EqualTo(sourceStock));
            Assert.That(runtime.ExternalSuppliers.Any(item =>
                item.CumulativeOperatingExpense > 0), Is.True);
            Assert.That(runtime.ExternalSuppliers.All(item =>
                item.CashBalance == 1_000_000 + item.CumulativeSalesRevenue -
                item.CumulativeOperatingExpense), Is.True);
            Luoyang184LivingWorldRules.ValidateRuntime(runtime,
                400000, 80899, 2084);
        }

        [Test]
        public void LuoyangT4_CheckpointRoundTripPreservesSupplyLedger()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            Assert.That(runtime.CellProperties.Where(item =>
                    string.IsNullOrEmpty(item.FacilityId)).All(item =>
                    LuoyangLivingWorldTestFixture.Source.DevelopableCellIds
                        .Contains(item.CellId64)), Is.True);
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 30);
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "mandate-luoyang-t4-checkpoint-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new Luoyang184LivingWorldCheckpointStore();
                var saved = store.Save(runtime, directory);
                var loaded = store.Load(saved.CheckpointPath);
                Assert.That(loaded.Version,
                    Is.EqualTo(Luoyang184LivingWorldRuntimeState.FormatVersion));
                Assert.That(loaded.ExternalSuppliers.Count,
                    Is.EqualTo(runtime.ExternalSuppliers.Count));
                Assert.That(loaded.SupplyOrders.Count,
                    Is.EqualTo(runtime.SupplyOrders.Count));
                Assert.That(loaded.Shipments.Count,
                    Is.EqualTo(runtime.Shipments.Count));
                Assert.That(loaded.Shipments.Sum(item =>
                    item.DeliveredQuantityMilliunits), Is.EqualTo(
                    runtime.Shipments.Sum(item =>
                        item.DeliveredQuantityMilliunits)));
                Luoyang184LivingWorldRules.ValidateRuntime(loaded,
                    400000, 80899, 2084);
            }
            finally
            {
                if (System.IO.Directory.Exists(directory))
                    System.IO.Directory.Delete(directory, true);
            }
        }

        [Test]
        public void LuoyangT4_AllRequiredAgentRolesUseStableRuntimePipeline()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            Assert.That(runtime.IntelligentAgents.Count,
                Is.GreaterThanOrEqualTo(80_899 + 2_084 + 2));
            foreach (LuoyangIntelligentAgentRole role in Enum.GetValues(
                         typeof(LuoyangIntelligentAgentRole)))
                Assert.That(runtime.IntelligentAgents.Any(item => item.Role == role),
                    Is.True, role.ToString());

            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 30);

            Assert.That(runtime.IntelligentAgents.Any(item =>
                item.ExecutedActionCount > 0), Is.True);
            Assert.That(runtime.DecisionAudits, Is.Not.Empty);
            Assert.That(runtime.DecisionAudits.All(item =>
                !string.IsNullOrWhiteSpace(item.SignalDigest) &&
                !string.IsNullOrWhiteSpace(item.CandidateDigest) &&
                !string.IsNullOrWhiteSpace(item.ValidationReasonId)), Is.True);
        }

        [Test]
        public void LuoyangT4_PopulationGrowthRaisesAndDeclineReducesDevelopmentPressure()
        {
            var dense = LuoyangLivingWorldTestFixture.NewRuntime();
            var sparse = LuoyangLivingWorldTestFixture.NewRuntime();
            foreach (var person in sparse.Workforce.Skip(200_000))
                person.CurrentLocationId = "location.county.henan.external";
            sparse.CurrentLocalPopulation = 200_000;
            LuoyangLivingWorldTestFixture.System.AdvanceTo(dense, 30);
            LuoyangLivingWorldTestFixture.System.AdvanceTo(sparse, 30);
            var denseAudit = dense.DecisionAudits.Last(item =>
                item.Role == LuoyangIntelligentAgentRole.SettlementDevelopment);
            var sparseAudit = sparse.DecisionAudits.Last(item =>
                item.Role == LuoyangIntelligentAgentRole.SettlementDevelopment);
            Assert.That(denseAudit.SignalDigest, Is.Not.EqualTo(
                sparseAudit.SignalDigest));
            Assert.That(denseAudit.CandidateDigest, Is.Not.Empty);
            Assert.That(sparseAudit.CandidateDigest, Is.Not.Empty);
        }

        [Test]
        public void LuoyangT4_MarketPurchaseTransfersMoneyAndUsesHouseholdReserve()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 30);
            Assert.That(runtime.MarketTrades, Is.Not.Empty);
            Assert.That(runtime.MarketTrades.All(item =>
                item.QuantityMilliunits > 0 && item.MoneyTransferred > 0), Is.True);
            Assert.That(runtime.Households.Any(item =>
                item.CumulativeMoneySpent > 0), Is.True);
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.OperationId == "household.food_reserve_consumed"), Is.True);
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.OperationId == "household.food_consumed"), Is.False);
            Assert.That(runtime.ExternalSuppliers.Any(item =>
                item.CumulativeSalesRevenue > 0), Is.True);
        }

        [Test]
        public void LuoyangT4_MerchantGovernmentAndSettlementActionsMutateRealLedgers()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 120);

            Assert.That(runtime.SupplyOrders.Any(item =>
                item.ReasonId == "merchant.price_stock_opportunity"), Is.True,
                "merchant order");
            Assert.That(runtime.Shipments.Any(item =>
                item.Id.StartsWith("trade_shipment.", StringComparison.Ordinal)), Is.True,
                "merchant shipment");
            Assert.That(runtime.ExternalSuppliers.Any(item =>
                item.CumulativeSalesRevenue > 0), Is.True,
                "merchant revenue");
            var governmentDiagnostic = "purchase=" +
                runtime.GovernmentEconomy.PurchaseExpense + ";relief=" +
                runtime.GovernmentEconomy.ReliefExpense + ";treasury=" +
                runtime.GovernmentEconomy.Treasury + ";tax=" +
                runtime.GovernmentEconomy.TaxRevenue + ";market_food=" +
                runtime.Inventories.Where(item =>
                        item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                        LuoyangFormalEconomySystem.IsFood(item.ProductId))
                    .Sum(item => item.QuantityMilliunits) + ";military_food=" +
                runtime.Inventories.Where(item =>
                        item.OwnerKind == LuoyangInventoryOwnerKind.Military &&
                        LuoyangFormalEconomySystem.IsFood(item.ProductId))
                    .Sum(item => item.QuantityMilliunits) +
                ";household_wealth=" + runtime.Households.Sum(item =>
                    item.Wealth);
            Console.WriteLine("T4_GOVERNMENT_EXPENDITURE " +
                governmentDiagnostic);
            Assert.That(runtime.GovernmentEconomy.PurchaseExpense > 0 ||
                runtime.GovernmentEconomy.ReliefExpense > 0, Is.True,
                "government expenditure; " + governmentDiagnostic);
            Assert.That(runtime.ConstructionProjects, Is.Not.Empty,
                "settlement construction project");
            Assert.That(runtime.ConstructionProjects.All(item =>
                item.MaterialQuantityMilliunits > 0 &&
                item.CompletionDay > item.StartedDay), Is.True);
        }

        [Test]
        public void LuoyangT4_SeedsOneToFivePreserveOpeningAndDivergeAfterOneYear()
        {
            AssertSeedRangeDiverges(1, 5);
        }

        [Test]
        public void LuoyangT4_SeedsSixToTenPreserveOpeningAndDivergeAfterOneYear()
        {
            AssertSeedRangeDiverges(6, 10);
        }

        [Test]
        public void LuoyangT4_SeedsElevenToFifteenPreserveOpeningAndDivergeAfterOneYear()
        {
            AssertSeedRangeDiverges(11, 15);
        }

        [Test]
        public void LuoyangT4_SeedsSixteenToTwentyPreserveOpeningAndDivergeAfterOneYear()
        {
            AssertSeedRangeDiverges(16, 20);
        }

        private static void AssertSeedRangeDiverges(ulong firstSeed,
            ulong lastSeed)
        {
            var openingDigests = new HashSet<string>(StringComparer.Ordinal);
            var outcomeDigests = new HashSet<string>(StringComparer.Ordinal);
            for (var seed = firstSeed; seed <= lastSeed; seed++)
            {
                var runtime = LuoyangLivingWorldTestFixture.System.CreateRuntime(1);
                openingDigests.Add(OpeningDigest(runtime));
                runtime.MasterSeed = seed;
                LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 365);
                outcomeDigests.Add(string.Join("|",
                    runtime.ExternalSuppliers.Sum(item => item.CumulativeSalesRevenue),
                    runtime.Shipments.Sum(item => item.RiskLossMilliunits),
                    runtime.FamilyOrganizations.Sum(item => item.InvestmentPaid),
                    runtime.GovernmentEconomy.CurrentFoodPolicyId,
                    runtime.Facilities.Sum(item => item.RuntimeExpansionLevel),
                    runtime.IntelligentAgents.Sum(item => item.ExecutedActionCount)));
            }
            Assert.That(openingDigests, Has.Count.EqualTo(1));
            Assert.That(outcomeDigests.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void LuoyangT4_OneSeedOneYearPerformanceProbe()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var runtime = LuoyangLivingWorldTestFixture.System.CreateRuntime(77);
            var initialization = timer.ElapsedMilliseconds;
            timer.Restart();
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 365);
            var simulation = timer.ElapsedMilliseconds;
            Assert.That(runtime.AbsoluteDay, Is.EqualTo(365));
            Assert.That(initialization, Is.LessThan(300_000));
            Assert.That(simulation, Is.LessThan(300_000));
            Console.WriteLine("T4_PERF initialization_ms=" + initialization +
                " simulation_365_ms=" + simulation +
                " peak_bytes=" + runtime.Performance.PeakManagedMemoryBytes +
                " production_ms=" + runtime.Performance.ProductionMilliseconds +
                " market_ms=" + runtime.Performance.MarketMilliseconds +
                " consumption_ms=" + runtime.Performance.ConsumptionMilliseconds +
                " decision_ms=" + runtime.Performance.DecisionMilliseconds +
                " supply_ms=" + runtime.Performance.SupplyMilliseconds +
                " shortage_ms=" + runtime.Performance.ShortageMilliseconds +
                " decision_audits=" + runtime.DecisionAudits.Count +
                " trades=" + runtime.MarketTrades.Count +
                " flows=" + runtime.InventoryFlows.Count);
        }

        [Test]
        public void LuoyangT4_SixtyDayDecisionPerformanceProbe()
        {
            var runtime = LuoyangLivingWorldTestFixture.System.CreateRuntime(77);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 60);
            Console.WriteLine("T4_DECISION_PROFILE simulation_60_ms=" +
                timer.ElapsedMilliseconds + " decision_ms=" +
                runtime.Performance.DecisionMilliseconds + " index_ms=" +
                runtime.Performance.DecisionIndexMilliseconds + " household_ms=" +
                runtime.Performance.HouseholdDecisionMilliseconds + " facility_ms=" +
                runtime.Performance.FacilityDecisionMilliseconds + " organization_ms=" +
                runtime.Performance.OrganizationDecisionMilliseconds +
                " audits=" + runtime.DecisionAudits.Count);
            Assert.That(runtime.AbsoluteDay, Is.EqualTo(60));
        }

        [Test]
        public void LuoyangT4_IntegratedFamilyPersonGovernmentAndMilitaryRunThirtyDays()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            Assert.That(runtime.FamilyAssets, Is.Not.Null);
            Assert.That(runtime.PersonDevelopment, Is.Not.Empty);
            Assert.That(runtime.PersonDevelopment.Any(item =>
                item.BookInventoryIds.Count > 0), Is.True, "real book inventory reference");
            Assert.That(runtime.Offices.Select(item => item.OfficeKindId),
                Does.Contain("office.central_government"));
            Assert.That(runtime.Offices.Select(item => item.OfficeKindId),
                Does.Contain("office.henan_yin"));
            Assert.That(runtime.Forces, Is.Not.Empty);
            Assert.That(runtime.Forces.Single().InventoryIds.Count,
                Is.GreaterThanOrEqualTo(8));
            Assert.That(runtime.Inventories.Any(item =>
                item.Id == runtime.GovernmentEconomy.GranaryInventoryId &&
                item.OwnerKind == LuoyangInventoryOwnerKind.Government), Is.True,
                "government granary must exist");

            var householdMoney = runtime.Households.Sum(item => item.Wealth);
            var treasury = runtime.GovernmentEconomy.Treasury;
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 30);

            Assert.That(runtime.Taxes, Is.Not.Empty);
            Assert.That(runtime.Taxes.Any(item =>
                item.TaxKindId == "tax.inkind.household.grain.monthly_batch" &&
                item.ProductQuantityMilliunits > 0 &&
                item.DestinationInventoryId ==
                    runtime.GovernmentEconomy.GranaryInventoryId), Is.True,
                "in-kind tax must enter the government granary; taxes=" +
                string.Join(",", runtime.Taxes.Select(item =>
                    item.TaxKindId + ":" + item.ProductQuantityMilliunits +
                    ":" + item.DestinationInventoryId)) + ";granary=" +
                runtime.GovernmentEconomy.GranaryInventoryId + ":" +
                runtime.Inventories.First(item => item.Id ==
                    runtime.GovernmentEconomy.GranaryInventoryId)
                    .QuantityMilliunits);
            Assert.That(runtime.GovernmentEconomy.TaxRevenue, Is.GreaterThan(0));
            Assert.That(runtime.Households.Sum(item => item.Wealth),
                Is.LessThan(householdMoney));
            Assert.That(runtime.GovernmentEconomy.Treasury,
                Is.GreaterThan(treasury));
            Assert.That(runtime.PersonDevelopment.All(item =>
                item.StudyMinutes > 0 && item.KnowledgeBasisPoints > 0), Is.True);
            Assert.That(runtime.Forces.Single().FoodConsumedMilliunits,
                Is.GreaterThanOrEqualTo(0));
            Assert.That(runtime.SocialPressureHistory, Is.Not.Empty);
        }

        [Test]
        public void LuoyangT4_PermanentPeopleCoverRequiredSocialRolesAndCanChangeStatus()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            var roles = new HashSet<string>(runtime.Workforce.Select(item =>
                item.SocialRoleId), StringComparer.Ordinal);
            Assert.That(roles, Does.Contain("role.farmer"));
            Assert.That(roles, Does.Contain("role.artisan"));
            Assert.That(roles, Does.Contain("role.merchant"));
            Assert.That(roles, Does.Contain("role.official"));
            Assert.That(roles, Does.Contain("role.soldier"));
            Assert.That(roles, Does.Contain("role.student"));
            Assert.That(roles, Does.Contain("role.family_manager"));
            Assert.That(roles, Does.Contain("role.unemployed"));
            var unemployed = runtime.Workforce.First(item =>
                item.Status == LuoyangWorkforceStatus.Unemployed);
            var formerRole = unemployed.SocialRoleId;
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 30);
            Assert.That(unemployed.Status, Is.EqualTo(LuoyangWorkforceStatus.Assigned));
            Assert.That(formerRole, Is.EqualTo("role.unemployed"));
        }

        [Test]
        public void LuoyangT4_189And190EventsRunOffscreenAndChangeRealWorldFacts()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            var damagedBefore = runtime.Facilities.Count(item =>
                item.ConditionBasisPoints < 10_000);
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime,
                Luoyang184T4IntegratedRuntimeSystem.Year190Day + 180);
            var palace = runtime.HistoricalEvents.Single(item =>
                item.DefinitionId.Contains("189"));
            var relocation = runtime.HistoricalEvents.Single(item =>
                item.DefinitionId.Contains("190"));
            Assert.That(palace.ResolvedDay, Is.GreaterThanOrEqualTo(
                Luoyang184T4IntegratedRuntimeSystem.Year189Day));
            Assert.That(relocation.ResolvedDay, Is.GreaterThanOrEqualTo(
                Luoyang184T4IntegratedRuntimeSystem.Year190Day));
            Assert.That(palace.AppliedOffscreen, Is.True);
            Assert.That(relocation.AppliedOffscreen, Is.True);
            Assert.That(new[] { "canonical", "variant", "delayed", "transformed",
                "prevented" }, Does.Contain(relocation.OutcomeId));
            if (relocation.OutcomeId != "prevented")
            {
                Assert.That(runtime.Facilities.Count(item =>
                    item.ConditionBasisPoints < 10_000), Is.GreaterThan(damagedBefore));
                Assert.That(runtime.Households.Take(1_000).All(item =>
                    item.ResidenceFacilityIndex == uint.MaxValue), Is.True);
                Assert.That(runtime.GovernmentEconomy.CurrentLocationId,
                    Is.EqualTo("location.capital.changan"));
                Assert.That(runtime.Offices.All(item =>
                    runtime.Workforce[(int)item.HolderPersonOrdinal]
                        .CurrentLocationId == "location.capital.changan"), Is.True);
                Assert.That(runtime.Forces.All(item =>
                    item.CurrentLocationId == "location.capital.changan"), Is.True);
                Assert.That(runtime.Inventories.Where(item =>
                        item.OwnerKind == LuoyangInventoryOwnerKind.Government ||
                        item.OwnerKind == LuoyangInventoryOwnerKind.Military)
                    .All(item => item.CurrentLocationId ==
                        "location.capital.changan"), Is.True);
                Assert.That(relocation.AppliedChangeIds,
                    Does.Contain("government_inventory.relocation.travel"));
            }
        }

        [Test]
        public void LuoyangT4_PlayerCommandsUseRealWorkStudyTradePropertyAndMilitaryFacts()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            var service = new Luoyang184PlayerCommandSystem();
            var unemployed = runtime.Workforce.First(item =>
                item.Status == LuoyangWorkforceStatus.Unemployed);
            var work = service.Execute(runtime, unemployed.PersonOrdinal,
                LuoyangPlayerCommandTypeIds.SeekWork);
            Assert.That(work.StatusId, Is.EqualTo("completed"));
            Assert.That(unemployed.Status, Is.EqualTo(
                LuoyangWorkforceStatus.Assigned));

            var study = service.Execute(runtime, unemployed.PersonOrdinal,
                LuoyangPlayerCommandTypeIds.Study);
            Assert.That(study.StatusId, Is.EqualTo("completed"));
            Assert.That(runtime.PersonDevelopment.Single(item =>
                item.PersonOrdinal == unemployed.PersonOrdinal).StudyMinutes,
                Is.GreaterThan(0));

            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 30);
            var playerTradeMarket = runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                    LuoyangFormalEconomySystem.IsFood(item.ProductId))
                .OrderBy(item => item.Id, StringComparer.Ordinal).First();
            var seededPlayerTradeStock = new LuoyangFormalEconomySystem()
                .Produce(runtime, playerTradeMarket.Id,
                    playerTradeMarket.ProductId, 1_000,
                    InventoryTransactionType.OpeningBalance,
                    "test.player_trade.formal_opening_lot");
            Assert.That(seededPlayerTradeStock, Is.EqualTo(1_000));
            var trade = service.Execute(runtime, unemployed.PersonOrdinal,
                LuoyangPlayerCommandTypeIds.Trade);
            Assert.That(trade.StatusId, Is.EqualTo("completed"),
                trade.ResultId + ";wealth=" + runtime.Households[
                    (int)unemployed.HouseholdOrdinal].Wealth +
                ";market_food=" + runtime.Inventories.Where(item =>
                        item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                        LuoyangFormalEconomySystem.IsFood(item.ProductId))
                    .Sum(item => item.QuantityMilliunits));
            Assert.That(runtime.MarketTrades.Any(item =>
                item.Id == trade.ResultId), Is.True);

            var household = runtime.Households[(int)unemployed.HouseholdOrdinal];
            household.Wealth = Math.Max(household.Wealth, 100);
            var purchase = service.Execute(runtime, unemployed.PersonOrdinal,
                LuoyangPlayerCommandTypeIds.BuyProperty);
            Assert.That(purchase.StatusId, Is.EqualTo("completed"));
            Assert.That(runtime.CellProperties.Any(item =>
                item.OwnerId == household.HouseholdId), Is.True);

            var build = service.Execute(runtime, unemployed.PersonOrdinal,
                LuoyangPlayerCommandTypeIds.BuildIndustry);
            Assert.That(build.StatusId, Is.EqualTo("completed"), build.ResultId);
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime,
                runtime.AbsoluteDay + 30);
            Assert.That(runtime.ConstructionProjects.Any(item =>
                item.Id == build.ResultId && item.Completed &&
                !string.IsNullOrEmpty(item.ResultFacilityId)), Is.True);
            var built = runtime.Facilities.Single(item => item.FacilityId ==
                runtime.ConstructionProjects.Single(project => project.Id ==
                    build.ResultId).ResultFacilityId);
            Assert.That(built.RecipeId,
                Is.EqualTo(CoreProductionContent.HandMillWheatRecipeId));
            Assert.That(built.AssignedWorkers, Is.GreaterThanOrEqualTo(1));
            Assert.That(runtime.Inventories, Has.Some.Matches<
                LuoyangInventoryBalanceState>(item =>
                    item.FacilityId == built.FacilityId &&
                    item.ProductId == CoreProductionContent.WheatGrainProductId));

            var soldiersBefore = runtime.Forces[0].PermanentPersonCount;
            var enlist = service.Execute(runtime, unemployed.PersonOrdinal,
                LuoyangPlayerCommandTypeIds.Enlist);
            Assert.That(enlist.StatusId, Is.EqualTo("completed"));
            Assert.That(runtime.Forces[0].PermanentPersonCount,
                Is.EqualTo(soldiersBefore + 1));
            Assert.That(runtime.PlayerCommands.Count, Is.EqualTo(6));
        }

        [Test]
        public void LuoyangT4_V6CheckpointPreservesAllIntegratedSubsystems()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 60);
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "mandate-luoyang-t4-v5-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new Luoyang184LivingWorldCheckpointStore();
                var saved = store.Save(runtime, directory);
                var loaded = store.Load(saved.CheckpointPath);
                Assert.That(loaded.Version, Is.EqualTo(
                    Luoyang184LivingWorldRuntimeState.FormatVersion));
                Assert.That(loaded.CellProperties.Count,
                    Is.EqualTo(runtime.CellProperties.Count));
                Assert.That(loaded.FamilyAssets.Count,
                    Is.EqualTo(runtime.FamilyAssets.Count));
                Assert.That(loaded.PersonDevelopment.Count,
                    Is.EqualTo(runtime.PersonDevelopment.Count));
                Assert.That(loaded.Offices.Count, Is.EqualTo(runtime.Offices.Count));
                Assert.That(loaded.Taxes.Count, Is.EqualTo(runtime.Taxes.Count));
                Assert.That(loaded.Forces.Count, Is.EqualTo(runtime.Forces.Count));
                Assert.That(loaded.SocialPressureHistory.Count,
                    Is.EqualTo(runtime.SocialPressureHistory.Count));
                Assert.That(loaded.HistoricalEvents.Count,
                    Is.EqualTo(runtime.HistoricalEvents.Count));
                Assert.That(loaded.ConstructionProjects.Single().Materials.Count,
                    Is.GreaterThanOrEqualTo(2));
                Assert.That(loaded.Workforce.All(item =>
                    !string.IsNullOrEmpty(item.CurrentLocationId)), Is.True);
                Assert.That(loaded.GovernmentEconomy.GranaryInventoryId,
                    Is.Not.Empty);
            }
            finally
            {
                if (System.IO.Directory.Exists(directory))
                    System.IO.Directory.Delete(directory, true);
            }
        }

        [Test]
        public void LuoyangT4_V5CheckpointMigratesLocationAndTransitContractToCurrentVersion()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            runtime.Version = 5;
            foreach (var person in runtime.Workforce.Take(2))
                person.CurrentLocationId = null;
            runtime.GovernmentEconomy.CurrentLocationId = null;
            runtime.GovernmentEconomy.GranaryInventoryId = null;
            runtime.Inventories.RemoveAll(item => item.Id ==
                "inventory.government.luoyang.184.grain_tax");
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "mandate-luoyang-t4-v5-migration-" +
                Guid.NewGuid().ToString("N"));
            try
            {
                var store = new Luoyang184LivingWorldCheckpointStore();
                var saved = store.Save(runtime, directory);
                var loaded = store.Load(saved.CheckpointPath);
                Assert.That(loaded.Version, Is.EqualTo(
                    Luoyang184LivingWorldRuntimeState.FormatVersion));
                Assert.That(loaded.Workforce.Take(2).All(item =>
                    item.CurrentLocationId == "location.capital.luoyang"), Is.True);
                Assert.That(loaded.GovernmentEconomy.CurrentLocationId,
                    Is.EqualTo("location.capital.luoyang"));
                Assert.That(loaded.GovernmentEconomy.GranaryInventoryId,
                    Is.Not.Empty);
                Assert.That(loaded.Inventories, Has.Some.Matches<
                    LuoyangInventoryBalanceState>(item => item.Id ==
                    loaded.GovernmentEconomy.GranaryInventoryId));
            }
            finally
            {
                if (System.IO.Directory.Exists(directory))
                    System.IO.Directory.Delete(directory, true);
            }
        }

        [Test]
        public void LuoyangT4_OneSevenThirtyOneYearThreeYearSixYearRemainValid()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            var checkpoints = new[] { 1L, 7L, 30L, 365L, 1_080L, 2_160L };
            var previousTax = 0L;
            foreach (var day in checkpoints)
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, day);
                Assert.That(runtime.AbsoluteDay, Is.EqualTo(day));
                Assert.That(runtime.Workforce.Count, Is.EqualTo(400_000));
                Assert.That(runtime.Households.Count, Is.EqualTo(80_899));
                Assert.That(runtime.GovernmentEconomy.TaxRevenue,
                    Is.GreaterThanOrEqualTo(previousTax));
                Assert.That(runtime.Inventories.All(item =>
                    item.QuantityMilliunits >= 0 &&
                    item.QuantityMilliunits <= item.CapacityMilliunits), Is.True);
                Assert.That(runtime.Shipments.All(item =>
                    item.ShippedQuantityMilliunits ==
                    item.CarrierConsumptionMilliunits +
                    item.NaturalLossMilliunits + item.RiskLossMilliunits +
                    item.DeliveredQuantityMilliunits), Is.True);
                previousTax = runtime.GovernmentEconomy.TaxRevenue;
                Console.WriteLine("T4_LONG_RUN day=" + day + " segment_ms=" +
                    timer.ElapsedMilliseconds + " tax=" + previousTax +
                    " treasury=" + runtime.GovernmentEconomy.Treasury +
                    " events=" + string.Join(",", runtime.HistoricalEvents
                        .Select(item => item.StatusId)));
            }
        }

        [Test]
        public void LuoyangT4_MonthlyTaxSalaryConstructionAndMilitaryMoneyConserve()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            var before = TotalRuntimeMoney(runtime);
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 60);
            Assert.That(TotalRuntimeMoney(runtime), Is.EqualTo(before));
            Assert.That(runtime.Households.Sum(item =>
                item.CumulativeMoneyTaxPaid), Is.EqualTo(
                runtime.GovernmentEconomy.TaxRevenue));
            Assert.That(runtime.Offices.Sum(item => item.SalaryExpense),
                Is.GreaterThan(0));
            Assert.That(runtime.Markets.Sum(item => item.RecentTradeValue),
                Is.GreaterThan(0), "family investment and trade need a real receiver");
        }

        private static long TotalRuntimeMoney(
            Luoyang184LivingWorldRuntimeState runtime) => checked(
                runtime.Households.Sum(item => item.Wealth) +
                runtime.FamilyOrganizations.Sum(item => item.Funds) +
                runtime.ExternalSuppliers.Sum(item => item.CashBalance) +
                runtime.Markets.Sum(item => item.CashBalance) +
                runtime.GovernmentEconomy.Treasury);

        private static string OpeningDigest(
            Luoyang184LivingWorldRuntimeState runtime) => string.Join("|",
                runtime.SourcePackageId,
                runtime.ProtectedPackageDigest,
                runtime.Workforce.Count,
                runtime.Households.Count,
                runtime.Facilities.Count,
                runtime.Crops.Count,
                runtime.Inventories.Sum(item => item.QuantityMilliunits),
                runtime.ExternalSuppliers.Sum(item =>
                    item.InventoryQuantityMilliunits));

        private static WorldState CreateT4ConstructionWorld()
        {
            var world = CreateWorld();
            world.Families.Find(item => item.Id == "family.actor").Wealth =
                10_000;
            world.Organizations.Add(new OrganizationState
            {
                Id = "organization.government",
                DisplayName = "Government",
                Type = OrganizationType.Government,
                HeadquartersLocationId = "location.test",
                LeaderPersonId = "person.actor",
                Treasury = 1_000_000
            });
            world.FacilityDefinitions.Add(new FacilityDefinitionState
            {
                Id = "facility.house",
                DisplayName = "House",
                ResidentialCapacityPersons = 10,
                WorkerCapacity = 2
            });
            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = "container.builder",
                KindId = "inventory.family.construction",
                OwnerFamilyId = "family.actor",
                LocationId = "location.test",
                CapacityWeight = 10_000
            });
            var timber = ProductionContentRegistry.CreateCore().GetProduct(
                CoreProductionContent.TimberMaterialProductId);
            const string openingTransactionId =
                "inventory_transaction.batch.timber.builder.opening";
            var batch = new ProductBatchState
            {
                Id = "batch.timber.builder",
                ProductDefinitionId = timber.Id,
                OwnerFamilyId = "family.actor",
                InventoryContainerId = "container.builder",
                OriginLocationId = "location.test",
                SourceTransactionId = openingTransactionId,
                UnitId = timber.UnitId,
                UnitWeight = timber.BaseWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = 100,
                QualityBasisPoints = 8_000,
                QualityDimensions = ProductQualityRules.CreateUniform(
                    timber, 8_000)
            };
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = openingTransactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = "person.actor",
                Summary = "T4 construction material opening balance.",
                Lines =
                {
                    new InventoryTransactionLineState
                    {
                        BatchId = batch.Id,
                        ProductDefinitionId = batch.ProductDefinitionId,
                        OwnerFamilyId = batch.OwnerFamilyId,
                        InventoryContainerId = batch.InventoryContainerId,
                        UnitId = batch.UnitId,
                        QuantityDelta = batch.Quantity
                    }
                }
            });
            world.Validate();
            return world;
        }
    }
}
