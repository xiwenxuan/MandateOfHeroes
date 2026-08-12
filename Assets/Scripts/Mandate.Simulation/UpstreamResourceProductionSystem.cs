using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class UpstreamResourceProductionSystem
    {
        public const string PrototypeIronBodyId =
            "resource_body.zhongshan.iron_vein.001";
        public const string PrototypeForestBodyId =
            "resource_body.zhongshan.forest_stand.001";
        public const string PrototypePastureForageBodyId =
            "resource_body.zhongshan.pasture_forage.001";
        public const string PrototypeTanningBarkBodyId =
            "resource_body.zhongshan.tanning_bark.001";
        public const string PrototypeIronMineSiteId =
            "production_site.zhongshan_merchants.iron_mine";
        public const string PrototypeLoggingSiteId =
            "production_site.zhongshan_merchants.logging_camp";
        public const string PrototypeCharcoalKilnSiteId =
            "production_site.zhongshan_merchants.charcoal_kiln";
        public const string PrototypeBloomerySiteId =
            "production_site.zhongshan_merchants.bloomery";
        public const string PrototypePastureForageSiteId =
            "production_site.zhongshan_merchants.pasture_forage";
        public const string PrototypeBarkHarvestingSiteId =
            "production_site.zhongshan_merchants.bark_harvesting";

        private const string MerchantOrganizationId =
            "organization.zhongshan_merchants";
        private const string ZhongshanLocationId = "location.zhongshan";
        private readonly ProductionContentRegistry _content;
        private readonly IPersonRepository _personRepository;

        public UpstreamResourceProductionSystem(
            ProductionContentRegistry content = null,
            IPersonRepository personRepository = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
            _personRepository = personRepository;
        }

        public static void InitializePrototype(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.ResourceBodies.Exists(item =>
                    item.Id == PrototypeIronBodyId))
            {
                return;
            }

            _ = ProcessingProductionSystem.FindContainer(
                world, MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId);
            world.ResourceBodies.Add(new ResourceBodyState
            {
                Id = PrototypeIronBodyId,
                ResourceKindId = "resource_kind.iron_ore_vein",
                OutputProductDefinitionId = CoreProductionContent.IronOreProductId,
                LocationId = ZhongshanLocationId,
                Provenance = "historical_inference",
                GenerationRuleVersion = "resource_rules.prototype.1",
                RequiredFacilityTag = CoreProductionContent.IronMiningFacilityTag,
                InitialQuantity = 20_000,
                RemainingQuantity = 20_000,
                QualityBasisPoints = 7_500,
                ExtractionDifficultyBasisPoints = 12_000
            });
            world.ResourceBodies.Add(new ResourceBodyState
            {
                Id = PrototypeForestBodyId,
                ResourceKindId = "resource_kind.temperate_forest_stand",
                OutputProductDefinitionId =
                    CoreProductionContent.TimberMaterialProductId,
                LocationId = ZhongshanLocationId,
                Provenance = "historical_inference",
                GenerationRuleVersion = "resource_rules.prototype.1",
                RequiredFacilityTag = CoreProductionContent.LoggingFacilityTag,
                InitialQuantity = 30_000,
                RemainingQuantity = 30_000,
                QualityBasisPoints = 8_000,
                ExtractionDifficultyBasisPoints = 9_000
            });
            world.ResourceBodies.Add(new ResourceBodyState
            {
                Id = PrototypePastureForageBodyId,
                ResourceKindId = "resource_kind.northern_pasture_forage",
                OutputProductDefinitionId =
                    CoreProductionContent.PastureFodderProductId,
                LocationId = ZhongshanLocationId,
                Provenance = "historical_inference",
                GenerationRuleVersion = "resource_rules.prototype.1",
                RequiredFacilityTag =
                    CoreProductionContent.PastureForageFacilityTag,
                InitialQuantity = 40_000,
                RemainingQuantity = 40_000,
                QualityBasisPoints = 7_800,
                ExtractionDifficultyBasisPoints = 7_000
            });
            world.ResourceBodies.Add(new ResourceBodyState
            {
                Id = PrototypeTanningBarkBodyId,
                ResourceKindId = "resource_kind.tannin_bark_stand",
                OutputProductDefinitionId =
                    CoreProductionContent.TanningBarkProductId,
                LocationId = ZhongshanLocationId,
                Provenance = "historical_inference",
                GenerationRuleVersion = "resource_rules.prototype.1",
                RequiredFacilityTag =
                    CoreProductionContent.BarkHarvestingFacilityTag,
                InitialQuantity = 8_000,
                RemainingQuantity = 8_000,
                QualityBasisPoints = 7_600,
                ExtractionDifficultyBasisPoints = 8_000
            });
            AddSite(
                world,
                PrototypeIronMineSiteId,
                "production_site_kind.iron_mine",
                "person.su_shuang",
                CoreProductionContent.IronMiningFacilityTag);
            AddSite(
                world,
                PrototypeLoggingSiteId,
                "production_site_kind.logging_camp",
                "person.zhang_shiping",
                CoreProductionContent.LoggingFacilityTag);
            AddSite(
                world,
                PrototypeCharcoalKilnSiteId,
                "production_site_kind.charcoal_kiln",
                "person.zhang_shiping",
                CoreProductionContent.CharcoalKilnFacilityTag);
            AddSite(
                world,
                PrototypeBloomerySiteId,
                "production_site_kind.bloomery",
                "person.su_shuang",
                CoreProductionContent.BloomeryFacilityTag);
            AddSite(
                world,
                PrototypePastureForageSiteId,
                "production_site_kind.pasture_forage",
                "person.zhang_shiping",
                CoreProductionContent.PastureForageFacilityTag);
            AddSite(
                world,
                PrototypeBarkHarvestingSiteId,
                "production_site_kind.bark_harvesting",
                "person.su_shuang",
                CoreProductionContent.BarkHarvestingFacilityTag);
        }

        public ResourceExtractionOrderState CreateOrder(
            WorldState world,
            string resourceBodyId,
            string productionSiteId,
            string managerPersonId,
            IEnumerable<string> workerPersonIds,
            ProductionControlMode controlMode,
            long quantity)
        {
            ProductInventorySystem.RequireWorld(world);
            world.Validate();
            _content.ValidateManifest(world.ProductionContentManifest);
            if (quantity <= 0 ||
                !Enum.IsDefined(typeof(ProductionControlMode), controlMode))
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            var resource = FindResourceBody(world, resourceBodyId);
            var site = ProcessingProductionSystem.FindProductionSite(
                world, productionSiteId);
            var container = ProcessingProductionSystem.FindContainer(
                world, site.InventoryContainerId);
            var manager = FindPerson(world, managerPersonId);
            var workers = ValidateAndSortWorkers(
                world, workerPersonIds, site);
            if (resource.RemainingQuantity - resource.ReservedQuantity < quantity ||
                resource.LocationId != site.LocationId ||
                site.OwnerOrganizationId != container.OwnerOrganizationId ||
                site.ManagerPersonId != manager.Id ||
                !site.FacilityTags.Contains(resource.RequiredFacilityTag) ||
                !manager.IsAlive || manager.LocationId != site.LocationId ||
                !ProcessingProductionSystem.HasMembership(
                    world, manager.Id, site.OwnerOrganizationId) ||
                ActiveOrdersAtSite(world, site.Id) >=
                    site.ConcurrentOrderCapacity)
            {
                throw new InvalidOperationException(
                    "Resource extraction requires available reserves and a compatible, co-located organization site.");
            }

            long effectiveLabor = 0;
            for (var i = 0; i < workers.Count; i++)
            {
                effectiveLabor = checked(effectiveLabor +
                    FindPerson(world, workers[i])
                        .LaborCapacityBasisPoints);
            }

            var laborDemand = checked(
                quantity * resource.ExtractionDifficultyBasisPoints);
            var duration = checked((laborDemand + effectiveLabor - 1) /
                effectiveLabor);
            duration = Math.Max(1, duration);
            var order = new ResourceExtractionOrderState
            {
                Id = $"resource_extraction.{world.AbsoluteDay}." +
                     $"{world.ResourceExtractionOrders.Count:D6}",
                ResourceBodyId = resource.Id,
                OwnerOrganizationId = site.OwnerOrganizationId,
                ProductionSiteId = site.Id,
                InventoryContainerId = container.Id,
                ManagerPersonId = manager.Id,
                WorkerPersonIds = workers,
                ControlMode = controlMode,
                Status = ProductionOrderStatus.Active,
                CreatedDay = world.AbsoluteDay,
                FinishDay = checked(world.AbsoluteDay + duration),
                RequestedQuantity = quantity
            };
            resource.ReservedQuantity = checked(
                resource.ReservedQuantity + quantity);
            world.ResourceExtractionOrders.Add(order);
            world.ResourceExtractionLedgerEntries.Add(new
                ResourceExtractionLedgerEntryState
                {
                    Id = $"resource_extraction_ledger.{world.AbsoluteDay}." +
                         $"{world.ResourceExtractionLedgerEntries.Count:D6}",
                    Day = world.AbsoluteDay,
                    Type = ResourceExtractionLedgerEntryType.Reserved,
                    ResourceBodyId = resource.Id,
                    ResourceExtractionOrderId = order.Id,
                    ActorPersonId = manager.Id,
                    ReservedQuantityDelta = quantity,
                    Summary = $"Reserved {quantity} units from {resource.Id}."
                });
            world.Validate();
            return order;
        }

        public ResourceExtractionOrderState CreateFamilyOrder(
            WorldState world,
            string resourceBodyId,
            string familyId,
            string storageFacilityId,
            string managerPersonId,
            IEnumerable<string> workerPersonIds,
            ProductionControlMode controlMode,
            long quantity)
        {
            ProductInventorySystem.RequireWorld(world);
            world.Validate();
            _content.ValidateManifest(world.ProductionContentManifest);
            if (quantity <= 0 ||
                !Enum.IsDefined(typeof(ProductionControlMode), controlMode))
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            var resource = FindResourceBody(world, resourceBodyId);
            var family = ProductInventorySystem.FindFamily(world, familyId);
            var storage = ProductInventorySystem.FindFacility(
                world, storageFacilityId);
            var village = ProductInventorySystem.FindVillage(
                world, storage.VillageId);
            var manager = FindPerson(world, managerPersonId);
            var workers = ValidateAndSortFamilyWorkers(
                world, workerPersonIds, family.Id, village.LocationId);
            if (resource.RemainingQuantity - resource.ReservedQuantity < quantity ||
                resource.LocationId != village.LocationId ||
                storage.OwnerFamilyId != family.Id ||
                manager.FamilyId != family.Id || !manager.IsAlive ||
                manager.LocationId != village.LocationId ||
                storage.CapabilityTags == null ||
                !storage.CapabilityTags.Contains(
                    resource.RequiredFacilityTag) ||
                ActiveFamilyOrdersAtFacility(world, storage.Id) >= 1)
            {
                throw new InvalidOperationException(
                    "Family resource extraction requires available reserves and a compatible, co-located family facility.");
            }

            long effectiveLabor = 0;
            for (var i = 0; i < workers.Count; i++)
            {
                effectiveLabor = checked(effectiveLabor +
                    FindPerson(world, workers[i]).LaborCapacityBasisPoints);
            }

            var laborDemand = checked(
                quantity * resource.ExtractionDifficultyBasisPoints);
            var duration = Math.Max(
                1,
                checked((laborDemand + effectiveLabor - 1) / effectiveLabor));
            var order = new ResourceExtractionOrderState
            {
                Id = $"resource_extraction.{world.AbsoluteDay}." +
                     $"{world.ResourceExtractionOrders.Count:D6}",
                ResourceBodyId = resource.Id,
                OwnerFamilyId = family.Id,
                StorageFacilityId = storage.Id,
                ManagerPersonId = manager.Id,
                WorkerPersonIds = workers,
                ControlMode = controlMode,
                Status = ProductionOrderStatus.Active,
                CreatedDay = world.AbsoluteDay,
                FinishDay = checked(world.AbsoluteDay + duration),
                RequestedQuantity = quantity
            };
            resource.ReservedQuantity = checked(
                resource.ReservedQuantity + quantity);
            world.ResourceExtractionOrders.Add(order);
            world.ResourceExtractionLedgerEntries.Add(new
                ResourceExtractionLedgerEntryState
                {
                    Id = $"resource_extraction_ledger.{world.AbsoluteDay}." +
                         $"{world.ResourceExtractionLedgerEntries.Count:D6}",
                    Day = world.AbsoluteDay,
                    Type = ResourceExtractionLedgerEntryType.Reserved,
                    ResourceBodyId = resource.Id,
                    ResourceExtractionOrderId = order.Id,
                    ActorPersonId = manager.Id,
                    ReservedQuantityDelta = quantity,
                    Summary = $"Reserved {quantity} units from {resource.Id}."
                });
            world.Validate();
            return order;
        }

        public void ResolveDueOrders(WorldState world)
        {
            ProductInventorySystem.RequireWorld(world);
            var due = new List<ResourceExtractionOrderState>();
            for (var i = 0; i < world.ResourceExtractionOrders.Count; i++)
            {
                var order = world.ResourceExtractionOrders[i];
                if (order.Status == ProductionOrderStatus.Active &&
                    order.FinishDay <= world.AbsoluteDay &&
                    CanSettle(world, order))
                {
                    due.Add(order);
                }
            }

            due.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < due.Count; i++)
            {
                Settle(world, due[i]);
            }
        }

        private void Settle(
            WorldState world,
            ResourceExtractionOrderState order)
        {
            var resource = FindResourceBody(world, order.ResourceBodyId);
            var product = _content.GetProduct(
                resource.OutputProductDefinitionId);
            if (resource.RemainingQuantity < order.RequestedQuantity ||
                resource.ReservedQuantity < order.RequestedQuantity)
            {
                throw new InvalidOperationException(
                    $"Reserved resource {resource.Id} is no longer available.");
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.ResourceExtractionSettled,
                order.ManagerPersonId,
                string.Empty,
                0,
                0,
                0,
                $"Settled resource extraction {order.Id}.");
            transaction.SourceResourceExtractionOrderId = order.Id;
            ProductBatchState batch;
            if (!string.IsNullOrEmpty(order.OwnerFamilyId))
            {
                var family = ProductInventorySystem.FindFamily(
                    world, order.OwnerFamilyId);
                var storage = ProductInventorySystem.FindFacility(
                    world, order.StorageFacilityId);
                var quality = checked(resource.QualityBasisPoints *
                    storage.ConditionBasisPoints / 10_000);
                batch = ProductInventorySystem.NewBatch(
                    world,
                    product,
                    family,
                    storage,
                    transaction.Id,
                    order.Id,
                    order.RequestedQuantity,
                    string.Empty,
                    0,
                    0);
                batch.QualityBasisPoints = quality;
                batch.QualityDimensions = ProductQualityRules.CreateUniform(
                    product, quality);
                storage.InventoryUnits = checked(
                    storage.InventoryUnits + batch.Quantity * batch.UnitWeight);
                transaction.FacilityInventoryDelta = checked(
                    batch.Quantity * batch.UnitWeight);
            }
            else
            {
                var site = ProcessingProductionSystem.FindProductionSite(
                    world, order.ProductionSiteId);
                var container = ProcessingProductionSystem.FindContainer(
                    world, order.InventoryContainerId);
                var quality = checked(resource.QualityBasisPoints *
                    site.ConditionBasisPoints / 10_000);
                batch = ProductInventorySystem.NewOrganizationBatch(
                    world,
                    product,
                    container,
                    transaction.Id,
                    order.Id,
                    order.RequestedQuantity,
                    quality);
            }
            transaction.Lines.Add(ProductInventorySystem.Line(
                batch, batch.Quantity, 0));

            resource.RemainingQuantity -= order.RequestedQuantity;
            resource.ReservedQuantity -= order.RequestedQuantity;
            order.ExtractedQuantity = order.RequestedQuantity;
            order.OutputBatchId = batch.Id;
            order.Status = ProductionOrderStatus.Completed;
            order.SettledDay = world.AbsoluteDay;
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(transaction);
            world.ResourceExtractionLedgerEntries.Add(new
                ResourceExtractionLedgerEntryState
                {
                    Id = $"resource_extraction_ledger.{world.AbsoluteDay}." +
                         $"{world.ResourceExtractionLedgerEntries.Count:D6}",
                    Day = world.AbsoluteDay,
                    Type = ResourceExtractionLedgerEntryType.Settled,
                    ResourceBodyId = resource.Id,
                    ResourceExtractionOrderId = order.Id,
                    ActorPersonId = order.ManagerPersonId,
                    RemainingQuantityDelta = -order.RequestedQuantity,
                    ReservedQuantityDelta = -order.RequestedQuantity,
                    OutputBatchId = batch.Id,
                    OutputQuantity = batch.Quantity,
                    Summary = $"Extracted {batch.Quantity} units from {resource.Id}."
                });
            world.Validate();
        }

        private bool CanSettle(
            WorldState world,
            ResourceExtractionOrderState order)
        {
            if (!string.IsNullOrEmpty(order.OwnerFamilyId))
            {
                var storage = ProductInventorySystem.FindFacility(
                    world, order.StorageFacilityId);
                var village = ProductInventorySystem.FindVillage(
                    world, storage.VillageId);
                for (var i = 0; i < order.WorkerPersonIds.Count; i++)
                {
                    var worker = FindPerson(world, order.WorkerPersonIds[i]);
                    if (!worker.IsAlive ||
                        worker.FamilyId != order.OwnerFamilyId ||
                        worker.LocationId != village.LocationId)
                    {
                        return false;
                    }
                }

                var familyResource = FindResourceBody(
                    world, order.ResourceBodyId);
                var familyOutputWeight = checked(order.RequestedQuantity *
                    _content.GetProduct(
                        familyResource.OutputProductDefinitionId)
                        .BaseWeight);
                return storage.InventoryUnits + familyOutputWeight <=
                    storage.Capacity;
            }

            var site = ProcessingProductionSystem.FindProductionSite(
                world, order.ProductionSiteId);
            var container = ProcessingProductionSystem.FindContainer(
                world, order.InventoryContainerId);
            for (var i = 0; i < order.WorkerPersonIds.Count; i++)
            {
                var worker = FindPerson(world, order.WorkerPersonIds[i]);
                if (!worker.IsAlive || worker.LocationId != site.LocationId ||
                    !ProcessingProductionSystem.HasMembership(
                        world, worker.Id, order.OwnerOrganizationId))
                {
                    return false;
                }
            }

            long usedWeight = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].InventoryContainerId == container.Id)
                {
                    usedWeight = checked(usedWeight +
                        world.ProductBatches[i].Quantity *
                        world.ProductBatches[i].UnitWeight);
                }
            }

            var resource = FindResourceBody(world, order.ResourceBodyId);
            var outputWeight = checked(order.RequestedQuantity *
                _content.GetProduct(resource.OutputProductDefinitionId)
                    .BaseWeight);
            return usedWeight + outputWeight <= container.CapacityWeight;
        }

        private List<string> ValidateAndSortWorkers(
            WorldState world,
            IEnumerable<string> workerPersonIds,
            ProductionSiteState site)
        {
            if (workerPersonIds == null)
            {
                throw new ArgumentNullException(nameof(workerPersonIds));
            }

            var workers = new List<string>(workerPersonIds);
            workers.Sort(StringComparer.Ordinal);
            if (workers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Resource extraction needs at least one real worker.");
            }

            string previous = null;
            for (var i = 0; i < workers.Count; i++)
            {
                if (string.IsNullOrEmpty(workers[i]) || workers[i] == previous)
                {
                    throw new InvalidOperationException(
                        "Resource extraction workers must be unique stable people.");
                }

                var worker = FindPerson(world, workers[i]);
                if (!worker.IsAlive || worker.LocationId != site.LocationId ||
                    worker.LaborCapacityBasisPoints <= 0 ||
                    !ProcessingProductionSystem.HasMembership(
                        world, worker.Id, site.OwnerOrganizationId))
                {
                    throw new InvalidOperationException(
                        $"Worker {worker.Id} cannot join resource extraction.");
                }

                previous = workers[i];
            }

            return workers;
        }

        private List<string> ValidateAndSortFamilyWorkers(
            WorldState world,
            IEnumerable<string> workerPersonIds,
            string familyId,
            string locationId)
        {
            if (workerPersonIds == null)
            {
                throw new ArgumentNullException(nameof(workerPersonIds));
            }

            var workers = new List<string>(workerPersonIds);
            workers.Sort(StringComparer.Ordinal);
            if (workers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Family resource extraction needs at least one real worker.");
            }

            string previous = null;
            for (var i = 0; i < workers.Count; i++)
            {
                if (string.IsNullOrEmpty(workers[i]) || workers[i] == previous)
                {
                    throw new InvalidOperationException(
                        "Family resource workers must be unique stable people.");
                }

                var worker = FindPerson(world, workers[i]);
                if (!worker.IsAlive || worker.FamilyId != familyId ||
                    worker.LocationId != locationId ||
                    worker.LaborCapacityBasisPoints <= 0)
                {
                    throw new InvalidOperationException(
                        $"Worker {worker.Id} cannot join family resource extraction.");
                }

                previous = workers[i];
            }

            return workers;
        }

        private PersonState FindPerson(WorldState world, string id)
        {
            return _personRepository == null
                ? ProductInventorySystem.FindPerson(world, id)
                : _personRepository.GetRequired(id);
        }

        private static ResourceBodyState FindResourceBody(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.ResourceBodies.Count; i++)
            {
                if (world.ResourceBodies[i].Id == id)
                {
                    return world.ResourceBodies[i];
                }
            }

            throw new InvalidOperationException($"Missing resource body {id}.");
        }

        private static int ActiveOrdersAtSite(WorldState world, string siteId)
        {
            var count = 0;
            for (var i = 0; i < world.ResourceExtractionOrders.Count; i++)
            {
                if (world.ResourceExtractionOrders[i].ProductionSiteId == siteId &&
                    world.ResourceExtractionOrders[i].Status ==
                        ProductionOrderStatus.Active)
                {
                    count++;
                }
            }

            return count;
        }

        private static int ActiveFamilyOrdersAtFacility(
            WorldState world,
            string storageFacilityId)
        {
            var count = 0;
            for (var i = 0; i < world.ResourceExtractionOrders.Count; i++)
            {
                if (world.ResourceExtractionOrders[i].StorageFacilityId ==
                        storageFacilityId &&
                    world.ResourceExtractionOrders[i].Status ==
                        ProductionOrderStatus.Active)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AddSite(
            WorldState world,
            string id,
            string kindId,
            string managerPersonId,
            string facilityTag)
        {
            world.ProductionSites.Add(new ProductionSiteState
            {
                Id = id,
                KindId = kindId,
                OwnerOrganizationId = MerchantOrganizationId,
                LocationId = ZhongshanLocationId,
                ManagerPersonId = managerPersonId,
                InventoryContainerId =
                    MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                ConcurrentOrderCapacity = 1,
                ConditionBasisPoints = 8_000,
                FacilityTags = new List<string> { facilityTag }
            });
        }
    }
}
