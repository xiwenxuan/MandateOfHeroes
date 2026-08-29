using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    /// <summary>
    /// A partition-friendly Luoyang specialization over the formal Person,
    /// Facility and inventory contracts. It keeps one compact record per
    /// permanent Person and one batched settlement per Household; it never
    /// creates NPC GameObjects or a second population truth.
    /// </summary>
    public sealed class Luoyang184LivingWorldSystem
    {
        public const string StateId = "living_world.luoyang.184.v1";
        public const string ScenarioId = "scenario.han.184.yellow_turban";
        public const string SupplyDependencyId = "SUPPLY_REGION_DEPENDENCY";
        public const string TransitionalSupplyId = "TRANSITIONAL_REFERENCE_SUPPLY";

        private const long MilliunitsPerUnit = 1_000;
        private const int PersonBatchSize = 12_500;
        private const int HouseholdBatchSize = 10_000;
        private readonly ILuoyang184LivingWorldSource source;
        private readonly LuoyangNormalSupplyCalibrationProfileState
            calibrationProfile;

        private static readonly HashSet<string> FoodProducts =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "product.food.millet_grain",
                "product.food.wheat_grain",
                "product.food.broomcorn_grain",
                "product.food.bean",
                "product.reference.food_equivalent",
                CoreProductionContent.WheatGrainProductId,
                CoreProductionContent.WheatFlourProductId,
                CoreProductionContent.DryRationProductId,
                CoreProductionContent.FreshMuttonProductId,
                CoreProductionContent.OffalProductId
            };

        public Luoyang184LivingWorldSystem(
            ILuoyang184LivingWorldSource source,
            LuoyangNormalSupplyCalibrationProfileState calibrationProfile = null)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            // The repaired 700k scenario is normal-supply by default. Smaller
            // historical fixtures retain their old balance while still using the
            // same formal authority, so unrelated milestone tests are not silently
            // rebalanced.
            this.calibrationProfile = calibrationProfile ??
                (source.PersonCount >= 700_000
                    ? new LuoyangNormalSupplyCalibrationProfileState()
                    : LuoyangNormalSupplyCalibrationProfileState
                        .CreateAuthorityOnly());
        }

        public Luoyang184LivingWorldRuntimeState CreateRuntime(
            ulong masterSeed, long absoluteDay = 0)
        {
            var timer = Stopwatch.StartNew();
            var runtime = new Luoyang184LivingWorldRuntimeState
            {
                AbsoluteDay = absoluteDay,
                MasterSeed = masterSeed,
                SourcePackageId = source.PackageId,
                ProtectedPackageDigest = source.ProtectedPackageDigest
            };

            BuildWorkforce(runtime);
            BuildHouseholds(runtime);
            runtime.DailyFoodDemandMilliunits = runtime.Households.Sum(item =>
                item.DailyFoodDemandMilliunits);
            runtime.CurrentUnemployedCount = runtime.Workforce.Count(item =>
                item.Status == LuoyangWorkforceStatus.Unemployed);
            runtime.CurrentLocalPopulation = runtime.Workforce.Count;
            BuildFamilyOrganizations(runtime);
            BuildFacilityRuntime(runtime);
            BuildOpeningProperty(runtime);
            BuildOpeningInventories(runtime);
            AllocateOpeningHouseholdFood(runtime);
            BuildExternalSuppliers(runtime);
            BuildCrops(runtime);
            new Luoyang184AgricultureDueScheduler().Initialize(runtime);
            BuildMarkets(runtime);
            BuildIntelligentAgents(runtime);
            new Luoyang184T4IntegratedRuntimeSystem().Initialize(runtime);
            var formalEconomy = new LuoyangFormalEconomySystem();
            formalEconomy.ApplyCapacityCalibrationBeforeActivation(
                runtime, calibrationProfile);
            formalEconomy.ActivateFromBootstrap(runtime);
            formalEconomy.ApplyNormalSupplyOpeningReserve(
                runtime, calibrationProfile);
            runtime.Performance.InitializationMilliseconds = timer.ElapsedMilliseconds;
            runtime.Performance.PeakManagedMemoryBytes = GC.GetTotalMemory(false);
            Luoyang184LivingWorldRules.ValidateRuntime(runtime,
                source.PersonCount, source.HouseholdCount, source.FacilityCount);
            return runtime;
        }

        public void AdvanceTo(
            Luoyang184LivingWorldRuntimeState runtime, long targetDay)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (runtime.RequiresSourceRehydration)
                throw new InvalidOperationException(
                    "This legacy Luoyang checkpoint requires protected-source " +
                    "rehydration before simulation can continue: " +
                    string.Join("; ", runtime.MigrationWarnings));
            if (targetDay < runtime.AbsoluteDay)
                throw new ArgumentOutOfRangeException(nameof(targetDay));
            var days = checked((int)(targetDay - runtime.AbsoluteDay));
            if (days == 0) return;
            var startingDay = runtime.AbsoluteDay;
            var householdConsumedBefore = runtime.Households
                .Select(item => item.CumulativeFoodConsumedMilliunits).ToArray();
            var totalTimer = Stopwatch.StartNew();
            for (var offset = 0; offset < days; offset++)
            {
                runtime.AbsoluteDay++;
                var supplyTimer = Stopwatch.StartNew();
                ResolveExternalSupply(runtime);
                new Luoyang184T4IntegratedRuntimeSystem()
                    .ProcureMilitaryFood(runtime);
                runtime.Performance.SupplyMilliseconds +=
                    supplyTimer.ElapsedMilliseconds;
                var decisionTimer = Stopwatch.StartNew();
                ResolveIntelligentAgents(runtime);
                runtime.Performance.DecisionMilliseconds +=
                    decisionTimer.ElapsedMilliseconds;
                var productionTimer = Stopwatch.StartNew();
                ResolveProduction(runtime);
                runtime.Performance.ProductionMilliseconds +=
                    productionTimer.ElapsedMilliseconds;

                var marketTimer = Stopwatch.StartNew();
                BalanceFoodMarketAccess(runtime);
                UpdateMarkets(runtime);
                runtime.Performance.MarketMilliseconds += marketTimer.ElapsedMilliseconds;

                var consumptionTimer = Stopwatch.StartNew();
                ResolveHouseholdConsumption(runtime);
                runtime.Performance.ConsumptionMilliseconds +=
                    consumptionTimer.ElapsedMilliseconds;
                var shortageTimer = Stopwatch.StartNew();
                ResolveShortageResponses(runtime);
                runtime.Performance.ShortageMilliseconds +=
                    shortageTimer.ElapsedMilliseconds;
                new Luoyang184PropertyConstructionRuntimeSystem().Advance(runtime);
                new Luoyang184T4IntegratedRuntimeSystem().AdvanceDay(runtime);

                if (IsEvidenceDay(runtime.AbsoluteDay - startingDay, days))
                    CaptureSnapshot(runtime);
            }
            SettleAllHouseholdConsumption(runtime);
            ReconcilePersonConsumption(runtime, days, householdConsumedBefore);
            var elapsed = totalTimer.ElapsedMilliseconds;
            if (days == 1) runtime.Performance.OneDayMilliseconds = elapsed;
            if (days == 7) runtime.Performance.SevenDayMilliseconds = elapsed;
            if (days == 30) runtime.Performance.ThirtyDayMilliseconds = elapsed;
            if (days == 365)
                runtime.Performance.ThreeHundredSixtyFiveDayMilliseconds = elapsed;
            runtime.Performance.PeakManagedMemoryBytes = Math.Max(
                runtime.Performance.PeakManagedMemoryBytes, GC.GetTotalMemory(false));
            new LuoyangFormalEconomySystem().RebuildProjection(runtime);
            Luoyang184LivingWorldRules.ValidateRuntime(runtime,
                source.PersonCount, source.HouseholdCount, source.FacilityCount);
        }

        public bool TryHarvestAtMaturity(
            Luoyang184LivingWorldRuntimeState runtime,
            string fieldId,
            int maturityBasisPoints,
            out long harvestedMilliunits)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            var crop = runtime.Crops.Find(item => item.FieldId == fieldId) ??
                throw new InvalidOperationException("Missing crop field " + fieldId + ".");
            crop.MaturityBasisPoints = maturityBasisPoints;
            if (!Luoyang184LivingWorldRules.CanHarvest(
                    maturityBasisPoints, crop.EarlyHarvestMinimumBasisPoints))
            {
                harvestedMilliunits = 0;
                return false;
            }
            if (crop.AssignedWorkers <= 0)
            {
                harvestedMilliunits = 0;
                return false;
            }
            var harvested = Harvest(runtime, crop, out harvestedMilliunits);
            if (harvested)
                new Luoyang184AgricultureDueScheduler().Reschedule(
                    runtime, crop);
            return harvested;
        }

        public Luoyang184LivingWorldState BuildWorldSummary(
            Luoyang184LivingWorldRuntimeState runtime,
            string checkpointRelativePath = "",
            string checkpointDigest = "")
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            var eligible = runtime.Workforce.Count(item =>
                item.Status != LuoyangWorkforceStatus.NotEligible);
            var employed = runtime.Workforce.Count(item =>
                item.Status != LuoyangWorkforceStatus.NotEligible &&
                item.Status != LuoyangWorkforceStatus.Unemployed);
            var stock = TotalFoodStock(runtime);
            var localProduction = runtime.InventoryFlows.Where(item =>
                item.OperationId == "production.crop_harvest" &&
                FoodProducts.Contains(item.ProductId)).Sum(item => item.QuantityMilliunits);
            var imported = runtime.InventoryFlows.Where(item =>
                item.OperationId == "supply.shipment_delivered" &&
                FoodProducts.Contains(item.ProductId)).Sum(item => item.QuantityMilliunits);
            var consumption = runtime.Households.Sum(item =>
                item.CumulativeFoodConsumedMilliunits);
            var demand = runtime.Households.Sum(item => item.DailyFoodDemandMilliunits);
            var shortage = runtime.Households.Sum(item =>
                item.CumulativeFoodShortageMilliunits);
            var loss = runtime.InventoryFlows.Sum(item => item.LossMilliunits);
            return new Luoyang184LivingWorldState
            {
                Id = StateId,
                ScenarioId = ScenarioId,
                SourcePackageId = runtime.SourcePackageId,
                ProtectedPackageDigest = runtime.ProtectedPackageDigest,
                CheckpointRelativePath = checkpointRelativePath ?? string.Empty,
                CheckpointDigest = checkpointDigest ?? string.Empty,
                InitializedDay = 0,
                LastSimulatedDay = runtime.AbsoluteDay,
                PermanentPersonCount = runtime.Workforce.Count,
                HouseholdCount = runtime.Households.Count,
                FacilityCount = runtime.Facilities.Count,
                LaborEligibleCount = eligible,
                EmployedCount = employed,
                UnemployedCount = runtime.Workforce.Count(item =>
                    item.Status == LuoyangWorkforceStatus.Unemployed),
                MilitaryCount = runtime.Workforce.Count(item =>
                    item.Status == LuoyangWorkforceStatus.MilitaryDuty),
                OfficialCount = runtime.Workforce.Count(item =>
                    item.Status == LuoyangWorkforceStatus.Official),
                StudentCount = runtime.Workforce.Count(item =>
                    item.Status == LuoyangWorkforceStatus.Student),
                FamilyManagerCount = runtime.Workforce.Count(item =>
                    item.Status == LuoyangWorkforceStatus.FamilyManagement),
                FacilitiesWithWorkers = runtime.Facilities.Count(item =>
                    item.AssignedWorkers > 0),
                FacilitiesIdleDueWorker = runtime.Facilities.Count(item =>
                    item.Status == LuoyangProductionRuntimeStatus.WaitingWorker),
                FacilitiesIdleDueInput = runtime.Facilities.Count(item =>
                    item.Status == LuoyangProductionRuntimeStatus.WaitingInput),
                FacilitiesOutputBlocked = runtime.Facilities.Count(item =>
                    item.Status == LuoyangProductionRuntimeStatus.OutputBlocked),
                DailyFoodDemandMilliunits = demand,
                LocalFoodProductionMilliunits = localProduction,
                FoodImportMilliunits = imported,
                FoodStockMilliunits = stock,
                FoodConsumptionMilliunits = consumption,
                FoodLossMilliunits = loss,
                FoodShortageMilliunits = shortage,
                HouseholdShortageCount = runtime.Households.Count(item =>
                    item.CumulativeFoodShortageMilliunits > 0),
                SupplyRegionDependency = false,
                SupplyStatusId = runtime.ExternalSuppliers.Count > 0
                    ? "REAL_ORDER_SHIPMENT_NETWORK"
                    : SupplyDependencyId,
                DaySnapshots = runtime.DaySnapshots.Select(CopySnapshot).ToList()
            };
        }

        public void AttachSummary(
            WorldState world,
            Luoyang184LivingWorldRuntimeState runtime,
            string checkpointRelativePath = "",
            string checkpointDigest = "")
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.SchemaVersion != WorldState.CurrentSchemaVersion)
                throw new InvalidOperationException("World must be migrated before attaching living-world state.");
            if (world.PopulationStorage.PermanentPersonCount != source.PersonCount ||
                world.Facilities.Count != source.FacilityCount)
                throw new InvalidOperationException("Luoyang protected world counts changed.");
            world.LuoyangLivingWorlds.RemoveAll(item => item.Id == StateId);
            world.LuoyangLivingWorlds.Add(BuildWorldSummary(
                runtime, checkpointRelativePath, checkpointDigest));
            world.Validate();
        }

        private void BuildWorkforce(Luoyang184LivingWorldRuntimeState runtime)
        {
            runtime.Workforce.Capacity = source.PersonCount;
            for (var start = 0; start < source.PersonCount; start += PersonBatchSize)
            {
                var count = Math.Min(PersonBatchSize, source.PersonCount - start);
                foreach (var person in source.ReadPersons(start, count))
                {
                    var activity = source.GetActivityId(person.ActivityIndex);
                    var occupation = source.GetOccupationId(person.OccupationIndex);
                    var age = checked((short)Math.Max(0, 184 - person.BirthYear));
                    var status = DetermineWorkforceStatus(person, age, activity, occupation);
                    runtime.Workforce.Add(new LuoyangWorkforceAssignmentState
                    {
                        PersonOrdinal = person.Ordinal,
                        HouseholdOrdinal = person.HouseholdOrdinal,
                        FacilityIndex = person.WorkFacilityIndex,
                        OccupationIndex = person.OccupationIndex,
                        ActivityIndex = person.ActivityIndex,
                        Age = age,
                        Status = status,
                        EffectiveLaborBasisPoints = CalculateEffectiveLabor(
                            person.HealthBasisPoints, age, status)
                    });
                }
            }
            runtime.Workforce.Sort((left, right) =>
                left.PersonOrdinal.CompareTo(right.PersonOrdinal));
        }

        private void BuildHouseholds(Luoyang184LivingWorldRuntimeState runtime)
        {
            runtime.Households.Capacity = source.HouseholdCount;
            for (var start = 0; start < source.HouseholdCount;
                 start += HouseholdBatchSize)
            {
                var count = Math.Min(HouseholdBatchSize,
                    source.HouseholdCount - start);
                foreach (var household in source.ReadHouseholds(start, count))
                {
                    long demand = 0;
                    for (var ordinal = household.MemberStartOrdinal;
                         ordinal < household.MemberStartOrdinal + household.MemberCount;
                         ordinal++)
                        demand += DailyPersonFoodDemand(runtime.Workforce[
                            checked((int)ordinal)]);
                    runtime.Households.Add(new LuoyangHouseholdConsumptionState
                    {
                        HouseholdId = source.GetHouseholdId(household.Ordinal),
                        HouseholdOrdinal = household.Ordinal,
                        HeadPersonOrdinal = household.HeadOrdinal,
                        MemberStartOrdinal = household.MemberStartOrdinal,
                        MemberCount = household.MemberCount,
                        FamilyOrganizationIndex = household.FamilyOrganizationIndex,
                        ResidenceFacilityIndex = household.ResidenceFacilityIndex,
                        Wealth = household.Wealth,
                        DailyFoodDemandMilliunits = demand,
                        FoodSecurityBasisPoints = 10_000,
                        LastAcquisitionSourceId = "opening.none",
                        AiResponseActionId = "household.monitor_needs"
                    });
                }
            }
            runtime.Households.Sort((left, right) =>
                left.HouseholdOrdinal.CompareTo(right.HouseholdOrdinal));
        }

        private void BuildFamilyOrganizations(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var sourceOrganization in source.FamilyOrganizations)
            {
                runtime.FamilyOrganizations.Add(
                    new LuoyangFamilyOrganizationRuntimeState
                    {
                        Index = sourceOrganization.Index,
                        Id = sourceOrganization.Id,
                        HeadPersonId = sourceOrganization.HeadPersonId,
                        Funds = sourceOrganization.Funds,
                        AssetValue = sourceOrganization.AssetValue,
                        HouseholdCount = runtime.Households.Count(item =>
                            item.FamilyOrganizationIndex == sourceOrganization.Index),
                        FamilyCenterFacilityId = sourceOrganization.FacilityIds
                            .OrderBy(item => item, StringComparer.Ordinal)
                            .FirstOrDefault() ?? string.Empty,
                        LastStrategyId = "family.monitor_estate"
                    });
            }
            runtime.GovernmentEconomy.Treasury = 100_000_000;
            runtime.GovernmentEconomy.CurrentFoodPolicyId =
                "government.food.market_reserve";
            runtime.GovernmentEconomy.CurrentDevelopmentPolicyId =
                "government.development.maintain_capital";
        }

        private void BuildFacilityRuntime(Luoyang184LivingWorldRuntimeState runtime)
        {
            var workerCounts = new int[source.FacilityCount];
            var workerEffective = new long[source.FacilityCount];
            foreach (var worker in runtime.Workforce)
            {
                if (worker.Status == LuoyangWorkforceStatus.Assigned &&
                    worker.FacilityIndex < source.FacilityCount)
                {
                    workerCounts[worker.FacilityIndex]++;
                    workerEffective[worker.FacilityIndex] +=
                        worker.EffectiveLaborBasisPoints;
                }
            }

            foreach (var facility in source.Facilities)
            {
                var production = IsProductionFacility(facility);
                var minimum = production
                    ? Math.Max(1, facility.MinimumWorkers > 0
                        ? facility.MinimumWorkers
                        : Math.Max(1, facility.WorkerCapacity / 4))
                    : facility.MinimumWorkers;
                var optimal = Math.Max(minimum,
                    facility.WorkerCapacity > 0
                        ? facility.WorkerCapacity
                        : minimum);
                MapRecipe(facility, out var recipe, out var input,
                    out var output, out var inputQuantity, out var outputQuantity);
                var assigned = workerCounts[facility.FacilityIndex];
                var status = !facility.Operational
                    ? LuoyangProductionRuntimeStatus.Maintenance
                    : string.IsNullOrEmpty(recipe)
                        ? LuoyangProductionRuntimeStatus.Idle
                        : assigned < minimum
                            ? LuoyangProductionRuntimeStatus.WaitingWorker
                            : LuoyangProductionRuntimeStatus.Ready;
                runtime.Facilities.Add(new LuoyangFacilityProductionRuntimeState
                {
                    FacilityIndex = facility.FacilityIndex,
                    FacilityId = facility.FacilityId,
                    DefinitionId = facility.DefinitionId,
                    OwnerId = facility.OwnerId,
                    SettlementId = facility.SettlementId,
                    CellId64 = facility.CellId64,
                    ResidentCapacity = facility.ResidentCapacity,
                    CurrentResidents = facility.CurrentResidents,
                    RecipeId = recipe,
                    InputProductId = input,
                    OutputProductId = output,
                    MinimumWorkers = minimum,
                    OptimalWorkers = optimal,
                    AssignedWorkers = assigned,
                    EffectiveWorkersBasisPoints = assigned == 0
                        ? 0
                        : (int)Math.Min(int.MaxValue,
                            workerEffective[facility.FacilityIndex] /
                            Math.Max(1, assigned)),
                    InputQuantity = inputQuantity,
                    OutputQuantity = outputQuantity,
                    Status = status,
                    StopReasonId = status == LuoyangProductionRuntimeStatus.WaitingWorker
                        ? "labor.minimum_crew_not_met"
                        : string.Empty,
                    AiResponseActionId = status ==
                        LuoyangProductionRuntimeStatus.WaitingWorker
                        ? "facility.request_workers"
                        : "facility.monitor_plan",
                    ConditionBasisPoints = facility.Operational ? 10_000 : 5_000
                });
            }
            AlignStaffedWorkshopsWithReferenceInputs(runtime);
        }

        private void AlignStaffedWorkshopsWithReferenceInputs(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var staffed = runtime.Facilities.Where(item =>
                    !string.IsNullOrEmpty(item.RecipeId) &&
                    !item.RecipeId.StartsWith("recipe.field.",
                        StringComparison.Ordinal) &&
                    item.AssignedWorkers >= item.MinimumWorkers)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .ToList();
            var inputs = source.SupplyChains.Where(item =>
                    item.ProductDefinitionId != "product.goods.general" &&
                    item.ProductDefinitionId != "product.food.millet_grain")
                .OrderBy(item => item.ChainId, StringComparer.Ordinal)
                .ToList();
            for (var index = 0; index < inputs.Count && index < staffed.Count;
                 index++)
                ApplyReferenceRecipe(staffed[index], inputs[index]
                    .ProductDefinitionId);
        }

        private static void ApplyReferenceRecipe(
            LuoyangFacilityProductionRuntimeState facility, string inputProductId)
        {
            facility.InputProductId = inputProductId;
            facility.InputQuantity = inputProductId ==
                "product.material.craft_fiber" ? 80_000 : 100_000;
            facility.OutputQuantity = inputProductId ==
                "product.material.craft_fiber" ? 60_000 :
                inputProductId == "product.material.timber" ? 75_000 : 70_000;
            if (inputProductId == "product.material.craft_fiber")
            {
                facility.RecipeId = "recipe.processing.weave_plain_cloth";
                facility.OutputProductId = CoreProductionContent.PlainClothProductId;
            }
            else if (inputProductId == "product.material.timber")
            {
                facility.RecipeId = "recipe.processing.carpentry_general_goods";
                facility.OutputProductId = "product.goods.general";
            }
            else
            {
                facility.RecipeId = "recipe.processing.sort_wool_hide";
                facility.OutputProductId = CoreProductionContent.LeatherMaterialProductId;
            }
        }

        private void BuildOpeningInventories(Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var chain in source.SupplyChains.OrderBy(item => item.ChainId,
                         StringComparer.Ordinal))
            {
                var destination = source.Facilities.First(item =>
                    item.FacilityId == chain.DestinationFacilityId);
                var facility = destination.StorageCapacity > 0
                    ? destination
                    : source.Facilities.First(item =>
                        item.FacilityId == chain.WarehouseFacilityId);
                var inventory = GetOrCreateInventory(runtime,
                    facility.FacilityId, chain.ProductDefinitionId,
                    OwnerKind(facility.OwnerId), facility.OwnerId,
                    false);
                var quantity = checked(chain.DeliveredUnits * MilliunitsPerUnit);
                var available = AvailableCapacity(runtime, facility.FacilityId);
                var stored = Math.Min(quantity, available);
                inventory.QuantityMilliunits += stored;
                runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                {
                    Id = chain.ChainId + ".opening",
                    Day = runtime.AbsoluteDay,
                    OperationId = "scenario.opening.delivered_stock",
                    ProductId = chain.ProductDefinitionId,
                    DestinationInventoryId = inventory.Id,
                    QuantityMilliunits = stored,
                    LossMilliunits = checked((chain.CarrierConsumptionUnits +
                        chain.NaturalLossUnits + chain.RoadLossUnits) * MilliunitsPerUnit),
                    FacilityId = facility.FacilityId,
                    PersonId = source.GetPersonId(chain.CarrierPersonOrdinal)
                });
            }
        }

        private void BuildOpeningProperty(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var facility in runtime.Facilities.OrderBy(item =>
                         item.CellId64).ThenBy(item => item.FacilityId,
                         StringComparer.Ordinal))
            {
                var existing = runtime.CellProperties.Find(item =>
                    item.CellId64 == facility.CellId64);
                if (existing == null)
                {
                    runtime.CellProperties.Add(new LuoyangCellPropertyRuntimeState
                    {
                        CellId64 = facility.CellId64,
                        OwnerId = facility.OwnerId,
                        AdministrativeControllerId =
                            "organization.government.han.luoyang",
                        BuildingRightHolderId = facility.OwnerId,
                        FacilityId = facility.FacilityId
                    });
                }
                else if (string.CompareOrdinal(facility.FacilityId,
                             existing.FacilityId) < 0)
                {
                    existing.FacilityId = facility.FacilityId;
                }
            }
            AddVacantDevelopmentProperties(runtime, source.DevelopableCellIds, 64);
        }

        private static void AddVacantDevelopmentProperties(
            Luoyang184LivingWorldRuntimeState runtime,
            IReadOnlyList<ulong> developableCellIds, int count)
        {
            var used = new HashSet<ulong>(runtime.CellProperties.Select(item =>
                item.CellId64));
            var candidates = developableCellIds.Where(item => !used.Contains(item))
                .OrderBy(item => item).Take(count).ToArray();
            if (candidates.Length < count)
                throw new InvalidOperationException(
                    "The canonical Luoyang map has insufficient vacant developable Cells.");
            foreach (var cellId64 in candidates)
            {
                runtime.CellProperties.Add(new LuoyangCellPropertyRuntimeState
                {
                    CellId64 = cellId64,
                    OwnerId = runtime.GovernmentEconomy.OrganizationId,
                    AdministrativeControllerId =
                        runtime.GovernmentEconomy.OrganizationId,
                    BuildingRightHolderId =
                        runtime.GovernmentEconomy.OrganizationId,
                    FacilityId = string.Empty
                });
            }
        }

        private void BuildExternalSuppliers(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var sourceSupplier in source.ExternalSuppliers.OrderBy(
                         item => item.SupplierId, StringComparer.Ordinal))
            {
                runtime.ExternalSuppliers.Add(
                    new LuoyangExternalSupplierRuntimeState
                    {
                        SupplierId = sourceSupplier.SupplierId,
                        Level = sourceSupplier.Level,
                        CountyId = sourceSupplier.CountyId,
                        SettlementId = sourceSupplier.SettlementId,
                        FacilityId = sourceSupplier.FacilityId,
                        InventoryId = sourceSupplier.InventoryId,
                        OrganizationId = sourceSupplier.OrganizationId,
                        ManagerPersonId = sourceSupplier.ManagerPersonId,
                        ManagerHouseholdId = sourceSupplier.ManagerHouseholdId,
                        ProductId = sourceSupplier.ProductId,
                        InventoryQuantityMilliunits =
                            sourceSupplier.OpeningQuantityMilliunits,
                        StorageCapacityMilliunits =
                            sourceSupplier.StorageCapacityMilliunits,
                        DailyProductionMilliunits =
                            sourceSupplier.DailyProductionMilliunits,
                        CashBalance = 1_000_000,
                        RouteId = sourceSupplier.RouteId,
                        DistanceKilometers = sourceSupplier.DistanceKilometers,
                        TravelDays = sourceSupplier.TravelDays,
                        NaturalLossBasisPoints =
                            sourceSupplier.NaturalLossBasisPoints,
                        RiskLossBasisPoints =
                            sourceSupplier.RiskLossBasisPoints,
                        EvidenceGrade = sourceSupplier.EvidenceGrade,
                        SourceReferenceId = sourceSupplier.SourceReferenceId
                    });
            }
        }

        private static void AllocateOpeningHouseholdFood(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var sources = runtime.Inventories.Where(item =>
                    FoodProducts.Contains(item.ProductId) &&
                    item.QuantityMilliunits > 0)
                .OrderBy(item => item.Id, StringComparer.Ordinal).ToList();
            if (sources.Count == 0) return;
            var sourceIndex = 0;
            var movedBySource = new Dictionary<string, long>(
                StringComparer.Ordinal);
            foreach (var household in runtime.Households)
            {
                var required = checked(household.DailyFoodDemandMilliunits * 7);
                while (required > 0 && sourceIndex < sources.Count)
                {
                    var source = sources[sourceIndex];
                    var moved = Math.Min(required, source.QuantityMilliunits);
                    source.QuantityMilliunits -= moved;
                    required -= moved;
                    household.FoodReserveMilliunits += moved;
                    household.CumulativeFoodAcquiredMilliunits += moved;
                    household.LastAcquisitionSourceId = "scenario.opening.food_allocation";
                    if (!movedBySource.ContainsKey(source.Id))
                        movedBySource[source.Id] = 0;
                    movedBySource[source.Id] += moved;
                    if (source.QuantityMilliunits == 0) sourceIndex++;
                }
                if (sourceIndex >= sources.Count) break;
            }
            foreach (var entry in movedBySource.OrderBy(item => item.Key,
                         StringComparer.Ordinal))
            {
                var source = sources.First(item => item.Id == entry.Key);
                runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                {
                    Id = "flow.scenario.opening.household_food." +
                         runtime.InventoryFlows.Count,
                    Day = runtime.AbsoluteDay,
                    OperationId = "scenario.opening.household_food_allocation",
                    ProductId = source.ProductId,
                    SourceInventoryId = source.Id,
                    DestinationInventoryId = "household.compact_reserves",
                    QuantityMilliunits = entry.Value,
                    FacilityId = source.FacilityId
                });
            }
        }

        private void ResolveExternalSupply(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var deliveredConstructionRequesters = new HashSet<string>(
                StringComparer.Ordinal);
            foreach (var supplier in runtime.ExternalSuppliers)
            {
                if (supplier.Level ==
                    LuoyangSupplierMaterializationLevel.DeferredExternalTrade)
                    continue;
                var productionScale = LuoyangFormalEconomySystem.IsFood(
                    supplier.ProductId)
                    ? calibrationProfile.ExternalProductionScale
                    : 1;
                var produced = Math.Min(
                    checked(supplier.DailyProductionMilliunits *
                            productionScale),
                    supplier.StorageCapacityMilliunits -
                    supplier.InventoryQuantityMilliunits);
                var operatingCost = Math.Min(supplier.CashBalance,
                    (produced + 9_999L) / 10_000L);
                if (operatingCost > 0)
                {
                    supplier.CashBalance -= operatingCost;
                    supplier.CumulativeOperatingExpense += operatingCost;
                    var household = runtime.Households.FirstOrDefault(item =>
                        item.HouseholdId == supplier.ManagerHouseholdId);
                    if (household != null) household.Wealth += operatingCost;
                }
                if (LuoyangFormalEconomySystem.IsFood(supplier.ProductId))
                    produced = new LuoyangFormalEconomySystem().Produce(runtime,
                        supplier.InventoryId, supplier.ProductId, produced,
                        InventoryTransactionType.RecipeSettled,
                        "external.production." + runtime.AbsoluteDay + "." +
                        supplier.SupplierId);
                else
                    supplier.InventoryQuantityMilliunits += produced;
                supplier.CumulativeProducedMilliunits += produced;
            }

            foreach (var shipment in runtime.Shipments.Where(item =>
                         !item.Delivered &&
                         item.ArrivalDay <= runtime.AbsoluteDay).OrderBy(
                         item => item.Id, StringComparer.Ordinal))
            {
                var destination = runtime.Inventories.Find(item =>
                    item.Id == shipment.DestinationInventoryId);
                if (destination == null)
                    throw new InvalidOperationException(
                        "Shipment destination inventory disappeared.");
                var stored = Math.Min(
                    shipment.DeliveredQuantityMilliunits,
                    destination.CapacityMilliunits -
                    destination.QuantityMilliunits);
                if (LuoyangFormalEconomySystem.IsFood(shipment.ProductId))
                    stored = new LuoyangFormalEconomySystem().ReceiveFreight(
                        runtime, shipment.Id, destination.Id,
                        shipment.ProductId, stored);
                else
                    destination.QuantityMilliunits += stored;
                shipment.Delivered = true;
                var order = runtime.SupplyOrders.Find(item =>
                    item.Id == shipment.OrderId);
                order.DeliveredQuantityMilliunits = stored;
                order.Status = LuoyangSupplyOrderStatus.Delivered;
                runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                {
                    Id = "flow.supply.delivery." + runtime.AbsoluteDay + "." +
                         runtime.InventoryFlows.Count,
                    Day = runtime.AbsoluteDay,
                    OperationId = "supply.shipment_delivered",
                    ProductId = shipment.ProductId,
                    SourceInventoryId = shipment.SourceInventoryId,
                    DestinationInventoryId = destination.Id,
                    QuantityMilliunits = stored,
                    LossMilliunits = checked(
                        shipment.CarrierConsumptionMilliunits +
                        shipment.NaturalLossMilliunits +
                        shipment.RiskLossMilliunits),
                    PersonId = shipment.CarrierPersonId,
                    FacilityId = destination.FacilityId
                });
                if (order.ReasonId != null && order.ReasonId.StartsWith(
                        "blueprint.material_procurement:",
                        StringComparison.Ordinal) &&
                    order.RequestedByAgentId != null &&
                    !order.RequestedByAgentId.StartsWith("player.",
                        StringComparison.Ordinal))
                    deliveredConstructionRequesters.Add(
                        order.RequestedByAgentId);
            }

            foreach (var requester in deliveredConstructionRequesters)
                TryStartDeliveredAiBlueprint(runtime, requester);

            var dailyFoodDemand = runtime.DailyFoodDemandMilliunits;
            if (runtime.IntelligentAgents.Count > 0)
                return;
            foreach (var productGroup in runtime.ExternalSuppliers.Where(item =>
                         item.Level != LuoyangSupplierMaterializationLevel
                             .DeferredExternalTrade).GroupBy(item =>
                         item.ProductId).OrderBy(item => item.Key,
                         StringComparer.Ordinal))
            {
                var destination = FindSupplyDestination(runtime,
                    productGroup.Key);
                if (destination == null) continue;
                var targetStock = FoodProducts.Contains(productGroup.Key)
                    ? checked(dailyFoodDemand * 30)
                    : Math.Max(100_000L,
                        destination.CapacityMilliunits / 4);
                var inbound = runtime.Shipments.Where(item =>
                        !item.Delivered && item.ProductId == productGroup.Key)
                    .Sum(item => item.DeliveredQuantityMilliunits);
                var shortage = targetStock - destination.QuantityMilliunits -
                               inbound;
                if (shortage <= 0) continue;
                var supplier = productGroup.Where(item =>
                        item.InventoryQuantityMilliunits > 0)
                    .OrderBy(item => item.Level)
                    .ThenBy(item => item.DistanceKilometers)
                    .ThenBy(item => item.SupplierId, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (supplier == null) continue;
                DispatchSupply(runtime, supplier, destination, shortage);
            }
        }

        private static void TryStartDeliveredAiBlueprint(
            Luoyang184LivingWorldRuntimeState runtime, string requester)
        {
            if (runtime.ConstructionProjects.Exists(item =>
                    !item.Cancelled && item.RequestedByAgentId == requester))
                return;
            var orders = runtime.SupplyOrders.Where(item =>
                    item.RequestedByAgentId == requester &&
                    item.ReasonId != null && item.ReasonId.StartsWith(
                        "blueprint.material_procurement:",
                        StringComparison.Ordinal))
                .OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
            if (orders.Length == 0 || orders.Any(item =>
                    item.Status != LuoyangSupplyOrderStatus.Delivered))
                return;
            var blueprintId = orders[0].ReasonId.Substring(
                "blueprint.material_procurement:".Length);
            if (orders.Any(item => !item.ReasonId.EndsWith(blueprintId,
                    StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "AI construction procurement mixes Blueprint identities.");
            var owner = runtime.GovernmentEconomy.OrganizationId;
            var property = runtime.CellProperties.Where(item =>
                    item.OwnerId == owner &&
                    item.BuildingRightHolderId == owner &&
                    string.IsNullOrEmpty(item.FacilityId) &&
                    !runtime.ConstructionProjects.Exists(project =>
                        !project.Completed && !project.Cancelled &&
                        project.CellId64 == item.CellId64))
                .OrderBy(item => item.CellId64).FirstOrDefault();
            if (property == null) return;
            new LuoyangVisualPresentationSystem().StartFromBlueprint(runtime,
                blueprintId, property.CellId64, owner, requester);
        }

        private static LuoyangInventoryBalanceState FindSupplyDestination(
            Luoyang184LivingWorldRuntimeState runtime, string productId)
        {
            var existing = runtime.Inventories.Where(item =>
                    item.ProductId == productId &&
                    (item.OwnerKind == LuoyangInventoryOwnerKind.Market ||
                     item.OwnerKind == LuoyangInventoryOwnerKind.Government))
                .OrderBy(item => item.OwnerKind ==
                    LuoyangInventoryOwnerKind.Market ? 0 : 1)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (existing != null) return existing;
            var template = runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market ||
                    item.OwnerKind == LuoyangInventoryOwnerKind.Government)
                .OrderByDescending(item => item.CapacityMilliunits)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (template == null) return null;
            var created = new LuoyangInventoryBalanceState
            {
                Id = "inventory.supply_destination." + productId,
                OwnerKind = template.OwnerKind,
                OwnerId = template.OwnerId,
                FacilityId = template.FacilityId,
                ProductId = productId,
                CapacityMilliunits = template.CapacityMilliunits,
                QuantityMilliunits = 0,
                IsTransitionalReferenceSupply = false
            };
            runtime.Inventories.Add(created);
            return created;
        }

        private void DispatchSupply(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangExternalSupplierRuntimeState supplier,
            LuoyangInventoryBalanceState destination,
            long requested)
        {
            var shipped = Math.Min(requested,
                supplier.InventoryQuantityMilliunits);
            var market = runtime.Markets.Find(item =>
                item.ProductId == supplier.ProductId);
            var unitPrice = Math.Max(1L, (market?.BasePrice ?? 1) * 8 / 10);
            var payerCash = market?.CashBalance ?? 0;
            shipped = Math.Min(shipped, payerCash * 1_000 / unitPrice);
            if (shipped <= 0) return;
            var purchaseCost = checked((shipped * unitPrice + 999) / 1_000);
            var carrierConsumption = Math.Min(shipped,
                checked((long)Math.Max(1, supplier.TravelDays) *
                    2_000L));
            var remaining = shipped - carrierConsumption;
            var naturalLoss = remaining *
                supplier.NaturalLossBasisPoints / 10_000;
            var riskRoll = new NamedRandom(runtime.MasterSeed).Range(
                "luoyang.supply.risk",
                new StableId(supplier.SupplierId),
                runtime.AbsoluteDay,
                "shipment_loss",
                0,
                10_000,
                checked((uint)runtime.SupplyOrders.Count));
            var riskLoss = riskRoll < supplier.RiskLossBasisPoints
                ? (remaining - naturalLoss) *
                  supplier.RiskLossBasisPoints / 10_000
                : 0;
            var delivered = shipped - carrierConsumption - naturalLoss - riskLoss;
            if (delivered <= 0) return;
            var orderId = "supply_order." + runtime.AbsoluteDay + "." +
                          runtime.SupplyOrders.Count.ToString("D6");
            var shipmentId = "shipment." + runtime.AbsoluteDay + "." +
                             runtime.Shipments.Count.ToString("D6");
            if (LuoyangFormalEconomySystem.IsFood(supplier.ProductId))
                new LuoyangFormalEconomySystem().DispatchFreight(runtime,
                    supplier.InventoryId, shipmentId, supplier.ProductId,
                    shipped,
                    checked(carrierConsumption + naturalLoss + riskLoss),
                    supplier.ManagerPersonId);
            else
                supplier.InventoryQuantityMilliunits -= shipped;
            if (market != null) market.CashBalance -= purchaseCost;
            supplier.CashBalance += purchaseCost;
            supplier.CumulativeSalesRevenue += purchaseCost;
            supplier.CumulativeDispatchedMilliunits += shipped;
            runtime.SupplyOrders.Add(new LuoyangSupplyOrderRuntimeState
            {
                Id = orderId,
                RequestedDay = runtime.AbsoluteDay,
                ProductId = supplier.ProductId,
                SupplierId = supplier.SupplierId,
                DestinationInventoryId = destination.Id,
                RequestedQuantityMilliunits = requested,
                DispatchedQuantityMilliunits = shipped,
                UnitPrice = unitPrice,
                PurchaseCost = purchaseCost,
                Status = LuoyangSupplyOrderStatus.InTransit,
                ShipmentId = shipmentId,
                RequestedByAgentId = "settlement.luoyang.supply_manager",
                ReasonId = "inventory.below_target"
            });
            runtime.Shipments.Add(new LuoyangShipmentRuntimeState
            {
                Id = shipmentId,
                OrderId = orderId,
                ProductId = supplier.ProductId,
                SupplierId = supplier.SupplierId,
                SourceInventoryId = supplier.InventoryId,
                DestinationInventoryId = destination.Id,
                RouteId = supplier.RouteId,
                CarrierPersonId = supplier.ManagerPersonId,
                DispatchDay = runtime.AbsoluteDay,
                ArrivalDay = checked(runtime.AbsoluteDay +
                    supplier.TravelDays),
                ShippedQuantityMilliunits = shipped,
                CarrierConsumptionMilliunits = carrierConsumption,
                NaturalLossMilliunits = naturalLoss,
                RiskLossMilliunits = riskLoss,
                DeliveredQuantityMilliunits = delivered,
                PurchaseCost = purchaseCost
            });
        }

        private void BuildCrops(Luoyang184LivingWorldRuntimeState runtime)
        {
            var granaries = source.Facilities.Where(item =>
                    item.StorageCapacity > 0 &&
                    item.DefinitionId.IndexOf("granary", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal).ToList();
            if (granaries.Count == 0)
                throw new InvalidOperationException("Luoyang has no real granary capacity.");
            var facilityIndex = source.Facilities.ToDictionary(
                item => item.FacilityId, item => item.FacilityIndex,
                StringComparer.Ordinal);
            for (var i = 0; i < source.Agriculture.Count; i++)
            {
                var field = source.Agriculture[i];
                var initialOffset = calibrationProfile
                    .AgricultureInitialStageWindowDays <= 0
                    ? 0
                    : i * calibrationProfile.AgricultureInitialStageWindowDays /
                      Math.Max(1, source.Agriculture.Count);
                var plantedDay = field.PlantedDay - initialOffset;
                // The accepted outer-agriculture contract samples every field
                // three times during the opening 30 days.  Initial-stage
                // staggering may move fields earlier inside their real crop
                // cycle, but it must not silently turn those opening samples
                // into accelerated harvests.
                var maturityDay = Math.Max(runtime.AbsoluteDay + 31,
                    field.MaturityDay - initialOffset);
                var destination = granaries[i % granaries.Count];
                var storage = GetOrCreateInventory(runtime,
                    destination.FacilityId, field.ProductDefinitionId,
                    OwnerKind(destination.OwnerId), destination.OwnerId, false);
                var seed = GetOrCreateInventory(runtime,
                    destination.FacilityId, SeedProductId(field.ProductDefinitionId),
                    OwnerKind(destination.OwnerId), destination.OwnerId, false);
                runtime.Crops.Add(new LuoyangCropRuntimeState
                {
                    FieldId = field.FieldId,
                    FacilityIndex = facilityIndex[field.FacilityId],
                    FacilityId = field.FacilityId,
                    CellId64 = field.CellId64,
                    CropProductId = field.ProductDefinitionId,
                    StorageInventoryId = storage.Id,
                    SeedInventoryId = seed.Id,
                    PlantingDay = plantedDay,
                    FullMaturityDay = maturityDay,
                    CycleDurationDays = field.MaturityDay - field.PlantedDay,
                    EarlyHarvestMinimumBasisPoints =
                        field.EarlyHarvestMinimumBasisPoints,
                    MaturityBasisPoints = Luoyang184LivingWorldRules
                        .CalculateMaturityBasisPoints(runtime.AbsoluteDay,
                            plantedDay, maturityDay),
                    FullYieldMilliunits = checked(
                        field.FullYieldUnits * MilliunitsPerUnit *
                        calibrationProfile.AgricultureYieldUnitScale),
                    AssignedWorkers = field.WorkerPersonOrdinals.Count,
                    Phase = LuoyangCropPhase.Growing
                });
            }
        }

        private void BuildMarkets(Luoyang184LivingWorldRuntimeState runtime)
        {
            var marketStorage = source.Facilities.Where(item =>
                    item.StorageCapacity > 0)
                .OrderByDescending(item => item.StorageCapacity)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .First();
            var marketProducts = new HashSet<string>(FoodProducts,
                StringComparer.Ordinal);
            foreach (var supplier in runtime.ExternalSuppliers)
                marketProducts.Add(supplier.ProductId);
            foreach (var productId in marketProducts.OrderBy(item => item,
                         StringComparer.Ordinal))
            {
                runtime.Markets.Add(new LuoyangMarketRuntimeState
                {
                    ProductId = productId,
                    BasePrice = 1,
                    CurrentPriceBasisPoints = 10_000
                });
                runtime.Inventories.Add(new LuoyangInventoryBalanceState
                {
                    Id = "inventory.market.luoyang.184." + productId,
                    OwnerKind = LuoyangInventoryOwnerKind.Market,
                    OwnerId = "market.luoyang.184",
                    FacilityId = marketStorage.FacilityId,
                    ProductId = productId,
                    CapacityMilliunits = checked(
                        Math.Max(1L, marketStorage.StorageCapacity) *
                        MilliunitsPerUnit)
                });
            }
        }

        private void BuildIntelligentAgents(
            Luoyang184LivingWorldRuntimeState runtime) =>
            new Luoyang184IntelligentAgentRuntimeSystem().BuildAgents(
                runtime, source);

        private static void ResolveIntelligentAgents(
            Luoyang184LivingWorldRuntimeState runtime) =>
            new Luoyang184IntelligentAgentRuntimeSystem().AdvanceDay(runtime);

        private void ResolveProduction(Luoyang184LivingWorldRuntimeState runtime)
        {
            new Luoyang184AgricultureDueScheduler().DispatchDue(
                runtime, crop => AdvanceCropDue(runtime, crop));

            foreach (var facility in runtime.Facilities.OrderBy(item =>
                         item.FacilityId, StringComparer.Ordinal))
            {
                if (string.IsNullOrEmpty(facility.RecipeId) ||
                    facility.RecipeId.StartsWith("recipe.field.",
                        StringComparison.Ordinal))
                    continue;
                if (facility.AssignedWorkers < facility.MinimumWorkers)
                {
                    facility.Status = LuoyangProductionRuntimeStatus.WaitingWorker;
                    facility.StopReasonId = "labor.minimum_crew_not_met";
                    facility.AiResponseActionId = "facility.request_workers";
                    continue;
                }
                var input = FindAvailableInventory(runtime,
                    facility.InputProductId, facility.InputQuantity);
                if (input == null)
                {
                    facility.Status = LuoyangProductionRuntimeStatus.WaitingInput;
                    facility.StopReasonId = "inventory.recipe_input_missing";
                    facility.AiResponseActionId = "facility.seek_input";
                    continue;
                }
                var output = GetOrCreateOutputInventory(runtime, facility);
                if (AvailableCapacity(runtime, output.FacilityId) <
                    facility.OutputQuantity)
                {
                    facility.Status = LuoyangProductionRuntimeStatus.OutputBlocked;
                    facility.StopReasonId = "inventory.output_capacity_full";
                    facility.AiResponseActionId = "facility.seek_storage";
                    continue;
                }
                if (facility.CycleStartedDay < 0)
                {
                    facility.CycleStartedDay = runtime.AbsoluteDay;
                    facility.CycleDueDay = runtime.AbsoluteDay +
                        RecipeDurationDays(facility.RecipeId);
                    facility.ProductionProgressBasisPoints = 0;
                }
                facility.Status = LuoyangProductionRuntimeStatus.InProgress;
                var duration = Math.Max(1,
                    facility.CycleDueDay - facility.CycleStartedDay);
                facility.ProductionProgressBasisPoints = (int)Math.Min(10_000,
                    (runtime.AbsoluteDay - facility.CycleStartedDay) * 10_000 / duration);
                if (runtime.AbsoluteDay < facility.CycleDueDay) continue;

                if (input.QuantityMilliunits < facility.InputQuantity)
                {
                    facility.Status = LuoyangProductionRuntimeStatus.WaitingInput;
                    facility.StopReasonId = "inventory.recipe_input_changed";
                    facility.AiResponseActionId = "facility.seek_input";
                    continue;
                }
                if (LuoyangFormalEconomySystem.IsFood(input.ProductId))
                    new LuoyangFormalEconomySystem().ConsumeInventory(runtime,
                        input.Id, input.ProductId, facility.InputQuantity,
                        InventoryTransactionType.RecipeSettled,
                        "production.input." + runtime.AbsoluteDay + "." +
                        facility.FacilityIndex);
                else
                    input.QuantityMilliunits -= facility.InputQuantity;
                if (LuoyangFormalEconomySystem.IsFood(output.ProductId))
                    new LuoyangFormalEconomySystem().Produce(runtime,
                        output.Id, output.ProductId, facility.OutputQuantity,
                        InventoryTransactionType.RecipeSettled,
                        "production.output." + runtime.AbsoluteDay + "." +
                        facility.FacilityIndex,
                        "workorder.compact.production." +
                        facility.FacilityIndex + "." +
                        facility.CycleStartedDay);
                else
                    output.QuantityMilliunits += facility.OutputQuantity;
                runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                {
                    Id = "flow.production." + runtime.AbsoluteDay + "." +
                         facility.FacilityIndex,
                    Day = runtime.AbsoluteDay,
                    OperationId = "production.recipe_settlement",
                    ProductId = facility.OutputProductId,
                    SourceInventoryId = input.Id,
                    DestinationInventoryId = output.Id,
                    QuantityMilliunits = facility.OutputQuantity,
                    LossMilliunits = Math.Max(0,
                        facility.InputQuantity - facility.OutputQuantity),
                    FacilityId = facility.FacilityId
                });
                facility.Status = LuoyangProductionRuntimeStatus.Completed;
                facility.StopReasonId = string.Empty;
                facility.AiResponseActionId = "facility.sell_output";
                facility.CycleStartedDay = -1;
                facility.CycleDueDay = -1;
                facility.ProductionProgressBasisPoints = 10_000;
            }
        }

        private void AdvanceCropDue(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangCropRuntimeState crop)
        {
            if (crop.Phase == LuoyangCropPhase.Harvested ||
                crop.Phase == LuoyangCropPhase.Fallow)
            {
                if (runtime.AbsoluteDay < crop.NextPlantingDay) return;
                if (!TrySowNextCycle(runtime, crop)) return;
            }
            crop.MaturityBasisPoints = Luoyang184LivingWorldRules
                .CalculateMaturityBasisPoints(runtime.AbsoluteDay,
                    crop.PlantingDay, crop.FullMaturityDay);
            crop.Phase = crop.MaturityBasisPoints <
                         crop.EarlyHarvestMinimumBasisPoints
                ? LuoyangCropPhase.Growing
                : crop.MaturityBasisPoints < 10_000
                    ? LuoyangCropPhase.Harvestable
                    : crop.MaturityBasisPoints <= 11_000
                        ? LuoyangCropPhase.Mature
                        : LuoyangCropPhase.AtRisk;
            if (crop.MaturityBasisPoints >= 10_000)
                Harvest(runtime, crop, out _);
        }

        private bool Harvest(Luoyang184LivingWorldRuntimeState runtime,
            LuoyangCropRuntimeState crop, out long harvestedMilliunits)
        {
            harvestedMilliunits = 0;
            if (crop.Phase == LuoyangCropPhase.Harvested ||
                crop.AssignedWorkers <= 0 ||
                !Luoyang184LivingWorldRules.CanHarvest(crop.MaturityBasisPoints,
                    crop.EarlyHarvestMinimumBasisPoints))
                return false;
            var inventory = runtime.Inventories.Find(item =>
                item.Id == crop.StorageInventoryId) ??
                throw new InvalidOperationException("Crop storage is missing.");
            var yield = Luoyang184LivingWorldRules.CalculateHarvestYield(
                crop.FullYieldMilliunits, crop.MaturityBasisPoints);
            var available = AvailableCapacity(runtime, inventory.FacilityId);
            var seedInventory = runtime.Inventories.Find(item =>
                item.Id == crop.SeedInventoryId) ??
                throw new InvalidOperationException("Crop seed inventory is missing.");
            var seedTarget = Math.Max(1_000, yield / 20);
            var seed = Math.Min(seedTarget, available);
            seedInventory.QuantityMilliunits += seed;
            available -= seed;
            var stored = Math.Min(yield - seed, available);
            var lost = yield - seed - stored;
            stored = new LuoyangFormalEconomySystem().Produce(runtime,
                inventory.Id, crop.CropProductId, stored,
                InventoryTransactionType.FoodHarvested,
                "harvest." + runtime.AbsoluteDay + "." + crop.FieldId +
                "." + crop.CycleNumber,
                "workorder.luoyang.harvest." + crop.FieldId + ".cycle." +
                crop.CycleNumber);
            var marketInventory = runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                    item.ProductId == crop.CropProductId &&
                    item.Id != inventory.Id)
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (marketInventory != null &&
                calibrationProfile.HarvestMarketReleaseBasisPoints > 0)
            {
                var marketRelease = checked(stored * calibrationProfile
                    .HarvestMarketReleaseBasisPoints / 10_000);
                var released = new LuoyangFormalEconomySystem().Transfer(
                    runtime, inventory.Id, marketInventory.Id,
                    crop.CropProductId, marketRelease,
                    InventoryTransactionType.FoodMarketTransferred,
                    "harvest.market_release." + runtime.AbsoluteDay + "." +
                    crop.FieldId + "." + crop.CycleNumber);
                if (released > 0)
                    runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                    {
                        Id = "flow.harvest.market_release." +
                             runtime.AbsoluteDay + "." + crop.FieldId,
                        Day = runtime.AbsoluteDay,
                        OperationId = "market.harvest_release",
                        ProductId = crop.CropProductId,
                        SourceInventoryId = inventory.Id,
                        DestinationInventoryId = marketInventory.Id,
                        QuantityMilliunits = released,
                        FacilityId = crop.FacilityId
                    });
            }
            crop.ActualYieldMilliunits = yield;
            crop.StoredYieldMilliunits = stored;
            crop.SeedRecoveredMilliunits = seed;
            crop.LostYieldMilliunits = lost;
            crop.CumulativeYieldMilliunits += yield;
            crop.CumulativeStoredYieldMilliunits += stored;
            crop.CumulativeSeedRecoveredMilliunits += seed;
            crop.CumulativeLostYieldMilliunits += lost;
            crop.HarvestQualityBasisPoints =
                Luoyang184LivingWorldRules.CalculateHarvestQuality(
                    crop.MaturityBasisPoints);
            crop.HarvestedDay = runtime.AbsoluteDay;
            crop.NextPlantingDay = runtime.AbsoluteDay + 30;
            crop.Phase = LuoyangCropPhase.Harvested;
            runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
            {
                Id = "flow.harvest." + runtime.AbsoluteDay + "." + crop.FieldId,
                Day = runtime.AbsoluteDay,
                OperationId = "production.crop_harvest",
                ProductId = crop.CropProductId,
                DestinationInventoryId = inventory.Id,
                QuantityMilliunits = stored,
                LossMilliunits = lost,
                FacilityId = crop.FacilityId
            });
            runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
            {
                Id = "flow.seed.recovery." + runtime.AbsoluteDay + "." +
                     crop.FieldId,
                Day = runtime.AbsoluteDay,
                OperationId = "production.seed_recovery",
                ProductId = seedInventory.ProductId,
                DestinationInventoryId = seedInventory.Id,
                QuantityMilliunits = seed,
                FacilityId = crop.FacilityId
            });
            harvestedMilliunits = stored;
            return true;
        }

        private bool TrySowNextCycle(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangCropRuntimeState crop)
        {
            var seedInventory = runtime.Inventories.Find(item =>
                item.Id == crop.SeedInventoryId) ??
                throw new InvalidOperationException("Crop seed inventory is missing.");
            var seedRequired = Math.Max(1_000,
                crop.FullYieldMilliunits / 20);
            if (crop.AssignedWorkers <= 0 ||
                seedInventory.QuantityMilliunits < seedRequired)
            {
                crop.Phase = LuoyangCropPhase.Fallow;
                return false;
            }
            seedInventory.QuantityMilliunits -= seedRequired;
            crop.CycleNumber++;
            crop.PlantingDay = runtime.AbsoluteDay;
            crop.FullMaturityDay = runtime.AbsoluteDay +
                                   Math.Max(1, crop.CycleDurationDays);
            crop.NextPlantingDay = -1;
            crop.MaturityBasisPoints = 0;
            crop.HarvestQualityBasisPoints = 0;
            crop.ActualYieldMilliunits = 0;
            crop.StoredYieldMilliunits = 0;
            crop.SeedRecoveredMilliunits = 0;
            crop.LostYieldMilliunits = 0;
            crop.Phase = LuoyangCropPhase.Sowing;
            runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
            {
                Id = "flow.seed.sowing." + runtime.AbsoluteDay + "." +
                     crop.FieldId,
                Day = runtime.AbsoluteDay,
                OperationId = "production.crop_sowing",
                ProductId = seedInventory.ProductId,
                SourceInventoryId = seedInventory.Id,
                QuantityMilliunits = seedRequired,
                FacilityId = crop.FacilityId
            });
            return true;
        }

        private void UpdateMarkets(Luoyang184LivingWorldRuntimeState runtime)
        {
            var dailyDemand = runtime.DailyFoodDemandMilliunits;
            var foodMarkets = runtime.Markets.Where(item =>
                    LuoyangFormalEconomySystem.IsFood(item.ProductId))
                .OrderBy(item => item.ProductId, StringComparer.Ordinal)
                .ToArray();
            var foodMarketOrdinals = foodMarkets.Select((item, index) =>
                    new { item.ProductId, Index = index })
                .ToDictionary(item => item.ProductId, item => item.Index,
                    StringComparer.Ordinal);
            foreach (var market in runtime.Markets)
            {
                var productDemand = 0L;
                if (foodMarketOrdinals.TryGetValue(market.ProductId,
                        out var foodMarketIndex))
                    productDemand = AllocatedFoodMarketDemand(dailyDemand,
                        foodMarkets.Length, foodMarketIndex);
                var stock = runtime.Inventories.Where(item =>
                    item.ProductId == market.ProductId &&
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market)
                    .Sum(item => item.QuantityMilliunits);
                market.SupplyMilliunits = stock;
                market.DemandMilliunits = productDemand;
                market.TransferredMilliunits = 0;
                market.FailedDemandMilliunits = Math.Max(0,
                    productDemand - stock);
                market.TransportCostBasisPoints = AverageTransportCost(runtime,
                    market.ProductId);
                market.RiskBasisPoints = AverageSupplyRisk(runtime,
                    market.ProductId);
                market.SeasonBasisPoints = SeasonalBasisPoints(
                    runtime.AbsoluteDay);
                market.ShortageBasisPoints = productDemand <= 0 ? 0 :
                    (int)Math.Min(10_000,
                        market.FailedDemandMilliunits * 10_000 /
                        productDemand);
                var recentTrade = market.RecentTradeQuantityMilliunits <= 0
                    ? 0
                    : (int)Math.Min(2_000,
                        market.RecentTradeQuantityMilliunits * 2_000 /
                        Math.Max(1L, productDemand));
                market.CurrentPriceBasisPoints = Math.Max(2_000,
                    Math.Min(40_000, 10_000 +
                        market.ShortageBasisPoints * 2 +
                        market.TransportCostBasisPoints +
                        market.RiskBasisPoints +
                        (10_000 - market.SeasonBasisPoints) + recentTrade));
                market.RecentTradeQuantityMilliunits =
                    market.RecentTradeQuantityMilliunits * 9 / 10;
                market.RecentTradeValue = market.RecentTradeValue * 9 / 10;
            }
        }

        private void BalanceFoodMarketAccess(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            if (calibrationProfile.MarketTargetStockDays <= 0) return;
            var foodMarkets = runtime.Markets.Where(item =>
                    LuoyangFormalEconomySystem.IsFood(item.ProductId))
                .OrderBy(item => item.ProductId, StringComparer.Ordinal)
                .ToArray();
            var formal = new LuoyangFormalEconomySystem();
            for (var index = 0; index < foodMarkets.Length; index++)
            {
                var market = foodMarkets[index];
                var destination = runtime.Inventories.Where(item =>
                        item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                        item.ProductId == market.ProductId)
                    .OrderBy(item => item.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (destination == null) continue;
                var daily = AllocatedFoodMarketDemand(
                    runtime.DailyFoodDemandMilliunits, foodMarkets.Length,
                    index);
                var target = checked(daily *
                    calibrationProfile.MarketTargetStockDays);
                var shortage = Math.Max(0, target -
                    LuoyangFormalEconomySystem.GetAvailableQuantity(runtime,
                        destination.Id, destination.ProductId));
                if (shortage <= 0) continue;
                foreach (var source in runtime.Inventories.Where(item =>
                             item.Id != destination.Id &&
                             item.ProductId == market.ProductId &&
                             item.OwnerKind != LuoyangInventoryOwnerKind.Market &&
                             item.OwnerKind !=
                                 LuoyangInventoryOwnerKind.Military)
                             .OrderBy(item => item.OwnerKind ==
                                 LuoyangInventoryOwnerKind.Government ? 1 : 0)
                             .ThenBy(item => item.Id, StringComparer.Ordinal))
                {
                    if (shortage <= 0) break;
                    var available = LuoyangFormalEconomySystem
                        .GetAvailableQuantity(runtime, source.Id,
                            source.ProductId);
                    if (available <= 0) continue;
                    var moved = formal.Transfer(runtime, source.Id,
                        destination.Id, source.ProductId,
                        Math.Min(shortage, available),
                        InventoryTransactionType.FoodMarketTransferred,
                        "market.stock_access." + runtime.AbsoluteDay + "." +
                        source.Id + "." + destination.Id);
                    if (moved <= 0) continue;
                    shortage -= moved;
                    runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                    {
                        Id = "flow.market.stock_access." +
                             runtime.AbsoluteDay + "." +
                             runtime.InventoryFlows.Count,
                        Day = runtime.AbsoluteDay,
                        OperationId = "market.stock_access",
                        ProductId = source.ProductId,
                        SourceInventoryId = source.Id,
                        DestinationInventoryId = destination.Id,
                        QuantityMilliunits = moved,
                        FacilityId = destination.FacilityId
                    });
                }
            }
        }

        private static long AllocatedFoodMarketDemand(
            long dailyDemand, int marketCount, int marketIndex)
        {
            if (marketCount <= 0 || marketIndex < 0 ||
                marketIndex >= marketCount) return 0;
            var share = dailyDemand / marketCount;
            return marketIndex == marketCount - 1
                ? dailyDemand - share * (marketCount - 1)
                : share;
        }

        private void ResolveHouseholdConsumption(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var formalEconomy = new LuoyangFormalEconomySystem();
            formalEconomy.SupplyHouseholdBucketFromMarkets(runtime,
                calibrationProfile.HouseholdMarketBufferDays);
            formalEconomy.SettleHouseholdConsumption(
                runtime, false, out var consumedTotal, out var shortageTotal);
            RecordHouseholdConsumptionFlow(runtime, consumedTotal,
                shortageTotal);
        }

        private static void SettleAllHouseholdConsumption(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            new LuoyangFormalEconomySystem().SettleHouseholdConsumption(
                runtime, true, out var consumed, out var shortage);
            RecordHouseholdConsumptionFlow(runtime, consumed, shortage);
        }

        private static void RecordHouseholdConsumptionFlow(
            Luoyang184LivingWorldRuntimeState runtime,
            long consumedTotal,
            long shortageTotal)
        {
            if (consumedTotal == 0 && shortageTotal == 0) return;
            runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
            {
                Id = "flow.consumption." + runtime.AbsoluteDay + "." +
                    runtime.InventoryFlows.Count,
                Day = runtime.AbsoluteDay,
                OperationId = "household.food_reserve_consumed",
                ProductId = "product.reference.food_equivalent",
                SourceInventoryId = "household.compact_reserves",
                QuantityMilliunits = consumedTotal,
                LossMilliunits = shortageTotal
            });
        }

        private static int AverageTransportCost(
            Luoyang184LivingWorldRuntimeState runtime, string productId)
        {
            var suppliers = runtime.ExternalSuppliers.Where(item =>
                item.ProductId == productId).ToList();
            return suppliers.Count == 0 ? 0 : (int)Math.Min(5_000,
                suppliers.Average(item => item.DistanceKilometers) * 10);
        }

        private static int AverageSupplyRisk(
            Luoyang184LivingWorldRuntimeState runtime, string productId)
        {
            var suppliers = runtime.ExternalSuppliers.Where(item =>
                item.ProductId == productId).ToList();
            return suppliers.Count == 0 ? 0 : (int)suppliers.Average(item =>
                item.RiskLossBasisPoints);
        }

        private static int SeasonalBasisPoints(long day)
        {
            var dayOfYear = (int)(day % 365);
            return dayOfYear >= 240 && dayOfYear <= 330 ? 11_000 :
                dayOfYear >= 90 && dayOfYear <= 180 ? 9_000 : 10_000;
        }

        private void ResolveShortageResponses(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            runtime.ShortageResponses.Clear();
            var demand = runtime.DailyFoodDemandMilliunits;
            var stock = TotalFoodStock(runtime);
            var level = ShortageFor(stock, demand);
            if (level != LuoyangShortageLevel.Normal)
                runtime.ShortageResponses.Add(new LuoyangShortageResponseState
                {
                    Id = "shortage.food." + runtime.AbsoluteDay,
                    SubjectKindId = "place",
                    SubjectId = "location.capital.luoyang",
                    ResourceId = "resource.food",
                    Level = level,
                    ResponseActionId = SupplyDependencyId,
                    DetectedDay = runtime.AbsoluteDay,
                    DeficitMilliunits = Math.Max(0, demand - stock)
                });
            foreach (var facility in runtime.Facilities)
            {
                if (facility.Status != LuoyangProductionRuntimeStatus.WaitingInput &&
                    facility.Status != LuoyangProductionRuntimeStatus.WaitingWorker &&
                    facility.Status != LuoyangProductionRuntimeStatus.OutputBlocked)
                    continue;
                runtime.ShortageResponses.Add(new LuoyangShortageResponseState
                {
                    Id = "shortage.facility." + runtime.AbsoluteDay + "." +
                         facility.FacilityIndex,
                    SubjectKindId = "facility",
                    SubjectId = facility.FacilityId,
                    ResourceId = facility.Status ==
                        LuoyangProductionRuntimeStatus.WaitingWorker
                        ? "resource.labor"
                        : facility.Status ==
                            LuoyangProductionRuntimeStatus.OutputBlocked
                            ? "resource.storage"
                            : facility.InputProductId,
                    Level = LuoyangShortageLevel.Shortage,
                    ResponseActionId = facility.AiResponseActionId,
                    DetectedDay = runtime.AbsoluteDay
                });
            }
        }

        private void ReconcilePersonConsumption(
            Luoyang184LivingWorldRuntimeState runtime,
            int simulatedDays,
            long[] householdConsumedBefore)
        {
            for (var householdIndex = 0;
                 householdIndex < runtime.Households.Count;
                 householdIndex++)
            {
                var household = runtime.Households[householdIndex];
                var consumedDelta = household.CumulativeFoodConsumedMilliunits -
                                    householdConsumedBefore[householdIndex];
                var totalDemandDelta = checked(
                    household.DailyFoodDemandMilliunits * simulatedDays);
                long allocated = 0;
                for (var offset = 0; offset < household.MemberCount; offset++)
                {
                    var person = runtime.Workforce[checked((int)
                        household.MemberStartOrdinal + offset)];
                    var personDemand = checked(
                        DailyPersonFoodDemand(person) * simulatedDays);
                    var personConsumed = totalDemandDelta <= 0
                        ? 0
                        : consumedDelta * personDemand / totalDemandDelta;
                    if (offset == household.MemberCount - 1)
                        personConsumed = consumedDelta - allocated;
                    allocated += personConsumed;
                    person.CumulativeFoodDemandMilliunits += personDemand;
                    person.CumulativeFoodConsumedMilliunits += personConsumed;
                }
            }
        }

        private void CaptureSnapshot(Luoyang184LivingWorldRuntimeState runtime)
        {
            var demand = runtime.DailyFoodDemandMilliunits;
            var consumed = runtime.Households.Sum(item =>
                item.CumulativeFoodConsumedMilliunits);
            var shortage = runtime.Households.Sum(item =>
                item.CumulativeFoodShortageMilliunits);
            runtime.DaySnapshots.RemoveAll(item => item.Day == runtime.AbsoluteDay);
            runtime.DaySnapshots.Add(new LuoyangLivingWorldDaySnapshotState
            {
                Day = runtime.AbsoluteDay,
                FoodStockMilliunits = TotalFoodStock(runtime),
                FoodDemandMilliunits = demand,
                FoodProducedMilliunits = runtime.InventoryFlows.Where(item =>
                    item.OperationId == "production.crop_harvest" &&
                    FoodProducts.Contains(item.ProductId)).Sum(item =>
                    item.QuantityMilliunits),
                FoodImportedMilliunits = runtime.InventoryFlows.Where(item =>
                    item.OperationId == "supply.shipment_delivered" &&
                    FoodProducts.Contains(item.ProductId)).Sum(item =>
                    item.QuantityMilliunits),
                FoodConsumedMilliunits = consumed,
                FoodLostMilliunits = runtime.InventoryFlows.Sum(item =>
                    item.LossMilliunits),
                FoodShortageMilliunits = shortage,
                ActiveProductionFacilities = runtime.Facilities.Count(item =>
                    item.Status == LuoyangProductionRuntimeStatus.InProgress ||
                    item.Status == LuoyangProductionRuntimeStatus.Completed),
                IdleDueWorker = runtime.Facilities.Count(item =>
                    item.Status == LuoyangProductionRuntimeStatus.WaitingWorker),
                IdleDueInput = runtime.Facilities.Count(item =>
                    item.Status == LuoyangProductionRuntimeStatus.WaitingInput),
                OutputBlocked = runtime.Facilities.Count(item =>
                    item.Status == LuoyangProductionRuntimeStatus.OutputBlocked),
                HouseholdShortageCount = runtime.Households.Count(item =>
                    item.CumulativeFoodShortageMilliunits > 0),
                HarvestableCrops = runtime.Crops.Count(item =>
                    item.Phase == LuoyangCropPhase.Harvestable),
                MatureCrops = runtime.Crops.Count(item =>
                    item.Phase == LuoyangCropPhase.Mature ||
                    item.Phase == LuoyangCropPhase.Harvested)
            });
            runtime.DaySnapshots.Sort((left, right) => left.Day.CompareTo(right.Day));
        }

        private LuoyangInventoryBalanceState GetOrCreateOutputInventory(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangFacilityProductionRuntimeState facility)
        {
            if (!string.IsNullOrEmpty(facility.OutputInventoryId))
                return runtime.Inventories.Find(item =>
                    item.Id == facility.OutputInventoryId);
            var storage = source.Facilities.Where(item => item.StorageCapacity > 0)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .First();
            var result = GetOrCreateInventory(runtime, storage.FacilityId,
                facility.OutputProductId, OwnerKind(facility.OwnerId),
                facility.OwnerId, false);
            facility.OutputInventoryId = result.Id;
            return result;
        }

        private LuoyangInventoryBalanceState GetOrCreateInventory(
            Luoyang184LivingWorldRuntimeState runtime,
            string facilityId,
            string productId,
            LuoyangInventoryOwnerKind ownerKind,
            string ownerId,
            bool transitional)
        {
            var id = "inventory.luoyang.184." + facilityId + "." + productId;
            var existing = runtime.Inventories.Find(item => item.Id == id);
            if (existing != null) return existing;
            var facility = source.Facilities.FirstOrDefault(item =>
                item.FacilityId == facilityId);
            existing = new LuoyangInventoryBalanceState
            {
                Id = id,
                OwnerKind = ownerKind,
                OwnerId = ownerId,
                FacilityId = facilityId,
                ProductId = productId,
                CapacityMilliunits = facility == null
                    ? 1_000_000L
                    : checked(Math.Max(0, facility.StorageCapacity) *
                        MilliunitsPerUnit),
                IsTransitionalReferenceSupply = transitional
            };
            runtime.Inventories.Add(existing);
            return existing;
        }

        private LuoyangInventoryBalanceState FindAvailableInventory(
            Luoyang184LivingWorldRuntimeState runtime,
            string productId,
            long required)
        {
            return runtime.Inventories.Where(item =>
                    item.ProductId == productId &&
                    item.QuantityMilliunits >= required)
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private long AvailableCapacity(
            Luoyang184LivingWorldRuntimeState runtime, string facilityId)
        {
            var sourceFacility = source.Facilities.FirstOrDefault(item =>
                item.FacilityId == facilityId);
            var calibrationScale = calibrationProfile
                .FoodStorageCapacityUnitScale;
            var capacity = sourceFacility == null
                ? Math.Max(1_000_000L, runtime.Inventories.Where(item =>
                    item.FacilityId == facilityId).Sum(item =>
                    item.CapacityMilliunits))
                : checked(Math.Max(0, sourceFacility.StorageCapacity) *
                    MilliunitsPerUnit * calibrationScale);
            var used = runtime.Inventories.Where(item =>
                item.FacilityId == facilityId).Sum(item => item.QuantityMilliunits);
            return Math.Max(0, capacity - used);
        }

        private static LuoyangWorkforceStatus DetermineWorkforceStatus(
            Luoyang184PermanentPersonRecord person,
            int age,
            string activity,
            string occupation)
        {
            if (age < 14 || age >= 70 || person.HealthBasisPoints <= 0)
                return LuoyangWorkforceStatus.NotEligible;
            if (HasCatalogReference(person.ForceIndex) ||
                HasCatalogReference(person.MilitaryOfficeIndex) ||
                activity.StartsWith("activity.military", StringComparison.Ordinal) ||
                occupation == "occupation.military")
                return LuoyangWorkforceStatus.MilitaryDuty;
            if (HasCatalogReference(person.CivilOfficeIndex) ||
                activity == "activity.work.government" ||
                occupation == "occupation.government")
                return LuoyangWorkforceStatus.Official;
            if (activity == "activity.study" ||
                occupation == "occupation.education.student")
                return LuoyangWorkforceStatus.Student;
            if (activity == "activity.work.family_management" ||
                occupation == "occupation.elite_family_management")
                return LuoyangWorkforceStatus.FamilyManagement;
            if (person.WorkFacilityIndex != uint.MaxValue)
                return LuoyangWorkforceStatus.Assigned;
            return LuoyangWorkforceStatus.Unemployed;
        }

        private static bool HasCatalogReference(ushort index) =>
            index != 0 && index != ushort.MaxValue;

        private static int CalculateEffectiveLabor(
            int healthBasisPoints,
            int age,
            LuoyangWorkforceStatus status)
        {
            if (status == LuoyangWorkforceStatus.NotEligible) return 0;
            var ageFactor = age < 20 ? 6_000 : age < 60 ? 10_000 : 6_500;
            var roleFactor = status == LuoyangWorkforceStatus.Assigned ||
                             status == LuoyangWorkforceStatus.Unemployed
                ? 10_000
                : 2_500;
            return Math.Max(0, Math.Min(10_000,
                healthBasisPoints * ageFactor / 10_000 * roleFactor / 10_000));
        }

        private static long DailyPersonFoodDemand(
            LuoyangWorkforceAssignmentState person)
        {
            var baseDemand = person.Age < 14 ? 650L :
                person.Age < 20 ? 850L : person.Age < 60 ? 1_000L : 800L;
            if (person.Status == LuoyangWorkforceStatus.MilitaryDuty)
                baseDemand = baseDemand * 12 / 10;
            else if (person.Status == LuoyangWorkforceStatus.Assigned &&
                     person.EffectiveLaborBasisPoints >= 8_000)
                baseDemand = baseDemand * 11 / 10;
            return baseDemand;
        }

        private static bool IsProductionFacility(
            Luoyang184LivingFacilitySourceRecord facility)
        {
            var id = facility.DefinitionId ?? string.Empty;
            return id.IndexOf("agriculture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("industry", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("workshop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("mill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("smith", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void MapRecipe(
            Luoyang184LivingFacilitySourceRecord facility,
            out string recipe,
            out string input,
            out string output,
            out long inputQuantity,
            out long outputQuantity)
        {
            recipe = input = output = string.Empty;
            inputQuantity = outputQuantity = 0;
            if ((facility.DefinitionId ?? string.Empty).IndexOf(
                    "agriculture", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                recipe = "recipe.field.luoyang_source_crop";
                return;
            }
            if ((facility.DefinitionId ?? string.Empty).IndexOf(
                    "workshop", StringComparison.OrdinalIgnoreCase) < 0 &&
                (facility.DefinitionId ?? string.Empty).IndexOf(
                    "industry", StringComparison.OrdinalIgnoreCase) < 0)
                return;
            switch (facility.FacilityIndex % 4)
            {
                case 0:
                    recipe = "recipe.processing.hand_mill_millet";
                    input = "product.food.millet_grain";
                    output = CoreProductionContent.WheatFlourProductId;
                    inputQuantity = 100_000;
                    outputQuantity = 85_000;
                    break;
                case 1:
                    recipe = "recipe.processing.weave_plain_cloth";
                    input = "product.material.craft_fiber";
                    output = CoreProductionContent.PlainClothProductId;
                    inputQuantity = 80_000;
                    outputQuantity = 60_000;
                    break;
                case 2:
                    recipe = "recipe.processing.carpentry_general_goods";
                    input = CoreProductionContent.TimberMaterialProductId;
                    output = "product.goods.general";
                    inputQuantity = 100_000;
                    outputQuantity = 75_000;
                    break;
                default:
                    recipe = "recipe.processing.sort_wool_hide";
                    input = "product.livestock.wool_and_hide";
                    output = CoreProductionContent.LeatherMaterialProductId;
                    inputQuantity = 100_000;
                    outputQuantity = 70_000;
                    break;
            }
        }

        private static int RecipeDurationDays(string recipeId)
        {
            if (recipeId.EndsWith("plain_cloth", StringComparison.Ordinal)) return 4;
            if (recipeId.EndsWith("general_goods", StringComparison.Ordinal)) return 3;
            if (recipeId.EndsWith("wool_hide", StringComparison.Ordinal)) return 3;
            return 2;
        }

        private static LuoyangInventoryOwnerKind OwnerKind(string ownerId)
        {
            if ((ownerId ?? string.Empty).IndexOf("government",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return LuoyangInventoryOwnerKind.Government;
            if ((ownerId ?? string.Empty).IndexOf("family_organization",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return LuoyangInventoryOwnerKind.FamilyOrganization;
            if ((ownerId ?? string.Empty).StartsWith("P", StringComparison.Ordinal))
                return LuoyangInventoryOwnerKind.Person;
            return LuoyangInventoryOwnerKind.Facility;
        }

        private static string SeedProductId(string cropProductId) =>
            "product.seed." + (cropProductId ?? string.Empty)
                .Replace("product.", string.Empty).Replace('.', '_');

        private static LuoyangShortageLevel ShortageFor(long stock, long demand)
        {
            if (demand <= 0 || stock >= demand * 30) return LuoyangShortageLevel.Normal;
            if (stock >= demand * 14) return LuoyangShortageLevel.Tight;
            if (stock >= demand * 7) return LuoyangShortageLevel.Shortage;
            if (stock >= demand) return LuoyangShortageLevel.SevereShortage;
            return LuoyangShortageLevel.Critical;
        }

        private static long TotalFoodStock(
            Luoyang184LivingWorldRuntimeState runtime) =>
            runtime.Inventories.Where(item => FoodProducts.Contains(item.ProductId))
                .Sum(item => item.QuantityMilliunits);

        private static bool IsEvidenceDay(long elapsed, int totalDays) =>
            elapsed == 1 || elapsed == 7 || elapsed == 30 || elapsed == 90 ||
            elapsed == 180 || elapsed == 365 || elapsed == totalDays;

        private static LuoyangLivingWorldDaySnapshotState CopySnapshot(
            LuoyangLivingWorldDaySnapshotState sourceSnapshot) =>
            new LuoyangLivingWorldDaySnapshotState
            {
                Day = sourceSnapshot.Day,
                FoodStockMilliunits = sourceSnapshot.FoodStockMilliunits,
                FoodDemandMilliunits = sourceSnapshot.FoodDemandMilliunits,
                FoodProducedMilliunits = sourceSnapshot.FoodProducedMilliunits,
                FoodImportedMilliunits = sourceSnapshot.FoodImportedMilliunits,
                FoodConsumedMilliunits = sourceSnapshot.FoodConsumedMilliunits,
                FoodLostMilliunits = sourceSnapshot.FoodLostMilliunits,
                FoodShortageMilliunits = sourceSnapshot.FoodShortageMilliunits,
                ActiveProductionFacilities = sourceSnapshot.ActiveProductionFacilities,
                IdleDueWorker = sourceSnapshot.IdleDueWorker,
                IdleDueInput = sourceSnapshot.IdleDueInput,
                OutputBlocked = sourceSnapshot.OutputBlocked,
                HouseholdShortageCount = sourceSnapshot.HouseholdShortageCount,
                HarvestableCrops = sourceSnapshot.HarvestableCrops,
                MatureCrops = sourceSnapshot.MatureCrops
            };
    }
}
