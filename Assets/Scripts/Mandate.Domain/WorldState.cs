using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum LocationKind : byte
    {
        Unknown,
        RegionalSeat,
        CountySeat,
        Pass,
        Port,
        MarketTown,
        Village,
        Camp
    }

    public enum TerrainKind : byte
    {
        Unknown,
        Plains,
        Hills,
        Mountains,
        Riverland,
        Forest,
        Marsh
    }

    [Flags]
    public enum LocationFeature : ushort
    {
        None = 0,
        Government = 1 << 0,
        Market = 1 << 1,
        Garrison = 1 << 2,
        Farmland = 1 << 3,
        Workshop = 1 << 4,
        Clinic = 1 << 5,
        Temple = 1 << 6,
        RelayStation = 1 << 7,
        Harbor = 1 << 8,
        Fortification = 1 << 9,
        All = Government |
              Market |
              Garrison |
              Farmland |
              Workshop |
              Clinic |
              Temple |
              RelayStation |
              Harbor |
              Fortification
    }

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
        public bool CountsTowardPopulation = true;
        public string PopulationOriginLocationId;
        public bool AbilityProfileInitialized;
        public CharacterAptitudeState Aptitudes = new CharacterAptitudeState();
        public ProfessionalSkillState ProfessionalSkills =
            new ProfessionalSkillState();
        public LifeGoalKind LifeGoal = LifeGoalKind.Unknown;
        public PersonalityState Personality = new PersonalityState();
        public NeedState Needs = new NeedState();
    }

    [Serializable]
    public sealed class LocationState
    {
        public string Id;
        public string DisplayName;
        public LocationKind Kind = LocationKind.CountySeat;
        public TerrainKind Terrain = TerrainKind.Plains;
        public LocationFeature Features = LocationFeature.None;
        public int StrategicImportance = 1;
        public string ParentLocationId;
        public int Population;
        public int PublicOrderBasisPoints = 5_000;
        public int GrainPrice = 100;
        public int MapXBasisPoints;
        public int MapYBasisPoints;
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
        public const int CurrentSchemaVersion = 4;

        public int SchemaVersion = CurrentSchemaVersion;
        public ulong MasterSeed;
        public long AbsoluteDay;
        public byte Segment;
        public long Revision;
        public string PlayerPersonId;
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
        public List<ConstructionProjectState> ConstructionProjects =
            new List<ConstructionProjectState>();
        public bool PopulationLedgerInitialized;
        public long PopulationOpeningTotal;
        public List<PopulationCohortState> PopulationCohorts =
            new List<PopulationCohortState>();
        public List<PopulationTransactionState> PopulationTransactions =
            new List<PopulationTransactionState>();
        public List<EducationPlanState> EducationPlans =
            new List<EducationPlanState>();
        public List<LearningRecordState> LearningRecords =
            new List<LearningRecordState>();

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
            ValidateUniqueIds(
                ConstructionProjects, item => item.Id, "construction project");
            ValidateUniqueIds(
                PopulationCohorts, item => item.Id, "population cohort");
            ValidateUniqueIds(
                PopulationTransactions, item => item.Id, "population transaction");
            ValidateUniqueIds(
                EducationPlans, item => item.Id, "education plan");
            ValidateUniqueIds(
                LearningRecords, item => item.Id, "learning record");

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
                if (person.Aptitudes == null)
                {
                    throw new InvalidOperationException(
                        $"Missing aptitudes for {person.Id}.");
                }

                if (person.ProfessionalSkills == null)
                {
                    throw new InvalidOperationException(
                        $"Missing professional skills for {person.Id}.");
                }

                ValidateBasisPoints(
                    person.Aptitudes.Constitution, person.Id, "constitution");
                ValidateBasisPoints(
                    person.Aptitudes.Strength, person.Id, "strength");
                ValidateBasisPoints(
                    person.Aptitudes.Dexterity, person.Id, "dexterity");
                ValidateBasisPoints(
                    person.Aptitudes.Perception, person.Id, "perception");
                ValidateBasisPoints(
                    person.Aptitudes.Memory, person.Id, "memory");
                ValidateBasisPoints(
                    person.Aptitudes.Reasoning, person.Id, "reasoning");
                ValidateBasisPoints(
                    person.Aptitudes.Willpower, person.Id, "willpower");
                ValidateBasisPoints(
                    person.Aptitudes.Affinity, person.Id, "affinity");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Military, person.Id, "military");
                ValidateBasisPoints(
                    person.ProfessionalSkills.MartialArts,
                    person.Id,
                    "martial arts");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Administration,
                    person.Id,
                    "administration");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Commerce, person.Id, "commerce");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Agriculture,
                    person.Id,
                    "agriculture");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Craft, person.Id, "craft");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Medicine, person.Id, "medicine");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Scholarship,
                    person.Id,
                    "scholarship");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Negotiation,
                    person.Id,
                    "negotiation");
                ValidateBasisPoints(
                    person.ProfessionalSkills.Intelligence,
                    person.Id,
                    "intelligence");
                if (!Enum.IsDefined(typeof(LifeGoalKind), person.LifeGoal))
                {
                    throw new InvalidOperationException(
                        $"Invalid life goal for {person.Id}.");
                }
            }

            if (!string.IsNullOrEmpty(PlayerPersonId) &&
                !personIds.Contains(PlayerPersonId))
            {
                throw new InvalidOperationException(
                    $"Player references missing person {PlayerPersonId}.");
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

                if (location.MapXBasisPoints < 0 ||
                    location.MapXBasisPoints > 10_000 ||
                    location.MapYBasisPoints < 0 ||
                    location.MapYBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        $"Invalid map position at {location.Id}.");
                }

                if (!Enum.IsDefined(typeof(LocationKind), location.Kind) ||
                    location.Kind == LocationKind.Unknown)
                {
                    throw new InvalidOperationException(
                        $"Invalid location kind at {location.Id}.");
                }

                if (!Enum.IsDefined(typeof(TerrainKind), location.Terrain) ||
                    location.Terrain == TerrainKind.Unknown)
                {
                    throw new InvalidOperationException(
                        $"Invalid terrain at {location.Id}.");
                }

                if ((location.Features & ~LocationFeature.All) != 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid location features at {location.Id}.");
                }

                if (location.StrategicImportance < 1 ||
                    location.StrategicImportance > 5)
                {
                    throw new InvalidOperationException(
                        $"Invalid strategic importance at {location.Id}.");
                }
            }

            for (var i = 0; i < Locations.Count; i++)
            {
                var location = Locations[i];
                if (string.IsNullOrEmpty(location.ParentLocationId))
                {
                    continue;
                }

                if (location.ParentLocationId == location.Id ||
                    !locationIds.Contains(location.ParentLocationId))
                {
                    throw new InvalidOperationException(
                        $"Location {location.Id} references invalid parent " +
                        $"{location.ParentLocationId}.");
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

                if (PopulationLedgerInitialized &&
                    person.CountsTowardPopulation &&
                    (string.IsNullOrEmpty(person.PopulationOriginLocationId) ||
                     !locationIds.Contains(person.PopulationOriginLocationId)))
                {
                    throw new InvalidOperationException(
                        $"Person {person.Id} has an invalid population origin.");
                }
            }

            ValidatePopulationLedger(personIds, locationIds);

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

            var constructionTargets =
                new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < ConstructionProjects.Count; i++)
            {
                var project = ConstructionProjects[i] ??
                    throw new InvalidOperationException(
                        "A construction project cannot be null.");
                _ = new StableId(project.Id);
                var featureValue = (ushort)project.TargetFeature;
                var singleFeature =
                    featureValue != 0 &&
                    (featureValue & (featureValue - 1)) == 0 &&
                    (project.TargetFeature & ~LocationFeature.All) == 0;
                var targetKey =
                    project.LocationId + "|" + (ushort)project.TargetFeature;
                if (!locationIds.Contains(project.LocationId) ||
                    !personIds.Contains(project.SponsorPersonId) ||
                    !singleFeature ||
                    !constructionTargets.Add(targetKey) ||
                    project.StartedDay < 0 ||
                    project.StartedDay > AbsoluteDay ||
                    project.RequiredProgress <= 0 ||
                    project.Progress < 0 ||
                    project.Progress > project.RequiredProgress ||
                    project.MoneyInvested < 0 ||
                    project.IsCompleted !=
                    (project.Progress == project.RequiredProgress) ||
                    project.IsCompleted &&
                    (project.CompletedDay < project.StartedDay ||
                     project.CompletedDay > AbsoluteDay) ||
                    !project.IsCompleted && project.CompletedDay != -1)
                {
                    throw new InvalidOperationException(
                        $"Invalid construction project {project.Id}.");
                }

                var location = FindLocation(Locations, project.LocationId);
                var featureExists =
                    (location.Features & project.TargetFeature) != 0;
                if (project.IsCompleted != featureExists)
                {
                    throw new InvalidOperationException(
                        $"Construction project {project.Id} is inconsistent " +
                        "with its location feature.");
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

            ValidateEducation(personIds, positionIds);
        }

        private void ValidateEducation(
            HashSet<string> personIds,
            HashSet<string> positionIds)
        {
            var activeStudents = new HashSet<string>(StringComparer.Ordinal);
            var activeTeacherCounts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            var planIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < EducationPlans.Count; i++)
            {
                var plan = EducationPlans[i];
                planIds.Add(plan.Id);
                if (!personIds.Contains(plan.StudentPersonId) ||
                    !string.IsNullOrEmpty(plan.TeacherPersonId) &&
                    !personIds.Contains(plan.TeacherPersonId) ||
                    plan.StudentPersonId == plan.TeacherPersonId)
                {
                    throw new InvalidOperationException(
                        $"Education plan {plan.Id} has an invalid person reference.");
                }

                if (!Enum.IsDefined(
                        typeof(ProfessionalDiscipline), plan.Discipline) ||
                    !Enum.IsDefined(
                        typeof(EducationFundingSource), plan.FundingSource) ||
                    !Enum.IsDefined(
                        typeof(EducationPlanStatus), plan.Status) ||
                    plan.MonthlyStudyDays < 1 ||
                    plan.MonthlyStudyDays > 20 ||
                    plan.MonthlyFee < 0 ||
                    plan.CreatedDay < 0 ||
                    plan.CreatedDay > AbsoluteDay ||
                    plan.LastResolvedDay < -1 ||
                    plan.LastResolvedDay > AbsoluteDay ||
                    plan.LastResolvedDay >= 0 &&
                    plan.LastResolvedDay < plan.CreatedDay ||
                    plan.TotalStudyDays < 0 ||
                    plan.TotalFeesPaid < 0 ||
                    plan.TotalSkillGain < 0)
                {
                    throw new InvalidOperationException(
                        $"Education plan {plan.Id} has invalid values.");
                }

                if (plan.FundingSource == EducationFundingSource.Family)
                {
                    if (string.IsNullOrEmpty(plan.FundingFamilyId) ||
                        !FamilyContainsPerson(
                            Families,
                            plan.FundingFamilyId,
                            plan.StudentPersonId))
                    {
                        throw new InvalidOperationException(
                            $"Education plan {plan.Id} has invalid family funding.");
                    }
                }
                else if (!string.IsNullOrEmpty(plan.FundingFamilyId))
                {
                    throw new InvalidOperationException(
                        $"Education plan {plan.Id} has unexpected family funding.");
                }

                if (!string.IsNullOrEmpty(plan.PracticePositionId) &&
                    (!positionIds.Contains(plan.PracticePositionId) ||
                     !HasMembershipPosition(
                         Memberships,
                         plan.StudentPersonId,
                         plan.PracticePositionId)))
                {
                    throw new InvalidOperationException(
                        $"Education plan {plan.Id} has invalid practice position.");
                }

                if (plan.Status != EducationPlanStatus.Active &&
                    plan.Status != EducationPlanStatus.Suspended)
                {
                    continue;
                }

                if (!activeStudents.Add(plan.StudentPersonId))
                {
                    throw new InvalidOperationException(
                        $"Person {plan.StudentPersonId} has multiple education plans.");
                }

                if (string.IsNullOrEmpty(plan.TeacherPersonId))
                {
                    continue;
                }

                activeTeacherCounts.TryGetValue(
                    plan.TeacherPersonId, out var teacherCount);
                teacherCount++;
                if (teacherCount > 3)
                {
                    throw new InvalidOperationException(
                        $"Teacher {plan.TeacherPersonId} exceeds student capacity.");
                }

                activeTeacherCounts[plan.TeacherPersonId] = teacherCount;
            }

            for (var i = 0; i < LearningRecords.Count; i++)
            {
                var record = LearningRecords[i];
                if (!planIds.Contains(record.EducationPlanId) ||
                    !personIds.Contains(record.StudentPersonId) ||
                    !string.IsNullOrEmpty(record.TeacherPersonId) &&
                    !personIds.Contains(record.TeacherPersonId) ||
                    !Enum.IsDefined(
                        typeof(ProfessionalDiscipline), record.Discipline) ||
                    !Enum.IsDefined(
                        typeof(LearningOutcomeKind), record.Outcome) ||
                    record.Day < 0 ||
                    record.Day > AbsoluteDay ||
                    record.MonthIndex < 0 ||
                    record.StudyDays < 0 ||
                    record.StudyDays > 20 ||
                    record.FeePaid < 0 ||
                    record.SkillGain < 0 ||
                    record.SkillAfter - record.SkillBefore != record.SkillGain ||
                    string.IsNullOrWhiteSpace(record.Summary))
                {
                    throw new InvalidOperationException(
                        $"Learning record {record.Id} has invalid values.");
                }

                ValidateBasisPoints(
                    record.SkillBefore, record.Id, "learning skill before");
                ValidateBasisPoints(
                    record.SkillAfter, record.Id, "learning skill after");
                ValidateBasisPoints(
                    record.CompositeAptitudeBasisPoints,
                    record.Id,
                    "learning aptitude");
                ValidateBasisPoints(
                    record.SoftPotentialBasisPoints,
                    record.Id,
                    "learning soft potential");
                ValidateOptionalLearningFactor(
                    record.TeacherFactorBasisPoints,
                    record.Id,
                    "teacher factor");
                ValidateOptionalLearningFactor(
                    record.FacilityFactorBasisPoints,
                    record.Id,
                    "facility factor");
                ValidateOptionalLearningFactor(
                    record.HealthFactorBasisPoints,
                    record.Id,
                    "health factor");
                ValidateOptionalLearningFactor(
                    record.MotivationFactorBasisPoints,
                    record.Id,
                    "motivation factor");
                ValidateOptionalLearningFactor(
                    record.PracticeFactorBasisPoints,
                    record.Id,
                    "practice factor");
                ValidateOptionalLearningFactor(
                    record.DiminishingFactorBasisPoints,
                    record.Id,
                    "diminishing factor");

                var plan = FindEducationPlan(EducationPlans, record.EducationPlanId);
                if (plan.StudentPersonId != record.StudentPersonId ||
                    plan.Discipline != record.Discipline)
                {
                    throw new InvalidOperationException(
                        $"Learning record {record.Id} does not match its plan.");
                }
            }
        }

        private void ValidatePopulationLedger(
            HashSet<string> personIds,
            HashSet<string> locationIds)
        {
            if (!PopulationLedgerInitialized)
            {
                if (PopulationOpeningTotal != 0 ||
                    PopulationCohorts.Count != 0 ||
                    PopulationTransactions.Count != 0)
                {
                    throw new InvalidOperationException(
                        "Uninitialized population ledger contains data.");
                }

                return;
            }

            if (PopulationOpeningTotal < 0)
            {
                throw new InvalidOperationException(
                    "Population opening total cannot be negative.");
            }

            var cohortIds = new HashSet<string>(StringComparer.Ordinal);
            var populationByLocation =
                new Dictionary<string, long>(StringComparer.Ordinal);
            for (var i = 0; i < Locations.Count; i++)
            {
                populationByLocation.Add(Locations[i].Id, 0);
            }

            long actualPopulation = 0;
            for (var i = 0; i < PopulationCohorts.Count; i++)
            {
                var cohort = PopulationCohorts[i];
                cohortIds.Add(cohort.Id);
                if (!locationIds.Contains(cohort.LocationId) ||
                    !locationIds.Contains(cohort.OriginLocationId) ||
                    !Enum.IsDefined(
                        typeof(PopulationOccupation),
                        cohort.Occupation) ||
                    cohort.Population < 0 ||
                    cohort.Households < 0 ||
                    cohort.WorkingAgePopulation < 0 ||
                    cohort.WorkingAgePopulation > cohort.Population ||
                    cohort.CollectiveWealth < 0 ||
                    cohort.StableSeed == 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid population cohort {cohort.Id}.");
                }

                ValidateBasisPoints(
                    cohort.AverageHealthBasisPoints,
                    cohort.Id,
                    "average health");
                ValidateBasisPoints(
                    cohort.SatisfactionBasisPoints,
                    cohort.Id,
                    "satisfaction");
                ValidateBasisPoints(
                    cohort.MigrationPressureBasisPoints,
                    cohort.Id,
                    "migration pressure");
                actualPopulation += cohort.Population;
                populationByLocation[cohort.LocationId] += cohort.Population;
            }

            for (var i = 0; i < People.Count; i++)
            {
                var person = People[i];
                if (!person.CountsTowardPopulation || !person.IsAlive)
                {
                    continue;
                }

                actualPopulation++;
                populationByLocation[person.LocationId]++;
            }

            long expectedPopulation = PopulationOpeningTotal;
            for (var i = 0; i < PopulationTransactions.Count; i++)
            {
                var transaction = PopulationTransactions[i];
                if (!Enum.IsDefined(
                        typeof(PopulationTransactionType),
                        transaction.Type) ||
                    transaction.Day < 0 ||
                    transaction.Day > AbsoluteDay ||
                    transaction.Quantity <= 0 ||
                    !string.IsNullOrEmpty(transaction.FromLocationId) &&
                    !locationIds.Contains(transaction.FromLocationId) ||
                    !string.IsNullOrEmpty(transaction.ToLocationId) &&
                    !locationIds.Contains(transaction.ToLocationId) ||
                    !string.IsNullOrEmpty(transaction.FromCohortId) &&
                    !cohortIds.Contains(transaction.FromCohortId) ||
                    !string.IsNullOrEmpty(transaction.ToCohortId) &&
                    !cohortIds.Contains(transaction.ToCohortId) ||
                    !string.IsNullOrEmpty(transaction.PersonId) &&
                    !personIds.Contains(transaction.PersonId))
                {
                    throw new InvalidOperationException(
                        $"Invalid population transaction {transaction.Id}.");
                }

                switch (transaction.Type)
                {
                    case PopulationTransactionType.Birth:
                        if (string.IsNullOrEmpty(transaction.ToLocationId) ||
                            string.IsNullOrEmpty(transaction.PersonId))
                        {
                            throw new InvalidOperationException(
                                $"Birth transaction {transaction.Id} is incomplete.");
                        }

                        expectedPopulation += transaction.Quantity;
                        break;
                    case PopulationTransactionType.Death:
                        if (string.IsNullOrEmpty(transaction.FromLocationId) ||
                            string.IsNullOrEmpty(transaction.PersonId))
                        {
                            throw new InvalidOperationException(
                                $"Death transaction {transaction.Id} is incomplete.");
                        }

                        expectedPopulation -= transaction.Quantity;
                        break;
                    case PopulationTransactionType.Migration:
                        if (string.IsNullOrEmpty(transaction.FromLocationId) ||
                            string.IsNullOrEmpty(transaction.ToLocationId) ||
                            transaction.FromLocationId ==
                            transaction.ToLocationId)
                        {
                            throw new InvalidOperationException(
                                $"Migration transaction {transaction.Id} is incomplete.");
                        }

                        break;
                    case PopulationTransactionType.Instantiation:
                        if (transaction.Quantity != 1 ||
                            string.IsNullOrEmpty(transaction.FromCohortId) ||
                            string.IsNullOrEmpty(transaction.PersonId))
                        {
                            throw new InvalidOperationException(
                                $"Instantiation transaction {transaction.Id} " +
                                "is incomplete.");
                        }

                        break;
                    case PopulationTransactionType.Reaggregation:
                        if (transaction.Quantity != 1 ||
                            string.IsNullOrEmpty(transaction.ToCohortId) ||
                            string.IsNullOrEmpty(transaction.PersonId))
                        {
                            throw new InvalidOperationException(
                                $"Reaggregation transaction {transaction.Id} " +
                                "is incomplete.");
                        }

                        break;
                }
            }

            if (actualPopulation != expectedPopulation)
            {
                throw new InvalidOperationException(
                    $"Population conservation failed: actual {actualPopulation}, " +
                    $"expected {expectedPopulation}.");
            }

            for (var i = 0; i < Locations.Count; i++)
            {
                var location = Locations[i];
                if (populationByLocation[location.Id] != location.Population)
                {
                    throw new InvalidOperationException(
                        $"Population summary mismatch at {location.Id}: " +
                        $"summary {location.Population}, ledger " +
                        $"{populationByLocation[location.Id]}.");
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

        private static LocationState FindLocation(
            List<LocationState> locations,
            string locationId)
        {
            for (var i = 0; i < locations.Count; i++)
            {
                if (locations[i].Id == locationId)
                {
                    return locations[i];
                }
            }

            return null;
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

        private static bool FamilyContainsPerson(
            IList<FamilyState> families,
            string familyId,
            string personId)
        {
            for (var i = 0; i < families.Count; i++)
            {
                if (families[i].Id == familyId)
                {
                    return families[i].MemberIds.Contains(personId);
                }
            }

            return false;
        }

        private static bool HasMembershipPosition(
            IList<MembershipState> memberships,
            string personId,
            string positionId)
        {
            for (var i = 0; i < memberships.Count; i++)
            {
                if (memberships[i].PersonId == personId &&
                    memberships[i].PositionId == positionId)
                {
                    return true;
                }
            }

            return false;
        }

        private static EducationPlanState FindEducationPlan(
            IList<EducationPlanState> plans,
            string planId)
        {
            for (var i = 0; i < plans.Count; i++)
            {
                if (plans[i].Id == planId)
                {
                    return plans[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing education plan {planId}.");
        }

        private static void ValidateOptionalLearningFactor(
            int value,
            string recordId,
            string field)
        {
            if (value < 0 || value > 12_000)
            {
                throw new InvalidOperationException(
                    $"Invalid {field} for {recordId}: {value}.");
            }
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
