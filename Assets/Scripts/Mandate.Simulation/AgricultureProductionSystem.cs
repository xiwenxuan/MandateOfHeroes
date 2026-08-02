using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class AgricultureProductionAudit
    {
        public int ActiveOrders;
        public int CompletedOrders;
        public int InvalidOrders;
        public long InputCommitted;
        public long ProducedQuantity;
        public long StoredQuantity;
        public long LostQuantity;

        public bool IsBalanced =>
            InvalidOrders == 0 &&
            ProducedQuantity == StoredQuantity + LostQuantity;
    }

    public sealed class AgricultureProductionSystem
    {
        private const int SeasonLaborDaysPerLandUnit = 12;
        private const int LaborWindowDays = 60;
        private readonly NamedRandom _random;
        private readonly ProductionContentRegistry _content;
        private readonly ResearchSystem _research;

        public AgricultureProductionSystem(
            ulong masterSeed,
            ProductionContentRegistry content = null)
        {
            _random = new NamedRandom(masterSeed);
            _content = content ?? ProductionContentRegistry.CreateCore();
            _research = new ResearchSystem(_content);
        }

        public AgricultureWorkOrderState CreateOrder(
            WorldState world,
            string villageId,
            string familyId,
            string fieldFacilityId,
            string storageFacilityId,
            string managerPersonId,
            string cropDefinitionId,
            string cropVarietyDefinitionId,
            string recipeDefinitionId,
            string methodDefinitionId,
            ProductionControlMode controlMode,
            int landUnits,
            IList<string> assignedWorkerIds,
            long harvestDay)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (!Enum.IsDefined(typeof(ProductionControlMode), controlMode))
            {
                throw new ArgumentOutOfRangeException(nameof(controlMode));
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            var crop = _content.GetCrop(cropDefinitionId);
            var variety = _content.GetCropVariety(cropVarietyDefinitionId);
            var recipe = _content.GetRecipe(recipeDefinitionId);
            var method = _content.GetMethod(methodDefinitionId);
            if (variety.CropDefinitionId != crop.Id ||
                recipe.CropDefinitionId != crop.Id ||
                !method.RecipeDefinitionIds.Contains(recipe.Id) ||
                recipe.Inputs.Count != 1 || recipe.Outputs.Count != 1)
            {
                throw new InvalidOperationException(
                    "Agriculture content definitions are incompatible.");
            }

            var input = recipe.Inputs[0];
            var output = recipe.Outputs[0];
            var seedProduct = _content.GetProduct(input.ProductDefinitionId);
            var harvestProduct = _content.GetProduct(output.ProductDefinitionId);
            if (seedProduct.UnitId != harvestProduct.UnitId ||
                harvestDay - world.AbsoluteDay < recipe.DurationDays)
            {
                throw new InvalidOperationException(
                    "Agriculture order does not satisfy recipe units or duration.");
            }

            if (landUnits <= 0 || harvestDay <= world.AbsoluteDay)
            {
                throw new ArgumentOutOfRangeException(nameof(landUnits));
            }

            var village = FindVillage(world, villageId);
            var family = FindFamily(world, familyId);
            var field = FindFacility(world, fieldFacilityId);
            var storage = FindFacility(world, storageFacilityId);
            var manager = FindPerson(world, managerPersonId);
            if (family.VillageId != village.Id ||
                family.LocationId != village.LocationId ||
                field.VillageId != village.Id ||
                field.Kind != VillageFacilityKind.Farmland ||
                storage.VillageId != village.Id ||
                storage.Kind != VillageFacilityKind.HouseholdGranary ||
                storage.OwnerFamilyId != family.Id ||
                manager.FamilyId != family.Id ||
                !manager.IsAlive ||
                manager.LocationId != village.LocationId ||
                landUnits > family.FarmlandUnits ||
                landUnits > field.Capacity)
            {
                throw new InvalidOperationException(
                    "Agriculture order references an unavailable input.");
            }

            for (var i = 0; i < world.AgricultureWorkOrders.Count; i++)
            {
                var active = world.AgricultureWorkOrders[i];
                if (active.Status == ProductionOrderStatus.Active &&
                    active.FamilyId == family.Id)
                {
                    throw new InvalidOperationException(
                        $"Family {family.Id} already has active agriculture work.");
                }
            }

            var seedNeeded = checked(
                landUnits * input.QuantityPerLandUnit);
            if (family.SeedGrain < seedNeeded)
            {
                throw new InvalidOperationException(
                    $"Family {family.Id} lacks seed grain.");
            }

            if (assignedWorkerIds == null || assignedWorkerIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "Agriculture work requires at least one worker.");
            }

            var workers = new List<string>(assignedWorkerIds);
            workers.Sort(StringComparer.Ordinal);
            var uniqueWorkers = new HashSet<string>(StringComparer.Ordinal);
            var assignedLaborDays = 0;
            for (var workerIndex = 0; workerIndex < workers.Count; workerIndex++)
            {
                var worker = FindPerson(world, workers[workerIndex]);
                if (!uniqueWorkers.Add(worker.Id) ||
                    !worker.IsAlive ||
                    worker.FamilyId != family.Id ||
                    worker.LocationId != village.LocationId ||
                    worker.LocalDuty != LocalDutyKind.None ||
                    worker.LaborCapacityBasisPoints <= 0 ||
                    HasOverlappingWork(world, worker.Id))
                {
                    throw new InvalidOperationException(
                        $"Worker {worker.Id} is unavailable for agriculture work.");
                }

                assignedLaborDays = checked(
                    assignedLaborDays + Math.Max(
                        1,
                        worker.LaborCapacityBasisPoints * LaborWindowDays / 10_000));
            }

            storage.InventoryUnits = family.Grain + family.SeedGrain;
            var technologyFactors = _research.ResolveProductionFactors(
                world,
                field.Id,
                recipe.Id,
                method.Id);
            family.SeedGrain -= seedNeeded;
            storage.InventoryUnits -= seedNeeded;
            family.CultivatedLandUnits = landUnits;
            family.PlantedSeedGrain = seedNeeded;
            var order = new AgricultureWorkOrderState
            {
                Id = $"agriculture.{world.AbsoluteDay}.{world.AgricultureWorkOrders.Count:D6}",
                VillageId = village.Id,
                FamilyId = family.Id,
                FieldFacilityId = field.Id,
                StorageFacilityId = storage.Id,
                ManagerPersonId = manager.Id,
                CropDefinitionId = crop.Id,
                CropVarietyDefinitionId = variety.Id,
                RecipeDefinitionId = recipe.Id,
                MethodDefinitionId = method.Id,
                SeedProductDefinitionId = seedProduct.Id,
                HarvestProductDefinitionId = harvestProduct.Id,
                UnitId = seedProduct.UnitId,
                ControlMode = controlMode,
                Status = ProductionOrderStatus.Active,
                CreatedDay = world.AbsoluteDay,
                PlantingDay = world.AbsoluteDay,
                HarvestDay = harvestDay,
                LandUnits = landUnits,
                SeedQuantityCommitted = seedNeeded,
                RequiredLaborDays = checked((int)ApplyFactor(
                    ApplyFactor(
                        landUnits * SeasonLaborDaysPerLandUnit,
                        method.LaborBasisPoints),
                    technologyFactors.LaborBasisPoints)),
                AssignedLaborDays = assignedLaborDays,
                TechnologyYieldBasisPoints =
                    technologyFactors.YieldBasisPoints,
                TechnologyLaborBasisPoints =
                    technologyFactors.LaborBasisPoints,
                AssignedWorkerIds = workers,
                AppliedTechnologyIds = new List<string>(
                    technologyFactors.AppliedTechnologyIds)
            };
            world.AgricultureWorkOrders.Add(order);
            AddProductionLedger(
                world,
                order,
                ProductionLedgerEntryType.InputCommitted,
                storage.Id,
                manager.Id,
                seedProduct.Id,
                seedProduct.UnitId,
                seedNeeded,
                -seedNeeded,
                0,
                -seedNeeded,
                $"{family.DisplayName} committed {seedNeeded} seed grain.");
            AddProductionLedger(
                world,
                order,
                ProductionLedgerEntryType.LaborCommitted,
                field.Id,
                manager.Id,
                string.Empty,
                CoreProductionContent.LaborDayUnitId,
                assignedLaborDays,
                0,
                0,
                0,
                $"{family.DisplayName} committed {assignedLaborDays} labor-days.");
            AddVillageLedger(
                world,
                order,
                VillageLedgerEntryType.Planting,
                0,
                (int)Math.Min(int.MaxValue, seedNeeded),
                $"{family.DisplayName} planted {landUnits} land units of " +
                $"{variety.DisplayName}.");
            return order;
        }

        public void CreateDelegatedSeasonOrders(
            WorldState world,
            VillageState village,
            long harvestDay)
        {
            _content.ValidateManifest(world.ProductionContentManifest);
            var recipe = _content.GetRecipe(
                CoreProductionContent.GrowWheatRecipeId);
            var seedPerLandUnit = recipe.Inputs[0].QuantityPerLandUnit;
            var field = FindFacility(
                world, village.Id, VillageFacilityKind.Farmland);
            var families = FamiliesForVillage(world, village);
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var landUnits = Math.Min(
                    family.FarmlandUnits,
                    (int)Math.Min(
                        int.MaxValue,
                        family.SeedGrain / seedPerLandUnit));
                var storage = FindHouseholdGranary(world, village.Id, family.Id);
                var workers = AvailableFamilyWorkers(world, village, family);
                if (landUnits <= 0 || storage == null || workers.Count == 0)
                {
                    continue;
                }

                CreateOrder(
                    world,
                    village.Id,
                    family.Id,
                    field.Id,
                    storage.Id,
                    workers[0],
                    CoreProductionContent.WheatCropId,
                    CoreProductionContent.PrototypeNorthernWheatVarietyId,
                    CoreProductionContent.GrowWheatRecipeId,
                    CoreProductionContent.PrototypeDrylandMethodId,
                    ProductionControlMode.DelegatedPolicy,
                    landUnits,
                    workers,
                    harvestDay);
            }
        }

        public void ResolveDueOrders(WorldState world, string villageId)
        {
            var due = new List<AgricultureWorkOrderState>();
            for (var i = 0; i < world.AgricultureWorkOrders.Count; i++)
            {
                var order = world.AgricultureWorkOrders[i];
                if (order.VillageId == villageId &&
                    order.Status == ProductionOrderStatus.Active &&
                    order.HarvestDay <= world.AbsoluteDay)
                {
                    due.Add(order);
                }
            }

            due.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < due.Count; i++)
            {
                ResolveOrder(world, due[i]);
            }
        }

        public AgricultureProductionAudit Audit(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            var audit = new AgricultureProductionAudit();
            for (var i = 0; i < world.AgricultureWorkOrders.Count; i++)
            {
                var order = world.AgricultureWorkOrders[i];
                audit.InputCommitted += order.SeedQuantityCommitted;
                if (order.Status == ProductionOrderStatus.Active)
                {
                    audit.ActiveOrders++;
                    continue;
                }

                if (order.Status != ProductionOrderStatus.Completed)
                {
                    continue;
                }

                audit.CompletedOrders++;
                audit.ProducedQuantity += order.ProducedQuantity;
                audit.StoredQuantity += order.StoredQuantity;
                audit.LostQuantity += order.LostQuantity;
                if (order.ProducedQuantity !=
                        order.StoredQuantity + order.LostQuantity ||
                    LedgerQuantity(
                        world, order.Id,
                        ProductionLedgerEntryType.InputCommitted,
                        order.SeedProductDefinitionId) !=
                    order.SeedQuantityCommitted ||
                    LedgerQuantity(
                        world, order.Id,
                        ProductionLedgerEntryType.LaborCommitted,
                        string.Empty) !=
                    order.AssignedLaborDays ||
                    LedgerQuantity(
                        world, order.Id,
                        ProductionLedgerEntryType.ProductStored,
                        order.HarvestProductDefinitionId) !=
                    order.StoredQuantity ||
                    LedgerQuantity(
                        world, order.Id,
                        ProductionLedgerEntryType.ProductLost,
                        order.HarvestProductDefinitionId) !=
                    order.LostQuantity)
                {
                    audit.InvalidOrders++;
                }
            }

            return audit;
        }

        private void ResolveOrder(WorldState world, AgricultureWorkOrderState order)
        {
            _content.ValidateManifest(world.ProductionContentManifest);
            var recipe = _content.GetRecipe(order.RecipeDefinitionId);
            var method = _content.GetMethod(order.MethodDefinitionId);
            var input = recipe.Inputs[0];
            var output = recipe.Outputs[0];
            if (input.ProductDefinitionId != order.SeedProductDefinitionId ||
                output.ProductDefinitionId != order.HarvestProductDefinitionId ||
                !method.RecipeDefinitionIds.Contains(recipe.Id))
            {
                throw new InvalidOperationException(
                    $"Production content changed for active order {order.Id}.");
            }

            var village = FindVillage(world, order.VillageId);
            var family = FindFamily(world, order.FamilyId);
            var field = FindFacility(world, order.FieldFacilityId);
            var storage = FindFacility(world, order.StorageFacilityId);
            var irrigation = FindFacility(
                world, village.Id, VillageFacilityKind.Irrigation);
            var manager = FindPerson(world, order.ManagerPersonId);
            var laborFactor = Math.Min(
                10_000,
                order.AssignedLaborDays * 10_000 /
                Math.Max(1, order.RequiredLaborDays));
            long workerSkill = 0;
            for (var i = 0; i < order.AssignedWorkerIds.Count; i++)
            {
                workerSkill += FindPerson(world, order.AssignedWorkerIds[i])
                    .ProfessionalSkills.Agriculture;
            }

            var averageWorkerSkill = (int)(
                workerSkill / Math.Max(1, order.AssignedWorkerIds.Count));
            var managerSkill = manager.ProfessionalSkills.Agriculture;
            var skillFactor = 5_000 +
                (averageWorkerSkill * 3 + managerSkill * 2) / 10;
            var irrigationFactor = irrigation == null
                ? 7_000
                : irrigation.ConditionBasisPoints;
            var weatherFactor = _random.Range(
                "agriculture_harvest",
                new StableId(order.Id),
                order.HarvestDay,
                "weather",
                8_500,
                11_001);
            long harvest = checked(
                order.LandUnits * output.QuantityPerLandUnit);
            harvest = ApplyFactor(harvest, method.YieldBasisPoints);
            harvest = ApplyFactor(
                harvest, order.TechnologyYieldBasisPoints);
            harvest = ApplyFactor(harvest, laborFactor);
            harvest = ApplyFactor(harvest, skillFactor);
            harvest = ApplyFactor(harvest, field.ConditionBasisPoints);
            harvest = ApplyFactor(harvest, irrigationFactor);
            harvest = ApplyFactor(harvest, family.ToolConditionBasisPoints);
            harvest = ApplyFactor(harvest, FindLocation(
                world, village.LocationId).PublicOrderBasisPoints);
            harvest = ApplyFactor(harvest, weatherFactor);

            storage.InventoryUnits = family.Grain + family.SeedGrain;
            var availableCapacity = Math.Max(
                0L, storage.Capacity - storage.InventoryUnits);
            var stored = Math.Min(harvest, availableCapacity);
            var lost = harvest - stored;
            var seedSaved = Math.Min(
                stored / 8,
                order.LandUnits * input.QuantityPerLandUnit);
            var foodStored = stored - seedSaved;
            family.SeedGrain += seedSaved;
            family.Grain += foodStored;
            family.LastHarvestGrain = stored;
            family.CultivatedLandUnits = 0;
            family.PlantedSeedGrain = 0;
            storage.InventoryUnits += stored;
            order.Status = ProductionOrderStatus.Completed;
            order.SettledDay = world.AbsoluteDay;
            order.ProducedQuantity = harvest;
            order.StoredQuantity = stored;
            order.LostQuantity = lost;

            AddProductionLedger(
                world,
                order,
                ProductionLedgerEntryType.ProductStored,
                storage.Id,
                manager.Id,
                order.HarvestProductDefinitionId,
                order.UnitId,
                stored,
                seedSaved,
                foodStored,
                stored,
                $"{family.DisplayName} stored {stored} units from " +
                $"{order.CropVarietyDefinitionId}.");
            AddProductionLedger(
                world,
                order,
                ProductionLedgerEntryType.ProductLost,
                storage.Id,
                manager.Id,
                order.HarvestProductDefinitionId,
                order.UnitId,
                lost,
                0,
                0,
                0,
                $"{family.DisplayName} lost {lost} grain to insufficient storage.");
            AddVillageLedger(
                world,
                order,
                VillageLedgerEntryType.Harvest,
                foodStored,
                (int)Math.Min(int.MaxValue, harvest),
                $"{family.DisplayName} harvested {harvest}, stored {stored}, lost {lost}.");
        }

        private static long ApplyFactor(long value, int factorBasisPoints)
        {
            return checked(value * Math.Max(0, factorBasisPoints) / 10_000);
        }

        private static bool HasOverlappingWork(WorldState world, string personId)
        {
            for (var i = 0; i < world.AgricultureWorkOrders.Count; i++)
            {
                var order = world.AgricultureWorkOrders[i];
                if (order.Status == ProductionOrderStatus.Active &&
                    order.AssignedWorkerIds.Contains(personId))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> AvailableFamilyWorkers(
            WorldState world,
            VillageState village,
            FamilyState family)
        {
            var result = new List<string>();
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var person = FindPerson(world, family.MemberIds[i]);
                if (person.IsAlive &&
                    person.LocationId == village.LocationId &&
                    person.LocalDuty == LocalDutyKind.None &&
                    person.LaborCapacityBasisPoints > 0 &&
                    !HasOverlappingWork(world, person.Id))
                {
                    result.Add(person.Id);
                }
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static List<FamilyState> FamiliesForVillage(
            WorldState world,
            VillageState village)
        {
            var result = new List<FamilyState>();
            for (var i = 0; i < village.HouseholdIds.Count; i++)
            {
                result.Add(FindFamily(world, village.HouseholdIds[i]));
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static long LedgerQuantity(
            WorldState world,
            string orderId,
            ProductionLedgerEntryType type,
            string productDefinitionId)
        {
            long total = 0;
            for (var i = 0; i < world.ProductionLedgerEntries.Count; i++)
            {
                var entry = world.ProductionLedgerEntries[i];
                if (entry.WorkOrderId == orderId && entry.Type == type &&
                    entry.ProductDefinitionId == productDefinitionId)
                {
                    total += entry.Quantity;
                }
            }

            return total;
        }

        private static void AddProductionLedger(
            WorldState world,
            AgricultureWorkOrderState order,
            ProductionLedgerEntryType type,
            string facilityId,
            string personId,
            string productDefinitionId,
            string unitId,
            long quantity,
            long familySeedDelta,
            long familyGrainDelta,
            long facilityInventoryDelta,
            string summary)
        {
            world.ProductionLedgerEntries.Add(new ProductionLedgerEntryState
            {
                Id = $"production_ledger.{world.AbsoluteDay}." +
                     $"{world.ProductionLedgerEntries.Count:D6}",
                WorkOrderId = order.Id,
                VillageId = order.VillageId,
                FamilyId = order.FamilyId,
                FacilityId = facilityId,
                PersonId = personId,
                ProductDefinitionId = productDefinitionId,
                UnitId = unitId,
                Day = world.AbsoluteDay,
                Type = type,
                Quantity = quantity,
                FamilySeedGrainDelta = familySeedDelta,
                FamilyGrainDelta = familyGrainDelta,
                FacilityInventoryDelta = facilityInventoryDelta,
                Summary = summary
            });
        }

        private static void AddVillageLedger(
            WorldState world,
            AgricultureWorkOrderState order,
            VillageLedgerEntryType type,
            long familyGrainDelta,
            int quantity,
            string summary)
        {
            world.VillageLedgerEntries.Add(new VillageLedgerEntryState
            {
                Id = $"village_ledger.{world.AbsoluteDay}." +
                     $"{world.VillageLedgerEntries.Count:D6}",
                Day = world.AbsoluteDay,
                Type = type,
                VillageId = order.VillageId,
                FamilyId = order.FamilyId,
                PersonId = order.ManagerPersonId,
                FamilyGrainDelta = familyGrainDelta,
                PublicGrainDelta = 0,
                Quantity = quantity,
                Summary = summary
            });
        }

        private static VillageState FindVillage(WorldState world, string id)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                if (world.Villages[i].Id == id)
                {
                    return world.Villages[i];
                }
            }

            throw new InvalidOperationException($"Missing village {id}.");
        }

        private static FamilyState FindFamily(WorldState world, string id)
        {
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == id)
                {
                    return world.Families[i];
                }
            }

            throw new InvalidOperationException($"Missing family {id}.");
        }

        private static PersonState FindPerson(WorldState world, string id)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == id)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {id}.");
        }

        private static VillageFacilityState FindFacility(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                if (world.VillageFacilities[i].Id == id)
                {
                    return world.VillageFacilities[i];
                }
            }

            throw new InvalidOperationException($"Missing facility {id}.");
        }

        private static VillageFacilityState FindFacility(
            WorldState world,
            string villageId,
            VillageFacilityKind kind)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                if (facility.VillageId == villageId && facility.Kind == kind)
                {
                    return facility;
                }
            }

            return null;
        }

        private static VillageFacilityState FindHouseholdGranary(
            WorldState world,
            string villageId,
            string familyId)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                if (facility.VillageId == villageId &&
                    facility.Kind == VillageFacilityKind.HouseholdGranary &&
                    facility.OwnerFamilyId == familyId)
                {
                    return facility;
                }
            }

            return null;
        }

        private static LocationState FindLocation(WorldState world, string id)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == id)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException($"Missing location {id}.");
        }
    }
}
