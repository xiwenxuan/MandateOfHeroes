using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HerbalMedicineSupplySystem
    {
        private const long ExtractionLot = 20;
        private const int ProcessingRunCount = 5;
        private const long MarketLot = 10;
        private const long MedicineReorderPoint = 20;
        private const long MarketUnitPrice = 100;

        private readonly ProductionContentRegistry _content;
        private readonly IPersonRepository _people;
        private readonly UpstreamResourceProductionSystem _extraction;
        private readonly ProcessingProductionSystem _processing;
        private readonly FormalCountyMarketSystem _market;

        public HerbalMedicineSupplySystem(
            ProductionContentRegistry content = null,
            IPersonRepository people = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
            _people = people;
            _extraction = new UpstreamResourceProductionSystem(
                _content, people);
            _processing = new ProcessingProductionSystem(_content, people);
            _market = new FormalCountyMarketSystem(_content);
        }

        public static void InitializePrototype(
            WorldState world,
            VillageState village)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (village == null)
            {
                throw new ArgumentNullException(nameof(village));
            }

            var resourceId = ResourceBodyId(village.Id);
            if (!world.ResourceBodies.Exists(item => item.Id == resourceId))
            {
                world.ResourceBodies.Add(new ResourceBodyState
                {
                    Id = resourceId,
                    ResourceKindId =
                        "resource_kind.wild_medicinal_plant_stand",
                    OutputProductDefinitionId =
                        CoreProductionContent.RawMedicinalPlantProductId,
                    LocationId = village.LocationId,
                    Provenance = "historical_inference",
                    GenerationRuleVersion =
                        "resource_rules.village_medicinal_plants.1",
                    RequiredFacilityTag =
                        CoreProductionContent.HerbGatheringFacilityTag,
                    InitialQuantity = 5_000,
                    RemainingQuantity = 5_000,
                    QualityBasisPoints = 7_500,
                    ExtractionDifficultyBasisPoints = 8_000
                });
            }

            var collector = FindCollectorFamily(world, village);
            var storage = FindHouseholdGranary(world, collector.Id);
            AddCapability(
                storage, CoreProductionContent.HerbGatheringFacilityTag);
            AddCapability(
                storage, CoreProductionContent.HerbDryingFacilityTag);
        }

        public void ResolveDaily(WorldState world)
        {
            ProductInventorySystem.RequireWorld(world);
            if (world.FoodInventoryAuthorityMode !=
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                return;
            }

            var villages = new List<VillageState>(world.Villages);
            villages.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < villages.Count; i++)
            {
                ResolveVillage(world, villages[i]);
            }
        }

        private void ResolveVillage(WorldState world, VillageState village)
        {
            InitializePrototype(world, village);
            var collector = FindCollectorFamily(world, village);
            var collectorStorage = FindHouseholdGranary(world, collector.Id);
            var collectorManager = Person(world, collector.HeadPersonId);
            var resource = FindResource(world, ResourceBodyId(village.Id));

            var rawAvailable = AvailableQuantity(
                world,
                collector.Id,
                collectorStorage.Id,
                CoreProductionContent.RawMedicinalPlantProductId);
            if (rawAvailable < ExtractionLot &&
                resource.RemainingQuantity - resource.ReservedQuantity >=
                    ExtractionLot &&
                !HasActiveExtraction(world, resource.Id) &&
                collectorStorage.InventoryUnits + ExtractionLot <=
                    collectorStorage.Capacity)
            {
                var worker = FindFamilyWorker(
                    world, collector, village.LocationId);
                if (worker != null)
                {
                    _extraction.CreateFamilyOrder(
                        world,
                        resource.Id,
                        collector.Id,
                        collectorStorage.Id,
                        collectorManager.Id,
                        new[] { worker.Id },
                        ProductionControlMode.DelegatedPolicy,
                        ExtractionLot);
                }
            }

            rawAvailable = AvailableQuantity(
                world,
                collector.Id,
                collectorStorage.Id,
                CoreProductionContent.RawMedicinalPlantProductId);
            if (rawAvailable >= ProcessingRunCount &&
                !HasActiveProcessing(world, collector.Id))
            {
                _processing.CreateOrder(
                    world,
                    CoreProductionContent.DryMedicinalPlantsRecipeId,
                    CoreProductionContent.HerbalDryingMethodId,
                    collector.Id,
                    collectorStorage.Id,
                    collectorManager.Id,
                    ProductionControlMode.DelegatedPolicy,
                    ProcessingRunCount);
            }

            var medicineAvailable = AvailableQuantity(
                world,
                collector.Id,
                collectorStorage.Id,
                CoreProductionContent.HerbalMedicineMaterialProductId);
            var governance = FindGovernance(world, village.ParentLocationId);
            if (medicineAvailable >= MarketLot &&
                !HasActiveMarketOrder(
                    world,
                    collector.Id,
                    CoreProductionContent.HerbalMedicineMaterialProductId,
                    FormalMarketOrderSide.Sell))
            {
                _market.CreateSellOrder(
                    world,
                    governance.Id,
                    collector.Id,
                    collectorStorage.Id,
                    CoreProductionContent.HerbalMedicineMaterialProductId,
                    MarketLot,
                    MarketUnitPrice,
                    0,
                    checked(world.AbsoluteDay + 30));
            }

            var physician = FindPhysician(world, village);
            if (physician == null || physician.FamilyId == collector.Id)
            {
                return;
            }
            var physicianFamily = ProductInventorySystem.FindFamily(
                world, physician.FamilyId);
            var physicianStorage = FindHouseholdGranary(
                world, physicianFamily.Id);
            var physicianMedicine = AvailableFamilyQuantityAtLocation(
                world,
                physicianFamily.Id,
                village.LocationId,
                CoreProductionContent.HerbalMedicineMaterialProductId);
            if (physicianMedicine >= MedicineReorderPoint ||
                HasActiveMarketOrder(
                    world,
                    physicianFamily.Id,
                    CoreProductionContent.HerbalMedicineMaterialProductId,
                    FormalMarketOrderSide.Buy))
            {
                return;
            }

            var affordable = physicianFamily.Wealth / MarketUnitPrice;
            var capacity = Math.Max(
                0L,
                physicianStorage.Capacity - physicianStorage.InventoryUnits);
            var quantity = Math.Min(MarketLot, Math.Min(affordable, capacity));
            if (quantity > 0)
            {
                _market.CreateBuyOrder(
                    world,
                    governance.Id,
                    physicianFamily.Id,
                    physicianStorage.Id,
                    CoreProductionContent.HerbalMedicineMaterialProductId,
                    quantity,
                    MarketUnitPrice,
                    0,
                    checked(world.AbsoluteDay + 30));
            }
        }

        private PersonState FindFamilyWorker(
            WorldState world,
            FamilyState family,
            string locationId)
        {
            var ids = new List<string>(family.MemberIds);
            ids.Sort(StringComparer.Ordinal);
            for (var i = 0; i < ids.Count; i++)
            {
                var person = Person(world, ids[i]);
                if (person.IsAlive && person.LocationId == locationId &&
                    person.LaborCapacityBasisPoints > 0)
                {
                    return person;
                }
            }

            return null;
        }

        private PersonState Person(WorldState world, string personId)
        {
            return _people == null
                ? ProductInventorySystem.FindPerson(world, personId)
                : _people.GetRequired(personId);
        }

        private static FamilyState FindCollectorFamily(
            WorldState world,
            VillageState village)
        {
            var familyIds = new List<string>(village.HouseholdIds);
            familyIds.Sort(StringComparer.Ordinal);
            FamilyState fallback = null;
            for (var i = 0; i < familyIds.Count; i++)
            {
                var family = ProductInventorySystem.FindFamily(
                    world, familyIds[i]);
                fallback ??= family;
                var head = ProductInventorySystem.FindPerson(
                    world, family.HeadPersonId);
                if (head.VillageOccupation == VillageOccupation.Farmer)
                {
                    return family;
                }
            }

            return fallback ?? throw new InvalidOperationException(
                $"Village {village.Id} has no household for herb gathering.");
        }

        private static VillageFacilityState FindHouseholdGranary(
            WorldState world,
            string familyId)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                if (facility.OwnerFamilyId == familyId &&
                    facility.Kind == VillageFacilityKind.HouseholdGranary)
                {
                    return facility;
                }
            }

            throw new InvalidOperationException(
                $"Family {familyId} has no household granary.");
        }

        private static PersonState FindPhysician(
            WorldState world,
            VillageState village)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                if (facility.VillageId == village.Id &&
                    facility.Kind == VillageFacilityKind.Clinic)
                {
                    return ProductInventorySystem.FindPerson(
                        world, facility.ManagerPersonId);
                }
            }

            return null;
        }

        private static CountyGovernanceState FindGovernance(
            WorldState world,
            string countyLocationId)
        {
            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                if (world.CountyGovernances[i].CountyLocationId ==
                    countyLocationId)
                {
                    return world.CountyGovernances[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing governance for county {countyLocationId}.");
        }

        private static ResourceBodyState FindResource(
            WorldState world,
            string resourceId)
        {
            for (var i = 0; i < world.ResourceBodies.Count; i++)
            {
                if (world.ResourceBodies[i].Id == resourceId)
                {
                    return world.ResourceBodies[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing medicinal resource {resourceId}.");
        }

        private static long AvailableQuantity(
            WorldState world,
            string familyId,
            string storageFacilityId,
            string productId)
        {
            long total = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == familyId &&
                    batch.StorageFacilityId == storageFacilityId &&
                    batch.ProductDefinitionId == productId)
                {
                    total = checked(
                        total + batch.Quantity - batch.ReservedQuantity);
                }
            }

            return total;
        }

        private static long AvailableFamilyQuantityAtLocation(
            WorldState world,
            string familyId,
            string locationId,
            string productId)
        {
            long total = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId != familyId ||
                    batch.ProductDefinitionId != productId)
                {
                    continue;
                }

                string batchLocation;
                if (!string.IsNullOrEmpty(batch.StorageFacilityId))
                {
                    var storage = ProductInventorySystem.FindFacility(
                        world, batch.StorageFacilityId);
                    batchLocation = ProductInventorySystem.FindVillage(
                        world, storage.VillageId).LocationId;
                }
                else if (!string.IsNullOrEmpty(batch.InventoryContainerId))
                {
                    batchLocation = ProductInventorySystem.FindContainer(
                        world, batch.InventoryContainerId).LocationId;
                }
                else
                {
                    continue;
                }

                if (batchLocation == locationId)
                {
                    total = checked(
                        total + batch.Quantity - batch.ReservedQuantity);
                }
            }

            return total;
        }

        private static bool HasActiveExtraction(
            WorldState world,
            string resourceBodyId)
        {
            return world.ResourceExtractionOrders.Exists(order =>
                order.ResourceBodyId == resourceBodyId &&
                order.Status == ProductionOrderStatus.Active);
        }

        private static bool HasActiveProcessing(
            WorldState world,
            string familyId)
        {
            return world.ProcessingWorkOrders.Exists(order =>
                order.OwnerFamilyId == familyId &&
                order.RecipeDefinitionId ==
                    CoreProductionContent.DryMedicinalPlantsRecipeId &&
                order.Status == ProductionOrderStatus.Active);
        }

        private static bool HasActiveMarketOrder(
            WorldState world,
            string familyId,
            string productId,
            FormalMarketOrderSide side)
        {
            return world.FormalMarketOrders.Exists(order =>
                order.OwnerFamilyId == familyId &&
                order.ProductDefinitionId == productId &&
                order.Side == side &&
                order.Status == FormalMarketOrderStatus.Active);
        }

        private static void AddCapability(
            VillageFacilityState facility,
            string capabilityId)
        {
            facility.CapabilityTags ??= new List<string>();
            if (!facility.CapabilityTags.Contains(capabilityId))
            {
                facility.CapabilityTags.Add(capabilityId);
                facility.CapabilityTags.Sort(StringComparer.Ordinal);
            }
        }

        public static string ResourceBodyId(string villageId)
        {
            return $"resource_body.{villageId}.wild_medicinal_plants.001";
        }
    }
}
