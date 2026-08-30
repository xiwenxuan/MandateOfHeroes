using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class LuoyangGoldenSliceProjection
    {
        public string Id = "luoyang.golden_slice.v1";
        public string RegionalStyleId = "regional_style.central_plains.han.v1";
        public List<FacilityVisualAnchor> FacilityAnchors =
            new List<FacilityVisualAnchor>();
        public List<PersonVisualRepresentation> Actors =
            new List<PersonVisualRepresentation>();
        public List<ShipmentVisualRepresentation> Shipments =
            new List<ShipmentVisualRepresentation>();
        public List<LuoyangCropRuntimeState> Crops =
            new List<LuoyangCropRuntimeState>();
        public List<RuntimeVisualSpline> RiverSplines =
            new List<RuntimeVisualSpline>();
        public List<RuntimeVisualSpline> RoadSplines =
            new List<RuntimeVisualSpline>();
    }

    /// <summary>Read-only visual projection over the one authoritative runtime.</summary>
    public sealed class LuoyangVisualPresentationSystem
    {
        public const string RegionalStyleId =
            "regional_style.central_plains.han.v1";
        private readonly List<FacilityVisualProfile> _profiles;
        private readonly List<BuildBlueprintDefinition> _blueprints;
        private readonly LuoyangFacilityModelBindingResolver _modelBindings;
        private readonly Dictionary<string, FacilityVisualProfile> _modelProfiles =
            new Dictionary<string, FacilityVisualProfile>(StringComparer.Ordinal);

        public LuoyangVisualPresentationSystem() : this(null, null)
        {
        }

        public LuoyangVisualPresentationSystem(
            LuoyangFacilityModelBindingCatalog bindings,
            HanBuildableFacilityModelCatalog models)
        {
            _profiles = CreateProfiles();
            _blueprints = CreateBlueprints();
            if ((bindings == null) != (models == null))
                throw new ArgumentException(
                    "Luoyang model bindings and model catalog must be supplied together.");
            if (bindings == null) return;
            _modelBindings = new LuoyangFacilityModelBindingResolver(bindings, models);
            RegisterModelProfiles(models);
        }

        public IReadOnlyList<FacilityVisualProfile> Profiles => _profiles;
        public IReadOnlyList<BuildBlueprintDefinition> Blueprints => _blueprints;

        public FacilityVisualProfile ResolveProfile(string definitionId,
            string facilityId = null)
        {
            var modelId = _modelBindings?.ResolveModelId(definitionId, facilityId);
            if (!string.IsNullOrEmpty(modelId) &&
                _modelProfiles.TryGetValue(modelId, out var modelProfile))
                return modelProfile;
            var exact = _profiles.FirstOrDefault(item =>
                string.Equals(item.FacilityTypeId, definitionId,
                    StringComparison.Ordinal));
            if (exact != null) return exact;
            var token = Classify(definitionId);
            return _profiles.First(item => item.FacilityTypeId == token);
        }

        public BuildBlueprintDefinition GetBlueprint(string blueprintId) =>
            _blueprints.FirstOrDefault(item => item.BlueprintId == blueprintId);

        public LuoyangGoldenSliceProjection BuildProjection(
            Luoyang184LivingWorldRuntimeState runtime, int actorBudget = 48,
            int facilityBudget = 72)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            var result = new LuoyangGoldenSliceProjection();
            result.RiverSplines.Add(CreateSpline("spline.river.luo.v1",
                "geography.water.luoyang.luo_river", "river", .035f,
                (.00f, .86f), (.22f, .88f), (.48f, .85f), (.72f, .91f),
                (1.00f, .89f)));
            var routeId = runtime.Shipments.OrderBy(item => item.Id,
                    StringComparer.Ordinal).Select(item => item.RouteId)
                .FirstOrDefault(item => !string.IsNullOrEmpty(item)) ??
                "route.luoyang.gate.market.warehouse";
            result.RoadSplines.Add(CreateSpline("spline.road.golden_slice.v1",
                routeId, "major_road", .018f, (.02f, .25f), (.18f, .33f),
                (.37f, .47f), (.58f, .55f), (.82f, .62f)));
            var selected = SelectRepresentativeFacilities(runtime, facilityBudget);
            for (var index = 0; index < selected.Count; index++)
            {
                var facility = selected[index];
                var profile = ResolveProfile(facility.DefinitionId,
                    facility.FacilityId);
                var column = index % 9;
                var row = index / 9;
                result.FacilityAnchors.Add(new FacilityVisualAnchor
                {
                    FacilityId = facility.FacilityId,
                    CellId64 = facility.CellId64,
                    VisualProfileId = profile.VisualProfileId,
                    LocalX = .12f + column * .095f,
                    LocalY = .18f + row * .095f,
                    RotationDegrees = (facility.CellId64 % 4) * 90f,
                    Scale = profile.Importance == FacilityVisualImportance.A
                        ? 1.2f : profile.Importance == FacilityVisualImportance.B
                            ? 1f : .82f,
                    VisualFootprintProfileId = "visual_footprint.single_cell.cluster",
                    EntranceAnchorId = "entrance.primary",
                    RoadConnectionAnchorId = "road.main"
                });
            }
            var selectedIndexes = new HashSet<int>(selected.Select(item =>
                item.FacilityIndex));
            foreach (var person in runtime.Workforce.Where(item =>
                         item.FacilityIndex < runtime.Facilities.Count &&
                         selectedIndexes.Contains((int)item.FacilityIndex))
                     .OrderByDescending(PersonPriority)
                     .ThenBy(item => item.PersonOrdinal).Take(actorBudget))
            {
                var anchor = result.FacilityAnchors.First(item => item.FacilityId ==
                    runtime.Facilities[(int)person.FacilityIndex].FacilityId);
                result.Actors.Add(new PersonVisualRepresentation
                {
                    PersonOrdinal = person.PersonOrdinal,
                    RuntimePersonId = "person.luoyang.184." +
                                      person.PersonOrdinal.ToString("D6"),
                    FacilityId = anchor.FacilityId,
                    LocalX = anchor.LocalX + ((int)(person.PersonOrdinal % 5) - 2) * .006f,
                    LocalY = anchor.LocalY + ((int)(person.PersonOrdinal % 7) - 3) * .005f,
                    Priority = PersonPriority(person)
                });
            }
            foreach (var shipment in runtime.Shipments.Where(item => !item.Delivered)
                         .OrderBy(item => item.Id, StringComparer.Ordinal).Take(6))
            {
                var duration = Math.Max(1, shipment.ArrivalDay - shipment.DispatchDay);
                var progress = (float)Math.Max(0, Math.Min(duration,
                    runtime.AbsoluteDay - shipment.DispatchDay)) / duration;
                result.Shipments.Add(new ShipmentVisualRepresentation
                {
                    ShipmentId = shipment.Id,
                    RouteId = shipment.RouteId,
                    ProductId = shipment.ProductId,
                    CargoMilliunits = shipment.ShippedQuantityMilliunits,
                    Progress01 = progress,
                    RepresentativeVehicleCount = (int)Math.Max(1,
                        Math.Min(8, shipment.ShippedQuantityMilliunits / 100_000))
                });
            }
            result.Crops.AddRange(runtime.Crops.OrderBy(item => item.FieldId,
                StringComparer.Ordinal).Take(12));
            return result;
        }

        private static RuntimeVisualSpline CreateSpline(string id,
            string bindingId, string kindId, float width,
            params (float X, float Y)[] points)
        {
            var result = new RuntimeVisualSpline
            {
                VisualSplineId = id,
                RuntimeBindingId = bindingId,
                KindId = kindId,
                Width = width
            };
            foreach (var point in points)
                result.Points.Add(new VisualSplinePoint { X = point.X, Y = point.Y });
            return result;
        }

        public LuoyangCompactConstructionProjectState StartFromBlueprint(
            Luoyang184LivingWorldRuntimeState runtime, string blueprintId,
            ulong cellId64, string ownerId, string requestedByAgentId)
        {
            var blueprint = GetBlueprint(blueprintId) ??
                throw new InvalidOperationException("Unknown BuildBlueprint.");
            var player = requestedByAgentId != null &&
                requestedByAgentId.StartsWith("player.", StringComparison.Ordinal);
            if (player && (blueprint.Availability & BuildAvailability.Player) == 0)
                throw new InvalidOperationException("Blueprint is not player-buildable.");
            if (!player && (blueprint.Availability & BuildAvailability.Ai) == 0)
                throw new InvalidOperationException("Blueprint is not AI-buildable.");
            return new Luoyang184PropertyConstructionRuntimeSystem().Start(
                runtime, LuoyangCompactConstructionKind.NewBuild, cellId64,
                string.Empty, blueprint.FacilityDefinitionId, ownerId,
                requestedByAgentId, blueprint.ConstructionDays,
                blueprint.RequiredMoney, true);
        }

        public long OrderMissingConstructionMaterials(
            Luoyang184LivingWorldRuntimeState runtime, string blueprintId,
            string ownerId, string requestedByAgentId)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            var blueprint = GetBlueprint(blueprintId) ??
                throw new InvalidOperationException("Unknown BuildBlueprint.");
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new InvalidOperationException("Construction material owner is required.");

            var latestArrival = runtime.AbsoluteDay;
            foreach (var requirement in blueprint.RequiredMaterials.OrderBy(item =>
                         item.ProductId, StringComparer.Ordinal))
            {
                var owned = runtime.Inventories.Where(item =>
                        item.OwnerId == ownerId &&
                        item.ProductId == requirement.ProductId)
                    .Sum(item => item.QuantityMilliunits);
                var inbound = runtime.Shipments.Where(item => !item.Delivered &&
                        item.ProductId == requirement.ProductId &&
                        runtime.Inventories.Any(inventory =>
                            inventory.Id == item.DestinationInventoryId &&
                            inventory.OwnerId == ownerId))
                    .Sum(item => item.DeliveredQuantityMilliunits);
                var missing = requirement.QuantityMilliunits - owned - inbound;
                if (missing <= 0) continue;
                var supplier = runtime.ExternalSuppliers.Where(item =>
                        item.ProductId == requirement.ProductId &&
                        item.Level != LuoyangSupplierMaterializationLevel
                            .DeferredExternalTrade &&
                        item.InventoryQuantityMilliunits > missing)
                    .OrderBy(item => item.TravelDays)
                    .ThenBy(item => item.SupplierId, StringComparer.Ordinal)
                    .FirstOrDefault() ?? throw new InvalidOperationException(
                        "No materialized supplier can fulfil " +
                        requirement.ProductId + ".");
                var destination = GetOrCreateOwnerMaterialInventory(runtime,
                    ownerId, requirement.ProductId);
                var shipped = CalculateRequiredShipment(missing, supplier);
                if (shipped > supplier.InventoryQuantityMilliunits)
                    throw new InvalidOperationException(
                        "Supplier stock is insufficient after transport loss.");
                var market = runtime.Markets.Find(item =>
                    item.ProductId == requirement.ProductId);
                var unitPrice = Math.Max(1L, market?.BasePrice ?? 1);
                var purchaseCost = checked((shipped * unitPrice + 999) / 1_000);
                var orderId = "supply_order.construction." + runtime.AbsoluteDay +
                              "." + runtime.SupplyOrders.Count.ToString("D6");
                var shipmentId = "shipment.construction." + runtime.AbsoluteDay +
                                 "." + runtime.Shipments.Count.ToString("D6");
                DebitMaterialBuyer(runtime, ownerId, purchaseCost);
                supplier.CashBalance = checked(supplier.CashBalance + purchaseCost);
                supplier.CumulativeSalesRevenue = checked(
                    supplier.CumulativeSalesRevenue + purchaseCost);

                var carrierConsumption = Math.Min(shipped,
                    checked((long)Math.Max(1, supplier.TravelDays) * 2_000L));
                var remaining = shipped - carrierConsumption;
                var naturalLoss = remaining *
                    supplier.NaturalLossBasisPoints / 10_000;
                var riskLoss = (remaining - naturalLoss) *
                    supplier.RiskLossBasisPoints / 10_000;
                var delivered = shipped - carrierConsumption - naturalLoss - riskLoss;
                if (delivered < missing)
                    throw new InvalidOperationException(
                        "Construction shipment does not cover its real losses.");
                if (LuoyangFormalEconomySystem.IsFood(requirement.ProductId))
                    new LuoyangFormalEconomySystem().DispatchFreight(runtime,
                        supplier.InventoryId, shipmentId,
                        requirement.ProductId, shipped, shipped - delivered,
                        supplier.ManagerPersonId);
                else
                    supplier.InventoryQuantityMilliunits -= shipped;
                supplier.CumulativeDispatchedMilliunits = checked(
                    supplier.CumulativeDispatchedMilliunits + shipped);
                var arrivalDay = checked(runtime.AbsoluteDay +
                    Math.Max(1, supplier.TravelDays));
                runtime.SupplyOrders.Add(new LuoyangSupplyOrderRuntimeState
                {
                    Id = orderId,
                    RequestedDay = runtime.AbsoluteDay,
                    ProductId = requirement.ProductId,
                    SupplierId = supplier.SupplierId,
                    DestinationInventoryId = destination.Id,
                    RequestedQuantityMilliunits = missing,
                    DispatchedQuantityMilliunits = shipped,
                    UnitPrice = unitPrice,
                    PurchaseCost = purchaseCost,
                    Status = LuoyangSupplyOrderStatus.InTransit,
                    ShipmentId = shipmentId,
                    RequestedByAgentId = requestedByAgentId ?? string.Empty,
                    ReasonId = "blueprint.material_procurement:" + blueprintId
                });
                runtime.Shipments.Add(new LuoyangShipmentRuntimeState
                {
                    Id = shipmentId,
                    OrderId = orderId,
                    ProductId = requirement.ProductId,
                    SupplierId = supplier.SupplierId,
                    SourceInventoryId = supplier.InventoryId,
                    DestinationInventoryId = destination.Id,
                    RouteId = supplier.RouteId,
                    CarrierPersonId = supplier.ManagerPersonId,
                    DispatchDay = runtime.AbsoluteDay,
                    ArrivalDay = arrivalDay,
                    ShippedQuantityMilliunits = shipped,
                    CarrierConsumptionMilliunits = carrierConsumption,
                    NaturalLossMilliunits = naturalLoss,
                    RiskLossMilliunits = riskLoss,
                    DeliveredQuantityMilliunits = delivered,
                    RemainingCargoQuantityMilliunits = delivered,
                    PurchaseCost = purchaseCost
                });
                latestArrival = Math.Max(latestArrival, arrivalDay);
            }
            return latestArrival;
        }

        private static long CalculateRequiredShipment(long required,
            LuoyangExternalSupplierRuntimeState supplier)
        {
            var shipped = checked(required +
                (long)Math.Max(1, supplier.TravelDays) * 2_000L);
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var carrier = Math.Min(shipped,
                    checked((long)Math.Max(1, supplier.TravelDays) * 2_000L));
                var remaining = shipped - carrier;
                var natural = remaining * supplier.NaturalLossBasisPoints / 10_000;
                var risk = (remaining - natural) *
                    supplier.RiskLossBasisPoints / 10_000;
                var delivered = shipped - carrier - natural - risk;
                if (delivered >= required) return shipped;
                shipped = checked(shipped + required - delivered + 1);
            }
            throw new InvalidOperationException(
                "Unable to price a loss-covered construction shipment.");
        }

        private static LuoyangInventoryBalanceState
            GetOrCreateOwnerMaterialInventory(
                Luoyang184LivingWorldRuntimeState runtime, string ownerId,
                string productId)
        {
            var existing = runtime.Inventories.Where(item =>
                    item.OwnerId == ownerId && item.ProductId == productId)
                .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
            if (existing != null) return existing;
            var inventory = new LuoyangInventoryBalanceState
            {
                Id = "inventory.construction." + ownerId + "." + productId,
                OwnerKind = runtime.GovernmentEconomy.OrganizationId == ownerId
                    ? LuoyangInventoryOwnerKind.Government
                    : LuoyangInventoryOwnerKind.Household,
                OwnerId = ownerId,
                FacilityId = string.Empty,
                ProductId = productId,
                CapacityMilliunits = 1_000_000
            };
            runtime.Inventories.Add(inventory);
            return inventory;
        }

        private static void DebitMaterialBuyer(
            Luoyang184LivingWorldRuntimeState runtime, string ownerId,
            long amount)
        {
            if (ownerId == runtime.GovernmentEconomy.OrganizationId)
            {
                if (runtime.GovernmentEconomy.Treasury < amount)
                    throw new InvalidOperationException(
                        "Government cannot afford construction materials.");
                runtime.GovernmentEconomy.Treasury -= amount;
                return;
            }
            var family = runtime.FamilyOrganizations.Find(item => item.Id == ownerId);
            if (family != null)
            {
                if (family.Funds < amount)
                    throw new InvalidOperationException(
                        "Family cannot afford construction materials.");
                family.Funds -= amount;
                return;
            }
            var household = runtime.Households.Find(item =>
                item.HouseholdId == ownerId) ?? throw new InvalidOperationException(
                "Construction material buyer has no money ledger.");
            if (household.Wealth < amount)
                throw new InvalidOperationException(
                    "Household cannot afford construction materials.");
            household.Wealth -= amount;
        }

        private static int PersonPriority(LuoyangWorkforceAssignmentState person)
        {
            if (person.Status == LuoyangWorkforceStatus.Official) return 100;
            if (person.Status == LuoyangWorkforceStatus.MilitaryDuty) return 80;
            if (person.Status == LuoyangWorkforceStatus.Assigned) return 60;
            return 20;
        }

        private List<LuoyangFacilityProductionRuntimeState>
            SelectRepresentativeFacilities(Luoyang184LivingWorldRuntimeState runtime,
                int budget)
        {
            var result = new List<LuoyangFacilityProductionRuntimeState>();
            var wanted = new[] { "gate", "market", "warehouse", "residential",
                "workshop", "industry", "granary", "government", "school",
                "barracks", "agriculture", "palace", "wall" };
            foreach (var token in wanted)
            {
                var item = runtime.Facilities.Where(facility =>
                        facility.DefinitionId.IndexOf(token,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(facility => facility.FacilityId,
                        StringComparer.Ordinal).FirstOrDefault();
                if (item != null && !result.Contains(item)) result.Add(item);
            }
            result.AddRange(runtime.Facilities.Where(item => !result.Contains(item))
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .Take(Math.Max(0, budget - result.Count)));
            return result;
        }

        private static string Classify(string id)
        {
            id = id ?? string.Empty;
            if (id.StartsWith("facility.fortification.", StringComparison.Ordinal) ||
                id.StartsWith("facility.military.", StringComparison.Ordinal))
            {
                if (id.EndsWith("gate", StringComparison.Ordinal))
                    return "visual.class.gate";
                if (id.EndsWith("wall", StringComparison.Ordinal))
                    return "visual.class.wall";
            }
            if (id.Contains("palace")) return "visual.class.palace";
            if (id.StartsWith("facility.industry.", StringComparison.Ordinal) ||
                id == "facility.industry.workshop")
                return "visual.class.production";
            if (id.StartsWith("facility.commercial.market", StringComparison.Ordinal) ||
                id.StartsWith("facility.commercial.shop", StringComparison.Ordinal) ||
                id == "facility.historical.market")
                return "visual.class.market";
            if (id.Contains("warehouse") || id.Contains("granary") ||
                id.EndsWith("taicang", StringComparison.Ordinal))
                return "visual.class.storage";
            if (id.StartsWith("facility.residential.", StringComparison.Ordinal) ||
                id == "facility.historical.urban_ward")
                return "visual.class.residence";
            if (id.StartsWith("facility.agriculture.", StringComparison.Ordinal) ||
                id.EndsWith("garden", StringComparison.Ordinal))
                return "visual.class.agriculture";
            if (id.StartsWith("facility.government.", StringComparison.Ordinal) ||
                id.Contains("office")) return "visual.class.government";
            if (id.StartsWith("facility.education.", StringComparison.Ordinal) ||
                id.EndsWith("school", StringComparison.Ordinal) ||
                id.Contains("taixue")) return "visual.class.education";
            if (id.StartsWith("facility.military.", StringComparison.Ordinal) ||
                id.Contains("barracks")) return "visual.class.military";
            return "visual.class.public";
        }

        private void RegisterModelProfiles(HanBuildableFacilityModelCatalog models)
        {
            var byVisualId = _profiles.ToDictionary(item => item.VisualProfileId,
                StringComparer.Ordinal);
            foreach (var model in models.Models)
            {
                if (!byVisualId.TryGetValue(model.VisualProfileId, out var profile))
                {
                    var template = _profiles.First(item => item.FacilityTypeId ==
                        Classify(model.FacilityDefinitionId));
                    profile = CloneForModel(template, model);
                    _profiles.Add(profile);
                    byVisualId.Add(profile.VisualProfileId, profile);
                }
                else if (!string.Equals(profile.MainAssetId, model.AssetId,
                             StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Visual profile asset conflicts with the Luoyang model catalog: " +
                        model.VisualProfileId);
                _modelProfiles.Add(model.ModelId, profile);
            }
        }

        private static FacilityVisualProfile CloneForModel(
            FacilityVisualProfile template,
            HanBuildableFacilityModelDefinition model) =>
            new FacilityVisualProfile
            {
                VisualProfileId = model.VisualProfileId,
                FacilityTypeId = model.FacilityDefinitionId,
                RegionalStyleId = template.RegionalStyleId,
                ScaleProfileId = template.ScaleProfileId,
                MainAssetId = model.AssetId,
                ModularKitId = model.ModularKitId,
                DecorationSetId = template.DecorationSetId,
                WallSetId = template.WallSetId,
                RoofSetId = template.RoofSetId,
                GateSetId = template.GateSetId,
                PropSetId = template.PropSetId,
                VegetationSetId = template.VegetationSetId,
                DamageVisualId = template.DamageVisualId,
                RuinVisualId = template.RuinVisualId,
                LodProfileId = "lod." + model.ModelId,
                CrowdAnchorCount = template.CrowdAnchorCount,
                WorkerAnchorCount = template.WorkerAnchorCount,
                VehicleAnchorCount = template.VehicleAnchorCount,
                ProductionEffectAnchorCount = template.ProductionEffectAnchorCount,
                Importance = template.Importance,
                ReusableConstructionAsset =
                    model.AvailabilityIds.Contains("Player") ||
                    model.AvailabilityIds.Contains("Ai"),
                Availability = ParseAvailability(model.AvailabilityIds)
            };

        private static BuildAvailability ParseAvailability(
            IEnumerable<string> availabilityIds)
        {
            var result = BuildAvailability.None;
            foreach (var id in availabilityIds)
                if (Enum.TryParse(id, true, out BuildAvailability value))
                    result |= value;
                else
                    throw new InvalidOperationException(
                        "Unknown build availability id: " + id);
            return result;
        }

        private static List<FacilityVisualProfile> CreateProfiles()
        {
            var classes = new[] { "gate", "wall", "palace", "market", "storage",
                "residence", "agriculture", "government", "education", "military",
                "production", "public" };
            var result = new List<FacilityVisualProfile>();
            foreach (var item in classes)
                result.Add(new FacilityVisualProfile
                {
                    VisualProfileId = "visual_profile.han.central_plains." + item + ".v1",
                    FacilityTypeId = "visual.class." + item,
                    RegionalStyleId = RegionalStyleId,
                    ScaleProfileId = "scale.facility.cluster.single_cell",
                    MainAssetId = MainAssetForVisualClass(item),
                    ModularKitId = "HAN_BUILDING_MODULAR_KIT_V1",
                    DecorationSetId = "HAN_PROPS_COMMON_V1",
                    WallSetId = "HAN_WALL_KIT_V1",
                    RoofSetId = "HAN_ROOF_GREY_TILE_V1",
                    GateSetId = "HAN_GATE_KIT_V1",
                    PropSetId = "HAN_PROPS_COMMON_V1",
                    VegetationSetId = "HAN_CENTRAL_PLAINS_VEGETATION_V1",
                    DamageVisualId = "HAN_DAMAGE_V1",
                    RuinVisualId = "HAN_RUIN_V1",
                    LodProfileId = "lod.facility." + item,
                    CrowdAnchorCount = item == "market" ? 16 : 2,
                    WorkerAnchorCount = item == "production" ? 8 : 2,
                    VehicleAnchorCount = item == "storage" || item == "market" ? 4 : 1,
                    ProductionEffectAnchorCount = item == "production" ? 3 : 0,
                    Importance = item == "gate" || item == "palace" ||
                                 item == "government"
                        ? FacilityVisualImportance.A
                        : item == "market" || item == "storage" ||
                          item == "military" || item == "production"
                            ? FacilityVisualImportance.B
                            : FacilityVisualImportance.C,
                    ReusableConstructionAsset = item != "palace",
                    Availability = item == "palace"
                        ? BuildAvailability.HistoricalInit | BuildAvailability.Event
                        : BuildAvailability.Player | BuildAvailability.Ai |
                          BuildAvailability.Family | BuildAvailability.Government |
                          BuildAvailability.HistoricalInit
                });
            result.Add(new FacilityVisualProfile
            {
                VisualProfileId =
                    "visual_profile.han.central_plains.field_hospital.v1",
                FacilityTypeId = "military_rear_medical_site.field_hospital",
                RegionalStyleId = RegionalStyleId,
                ScaleProfileId = "scale.facility.cluster.single_cell",
                MainAssetId = HanBuildableFacilityModelIds.FieldHospitalAsset,
                ModularKitId = HanBuildableFacilityModelCatalogRules.ModularKitId,
                DecorationSetId = "HAN_PROPS_MEDICAL_V1",
                WallSetId = "HAN_WALL_KIT_V1",
                RoofSetId = "HAN_ROOF_GREY_TILE_V1",
                GateSetId = "HAN_GATE_KIT_V1",
                PropSetId = "HAN_PROPS_MEDICAL_V1",
                VegetationSetId = "HAN_CENTRAL_PLAINS_VEGETATION_V1",
                DamageVisualId = "HAN_DAMAGE_V1",
                RuinVisualId = "HAN_RUIN_V1",
                LodProfileId = "lod.facility.field_hospital",
                CrowdAnchorCount = 4,
                WorkerAnchorCount = 6,
                VehicleAnchorCount = 2,
                ProductionEffectAnchorCount = 0,
                Importance = FacilityVisualImportance.B,
                ReusableConstructionAsset = true,
                Availability = BuildAvailability.Player |
                               BuildAvailability.Military |
                               BuildAvailability.HistoricalInit |
                               BuildAvailability.Event
            });
            return result;
        }

        private static string MainAssetForVisualClass(string visualClass)
        {
            switch (visualClass)
            {
                case "gate": return HanBuildableFacilityModelIds.CityGateAsset;
                case "wall": return HanBuildableFacilityModelIds.CityWallAsset;
                case "market": return HanBuildableFacilityModelIds.MarketAsset;
                case "storage": return HanBuildableFacilityModelIds.WarehouseAsset;
                case "residence": return HanBuildableFacilityModelIds.ResidenceAsset;
                case "production": return HanBuildableFacilityModelIds.WorkshopAsset;
                default: return "HAN_" + visualClass.ToUpperInvariant() + "_A";
            }
        }

        private static List<BuildBlueprintDefinition> CreateBlueprints()
        {
            var availability = BuildAvailability.Player | BuildAvailability.Ai |
                BuildAvailability.Family | BuildAvailability.Government |
                BuildAvailability.HistoricalInit;
            return new List<BuildBlueprintDefinition>
            {
                Blueprint("blueprint.han.residence.general.v1",
                    "facility.residential.urban_quarter", "residence", 20, 20,
                    availability),
                Blueprint("blueprint.han.warehouse.general.v1",
                    "facility.storage.warehouse", "storage", 30, 30,
                    availability),
                Blueprint("blueprint.han.workshop.general.v1",
                    "facility.industry.workshop", "production", 30, 30,
                    availability),
                Blueprint("blueprint.han.market.general.v1",
                    "facility.commercial.market", "market", 35, 35,
                    availability),
                Blueprint("blueprint.han.palace.historical.nangong.v1",
                    "facility.historical.palace_complex", "palace", 365,
                    100_000, BuildAvailability.HistoricalInit |
                    BuildAvailability.Event, "historical_init_only")
            };
        }

        private static BuildBlueprintDefinition Blueprint(string id,
            string definition, string visualClass, int days, long money,
            BuildAvailability availability, string restriction = "none") =>
            new BuildBlueprintDefinition
            {
                BlueprintId = id,
                FacilityDefinitionId = definition,
                VisualProfileId = "visual_profile.han.central_plains." +
                                  visualClass + ".v1",
                AllowedTerrain = { "plain", "urban", "developable" },
                AllowedCellConditionId = "empty_developable_cell",
                AuthorityRequirementId = "authority.by_facility_type",
                OwnershipRequirementId = "owner_and_building_right_holder",
                RequiredMoney = money,
                RequiredWorkers = 4,
                ConstructionDays = days,
                ConstructionStages = { ConstructionVisualStage.Ghost,
                    ConstructionVisualStage.SitePreparation,
                    ConstructionVisualStage.Foundation,
                    ConstructionVisualStage.Frame,
                    ConstructionVisualStage.Structure,
                    ConstructionVisualStage.Finishing,
                    ConstructionVisualStage.Complete },
                Availability = availability,
                RegionalStyleId = RegionalStyleId,
                HistoricalRestrictionId = restriction,
                RequiredMaterials = { new BuildMaterialRequirement
                    { ProductId = CoreProductionContent.TimberMaterialProductId,
                        QuantityMilliunits = 10_000 },
                    new BuildMaterialRequirement
                    { ProductId = "product.reference.building_material",
                        QuantityMilliunits = 10_000 } }
            };
    }
}
