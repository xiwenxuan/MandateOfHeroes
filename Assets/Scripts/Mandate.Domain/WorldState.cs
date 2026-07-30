using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class PersonState
    {
        public string Id;
        public string DisplayName;
        public string LocationId;
        public long BirthDay;
        public bool IsAlive = true;
        public int HealthBasisPoints = 10_000;
        public long Wealth;
        public int Provisions = 10;
        public int CargoCapacity = 50;
        public int MedicalSkillBasisPoints;
        public PersonGender Gender = PersonGender.Unknown;
        public string FatherPersonId;
        public string MotherPersonId;
        public string SpousePersonId;
        public long LastChildbirthDay = -1;
        public PersonalityState Personality = new PersonalityState();
        public NeedState Needs = new NeedState();
    }

    [Serializable]
    public sealed class LocationState
    {
        public string Id;
        public string DisplayName;
        public int Population;
        public int PublicOrderBasisPoints = 5_000;
        public int GrainPrice = 100;
    }

    [Serializable]
    public sealed class FamilyState
    {
        public string Id;
        public string DisplayName;
        public string HeadPersonId;
        public long Wealth;
        public long Debt;
        public List<string> MemberIds = new List<string>();
    }

    [Serializable]
    public sealed class WorldState
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public ulong MasterSeed;
        public long AbsoluteDay;
        public byte Segment;
        public long Revision;
        public List<PersonState> People = new List<PersonState>();
        public List<LocationState> Locations = new List<LocationState>();
        public List<FamilyState> Families = new List<FamilyState>();
        public List<RouteState> Routes = new List<RouteState>();
        public List<JourneyState> Journeys = new List<JourneyState>();
        public List<RelationshipState> Relationships = new List<RelationshipState>();
        public List<OrganizationState> Organizations = new List<OrganizationState>();
        public List<PositionState> Positions = new List<PositionState>();
        public List<MembershipState> Memberships = new List<MembershipState>();
        public List<TaskDefinitionState> TaskDefinitions = new List<TaskDefinitionState>();
        public List<TaskInstanceState> Tasks = new List<TaskInstanceState>();
        public List<HistoricalEventDefinitionState> HistoricalEventDefinitions =
            new List<HistoricalEventDefinitionState>();
        public List<HistoricalAnchorRuntimeState> HistoricalAnchors =
            new List<HistoricalAnchorRuntimeState>();
        public List<LifeEventRecordState> LifeEvents = new List<LifeEventRecordState>();
        public List<CommodityState> Commodities = new List<CommodityState>();
        public List<MarketListingState> MarketListings = new List<MarketListingState>();
        public List<InventoryStackState> Inventories = new List<InventoryStackState>();
        public List<TradeRecordState> TradeRecords = new List<TradeRecordState>();
        public List<ArmyState> Armies = new List<ArmyState>();
        public List<ArmyMarchState> ArmyMarches = new List<ArmyMarchState>();
        public List<BattleRecordState> Battles = new List<BattleRecordState>();
        public List<MilitarySupplyRecordState> MilitarySupplies =
            new List<MilitarySupplyRecordState>();
        public List<MedicalTreatmentRecordState> MedicalTreatments =
            new List<MedicalTreatmentRecordState>();

        public WorldTime Time => new WorldTime(AbsoluteDay, (DaySegment)Segment);

        public static WorldState Create(ulong masterSeed)
        {
            return new WorldState
            {
                MasterSeed = masterSeed,
                AbsoluteDay = 0,
                Segment = (byte)DaySegment.Dawn,
                Revision = 0
            };
        }

        public void AdvanceOneDay()
        {
            Validate();
            AbsoluteDay = checked(AbsoluteDay + 1);
            Revision = checked(Revision + 1);
        }

        public bool AdvanceOneSegment()
        {
            Validate();
            var previousDay = AbsoluteDay;
            var next = Time.AdvanceSegments(1);
            AbsoluteDay = next.AbsoluteDay;
            Segment = (byte)next.Segment;
            Revision = checked(Revision + 1);
            return AbsoluteDay != previousDay;
        }

        public void Validate()
        {
            if (SchemaVersion != CurrentSchemaVersion)
            {
                throw new InvalidOperationException($"Unsupported world schema {SchemaVersion}.");
            }

            _ = new WorldTime(AbsoluteDay, (DaySegment)Segment);
            ValidateUniqueIds(People, person => person.Id, "person");
            ValidateUniqueIds(Locations, location => location.Id, "location");
            ValidateUniqueIds(Families, family => family.Id, "family");
            ValidateUniqueIds(Routes, route => route.Id, "route");
            ValidateUniqueIds(Journeys, journey => journey.Id, "journey");
            ValidateUniqueIds(Relationships, relationship => relationship.Id, "relationship");
            ValidateUniqueIds(Organizations, organization => organization.Id, "organization");
            ValidateUniqueIds(Positions, position => position.Id, "position");
            ValidateUniqueIds(Memberships, membership => membership.Id, "membership");
            ValidateUniqueIds(TaskDefinitions, task => task.Id, "task definition");
            ValidateUniqueIds(Tasks, task => task.Id, "task");
            ValidateUniqueIds(
                HistoricalEventDefinitions, item => item.Id, "historical event definition");
            ValidateUniqueIds(HistoricalAnchors, item => item.Id, "historical anchor");
            ValidateUniqueIds(LifeEvents, item => item.Id, "life event");
            ValidateUniqueIds(Commodities, item => item.Id, "commodity");
            ValidateUniqueIds(MarketListings, item => item.Id, "market listing");
            ValidateUniqueIds(Inventories, item => item.Id, "inventory stack");
            ValidateUniqueIds(TradeRecords, item => item.Id, "trade record");
            ValidateUniqueIds(Armies, item => item.Id, "army");
            ValidateUniqueIds(ArmyMarches, item => item.Id, "army march");
            ValidateUniqueIds(Battles, item => item.Id, "battle");
            ValidateUniqueIds(MilitarySupplies, item => item.Id, "military supply");
            ValidateUniqueIds(
                MedicalTreatments, item => item.Id, "medical treatment");

            var personIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < People.Count; i++)
            {
                var person = People[i] ?? throw new InvalidOperationException("A person cannot be null.");
                _ = new StableId(person.Id);
                personIds.Add(person.Id);
                if (person.HealthBasisPoints < 0 || person.HealthBasisPoints > 10_000)
                {
                    throw new InvalidOperationException($"Invalid health for {person.Id}.");
                }

                if (person.CargoCapacity < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid cargo capacity for {person.Id}.");
                }

                ValidateBasisPoints(person.Personality.Ambition, person.Id, "ambition");
                ValidateBasisPoints(person.Personality.FamilyDuty, person.Id, "family duty");
                ValidateBasisPoints(person.Personality.Sociability, person.Id, "sociability");
                ValidateBasisPoints(person.Personality.RiskTolerance, person.Id, "risk tolerance");
                ValidateBasisPoints(person.Personality.Benevolence, person.Id, "benevolence");
                ValidateBasisPoints(person.Needs.Livelihood, person.Id, "livelihood need");
                ValidateBasisPoints(person.Needs.Family, person.Id, "family need");
                ValidateBasisPoints(person.Needs.Status, person.Id, "status need");
                ValidateBasisPoints(person.Needs.Wealth, person.Id, "wealth need");
                ValidateBasisPoints(person.Needs.Relationships, person.Id, "relationship need");
                ValidateBasisPoints(person.Needs.WarPressure, person.Id, "war pressure");
                ValidateBasisPoints(
                    person.MedicalSkillBasisPoints, person.Id, "medical skill");
            }

            for (var i = 0; i < People.Count; i++)
            {
                var person = People[i];
                ValidateOptionalPersonReference(
                    personIds, person.FatherPersonId, person.Id, "father");
                ValidateOptionalPersonReference(
                    personIds, person.MotherPersonId, person.Id, "mother");
                ValidateOptionalPersonReference(
                    personIds, person.SpousePersonId, person.Id, "spouse");
            }

            var locationIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Locations.Count; i++)
            {
                var location = Locations[i] ?? throw new InvalidOperationException("A location cannot be null.");
                _ = new StableId(location.Id);
                locationIds.Add(location.Id);
                if (location.Population < 0)
                {
                    throw new InvalidOperationException($"Negative population at {location.Id}.");
                }

                if (location.PublicOrderBasisPoints < 0 ||
                    location.PublicOrderBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        $"Invalid public order at {location.Id}.");
                }

                if (location.GrainPrice <= 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid grain price at {location.Id}.");
                }
            }

            for (var i = 0; i < People.Count; i++)
            {
                var person = People[i];
                if (!locationIds.Contains(person.LocationId))
                {
                    throw new InvalidOperationException(
                        $"Person {person.Id} references missing location {person.LocationId}.");
                }
            }

            for (var i = 0; i < Families.Count; i++)
            {
                var family = Families[i] ?? throw new InvalidOperationException("A family cannot be null.");
                _ = new StableId(family.Id);
                if (!personIds.Contains(family.HeadPersonId))
                {
                    throw new InvalidOperationException(
                        $"Family {family.Id} references missing head {family.HeadPersonId}.");
                }

                if (family.Wealth < 0 || family.Debt < 0)
                {
                    throw new InvalidOperationException(
                        $"Family {family.Id} has invalid finances.");
                }

                for (var memberIndex = 0; memberIndex < family.MemberIds.Count; memberIndex++)
                {
                    if (!personIds.Contains(family.MemberIds[memberIndex]))
                    {
                        throw new InvalidOperationException(
                            $"Family {family.Id} references missing member {family.MemberIds[memberIndex]}.");
                    }
                }
            }

            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Routes.Count; i++)
            {
                var route = Routes[i];
                _ = new StableId(route.Id);
                routeIds.Add(route.Id);
                if (!locationIds.Contains(route.FromLocationId) ||
                    !locationIds.Contains(route.ToLocationId))
                {
                    throw new InvalidOperationException(
                        $"Route {route.Id} references a missing endpoint.");
                }

                if (route.FromLocationId == route.ToLocationId ||
                    route.DistanceKilometers <= 0 ||
                    route.SecurityBasisPoints < 0 ||
                    route.SecurityBasisPoints > 10_000)
                {
                    throw new InvalidOperationException($"Invalid route {route.Id}.");
                }
            }

            var travelingPeople = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Journeys.Count; i++)
            {
                var journey = Journeys[i];
                _ = new StableId(journey.Id);
                if (!personIds.Contains(journey.PersonId) ||
                    !routeIds.Contains(journey.RouteId) ||
                    !locationIds.Contains(journey.OriginLocationId) ||
                    !locationIds.Contains(journey.DestinationLocationId))
                {
                    throw new InvalidOperationException(
                        $"Journey {journey.Id} contains a missing reference.");
                }

                if (!travelingPeople.Add(journey.PersonId))
                {
                    throw new InvalidOperationException(
                        $"Person {journey.PersonId} has more than one journey.");
                }

                if (journey.RemainingKilometers <= 0)
                {
                    throw new InvalidOperationException(
                        $"Journey {journey.Id} has no remaining distance.");
                }

                var route = FindRoute(Routes, journey.RouteId);
                var forward = route.FromLocationId == journey.OriginLocationId &&
                    route.ToLocationId == journey.DestinationLocationId;
                var backward = route.Bidirectional &&
                    route.ToLocationId == journey.OriginLocationId &&
                    route.FromLocationId == journey.DestinationLocationId;
                if (!forward && !backward)
                {
                    throw new InvalidOperationException(
                        $"Journey {journey.Id} does not follow route {route.Id}.");
                }
            }

            for (var i = 0; i < Relationships.Count; i++)
            {
                var relationship = Relationships[i];
                _ = new StableId(relationship.Id);
                if (!personIds.Contains(relationship.FromPersonId) ||
                    !personIds.Contains(relationship.ToPersonId) ||
                    relationship.FromPersonId == relationship.ToPersonId)
                {
                    throw new InvalidOperationException(
                        $"Invalid relationship endpoints for {relationship.Id}.");
                }

                ValidateRelationshipValue(
                    relationship.Affection, relationship.Id, "affection");
                ValidateRelationshipValue(
                    relationship.Trust, relationship.Id, "trust");
                ValidateRelationshipValue(
                    relationship.Respect, relationship.Id, "respect");
                ValidateRelationshipValue(
                    relationship.Obligation, relationship.Id, "obligation");
            }

            var organizationIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Organizations.Count; i++)
            {
                var organization = Organizations[i];
                _ = new StableId(organization.Id);
                organizationIds.Add(organization.Id);
                if (!locationIds.Contains(organization.HeadquartersLocationId))
                {
                    throw new InvalidOperationException(
                        $"Organization {organization.Id} has no valid headquarters.");
                }

                if (!string.IsNullOrEmpty(organization.LeaderPersonId) &&
                    !personIds.Contains(organization.LeaderPersonId))
                {
                    throw new InvalidOperationException(
                        $"Organization {organization.Id} has no valid leader.");
                }

                ValidateBasisPoints(
                    organization.ReputationBasisPoints,
                    organization.Id,
                    "organization reputation");
            }

            var positionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Positions.Count; i++)
            {
                var position = Positions[i];
                _ = new StableId(position.Id);
                positionIds.Add(position.Id);
                if (!organizationIds.Contains(position.OrganizationId) ||
                    position.Capacity <= 0 ||
                    position.Rank < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid position {position.Id}.");
                }
            }

            var taskDefinitionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < TaskDefinitions.Count; i++)
            {
                var definition = TaskDefinitions[i];
                _ = new StableId(definition.Id);
                taskDefinitionIds.Add(definition.Id);
                if (!organizationIds.Contains(definition.IssuerOrganizationId) ||
                    !locationIds.Contains(definition.OriginLocationId) ||
                    !string.IsNullOrEmpty(definition.TargetLocationId) &&
                    !locationIds.Contains(definition.TargetLocationId) ||
                    !string.IsNullOrEmpty(definition.RequiredPositionId) &&
                    !positionIds.Contains(definition.RequiredPositionId) ||
                    definition.RequiredProgress <= 0 ||
                    definition.DurationDays <= 0 ||
                    definition.RewardMoney < 0 ||
                    definition.RewardProvisions < 0 ||
                    definition.ArmyProvisionReward < 0 ||
                    definition.ArmyProvisionReward > 0 &&
                    string.IsNullOrEmpty(definition.TargetArmyId))
                {
                    throw new InvalidOperationException(
                        $"Invalid task definition {definition.Id}.");
                }

                if (definition.Kind == TaskKind.TravelDelivery &&
                    string.IsNullOrEmpty(definition.TargetLocationId))
                {
                    throw new InvalidOperationException(
                        $"Travel task {definition.Id} has no target.");
                }
            }

            var activeAssignees = new HashSet<string>(StringComparer.Ordinal);
            var taskInstanceIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Tasks.Count; i++)
            {
                var task = Tasks[i];
                _ = new StableId(task.Id);
                taskInstanceIds.Add(task.Id);
                if (!taskDefinitionIds.Contains(task.DefinitionId) ||
                    !personIds.Contains(task.AssigneePersonId) ||
                    task.DeadlineDay < task.AcceptedDay ||
                    task.Progress < 0)
                {
                    throw new InvalidOperationException($"Invalid task {task.Id}.");
                }

                if (task.Status == TaskStatus.Active &&
                    !activeAssignees.Add(task.AssigneePersonId))
                {
                    throw new InvalidOperationException(
                        $"Person {task.AssigneePersonId} has multiple active tasks.");
                }
            }

            var armyIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Armies.Count; i++)
            {
                var army = Armies[i] ??
                    throw new InvalidOperationException("An army cannot be null.");
                _ = new StableId(army.Id);
                armyIds.Add(army.Id);
                if (!organizationIds.Contains(army.OrganizationId) ||
                    !personIds.Contains(army.CommanderPersonId) ||
                    !locationIds.Contains(army.LocationId) ||
                    army.Troops < 0 ||
                    army.WoundedTroops < 0 ||
                    army.MaximumTroops <= 0 ||
                    (long)army.Troops + army.WoundedTroops > army.MaximumTroops ||
                    army.Provisions < 0)
                {
                    throw new InvalidOperationException($"Invalid army {army.Id}.");
                }

                ValidateBasisPoints(
                    army.MoraleBasisPoints, army.Id, "army morale");
                ValidateBasisPoints(
                    army.TrainingBasisPoints, army.Id, "army training");
            }

            for (var i = 0; i < TaskDefinitions.Count; i++)
            {
                var targetArmyId = TaskDefinitions[i].TargetArmyId;
                if (!string.IsNullOrEmpty(targetArmyId) &&
                    !armyIds.Contains(targetArmyId))
                {
                    throw new InvalidOperationException(
                        $"Task definition {TaskDefinitions[i].Id} has a missing target army.");
                }
            }

            var marchingArmies = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < ArmyMarches.Count; i++)
            {
                var march = ArmyMarches[i] ??
                    throw new InvalidOperationException("An army march cannot be null.");
                _ = new StableId(march.Id);
                if (!armyIds.Contains(march.ArmyId) ||
                    !routeIds.Contains(march.RouteId) ||
                    !locationIds.Contains(march.OriginLocationId) ||
                    !locationIds.Contains(march.DestinationLocationId) ||
                    march.RemainingKilometers <= 0 ||
                    !marchingArmies.Add(march.ArmyId))
                {
                    throw new InvalidOperationException(
                        $"Invalid army march {march.Id}.");
                }

                var marchingArmy = FindArmy(Armies, march.ArmyId);
                if (!marchingArmy.IsMobilized ||
                    marchingArmy.Troops <= 0 ||
                    marchingArmy.LocationId != march.OriginLocationId)
                {
                    throw new InvalidOperationException(
                        $"Army march {march.Id} has an invalid army state.");
                }

                var route = FindRoute(Routes, march.RouteId);
                var forward =
                    route.FromLocationId == march.OriginLocationId &&
                    route.ToLocationId == march.DestinationLocationId;
                var backward =
                    route.Bidirectional &&
                    route.ToLocationId == march.OriginLocationId &&
                    route.FromLocationId == march.DestinationLocationId;
                if (!forward && !backward)
                {
                    throw new InvalidOperationException(
                        $"Army march {march.Id} does not follow its route.");
                }
            }

            for (var i = 0; i < Battles.Count; i++)
            {
                var battle = Battles[i] ??
                    throw new InvalidOperationException("A battle cannot be null.");
                _ = new StableId(battle.Id);
                if (!locationIds.Contains(battle.LocationId) ||
                    !armyIds.Contains(battle.AttackerArmyId) ||
                    !armyIds.Contains(battle.DefenderArmyId) ||
                    battle.AttackerArmyId == battle.DefenderArmyId ||
                    battle.Day < 0 ||
                    battle.AttackerInitialTroops <= 0 ||
                    battle.DefenderInitialTroops <= 0 ||
                    battle.AttackerCasualties < 0 ||
                    battle.DefenderCasualties < 0 ||
                    battle.AttackerCasualties > battle.AttackerInitialTroops ||
                    battle.DefenderCasualties > battle.DefenderInitialTroops ||
                    battle.AttackerWounded < 0 ||
                    battle.DefenderWounded < 0 ||
                    battle.AttackerWounded > battle.AttackerCasualties ||
                    battle.DefenderWounded > battle.DefenderCasualties ||
                    !string.IsNullOrEmpty(battle.WinnerArmyId) &&
                    !armyIds.Contains(battle.WinnerArmyId))
                {
                    throw new InvalidOperationException(
                        $"Invalid battle {battle.Id}.");
                }
            }

            for (var i = 0; i < MilitarySupplies.Count; i++)
            {
                var supply = MilitarySupplies[i] ??
                    throw new InvalidOperationException(
                        "A military supply record cannot be null.");
                _ = new StableId(supply.Id);
                if (!armyIds.Contains(supply.ArmyId) ||
                    !string.IsNullOrEmpty(supply.SupplierPersonId) &&
                    !personIds.Contains(supply.SupplierPersonId) ||
                    !string.IsNullOrEmpty(supply.SourceTaskInstanceId) &&
                    !taskInstanceIds.Contains(supply.SourceTaskInstanceId) ||
                    supply.Day < 0 ||
                    supply.GrainUnits < 0 ||
                    supply.ProvisionsAdded <= 0 ||
                    supply.UnitPrice < 0 ||
                    supply.TotalPaid < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid military supply {supply.Id}.");
                }
            }

            for (var i = 0; i < MedicalTreatments.Count; i++)
            {
                var treatment = MedicalTreatments[i] ??
                    throw new InvalidOperationException(
                        "A medical treatment record cannot be null.");
                _ = new StableId(treatment.Id);
                if (!personIds.Contains(treatment.PhysicianPersonId) ||
                    !armyIds.Contains(treatment.ArmyId) ||
                    treatment.Day < 0 ||
                    treatment.PatientsTreated <= 0 ||
                    treatment.RecoveredTroops < 0 ||
                    treatment.RecoveredTroops > treatment.PatientsTreated ||
                    treatment.HerbsConsumed <= 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid medical treatment {treatment.Id}.");
                }
            }

            var historicalDefinitionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < HistoricalEventDefinitions.Count; i++)
            {
                var definition = HistoricalEventDefinitions[i];
                _ = new StableId(definition.Id);
                historicalDefinitionIds.Add(definition.Id);
                if (definition.EarliestDay < 0 ||
                    definition.LatestDay < definition.EarliestDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid historical date window for {definition.Id}.");
                }

                for (var effectIndex = 0;
                     effectIndex < definition.Effects.Count;
                     effectIndex++)
                {
                    var effect = definition.Effects[effectIndex];
                    var validTarget = EffectTargetExists(
                        effect.Type,
                        effect.TargetId,
                        personIds,
                        locationIds,
                        routeIds,
                        taskDefinitionIds,
                        armyIds);
                    if (!validTarget)
                    {
                        throw new InvalidOperationException(
                            $"Historical effect in {definition.Id} has an invalid target.");
                    }
                }
            }

            for (var i = 0; i < HistoricalEventDefinitions.Count; i++)
            {
                var prerequisite = HistoricalEventDefinitions[i].PrerequisiteEventId;
                if (!string.IsNullOrEmpty(prerequisite) &&
                    !historicalDefinitionIds.Contains(prerequisite))
                {
                    throw new InvalidOperationException(
                        $"Historical event {HistoricalEventDefinitions[i].Id} " +
                        $"has a missing prerequisite.");
                }
            }

            var anchorDefinitions = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < HistoricalAnchors.Count; i++)
            {
                var anchor = HistoricalAnchors[i];
                _ = new StableId(anchor.Id);
                if (!historicalDefinitionIds.Contains(anchor.DefinitionId) ||
                    !anchorDefinitions.Add(anchor.DefinitionId) ||
                    anchor.ResolvedDay < -1)
                {
                    throw new InvalidOperationException(
                        $"Invalid historical anchor {anchor.Id}.");
                }
            }

            for (var i = 0; i < LifeEvents.Count; i++)
            {
                var lifeEvent = LifeEvents[i];
                _ = new StableId(lifeEvent.Id);
                if (!string.IsNullOrEmpty(lifeEvent.PrimaryPersonId) &&
                    !personIds.Contains(lifeEvent.PrimaryPersonId) ||
                    !string.IsNullOrEmpty(lifeEvent.SecondaryPersonId) &&
                    !personIds.Contains(lifeEvent.SecondaryPersonId) ||
                    !string.IsNullOrEmpty(lifeEvent.FamilyId) &&
                    !FamilyExists(Families, lifeEvent.FamilyId) ||
                    lifeEvent.Day < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid life event {lifeEvent.Id}.");
                }
            }

            var commodityIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Commodities.Count; i++)
            {
                var commodity = Commodities[i] ??
                    throw new InvalidOperationException("A commodity cannot be null.");
                _ = new StableId(commodity.Id);
                commodityIds.Add(commodity.Id);
                if (commodity.BasePrice <= 0 || commodity.UnitWeight <= 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid commodity {commodity.Id}.");
                }
            }

            var marketKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MarketListings.Count; i++)
            {
                var listing = MarketListings[i] ??
                    throw new InvalidOperationException("A market listing cannot be null.");
                _ = new StableId(listing.Id);
                if (!locationIds.Contains(listing.LocationId) ||
                    !commodityIds.Contains(listing.CommodityId) ||
                    listing.Price <= 0 ||
                    listing.EquilibriumPrice <= 0 ||
                    listing.Stock < 0 ||
                    listing.TargetStock <= 0 ||
                    !marketKeys.Add(listing.LocationId + "|" + listing.CommodityId))
                {
                    throw new InvalidOperationException(
                        $"Invalid market listing {listing.Id}.");
                }
            }

            var carriedWeight = new Dictionary<string, long>(StringComparer.Ordinal);
            var inventoryKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Inventories.Count; i++)
            {
                var stack = Inventories[i] ??
                    throw new InvalidOperationException("An inventory stack cannot be null.");
                _ = new StableId(stack.Id);
                if (!personIds.Contains(stack.OwnerPersonId) ||
                    !commodityIds.Contains(stack.CommodityId) ||
                    stack.Quantity <= 0 ||
                    stack.AverageUnitCost < 0 ||
                    !inventoryKeys.Add(stack.OwnerPersonId + "|" + stack.CommodityId))
                {
                    throw new InvalidOperationException(
                        $"Invalid inventory stack {stack.Id}.");
                }

                var commodity = FindCommodity(Commodities, stack.CommodityId);
                carriedWeight.TryGetValue(stack.OwnerPersonId, out var currentWeight);
                carriedWeight[stack.OwnerPersonId] = checked(
                    currentWeight + (long)stack.Quantity * commodity.UnitWeight);
            }

            foreach (var pair in carriedWeight)
            {
                var owner = FindPerson(People, pair.Key);
                if (pair.Value > owner.CargoCapacity)
                {
                    throw new InvalidOperationException(
                        $"Person {pair.Key} exceeds cargo capacity.");
                }
            }

            for (var i = 0; i < TradeRecords.Count; i++)
            {
                var record = TradeRecords[i] ??
                    throw new InvalidOperationException("A trade record cannot be null.");
                _ = new StableId(record.Id);
                if (!personIds.Contains(record.PersonId) ||
                    !locationIds.Contains(record.LocationId) ||
                    !commodityIds.Contains(record.CommodityId) ||
                    record.Day < 0 ||
                    record.Quantity <= 0 ||
                    record.UnitPrice <= 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid trade record {record.Id}.");
                }
            }

            var membershipKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Memberships.Count; i++)
            {
                var membership = Memberships[i];
                _ = new StableId(membership.Id);
                if (!personIds.Contains(membership.PersonId) ||
                    !organizationIds.Contains(membership.OrganizationId) ||
                    !positionIds.Contains(membership.PositionId))
                {
                    throw new InvalidOperationException(
                        $"Membership {membership.Id} contains a missing reference.");
                }

                var position = FindPosition(Positions, membership.PositionId);
                if (position.OrganizationId != membership.OrganizationId)
                {
                    throw new InvalidOperationException(
                        $"Membership {membership.Id} uses a position from another organization.");
                }

                var key = membership.PersonId + "|" + membership.OrganizationId;
                if (!membershipKeys.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate membership for {key}.");
                }

                ValidateBasisPoints(
                    membership.LoyaltyBasisPoints,
                    membership.Id,
                    "membership loyalty");
            }

            for (var i = 0; i < Positions.Count; i++)
            {
                var occupied = 0;
                for (var memberIndex = 0; memberIndex < Memberships.Count; memberIndex++)
                {
                    if (Memberships[memberIndex].PositionId == Positions[i].Id)
                    {
                        occupied++;
                    }
                }

                if (occupied > Positions[i].Capacity)
                {
                    throw new InvalidOperationException(
                        $"Position {Positions[i].Id} exceeds its capacity.");
                }
            }
        }

        private static void ValidateUniqueIds<T>(
            IList<T> items,
            Func<T, string> selectId,
            string entityType)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    throw new InvalidOperationException($"A {entityType} cannot be null.");
                }

                var id = selectId(item);
                _ = new StableId(id);
                if (!ids.Add(id))
                {
                    throw new InvalidOperationException($"Duplicate {entityType} ID: {id}.");
                }
            }
        }

        private static void ValidateBasisPoints(int value, string personId, string field)
        {
            if (value < 0 || value > 10_000)
            {
                throw new InvalidOperationException(
                    $"Invalid {field} for {personId}: {value}.");
            }
        }

        private static RouteState FindRoute(IList<RouteState> routes, string routeId)
        {
            for (var i = 0; i < routes.Count; i++)
            {
                if (routes[i].Id == routeId)
                {
                    return routes[i];
                }
            }

            throw new InvalidOperationException($"Missing route {routeId}.");
        }

        private static void ValidateRelationshipValue(
            int value,
            string relationshipId,
            string field)
        {
            if (value < -10_000 || value > 10_000)
            {
                throw new InvalidOperationException(
                    $"Invalid {field} for {relationshipId}: {value}.");
            }
        }

        private static PositionState FindPosition(
            IList<PositionState> positions,
            string positionId)
        {
            for (var i = 0; i < positions.Count; i++)
            {
                if (positions[i].Id == positionId)
                {
                    return positions[i];
                }
            }

            throw new InvalidOperationException($"Missing position {positionId}.");
        }

        private static bool EffectTargetExists(
            HistoricalEffectType effectType,
            string targetId,
            HashSet<string> personIds,
            HashSet<string> locationIds,
            HashSet<string> routeIds,
            HashSet<string> taskDefinitionIds,
            HashSet<string> armyIds)
        {
            switch (effectType)
            {
                case HistoricalEffectType.AdjustPublicOrder:
                case HistoricalEffectType.AdjustGrainPrice:
                    return locationIds.Contains(targetId);
                case HistoricalEffectType.SetWarPressure:
                    return personIds.Contains(targetId);
                case HistoricalEffectType.AdjustRouteSecurity:
                    return routeIds.Contains(targetId);
                case HistoricalEffectType.SetTaskAvailability:
                    return taskDefinitionIds.Contains(targetId);
                case HistoricalEffectType.SetArmyMobilized:
                    return armyIds.Contains(targetId);
                default:
                    return false;
            }
        }

        private static void ValidateOptionalPersonReference(
            HashSet<string> personIds,
            string referencedPersonId,
            string ownerId,
            string field)
        {
            if (!string.IsNullOrEmpty(referencedPersonId) &&
                !personIds.Contains(referencedPersonId))
            {
                throw new InvalidOperationException(
                    $"Person {ownerId} has a missing {field} reference.");
            }
        }

        private static bool FamilyExists(IList<FamilyState> families, string familyId)
        {
            for (var i = 0; i < families.Count; i++)
            {
                if (families[i].Id == familyId)
                {
                    return true;
                }
            }

            return false;
        }

        private static CommodityState FindCommodity(
            IList<CommodityState> commodities,
            string commodityId)
        {
            for (var i = 0; i < commodities.Count; i++)
            {
                if (commodities[i].Id == commodityId)
                {
                    return commodities[i];
                }
            }

            throw new InvalidOperationException($"Missing commodity {commodityId}.");
        }

        private static PersonState FindPerson(
            IList<PersonState> people,
            string personId)
        {
            for (var i = 0; i < people.Count; i++)
            {
                if (people[i].Id == personId)
                {
                    return people[i];
                }
            }

            throw new InvalidOperationException($"Missing person {personId}.");
        }

        private static ArmyState FindArmy(
            IList<ArmyState> armies,
            string armyId)
        {
            for (var i = 0; i < armies.Count; i++)
            {
                if (armies[i].Id == armyId)
                {
                    return armies[i];
                }
            }

            throw new InvalidOperationException($"Missing army {armyId}.");
        }
    }
}
