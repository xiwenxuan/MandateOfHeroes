using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    /// <summary>
    /// Property and construction adapter over the single Luoyang compact world
    /// account. All money, materials, labourers and Facilities referenced here
    /// are existing runtime facts; no presentation-only ownership is created.
    /// </summary>
    public sealed class Luoyang184PropertyConstructionRuntimeSystem
    {
        private static readonly string[] ConstructionProducts =
        {
            CoreProductionContent.TimberMaterialProductId,
            "product.reference.building_material",
            "product.material.iron"
        };

        public LuoyangCellPropertyTransferRuntimeState Transfer(
            Luoyang184LivingWorldRuntimeState runtime,
            ulong cellId64,
            string fromOwnerId,
            string toOwnerId,
            long price,
            string authorizingPersonId)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            var property = runtime.CellProperties.Find(item =>
                item.CellId64 == cellId64) ?? throw new InvalidOperationException(
                "The Cell has no property record.");
            if (property.OwnerId != fromOwnerId || string.IsNullOrWhiteSpace(toOwnerId) ||
                fromOwnerId == toOwnerId || price < 0)
                throw new InvalidOperationException("Property transfer terms are invalid.");
            if (price > 0)
            {
                Debit(runtime, toOwnerId, price);
                Credit(runtime, fromOwnerId, price);
            }
            var transfer = new LuoyangCellPropertyTransferRuntimeState
            {
                Id = "luoyang.cell_transfer." + runtime.AbsoluteDay + "." +
                     runtime.CellPropertyTransfers.Count.ToString("D6"),
                CellId64 = cellId64,
                FromOwnerId = fromOwnerId,
                ToOwnerId = toOwnerId,
                Price = price,
                Day = runtime.AbsoluteDay,
                AuthorizingPersonId = authorizingPersonId ?? string.Empty
            };
            property.OwnerId = toOwnerId;
            property.BuildingRightHolderId = toOwnerId;
            property.LastTransferDay = runtime.AbsoluteDay;
            property.LastTransferPrice = price;
            property.Revision++;
            var facility = runtime.Facilities.Find(item =>
                item.FacilityId == property.FacilityId);
            if (facility != null) facility.OwnerId = toOwnerId;
            runtime.CellPropertyTransfers.Add(transfer);
            return transfer;
        }

        public LuoyangCompactConstructionProjectState Start(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangCompactConstructionKind kind,
            ulong cellId64,
            string targetFacilityId,
            string facilityDefinitionId,
            string ownerId,
            string requestedByAgentId,
            int constructionDays,
            long moneyCost,
            bool requireOwnerMaterials = false)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            var property = runtime.CellProperties.Find(item =>
                item.CellId64 == cellId64);
            var target = runtime.Facilities.Find(item =>
                item.FacilityId == targetFacilityId);
            if (property == null || property.OwnerId != ownerId ||
                property.BuildingRightHolderId != ownerId ||
                constructionDays <= 0 || moneyCost < 0 ||
                runtime.ConstructionProjects.Exists(item => !item.Completed &&
                    !item.Cancelled && item.CellId64 == cellId64))
                throw new InvalidOperationException("Construction right or terms are invalid.");
            if (kind == LuoyangCompactConstructionKind.NewBuild)
            {
                if (target != null || !string.IsNullOrEmpty(property.FacilityId) ||
                    string.IsNullOrWhiteSpace(facilityDefinitionId))
                    throw new InvalidOperationException("New construction requires an empty owned Cell.");
            }
            else if (target == null || target.CellId64 != cellId64 ||
                     target.OwnerId != ownerId)
            {
                throw new InvalidOperationException("Facility work requires the owned target Facility.");
            }

            var laborers = SelectLaborers(runtime, target, 4);
            if (laborers.Count < 4)
                throw new InvalidOperationException("Four real permanent labourers are required.");
            var materials = ReserveAndConsumeMaterials(runtime, kind, ownerId,
                requireOwnerMaterials);
            if (moneyCost > 0)
            {
                Debit(runtime, ownerId, moneyCost);
                PayConstructionLabor(runtime, laborers, moneyCost);
            }
            var project = new LuoyangCompactConstructionProjectState
            {
                Id = "luoyang.construction." + runtime.AbsoluteDay + "." +
                     runtime.ConstructionProjects.Count.ToString("D6"),
                Kind = kind,
                CellId64 = cellId64,
                TargetFacilityId = targetFacilityId ?? string.Empty,
                FacilityDefinitionId = facilityDefinitionId ?? target?.DefinitionId,
                OwnerId = ownerId,
                StartedDay = runtime.AbsoluteDay,
                CompletionDay = checked(runtime.AbsoluteDay + constructionDays),
                RequiredLaborers = 4,
                LaborerPersonOrdinals = laborers,
                MoneyCost = moneyCost,
                RequestedByAgentId = requestedByAgentId ?? string.Empty,
                Materials = materials
            };
            var primary = materials[0];
            project.MaterialInventoryId = primary.InventoryId;
            project.MaterialProductId = primary.ProductId;
            project.MaterialQuantityMilliunits = materials.Sum(item =>
                item.ConsumedMilliunits);
            runtime.ConstructionProjects.Add(project);
            foreach (var material in materials)
            {
                runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                {
                    Id = "flow.construction." + runtime.AbsoluteDay + "." +
                         runtime.InventoryFlows.Count,
                    Day = runtime.AbsoluteDay,
                    OperationId = "construction.material_consumed",
                    ProductId = material.ProductId,
                    SourceInventoryId = material.InventoryId,
                    QuantityMilliunits = 0,
                    LossMilliunits = material.ConsumedMilliunits,
                    FacilityId = targetFacilityId ?? string.Empty
                });
            }
            return project;
        }

        public void Advance(Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var project in runtime.ConstructionProjects.Where(item =>
                         !item.Completed && !item.Cancelled &&
                         item.CompletionDay <= runtime.AbsoluteDay)
                     .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                var target = runtime.Facilities.Find(item =>
                    item.FacilityId == project.TargetFacilityId);
                if (project.Kind == LuoyangCompactConstructionKind.Repair)
                {
                    if (target == null) continue;
                    target.ConditionBasisPoints = 10_000;
                    project.ResultFacilityId = target.FacilityId;
                }
                else if (project.Kind == LuoyangCompactConstructionKind.Expansion)
                {
                    if (target == null) continue;
                    target.RuntimeExpansionLevel++;
                    target.OptimalWorkers = checked(target.OptimalWorkers + 1);
                    project.ResultFacilityId = target.FacilityId;
                }
                else
                {
                    var property = runtime.CellProperties.Find(item =>
                        item.CellId64 == project.CellId64);
                    if (property == null || !string.IsNullOrEmpty(property.FacilityId))
                        continue;
                    var facility = new LuoyangFacilityProductionRuntimeState
                    {
                        FacilityIndex = runtime.Facilities.Count,
                        FacilityId = "facility.runtime.luoyang.t4." +
                                     runtime.Facilities.Count.ToString("D6"),
                        DefinitionId = project.FacilityDefinitionId,
                        OwnerId = project.OwnerId,
                        CellId64 = project.CellId64,
                        MinimumWorkers = 1,
                        OptimalWorkers = 4,
                        Status = LuoyangProductionRuntimeStatus.Idle,
                        ConditionBasisPoints = 10_000
                    };
                    ConfigureNewFacilityRuntime(facility);
                    runtime.Facilities.Add(facility);
                    foreach (var ordinal in project.LaborerPersonOrdinals)
                    {
                        var worker = runtime.Workforce[(int)ordinal];
                        if (worker.Status == LuoyangWorkforceStatus.Assigned &&
                            worker.FacilityIndex < runtime.Facilities.Count - 1)
                        {
                            var former = runtime.Facilities[(int)worker.FacilityIndex];
                            former.AssignedWorkers = Math.Max(0,
                                former.AssignedWorkers - 1);
                        }
                        worker.Status = LuoyangWorkforceStatus.Assigned;
                        worker.FacilityIndex = (uint)facility.FacilityIndex;
                        worker.CurrentActivityId = "activity.work";
                        facility.AssignedWorkers++;
                    }
                    if (!string.IsNullOrEmpty(facility.InputProductId))
                    {
                        runtime.Inventories.Add(NewFacilityInventory(facility,
                            facility.InputProductId));
                    }
                    if (!string.IsNullOrEmpty(facility.OutputProductId))
                    {
                        var output = NewFacilityInventory(facility,
                            facility.OutputProductId);
                        runtime.Inventories.Add(output);
                        facility.OutputInventoryId = output.Id;
                    }
                    if (facility.DefinitionId.IndexOf("warehouse",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        facility.DefinitionId.IndexOf("market",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var storage = NewFacilityInventory(facility,
                            CoreProductionContent.WheatGrainProductId);
                        runtime.Inventories.Add(storage);
                        facility.OutputInventoryId = storage.Id;
                    }
                    property.FacilityId = facility.FacilityId;
                    project.ResultFacilityId = facility.FacilityId;
                }
                project.Completed = true;
            }
        }

        public void Abandon(Luoyang184LivingWorldRuntimeState runtime,
            string facilityId, string ownerId)
        {
            var facility = runtime?.Facilities.Find(item =>
                item.FacilityId == facilityId) ?? throw new InvalidOperationException(
                "Facility is missing.");
            if (facility.OwnerId != ownerId)
                throw new InvalidOperationException("Facility abandonment authority is invalid.");
            facility.ConditionBasisPoints = 0;
            facility.Status = LuoyangProductionRuntimeStatus.Maintenance;
            facility.StopReasonId = "facility.abandoned_by_owner";
            facility.AssignedWorkers = 0;
        }

        private static void ConfigureNewFacilityRuntime(
            LuoyangFacilityProductionRuntimeState facility)
        {
            if (facility.DefinitionId.IndexOf("industry",
                    StringComparison.OrdinalIgnoreCase) < 0 &&
                facility.DefinitionId.IndexOf("workshop",
                    StringComparison.OrdinalIgnoreCase) < 0 &&
                facility.DefinitionId.IndexOf("mill",
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                facility.Status = LuoyangProductionRuntimeStatus.Idle;
                return;
            }
            facility.RecipeId = CoreProductionContent.HandMillWheatRecipeId;
            facility.InputProductId = CoreProductionContent.WheatGrainProductId;
            facility.OutputProductId = CoreProductionContent.WheatFlourProductId;
            facility.InputQuantity = 100_000;
            facility.OutputQuantity = 85_000;
        }

        private static LuoyangInventoryBalanceState NewFacilityInventory(
            LuoyangFacilityProductionRuntimeState facility, string productId) =>
            new LuoyangInventoryBalanceState
            {
                Id = "inventory.luoyang.184." + facility.FacilityId + "." +
                     productId,
                OwnerKind = LuoyangInventoryOwnerKind.Household,
                OwnerId = facility.OwnerId,
                FacilityId = facility.FacilityId,
                ProductId = productId,
                CapacityMilliunits = 1_000_000
            };

        private static List<LuoyangCompactConstructionMaterialState>
            ReserveAndConsumeMaterials(Luoyang184LivingWorldRuntimeState runtime,
                LuoyangCompactConstructionKind kind, string ownerId,
                bool requireOwnerMaterials)
        {
            var result = new List<LuoyangCompactConstructionMaterialState>();
            var required = kind == LuoyangCompactConstructionKind.Repair
                ? 5_000L : 10_000L;
            foreach (var product in ConstructionProducts)
            {
                var inventory = runtime.Inventories.Where(item =>
                        item.ProductId == product &&
                        (!requireOwnerMaterials || item.OwnerId == ownerId) &&
                        item.QuantityMilliunits >= required)
                    .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
                if (inventory == null) continue;
                inventory.QuantityMilliunits -= required;
                result.Add(new LuoyangCompactConstructionMaterialState
                {
                    InventoryId = inventory.Id,
                    ProductId = product,
                    ConsumedMilliunits = required
                });
            }
            if (result.Count < 2)
            {
                foreach (var material in result)
                {
                    var inventory = runtime.Inventories.Find(item =>
                        item.Id == material.InventoryId);
                    if (inventory != null)
                        inventory.QuantityMilliunits += material.ConsumedMilliunits;
                }
                throw new InvalidOperationException(
                    "At least two applicable real construction material classes are required.");
            }
            return result;
        }

        private static List<uint> SelectLaborers(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangFacilityProductionRuntimeState target, int count)
        {
            var result = new List<uint>();
            if (target != null)
            {
                result.AddRange(runtime.Workforce.Where(item =>
                        item.Status == LuoyangWorkforceStatus.Assigned &&
                        item.FacilityIndex == target.FacilityIndex)
                    .OrderBy(item => item.PersonOrdinal).Take(count)
                    .Select(item => item.PersonOrdinal));
            }
            if (result.Count < count)
            {
                result.AddRange(runtime.Workforce.Where(item =>
                        item.Status == LuoyangWorkforceStatus.Unemployed &&
                        !result.Contains(item.PersonOrdinal))
                    .OrderBy(item => item.PersonOrdinal).Take(count - result.Count)
                    .Select(item => item.PersonOrdinal));
            }
            if (result.Count < count)
            {
                result.AddRange(runtime.Workforce.Where(item =>
                        item.Status == LuoyangWorkforceStatus.Assigned &&
                        !result.Contains(item.PersonOrdinal))
                    .OrderBy(item => item.PersonOrdinal).Take(count - result.Count)
                    .Select(item => item.PersonOrdinal));
            }
            return result;
        }

        private static void Debit(Luoyang184LivingWorldRuntimeState runtime,
            string ownerId, long amount)
        {
            if (ownerId == runtime.GovernmentEconomy.OrganizationId)
            {
                if (runtime.GovernmentEconomy.Treasury < amount)
                    throw new InvalidOperationException("Government treasury is insufficient.");
                runtime.GovernmentEconomy.Treasury -= amount;
                runtime.GovernmentEconomy.ConstructionExpense += amount;
                return;
            }
            var family = runtime.FamilyOrganizations.Find(item => item.Id == ownerId);
            if (family != null)
            {
                if (family.Funds < amount)
                    throw new InvalidOperationException("Family funds are insufficient.");
                family.Funds -= amount;
                return;
            }
            var household = runtime.Households.Find(item =>
                item.HouseholdId == ownerId);
            if (household != null)
            {
                if (household.Wealth < amount)
                    throw new InvalidOperationException("Household wealth is insufficient.");
                household.Wealth -= amount;
                return;
            }
            var supplier = runtime.ExternalSuppliers.Find(item =>
                item.OrganizationId == ownerId);
            if (supplier == null || supplier.CashBalance < amount)
                throw new InvalidOperationException("Owner has no sufficient money ledger.");
            supplier.CashBalance -= amount;
        }

        private static void Credit(Luoyang184LivingWorldRuntimeState runtime,
            string ownerId, long amount)
        {
            if (ownerId == runtime.GovernmentEconomy.OrganizationId)
                runtime.GovernmentEconomy.Treasury += amount;
            else
            {
                var family = runtime.FamilyOrganizations.Find(item => item.Id == ownerId);
                if (family != null) family.Funds += amount;
                else
                {
                    var household = runtime.Households.Find(item =>
                        item.HouseholdId == ownerId);
                    if (household != null) household.Wealth += amount;
                    else
                    {
                        var supplier = runtime.ExternalSuppliers.Find(item =>
                            item.OrganizationId == ownerId);
                        if (supplier == null)
                            throw new InvalidOperationException("Owner has no money ledger.");
                        supplier.CashBalance += amount;
                    }
                }
            }
        }

        private static void PayConstructionLabor(
            Luoyang184LivingWorldRuntimeState runtime,
            IReadOnlyList<uint> laborers,
            long amount)
        {
            if (amount <= 0 || laborers.Count == 0) return;
            var households = laborers.Select(ordinal =>
                    runtime.Workforce[(int)ordinal].HouseholdOrdinal)
                .Distinct().OrderBy(item => item).ToArray();
            var quotient = amount / households.Length;
            var remainder = amount % households.Length;
            for (var index = 0; index < households.Length; index++)
                runtime.Households[(int)households[index]].Wealth +=
                    quotient + (index < remainder ? 1 : 0);
        }
    }
}
