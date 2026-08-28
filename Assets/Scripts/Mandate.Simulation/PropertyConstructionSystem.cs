using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    /// <summary>
    /// Owns the auditable Cell-property and material/labour/time construction
    /// contract. A decision may create a project, but only this service may
    /// reserve inventory, record labour and finally create a Facility.
    /// </summary>
    public sealed class PropertyConstructionSystem
    {
        public WorldCellPropertyState GrantOpeningProperty(
            WorldState world,
            ulong cellId64,
            string locationId,
            string ownerId,
            string administrativeControllerId)
        {
            RequireWorld(world);
            if (cellId64 == 0 || !HasLocation(world, locationId) ||
                !HasOwner(world, ownerId) ||
                world.CellProperties.Exists(item => item.CellId64 == cellId64))
            {
                throw new InvalidOperationException(
                    "Opening Cell property identity is invalid or duplicated.");
            }
            var property = new WorldCellPropertyState
            {
                Id = "cell_property." + cellId64,
                CellId64 = cellId64,
                LocationId = locationId,
                OwnerId = ownerId,
                AdministrativeControllerId =
                    administrativeControllerId ?? string.Empty,
                AcquiredDay = world.AbsoluteDay,
                LastTransferDay = world.AbsoluteDay,
                LastTransferPrice = 0,
                Revision = 0
            };
            world.CellProperties.Add(property);
            world.CellPropertyTransfers.Add(new CellPropertyTransferState
            {
                Id = "cell_transfer.opening." + cellId64,
                CellId64 = cellId64,
                LocationId = locationId,
                FromOwnerId = string.Empty,
                ToOwnerId = ownerId,
                Kind = CellPropertyTransferKind.AdministrativeGrant,
                Price = 0,
                Day = world.AbsoluteDay,
                AuthorizingPersonId = string.Empty
            });
            world.Revision = checked(world.Revision + 1);
            world.Validate();
            return property;
        }

        public CellPropertyTransferState TransferProperty(
            WorldState world,
            ulong cellId64,
            string fromOwnerId,
            string toOwnerId,
            long price,
            CellPropertyTransferKind kind,
            string authorizingPersonId)
        {
            RequireWorld(world);
            var property = world.CellProperties.Find(item =>
                item.CellId64 == cellId64) ??
                throw new InvalidOperationException("Cell has no property record.");
            if (property.OwnerId != fromOwnerId || !HasOwner(world, toOwnerId) ||
                price < 0 || (kind == CellPropertyTransferKind.Purchase ||
                              kind == CellPropertyTransferKind.Sale) && price <= 0)
            {
                throw new InvalidOperationException(
                    "Cell property transfer authority or terms are invalid.");
            }
            if (!string.IsNullOrEmpty(authorizingPersonId) &&
                !world.People.Exists(item => item.Id == authorizingPersonId &&
                    item.IsAlive))
            {
                throw new InvalidOperationException(
                    "Cell property transfer authorizer is unavailable.");
            }

            if (price > 0)
            {
                DebitOwner(world, toOwnerId, price);
                CreditOwner(world, fromOwnerId, price);
            }
            var transfer = new CellPropertyTransferState
            {
                Id = "cell_transfer." + world.AbsoluteDay + "." +
                     world.CellPropertyTransfers.Count.ToString("D6"),
                CellId64 = cellId64,
                LocationId = property.LocationId,
                FromOwnerId = fromOwnerId,
                ToOwnerId = toOwnerId,
                Kind = kind,
                Price = price,
                Day = world.AbsoluteDay,
                AuthorizingPersonId = authorizingPersonId ?? string.Empty
            };
            property.OwnerId = toOwnerId;
            property.LastTransferDay = world.AbsoluteDay;
            property.LastTransferPrice = price;
            property.Revision = checked(property.Revision + 1);
            world.CellPropertyTransfers.Add(transfer);
            world.Revision = checked(world.Revision + 1);
            world.Validate();
            return transfer;
        }

        public FacilityConstructionProjectState StartProject(
            WorldState world,
            string locationId,
            ulong cellId64,
            string facilityDefinitionId,
            string ownerId,
            string sponsorPersonId,
            string materialInventoryContainerId,
            string materialProductId,
            long materialQuantity,
            int requiredLaborMinutes,
            int constructionDays,
            long moneyCost)
        {
            RequireWorld(world);
            var property = world.CellProperties.Find(item =>
                item.CellId64 == cellId64) ??
                throw new InvalidOperationException(
                    "Construction requires a real Cell property record.");
            if (property.LocationId != locationId || property.OwnerId != ownerId ||
                !world.FacilityDefinitions.Exists(item =>
                    item.Id == facilityDefinitionId) ||
                !world.People.Exists(item => item.Id == sponsorPersonId &&
                    item.IsAlive && item.LocationId == locationId) ||
                !world.InventoryContainers.Exists(item =>
                    item.Id == materialInventoryContainerId &&
                    item.LocationId == locationId) ||
                world.Facilities.Exists(item => item.CellId64 == cellId64) ||
                world.FacilityConstructionProjects.Exists(item =>
                    item.CellId64 == cellId64 &&
                    item.Status != FacilityConstructionStatus.Cancelled) ||
                materialQuantity <= 0 || requiredLaborMinutes <= 0 ||
                constructionDays <= 0 || moneyCost < 0)
            {
                throw new InvalidOperationException(
                    "Construction identity, right, resources or duration are invalid.");
            }

            var materials = ReserveMaterial(
                world,
                materialInventoryContainerId,
                materialProductId,
                materialQuantity);
            if (moneyCost > 0)
            {
                DebitOwner(world, ownerId, moneyCost);
            }
            var project = new FacilityConstructionProjectState
            {
                Id = "facility_construction." + world.AbsoluteDay + "." +
                     world.FacilityConstructionProjects.Count.ToString("D6"),
                LocationId = locationId,
                CellId64 = cellId64,
                FacilityDefinitionId = facilityDefinitionId,
                Kind = FacilityConstructionProjectKind.NewBuild,
                TargetFacilityId = string.Empty,
                OwnerId = ownerId,
                SponsorPersonId = sponsorPersonId,
                MaterialInventoryContainerId = materialInventoryContainerId,
                StartedDay = world.AbsoluteDay,
                EarliestCompletionDay = checked(
                    world.AbsoluteDay + constructionDays),
                RequiredLaborMinutes = requiredLaborMinutes,
                CompletedLaborMinutes = 0,
                MoneyCost = moneyCost,
                Status = FacilityConstructionStatus.Planned,
                Materials = materials
            };
            world.FacilityConstructionProjects.Add(project);
            RecordReservedMaterials(world, project);
            world.Revision = checked(world.Revision + 1);
            world.Validate();
            return project;
        }

        public FacilityConstructionProjectState StartFacilityWork(
            WorldState world,
            string facilityId,
            FacilityConstructionProjectKind kind,
            string sponsorPersonId,
            string materialInventoryContainerId,
            string materialProductId,
            long materialQuantity,
            int requiredLaborMinutes,
            int constructionDays,
            long moneyCost)
        {
            return StartFacilityWork(world, facilityId, kind, sponsorPersonId,
                materialInventoryContainerId,
                new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    { materialProductId, materialQuantity }
                }, requiredLaborMinutes, constructionDays, moneyCost);
        }

        public FacilityConstructionProjectState StartFacilityWork(
            WorldState world,
            string facilityId,
            FacilityConstructionProjectKind kind,
            string sponsorPersonId,
            string materialInventoryContainerId,
            IReadOnlyDictionary<string, long> materialRequirements,
            int requiredLaborMinutes,
            int constructionDays,
            long moneyCost)
        {
            RequireWorld(world);
            if (kind == FacilityConstructionProjectKind.NewBuild)
                throw new InvalidOperationException("Use StartProject for a new build.");
            var facility = world.Facilities.Find(item => item.Id == facilityId) ??
                throw new InvalidOperationException("Target Facility is missing.");
            var property = world.CellProperties.Find(item =>
                item.CellId64 == facility.CellId64) ??
                throw new InvalidOperationException("Facility Cell has no property record.");
            if (property.OwnerId != facility.OwnerId ||
                facility.LifecycleStatus == FacilityLifecycleStatus.Destroyed &&
                    kind != FacilityConstructionProjectKind.Repair ||
                !world.People.Exists(item => item.Id == sponsorPersonId &&
                    item.IsAlive && item.LocationId == facility.SettlementId) ||
                !world.InventoryContainers.Exists(item =>
                    item.Id == materialInventoryContainerId &&
                    item.LocationId == facility.SettlementId) ||
                world.FacilityConstructionProjects.Exists(item =>
                    item.TargetFacilityId == facility.Id &&
                    item.Status != FacilityConstructionStatus.Completed &&
                    item.Status != FacilityConstructionStatus.Cancelled) ||
                materialRequirements == null ||
                materialRequirements.Count == 0 ||
                requiredLaborMinutes <= 0 ||
                constructionDays <= 0 || moneyCost < 0)
            {
                throw new InvalidOperationException(
                    "Facility work right, resources or duration are invalid.");
            }
            var materials = ReserveMaterials(world,
                materialInventoryContainerId, materialRequirements);
            if (moneyCost > 0) DebitOwner(world, facility.OwnerId, moneyCost);
            var project = new FacilityConstructionProjectState
            {
                Id = "facility_work." + world.AbsoluteDay + "." +
                     world.FacilityConstructionProjects.Count.ToString("D6"),
                LocationId = facility.SettlementId,
                CellId64 = facility.CellId64,
                FacilityDefinitionId = facility.DefinitionId,
                Kind = kind,
                TargetFacilityId = facility.Id,
                OwnerId = facility.OwnerId,
                SponsorPersonId = sponsorPersonId,
                MaterialInventoryContainerId = materialInventoryContainerId,
                StartedDay = world.AbsoluteDay,
                EarliestCompletionDay = checked(world.AbsoluteDay + constructionDays),
                RequiredLaborMinutes = requiredLaborMinutes,
                MoneyCost = moneyCost,
                Status = FacilityConstructionStatus.Planned,
                Materials = materials
            };
            world.FacilityConstructionProjects.Add(project);
            RecordReservedMaterials(world, project);
            world.Revision = checked(world.Revision + 1);
            world.Validate();
            return project;
        }

        public void CancelProject(WorldState world, string projectId)
        {
            RequireWorld(world);
            var project = FindProject(world, projectId);
            if (project.Status == FacilityConstructionStatus.Completed ||
                project.Status == FacilityConstructionStatus.Cancelled)
                throw new InvalidOperationException("Project can no longer be cancelled.");
            var transaction = ProductInventorySystem.NewTransaction(
                world, InventoryTransactionType.FacilityConstructionMaterialReleased,
                project.SponsorPersonId, string.Empty, 0, 0, 0,
                "Released materials for cancelled " + project.Id + ".");
            transaction.SourceFacilityConstructionProjectId = project.Id;
            foreach (var material in project.Materials)
            {
                var batch = world.ProductBatches.Find(item => item.Id == material.BatchId) ??
                    throw new InvalidOperationException("Reserved construction batch disappeared.");
                if (batch.ReservedQuantity < material.ReservedQuantity)
                    throw new InvalidOperationException("Construction reservation is no longer conserved.");
                batch.ReservedQuantity -= material.ReservedQuantity;
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, 0, -material.ReservedQuantity));
            }
            project.Status = FacilityConstructionStatus.Cancelled;
            world.InventoryTransactions.Add(transaction);
            world.Revision = checked(world.Revision + 1);
            world.Validate();
        }

        public void AbandonFacility(WorldState world, string facilityId, string ownerId)
        {
            RequireWorld(world);
            var facility = world.Facilities.Find(item => item.Id == facilityId) ??
                throw new InvalidOperationException("Target Facility is missing.");
            if (facility.OwnerId != ownerId ||
                facility.LifecycleStatus == FacilityLifecycleStatus.Destroyed)
                throw new InvalidOperationException("Facility abandonment authority is invalid.");
            facility.LifecycleStatus = FacilityLifecycleStatus.Abandoned;
            facility.ControllerId = string.Empty;
            world.Revision = checked(world.Revision + 1);
            world.Validate();
        }

        public FacilityConstructionLaborState ContributeLabor(
            WorldState world,
            string projectId,
            string workerPersonId,
            int laborMinutes)
        {
            RequireWorld(world);
            var project = FindProject(world, projectId);
            if (project.Status == FacilityConstructionStatus.Completed ||
                project.Status == FacilityConstructionStatus.Cancelled ||
                laborMinutes <= 0 ||
                !world.People.Exists(item => item.Id == workerPersonId &&
                    item.IsAlive && item.LocationId == project.LocationId) ||
                world.Journeys.Exists(item => item.PersonId == workerPersonId) ||
                world.FacilityConstructionLabor.Exists(item =>
                    item.WorkerPersonId == workerPersonId &&
                    item.Day == world.AbsoluteDay))
            {
                throw new InvalidOperationException(
                    "Construction labour contribution is invalid.");
            }
            var accepted = Math.Min(
                laborMinutes,
                project.RequiredLaborMinutes - project.CompletedLaborMinutes);
            if (accepted <= 0)
            {
                throw new InvalidOperationException(
                    "Construction labour requirement is already met.");
            }
            var labor = new FacilityConstructionLaborState
            {
                Id = "facility_construction_labor." + world.AbsoluteDay + "." +
                     world.FacilityConstructionLabor.Count.ToString("D6"),
                ProjectId = project.Id,
                WorkerPersonId = workerPersonId,
                Day = world.AbsoluteDay,
                LaborMinutes = accepted
            };
            project.CompletedLaborMinutes = checked(
                project.CompletedLaborMinutes + accepted);
            project.Status = FacilityConstructionStatus.InProgress;
            world.FacilityConstructionLabor.Add(labor);
            world.Revision = checked(world.Revision + 1);
            world.Validate();
            return labor;
        }

        public FacilityState TryComplete(
            WorldState world,
            string projectId)
        {
            return TryCompleteCore(world, projectId, true);
        }

        internal FacilityState TryCompleteDeferredValidation(
            WorldState world,
            string projectId)
        {
            return TryCompleteCore(world, projectId, false);
        }

        private FacilityState TryCompleteCore(
            WorldState world,
            string projectId,
            bool validateAfter)
        {
            RequireWorld(world);
            var project = FindProject(world, projectId);
            if (project.Status == FacilityConstructionStatus.Completed)
            {
                return world.Facilities.Find(item =>
                    item.Id == project.ResultFacilityId);
            }
            if (project.Status == FacilityConstructionStatus.Cancelled ||
                project.CompletedLaborMinutes < project.RequiredLaborMinutes ||
                world.AbsoluteDay < project.EarliestCompletionDay)
            {
                return null;
            }

            ConsumeReservedMaterials(world, project);
            if (project.Kind != FacilityConstructionProjectKind.NewBuild)
            {
                var target = world.Facilities.Find(item =>
                    item.Id == project.TargetFacilityId) ??
                    throw new InvalidOperationException("Target Facility disappeared.");
                if (project.Kind == FacilityConstructionProjectKind.Repair)
                {
                    target.ConditionBasisPoints = 10_000;
                    target.LifecycleStatus = FacilityLifecycleStatus.Operational;
                    target.ControllerId = target.OwnerId;
                }
                else
                {
                    target.RuntimeExpansionLevel = checked(
                        target.RuntimeExpansionLevel + 1);
                    target.StorageCapacity = checked(target.StorageCapacity +
                        Math.Max(1L, target.StorageCapacity / 4L));
                }
                project.Status = FacilityConstructionStatus.Completed;
                project.CompletedDay = world.AbsoluteDay;
                project.ResultFacilityId = target.Id;
                world.Revision = checked(world.Revision + 1);
                if (validateAfter) world.Validate();
                return target;
            }
            var definition = world.FacilityDefinitions.Find(item =>
                item.Id == project.FacilityDefinitionId);
            var facility = new FacilityState
            {
                Id = "facility.runtime." + project.CellId64 + "." +
                     world.Facilities.Count.ToString("D6"),
                DisplayName = definition.DisplayName,
                DefinitionId = definition.Id,
                CellId64 = project.CellId64,
                OwnerId = project.OwnerId,
                ControllerId = project.OwnerId,
                AdministrativeControllerId = world.CellProperties.Find(item =>
                    item.CellId64 == project.CellId64)
                    ?.AdministrativeControllerId ?? string.Empty,
                SettlementId = project.LocationId,
                HistoricalConfidence = HistoricalConfidenceLevel.GameplayReconstruction,
                SpatialPrecision = HistoricalSpatialPrecision.Confirmed,
                SourceNote = "Runtime construction " + project.Id,
                LifecycleStatus = FacilityLifecycleStatus.Operational
            };
            world.Facilities.Add(facility);
            project.Status = FacilityConstructionStatus.Completed;
            project.CompletedDay = world.AbsoluteDay;
            project.ResultFacilityId = facility.Id;
            world.Revision = checked(world.Revision + 1);
            if (validateAfter) world.Validate();
            return facility;
        }

        private static List<FacilityConstructionMaterialState> ReserveMaterial(
            WorldState world,
            string containerId,
            string productId,
            long quantity)
        {
            var candidates = world.ProductBatches.FindAll(item =>
                item.InventoryContainerId == containerId &&
                item.ProductDefinitionId == productId &&
                item.Quantity > item.ReservedQuantity);
            candidates.Sort((left, right) => string.CompareOrdinal(
                left.Id, right.Id));
            var materials = new List<FacilityConstructionMaterialState>();
            var remaining = quantity;
            foreach (var batch in candidates)
            {
                var reserve = Math.Min(
                    remaining,
                    batch.Quantity - batch.ReservedQuantity);
                if (reserve <= 0) continue;
                batch.ReservedQuantity = checked(batch.ReservedQuantity + reserve);
                materials.Add(new FacilityConstructionMaterialState
                {
                    BatchId = batch.Id,
                    ProductDefinitionId = productId,
                    ReservedQuantity = reserve
                });
                remaining -= reserve;
                if (remaining == 0) break;
            }
            if (remaining != 0)
            {
                foreach (var material in materials)
                {
                    var batch = world.ProductBatches.Find(item =>
                        item.Id == material.BatchId);
                    batch.ReservedQuantity -= material.ReservedQuantity;
                }
                throw new InvalidOperationException(
                    "Construction materials are not backed by real inventory.");
            }
            return materials;
        }

        private static List<FacilityConstructionMaterialState> ReserveMaterials(
            WorldState world,
            string containerId,
            IReadOnlyDictionary<string, long> requirements)
        {
            var productIds = new List<string>(requirements.Keys);
            productIds.Sort(StringComparer.Ordinal);
            var materials = new List<FacilityConstructionMaterialState>();
            try
            {
                foreach (var productId in productIds)
                {
                    _ = new StableId(productId);
                    var quantity = requirements[productId];
                    if (quantity <= 0)
                        throw new InvalidOperationException(
                            "Construction material quantities must be positive.");
                    var candidates = world.ProductBatches.FindAll(item =>
                        item.InventoryContainerId == containerId &&
                        item.ProductDefinitionId == productId &&
                        item.Quantity > item.ReservedQuantity);
                    candidates.Sort((left, right) =>
                    {
                        var byDay = left.ProducedDay.CompareTo(
                            right.ProducedDay);
                        return byDay != 0
                            ? byDay
                            : string.CompareOrdinal(left.Id, right.Id);
                    });
                    var remaining = quantity;
                    foreach (var batch in candidates)
                    {
                        var reserve = Math.Min(remaining,
                            batch.Quantity - batch.ReservedQuantity);
                        if (reserve <= 0) continue;
                        batch.ReservedQuantity = checked(
                            batch.ReservedQuantity + reserve);
                        materials.Add(new FacilityConstructionMaterialState
                        {
                            BatchId = batch.Id,
                            ProductDefinitionId = productId,
                            ReservedQuantity = reserve
                        });
                        remaining -= reserve;
                        if (remaining == 0) break;
                    }
                    if (remaining != 0)
                        throw new InvalidOperationException(
                            "Construction materials are not backed by real inventory.");
                }
                return materials;
            }
            catch
            {
                foreach (var material in materials)
                {
                    var batch = world.ProductBatches.Find(item =>
                        item.Id == material.BatchId);
                    if (batch != null)
                        batch.ReservedQuantity -= material.ReservedQuantity;
                }
                throw;
            }
        }

        private static void ConsumeReservedMaterials(
            WorldState world,
            FacilityConstructionProjectState project)
        {
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.FacilityConstructionMaterialConsumed,
                project.SponsorPersonId,
                string.Empty,
                0,
                0,
                0,
                "Consumed reserved materials for " + project.Id + ".");
            transaction.SourceFacilityConstructionProjectId = project.Id;
            foreach (var material in project.Materials)
            {
                var batch = world.ProductBatches.Find(item =>
                    item.Id == material.BatchId) ??
                    throw new InvalidOperationException(
                        "Reserved construction batch disappeared.");
                if (batch.ReservedQuantity < material.ReservedQuantity ||
                    batch.Quantity < material.ReservedQuantity)
                {
                    throw new InvalidOperationException(
                        "Reserved construction material is no longer conserved.");
                }
                batch.Quantity -= material.ReservedQuantity;
                batch.ReservedQuantity -= material.ReservedQuantity;
                material.ConsumedQuantity = material.ReservedQuantity;
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, -material.ReservedQuantity,
                    -material.ReservedQuantity));
            }
            world.InventoryTransactions.Add(transaction);
        }

        private static void RecordReservedMaterials(
            WorldState world,
            FacilityConstructionProjectState project)
        {
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.FacilityConstructionMaterialReserved,
                project.SponsorPersonId,
                string.Empty,
                0,
                0,
                0,
                "Reserved materials for " + project.Id + ".");
            transaction.SourceFacilityConstructionProjectId = project.Id;
            foreach (var material in project.Materials)
            {
                var batch = world.ProductBatches.Find(item =>
                    item.Id == material.BatchId) ??
                    throw new InvalidOperationException(
                        "Reserved construction batch disappeared.");
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, 0, material.ReservedQuantity));
            }
            world.InventoryTransactions.Add(transaction);
        }

        private static FacilityConstructionProjectState FindProject(
            WorldState world,
            string id) => world.FacilityConstructionProjects.Find(item =>
                item.Id == id) ?? throw new InvalidOperationException(
                "Missing Facility construction project " + id + ".");

        private static bool HasLocation(WorldState world, string locationId) =>
            world.Locations.Exists(item => item.Id == locationId);

        private static bool HasOwner(WorldState world, string ownerId) =>
            world.Families.Exists(item => item.Id == ownerId) ||
            world.Organizations.Exists(item => item.Id == ownerId);

        private static void DebitOwner(
            WorldState world,
            string ownerId,
            long amount)
        {
            var family = world.Families.Find(item => item.Id == ownerId);
            if (family != null)
            {
                if (family.Wealth < amount)
                    throw new InvalidOperationException("Family funds are insufficient.");
                family.Wealth -= amount;
                return;
            }
            var organization = world.Organizations.Find(item => item.Id == ownerId) ??
                throw new InvalidOperationException("Property owner is missing.");
            if (organization.Treasury < amount)
                throw new InvalidOperationException("Organization funds are insufficient.");
            organization.Treasury -= amount;
        }

        private static void CreditOwner(
            WorldState world,
            string ownerId,
            long amount)
        {
            var family = world.Families.Find(item => item.Id == ownerId);
            if (family != null)
            {
                family.Wealth = checked(family.Wealth + amount);
                return;
            }
            var organization = world.Organizations.Find(item => item.Id == ownerId) ??
                throw new InvalidOperationException("Property owner is missing.");
            organization.Treasury = checked(organization.Treasury + amount);
        }

        private static void RequireWorld(WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.Validate();
        }
    }

    public sealed class HouseholdMigrationSystem
    {
        public HouseholdMigrationState Start(
            WorldState world,
            string familyId,
            string targetLocationId,
            string routeId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.Validate();
            var family = world.Families.Find(item => item.Id == familyId) ??
                throw new InvalidOperationException("Migration family is missing.");
            if (world.HouseholdMigrations.Exists(item =>
                    item.FamilyId == familyId && !item.IsCompleted))
                throw new InvalidOperationException("Household is already migrating.");
            var route = world.Routes.Find(item => item.Id == routeId) ??
                throw new InvalidOperationException("Migration route is missing.");
            var forward = route.FromLocationId == family.LocationId &&
                          route.ToLocationId == targetLocationId;
            var backward = route.Bidirectional &&
                           route.ToLocationId == family.LocationId &&
                           route.FromLocationId == targetLocationId;
            if (!forward && !backward)
                throw new InvalidOperationException("Migration route does not connect target.");

            var migration = new HouseholdMigrationState
            {
                Id = "household_migration." + family.Id + "." +
                     world.AbsoluteDay + "." + world.HouseholdMigrations.Count,
                FamilyId = family.Id,
                OriginLocationId = family.LocationId,
                DestinationLocationId = targetLocationId,
                RouteId = route.Id,
                StartedDay = world.AbsoluteDay
            };
            var travel = new TravelSystem();
            var members = new List<string>(family.MemberIds);
            members.Sort(StringComparer.Ordinal);
            foreach (var memberId in members)
            {
                var person = world.People.Find(item => item.Id == memberId);
                if (person == null || !person.IsAlive ||
                    person.LocationId != family.LocationId) continue;
                migration.JourneyIds.Add(travel.StartJourney(
                    world,
                    new StableId(person.Id),
                    new StableId(route.Id),
                    new StableId(targetLocationId),
                    TravelMode.Foot).Id);
            }
            if (migration.JourneyIds.Count == 0)
                throw new InvalidOperationException("No living household member can migrate.");
            world.HouseholdMigrations.Add(migration);
            world.Revision = checked(world.Revision + 1);
            world.Validate();
            return migration;
        }

        public int CompleteArrivals(WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var completed = 0;
            foreach (var migration in world.HouseholdMigrations)
            {
                if (migration.IsCompleted) continue;
                var anyJourney = migration.JourneyIds.Exists(id =>
                    world.Journeys.Exists(item => item.Id == id));
                if (anyJourney) continue;
                var family = world.Families.Find(item =>
                    item.Id == migration.FamilyId);
                if (family == null) continue;
                family.LocationId = migration.DestinationLocationId;
                migration.IsCompleted = true;
                migration.CompletedDay = world.AbsoluteDay;
                completed++;
            }
            if (completed > 0) world.Revision = checked(world.Revision + 1);
            world.Validate();
            return completed;
        }
    }
}
