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
        public string BirthLocationId;
        public string FamilyId;
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
        public VillageOccupation VillageOccupation = VillageOccupation.Unknown;
        public int LaborCapacityBasisPoints = 10_000;
        public long NextIndependentEventDay = -1;
        public string NextIndependentEventReason;
        public LocalDutyKind LocalDuty = LocalDutyKind.None;
        public long LocalDutyUntilDay = -1;
        public bool AbilityProfileInitialized;
        public CharacterAptitudeState Aptitudes = new CharacterAptitudeState();
        public ProfessionalSkillState ProfessionalSkills =
            new ProfessionalSkillState();
        public List<SkillMasteryState> SkillMasteries =
            new List<SkillMasteryState>();
        public List<KnowledgeMasteryState> KnowledgeMasteries =
            new List<KnowledgeMasteryState>();
        public List<TechnologyMasteryState> TechnologyMasteries =
            new List<TechnologyMasteryState>();
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
        public string LocationId;
        public string VillageId;
        public long Grain;
        public long SeedGrain;
        public int FarmlandUnits;
        public int CultivatedLandUnits;
        public long PlantedSeedGrain;
        public int ToolConditionBasisPoints = 10_000;
        public int FoodSecurityBasisPoints = 10_000;
        public long TaxArrearsGrain;
        public int CorveeDaysThisYear;
        public long LastHarvestGrain;
        public long LastConsumptionGrain;
        public List<string> MemberIds = new List<string>();
    }

    [Serializable]
    public sealed class WorldState
    {
        public const int CurrentSchemaVersion = 14;

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
        public List<AttentionFocusState> AttentionFocuses =
            new List<AttentionFocusState>();
        public List<AttentionLedgerEntryState> AttentionLedgerEntries =
            new List<AttentionLedgerEntryState>();
        public List<OrganizationState> Organizations = new List<OrganizationState>();
        public List<PositionState> Positions = new List<PositionState>();
        public List<MembershipState> Memberships = new List<MembershipState>();
        public List<CountyGovernanceState> CountyGovernances =
            new List<CountyGovernanceState>();
        public List<CountyGentryHouseState> CountyGentryHouses =
            new List<CountyGentryHouseState>();
        public List<CountyHouseholdTaxState> CountyHouseholdTaxes =
            new List<CountyHouseholdTaxState>();
        public List<CountyFiscalLedgerEntryState> CountyFiscalLedgerEntries =
            new List<CountyFiscalLedgerEntryState>();
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
        public PopulationStorageState PopulationStorage =
            new PopulationStorageState();
        public List<EducationPlanState> EducationPlans =
            new List<EducationPlanState>();
        public List<LearningRecordState> LearningRecords =
            new List<LearningRecordState>();
        public bool MilitaryServiceInitialized;
        public List<MilitaryFormationState> MilitaryFormations =
            new List<MilitaryFormationState>();
        public List<MilitaryServiceState> MilitaryServices =
            new List<MilitaryServiceState>();
        public List<MilitaryOrderState> MilitaryOrders =
            new List<MilitaryOrderState>();
        public bool MilitaryEquipmentInitialized;
        public List<MilitaryEquipmentDefinitionState> MilitaryEquipmentDefinitions =
            new List<MilitaryEquipmentDefinitionState>();
        public List<MilitaryArmoryStockState> MilitaryArmoryStocks =
            new List<MilitaryArmoryStockState>();
        public List<MilitaryEquipmentIssueState> MilitaryEquipmentIssues =
            new List<MilitaryEquipmentIssueState>();
        public List<MilitaryEquipmentTransactionState> MilitaryEquipmentTransactions =
            new List<MilitaryEquipmentTransactionState>();
        public List<VillageState> Villages = new List<VillageState>();
        public List<VillageFacilityState> VillageFacilities =
            new List<VillageFacilityState>();
        public List<VillageLedgerEntryState> VillageLedgerEntries =
            new List<VillageLedgerEntryState>();
        public List<AgricultureWorkOrderState> AgricultureWorkOrders =
            new List<AgricultureWorkOrderState>();
        public List<ProductionLedgerEntryState> ProductionLedgerEntries =
            new List<ProductionLedgerEntryState>();
        public List<ProductBatchState> ProductBatches =
            new List<ProductBatchState>();
        public List<InventoryContainerState> InventoryContainers =
            new List<InventoryContainerState>();
        public List<InventoryTransactionState> InventoryTransactions =
            new List<InventoryTransactionState>();
        public List<ProcessingWorkOrderState> ProcessingWorkOrders =
            new List<ProcessingWorkOrderState>();
        public List<ResearchProjectState> ResearchProjects =
            new List<ResearchProjectState>();
        public List<TechnologyApplicationState> TechnologyApplications =
            new List<TechnologyApplicationState>();
        public List<ResearchLedgerEntryState> ResearchLedgerEntries =
            new List<ResearchLedgerEntryState>();
        public List<MilitaryProcurementOrderState> MilitaryProcurementOrders =
            new List<MilitaryProcurementOrderState>();
        public List<MilitaryProcurementLedgerEntryState>
            MilitaryProcurementLedgerEntries =
                new List<MilitaryProcurementLedgerEntryState>();
        public ProductionContentManifestState ProductionContentManifest;

        public WorldTime Time => new WorldTime(AbsoluteDay, (DaySegment)Segment);

        public static WorldState Create(ulong masterSeed)
        {
            return new WorldState
            {
                MasterSeed = masterSeed,
                AbsoluteDay = 0,
                Segment = (byte)DaySegment.Dawn,
                Revision = 0,
                ProductionContentManifest =
                    ProductionContentRegistry.CreateCore().CreateManifest()
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
            if (PopulationStorage == null)
            {
                throw new InvalidOperationException(
                    "Population storage metadata cannot be null.");
            }

            PopulationStorage.Validate(People.Count);
            ValidateUniqueIds(People, person => person.Id, "person");
            ValidateUniqueIds(Locations, location => location.Id, "location");
            ValidateUniqueIds(Families, family => family.Id, "family");
            ValidateUniqueIds(Routes, route => route.Id, "route");
            ValidateUniqueIds(Journeys, journey => journey.Id, "journey");
            ValidateUniqueIds(Relationships, relationship => relationship.Id, "relationship");
            ValidateUniqueIds(
                AttentionFocuses, attention => attention.Id, "attention focus");
            ValidateUniqueIds(
                AttentionLedgerEntries,
                entry => entry.Id,
                "attention ledger entry");
            ValidateUniqueIds(Organizations, organization => organization.Id, "organization");
            ValidateUniqueIds(Positions, position => position.Id, "position");
            ValidateUniqueIds(Memberships, membership => membership.Id, "membership");
            ValidateUniqueIds(
                CountyGovernances, item => item.Id, "county governance");
            ValidateUniqueIds(
                CountyGentryHouses, item => item.Id, "county gentry house");
            ValidateUniqueIds(
                CountyHouseholdTaxes, item => item.Id, "county household tax");
            ValidateUniqueIds(
                CountyFiscalLedgerEntries,
                item => item.Id,
                "county fiscal ledger entry");
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
            ValidateUniqueIds(
                MilitaryFormations, item => item.Id, "military formation");
            ValidateUniqueIds(
                MilitaryServices, item => item.Id, "military service");
            ValidateUniqueIds(
                MilitaryOrders, item => item.Id, "military order");
            ValidateUniqueIds(
                MilitaryEquipmentDefinitions,
                item => item.Id,
                "military equipment definition");
            ValidateUniqueIds(
                MilitaryArmoryStocks,
                item => item.Id,
                "military armory stock");
            ValidateUniqueIds(
                MilitaryEquipmentIssues,
                item => item.Id,
                "military equipment issue");
            ValidateUniqueIds(
                MilitaryEquipmentTransactions,
                item => item.Id,
                "military equipment transaction");
            ValidateUniqueIds(Villages, item => item.Id, "village");
            ValidateUniqueIds(
                VillageFacilities, item => item.Id, "village facility");
            ValidateUniqueIds(
                VillageLedgerEntries, item => item.Id, "village ledger entry");
            ValidateUniqueIds(
                AgricultureWorkOrders, item => item.Id, "agriculture work order");
            ValidateUniqueIds(
                ProductionLedgerEntries, item => item.Id, "production ledger entry");
            ValidateUniqueIds(ProductBatches, item => item.Id, "product batch");
            ValidateUniqueIds(
                InventoryContainers, item => item.Id, "inventory container");
            ValidateUniqueIds(
                InventoryTransactions, item => item.Id, "inventory transaction");
            ValidateUniqueIds(
                ProcessingWorkOrders, item => item.Id, "processing work order");
            ValidateUniqueIds(
                ResearchProjects, item => item.Id, "research project");
            ValidateUniqueIds(
                TechnologyApplications, item => item.Id, "technology application");
            ValidateUniqueIds(
                ResearchLedgerEntries, item => item.Id, "research ledger entry");
            ValidateUniqueIds(
                MilitaryProcurementOrders,
                item => item.Id,
                "military procurement order");
            ValidateUniqueIds(
                MilitaryProcurementLedgerEntries,
                item => item.Id,
                "military procurement ledger entry");

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

                if (person.LaborCapacityBasisPoints < 0 ||
                    person.LaborCapacityBasisPoints > 10_000 ||
                    !Enum.IsDefined(
                        typeof(VillageOccupation), person.VillageOccupation) ||
                    !Enum.IsDefined(typeof(LocalDutyKind), person.LocalDuty) ||
                    person.NextIndependentEventDay < -1 ||
                    person.LocalDutyUntilDay < -1)
                {
                    throw new InvalidOperationException(
                        $"Invalid village profile for {person.Id}.");
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
                ValidatePersonProgression(person);
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

                if (!string.IsNullOrEmpty(person.BirthLocationId) &&
                    !locationIds.Contains(person.BirthLocationId))
                {
                    throw new InvalidOperationException(
                        $"Person {person.Id} has an invalid birth location.");
                }
            }

            ValidatePopulationLedger(personIds, locationIds);

            var assignedFamilyByPerson =
                new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < Families.Count; i++)
            {
                var family = Families[i] ?? throw new InvalidOperationException("A family cannot be null.");
                _ = new StableId(family.Id);
                if (!personIds.Contains(family.HeadPersonId))
                {
                    throw new InvalidOperationException(
                        $"Family {family.Id} references missing head {family.HeadPersonId}.");
                }

                if (!family.MemberIds.Contains(family.HeadPersonId))
                {
                    throw new InvalidOperationException(
                        $"Family {family.Id} does not contain its head.");
                }

                if (family.Wealth < 0 || family.Debt < 0 ||
                    family.Grain < 0 || family.SeedGrain < 0 ||
                    family.FarmlandUnits < 0 ||
                    family.CultivatedLandUnits < 0 ||
                    family.CultivatedLandUnits > family.FarmlandUnits ||
                    family.PlantedSeedGrain < 0 ||
                    family.ToolConditionBasisPoints < 0 ||
                    family.ToolConditionBasisPoints > 10_000 ||
                    family.FoodSecurityBasisPoints < 0 ||
                    family.FoodSecurityBasisPoints > 10_000 ||
                    family.TaxArrearsGrain < 0 ||
                    family.CorveeDaysThisYear < 0 ||
                    family.LastHarvestGrain < 0 ||
                    family.LastConsumptionGrain < 0)
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

                    var memberId = family.MemberIds[memberIndex];
                    if (assignedFamilyByPerson.ContainsKey(memberId))
                    {
                        throw new InvalidOperationException(
                            $"Person {memberId} belongs to multiple families.");
                    }

                    assignedFamilyByPerson.Add(memberId, family.Id);
                }

                if (!string.IsNullOrEmpty(family.LocationId) &&
                    !locationIds.Contains(family.LocationId))
                {
                    throw new InvalidOperationException(
                        $"Family {family.Id} references a missing location.");
                }
            }

            for (var i = 0; i < People.Count; i++)
            {
                var person = People[i];
                var isAssigned = assignedFamilyByPerson.TryGetValue(
                    person.Id, out var assignedFamilyId);
                if (string.IsNullOrEmpty(person.FamilyId))
                {
                    if (isAssigned)
                    {
                        for (var familyIndex = 0;
                             familyIndex < Families.Count;
                             familyIndex++)
                        {
                            if (Families[familyIndex].Id == assignedFamilyId &&
                                !string.IsNullOrEmpty(
                                    Families[familyIndex].VillageId))
                            {
                                throw new InvalidOperationException(
                                    $"Village person {person.Id} lacks a family reference.");
                            }
                        }
                    }

                    continue;
                }

                if (!isAssigned || assignedFamilyId != person.FamilyId)
                {
                    throw new InvalidOperationException(
                        $"Person {person.Id} has an inconsistent family reference.");
                }
            }

            ValidateVillages(personIds, locationIds);
            ValidateInventoryProduction(personIds, locationIds);
            ValidateProduction(personIds);
            ValidateResearch(personIds);

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
                if (!locationIds.Contains(organization.HeadquartersLocationId) ||
                    organization.Treasury < 0)
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

            ValidateCountyGovernance(locationIds, organizationIds);

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
                    battle.AttackerEquipmentReadinessBasisPoints < 0 ||
                    battle.AttackerEquipmentReadinessBasisPoints > 10_000 ||
                    battle.DefenderEquipmentReadinessBasisPoints < 0 ||
                    battle.DefenderEquipmentReadinessBasisPoints > 10_000 ||
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
            ValidateMilitaryService(personIds, locationIds, armyIds);
            ValidateMilitaryEquipment(personIds, armyIds);
            ValidateMilitaryProcurement(
                personIds, locationIds, organizationIds, armyIds, routeIds);
            ValidateAttention(personIds);
        }

        private void ValidateMilitaryProcurement(
            HashSet<string> personIds,
            HashSet<string> locationIds,
            HashSet<string> organizationIds,
            HashSet<string> armyIds,
            HashSet<string> routeIds)
        {
            var batches = new Dictionary<string, ProductBatchState>(
                StringComparer.Ordinal);
            var containers = new Dictionary<string, InventoryContainerState>(
                StringComparer.Ordinal);
            var equipment = new Dictionary<string,
                MilitaryEquipmentDefinitionState>(StringComparer.Ordinal);
            var journeys = new Dictionary<string, JourneyState>(
                StringComparer.Ordinal);
            for (var i = 0; i < ProductBatches.Count; i++)
            {
                batches.Add(ProductBatches[i].Id, ProductBatches[i]);
            }

            for (var i = 0; i < InventoryContainers.Count; i++)
            {
                containers.Add(InventoryContainers[i].Id, InventoryContainers[i]);
            }

            for (var i = 0; i < MilitaryEquipmentDefinitions.Count; i++)
            {
                equipment.Add(
                    MilitaryEquipmentDefinitions[i].Id,
                    MilitaryEquipmentDefinitions[i]);
            }

            for (var i = 0; i < Journeys.Count; i++)
            {
                journeys.Add(Journeys[i].Id, Journeys[i]);
            }

            var orders = new Dictionary<string, MilitaryProcurementOrderState>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryProcurementOrders.Count; i++)
            {
                var order = MilitaryProcurementOrders[i] ??
                    throw new InvalidOperationException(
                        "A military procurement order cannot be null.");
                orders.Add(order.Id, order);
                var hasJourney = journeys.TryGetValue(
                    order.JourneyId, out var journey);
                var validStatus =
                    order.Status == MilitaryProcurementStatus.InTransit &&
                    order.DeliveredDay == -1 && hasJourney &&
                    journey.PersonId == order.CarrierPersonId &&
                    journey.RouteId == order.RouteId &&
                    journey.OriginLocationId == order.OriginLocationId &&
                    journey.DestinationLocationId == order.DestinationLocationId ||
                    order.Status == MilitaryProcurementStatus.AwaitingArmy &&
                    order.DeliveredDay == -1 && !hasJourney ||
                    order.Status == MilitaryProcurementStatus.Delivered &&
                    order.DeliveredDay >= order.CreatedDay &&
                    order.DeliveredDay <= AbsoluteDay && !hasJourney;
                var hasEquipment = equipment.TryGetValue(
                    order.EquipmentDefinitionId, out var definition);
                var hasBatch = batches.TryGetValue(
                    order.SourceBatchId, out var batch);
                var hasContainer = containers.TryGetValue(
                    order.InventoryContainerId, out var container);
                if (!Enum.IsDefined(
                        typeof(MilitaryProcurementStatus), order.Status) ||
                    !personIds.Contains(order.IssuerPersonId) ||
                    !personIds.Contains(order.CarrierPersonId) ||
                    !organizationIds.Contains(order.BuyerOrganizationId) ||
                    !organizationIds.Contains(order.SupplierOrganizationId) ||
                    order.BuyerOrganizationId == order.SupplierOrganizationId ||
                    !armyIds.Contains(order.TargetArmyId) ||
                    FindArmy(Armies, order.TargetArmyId).OrganizationId !=
                        order.BuyerOrganizationId ||
                    !hasEquipment ||
                    definition.ProductDefinitionId != order.ProductDefinitionId ||
                    !hasBatch ||
                    batch.ProductDefinitionId != order.ProductDefinitionId ||
                    batch.OwnerOrganizationId != order.SupplierOrganizationId ||
                    !hasContainer ||
                    batch.InventoryContainerId != container.Id ||
                    container.OwnerOrganizationId != order.SupplierOrganizationId ||
                    container.CarrierPersonId != order.CarrierPersonId ||
                    !routeIds.Contains(order.RouteId) ||
                    !locationIds.Contains(order.OriginLocationId) ||
                    !locationIds.Contains(order.DestinationLocationId) ||
                    order.CreatedDay < 0 || order.CreatedDay > AbsoluteDay ||
                    order.Quantity <= 0 || order.UnitPrice <= 0 ||
                    order.TotalPaid != checked(order.UnitPrice * order.Quantity) ||
                    !validStatus)
                {
                    throw new InvalidOperationException(
                        $"Invalid military procurement order {order.Id}: " +
                        $"status={validStatus}, journey={hasJourney}, " +
                        $"people={personIds.Contains(order.IssuerPersonId)}/" +
                        $"{personIds.Contains(order.CarrierPersonId)}, " +
                        $"organizations=" +
                        $"{organizationIds.Contains(order.BuyerOrganizationId)}/" +
                        $"{organizationIds.Contains(order.SupplierOrganizationId)}, " +
                        $"army={armyIds.Contains(order.TargetArmyId)}, " +
                        $"equipment={hasEquipment}, batch={hasBatch}, " +
                        $"container={hasContainer}, " +
                        $"route={routeIds.Contains(order.RouteId)}, " +
                        $"quantity={order.Quantity}, price={order.UnitPrice}, " +
                        $"paid={order.TotalPaid}.");
                }
            }

            var dispatchCount = new Dictionary<string, int>(StringComparer.Ordinal);
            var receiptCount = new Dictionary<string, int>(StringComparer.Ordinal);
            var inventoryDispatchCount = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var equipmentReceiptCount = new Dictionary<string, int>(
                StringComparer.Ordinal);
            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                var transaction = InventoryTransactions[i];
                if (string.IsNullOrEmpty(
                        transaction.SourceMilitaryProcurementId))
                {
                    continue;
                }

                var order = orders[transaction.SourceMilitaryProcurementId];
                if (transaction.Type !=
                        InventoryTransactionType.MilitaryProcurementDispatched ||
                    transaction.Day != order.CreatedDay ||
                    transaction.Lines.Count != 1 ||
                    transaction.Lines[0].BatchId != order.SourceBatchId ||
                    transaction.Lines[0].ProductDefinitionId !=
                        order.ProductDefinitionId ||
                    transaction.Lines[0].QuantityDelta != -order.Quantity ||
                    transaction.Lines[0].ReservedQuantityDelta != 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid procurement inventory dispatch {transaction.Id}.");
                }

                AddEquipmentCount(
                    inventoryDispatchCount, order.Id, 1);
            }

            for (var i = 0; i < MilitaryEquipmentTransactions.Count; i++)
            {
                var transaction = MilitaryEquipmentTransactions[i];
                if (string.IsNullOrEmpty(
                        transaction.SourceProcurementOrderId))
                {
                    continue;
                }

                var order = orders[transaction.SourceProcurementOrderId];
                if (transaction.Type !=
                        MilitaryEquipmentTransactionType.ProcurementReceipt ||
                    transaction.Day != order.DeliveredDay ||
                    transaction.EquipmentDefinitionId !=
                        order.EquipmentDefinitionId ||
                    transaction.ToArmyId != order.TargetArmyId ||
                    transaction.Quantity != order.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Invalid procurement armory receipt {transaction.Id}.");
                }

                AddEquipmentCount(
                    equipmentReceiptCount, order.Id, 1);
            }

            for (var i = 0; i < MilitaryProcurementLedgerEntries.Count; i++)
            {
                var entry = MilitaryProcurementLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A military procurement ledger entry cannot be null.");
                if (!orders.TryGetValue(
                        entry.ProcurementOrderId, out var order) ||
                    entry.Day < order.CreatedDay || entry.Day > AbsoluteDay ||
                    entry.BuyerOrganizationId != order.BuyerOrganizationId ||
                    entry.SupplierOrganizationId != order.SupplierOrganizationId ||
                    !Enum.IsDefined(
                        typeof(MilitaryProcurementLedgerType), entry.Type) ||
                    string.IsNullOrWhiteSpace(entry.Summary))
                {
                    throw new InvalidOperationException(
                        $"Invalid military procurement ledger entry {entry.Id}.");
                }

                if (entry.Type == MilitaryProcurementLedgerType.DispatchPayment)
                {
                    if (entry.Day != order.CreatedDay ||
                        entry.BuyerMoneyDelta != -order.TotalPaid ||
                        entry.SupplierMoneyDelta != order.TotalPaid ||
                        entry.ArmoryQuantityDelta != 0)
                    {
                        throw new InvalidOperationException(
                            $"Unbalanced procurement payment {entry.Id}.");
                    }

                    AddEquipmentCount(dispatchCount, order.Id, 1);
                }
                else
                {
                    if (order.Status != MilitaryProcurementStatus.Delivered ||
                        entry.Day != order.DeliveredDay ||
                        entry.BuyerMoneyDelta != 0 ||
                        entry.SupplierMoneyDelta != 0 ||
                        entry.ArmoryQuantityDelta != order.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Invalid procurement receipt {entry.Id}.");
                    }

                    AddEquipmentCount(receiptCount, order.Id, 1);
                }
            }

            foreach (var pair in orders)
            {
                dispatchCount.TryGetValue(pair.Key, out var dispatches);
                receiptCount.TryGetValue(pair.Key, out var receipts);
                inventoryDispatchCount.TryGetValue(
                    pair.Key, out var inventoryDispatches);
                equipmentReceiptCount.TryGetValue(
                    pair.Key, out var equipmentReceipts);
                var delivered = pair.Value.Status ==
                    MilitaryProcurementStatus.Delivered;
                if (dispatches != 1 || inventoryDispatches != 1 ||
                    (pair.Value.Status == MilitaryProcurementStatus.Delivered
                        ? receipts != 1
                        : receipts != 0) ||
                    (delivered ? equipmentReceipts != 1 : equipmentReceipts != 0))
                {
                    throw new InvalidOperationException(
                        $"Incomplete procurement ledger for {pair.Key}.");
                }
            }
        }

        private void ValidateAttention(HashSet<string> personIds)
        {
            var focusKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < AttentionFocuses.Count; i++)
            {
                var focus = AttentionFocuses[i];
                _ = new StableId(focus.ReasonId);
                _ = new StableId(focus.TargetId);
                if (!personIds.Contains(focus.ObserverPersonId) ||
                    !Enum.IsDefined(typeof(AttentionTargetKind), focus.TargetKind) ||
                    !Enum.IsDefined(typeof(AttentionLevel), focus.Level) ||
                    focus.Level == AttentionLevel.None ||
                    focus.CreatedDay < 0 ||
                    focus.LastChangedDay < focus.CreatedDay ||
                    focus.LastChangedDay > AbsoluteDay ||
                    !AttentionTargetExists(focus.TargetKind, focus.TargetId))
                {
                    throw new InvalidOperationException(
                        $"Invalid attention focus {focus.Id}.");
                }

                var key = focus.ObserverPersonId + "|" +
                          (byte)focus.TargetKind + "|" + focus.TargetId + "|" +
                          focus.ReasonId;
                if (!focusKeys.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate attention reason for {key}.");
                }
            }

            for (var i = 0; i < AttentionLedgerEntries.Count; i++)
            {
                var entry = AttentionLedgerEntries[i];
                _ = new StableId(entry.ReasonId);
                _ = new StableId(entry.TargetId);
                if (entry.Day < 0 || entry.Day > AbsoluteDay ||
                    !personIds.Contains(entry.ObserverPersonId) ||
                    !Enum.IsDefined(typeof(AttentionTargetKind), entry.TargetKind) ||
                    !Enum.IsDefined(
                        typeof(AttentionLedgerChangeKind), entry.ChangeKind) ||
                    !Enum.IsDefined(typeof(AttentionLevel), entry.PreviousLevel) ||
                    !Enum.IsDefined(typeof(AttentionLevel), entry.NewLevel) ||
                    !AttentionTargetExists(entry.TargetKind, entry.TargetId) ||
                    !ValidAttentionTransition(entry))
                {
                    throw new InvalidOperationException(
                        $"Invalid attention ledger entry {entry.Id}.");
                }
            }
        }

        private bool AttentionTargetExists(
            AttentionTargetKind kind,
            string targetId)
        {
            switch (kind)
            {
                case AttentionTargetKind.Person:
                    return ContainsId(People, item => item.Id, targetId);
                case AttentionTargetKind.Family:
                    return ContainsId(Families, item => item.Id, targetId);
                case AttentionTargetKind.Village:
                    return ContainsId(Villages, item => item.Id, targetId);
                case AttentionTargetKind.Facility:
                    return ContainsId(
                        VillageFacilities, item => item.Id, targetId);
                case AttentionTargetKind.Organization:
                    return ContainsId(
                        Organizations, item => item.Id, targetId);
                default:
                    return false;
            }
        }

        private static bool ValidAttentionTransition(
            AttentionLedgerEntryState entry)
        {
            switch (entry.ChangeKind)
            {
                case AttentionLedgerChangeKind.Added:
                    return entry.PreviousLevel == AttentionLevel.None &&
                           entry.NewLevel != AttentionLevel.None;
                case AttentionLedgerChangeKind.Updated:
                    return entry.PreviousLevel != AttentionLevel.None &&
                           entry.NewLevel != AttentionLevel.None &&
                           entry.PreviousLevel != entry.NewLevel;
                case AttentionLedgerChangeKind.Removed:
                    return entry.PreviousLevel != AttentionLevel.None &&
                           entry.NewLevel == AttentionLevel.None;
                default:
                    return false;
            }
        }

        private static bool ContainsId<T>(
            IList<T> items,
            Func<T, string> selectId,
            string id)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (string.Equals(selectId(items[i]), id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void ValidateMilitaryService(
            HashSet<string> personIds,
            HashSet<string> locationIds,
            HashSet<string> armyIds)
        {
            if (!MilitaryServiceInitialized)
            {
                if (MilitaryFormations.Count != 0 ||
                    MilitaryServices.Count != 0 ||
                    MilitaryOrders.Count != 0)
                {
                    throw new InvalidOperationException(
                        "Uninitialized military service must not contain records.");
                }

                return;
            }

            var formationIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryFormations.Count; i++)
            {
                var formation = MilitaryFormations[i];
                formationIds.Add(formation.Id);
                if (!armyIds.Contains(formation.ArmyId) ||
                    !personIds.Contains(formation.CommanderPersonId) ||
                    formation.AuthorizedStrength <= 0 ||
                    formation.DisplayOrder < 0 ||
                    !Enum.IsDefined(typeof(MilitaryFormationKind), formation.Kind))
                {
                    throw new InvalidOperationException(
                        $"Invalid military formation {formation.Id}.");
                }
            }

            for (var i = 0; i < MilitaryFormations.Count; i++)
            {
                var formation = MilitaryFormations[i];
                if (string.IsNullOrEmpty(formation.ParentFormationId))
                {
                    if (formation.Kind != MilitaryFormationKind.Army)
                    {
                        throw new InvalidOperationException(
                            $"Formation {formation.Id} is not an army root.");
                    }
                }
                else
                {
                    var parent = FindMilitaryFormation(
                        MilitaryFormations, formation.ParentFormationId);
                    if (parent.ArmyId != formation.ArmyId ||
                        formation.ParentFormationId == formation.Id)
                    {
                        throw new InvalidOperationException(
                            $"Formation {formation.Id} has an invalid parent.");
                    }
                }
            }

            var servingPeople = new HashSet<string>(StringComparer.Ordinal);
            var activeByArmy = new Dictionary<string, int>(StringComparer.Ordinal);
            var woundedByArmy = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryServices.Count; i++)
            {
                var service = MilitaryServices[i];
                if (!servingPeople.Add(service.PersonId) ||
                    !personIds.Contains(service.PersonId) ||
                    !armyIds.Contains(service.ArmyId) ||
                    !formationIds.Contains(service.FormationId) ||
                    !Enum.IsDefined(typeof(MilitaryServiceRole), service.Role) ||
                    !Enum.IsDefined(typeof(MilitaryServiceStatus), service.Status) ||
                    service.Rank < 0 ||
                    service.EnlistedDay < 0 ||
                    service.LastStatusChangeDay < service.EnlistedDay ||
                    service.LastStatusChangeDay > AbsoluteDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid military service {service.Id}.");
                }

                ValidateBasisPoints(
                    service.DisciplineBasisPoints, service.Id, "discipline");
                ValidateBasisPoints(
                    service.LoyaltyBasisPoints, service.Id, "loyalty");
                ValidateBasisPoints(
                    service.ServiceExperienceBasisPoints,
                    service.Id,
                    "service experience");
                var formation = FindMilitaryFormation(
                    MilitaryFormations, service.FormationId);
                if (formation.ArmyId != service.ArmyId)
                {
                    throw new InvalidOperationException(
                        $"Military service {service.Id} is in another army's formation.");
                }

                var person = FindPerson(People, service.PersonId);
                var army = FindArmy(Armies, service.ArmyId);
                var available =
                    service.Status == MilitaryServiceStatus.Mustering ||
                    service.Status == MilitaryServiceStatus.Active ||
                    service.Status == MilitaryServiceStatus.Wounded;
                if (available && (!person.IsAlive || person.LocationId != army.LocationId) ||
                    service.Status == MilitaryServiceStatus.Dead && person.IsAlive)
                {
                    throw new InvalidOperationException(
                        $"Military service {service.Id} disagrees with its person.");
                }

                if (service.Status == MilitaryServiceStatus.Mustering ||
                    service.Status == MilitaryServiceStatus.Active)
                {
                    AddCount(activeByArmy, service.ArmyId);
                }
                else if (service.Status == MilitaryServiceStatus.Wounded)
                {
                    AddCount(woundedByArmy, service.ArmyId);
                }
            }

            for (var i = 0; i < Armies.Count; i++)
            {
                var army = Armies[i];
                var rootCount = 0;
                for (var formationIndex = 0;
                     formationIndex < MilitaryFormations.Count;
                     formationIndex++)
                {
                    var formation = MilitaryFormations[formationIndex];
                    if (formation.ArmyId == army.Id &&
                        string.IsNullOrEmpty(formation.ParentFormationId))
                    {
                        rootCount++;
                        if (formation.CommanderPersonId != army.CommanderPersonId)
                        {
                            throw new InvalidOperationException(
                                $"Army {army.Id} root commander does not match.");
                        }
                    }
                }

                activeByArmy.TryGetValue(army.Id, out var active);
                woundedByArmy.TryGetValue(army.Id, out var wounded);
                if (rootCount != 1 ||
                    active != army.Troops ||
                    wounded != army.WoundedTroops)
                {
                    throw new InvalidOperationException(
                        $"Army {army.Id} military service cache is inconsistent.");
                }
            }

            for (var i = 0; i < MilitaryFormations.Count; i++)
            {
                var formation = MilitaryFormations[i];
                MilitaryServiceState commanderService = null;
                for (var serviceIndex = 0;
                     serviceIndex < MilitaryServices.Count;
                     serviceIndex++)
                {
                    var service = MilitaryServices[serviceIndex];
                    if (service.PersonId == formation.CommanderPersonId &&
                        service.ArmyId == formation.ArmyId &&
                        service.FormationId == formation.Id)
                    {
                        commanderService = service;
                        break;
                    }
                }

                if (commanderService == null ||
                    formation.Kind == MilitaryFormationKind.Army &&
                    commanderService.Role != MilitaryServiceRole.Commander ||
                    formation.Kind != MilitaryFormationKind.Army &&
                    commanderService.Role != MilitaryServiceRole.Officer)
                {
                    throw new InvalidOperationException(
                        $"Formation {formation.Id} has no available commander.");
                }
            }

            for (var i = 0; i < MilitaryOrders.Count; i++)
            {
                var order = MilitaryOrders[i];
                MilitaryFormationState targetFormation = null;
                if (!string.IsNullOrEmpty(order.FormationId))
                {
                    targetFormation = FindMilitaryFormation(
                        MilitaryFormations, order.FormationId);
                }

                var shouldAuthorize =
                    order.ActualAuthority >= order.RequiredAuthority;
                if (!personIds.Contains(order.IssuerPersonId) ||
                    !armyIds.Contains(order.ArmyId) ||
                    targetFormation != null &&
                    targetFormation.ArmyId != order.ArmyId ||
                    !string.IsNullOrEmpty(order.TargetLocationId) &&
                    !locationIds.Contains(order.TargetLocationId) ||
                    !string.IsNullOrEmpty(order.TargetArmyId) &&
                    !armyIds.Contains(order.TargetArmyId) ||
                    order.Day < 0 ||
                    order.Day > AbsoluteDay ||
                    !Enum.IsDefined(typeof(MilitaryOrderType), order.Type) ||
                    !Enum.IsDefined(
                        typeof(MilitaryAuthorityLevel), order.RequiredAuthority) ||
                    !Enum.IsDefined(
                        typeof(MilitaryAuthorityLevel), order.ActualAuthority) ||
                    !Enum.IsDefined(typeof(MilitaryOrderResult), order.Result) ||
                    shouldAuthorize !=
                    (order.Result == MilitaryOrderResult.Authorized) ||
                    string.IsNullOrWhiteSpace(order.Summary))
                {
                    throw new InvalidOperationException(
                        $"Invalid military order {order.Id}.");
                }
            }
        }

        private sealed class EquipmentLedgerBalance
        {
            public int Opening;
            public int Available;
            public int Damaged;
            public int Issued;
        }

        private void ValidateMilitaryEquipment(
            HashSet<string> personIds,
            HashSet<string> armyIds)
        {
            if (!MilitaryEquipmentInitialized)
            {
                if (MilitaryEquipmentDefinitions.Count != 0 ||
                    MilitaryArmoryStocks.Count != 0 ||
                    MilitaryEquipmentIssues.Count != 0 ||
                    MilitaryEquipmentTransactions.Count != 0)
                {
                    throw new InvalidOperationException(
                        "Uninitialized military equipment must not contain records.");
                }

                return;
            }

            if (!MilitaryServiceInitialized)
            {
                throw new InvalidOperationException(
                    "Military equipment requires real military service.");
            }

            var definitions = new Dictionary<string,
                MilitaryEquipmentDefinitionState>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryEquipmentDefinitions.Count; i++)
            {
                var definition = MilitaryEquipmentDefinitions[i];
                _ = new StableId(definition.CategoryId);
                _ = new StableId(definition.SlotId);
                ValidateContentReference(
                    definition.ProductDefinitionId,
                    "military equipment product",
                    definition.Id);
                if (string.IsNullOrWhiteSpace(definition.DisplayName) ||
                    definition.UnitWeight <= 0 ||
                    definition.MaximumConditionBasisPoints <= 0 ||
                    definition.MaximumConditionBasisPoints > 10_000 ||
                    definition.MeleePowerBasisPoints < 0 ||
                    definition.MeleePowerBasisPoints > 10_000 ||
                    definition.RangedPowerBasisPoints < 0 ||
                    definition.RangedPowerBasisPoints > 10_000 ||
                    definition.ProtectionBasisPoints < 0 ||
                    definition.ProtectionBasisPoints > 10_000 ||
                    definition.RequiredStrengthBasisPoints < 0 ||
                    definition.RequiredStrengthBasisPoints > 10_000 ||
                    definition.RequiredDexterityBasisPoints < 0 ||
                    definition.RequiredDexterityBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        $"Invalid military equipment definition {definition.Id}.");
                }

                definitions.Add(definition.Id, definition);
            }

            for (var i = 0; i < MilitaryEquipmentDefinitions.Count; i++)
            {
                var compatible =
                    MilitaryEquipmentDefinitions[i].CompatibleEquipmentId;
                if (!string.IsNullOrEmpty(compatible) &&
                    !definitions.ContainsKey(compatible))
                {
                    throw new InvalidOperationException(
                        $"Equipment {MilitaryEquipmentDefinitions[i].Id} " +
                        "has missing compatibility equipment.");
                }
            }

            var stocks = new Dictionary<string, MilitaryArmoryStockState>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryArmoryStocks.Count; i++)
            {
                var stock = MilitaryArmoryStocks[i];
                var key = EquipmentLedgerKey(
                    stock.ArmyId, stock.EquipmentDefinitionId);
                if (!armyIds.Contains(stock.ArmyId) ||
                    !definitions.ContainsKey(stock.EquipmentDefinitionId) ||
                    stocks.ContainsKey(key) ||
                    stock.AvailableQuantity < 0 ||
                    stock.DamagedQuantity < 0 ||
                    stock.OpeningQuantity < 0 ||
                    stock.AverageConditionBasisPoints < 0 ||
                    stock.AverageConditionBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        $"Invalid military armory stock {stock.Id}.");
                }

                stocks.Add(key, stock);
            }

            var services = new Dictionary<string, MilitaryServiceState>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryServices.Count; i++)
            {
                services.Add(MilitaryServices[i].Id, MilitaryServices[i]);
            }

            var issueSlots = new HashSet<string>(StringComparer.Ordinal);
            var issuedByLedger = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var definitionsByService = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryEquipmentIssues.Count; i++)
            {
                var issue = MilitaryEquipmentIssues[i];
                if (!services.TryGetValue(
                        issue.MilitaryServiceId, out var service) ||
                    !personIds.Contains(issue.PersonId) ||
                    !armyIds.Contains(issue.ArmyId) ||
                    !definitions.TryGetValue(
                        issue.EquipmentDefinitionId, out var definition) ||
                    service.PersonId != issue.PersonId ||
                    service.ArmyId != issue.ArmyId ||
                    definition.SlotId != issue.SlotId ||
                    !issueSlots.Add(
                        issue.MilitaryServiceId + "|" + issue.SlotId) ||
                    issue.Quantity <= 0 ||
                    definition.IsUnique && issue.Quantity != 1 ||
                    issue.ConditionBasisPoints < 5_000 ||
                    issue.ConditionBasisPoints >
                    definition.MaximumConditionBasisPoints ||
                    issue.IssuedDay < 0 ||
                    issue.LastChangedDay < issue.IssuedDay ||
                    issue.LastChangedDay > AbsoluteDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid military equipment issue {issue.Id}.");
                }

                var ledgerKey = EquipmentLedgerKey(
                    issue.ArmyId, issue.EquipmentDefinitionId);
                AddEquipmentCount(issuedByLedger, ledgerKey, issue.Quantity);
                definitionsByService.Add(
                    issue.MilitaryServiceId + "|" +
                    issue.EquipmentDefinitionId);
            }

            for (var i = 0; i < MilitaryEquipmentIssues.Count; i++)
            {
                var issue = MilitaryEquipmentIssues[i];
                var compatible =
                    definitions[issue.EquipmentDefinitionId]
                        .CompatibleEquipmentId;
                if (!string.IsNullOrEmpty(compatible) &&
                    !definitionsByService.Contains(
                        issue.MilitaryServiceId + "|" + compatible))
                {
                    throw new InvalidOperationException(
                        $"Equipment issue {issue.Id} lacks its compatible item.");
                }
            }

            var balances = new Dictionary<string, EquipmentLedgerBalance>(
                StringComparer.Ordinal);
            var procurementIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryProcurementOrders.Count; i++)
            {
                procurementIds.Add(MilitaryProcurementOrders[i].Id);
            }
            long previousDay = -1;
            for (var i = 0; i < MilitaryEquipmentTransactions.Count; i++)
            {
                var transaction = MilitaryEquipmentTransactions[i];
                if (!definitions.ContainsKey(
                        transaction.EquipmentDefinitionId) ||
                    transaction.Quantity <= 0 ||
                    transaction.Day < previousDay ||
                    transaction.Day > AbsoluteDay ||
                    !Enum.IsDefined(
                        typeof(MilitaryEquipmentTransactionType),
                        transaction.Type) ||
                    !string.IsNullOrEmpty(transaction.FromArmyId) &&
                    !armyIds.Contains(transaction.FromArmyId) ||
                    !string.IsNullOrEmpty(transaction.ToArmyId) &&
                    !armyIds.Contains(transaction.ToArmyId) ||
                    !string.IsNullOrEmpty(transaction.MilitaryServiceId) &&
                    !services.ContainsKey(transaction.MilitaryServiceId) ||
                    !string.IsNullOrEmpty(transaction.BattleId) &&
                    !ContainsId(Battles, item => item.Id, transaction.BattleId) ||
                    !string.IsNullOrEmpty(transaction.SourceProcurementOrderId) &&
                    !procurementIds.Contains(
                        transaction.SourceProcurementOrderId) ||
                    (transaction.Type ==
                         MilitaryEquipmentTransactionType.ProcurementReceipt) !=
                    !string.IsNullOrEmpty(
                        transaction.SourceProcurementOrderId) ||
                    string.IsNullOrWhiteSpace(transaction.Summary))
                {
                    throw new InvalidOperationException(
                        $"Invalid military equipment transaction {transaction.Id}.");
                }

                previousDay = transaction.Day;
                ApplyEquipmentTransaction(
                    transaction, services, balances);
            }

            foreach (var pair in stocks)
            {
                var balance = GetEquipmentBalance(balances, pair.Key);
                issuedByLedger.TryGetValue(pair.Key, out var issued);
                if (balance.Opening != pair.Value.OpeningQuantity ||
                    balance.Available != pair.Value.AvailableQuantity ||
                    balance.Damaged != pair.Value.DamagedQuantity ||
                    balance.Issued != issued ||
                    balance.Available < 0 ||
                    balance.Damaged < 0 ||
                    balance.Issued < 0)
                {
                    throw new InvalidOperationException(
                        $"Military equipment ledger mismatch at {pair.Key}.");
                }
            }

            foreach (var pair in balances)
            {
                if (!stocks.ContainsKey(pair.Key))
                {
                    throw new InvalidOperationException(
                        $"Military equipment ledger has no stock {pair.Key}.");
                }
            }
        }

        private static void ApplyEquipmentTransaction(
            MilitaryEquipmentTransactionState transaction,
            Dictionary<string, MilitaryServiceState> services,
            Dictionary<string, EquipmentLedgerBalance> balances)
        {
            EquipmentLedgerBalance from = null;
            EquipmentLedgerBalance to = null;
            if (!string.IsNullOrEmpty(transaction.FromArmyId))
            {
                from = GetEquipmentBalance(
                    balances,
                    EquipmentLedgerKey(
                        transaction.FromArmyId,
                        transaction.EquipmentDefinitionId));
            }

            if (!string.IsNullOrEmpty(transaction.ToArmyId))
            {
                to = GetEquipmentBalance(
                    balances,
                    EquipmentLedgerKey(
                        transaction.ToArmyId,
                        transaction.EquipmentDefinitionId));
            }

            var serviceArmyId = string.IsNullOrEmpty(
                transaction.MilitaryServiceId)
                ? string.Empty
                : services[transaction.MilitaryServiceId].ArmyId;
            var quantity = transaction.Quantity;
            switch (transaction.Type)
            {
                case MilitaryEquipmentTransactionType.OpeningStock:
                    RequireEquipmentTransaction(
                        transaction, from == null && to != null &&
                        string.IsNullOrEmpty(transaction.MilitaryServiceId));
                    to.Opening += quantity;
                    to.Available += quantity;
                    break;
                case MilitaryEquipmentTransactionType.Issue:
                    RequireEquipmentTransaction(
                        transaction, from != null && to == null &&
                        transaction.FromArmyId == serviceArmyId);
                    from.Available -= quantity;
                    from.Issued += quantity;
                    break;
                case MilitaryEquipmentTransactionType.Return:
                    RequireEquipmentTransaction(
                        transaction, from == null && to != null &&
                        transaction.ToArmyId == serviceArmyId);
                    to.Issued -= quantity;
                    to.Available += quantity;
                    break;
                case MilitaryEquipmentTransactionType.Damage:
                    RequireEquipmentTransaction(
                        transaction, from != null && to == null &&
                        transaction.FromArmyId == serviceArmyId);
                    from.Issued -= quantity;
                    from.Damaged += quantity;
                    break;
                case MilitaryEquipmentTransactionType.Repair:
                    RequireEquipmentTransaction(
                        transaction, from != null && to != null &&
                        transaction.FromArmyId == transaction.ToArmyId &&
                        string.IsNullOrEmpty(transaction.MilitaryServiceId));
                    from.Damaged -= quantity;
                    to.Available += quantity;
                    break;
                case MilitaryEquipmentTransactionType.Loss:
                    RequireEquipmentTransaction(
                        transaction, from != null && to == null &&
                        transaction.FromArmyId == serviceArmyId);
                    from.Issued -= quantity;
                    break;
                case MilitaryEquipmentTransactionType.Capture:
                    RequireEquipmentTransaction(
                        transaction, from != null && to != null &&
                        transaction.FromArmyId == serviceArmyId &&
                        transaction.FromArmyId != transaction.ToArmyId &&
                        !string.IsNullOrEmpty(transaction.BattleId));
                    from.Issued -= quantity;
                    to.Available += quantity;
                    break;
                case MilitaryEquipmentTransactionType.Transfer:
                    RequireEquipmentTransaction(
                        transaction, from != null && to != null &&
                        transaction.FromArmyId != transaction.ToArmyId &&
                        string.IsNullOrEmpty(transaction.MilitaryServiceId));
                    from.Available -= quantity;
                    to.Available += quantity;
                    break;
                case MilitaryEquipmentTransactionType.ProcurementReceipt:
                    RequireEquipmentTransaction(
                        transaction, from == null && to != null &&
                        string.IsNullOrEmpty(transaction.MilitaryServiceId) &&
                        string.IsNullOrEmpty(transaction.BattleId));
                    to.Available += quantity;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported equipment transaction {transaction.Id}.");
            }

            if (from != null &&
                (from.Available < 0 || from.Damaged < 0 || from.Issued < 0) ||
                to != null &&
                (to.Available < 0 || to.Damaged < 0 || to.Issued < 0))
            {
                throw new InvalidOperationException(
                    $"Equipment transaction {transaction.Id} overdraws assets.");
            }
        }

        private static void RequireEquipmentTransaction(
            MilitaryEquipmentTransactionState transaction,
            bool valid)
        {
            if (!valid)
            {
                throw new InvalidOperationException(
                    $"Invalid equipment transaction shape {transaction.Id}.");
            }
        }

        private static EquipmentLedgerBalance GetEquipmentBalance(
            Dictionary<string, EquipmentLedgerBalance> balances,
            string key)
        {
            if (!balances.TryGetValue(key, out var result))
            {
                result = new EquipmentLedgerBalance();
                balances.Add(key, result);
            }

            return result;
        }

        private static string EquipmentLedgerKey(
            string armyId,
            string equipmentDefinitionId)
        {
            return armyId + "|" + equipmentDefinitionId;
        }

        private static void AddEquipmentCount(
            Dictionary<string, int> counts,
            string key,
            int quantity)
        {
            counts.TryGetValue(key, out var current);
            counts[key] = checked(current + quantity);
        }

        private static void AddCount(
            Dictionary<string, int> counts,
            string key)
        {
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
        }

        private static MilitaryFormationState FindMilitaryFormation(
            List<MilitaryFormationState> formations,
            string formationId)
        {
            for (var i = 0; i < formations.Count; i++)
            {
                if (formations[i].Id == formationId)
                {
                    return formations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military formation {formationId}.");
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

        private void ValidateVillages(
            HashSet<string> personIds,
            HashSet<string> locationIds)
        {
            var familyIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Families.Count; i++)
            {
                familyIds.Add(Families[i].Id);
            }

            var villageIds = new HashSet<string>(StringComparer.Ordinal);
            var assignedHouseholds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Villages.Count; i++)
            {
                var village = Villages[i] ??
                    throw new InvalidOperationException("A village cannot be null.");
                _ = new StableId(village.Id);
                villageIds.Add(village.Id);
                if (!locationIds.Contains(village.LocationId) ||
                    !string.IsNullOrEmpty(village.ParentLocationId) &&
                    !locationIds.Contains(village.ParentLocationId) ||
                    village.PublicGranaryGrain < 0 ||
                    village.TaxGrainCollected < 0 ||
                    village.CorveeDaysCompleted < 0 ||
                    village.LevyPersonDays < 0 ||
                    village.LivingResidentCount < 0 ||
                    village.WorkingResidentCount < 0 ||
                    village.HouseholdCount < 0 ||
                    village.FoodSecurityBasisPoints < 0 ||
                    village.FoodSecurityBasisPoints > 10_000 ||
                    village.LastSettlementDay < -1 ||
                    village.NextSettlementDay < 0 ||
                    village.LedgerOpeningFamilyGrain < 0 ||
                    village.LedgerOpeningPublicGrain < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid village {village.Id}.");
                }

                if (village.HouseholdCount != village.HouseholdIds.Count)
                {
                    throw new InvalidOperationException(
                        $"Village {village.Id} has a stale household cache.");
                }

                for (var householdIndex = 0;
                     householdIndex < village.HouseholdIds.Count;
                     householdIndex++)
                {
                    var familyId = village.HouseholdIds[householdIndex];
                    if (!familyIds.Contains(familyId) ||
                        !assignedHouseholds.Add(familyId))
                    {
                        throw new InvalidOperationException(
                            $"Village {village.Id} has an invalid household {familyId}.");
                    }

                    FamilyState family = null;
                    for (var familyIndex = 0;
                         familyIndex < Families.Count;
                         familyIndex++)
                    {
                        if (Families[familyIndex].Id == familyId)
                        {
                            family = Families[familyIndex];
                            break;
                        }
                    }

                    if (family == null ||
                        family.VillageId != village.Id ||
                        family.LocationId != village.LocationId)
                    {
                        throw new InvalidOperationException(
                            $"Village {village.Id} and household {familyId} disagree.");
                    }
                }
            }

            for (var i = 0; i < Families.Count; i++)
            {
                var family = Families[i];
                if (!string.IsNullOrEmpty(family.VillageId) &&
                    (!villageIds.Contains(family.VillageId) ||
                     !assignedHouseholds.Contains(family.Id)))
                {
                    throw new InvalidOperationException(
                        $"Family {family.Id} has an invalid village reference.");
                }
            }

            for (var i = 0; i < VillageFacilities.Count; i++)
            {
                var facility = VillageFacilities[i] ??
                    throw new InvalidOperationException(
                        "A village facility cannot be null.");
                if (!villageIds.Contains(facility.VillageId) ||
                    !Enum.IsDefined(
                        typeof(VillageFacilityKind), facility.Kind) ||
                    !string.IsNullOrEmpty(facility.OwnerFamilyId) &&
                    !familyIds.Contains(facility.OwnerFamilyId) ||
                    !string.IsNullOrEmpty(facility.ManagerPersonId) &&
                    !personIds.Contains(facility.ManagerPersonId) ||
                    facility.Capacity < 0 ||
                    facility.ConditionBasisPoints < 0 ||
                    facility.ConditionBasisPoints > 10_000 ||
                    facility.InventoryUnits < 0 ||
                    facility.Kind == VillageFacilityKind.HouseholdGranary &&
                    facility.InventoryUnits > facility.Capacity)
                {
                    throw new InvalidOperationException(
                        $"Invalid village facility {facility.Id}.");
                }
            }

            for (var i = 0; i < VillageLedgerEntries.Count; i++)
            {
                var entry = VillageLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A village ledger entry cannot be null.");
                if (entry.Day < 0 || entry.Day > AbsoluteDay ||
                    !villageIds.Contains(entry.VillageId) ||
                    !Enum.IsDefined(
                        typeof(VillageLedgerEntryType), entry.Type) ||
                    !string.IsNullOrEmpty(entry.FamilyId) &&
                    !familyIds.Contains(entry.FamilyId) ||
                    !string.IsNullOrEmpty(entry.PersonId) &&
                    !personIds.Contains(entry.PersonId) ||
                    entry.Quantity < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid village ledger entry {entry.Id}.");
                }

            }
        }

        private void ValidateCountyGovernance(
            HashSet<string> locationIds,
            HashSet<string> organizationIds)
        {
            var governanceIds = new HashSet<string>(StringComparer.Ordinal);
            var governedLocations = new HashSet<string>(StringComparer.Ordinal);
            var governmentOrganizations = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < CountyGovernances.Count; i++)
            {
                var governance = CountyGovernances[i] ??
                    throw new InvalidOperationException(
                        "A county governance cannot be null.");
                _ = new StableId(governance.Id);
                governanceIds.Add(governance.Id);
                if (!locationIds.Contains(governance.CountyLocationId) ||
                    !LocationHasKind(
                        governance.CountyLocationId,
                        LocationKind.CountySeat) ||
                    !organizationIds.Contains(
                        governance.GovernmentOrganizationId) ||
                    !FamilyExists(Families, governance.AdministratorFamilyId) ||
                    !FamilyBelongsToCounty(
                        governance.AdministratorFamilyId,
                        governance.CountyLocationId) ||
                    !governedLocations.Add(governance.CountyLocationId) ||
                    !governmentOrganizations.Add(
                        governance.GovernmentOrganizationId) ||
                    governance.AnnualCashTaxRateBasisPoints < 0 ||
                    governance.AnnualCashTaxRateBasisPoints > 10_000 ||
                    governance.LocalGrainRetentionBasisPoints < 0 ||
                    governance.LocalGrainRetentionBasisPoints > 10_000 ||
                    governance.RegistrationCoverageBasisPoints < 0 ||
                    governance.RegistrationCoverageBasisPoints > 10_000 ||
                    governance.AdministrativeEfficiencyBasisPoints < 0 ||
                    governance.AdministrativeEfficiencyBasisPoints > 10_000 ||
                    governance.GentryInfluenceBasisPoints < 0 ||
                    governance.GentryInfluenceBasisPoints > 10_000 ||
                    governance.LastMarketPressureBasisPoints < 0 ||
                    governance.LastMarketPressureBasisPoints > 20_000 ||
                    governance.CountyGranaryGrain < 0 ||
                    governance.TotalMoneyTaxCollected < 0 ||
                    governance.TotalGrainTaxReceived < 0 ||
                    governance.TotalAdministrationPaid < 0 ||
                    governance.TotalReliefGrain < 0 ||
                    governance.LastPublicOrderChange < -10_000 ||
                    governance.LastPublicOrderChange > 10_000 ||
                    governance.LastSettlementDay < -1 ||
                    governance.NextSettlementDay < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid county governance {governance.Id}.");
                }

                OrganizationState organization = null;
                for (var organizationIndex = 0;
                     organizationIndex < Organizations.Count;
                     organizationIndex++)
                {
                    if (Organizations[organizationIndex].Id ==
                        governance.GovernmentOrganizationId)
                    {
                        organization = Organizations[organizationIndex];
                        break;
                    }
                }

                if (organization == null ||
                    organization.Type != OrganizationType.Government ||
                    organization.HeadquartersLocationId !=
                    governance.CountyLocationId)
                {
                    throw new InvalidOperationException(
                        $"County governance {governance.Id} lacks government.");
                }
            }

            var gentryKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < CountyGentryHouses.Count; i++)
            {
                var gentry = CountyGentryHouses[i] ??
                    throw new InvalidOperationException(
                        "A county gentry house cannot be null.");
                _ = new StableId(gentry.Id);
                if (!governanceIds.Contains(gentry.CountyGovernanceId) ||
                    !FamilyExists(Families, gentry.FamilyId) ||
                    !FamilyBelongsToGovernance(
                        governanceIds,
                        gentry.CountyGovernanceId,
                        gentry.FamilyId) ||
                    !gentryKeys.Add(
                        gentry.CountyGovernanceId + "|" + gentry.FamilyId) ||
                    gentry.InfluenceBasisPoints < 0 ||
                    gentry.InfluenceBasisPoints > 10_000 ||
                    gentry.TaxComplianceBasisPoints < 0 ||
                    gentry.TaxComplianceBasisPoints > 10_000 ||
                    gentry.TotalAssessmentReductionMoney < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid county gentry house {gentry.Id}.");
                }
            }

            var taxKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < CountyHouseholdTaxes.Count; i++)
            {
                var tax = CountyHouseholdTaxes[i] ??
                    throw new InvalidOperationException(
                        "A county household tax cannot be null.");
                _ = new StableId(tax.Id);
                if (!governanceIds.Contains(tax.CountyGovernanceId) ||
                    !FamilyExists(Families, tax.FamilyId) ||
                    !FamilyBelongsToGovernance(
                        governanceIds,
                        tax.CountyGovernanceId,
                        tax.FamilyId) ||
                    !taxKeys.Add(
                        tax.CountyGovernanceId + "|" + tax.FamilyId) ||
                    tax.AssessedMoney < 0 ||
                    tax.PaidMoney < 0 ||
                    tax.ArrearsMoney < 0 ||
                    tax.LastAssessmentDay < -1)
                {
                    throw new InvalidOperationException(
                        $"Invalid county household tax {tax.Id}.");
                }
            }

            for (var i = 0; i < CountyFiscalLedgerEntries.Count; i++)
            {
                var entry = CountyFiscalLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A county fiscal ledger entry cannot be null.");
                _ = new StableId(entry.Id);
                if (!governanceIds.Contains(entry.CountyGovernanceId) ||
                    !string.IsNullOrEmpty(entry.FamilyId) &&
                    !FamilyExists(Families, entry.FamilyId) ||
                    !string.IsNullOrEmpty(entry.VillageId) &&
                    !VillageExists(entry.VillageId) ||
                    !Enum.IsDefined(typeof(CountyFiscalEntryType), entry.Type) ||
                    entry.Amount < 0 ||
                    entry.FamilyMoneyDelta + entry.GovernmentMoneyDelta != 0 ||
                    entry.VillageGrainDelta + entry.CountyGrainDelta != 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid county fiscal ledger entry {entry.Id}.");
                }
            }
        }

        private bool VillageExists(string villageId)
        {
            for (var i = 0; i < Villages.Count; i++)
            {
                if (Villages[i].Id == villageId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool LocationHasKind(string locationId, LocationKind kind)
        {
            for (var i = 0; i < Locations.Count; i++)
            {
                if (Locations[i].Id == locationId)
                {
                    return Locations[i].Kind == kind;
                }
            }

            return false;
        }

        private bool FamilyBelongsToGovernance(
            HashSet<string> governanceIds,
            string governanceId,
            string familyId)
        {
            if (!governanceIds.Contains(governanceId))
            {
                return false;
            }

            for (var i = 0; i < CountyGovernances.Count; i++)
            {
                if (CountyGovernances[i].Id == governanceId)
                {
                    return FamilyBelongsToCounty(
                        familyId, CountyGovernances[i].CountyLocationId);
                }
            }

            return false;
        }

        private bool FamilyBelongsToCounty(
            string familyId,
            string countyLocationId)
        {
            FamilyState family = null;
            for (var i = 0; i < Families.Count; i++)
            {
                if (Families[i].Id == familyId)
                {
                    family = Families[i];
                    break;
                }
            }

            if (family == null)
            {
                return false;
            }

            if (family.LocationId == countyLocationId)
            {
                return true;
            }

            for (var i = 0; i < Villages.Count; i++)
            {
                if (Villages[i].Id == family.VillageId)
                {
                    return Villages[i].ParentLocationId == countyLocationId;
                }
            }

            return false;
        }

        private void ValidatePersonProgression(PersonState person)
        {
            if (person.SkillMasteries == null ||
                person.KnowledgeMasteries == null ||
                person.TechnologyMasteries == null)
            {
                throw new InvalidOperationException(
                    $"Missing progression collections for {person.Id}.");
            }

            var skillIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < person.SkillMasteries.Count; i++)
            {
                var mastery = person.SkillMasteries[i] ??
                    throw new InvalidOperationException(
                        $"Null skill mastery for {person.Id}.");
                ValidateContentReference(
                    mastery.SkillDefinitionId, "skill", person.Id);
                if (!skillIds.Add(mastery.SkillDefinitionId) ||
                    mastery.MasteryBasisPoints < 0 ||
                    mastery.MasteryBasisPoints > 10_000 ||
                    mastery.LastChangedDay < 0 ||
                    mastery.LastChangedDay > AbsoluteDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid skill mastery for {person.Id}.");
                }
            }

            var knowledgeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < person.KnowledgeMasteries.Count; i++)
            {
                var mastery = person.KnowledgeMasteries[i] ??
                    throw new InvalidOperationException(
                        $"Null knowledge mastery for {person.Id}.");
                ValidateContentReference(
                    mastery.KnowledgeDefinitionId, "knowledge", person.Id);
                if (!knowledgeIds.Add(mastery.KnowledgeDefinitionId) ||
                    mastery.MasteryBasisPoints <= 0 ||
                    mastery.MasteryBasisPoints > 10_000 ||
                    mastery.LearnedDay < 0 ||
                    mastery.LearnedDay > AbsoluteDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid knowledge mastery for {person.Id}.");
                }
            }

            var technologyIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < person.TechnologyMasteries.Count; i++)
            {
                var mastery = person.TechnologyMasteries[i] ??
                    throw new InvalidOperationException(
                        $"Null technology mastery for {person.Id}.");
                ValidateContentReference(
                    mastery.TechnologyDefinitionId, "technology", person.Id);
                if (!technologyIds.Add(mastery.TechnologyDefinitionId) ||
                    mastery.MasteredDay < 0 ||
                    mastery.MasteredDay > AbsoluteDay ||
                    string.IsNullOrWhiteSpace(mastery.SourceId))
                {
                    throw new InvalidOperationException(
                        $"Invalid technology mastery for {person.Id}.");
                }
            }
        }

        private void ValidateResearch(HashSet<string> personIds)
        {
            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < VillageFacilities.Count; i++)
            {
                facilityIds.Add(VillageFacilities[i].Id);
            }

            var projectIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < ResearchProjects.Count; i++)
            {
                var project = ResearchProjects[i] ??
                    throw new InvalidOperationException(
                        "A research project cannot be null.");
                projectIds.Add(project.Id);
                ValidateContentReference(
                    project.TechnologyDefinitionId, "technology", project.Id);
                if (!personIds.Contains(project.LeadPersonId) ||
                    !facilityIds.Contains(project.ResearchFacilityId) ||
                    !Enum.IsDefined(
                        typeof(ResearchControlMode), project.ControlMode) ||
                    !Enum.IsDefined(
                        typeof(ResearchProjectStatus), project.Status) ||
                    project.StartedDay < 0 ||
                    project.StartedDay > AbsoluteDay ||
                    project.LastProgressDay < -1 ||
                    project.LastProgressDay > AbsoluteDay ||
                    project.CompletedDay < -1 ||
                    project.CompletedDay > AbsoluteDay ||
                    project.RequiredResearchPoints <= 0 ||
                    project.ProgressResearchPoints < 0 ||
                    project.ProgressResearchPoints >
                        project.RequiredResearchPoints ||
                    project.FundingCommitted < 0 ||
                    project.Status == ResearchProjectStatus.Active &&
                    (project.CompletedDay != -1 ||
                     project.ProgressResearchPoints >=
                        project.RequiredResearchPoints) ||
                    project.Status == ResearchProjectStatus.Completed &&
                    (project.CompletedDay < project.StartedDay ||
                     project.ProgressResearchPoints !=
                        project.RequiredResearchPoints))
                {
                    throw new InvalidOperationException(
                        $"Invalid research project {project.Id}.");
                }
            }

            for (var personIndex = 0; personIndex < People.Count; personIndex++)
            {
                var person = People[personIndex];
                for (var masteryIndex = 0;
                     masteryIndex < person.TechnologyMasteries.Count;
                     masteryIndex++)
                {
                    var mastery = person.TechnologyMasteries[masteryIndex];
                    if (!string.IsNullOrEmpty(mastery.ResearchProjectId) &&
                        !projectIds.Contains(mastery.ResearchProjectId))
                    {
                        throw new InvalidOperationException(
                            $"Technology mastery for {person.Id} references " +
                            $"missing project {mastery.ResearchProjectId}.");
                    }
                }
            }

            var applicationIds = new HashSet<string>(StringComparer.Ordinal);
            var activeTargets = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < TechnologyApplications.Count; i++)
            {
                var application = TechnologyApplications[i] ??
                    throw new InvalidOperationException(
                        "A technology application cannot be null.");
                applicationIds.Add(application.Id);
                ValidateContentReference(
                    application.TechnologyDefinitionId,
                    "technology",
                    application.Id);
                if (!personIds.Contains(application.AppliedByPersonId) ||
                    !facilityIds.Contains(application.TargetFacilityId) ||
                    application.AppliedDay < 0 ||
                    application.AppliedDay > AbsoluteDay ||
                    application.IsActive &&
                    !activeTargets.Add(
                        application.TechnologyDefinitionId + "@" +
                        application.TargetFacilityId))
                {
                    throw new InvalidOperationException(
                        $"Invalid technology application {application.Id}.");
                }
            }

            for (var i = 0; i < ResearchLedgerEntries.Count; i++)
            {
                var entry = ResearchLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A research ledger entry cannot be null.");
                var isKnowledge = entry.Type ==
                    ResearchLedgerEntryType.KnowledgeLearned;
                if (!Enum.IsDefined(
                        typeof(ResearchLedgerEntryType), entry.Type) ||
                    entry.Day < 0 || entry.Day > AbsoluteDay ||
                    !personIds.Contains(entry.PersonId) ||
                    !string.IsNullOrEmpty(entry.FacilityId) &&
                    !facilityIds.Contains(entry.FacilityId) ||
                    !string.IsNullOrEmpty(entry.ResearchProjectId) &&
                    !projectIds.Contains(entry.ResearchProjectId) ||
                    !string.IsNullOrEmpty(entry.TechnologyApplicationId) &&
                    !applicationIds.Contains(entry.TechnologyApplicationId) ||
                    entry.FundingDelta > 0 ||
                    entry.ProgressDelta < 0 ||
                    isKnowledge !=
                    !string.IsNullOrEmpty(entry.KnowledgeDefinitionId) ||
                    isKnowledge ==
                    !string.IsNullOrEmpty(entry.TechnologyDefinitionId))
                {
                    throw new InvalidOperationException(
                        $"Invalid research ledger entry {entry.Id}.");
                }

                if (isKnowledge)
                {
                    ValidateContentReference(
                        entry.KnowledgeDefinitionId, "knowledge", entry.Id);
                }
                else
                {
                    ValidateContentReference(
                        entry.TechnologyDefinitionId, "technology", entry.Id);
                }
            }
        }

        private void ValidateInventoryProduction(
            HashSet<string> personIds,
            HashSet<string> locationIds)
        {
            var familyIds = new HashSet<string>(StringComparer.Ordinal);
            var organizationIds = new HashSet<string>(StringComparer.Ordinal);
            var facilities = new Dictionary<string, VillageFacilityState>(
                StringComparer.Ordinal);
            var containers = new Dictionary<string, InventoryContainerState>(
                StringComparer.Ordinal);
            var batches = new Dictionary<string, ProductBatchState>(
                StringComparer.Ordinal);
            var processingOrders = new Dictionary<string, ProcessingWorkOrderState>(
                StringComparer.Ordinal);
            var processingOrderIds = new HashSet<string>(StringComparer.Ordinal);
            var procurementOrderIds = new HashSet<string>(StringComparer.Ordinal);
            var transactionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Families.Count; i++)
            {
                familyIds.Add(Families[i].Id);
            }

            for (var i = 0; i < Organizations.Count; i++)
            {
                organizationIds.Add(Organizations[i].Id);
            }

            for (var i = 0; i < VillageFacilities.Count; i++)
            {
                facilities.Add(VillageFacilities[i].Id, VillageFacilities[i]);
            }

            for (var i = 0; i < ProcessingWorkOrders.Count; i++)
            {
                processingOrderIds.Add(ProcessingWorkOrders[i].Id);
            }

            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                transactionIds.Add(InventoryTransactions[i].Id);
            }

            for (var i = 0; i < MilitaryProcurementOrders.Count; i++)
            {
                procurementOrderIds.Add(MilitaryProcurementOrders[i].Id);
            }

            for (var i = 0; i < InventoryContainers.Count; i++)
            {
                var container = InventoryContainers[i] ??
                    throw new InvalidOperationException(
                        "An inventory container cannot be null.");
                containers.Add(container.Id, container);
                ValidateContentReference(
                    container.KindId, "inventory container kind", container.Id);
                var familyOwned = !string.IsNullOrEmpty(container.OwnerFamilyId);
                var organizationOwned =
                    !string.IsNullOrEmpty(container.OwnerOrganizationId);
                if (familyOwned == organizationOwned ||
                    familyOwned && !familyIds.Contains(container.OwnerFamilyId) ||
                    organizationOwned &&
                    !organizationIds.Contains(container.OwnerOrganizationId) ||
                    !string.IsNullOrEmpty(container.CarrierPersonId) &&
                    !personIds.Contains(container.CarrierPersonId) ||
                    !locationIds.Contains(container.LocationId) ||
                    container.CapacityWeight <= 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid inventory container {container.Id}.");
                }
            }

            for (var i = 0; i < ProductBatches.Count; i++)
            {
                var batch = ProductBatches[i] ??
                    throw new InvalidOperationException("A product batch cannot be null.");
                batches.Add(batch.Id, batch);
                ValidateContentReference(
                    batch.ProductDefinitionId, "batch product", batch.Id);
                ValidateContentReference(batch.UnitId, "batch unit", batch.Id);
                if (!string.IsNullOrEmpty(batch.CropVarietyDefinitionId))
                {
                    ValidateContentReference(
                        batch.CropVarietyDefinitionId, "batch variety", batch.Id);
                }

                var familyStored = !string.IsNullOrEmpty(batch.OwnerFamilyId) &&
                    string.IsNullOrEmpty(batch.OwnerOrganizationId) &&
                    !string.IsNullOrEmpty(batch.StorageFacilityId) &&
                    string.IsNullOrEmpty(batch.InventoryContainerId);
                var organizationStored =
                    string.IsNullOrEmpty(batch.OwnerFamilyId) &&
                    !string.IsNullOrEmpty(batch.OwnerOrganizationId) &&
                    string.IsNullOrEmpty(batch.StorageFacilityId) &&
                    !string.IsNullOrEmpty(batch.InventoryContainerId);
                var validFamilyStorage = familyStored &&
                    familyIds.Contains(batch.OwnerFamilyId) &&
                    facilities.TryGetValue(
                        batch.StorageFacilityId, out var facility) &&
                    facility.OwnerFamilyId == batch.OwnerFamilyId;
                var validOrganizationStorage = organizationStored &&
                    organizationIds.Contains(batch.OwnerOrganizationId) &&
                    containers.TryGetValue(
                        batch.InventoryContainerId, out var container) &&
                    container.OwnerOrganizationId == batch.OwnerOrganizationId;
                if (!validFamilyStorage && !validOrganizationStorage ||
                    !locationIds.Contains(batch.OriginLocationId) ||
                    !transactionIds.Contains(batch.SourceTransactionId) ||
                    !string.IsNullOrEmpty(batch.SourceWorkOrderId) &&
                    !processingOrderIds.Contains(batch.SourceWorkOrderId) ||
                    batch.ProducedDay < 0 || batch.ProducedDay > AbsoluteDay ||
                    batch.UnitWeight <= 0 ||
                    batch.Quantity < 0 || batch.ReservedQuantity < 0 ||
                    batch.ReservedQuantity > batch.Quantity ||
                    batch.QualityBasisPoints < 0 ||
                    batch.QualityBasisPoints > 10_000 ||
                    batch.FreshnessBasisPoints < 0 ||
                    batch.FreshnessBasisPoints > 10_000 ||
                    batch.SeedVigorBasisPoints < 0 ||
                    batch.SeedVigorBasisPoints > 10_000 ||
                    batch.SeedPurityBasisPoints < 0 ||
                    batch.SeedPurityBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        $"Invalid product batch {batch.Id}.");
                }
            }

            for (var i = 0; i < ProcessingWorkOrders.Count; i++)
            {
                var order = ProcessingWorkOrders[i] ??
                    throw new InvalidOperationException(
                        "A processing work order cannot be null.");
                processingOrders.Add(order.Id, order);
                ValidateContentReference(
                    order.RecipeDefinitionId, "processing recipe", order.Id);
                ValidateContentReference(
                    order.MethodDefinitionId, "processing method", order.Id);
                if (!familyIds.Contains(order.OwnerFamilyId) ||
                    !facilities.TryGetValue(order.StorageFacilityId, out var facility) ||
                    facility.OwnerFamilyId != order.OwnerFamilyId ||
                    !personIds.Contains(order.ManagerPersonId) ||
                    !Enum.IsDefined(typeof(ProductionControlMode), order.ControlMode) ||
                    !Enum.IsDefined(typeof(ProductionOrderStatus), order.Status) ||
                    order.CreatedDay < 0 || order.FinishDay <= order.CreatedDay ||
                    order.SettledDay < -1 || order.SettledDay > AbsoluteDay ||
                    order.RunCount <= 0 || order.InputReservations == null ||
                    order.InputReservations.Count == 0 ||
                    order.OutputBatchIds == null ||
                    order.Status == ProductionOrderStatus.Active &&
                    (order.SettledDay != -1 || order.OutputBatchIds.Count != 0) ||
                    order.Status == ProductionOrderStatus.Completed &&
                    (order.SettledDay < order.FinishDay ||
                     order.OutputBatchIds.Count == 0))
                {
                    throw new InvalidOperationException(
                        $"Invalid processing work order {order.Id}.");
                }

                var reservationKeys = new HashSet<string>(StringComparer.Ordinal);
                for (var reservationIndex = 0;
                     reservationIndex < order.InputReservations.Count;
                     reservationIndex++)
                {
                    var reservation = order.InputReservations[reservationIndex];
                    if (reservation == null || reservation.Quantity <= 0 ||
                        !batches.ContainsKey(reservation.BatchId) ||
                        !reservationKeys.Add(reservation.BatchId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid reservation on {order.Id}.");
                    }
                }

                var outputIds = new HashSet<string>(StringComparer.Ordinal);
                for (var outputIndex = 0;
                     outputIndex < order.OutputBatchIds.Count;
                     outputIndex++)
                {
                    if (!batches.TryGetValue(
                            order.OutputBatchIds[outputIndex], out var output) ||
                        output.SourceWorkOrderId != order.Id ||
                        !outputIds.Add(output.Id))
                    {
                        throw new InvalidOperationException(
                            $"Invalid output batch on {order.Id}.");
                    }
                }
            }

            var quantityDeltas = new Dictionary<string, long>(StringComparer.Ordinal);
            var reservationDeltas = new Dictionary<string, long>(StringComparer.Ordinal);
            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                var transaction = InventoryTransactions[i] ??
                    throw new InvalidOperationException(
                        "An inventory transaction cannot be null.");
                if (transaction.Day < 0 || transaction.Day > AbsoluteDay ||
                    !Enum.IsDefined(
                        typeof(InventoryTransactionType), transaction.Type) ||
                    !string.IsNullOrEmpty(transaction.ActorPersonId) &&
                    !personIds.Contains(transaction.ActorPersonId) ||
                    !string.IsNullOrEmpty(transaction.SourceWorkOrderId) &&
                    !processingOrders.ContainsKey(transaction.SourceWorkOrderId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceMilitaryProcurementId) &&
                    !procurementOrderIds.Contains(
                        transaction.SourceMilitaryProcurementId) ||
                    transaction.Lines == null || transaction.Lines.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid inventory transaction {transaction.Id}.");
                }


                var requiresOrder =
                    transaction.Type == InventoryTransactionType.Reserved ||
                    transaction.Type == InventoryTransactionType.ReservationReleased ||
                    transaction.Type == InventoryTransactionType.RecipeSettled;
                var requiresProcurement = transaction.Type ==
                    InventoryTransactionType.MilitaryProcurementDispatched;
                if (requiresOrder !=
                    !string.IsNullOrEmpty(transaction.SourceWorkOrderId) ||
                    requiresProcurement !=
                    !string.IsNullOrEmpty(
                        transaction.SourceMilitaryProcurementId) ||
                    transaction.Type ==
                        InventoryTransactionType.LegacyBalanceConverted &&
                    transaction.LegacyFamilyGrainDelta == 0 &&
                    transaction.LegacyFamilySeedGrainDelta == 0 ||
                    transaction.Type == InventoryTransactionType.OpeningBalance &&
                    (transaction.LegacyFamilyGrainDelta != 0 ||
                     transaction.LegacyFamilySeedGrainDelta != 0))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has invalid provenance.");
                }

                for (var lineIndex = 0;
                     lineIndex < transaction.Lines.Count;
                     lineIndex++)
                {
                    var line = transaction.Lines[lineIndex];
                    if (line == null || !batches.TryGetValue(line.BatchId, out var batch) ||
                        line.ProductDefinitionId != batch.ProductDefinitionId ||
                        line.OwnerFamilyId != batch.OwnerFamilyId ||
                        line.OwnerOrganizationId != batch.OwnerOrganizationId ||
                        line.StorageFacilityId != batch.StorageFacilityId ||
                        line.InventoryContainerId != batch.InventoryContainerId ||
                        line.UnitId != batch.UnitId ||
                        line.QuantityDelta == 0 && line.ReservedQuantityDelta == 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid line on inventory transaction {transaction.Id}.");
                    }

                    AddDelta(quantityDeltas, line.BatchId, line.QuantityDelta);
                    AddDelta(
                        reservationDeltas,
                        line.BatchId,
                        line.ReservedQuantityDelta);
                }
            }

            for (var i = 0; i < ProductBatches.Count; i++)
            {
                var batch = ProductBatches[i];
                quantityDeltas.TryGetValue(batch.Id, out var quantity);
                reservationDeltas.TryGetValue(batch.Id, out var reserved);
                if (quantity != batch.Quantity || reserved != batch.ReservedQuantity)
                {
                    throw new InvalidOperationException(
                        $"Product batch ledger mismatch for {batch.Id}.");
                }
            }

            for (var i = 0; i < VillageFacilities.Count; i++)
            {
                var facility = VillageFacilities[i];
                if (facility.Kind != VillageFacilityKind.HouseholdGranary)
                {
                    continue;
                }

                long trackedBatchWeight = 0;
                for (var batchIndex = 0;
                     batchIndex < ProductBatches.Count;
                     batchIndex++)
                {
                    var batch = ProductBatches[batchIndex];
                    if (batch.StorageFacilityId == facility.Id)
                    {
                        trackedBatchWeight = checked(
                            trackedBatchWeight +
                            batch.Quantity * batch.UnitWeight);
                    }
                }

                if (trackedBatchWeight > facility.InventoryUnits)
                {
                    throw new InvalidOperationException(
                        $"Tracked batches exceed granary stock for {facility.Id}.");
                }
            }

            for (var i = 0; i < InventoryContainers.Count; i++)
            {
                var container = InventoryContainers[i];
                long trackedBatchWeight = 0;
                for (var batchIndex = 0;
                     batchIndex < ProductBatches.Count;
                     batchIndex++)
                {
                    var batch = ProductBatches[batchIndex];
                    if (batch.InventoryContainerId == container.Id)
                    {
                        trackedBatchWeight = checked(
                            trackedBatchWeight +
                            batch.Quantity * batch.UnitWeight);
                    }
                }

                if (trackedBatchWeight > container.CapacityWeight)
                {
                    throw new InvalidOperationException(
                        $"Tracked batches exceed container capacity for {container.Id}.");
                }
            }
        }

        private static void AddDelta(
            IDictionary<string, long> totals,
            string id,
            long delta)
        {
            totals.TryGetValue(id, out var current);
            totals[id] = checked(current + delta);
        }

        private void ValidateProduction(HashSet<string> personIds)
        {
            ValidateProductionContentManifest();
            var villageIds = new HashSet<string>(StringComparer.Ordinal);
            var familyIds = new HashSet<string>(StringComparer.Ordinal);
            var facilities = new Dictionary<string, VillageFacilityState>(
                StringComparer.Ordinal);
            for (var i = 0; i < Villages.Count; i++)
            {
                villageIds.Add(Villages[i].Id);
            }

            for (var i = 0; i < Families.Count; i++)
            {
                familyIds.Add(Families[i].Id);
            }

            for (var i = 0; i < VillageFacilities.Count; i++)
            {
                facilities.Add(VillageFacilities[i].Id, VillageFacilities[i]);
            }

            var ordersById = new Dictionary<string, AgricultureWorkOrderState>(
                StringComparer.Ordinal);
            var activeFamilyIds = new HashSet<string>(StringComparer.Ordinal);
            var activeWorkerIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < AgricultureWorkOrders.Count; i++)
            {
                var order = AgricultureWorkOrders[i] ??
                    throw new InvalidOperationException(
                        "An agriculture work order cannot be null.");
                ordersById.Add(order.Id, order);
                ValidateContentReference(order.CropDefinitionId, "crop", order.Id);
                ValidateContentReference(
                    order.CropVarietyDefinitionId, "crop variety", order.Id);
                ValidateContentReference(order.RecipeDefinitionId, "recipe", order.Id);
                ValidateContentReference(
                    order.MethodDefinitionId, "production method", order.Id);
                ValidateContentReference(
                    order.SeedProductDefinitionId, "seed product", order.Id);
                ValidateContentReference(
                    order.HarvestProductDefinitionId, "harvest product", order.Id);
                ValidateContentReference(order.UnitId, "unit", order.Id);
                if (!villageIds.Contains(order.VillageId) ||
                    !familyIds.Contains(order.FamilyId) ||
                    !facilities.TryGetValue(
                        order.FieldFacilityId, out var field) ||
                    field.Kind != VillageFacilityKind.Farmland ||
                    field.VillageId != order.VillageId ||
                    !facilities.TryGetValue(
                        order.StorageFacilityId, out var storage) ||
                    storage.VillageId != order.VillageId ||
                    storage.Kind != VillageFacilityKind.HouseholdGranary ||
                    storage.OwnerFamilyId != order.FamilyId ||
                    !personIds.Contains(order.ManagerPersonId) ||
                    !Enum.IsDefined(
                        typeof(ProductionControlMode), order.ControlMode) ||
                    !Enum.IsDefined(
                        typeof(ProductionOrderStatus), order.Status) ||
                    order.CreatedDay < 0 ||
                    order.PlantingDay < order.CreatedDay ||
                    order.HarvestDay <= order.PlantingDay ||
                    order.HarvestDay > AbsoluteDay &&
                    order.Status == ProductionOrderStatus.Completed ||
                    order.SettledDay < -1 ||
                    order.SettledDay > AbsoluteDay ||
                    order.LandUnits <= 0 ||
                    order.LandUnits > field.Capacity ||
                    order.SeedQuantityCommitted <= 0 ||
                    order.RequiredLaborDays <= 0 ||
                    order.AssignedLaborDays < 0 ||
                    order.TechnologyYieldBasisPoints <= 0 ||
                    order.TechnologyYieldBasisPoints > 30_000 ||
                    order.TechnologyLaborBasisPoints <= 0 ||
                    order.TechnologyLaborBasisPoints > 30_000 ||
                    order.ProducedQuantity < 0 ||
                    order.StoredQuantity < 0 ||
                    order.LostQuantity < 0 ||
                    order.Status == ProductionOrderStatus.Completed &&
                    (order.SettledDay < order.HarvestDay ||
                     order.ProducedQuantity !=
                     order.StoredQuantity + order.LostQuantity) ||
                    order.Status == ProductionOrderStatus.Active &&
                    (order.SettledDay != -1 || order.ProducedQuantity != 0 ||
                     order.StoredQuantity != 0 || order.LostQuantity != 0) ||
                    order.AssignedWorkerIds == null ||
                    order.AssignedWorkerIds.Count == 0 ||
                    order.AppliedTechnologyIds == null)
                {
                    throw new InvalidOperationException(
                        $"Invalid agriculture work order {order.Id}.");
                }

                var workerIds = new HashSet<string>(StringComparer.Ordinal);
                for (var workerIndex = 0;
                     workerIndex < order.AssignedWorkerIds.Count;
                     workerIndex++)
                {
                    var workerId = order.AssignedWorkerIds[workerIndex];
                    if (!personIds.Contains(workerId) || !workerIds.Add(workerId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid worker on agriculture work order {order.Id}.");
                    }

                    if (order.Status == ProductionOrderStatus.Active &&
                        !activeWorkerIds.Add(workerId))
                    {
                        throw new InvalidOperationException(
                            $"Worker {workerId} has overlapping agriculture work.");
                    }
                }

                var technologyIds = new HashSet<string>(StringComparer.Ordinal);
                for (var technologyIndex = 0;
                     technologyIndex < order.AppliedTechnologyIds.Count;
                     technologyIndex++)
                {
                    var technologyId = order.AppliedTechnologyIds[technologyIndex];
                    ValidateContentReference(
                        technologyId, "technology", order.Id);
                    if (!technologyIds.Add(technologyId))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate technology {technologyId} on {order.Id}.");
                    }
                }

                if (order.Status == ProductionOrderStatus.Active &&
                    !activeFamilyIds.Add(order.FamilyId))
                {
                    throw new InvalidOperationException(
                        $"Family {order.FamilyId} has overlapping agriculture work.");
                }
            }

            for (var i = 0; i < ProductionLedgerEntries.Count; i++)
            {
                var entry = ProductionLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A production ledger entry cannot be null.");
                if (!ordersById.TryGetValue(
                        entry.WorkOrderId, out var ledgerOrder) ||
                    !villageIds.Contains(entry.VillageId) ||
                    !familyIds.Contains(entry.FamilyId) ||
                    !facilities.ContainsKey(entry.FacilityId) ||
                    !string.IsNullOrEmpty(entry.PersonId) &&
                    !personIds.Contains(entry.PersonId) ||
                    entry.Day < 0 || entry.Day > AbsoluteDay ||
                    !Enum.IsDefined(
                        typeof(ProductionLedgerEntryType), entry.Type) ||
                    entry.Quantity < 0 ||
                    !LedgerContentMatchesOrder(entry, ledgerOrder))
                {
                    throw new InvalidOperationException(
                        $"Invalid production ledger entry {entry.Id}.");
                }
            }
        }

        private void ValidateProductionContentManifest()
        {
            var manifest = ProductionContentManifest;
            if (manifest == null || manifest.ContentSchemaVersion != 2 ||
                string.IsNullOrWhiteSpace(manifest.ResolvedHash) ||
                manifest.Packages == null || manifest.Packages.Count == 0)
            {
                throw new InvalidOperationException(
                    "Production content manifest is missing or unsupported.");
            }

            var packageIds = new HashSet<string>(StringComparer.Ordinal);
            ProductionContentPackageManifestState previous = null;
            for (var i = 0; i < manifest.Packages.Count; i++)
            {
                var package = manifest.Packages[i];
                if (package == null || string.IsNullOrWhiteSpace(package.Version) ||
                    string.IsNullOrWhiteSpace(package.ContentHash))
                {
                    throw new InvalidOperationException(
                        "Production content manifest contains an invalid package.");
                }

                ValidateContentReference(
                    package.PackageId, "content package", "manifest");
                if (!packageIds.Add(package.PackageId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate production content package {package.PackageId}.");
                }

                if (previous != null &&
                    (previous.LoadOrder > package.LoadOrder ||
                     previous.LoadOrder == package.LoadOrder &&
                     string.CompareOrdinal(
                         previous.PackageId, package.PackageId) >= 0))
                {
                    throw new InvalidOperationException(
                        "Production content manifest packages are not stably ordered.");
                }

                previous = package;
            }
        }

        private static bool LedgerContentMatchesOrder(
            ProductionLedgerEntryState entry,
            AgricultureWorkOrderState order)
        {
            if (entry.UnitId == null)
            {
                return false;
            }

            ValidateContentReference(entry.UnitId, "ledger unit", entry.Id);
            switch (entry.Type)
            {
                case ProductionLedgerEntryType.InputCommitted:
                    return entry.ProductDefinitionId ==
                               order.SeedProductDefinitionId &&
                           entry.UnitId == order.UnitId;
                case ProductionLedgerEntryType.LaborCommitted:
                    return string.IsNullOrEmpty(entry.ProductDefinitionId) &&
                           entry.UnitId == CoreProductionContent.LaborDayUnitId;
                case ProductionLedgerEntryType.ProductStored:
                case ProductionLedgerEntryType.ProductLost:
                    return entry.ProductDefinitionId ==
                               order.HarvestProductDefinitionId &&
                           entry.UnitId == order.UnitId;
                default:
                    return false;
            }
        }

        private static void ValidateContentReference(
            string id,
            string kind,
            string ownerId)
        {
            try
            {
                _ = new StableId(id);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"Invalid {kind} reference on {ownerId}: {exception.Message}");
            }

            if (id.IndexOf('.') <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid {kind} reference on {ownerId}: {id}.");
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
