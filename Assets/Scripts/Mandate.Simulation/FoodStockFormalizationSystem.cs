using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class FoodStockFormalizationResult
    {
        public long FamilyFoodQuantity { get; internal set; }
        public long VillageGranaryFoodQuantity { get; internal set; }
        public long CountyGranaryFoodQuantity { get; internal set; }
        public int FamilyTransactions { get; internal set; }
        public int VillageContainers { get; internal set; }
        public int CountyContainers { get; internal set; }

        public long TotalFormalizedQuantity => checked(
            FamilyFoodQuantity +
            VillageGranaryFoodQuantity +
            CountyGranaryFoodQuantity);
    }

    public sealed class FoodStockFormalizationAudit
    {
        public long LegacyFoodQuantity { get; internal set; }
        public long FormalizedBatchQuantity { get; internal set; }
        public int MissingContainerReferences { get; internal set; }
        public int InvalidFormalizationTransactions { get; internal set; }

        public bool IsValid =>
            LegacyFoodQuantity == 0 &&
            MissingContainerReferences == 0 &&
            InvalidFormalizationTransactions == 0;
    }

    public sealed class FoodStockFormalizationSystem
    {
        private const string VillageContainerKindId =
            "inventory.village_public_granary";
        private const string CountyContainerKindId =
            "inventory.county_granary";

        private readonly ProductionContentRegistry _content;
        private readonly IReadOnlyList<FoodDefinition> _openingFoods;

        public FoodStockFormalizationSystem(ProductionContentRegistry content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _openingFoods = BuildOpeningFoods(content);
        }

        public FoodStockFormalizationResult FormalizeLegacyStocks(
            WorldState world)
        {
            ProductInventorySystem.RequireWorld(world);
            if (world.SchemaVersion != WorldState.CurrentSchemaVersion ||
                world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.LegacyScalar)
            {
                throw new InvalidOperationException(
                    "Only a current legacy-scalar world can be formalized.");
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            ValidateConversionInputs(world);
            var result = new FoodStockFormalizationResult();

            var families = new List<FamilyState>(world.Families);
            families.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < families.Count; i++)
            {
                if (families[i].Grain <= 0)
                {
                    continue;
                }

                var storage = FindHouseholdGranary(world, families[i].Id);
                var quantity = families[i].Grain;
                var transaction = NewFormalizationTransaction(
                    world,
                    families[i].HeadPersonId,
                    -quantity,
                    0,
                    0,
                    string.Empty,
                    string.Empty,
                    $"Formalized legacy food for family {families[i].Id}.");
                AddFamilyBatches(
                    world, families[i], storage, transaction, quantity);
                families[i].Grain = 0;
                world.InventoryTransactions.Add(transaction);
                result.FamilyFoodQuantity = checked(
                    result.FamilyFoodQuantity + quantity);
                result.FamilyTransactions++;
            }

            var villages = new List<VillageState>(world.Villages);
            villages.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < villages.Count; i++)
            {
                var governance = FindGovernanceForVillage(world, villages[i]);
                var publicGranary = FindPublicGranary(world, villages[i].Id);
                var container = CreateContainer(
                    world,
                    $"inventory.village_granary.{villages[i].Id}",
                    VillageContainerKindId,
                    governance.GovernmentOrganizationId,
                    villages[i].LocationId,
                    villages[i].PublicGranaryGrain,
                    publicGranary == null ? 1L : publicGranary.Capacity);
                villages[i].PublicGranaryInventoryContainerId = container.Id;
                world.InventoryContainers.Add(container);
                result.VillageContainers++;

                var quantity = villages[i].PublicGranaryGrain;
                if (quantity > 0)
                {
                    var actor = FindOrganizationLeader(
                        world, governance.GovernmentOrganizationId);
                    var transaction = NewFormalizationTransaction(
                        world,
                        actor,
                        0,
                        -quantity,
                        0,
                        villages[i].Id,
                        string.Empty,
                        $"Formalized public granary food for village {villages[i].Id}.");
                    AddOrganizationBatches(
                        world, container, transaction, quantity);
                    world.InventoryTransactions.Add(transaction);
                    result.VillageGranaryFoodQuantity = checked(
                        result.VillageGranaryFoodQuantity + quantity);
                }

                villages[i].PublicGranaryGrain = 0;
                if (publicGranary != null)
                {
                    publicGranary.InventoryUnits = 0;
                }
            }

            var governances = new List<CountyGovernanceState>(
                world.CountyGovernances);
            governances.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < governances.Count; i++)
            {
                long governedGranaryCapacity = 0;
                for (var villageIndex = 0;
                     villageIndex < villages.Count;
                     villageIndex++)
                {
                    if (villages[villageIndex].ParentLocationId !=
                        governances[i].CountyLocationId)
                    {
                        continue;
                    }

                    var facility = FindPublicGranary(
                        world, villages[villageIndex].Id);
                    governedGranaryCapacity = checked(
                        governedGranaryCapacity +
                        (facility == null ? 0L : facility.Capacity));
                }
                var container = CreateContainer(
                    world,
                    $"inventory.county_granary.{governances[i].Id}",
                    CountyContainerKindId,
                    governances[i].GovernmentOrganizationId,
                    governances[i].CountyLocationId,
                    governances[i].CountyGranaryGrain,
                    governedGranaryCapacity);
                governances[i].GranaryInventoryContainerId = container.Id;
                world.InventoryContainers.Add(container);
                result.CountyContainers++;

                var quantity = governances[i].CountyGranaryGrain;
                if (quantity > 0)
                {
                    var actor = FindOrganizationLeader(
                        world, governances[i].GovernmentOrganizationId);
                    var transaction = NewFormalizationTransaction(
                        world,
                        actor,
                        0,
                        0,
                        -quantity,
                        string.Empty,
                        governances[i].Id,
                        $"Formalized county granary food for {governances[i].Id}.");
                    AddOrganizationBatches(
                        world, container, transaction, quantity);
                    world.InventoryTransactions.Add(transaction);
                    result.CountyGranaryFoodQuantity = checked(
                        result.CountyGranaryFoodQuantity + quantity);
                }

                governances[i].CountyGranaryGrain = 0;
            }

            world.FoodInventoryAuthorityMode =
                FoodInventoryAuthorityMode.FormalProductBatches;
            return result;
        }

        public FoodStockFormalizationAudit Audit(WorldState world)
        {
            ProductInventorySystem.RequireWorld(world);
            var audit = new FoodStockFormalizationAudit();
            for (var i = 0; i < world.Families.Count; i++)
            {
                audit.LegacyFoodQuantity = checked(
                    audit.LegacyFoodQuantity + world.Families[i].Grain);
            }

            for (var i = 0; i < world.Villages.Count; i++)
            {
                var village = world.Villages[i];
                audit.LegacyFoodQuantity = checked(
                    audit.LegacyFoodQuantity + village.PublicGranaryGrain);
                if (world.FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches &&
                    !HasContainer(
                        world, village.PublicGranaryInventoryContainerId))
                {
                    audit.MissingContainerReferences++;
                }
            }

            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                var governance = world.CountyGovernances[i];
                audit.LegacyFoodQuantity = checked(
                    audit.LegacyFoodQuantity + governance.CountyGranaryGrain);
                if (world.FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches &&
                    !HasContainer(world, governance.GranaryInventoryContainerId))
                {
                    audit.MissingContainerReferences++;
                }
            }

            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                var transaction = world.InventoryTransactions[i];
                if (transaction.Type !=
                    InventoryTransactionType.LegacyFoodStockFormalized)
                {
                    continue;
                }

                long positive = 0;
                for (var lineIndex = 0;
                     lineIndex < transaction.Lines.Count;
                     lineIndex++)
                {
                    positive = checked(
                        positive + transaction.Lines[lineIndex].QuantityDelta);
                }

                var removed = checked(-(
                    transaction.LegacyFamilyGrainDelta +
                    transaction.LegacyVillagePublicGranaryDelta +
                    transaction.LegacyCountyGranaryDelta));
                if (removed <= 0 || positive != removed)
                {
                    audit.InvalidFormalizationTransactions++;
                }
                audit.FormalizedBatchQuantity = checked(
                    audit.FormalizedBatchQuantity + positive);
            }

            return audit;
        }

        private void ValidateConversionInputs(WorldState world)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                ids.Add(world.InventoryContainers[i].Id);
            }

            for (var i = 0; i < world.Families.Count; i++)
            {
                var family = world.Families[i];
                if (family.Grain <= 0)
                {
                    continue;
                }

                var storage = FindHouseholdGranary(world, family.Id);
                if (storage == null ||
                    storage.InventoryUnits != ProductInventorySystem
                        .CalculatePhysicalInventoryUnits(
                            world, storage.Id, family.Id, _content))
                {
                    throw new InvalidOperationException(
                        $"Family {family.Id} has no consistent household granary.");
                }
            }

            for (var i = 0; i < world.Villages.Count; i++)
            {
                var village = world.Villages[i];
                _ = FindGovernanceForVillage(world, village);
                var id = $"inventory.village_granary.{village.Id}";
                if (!ids.Add(id))
                {
                    throw new InvalidOperationException(
                        $"Inventory container {id} already exists.");
                }

                var granary = FindPublicGranary(world, village.Id);
                if (granary != null &&
                    granary.InventoryUnits != village.PublicGranaryGrain)
                {
                    throw new InvalidOperationException(
                        $"Village {village.Id} public granary is inconsistent.");
                }
            }

            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                var governance = world.CountyGovernances[i];
                _ = FindOrganization(world, governance.GovernmentOrganizationId);
                var id = $"inventory.county_granary.{governance.Id}";
                if (!ids.Add(id))
                {
                    throw new InvalidOperationException(
                        $"Inventory container {id} already exists.");
                }
            }
        }

        private void AddFamilyBatches(
            WorldState world,
            FamilyState family,
            VillageFacilityState storage,
            InventoryTransactionState transaction,
            long quantity)
        {
            var allocation = Allocate(quantity);
            for (var i = 0; i < allocation.Count; i++)
            {
                var product = _content.GetProduct(
                    allocation[i].Food.ProductDefinitionId);
                var batch = ProductInventorySystem.NewBatch(
                    world,
                    product,
                    family,
                    storage,
                    transaction.Id,
                    string.Empty,
                    allocation[i].Quantity,
                    string.Empty,
                    0,
                    0);
                world.ProductBatches.Add(batch);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, batch.Quantity, 0));
            }
        }

        private void AddOrganizationBatches(
            WorldState world,
            InventoryContainerState container,
            InventoryTransactionState transaction,
            long quantity)
        {
            var allocation = Allocate(quantity);
            for (var i = 0; i < allocation.Count; i++)
            {
                var product = _content.GetProduct(
                    allocation[i].Food.ProductDefinitionId);
                var batch = ProductInventorySystem.NewOrganizationBatch(
                    world,
                    product,
                    container,
                    transaction.Id,
                    string.Empty,
                    allocation[i].Quantity,
                    8_000);
                world.ProductBatches.Add(batch);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, batch.Quantity, 0));
            }
        }

        private List<OpeningAllocation> Allocate(long quantity)
        {
            var result = new List<OpeningAllocation>();
            long allocated = 0;
            for (var i = 0; i < _openingFoods.Count; i++)
            {
                var amount = checked(
                    quantity * _openingFoods[i].OpeningShareBasisPoints /
                    10_000L);
                if (amount <= 0)
                {
                    continue;
                }

                result.Add(new OpeningAllocation(_openingFoods[i], amount));
                allocated = checked(allocated + amount);
            }

            var remainder = quantity - allocated;
            if (remainder > 0)
            {
                if (result.Count != 0 &&
                    ReferenceEquals(result[0].Food, _openingFoods[0]))
                {
                    result[0].Quantity = checked(
                        result[0].Quantity + remainder);
                }
                else
                {
                    result.Insert(
                        0,
                        new OpeningAllocation(_openingFoods[0], remainder));
                }
            }

            return result;
        }

        private static IReadOnlyList<FoodDefinition> BuildOpeningFoods(
            ProductionContentRegistry content)
        {
            var all = content.GetFoodsInStableOrder();
            var result = new List<FoodDefinition>();
            long totalShare = 0;
            for (var i = 0; i < all.Count; i++)
            {
                if (all[i].OpeningShareBasisPoints <= 0)
                {
                    continue;
                }

                var product = content.GetProduct(all[i].ProductDefinitionId);
                if (product.BaseWeight != 1)
                {
                    throw new ProductionContentException(
                        $"Opening food {product.Id} must weigh one legacy stock unit.");
                }
                result.Add(all[i]);
                totalShare = checked(
                    totalShare + all[i].OpeningShareBasisPoints);
            }

            if (result.Count == 0 || totalShare != 10_000)
            {
                throw new ProductionContentException(
                    "Formal food stock conversion requires stable opening shares totaling 10000.");
            }

            return result;
        }

        private static InventoryTransactionState NewFormalizationTransaction(
            WorldState world,
            string actorPersonId,
            long familyDelta,
            long villageDelta,
            long countyDelta,
            string villageId,
            string governanceId,
            string summary)
        {
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.LegacyFoodStockFormalized,
                actorPersonId ?? string.Empty,
                string.Empty,
                familyDelta,
                0,
                0,
                summary);
            transaction.SourceVillageId = villageId;
            transaction.SourceCountyGovernanceId = governanceId;
            transaction.LegacyVillagePublicGranaryDelta = villageDelta;
            transaction.LegacyCountyGranaryDelta = countyDelta;
            return transaction;
        }

        private static InventoryContainerState CreateContainer(
            WorldState world,
            string id,
            string kindId,
            string organizationId,
            string locationId,
            long openingQuantity,
            long minimumCapacity)
        {
            _ = new StableId(id);
            return new InventoryContainerState
            {
                Id = id,
                KindId = kindId,
                OwnerFamilyId = string.Empty,
                OwnerOrganizationId = organizationId,
                CarrierPersonId = string.Empty,
                LocationId = locationId,
                CapacityWeight = Math.Max(
                    1L, Math.Max(openingQuantity, minimumCapacity)),
                FoodStorageEnvironmentId = kindId == VillageContainerKindId
                    ? "storage.environment.village_public_granary"
                    : "storage.environment.county_granary",
                FoodStorageProtectionBasisPoints =
                    kindId == VillageContainerKindId ? 3_500 : 4_500
            };
        }

        private static VillageFacilityState FindHouseholdGranary(
            WorldState world,
            string familyId)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                if (facility.Kind == VillageFacilityKind.HouseholdGranary &&
                    facility.OwnerFamilyId == familyId)
                {
                    return facility;
                }
            }

            return null;
        }

        private static VillageFacilityState FindPublicGranary(
            WorldState world,
            string villageId)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                if (facility.VillageId == villageId &&
                    facility.Kind == VillageFacilityKind.Granary &&
                    string.IsNullOrEmpty(facility.OwnerFamilyId))
                {
                    return facility;
                }
            }

            return null;
        }

        private static CountyGovernanceState FindGovernanceForVillage(
            WorldState world,
            VillageState village)
        {
            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                if (world.CountyGovernances[i].CountyLocationId ==
                    village.ParentLocationId)
                {
                    return world.CountyGovernances[i];
                }
            }

            throw new InvalidOperationException(
                $"Village {village.Id} has no county governance granary owner.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == organizationId)
                {
                    return world.Organizations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing organization {organizationId}.");
        }

        private static string FindOrganizationLeader(
            WorldState world,
            string organizationId)
        {
            return FindOrganization(world, organizationId).LeaderPersonId ??
                string.Empty;
        }

        private static bool HasContainer(WorldState world, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                if (world.InventoryContainers[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class OpeningAllocation
        {
            public OpeningAllocation(FoodDefinition food, long quantity)
            {
                Food = food;
                Quantity = quantity;
            }

            public FoodDefinition Food { get; }
            public long Quantity { get; set; }
        }
    }
}
