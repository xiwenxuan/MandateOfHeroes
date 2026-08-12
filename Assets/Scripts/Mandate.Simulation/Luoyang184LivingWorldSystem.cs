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

        private static readonly HashSet<string> FoodProducts =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "product.food.millet_grain",
                "product.food.wheat_grain",
                "product.food.broomcorn_grain",
                "product.food.bean",
                CoreProductionContent.WheatGrainProductId,
                CoreProductionContent.WheatFlourProductId,
                CoreProductionContent.DryRationProductId,
                CoreProductionContent.FreshMuttonProductId,
                CoreProductionContent.OffalProductId
            };

        public Luoyang184LivingWorldSystem(ILuoyang184LivingWorldSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
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
            BuildFacilityRuntime(runtime);
            BuildOpeningInventories(runtime);
            BuildCrops(runtime);
            BuildMarkets(runtime);
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
                var productionTimer = Stopwatch.StartNew();
                ResolveProduction(runtime);
                runtime.Performance.ProductionMilliseconds +=
                    productionTimer.ElapsedMilliseconds;

                var marketTimer = Stopwatch.StartNew();
                UpdateMarkets(runtime);
                runtime.Performance.MarketMilliseconds += marketTimer.ElapsedMilliseconds;

                var consumptionTimer = Stopwatch.StartNew();
                ResolveHouseholdConsumption(runtime);
                runtime.Performance.ConsumptionMilliseconds +=
                    consumptionTimer.ElapsedMilliseconds;
                ResolveShortageResponses(runtime);

                if (IsEvidenceDay(runtime.AbsoluteDay - startingDay, days))
                    CaptureSnapshot(runtime);
            }
            ReconcilePersonConsumption(runtime, days, householdConsumedBefore);
            var elapsed = totalTimer.ElapsedMilliseconds;
            if (days == 1) runtime.Performance.OneDayMilliseconds = elapsed;
            if (days == 7) runtime.Performance.SevenDayMilliseconds = elapsed;
            if (days == 30) runtime.Performance.ThirtyDayMilliseconds = elapsed;
            if (days == 365)
                runtime.Performance.ThreeHundredSixtyFiveDayMilliseconds = elapsed;
            runtime.Performance.PeakManagedMemoryBytes = Math.Max(
                runtime.Performance.PeakManagedMemoryBytes, GC.GetTotalMemory(false));
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
            return Harvest(runtime, crop, out harvestedMilliunits);
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
                item.OperationId == "supply.reference_arrival" &&
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
                SupplyRegionDependency = demand > 0 &&
                    localProduction / Math.Max(1, runtime.AbsoluteDay) < demand,
                SupplyStatusId = SupplyDependencyId,
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
                        HouseholdOrdinal = household.Ordinal,
                        HeadPersonOrdinal = household.HeadOrdinal,
                        MemberStartOrdinal = household.MemberStartOrdinal,
                        MemberCount = household.MemberCount,
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
                        : "facility.monitor_plan"
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
                    true);
                var quantity = checked(chain.DeliveredUnits * MilliunitsPerUnit);
                var available = AvailableCapacity(runtime, facility.FacilityId);
                var stored = Math.Min(quantity, available);
                inventory.QuantityMilliunits += stored;
                runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                {
                    Id = chain.ChainId + ".opening",
                    Day = runtime.AbsoluteDay,
                    OperationId = "supply.reference_arrival",
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
                    PlantingDay = field.PlantedDay,
                    FullMaturityDay = field.MaturityDay,
                    CycleDurationDays = field.MaturityDay - field.PlantedDay,
                    EarlyHarvestMinimumBasisPoints =
                        field.EarlyHarvestMinimumBasisPoints,
                    MaturityBasisPoints = Luoyang184LivingWorldRules
                        .CalculateMaturityBasisPoints(runtime.AbsoluteDay,
                            field.PlantedDay, field.MaturityDay),
                    FullYieldMilliunits = checked(
                        field.FullYieldUnits * MilliunitsPerUnit),
                    AssignedWorkers = field.WorkerPersonOrdinals.Count,
                    Phase = LuoyangCropPhase.Growing
                });
            }
        }

        private void BuildMarkets(Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var productId in FoodProducts.OrderBy(item => item,
                         StringComparer.Ordinal))
                runtime.Markets.Add(new LuoyangMarketRuntimeState
                {
                    ProductId = productId,
                    BasePrice = 1,
                    CurrentPriceBasisPoints = 10_000
                });
        }

        private void ResolveProduction(Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var crop in runtime.Crops.OrderBy(item => item.FieldId,
                         StringComparer.Ordinal))
            {
                if (crop.Phase == LuoyangCropPhase.Harvested)
                {
                    if (runtime.AbsoluteDay < crop.NextPlantingDay) continue;
                    if (!TrySowNextCycle(runtime, crop)) continue;
                }
                crop.MaturityBasisPoints = Luoyang184LivingWorldRules
                    .CalculateMaturityBasisPoints(runtime.AbsoluteDay,
                        crop.PlantingDay, crop.FullMaturityDay);
                crop.Phase = crop.MaturityBasisPoints < 8_000
                    ? LuoyangCropPhase.Growing
                    : crop.MaturityBasisPoints < 10_000
                        ? LuoyangCropPhase.Harvestable
                        : crop.MaturityBasisPoints <= 11_000
                            ? LuoyangCropPhase.Mature
                            : LuoyangCropPhase.AtRisk;
                if (crop.MaturityBasisPoints >= 10_000)
                    Harvest(runtime, crop, out _);
            }

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
                input.QuantityMilliunits -= facility.InputQuantity;
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
            inventory.QuantityMilliunits += stored;
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
            var dailyDemand = runtime.Households.Sum(item =>
                item.DailyFoodDemandMilliunits);
            foreach (var market in runtime.Markets)
            {
                var stock = runtime.Inventories.Where(item =>
                    item.ProductId == market.ProductId &&
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market)
                    .Sum(item => item.QuantityMilliunits);
                market.SupplyMilliunits = stock;
                market.DemandMilliunits = dailyDemand;
                market.TransferredMilliunits = 0;
                market.FailedDemandMilliunits = Math.Max(0, dailyDemand - stock);
                market.CurrentPriceBasisPoints = dailyDemand <= 0
                    ? 10_000
                    : 10_000 + (int)Math.Min(20_000,
                        market.FailedDemandMilliunits * 20_000 / dailyDemand);
            }
        }

        private void ResolveHouseholdConsumption(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var food = runtime.Inventories.Where(item =>
                    FoodProducts.Contains(item.ProductId) &&
                    item.QuantityMilliunits > 0)
                .OrderBy(item => item.OwnerKind)
                .ThenBy(item => item.Id, StringComparer.Ordinal).ToList();
            var dailyFlows = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var household in runtime.Households)
            {
                var demand = household.DailyFoodDemandMilliunits;
                var remaining = demand;
                var sourceId = "acquisition.failed";
                foreach (var inventory in food)
                {
                    if (remaining <= 0) break;
                    if (inventory.QuantityMilliunits <= 0) continue;
                    var quantity = Math.Min(remaining, inventory.QuantityMilliunits);
                    inventory.QuantityMilliunits -= quantity;
                    remaining -= quantity;
                    sourceId = inventory.OwnerKind == LuoyangInventoryOwnerKind.Government
                        ? "acquisition.government_relief"
                        : inventory.OwnerKind == LuoyangInventoryOwnerKind.Market
                            ? "acquisition.market_purchase"
                            : "acquisition.local_distribution";
                    var key = inventory.Id + "\n" + inventory.ProductId;
                    dailyFlows.TryGetValue(key, out var transferred);
                    dailyFlows[key] = transferred + quantity;
                }
                var consumed = demand - remaining;
                household.CumulativeFoodDemandMilliunits += demand;
                household.CumulativeFoodAcquiredMilliunits += consumed;
                household.CumulativeFoodConsumedMilliunits += consumed;
                household.CumulativeFoodShortageMilliunits += remaining;
                household.FoodSecurityBasisPoints = demand <= 0
                    ? 10_000
                    : (int)Math.Min(10_000, consumed * 10_000 / demand);
                household.LastAcquisitionSourceId = sourceId;
                household.AiResponseActionId = remaining <= 0
                    ? "household.consume_and_monitor"
                    : household.CumulativeFoodShortageMilliunits > demand * 7
                        ? "household.seek_relief_or_migration"
                        : "household.seek_market_or_relief";
            }
            foreach (var pair in dailyFlows.OrderBy(item => item.Key,
                         StringComparer.Ordinal))
            {
                var split = pair.Key.Split(
                    new[] { '\n' }, StringSplitOptions.None);
                runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                {
                    Id = "flow.consumption." + runtime.AbsoluteDay + "." +
                         runtime.InventoryFlows.Count,
                    Day = runtime.AbsoluteDay,
                    OperationId = "household.food_consumed",
                    ProductId = split[1],
                    SourceInventoryId = split[0],
                    QuantityMilliunits = pair.Value
                });
            }
        }

        private void ResolveShortageResponses(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            runtime.ShortageResponses.Clear();
            var demand = runtime.Households.Sum(item => item.DailyFoodDemandMilliunits);
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
            var demand = runtime.Households.Sum(item => item.DailyFoodDemandMilliunits);
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
                    item.OperationId == "supply.reference_arrival" &&
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
            var facility = source.Facilities.First(item =>
                item.FacilityId == facilityId);
            existing = new LuoyangInventoryBalanceState
            {
                Id = id,
                OwnerKind = ownerKind,
                OwnerId = ownerId,
                FacilityId = facilityId,
                ProductId = productId,
                CapacityMilliunits = checked(
                    Math.Max(0, facility.StorageCapacity) * MilliunitsPerUnit),
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
            var sourceFacility = source.Facilities.First(item =>
                item.FacilityId == facilityId);
            var capacity = checked(
                Math.Max(0, sourceFacility.StorageCapacity) * MilliunitsPerUnit);
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
