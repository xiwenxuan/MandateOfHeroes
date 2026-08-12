using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class WorldDecisionScore
    {
        public string ActionId;
        public int ScoreBasisPoints;
        public string Explanation;
        public List<WorldDecisionScoreComponent> Components =
            new List<WorldDecisionScoreComponent>();
    }

    public sealed class WorldDecisionScoreComponent
    {
        public string ComponentId;
        public int RawBasisPoints;
        public int WeightBasisPoints;
        public int ContributionBasisPoints;
        public string Evidence;
    }

    public sealed class WorldDecisionResult
    {
        public string PolicyId;
        public string PolicyVersion;
        public string ModelVersion;
        public WorldActionIntent SelectedAction;
        public List<WorldDecisionScore> Scores = new List<WorldDecisionScore>();
    }

    public interface IDecisionPolicy
    {
        string PolicyId { get; }
        string PolicyVersion { get; }
        string ModelVersion { get; }

        WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates);
    }

    public sealed class WorldSeedService
    {
        public const string DecisionSystemId = "mandate.living_world.decision";

        public NamedRandom CreateRandom(WorldState world)
        {
            if (world == null || world.MasterSeed == 0)
            {
                throw new InvalidOperationException(
                    "Living-world decisions require a non-zero World Seed.");
            }
            return new NamedRandom(world.MasterSeed);
        }

        public int DecisionJitter(
            WorldState world,
            string agentId,
            long decisionSequence,
            long absoluteDay,
            string actionId,
            int magnitude)
        {
            if (decisionSequence < 0 || magnitude < 0)
            {
                throw new ArgumentOutOfRangeException(
                    decisionSequence < 0 ? nameof(decisionSequence) : nameof(magnitude));
            }
            if (magnitude == 0)
            {
                return 0;
            }

            return CreateRandom(world).Range(
                DecisionSystemId,
                new StableId(agentId),
                absoluteDay,
                actionId + ".sequence." + decisionSequence,
                -magnitude,
                magnitude + 1);
        }
    }

    public sealed class LivingWorldSignalCalculator
    {
        public WorldDecisionContext BuildContext(
            WorldState world,
            string agentId,
            WorldAgentKind agentKind,
            string locationId,
            long decisionSequence)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            var location = world.Locations.Find(item => item.Id == locationId) ??
                throw new InvalidOperationException(
                    $"Missing signal location {locationId}.");

            var residentCount = 0;
            var availableLabor = 0L;
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (!person.IsAlive || person.LocationId != locationId)
                {
                    continue;
                }
                residentCount++;
                availableLabor += Math.Max(
                    0,
                    person.LaborCapacityBasisPoints -
                    person.PermanentLaborCapacityPenaltyBasisPoints);
            }
            var population = Math.Max(location.Population, residentCount);
            var housing = 0L;
            var jobs = 0L;
            var storageCapacity = 0L;
            var usedStorage = 0L;
            for (var i = 0; i < world.Facilities.Count; i++)
            {
                var facility = world.Facilities[i];
                if (facility.SettlementId != locationId ||
                    facility.LifecycleStatus != FacilityLifecycleStatus.Operational)
                {
                    continue;
                }
                var definition = world.FacilityDefinitions.Find(item =>
                    item.Id == facility.DefinitionId);
                if (definition != null)
                {
                    housing += definition.ResidentialCapacityPersons;
                    jobs += definition.WorkerCapacity;
                }
                storageCapacity += Math.Max(0, facility.StorageCapacity);
            }
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                var container = world.InventoryContainers[i];
                if (container.LocationId != locationId)
                {
                    continue;
                }
                storageCapacity += Math.Max(0, container.CapacityWeight);
                for (var batchIndex = 0;
                    batchIndex < world.ProductBatches.Count;
                    batchIndex++)
                {
                    var batch = world.ProductBatches[batchIndex];
                    if (batch.InventoryContainerId == container.Id)
                    {
                        usedStorage = checked(
                            usedStorage + batch.Quantity * batch.UnitWeight);
                    }
                }
            }

            var foodUnits = 0L;
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].LocationId == locationId)
                {
                    foodUnits = checked(foodUnits + world.Families[i].Grain);
                }
            }
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                var container = world.InventoryContainers.Find(item =>
                    item.Id == batch.InventoryContainerId);
                if (container != null && container.LocationId == locationId)
                {
                    foodUnits = checked(foodUnits + batch.Quantity);
                }
            }

            var averageRouteRisk = 0;
            var routeCount = 0;
            var transportCapacity = 0L;
            for (var i = 0; i < world.Routes.Count; i++)
            {
                var route = world.Routes[i];
                if (route.FromLocationId != locationId &&
                    route.ToLocationId != locationId)
                {
                    continue;
                }
                routeCount++;
                averageRouteRisk += 10_000 - route.SecurityBasisPoints;
            }
            for (var i = 0; i < world.CivilianCarrierRegistrations.Count; i++)
            {
                var registration = world.CivilianCarrierRegistrations[i];
                if (!registration.Active)
                {
                    continue;
                }
                var container = world.InventoryContainers.Find(item =>
                    item.Id == registration.TransportInventoryContainerId);
                if (container != null && container.LocationId == locationId)
                {
                    transportCapacity += container.CapacityWeight;
                }
            }

            var foodNeed = Math.Max(1L, population * 30L);
            var context = new WorldDecisionContext
            {
                AgentId = agentId,
                AgentKind = agentKind,
                LocationId = locationId,
                AbsoluteDay = world.AbsoluteDay,
                DecisionSequence = decisionSequence
            };
            Add(context, WorldSignalIds.PopulationPressure,
                ScalePressure(population, Math.Max(1L, housing)),
                $"population={population};housing={housing}");
            Add(context, WorldSignalIds.FoodPressure,
                ScaleShortage(foodUnits, foodNeed),
                $"food={foodUnits};need={foodNeed}");
            Add(context, WorldSignalIds.HousingPressure,
                ScalePressure(population, Math.Max(1L, housing)),
                $"population={population};capacity={housing}");
            Add(context, WorldSignalIds.EmploymentPressure,
                ScalePressure(availableLabor / 10_000L, Math.Max(1L, jobs)),
                $"labor={availableLabor};jobs={jobs}");
            Add(context, WorldSignalIds.InventoryPressure,
                ScaleShortage(foodUnits, foodNeed), "real inventory and household grain");
            Add(context, WorldSignalIds.PricePressure,
                Clamp(location.GrainPrice * 50), "location grain price");
            Add(context, WorldSignalIds.LaborAvailability,
                Clamp((int)Math.Min(10_000L, availableLabor / Math.Max(1, population))),
                "living resident labor capacity");
            Add(context, WorldSignalIds.LandAvailability,
                location.Features.HasFlag(LocationFeature.Farmland) ? 7_000 : 2_000,
                "authored location feature; Cell validation remains authoritative");
            Add(context, WorldSignalIds.WaterAvailability,
                location.Terrain == TerrainKind.Riverland ? 8_000 : 4_000,
                "terrain-derived opportunity");
            Add(context, WorldSignalIds.TransportCapacity,
                Clamp((int)Math.Min(10_000L, transportCapacity / 10L)),
                "registered real carrier containers");
            Add(context, WorldSignalIds.RouteRisk,
                routeCount == 0 ? 10_000 : averageRouteRisk / routeCount,
                $"adjacent_routes={routeCount}");
            Add(context, WorldSignalIds.SecurityRisk,
                10_000 - location.PublicOrderBasisPoints, "public order inverse");
            Add(context, WorldSignalIds.WarPressure,
                AverageWarPressure(world, locationId), "resident war pressure");
            Add(context, WorldSignalIds.GovernmentPressure,
                10_000 - location.PublicOrderBasisPoints, "public order inverse");
            Add(context, WorldSignalIds.ProfitOpportunity,
                Clamp(location.GrainPrice * 40), "price-based opportunity; not inventory");
            Add(context, WorldSignalIds.MigrationPressure,
                Math.Max(
                    Get(context, WorldSignalIds.FoodPressure),
                    Get(context, WorldSignalIds.SecurityRisk)),
                "food/security maximum");
            Add(context, WorldSignalIds.StoragePressure,
                storageCapacity <= 0
                    ? usedStorage > 0 ? 10_000 : 0
                    : Clamp((int)Math.Min(10_000L,
                        usedStorage * 10_000L / storageCapacity)),
                $"used_weight={usedStorage};capacity={storageCapacity}");
            Add(context, WorldSignalIds.ResourceOpportunity,
                location.Features.HasFlag(LocationFeature.Farmland) ||
                location.Features.HasFlag(LocationFeature.Workshop)
                    ? 6_000
                    : 2_000,
                "known authored feature; resource bodies remain authoritative");
            return context;
        }

        private static void Add(
            WorldDecisionContext context,
            string id,
            int value,
            string evidence)
        {
            context.Signals.Add(new WorldSignalValue
            {
                SignalId = id,
                ValueBasisPoints = Clamp(value),
                EvidenceSummary = evidence
            });
        }

        private static int Get(WorldDecisionContext context, string id) =>
            context.Signals.Find(item => item.SignalId == id)
                ?.ValueBasisPoints ?? 0;

        private static int ScalePressure(long demand, long capacity)
        {
            if (demand <= capacity)
            {
                return 0;
            }
            return Clamp((int)Math.Min(
                10_000L,
                (demand - capacity) * 10_000L / Math.Max(1L, demand)));
        }

        private static int ScaleShortage(long available, long required)
        {
            if (available >= required)
            {
                return 0;
            }
            return Clamp((int)Math.Min(
                10_000L,
                (required - available) * 10_000L / Math.Max(1L, required)));
        }

        private static int AverageWarPressure(WorldState world, string locationId)
        {
            var total = 0L;
            var count = 0;
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (person.IsAlive && person.LocationId == locationId)
                {
                    total += person.Needs?.WarPressure ?? 0;
                    count++;
                }
            }
            return count == 0 ? 0 : (int)(total / count);
        }

        private static int Clamp(int value) =>
            Math.Max(0, Math.Min(10_000, value));
    }

    public sealed class WorldActionValidator
    {
        public WorldActionValidationResult Validate(
            WorldState world,
            WorldActionIntent action)
        {
            if (world == null || action == null)
            {
                throw new ArgumentNullException(
                    world == null ? nameof(world) : nameof(action));
            }
            if (string.IsNullOrWhiteSpace(action.Id) ||
                string.IsNullOrWhiteSpace(action.ActionTypeId) ||
                string.IsNullOrWhiteSpace(action.AgentId))
            {
                return Invalid("invalid_identity", "Action identity is incomplete.");
            }

            if (!AgentExists(world, action))
            {
                return Invalid("agent_missing", "The proposed actor does not exist.");
            }
            if (!string.IsNullOrEmpty(action.LocationId) &&
                !world.Locations.Exists(item => item.Id == action.LocationId))
            {
                return Invalid("location_missing", "The action location does not exist.");
            }

            switch (action.ActionTypeId)
            {
                case WorldActionTypeIds.NoAction:
                case WorldActionTypeIds.Observe:
                    return Valid("observation_is_non_mutating");
                case WorldActionTypeIds.MovePerson:
                    return ValidateMove(world, action);
                case WorldActionTypeIds.CreateTradeOrder:
                    return ValidateInventoryBackedOrder(world, action);
                case WorldActionTypeIds.CreateMarketBuyOrder:
                    return ValidateMarketBuyOrder(world, action);
                case WorldActionTypeIds.CreateTransferOrder:
                case WorldActionTypeIds.CreateMilitarySupplyOrder:
                case WorldActionTypeIds.CreateShipment:
                    return ValidateInventoryBackedOrder(world, action);
                case WorldActionTypeIds.CreateGovernmentPurchase:
                    return ValidateGovernmentPurchase(world, action);
                case WorldActionTypeIds.Invest:
                case WorldActionTypeIds.EstablishFamilyCenter:
                    return ValidateFamilyOrganizationAction(world, action);
                case WorldActionTypeIds.MigrateHousehold:
                    return ValidateHouseholdMigration(world, action);
                case WorldActionTypeIds.BuildFacility:
                case WorldActionTypeIds.ReclaimLand:
                case WorldActionTypeIds.AcquireLand:
                    return ValidateConstructionIntent(world, action);
                default:
                    return new WorldActionValidationResult
                    {
                        Status = WorldActionValidationStatus.Deferred,
                        ReasonId = "domain_adapter_required",
                        Explanation =
                            "The action is recognized but requires its domain command adapter."
                    };
            }
        }

        private static WorldActionValidationResult ValidateMarketBuyOrder(
            WorldState world,
            WorldActionIntent action)
        {
            if (action.AgentKind != WorldAgentKind.Household)
            {
                return Invalid(
                    "buyer_must_be_household",
                    "A formal family buy order must be owned by a Household.");
            }
            var family = world.Families.Find(item => item.Id == action.AgentId);
            if (family == null)
            {
                return Invalid("buyer_missing", "The buyer family does not exist.");
            }
            if (!long.TryParse(Argument(action, "quantity"), out var quantity) ||
                !long.TryParse(Argument(action, "maximum_unit_price"),
                    out var unitPrice) || quantity <= 0 || unitPrice <= 0)
            {
                return Invalid(
                    "buy_terms_invalid",
                    "A positive quantity and maximum unit price are required.");
            }
            if (family.Wealth < quantity * unitPrice)
            {
                return Invalid("buyer_funds_insufficient", "The buyer cannot fund escrow.");
            }
            if (!world.CountyGovernances.Exists(item =>
                    item.Id == Argument(action, "county_governance_id")))
            {
                return Invalid("county_market_missing", "The county market is missing.");
            }
            if (string.IsNullOrWhiteSpace(
                    Argument(action, "product_definition_id")) ||
                !world.Facilities.Exists(item =>
                    item.Id == Argument(action, "storage_facility_id")))
            {
                return Invalid(
                    "buy_storage_or_product_missing",
                    "The product and real receiving storage are required.");
            }
            return new WorldActionValidationResult
            {
                Status = WorldActionValidationStatus.Valid,
                ReasonId = "formal_buy_order_funded",
                Explanation = "The buy order has a family, storage and real escrow funds.",
                ExecutableQuantity = quantity
            };
        }

        private static WorldActionValidationResult ValidateGovernmentPurchase(
            WorldState world,
            WorldActionIntent action)
        {
            var governance = world.CountyGovernances.Find(item =>
                item.GovernmentOrganizationId == action.AgentId &&
                item.Id == Argument(action, "county_governance_id"));
            var organization = world.Organizations.Find(item =>
                item.Id == action.AgentId &&
                item.Type == OrganizationType.Government);
            if (governance == null || organization == null)
            {
                return Invalid(
                    "government_authority_missing",
                    "A real county government and organization are required.");
            }
            if (!long.TryParse(Argument(action, "quantity"), out var quantity) ||
                !long.TryParse(Argument(action, "maximum_unit_price"),
                    out var unitPrice) || quantity <= 0 || unitPrice <= 0)
            {
                return Invalid("government_terms_invalid", "Purchase terms are invalid.");
            }
            if (organization.Treasury < quantity * unitPrice)
            {
                return Invalid(
                    "government_budget_insufficient",
                    "The government treasury cannot fund the purchase.");
            }
            return new WorldActionValidationResult
            {
                Status = WorldActionValidationStatus.Deferred,
                ReasonId = "government_purchase_command_required",
                Explanation =
                    "Authority and budget exist; the persistent procurement command must execute it."
            };
        }

        private static WorldActionValidationResult ValidateFamilyOrganizationAction(
            WorldState world,
            WorldActionIntent action)
        {
            if (action.AgentKind != WorldAgentKind.Organization)
            {
                return Invalid(
                    "family_organization_authority_required",
                    "Personal or Household assets cannot be spent as organization assets.");
            }
            var profile = world.FamilyOrganizationProfiles.Find(item =>
                item.OrganizationId == action.AgentId);
            if (profile == null)
            {
                return Invalid(
                    "family_organization_missing",
                    "The FamilyOrganization profile does not exist.");
            }
            if (!long.TryParse(Argument(action, "asset_cost"), out var cost) ||
                cost <= 0 || profile.FamilyAssets < cost)
            {
                return Invalid(
                    "organization_assets_insufficient",
                    "The FamilyOrganization lacks dedicated assets for this action.");
            }
            return Valid("family_organization_assets_authorized");
        }

        private static WorldActionValidationResult ValidateHouseholdMigration(
            WorldState world,
            WorldActionIntent action)
        {
            if (action.AgentKind != WorldAgentKind.Household)
            {
                return Invalid("household_required", "Migration requires a Household.");
            }
            var family = world.Families.Find(item => item.Id == action.AgentId);
            var target = Argument(action, "target_location_id");
            if (family == null || !world.Locations.Exists(item => item.Id == target))
            {
                return Invalid("migration_target_missing", "Migration target is missing.");
            }
            if (family.LocationId == target)
            {
                return Invalid("already_at_target", "The Household is already there.");
            }
            if (!world.Routes.Exists(item =>
                    item.FromLocationId == family.LocationId &&
                    item.ToLocationId == target ||
                    item.Bidirectional && item.ToLocationId == family.LocationId &&
                    item.FromLocationId == target))
            {
                return Invalid("known_route_missing", "No real route connects the Household.");
            }
            return Valid("household_and_route_exist");
        }

        private static WorldActionValidationResult ValidateMove(
            WorldState world,
            WorldActionIntent action)
        {
            var target = Argument(action, "target_location_id");
            var person = world.People.Find(item => item.Id == action.AgentId);
            if (person == null || !person.IsAlive)
            {
                return Invalid("person_not_alive", "Only a living Person may move.");
            }
            if (!world.Locations.Exists(item => item.Id == target))
            {
                return Invalid("target_location_missing", "Move target is unknown.");
            }
            if (world.Journeys.Exists(item => item.PersonId == person.Id))
            {
                return new WorldActionValidationResult
                {
                    Status = WorldActionValidationStatus.Deferred,
                    ReasonId = "person_already_travelling",
                    Explanation = "The Person already has an active Journey."
                };
            }
            if (person.LocationId == target)
            {
                return Invalid("already_at_target", "The Person is already there.");
            }
            if (!world.Routes.Exists(item =>
                    item.FromLocationId == person.LocationId &&
                    item.ToLocationId == target ||
                    item.Bidirectional &&
                    item.ToLocationId == person.LocationId &&
                    item.FromLocationId == target))
            {
                return Invalid("known_route_missing", "No real route connects the locations.");
            }
            return Valid("real_person_and_route_exist");
        }

        private static WorldActionValidationResult ValidateInventoryBackedOrder(
            WorldState world,
            WorldActionIntent action)
        {
            var containerId = Argument(action, "source_container_id");
            var productId = Argument(action, "product_definition_id");
            if (!long.TryParse(Argument(action, "quantity"), out var quantity) ||
                quantity <= 0)
            {
                return Invalid("quantity_invalid", "A positive quantity is required.");
            }
            if (!world.InventoryContainers.Exists(item => item.Id == containerId))
            {
                return Invalid("source_container_missing", "No real source inventory exists.");
            }

            var available = 0L;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId == containerId &&
                    batch.ProductDefinitionId == productId)
                {
                    available = checked(
                        available + Math.Max(0, batch.Quantity - batch.ReservedQuantity));
                }
            }
            if (available <= 0)
            {
                return Invalid("inventory_unavailable", "No unreserved real cargo exists.");
            }
            if (available < quantity)
            {
                return new WorldActionValidationResult
                {
                    Status = WorldActionValidationStatus.PartiallyExecutable,
                    ReasonId = "inventory_partial",
                    Explanation = "Only part of the requested real inventory is available.",
                    ExecutableQuantity = available
                };
            }
            return new WorldActionValidationResult
            {
                Status = WorldActionValidationStatus.Valid,
                ReasonId = "inventory_backed",
                Explanation = "The order is backed by unreserved ProductBatch inventory.",
                ExecutableQuantity = quantity
            };
        }

        private static WorldActionValidationResult ValidateConstructionIntent(
            WorldState world,
            WorldActionIntent action)
        {
            var definitionId = Argument(action, "facility_definition_id");
            var ownerId = Argument(action, "owner_id");
            if (action.ActionTypeId == WorldActionTypeIds.BuildFacility &&
                !world.FacilityDefinitions.Exists(item => item.Id == definitionId))
            {
                return Invalid("facility_definition_missing", "Facility definition is missing.");
            }
            if (!string.IsNullOrEmpty(ownerId) &&
                !world.Organizations.Exists(item => item.Id == ownerId) &&
                !world.Families.Exists(item => item.Id == ownerId))
            {
                return Invalid("owner_missing", "The proposed owner does not exist.");
            }
            var cellText = Argument(action, "cell_id");
            if (string.IsNullOrEmpty(cellText))
            {
                return new WorldActionValidationResult
                {
                    Status = WorldActionValidationStatus.Deferred,
                    ReasonId = "cell_selection_required",
                    Explanation =
                        "Demand cannot create a Facility; a legal Cell must be selected and validated."
                };
            }
            if (!ulong.TryParse(cellText, out var cellId) || cellId == 0 ||
                world.Facilities.Exists(item => item.CellId64 == cellId))
            {
                return Invalid(
                    "construction_cell_invalid",
                    "The selected Cell is invalid or already occupied.");
            }
            var containerId = Argument(action, "material_container_id");
            var productId = Argument(action, "material_product_id");
            if (!long.TryParse(
                    Argument(action, "material_quantity"), out var materialQuantity) ||
                materialQuantity <= 0 ||
                !world.InventoryContainers.Exists(item => item.Id == containerId))
            {
                return new WorldActionValidationResult
                {
                    Status = WorldActionValidationStatus.Deferred,
                    ReasonId = "construction_resources_required",
                    Explanation =
                        "A legal construction intent requires a real material container and quantity."
                };
            }
            var available = 0L;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId == containerId &&
                    batch.ProductDefinitionId == productId)
                {
                    available = checked(
                        available + Math.Max(0, batch.Quantity - batch.ReservedQuantity));
                }
            }
            var workerId = Argument(action, "worker_person_id");
            if (available < materialQuantity ||
                !int.TryParse(Argument(action, "labor_minutes"), out var laborMinutes) ||
                laborMinutes <= 0 ||
                !world.People.Exists(item => item.Id == workerId && item.IsAlive &&
                    item.LocationId == action.LocationId))
            {
                return new WorldActionValidationResult
                {
                    Status = WorldActionValidationStatus.Deferred,
                    ReasonId = "construction_resources_or_labor_unavailable",
                    Explanation =
                        "The selected Cell lacks sufficient real material or living labor."
                };
            }
            return Valid("construction_intent_ready_for_domain_validation");
        }

        private static bool AgentExists(WorldState world, WorldActionIntent action)
        {
            switch (action.AgentKind)
            {
                case WorldAgentKind.Person:
                    return world.People.Exists(item =>
                        item.Id == action.AgentId && item.IsAlive);
                case WorldAgentKind.Household:
                    return world.Families.Exists(item => item.Id == action.AgentId);
                case WorldAgentKind.Organization:
                case WorldAgentKind.Government:
                    return world.Organizations.Exists(item => item.Id == action.AgentId);
                case WorldAgentKind.Force:
                    return world.Armies.Exists(item => item.Id == action.AgentId);
                case WorldAgentKind.Settlement:
                    return world.Locations.Exists(item => item.Id == action.AgentId);
                default:
                    return false;
            }
        }

        private static string Argument(WorldActionIntent action, string key) =>
            action.Arguments.Find(item => item.Key == key)?.Value ?? string.Empty;

        private static WorldActionValidationResult Valid(string reason) =>
            new WorldActionValidationResult
            {
                Status = WorldActionValidationStatus.Valid,
                ReasonId = reason,
                Explanation = reason
            };

        private static WorldActionValidationResult Invalid(
            string reason,
            string explanation) =>
            new WorldActionValidationResult
            {
                Status = WorldActionValidationStatus.Invalid,
                ReasonId = reason,
                Explanation = explanation
            };
    }

    public sealed class RuleDecisionPolicy : IDecisionPolicy
    {
        private readonly WorldActionValidator _validator;

        public RuleDecisionPolicy(WorldActionValidator validator = null)
        {
            _validator = validator ?? new WorldActionValidator();
        }

        public string PolicyId => DecisionPolicyIds.Rule;
        public string PolicyVersion => "1";
        public string ModelVersion => "none";

        public WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates)
        {
            var ordered = CopyAndSort(candidates);
            var result = NewResult(this);
            for (var i = 0; i < ordered.Count; i++)
            {
                var validation = _validator.Validate(world, ordered[i]);
                result.Scores.Add(new WorldDecisionScore
                {
                    ActionId = ordered[i].Id,
                    ScoreBasisPoints = validation.CanExecute ? 10_000 : 0,
                    Explanation = validation.ReasonId
                });
                if (result.SelectedAction == null && validation.CanExecute)
                {
                    result.SelectedAction = ordered[i];
                }
            }
            return result;
        }

        internal static List<WorldActionIntent> CopyAndSort(
            IReadOnlyList<WorldActionIntent> candidates)
        {
            var ordered = new List<WorldActionIntent>();
            if (candidates != null)
            {
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i] != null)
                    {
                        ordered.Add(candidates[i]);
                    }
                }
            }
            ordered.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return ordered;
        }

        internal static WorldDecisionResult NewResult(IDecisionPolicy policy) =>
            new WorldDecisionResult
            {
                PolicyId = policy.PolicyId,
                PolicyVersion = policy.PolicyVersion,
                ModelVersion = policy.ModelVersion
            };
    }

    public sealed class UtilityDecisionPolicy : IDecisionPolicy
    {
        private readonly WorldActionValidator _validator;

        public UtilityDecisionPolicy(WorldActionValidator validator = null)
        {
            _validator = validator ?? new WorldActionValidator();
        }

        public string PolicyId => DecisionPolicyIds.Utility;
        public string PolicyVersion => "1";
        public string ModelVersion => "none";

        public WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates)
        {
            return new UtilityDecisionEngine(_validator).Decide(
                world, context, candidates, this);
        }

        internal static int SignalAffinity(
            WorldDecisionContext context,
            string actionTypeId)
        {
            string signalId;
            switch (actionTypeId)
            {
                case WorldActionTypeIds.BuildFacility:
                    signalId = WorldSignalIds.HousingPressure;
                    break;
                case WorldActionTypeIds.CreateTradeOrder:
                case WorldActionTypeIds.CreateTransferOrder:
                case WorldActionTypeIds.CreateGovernmentPurchase:
                case WorldActionTypeIds.CreateMilitarySupplyOrder:
                    signalId = WorldSignalIds.FoodPressure;
                    break;
                case WorldActionTypeIds.MovePerson:
                case WorldActionTypeIds.MigrateHousehold:
                    signalId = WorldSignalIds.MigrationPressure;
                    break;
                default:
                    return 0;
            }
            return context.Signals.Find(item => item.SignalId == signalId)
                ?.ValueBasisPoints ?? 0;
        }
    }

    public interface INeuralActionScorer
    {
        string ModelVersion { get; }
        int Score(WorldDecisionContext context, WorldActionIntent action);
    }

    public sealed class NeuralDecisionPolicyAdapter : IDecisionPolicy
    {
        private readonly INeuralActionScorer _scorer;
        private readonly WorldActionValidator _validator;

        public NeuralDecisionPolicyAdapter(
            INeuralActionScorer scorer,
            WorldActionValidator validator = null)
        {
            _scorer = scorer;
            _validator = validator ?? new WorldActionValidator();
        }

        public string PolicyId => DecisionPolicyIds.NeuralAdapter;
        public string PolicyVersion => "1";
        public string ModelVersion => _scorer?.ModelVersion ?? "missing";

        public WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates)
        {
            if (_scorer == null)
            {
                return Fallback(world, context, candidates, "model_missing");
            }
            var result = RuleDecisionPolicy.NewResult(this);
            var ordered = RuleDecisionPolicy.CopyAndSort(candidates);
            var best = int.MinValue;
            for (var i = 0; i < ordered.Count; i++)
            {
                var action = ordered[i];
                int score;
                try
                {
                    score = _scorer.Score(context, action);
                }
                catch (Exception exception)
                {
                    return Fallback(
                        world,
                        context,
                        candidates,
                        "model_error:" + exception.GetType().Name);
                }
                var validation = _validator.Validate(world, action);
                result.Scores.Add(new WorldDecisionScore
                {
                    ActionId = action.Id,
                    ScoreBasisPoints = score,
                    Explanation = validation.ReasonId
                });
                if (validation.CanExecute && score > best)
                {
                    best = score;
                    result.SelectedAction = action;
                }
            }
            return result;
        }

        private WorldDecisionResult Fallback(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates,
            string reason)
        {
            var fallback = new UtilityDecisionPolicy(_validator).Decide(
                world, context, candidates);
            if (fallback.SelectedAction == null)
            {
                fallback = new RuleDecisionPolicy(_validator).Decide(
                    world, context, candidates);
            }
            if (fallback.SelectedAction == null)
            {
                fallback.SelectedAction = new WorldActionIntent
                {
                    Id = "action." + context.AgentId + "." +
                        context.DecisionSequence + ".safe_no_action",
                    ActionTypeId = WorldActionTypeIds.NoAction,
                    AgentId = context.AgentId,
                    AgentKind = context.AgentKind,
                    LocationId = context.LocationId
                };
                fallback.Scores.Add(new WorldDecisionScore
                {
                    ActionId = fallback.SelectedAction.Id,
                    ScoreBasisPoints = 0,
                    Explanation = "safe_no_action"
                });
            }
            fallback.PolicyId = PolicyId;
            fallback.PolicyVersion = PolicyVersion + ".fallback";
            fallback.ModelVersion = ModelVersion;
            for (var i = 0; i < fallback.Scores.Count; i++)
            {
                fallback.Scores[i].Explanation += ";" + reason +
                    ";fallback=utility_then_rule_then_safe_no_action";
            }
            return fallback;
        }
    }

    public sealed class RandomizedDecisionPolicy : IDecisionPolicy
    {
        private readonly IDecisionPolicy _inner;
        private readonly WorldSeedService _seed;
        private readonly WorldActionValidator _validator;
        private readonly int _jitterMagnitude;

        public RandomizedDecisionPolicy(
            IDecisionPolicy inner,
            int jitterMagnitude = 250,
            WorldSeedService seed = null,
            WorldActionValidator validator = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (jitterMagnitude < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(jitterMagnitude));
            }
            _jitterMagnitude = jitterMagnitude;
            _seed = seed ?? new WorldSeedService();
            _validator = validator ?? new WorldActionValidator();
        }

        public string PolicyId => DecisionPolicyIds.RandomizedWrapper;
        public string PolicyVersion => "1+" + _inner.PolicyVersion;
        public string ModelVersion => _inner.ModelVersion;

        public WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates)
        {
            var inner = _inner.Decide(world, context, candidates);
            var byId = new Dictionary<string, WorldActionIntent>(StringComparer.Ordinal);
            if (candidates != null)
            {
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i] != null)
                    {
                        byId[candidates[i].Id] = candidates[i];
                    }
                }
            }
            var result = new WorldDecisionResult
            {
                PolicyId = PolicyId,
                PolicyVersion = PolicyVersion,
                ModelVersion = ModelVersion
            };
            var best = int.MinValue;
            for (var i = 0; i < inner.Scores.Count; i++)
            {
                var source = inner.Scores[i];
                var score = checked(source.ScoreBasisPoints +
                    _seed.DecisionJitter(
                        world,
                        context.AgentId,
                        context.DecisionSequence,
                        context.AbsoluteDay,
                        source.ActionId,
                        _jitterMagnitude));
                result.Scores.Add(new WorldDecisionScore
                {
                    ActionId = source.ActionId,
                    ScoreBasisPoints = score,
                    Explanation = source.Explanation + ";stable_seed_jitter"
                });
                if (byId.TryGetValue(source.ActionId, out var action) &&
                    _validator.Validate(world, action).CanExecute &&
                    score > best)
                {
                    best = score;
                    result.SelectedAction = action;
                }
            }
            return result;
        }
    }

    public sealed class HistoricalConstraintDecisionPolicy : IDecisionPolicy
    {
        private readonly IDecisionPolicy _inner;
        private readonly Func<WorldState, WorldDecisionContext,
            WorldActionIntent, bool> _allows;

        public HistoricalConstraintDecisionPolicy(
            IDecisionPolicy inner,
            Func<WorldState, WorldDecisionContext,
                WorldActionIntent, bool> allows)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _allows = allows ?? throw new ArgumentNullException(nameof(allows));
        }

        public string PolicyId => DecisionPolicyIds.HistoricalConstraint;
        public string PolicyVersion => "1+" + _inner.PolicyVersion;
        public string ModelVersion => _inner.ModelVersion;

        public WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates)
        {
            var permitted = new List<WorldActionIntent>();
            var ordered = RuleDecisionPolicy.CopyAndSort(candidates);
            for (var i = 0; i < ordered.Count; i++)
            {
                if (_allows(world, context, ordered[i]))
                {
                    permitted.Add(ordered[i]);
                }
            }

            var inner = _inner.Decide(world, context, permitted);
            inner.PolicyId = PolicyId;
            inner.PolicyVersion = PolicyVersion;
            return inner;
        }
    }

    public sealed class LivingWorldDecisionService
    {
        public WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionAgentState agent,
            WorldDecisionContext context,
            IDecisionPolicy policy,
            IReadOnlyList<WorldActionIntent> candidates)
        {
            if (world == null || agent == null || context == null || policy == null)
            {
                throw new ArgumentNullException("Living-world decision input is null.");
            }
            if (context.DecisionSequence != agent.DecisionSequence ||
                context.AgentId != agent.AgentId ||
                context.AgentKind != agent.AgentKind)
            {
                throw new InvalidOperationException(
                    "Decision context does not match persisted agent state.");
            }

            var result = policy.Decide(world, context, candidates);
            agent.PolicyId = policy.PolicyId;
            agent.PolicyVersion = policy.PolicyVersion;
            agent.ModelVersion = policy.ModelVersion;
            agent.LastDecisionDay = world.AbsoluteDay;
            agent.LastActionId = result.SelectedAction?.Id ?? string.Empty;
            agent.DecisionSequence = checked(agent.DecisionSequence + 1);
            return result;
        }
    }

    public sealed class WorldSimulationLodScheduler
    {
        public int CadenceDays(WorldSimulationLodTier tier)
        {
            switch (tier)
            {
                case WorldSimulationLodTier.Hot:
                    return 1;
                case WorldSimulationLodTier.Warm:
                    return 7;
                case WorldSimulationLodTier.Cold:
                    return 30;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        public bool IsDue(WorldSimulationLodState state, long absoluteDay) =>
            state != null && absoluteDay >= state.NextEvaluationDay;

        public void MarkEvaluated(
            WorldSimulationLodState state,
            long absoluteDay)
        {
            if (state == null || absoluteDay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(absoluteDay));
            }
            state.LastEvaluatedDay = absoluteDay;
            state.NextEvaluationDay = checked(
                absoluteDay + CadenceDays(state.Tier));
        }

        public void ChangeTier(
            WorldSimulationLodState state,
            WorldSimulationLodTier tier,
            long absoluteDay)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            state.Tier = tier;
            state.NextEvaluationDay = absoluteDay;
        }
    }
}
