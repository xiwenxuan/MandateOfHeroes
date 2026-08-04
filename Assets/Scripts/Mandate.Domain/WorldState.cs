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

    public enum PersistentWorldCommandStatus : byte
    {
        Pending,
        Completed,
        Cancelled
    }

    public enum WorldCommandBatchOutcome : byte
    {
        Succeeded,
        Rejected
    }

    public enum WorldEventDispatchStatus : byte
    {
        Pending,
        Dispatched
    }

    [Serializable]
    public sealed class WorldCommandArgumentState
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public sealed class PersistentWorldCommandState
    {
        public string Id;
        public string CommandTypeId;
        public string IssuerId;
        public long CreatedDay;
        public byte CreatedSegment;
        public long DueDay;
        public byte DueSegment;
        public int Priority;
        public PersistentWorldCommandStatus Status;
        public int AttemptCount;
        public string LastAttemptResultId;
        public long CompletedDay = -1;
        public byte CompletedSegment;
        public string CompletionResultId;
        public List<WorldCommandArgumentState> Arguments =
            new List<WorldCommandArgumentState>();
    }

    [Serializable]
    public sealed class WorldTransactionExecutionState
    {
        public string TransactionId;
        public string TransactionKindId;
        public int Priority;
    }

    [Serializable]
    public sealed class WorldCommandBatchResultState
    {
        public string Id;
        public WorldCommandBatchOutcome Outcome;
        public long Day;
        public byte Segment;
        public string FailureCode;
        public List<string> CommandIds = new List<string>();
        public List<WorldTransactionExecutionState> Transactions =
            new List<WorldTransactionExecutionState>();
        public List<string> PublishedEventIds = new List<string>();
    }

    [Serializable]
    public sealed class WorldEventOutboxState
    {
        public string Id;
        public string EventTypeId;
        public string SourceTransactionId;
        public long Day;
        public byte Segment;
        public WorldEventDispatchStatus DispatchStatus;
        public long DispatchedDay = -1;
        public byte DispatchedSegment;
        public List<string> DeliveredHandlerIds = new List<string>();
    }

    [Serializable]
    public sealed class WorldState
    {
        public const int CurrentSchemaVersion = 36;

        public int SchemaVersion = CurrentSchemaVersion;
        public ulong MasterSeed;
        public long AbsoluteDay;
        public byte Segment;
        public long Revision;
        public string PlayerPersonId;
        public List<PersistentWorldCommandState> PersistentWorldCommands =
            new List<PersistentWorldCommandState>();
        public List<WorldCommandBatchResultState> WorldCommandBatchResults =
            new List<WorldCommandBatchResultState>();
        public List<WorldEventOutboxState> WorldEventOutbox =
            new List<WorldEventOutboxState>();
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
        public List<FormalMarketOrderState> FormalMarketOrders =
            new List<FormalMarketOrderState>();
        public List<FormalMarketTradeState> FormalMarketTrades =
            new List<FormalMarketTradeState>();
        public List<PublicReliefProcurementTradeState>
            PublicReliefProcurementTrades =
                new List<PublicReliefProcurementTradeState>();
        public List<PublicReliefRecoveryState> PublicReliefRecoveries =
            new List<PublicReliefRecoveryState>();
        public List<FormalMarketPriceState> FormalMarketPrices =
            new List<FormalMarketPriceState>();
        public List<CivilianFreightState> CivilianFreights =
            new List<CivilianFreightState>();
        public List<CivilianFreightLedgerEntryState> CivilianFreightLedgerEntries =
            new List<CivilianFreightLedgerEntryState>();
        public List<CivilianFreightDemandState> CivilianFreightDemands =
            new List<CivilianFreightDemandState>();
        public List<CivilianCarrierRegistrationState>
            CivilianCarrierRegistrations =
                new List<CivilianCarrierRegistrationState>();
        public List<CivilianCarrierOfferState> CivilianCarrierOffers =
            new List<CivilianCarrierOfferState>();
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
        public FoodInventoryAuthorityMode FoodInventoryAuthorityMode =
            FoodInventoryAuthorityMode.LegacyScalar;
        public List<InventoryContainerState> InventoryContainers =
            new List<InventoryContainerState>();
        public List<ProductionSiteState> ProductionSites =
            new List<ProductionSiteState>();
        public List<InventoryTransactionState> InventoryTransactions =
            new List<InventoryTransactionState>();
        public List<ProcessingWorkOrderState> ProcessingWorkOrders =
            new List<ProcessingWorkOrderState>();
        public List<ProductionPracticeLedgerEntryState>
            ProductionPracticeLedgerEntries =
                new List<ProductionPracticeLedgerEntryState>();
        public List<ResourceBodyState> ResourceBodies =
            new List<ResourceBodyState>();
        public List<ResourceExtractionOrderState> ResourceExtractionOrders =
            new List<ResourceExtractionOrderState>();
        public List<ResourceExtractionLedgerEntryState>
            ResourceExtractionLedgerEntries =
                new List<ResourceExtractionLedgerEntryState>();
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
        public List<MilitaryLogisticsOrderState> MilitaryLogisticsOrders =
            new List<MilitaryLogisticsOrderState>();
        public List<MilitaryLogisticsLegState> MilitaryLogisticsLegs =
            new List<MilitaryLogisticsLegState>();
        public List<MilitaryLogisticsEscortState> MilitaryLogisticsEscorts =
            new List<MilitaryLogisticsEscortState>();
        public List<MilitaryLogisticsIncidentState> MilitaryLogisticsIncidents =
            new List<MilitaryLogisticsIncidentState>();
        public List<MilitaryLogisticsClashState> MilitaryLogisticsClashes =
            new List<MilitaryLogisticsClashState>();
        public List<MilitaryLogisticsLiabilitySettlementState>
            MilitaryLogisticsLiabilitySettlements =
                new List<MilitaryLogisticsLiabilitySettlementState>();
        public List<MilitaryLogisticsDelegationGoalState>
            MilitaryLogisticsDelegationGoals =
                new List<MilitaryLogisticsDelegationGoalState>();
        public List<MilitaryLogisticsDelegationOfferState>
            MilitaryLogisticsDelegationOffers =
                new List<MilitaryLogisticsDelegationOfferState>();
        public List<MilitaryLogisticsDelegationReportState>
            MilitaryLogisticsDelegationReports =
                new List<MilitaryLogisticsDelegationReportState>();
        public List<MilitaryLogisticsLedgerEntryState>
            MilitaryLogisticsLedgerEntries =
                new List<MilitaryLogisticsLedgerEntryState>();
        public List<MilitaryEquipmentRepairOrderState>
            MilitaryEquipmentRepairOrders =
                new List<MilitaryEquipmentRepairOrderState>();
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

            if (!Enum.IsDefined(
                    typeof(FoodInventoryAuthorityMode),
                    FoodInventoryAuthorityMode))
            {
                throw new InvalidOperationException(
                    "Food inventory authority mode is invalid.");
            }

            if (FoodInventoryAuthorityMode ==
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                for (var familyIndex = 0;
                     familyIndex < Families.Count;
                     familyIndex++)
                {
                    if (Families[familyIndex].Grain != 0)
                    {
                        throw new InvalidOperationException(
                            $"Formal food inventory cannot retain legacy grain for {Families[familyIndex].Id}.");
                    }
                }
            }

            PopulationStorage.Validate(People.Count);
            ValidateUniqueIds(
                PersistentWorldCommands,
                item => item.Id,
                "persistent world command");
            ValidateUniqueIds(
                WorldCommandBatchResults,
                item => item.Id,
                "world command batch result");
            ValidateUniqueIds(
                WorldEventOutbox,
                item => item.Id,
                "world event outbox entry");
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
            ValidateUniqueIds(
                FormalMarketOrders, item => item.Id, "formal market order");
            ValidateUniqueIds(
                FormalMarketTrades, item => item.Id, "formal market trade");
            ValidateUniqueIds(
                PublicReliefProcurementTrades,
                item => item.Id,
                "public relief procurement trade");
            ValidateUniqueIds(
                PublicReliefRecoveries,
                item => item.Id,
                "public relief recovery");
            ValidateUniqueIds(
                FormalMarketPrices, item => item.Id, "formal market price");
            ValidateUniqueIds(
                CivilianFreights, item => item.Id, "civilian freight");
            ValidateUniqueIds(
                CivilianFreightLedgerEntries,
                item => item.Id,
                "civilian freight ledger entry");
            ValidateUniqueIds(
                CivilianFreightDemands,
                item => item.Id,
                "civilian freight demand");
            ValidateUniqueIds(
                CivilianCarrierRegistrations,
                item => item.Id,
                "civilian carrier registration");
            ValidateUniqueIds(
                CivilianCarrierOffers,
                item => item.Id,
                "civilian carrier offer");
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
                ProductionSites, item => item.Id, "production site");
            ValidateUniqueIds(
                InventoryTransactions, item => item.Id, "inventory transaction");
            ValidateUniqueIds(
                ProcessingWorkOrders, item => item.Id, "processing work order");
            ValidateUniqueIds(
                ProductionPracticeLedgerEntries,
                item => item.Id,
                "production practice ledger entry");
            ValidateUniqueIds(
                ResourceBodies, item => item.Id, "resource body");
            ValidateUniqueIds(
                ResourceExtractionOrders,
                item => item.Id,
                "resource extraction order");
            ValidateUniqueIds(
                ResourceExtractionLedgerEntries,
                item => item.Id,
                "resource extraction ledger entry");
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
                MilitaryEquipmentRepairOrders,
                item => item.Id,
                "military equipment repair order");
            ValidateUniqueIds(
                MilitaryProcurementLedgerEntries,
                item => item.Id,
                "military procurement ledger entry");
            ValidateUniqueIds(
                MilitaryLogisticsOrders,
                item => item.Id,
                "military logistics order");
            ValidateUniqueIds(
                MilitaryLogisticsLegs,
                item => item.Id,
                "military logistics leg");
            ValidateUniqueIds(
                MilitaryLogisticsEscorts,
                item => item.Id,
                "military logistics escort");
            ValidateUniqueIds(
                MilitaryLogisticsIncidents,
                item => item.Id,
                "military logistics incident");
            ValidateUniqueIds(
                MilitaryLogisticsClashes,
                item => item.Id,
                "military logistics clash");
            ValidateUniqueIds(
                MilitaryLogisticsLiabilitySettlements,
                item => item.Id,
                "military logistics liability settlement");
            ValidateUniqueIds(
                MilitaryLogisticsDelegationGoals,
                item => item.Id,
                "military logistics delegation goal");
            ValidateUniqueIds(
                MilitaryLogisticsDelegationOffers,
                item => item.Id,
                "military logistics delegation offer");
            ValidateUniqueIds(
                MilitaryLogisticsDelegationReports,
                item => item.Id,
                "military logistics delegation report");
            ValidateUniqueIds(
                MilitaryLogisticsLedgerEntries,
                item => item.Id,
                "military logistics ledger entry");

            ValidatePersistentWorldExecution();

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
            ValidateFormalMarket(locationIds);
            ValidateCivilianFreight(personIds, locationIds);
            ValidatePublicReliefRecovery();
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
                    !string.IsNullOrEmpty(supply.SourceLogisticsOrderId) &&
                    !ContainsId(
                        MilitaryLogisticsOrders,
                        item => item.Id,
                        supply.SourceLogisticsOrderId) ||
                    (supply.Type == MilitarySupplyType.LogisticsDelivery) !=
                    !string.IsNullOrEmpty(supply.SourceLogisticsOrderId) ||
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
            ValidateMilitaryLogistics(
                personIds, locationIds, organizationIds, armyIds, routeIds);
            ValidateMilitaryLogisticsDelegation(
                personIds, locationIds, organizationIds, armyIds, routeIds);
            ValidateAttention(personIds);
        }

        private void ValidateMilitaryLogisticsDelegation(
            HashSet<string> personIds,
            HashSet<string> locationIds,
            HashSet<string> organizationIds,
            HashSet<string> armyIds,
            HashSet<string> routeIds)
        {
            var goals = new Dictionary<string,
                MilitaryLogisticsDelegationGoalState>(StringComparer.Ordinal);
            var offers = new Dictionary<string,
                MilitaryLogisticsDelegationOfferState>(StringComparer.Ordinal);
            var logisticsOrders = new Dictionary<string,
                MilitaryLogisticsOrderState>(StringComparer.Ordinal);
            var batches = new Dictionary<string, ProductBatchState>(
                StringComparer.Ordinal);
            var containers = new Dictionary<string, InventoryContainerState>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryLogisticsOrders.Count; i++)
            {
                logisticsOrders.Add(
                    MilitaryLogisticsOrders[i].Id,
                    MilitaryLogisticsOrders[i]);
            }

            for (var i = 0; i < ProductBatches.Count; i++)
            {
                batches.Add(ProductBatches[i].Id, ProductBatches[i]);
            }

            for (var i = 0; i < InventoryContainers.Count; i++)
            {
                containers.Add(
                    InventoryContainers[i].Id,
                    InventoryContainers[i]);
            }

            for (var i = 0;
                 i < MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                var goal = MilitaryLogisticsDelegationGoals[i] ??
                    throw new InvalidOperationException(
                        "A military logistics delegation goal cannot be null.");
                _ = new StableId(goal.ProductDefinitionId);
                _ = new StableId(goal.CarrierPreferenceId);
                _ = new StableId(goal.CargoConsumptionPolicyId);
                _ = new StableId(goal.RiskPolicyId);
                _ = new StableId(goal.FulfillmentPolicyId);
                _ = new StableId(goal.ReplacementProcurementPolicyId);
                if (!string.IsNullOrEmpty(goal.CancellationReasonId))
                {
                    _ = new StableId(goal.CancellationReasonId);
                }
                if (!string.IsNullOrEmpty(goal.ReplacesGoalId))
                {
                    _ = new StableId(goal.ReplacesGoalId);
                }
                if (!string.IsNullOrEmpty(
                        goal.LastReplacementAuthorizationReasonId))
                {
                    _ = new StableId(
                        goal.LastReplacementAuthorizationReasonId);
                }
                var validPreference = goal.CarrierPreferenceId ==
                        MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost ||
                    goal.CarrierPreferenceId ==
                        MilitaryLogisticsDelegationCarrierPreferenceIds.SafestRoute ||
                    goal.CarrierPreferenceId ==
                        MilitaryLogisticsDelegationCarrierPreferenceIds
                            .OwnOrganizationFirst;
                var validRisk = goal.RiskPolicyId ==
                        MilitaryLogisticsRiskPolicyIds.None &&
                    string.IsNullOrEmpty(goal.ThreatOrganizationId) ||
                    goal.RiskPolicyId ==
                        MilitaryLogisticsRiskPolicyIds.Standard &&
                    organizationIds.Contains(goal.ThreatOrganizationId);
                var strictFulfillment = goal.FulfillmentPolicyId ==
                    MilitaryLogisticsDelegationFulfillmentPolicyIds
                        .FullReceiptRequired;
                var legacyFulfillment = goal.FulfillmentPolicyId ==
                    MilitaryLogisticsDelegationFulfillmentPolicyIds
                        .LegacyOrderCompletion;
                var waitForCustody = goal.ReplacementProcurementPolicyId ==
                    MilitaryLogisticsReplacementProcurementPolicyIds
                        .WaitForCustodyResolution;
                var explicitReplacement =
                    goal.ReplacementProcurementPolicyId ==
                        MilitaryLogisticsReplacementProcurementPolicyIds
                            .ExplicitAuthorization;
                var legacyReplacement =
                    goal.ReplacementProcurementPolicyId ==
                        MilitaryLogisticsReplacementProcurementPolicyIds
                            .LegacyUnrestricted;
                var hasReplacementAudit =
                    goal.LastReplacementAuthorizedDay >= 0 ||
                    !string.IsNullOrEmpty(
                        goal.LastReplacementAuthorizedByPersonId) ||
                    !string.IsNullOrEmpty(
                        goal.LastReplacementAuthorizationReasonId);
                if (!Enum.IsDefined(
                        typeof(MilitaryLogisticsDelegationStatus),
                        goal.Status) ||
                    !personIds.Contains(goal.IssuerPersonId) ||
                    !personIds.Contains(goal.AssigneePersonId) ||
                    !string.IsNullOrEmpty(goal.DelegatedByPersonId) &&
                        !personIds.Contains(goal.DelegatedByPersonId) ||
                    !string.IsNullOrEmpty(goal.CancelledByPersonId) &&
                        !personIds.Contains(goal.CancelledByPersonId) ||
                    !armyIds.Contains(goal.TargetArmyId) ||
                    !locationIds.Contains(goal.DestinationLocationId) ||
                    goal.CreatedDay < 0 || goal.CreatedDay > AbsoluteDay ||
                    goal.DeadlineDay < goal.CreatedDay ||
                    goal.ReportIntervalDays <= 0 ||
                    goal.LastEvaluatedDay < -1 ||
                    goal.LastEvaluatedDay > AbsoluteDay ||
                    goal.LastEvaluatedDay >= 0 &&
                        goal.LastEvaluatedDay < goal.CreatedDay ||
                    goal.NextEvaluationDay < goal.CreatedDay ||
                    goal.FulfilledDay < -1 ||
                    goal.FulfilledDay > AbsoluteDay ||
                    goal.Status ==
                        MilitaryLogisticsDelegationStatus.Fulfilled &&
                        goal.FulfilledDay < goal.CreatedDay ||
                    goal.Status !=
                        MilitaryLogisticsDelegationStatus.Fulfilled &&
                        goal.FulfilledDay != -1 ||
                    goal.ChildGoalIds == null ||
                    goal.ReplacementGoalIds == null ||
                    goal.CompletedLogisticsOrderIds == null ||
                    goal.DelegationDepth < 0 ||
                    goal.DelegationDepth >
                        MilitaryLogisticsDelegationContract
                            .MaximumDelegationDepth ||
                    !Enum.IsDefined(
                        typeof(MilitaryAuthorityLevel),
                        goal.AssigneeAuthorityAtDelegation) ||
                    goal.AssigneeAuthorityAtDelegation ==
                        MilitaryAuthorityLevel.None ||
                    goal.RequestedCargoQuantity <= 0 ||
                    goal.ReceivedCargoQuantity < 0 ||
                    goal.ReceivedCargoQuantity >
                        goal.RequestedCargoQuantity ||
                    goal.OutstandingCargoQuantity < 0 ||
                    goal.OutstandingCargoQuantity >
                        goal.RequestedCargoQuantity ||
                    strictFulfillment &&
                        goal.ReceivedCargoQuantity +
                            goal.OutstandingCargoQuantity !=
                        goal.RequestedCargoQuantity ||
                    legacyFulfillment &&
                        (goal.Status !=
                             MilitaryLogisticsDelegationStatus.Fulfilled ||
                         goal.OutstandingCargoQuantity != 0) ||
                    !strictFulfillment && !legacyFulfillment ||
                    strictFulfillment &&
                        goal.Status ==
                            MilitaryLogisticsDelegationStatus.Fulfilled &&
                        goal.OutstandingCargoQuantity != 0 ||
                    !waitForCustody && !explicitReplacement &&
                        !legacyReplacement ||
                    goal.AuthorizedReplacementQuantity < 0 ||
                    goal.ConsumedReplacementAuthorizationQuantity < 0 ||
                    goal.ConsumedReplacementAuthorizationQuantity >
                        goal.AuthorizedReplacementQuantity ||
                    explicitReplacement &&
                    (goal.AuthorizedReplacementQuantity <= 0 ||
                     goal.LastReplacementAuthorizedDay < goal.CreatedDay ||
                     goal.LastReplacementAuthorizedDay > AbsoluteDay ||
                     !personIds.Contains(
                         goal.LastReplacementAuthorizedByPersonId) ||
                     string.IsNullOrEmpty(
                         goal.LastReplacementAuthorizationReasonId)) ||
                    !explicitReplacement &&
                    (goal.AuthorizedReplacementQuantity != 0 ||
                     goal.ConsumedReplacementAuthorizationQuantity != 0 ||
                     hasReplacementAudit) ||
                    goal.CompensationReceived < 0 ||
                    goal.CompensationReceived > goal.CommittedCost ||
                    goal.MaximumUnitPrice < 0 || goal.BudgetLimit < 0 ||
                    goal.UnassignedCargoQuantity < 0 ||
                    goal.UnassignedCargoQuantity >
                        goal.RequestedCargoQuantity ||
                    goal.AvailableBudgetReserve < 0 ||
                    goal.AvailableBudgetReserve > goal.BudgetLimit ||
                    goal.CancelledDay < -1 ||
                    goal.CancelledDay > AbsoluteDay ||
                    goal.Status ==
                        MilitaryLogisticsDelegationStatus.Cancelled &&
                    (goal.CancelledDay < goal.CreatedDay ||
                     string.IsNullOrEmpty(goal.CancelledByPersonId) ||
                     string.IsNullOrEmpty(goal.CancellationReasonId)) ||
                    goal.Status !=
                        MilitaryLogisticsDelegationStatus.Cancelled &&
                    (goal.CancelledDay != -1 ||
                     !string.IsNullOrEmpty(goal.CancelledByPersonId) ||
                     !string.IsNullOrEmpty(goal.CancellationReasonId)) ||
                    goal.CommittedCost < 0 ||
                    goal.CommittedCost - goal.CompensationReceived >
                        goal.BudgetLimit ||
                    !validPreference || !validRisk)
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics delegation goal {goal.Id}.");
                }

                goals.Add(goal.Id, goal);
            }

            ValidateMilitaryLogisticsDelegationHierarchy(goals);

            var selectedOfferCounts = new Dictionary<string, int>(
                StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                var offer = MilitaryLogisticsDelegationOffers[i] ??
                    throw new InvalidOperationException(
                        "A military logistics delegation offer cannot be null.");
                _ = new StableId(offer.AcquisitionMethodId);
                _ = new StableId(offer.LiabilityPolicyId);
                if (!string.IsNullOrEmpty(offer.LogisticsOrderId))
                {
                    _ = new StableId(offer.LogisticsOrderId);
                }
                MilitaryLogisticsOrderState linkedOrder = null;
                var hasLinkedOrder = !string.IsNullOrEmpty(
                        offer.LogisticsOrderId) &&
                    logisticsOrders.TryGetValue(
                        offer.LogisticsOrderId, out linkedOrder);
                var hasGoal = goals.TryGetValue(
                    offer.GoalId, out var goal);
                var requiredOfferQuantity = hasLinkedOrder
                    ? linkedOrder.DispatchedCargoQuantity
                    : hasGoal
                        ? goal.OutstandingCargoQuantity
                        : int.MaxValue;
                var isClosedOffer = offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.Withdrawn ||
                    offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.Expired ||
                    offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.GoalCancelled ||
                    offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.Completed;
                if (!hasGoal ||
                    goal.ChildGoalIds.Count != 0 ||
                    !Enum.IsDefined(
                        typeof(MilitaryLogisticsDelegationOfferStatus),
                        offer.Status) ||
                    !personIds.Contains(offer.CarrierPersonId) ||
                    !organizationIds.Contains(offer.CarrierOrganizationId) ||
                    !organizationIds.Contains(offer.LossBearerOrganizationId) ||
                    !batches.TryGetValue(
                        offer.SourceCargoBatchId,
                        out var cargoBatch) ||
                    cargoBatch.ProductDefinitionId != goal.ProductDefinitionId ||
                    !containers.TryGetValue(
                        offer.TransportInventoryContainerId,
                        out var transportContainer) ||
                    transportContainer.CarrierPersonId != offer.CarrierPersonId ||
                    transportContainer.OwnerOrganizationId !=
                        offer.CarrierOrganizationId ||
                    !locationIds.Contains(offer.OriginLocationId) ||
                    !routeIds.Contains(offer.RouteId) ||
                    !RouteConnects(
                        FindRoute(Routes, offer.RouteId),
                        offer.OriginLocationId,
                        goal.DestinationLocationId) ||
                    offer.SubmittedDay < goal.CreatedDay ||
                    offer.SubmittedDay > AbsoluteDay ||
                    offer.ValidUntilDay < offer.SubmittedDay ||
                    offer.ValidUntilDay > goal.DeadlineDay ||
                    offer.ClosedDay < -1 ||
                    offer.ClosedDay > AbsoluteDay ||
                    isClosedOffer &&
                        offer.ClosedDay < offer.SubmittedDay ||
                    !isClosedOffer &&
                        offer.ClosedDay != -1 ||
                    (offer.Status ==
                         MilitaryLogisticsDelegationOfferStatus.Selected ||
                     offer.Status ==
                         MilitaryLogisticsDelegationOfferStatus.Completed) !=
                        hasLinkedOrder ||
                    hasLinkedOrder &&
                    (linkedOrder.TargetArmyId != goal.TargetArmyId ||
                     linkedOrder.IssuerPersonId != goal.IssuerPersonId ||
                     linkedOrder.LiabilityPolicyId !=
                        offer.LiabilityPolicyId ||
                     linkedOrder.CargoProductDefinitionId !=
                        goal.ProductDefinitionId ||
                     linkedOrder.FinalDestinationLocationId !=
                        goal.DestinationLocationId) ||
                    offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.Completed &&
                        !goal.CompletedLogisticsOrderIds.Contains(
                            offer.LogisticsOrderId) ||
                    offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.Selected &&
                        (goal.Status !=
                             MilitaryLogisticsDelegationStatus.Dispatched ||
                         goal.SelectedOfferId != offer.Id ||
                         goal.LogisticsOrderId != offer.LogisticsOrderId) ||
                    offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.GoalCancelled &&
                        goal.Status !=
                            MilitaryLogisticsDelegationStatus.Cancelled ||
                    goal.Status ==
                        MilitaryLogisticsDelegationStatus.Cancelled &&
                        offer.Status ==
                            MilitaryLogisticsDelegationOfferStatus.Active ||
                    offer.AvailableCargoQuantity < requiredOfferQuantity ||
                    offer.ConvoyProvisionQuantity < 0 ||
                    offer.DailyConvoyProvisionUse <= 0 ||
                    offer.UnitPrice < 0 ||
                    offer.LiabilityPolicyId !=
                        MilitaryLogisticsLiabilityPolicyIds.BuyerRetainsRisk &&
                    offer.LiabilityPolicyId !=
                        MilitaryLogisticsLiabilityPolicyIds
                            .LossBearerCompensates &&
                    offer.LiabilityPolicyId !=
                        MilitaryLogisticsLiabilityPolicyIds
                            .LegacyNoRetroactiveSettlement ||
                    offer.LiabilityPolicyId ==
                        MilitaryLogisticsLiabilityPolicyIds.BuyerRetainsRisk &&
                        offer.LossBearerOrganizationId !=
                            FindArmy(Armies, goal.TargetArmyId)
                                .OrganizationId ||
                    offer.LiabilityPolicyId ==
                        MilitaryLogisticsLiabilityPolicyIds
                            .LossBearerCompensates &&
                        offer.LossBearerOrganizationId !=
                            offer.CarrierOrganizationId ||
                    offer.ConvoyProvisionQuantity > 0 &&
                    (!batches.TryGetValue(
                         offer.SourceProvisionBatchId,
                         out var provisionBatch) ||
                     provisionBatch.OwnerOrganizationId !=
                         offer.CarrierOrganizationId) ||
                    offer.AcquisitionMethodId !=
                        MilitarySupplyAcquisitionMethodIds.CommercialPurchase &&
                    offer.AcquisitionMethodId !=
                        MilitarySupplyAcquisitionMethodIds.InternalDepotTransfer ||
                    offer.AcquisitionMethodId ==
                        MilitarySupplyAcquisitionMethodIds.CommercialPurchase &&
                    (offer.UnitPrice <= 0 ||
                     cargoBatch.OwnerOrganizationId ==
                        FindArmy(Armies, goal.TargetArmyId).OrganizationId) ||
                    offer.AcquisitionMethodId ==
                        MilitarySupplyAcquisitionMethodIds.InternalDepotTransfer &&
                    (offer.UnitPrice != 0 ||
                     cargoBatch.OwnerOrganizationId !=
                        FindArmy(Armies, goal.TargetArmyId).OrganizationId))
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics delegation offer {offer.Id}.");
                }

                offers.Add(offer.Id, offer);
                if (offer.Status ==
                    MilitaryLogisticsDelegationOfferStatus.Selected)
                {
                    selectedOfferCounts.TryGetValue(
                        offer.GoalId, out var selectedCount);
                    selectedOfferCounts[offer.GoalId] = selectedCount + 1;
                }
            }

            foreach (var pair in goals)
            {
                var goal = pair.Value;
                selectedOfferCounts.TryGetValue(goal.Id, out var selectedCount);
                var completedOrderIds = new HashSet<string>(
                    StringComparer.Ordinal);
                long expectedCommittedCost = 0;
                var completedReceipt = 0;
                var validHistory = true;
                for (var i = 0;
                     i < goal.CompletedLogisticsOrderIds.Count;
                     i++)
                {
                    var orderId = goal.CompletedLogisticsOrderIds[i];
                    if (!completedOrderIds.Add(orderId) ||
                        !logisticsOrders.TryGetValue(
                            orderId, out var completedOrder) ||
                        completedOrder.Status !=
                            MilitaryLogisticsStatus.Delivered ||
                        completedOrder.TargetArmyId != goal.TargetArmyId ||
                        completedOrder.IssuerPersonId != goal.IssuerPersonId ||
                        completedOrder.CargoProductDefinitionId !=
                            goal.ProductDefinitionId ||
                        completedOrder.FinalDestinationLocationId !=
                            goal.DestinationLocationId)
                    {
                        validHistory = false;
                        break;
                    }

                    var completedOfferCount = 0;
                    foreach (var offerPair in offers)
                    {
                        var completedOffer = offerPair.Value;
                        if (completedOffer.GoalId == goal.Id &&
                            completedOffer.LogisticsOrderId == orderId)
                        {
                            completedOfferCount++;
                            validHistory &= completedOffer.Status ==
                                MilitaryLogisticsDelegationOfferStatus.Completed;
                        }
                    }
                    validHistory &= completedOfferCount == 1;
                    completedReceipt = checked(
                        completedReceipt +
                        completedOrder.DeliveredCargoQuantity);
                    expectedCommittedCost = checked(
                        expectedCommittedCost + completedOrder.TotalPaid);
                }

                if (goal.ChildGoalIds.Count == 0)
                {
                    validHistory &= completedReceipt ==
                        goal.ReceivedCargoQuantity;
                }
                else
                {
                    validHistory &=
                        goal.CompletedLogisticsOrderIds.Count == 0;
                }

                var hasActiveDispatch = goal.Status ==
                    MilitaryLogisticsDelegationStatus.Dispatched;
                if (hasActiveDispatch)
                {
                    validHistory &= selectedCount == 1 &&
                        offers.TryGetValue(
                            goal.SelectedOfferId, out var selectedOffer) &&
                        selectedOffer.GoalId == goal.Id &&
                        selectedOffer.Status ==
                            MilitaryLogisticsDelegationOfferStatus.Selected &&
                        selectedOffer.LogisticsOrderId ==
                            goal.LogisticsOrderId &&
                        logisticsOrders.TryGetValue(
                            goal.LogisticsOrderId, out var logisticsOrder) &&
                        logisticsOrder.TargetArmyId == goal.TargetArmyId &&
                        logisticsOrder.IssuerPersonId == goal.IssuerPersonId &&
                        logisticsOrder.CargoProductDefinitionId ==
                            goal.ProductDefinitionId &&
                        logisticsOrder.FinalDestinationLocationId ==
                            goal.DestinationLocationId &&
                        logisticsOrder.DispatchedCargoQuantity ==
                            goal.OutstandingCargoQuantity;
                    if (logisticsOrders.TryGetValue(
                            goal.LogisticsOrderId, out var activeOrder))
                    {
                        expectedCommittedCost = checked(
                            expectedCommittedCost + activeOrder.TotalPaid);
                    }
                }
                else
                {
                    validHistory &= selectedCount == 0 &&
                        string.IsNullOrEmpty(goal.SelectedOfferId) &&
                        string.IsNullOrEmpty(goal.LogisticsOrderId);
                }

                validHistory &= goal.CommittedCost == expectedCommittedCost;
                if (!validHistory)
                {
                    throw new InvalidOperationException(
                        $"Military logistics delegation link diverged for {goal.Id}.");
                }
            }

            var settledOrderIds = new HashSet<string>(StringComparer.Ordinal);
            var compensationByGoal = new Dictionary<string, long>(
                StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryLogisticsLiabilitySettlements.Count;
                 i++)
            {
                var settlement =
                    MilitaryLogisticsLiabilitySettlements[i] ??
                    throw new InvalidOperationException(
                        "A military logistics liability settlement cannot be null.");
                _ = new StableId(settlement.LiabilityPolicyId);
                var hasGoal = goals.TryGetValue(
                    settlement.GoalId, out var goal);
                var hasOrder = logisticsOrders.TryGetValue(
                    settlement.LogisticsOrderId, out var order);
                var expectedDue = hasOrder &&
                    order.LiabilityPolicyId ==
                        MilitaryLogisticsLiabilityPolicyIds
                            .LossBearerCompensates &&
                    order.LossBearerOrganizationId !=
                        order.BuyerOrganizationId &&
                    order.UnitPrice > 0
                        ? Math.Min(
                            order.TotalPaid,
                            checked(order.UnitPrice *
                                (order.NaturalLossQuantity +
                                 order.HostileLossQuantity)))
                        : 0;
                var validStatus = settlement.OutstandingAmount == 0 &&
                        settlement.Status ==
                            MilitaryLogisticsLiabilitySettlementStatus.Settled ||
                    settlement.OutstandingAmount > 0 &&
                        settlement.Status ==
                            MilitaryLogisticsLiabilitySettlementStatus.InArrears;
                if (!hasGoal || !hasOrder ||
                    !settledOrderIds.Add(settlement.LogisticsOrderId) ||
                    !goal.CompletedLogisticsOrderIds.Contains(order.Id) ||
                    order.Status != MilitaryLogisticsStatus.Delivered ||
                    settlement.LiabilityPolicyId !=
                        order.LiabilityPolicyId ||
                    settlement.PayerOrganizationId !=
                        order.LossBearerOrganizationId ||
                    settlement.PayeeOrganizationId !=
                        order.BuyerOrganizationId ||
                    !organizationIds.Contains(
                        settlement.PayerOrganizationId) ||
                    !organizationIds.Contains(
                        settlement.PayeeOrganizationId) ||
                    settlement.NaturalLossQuantity !=
                        order.NaturalLossQuantity ||
                    settlement.HostileLossQuantity !=
                        order.HostileLossQuantity ||
                    settlement.UnitValue != order.UnitPrice ||
                    settlement.AmountDue != expectedDue ||
                    settlement.AmountPaid < 0 ||
                    settlement.AmountPaid > settlement.AmountDue ||
                    settlement.OutstandingAmount !=
                        settlement.AmountDue - settlement.AmountPaid ||
                    settlement.CreatedDay < order.DeliveredDay ||
                    settlement.CreatedDay > AbsoluteDay ||
                    settlement.LastPaymentDay < -1 ||
                    settlement.LastPaymentDay > AbsoluteDay ||
                    settlement.AmountPaid == 0 &&
                        settlement.LastPaymentDay != -1 ||
                    settlement.AmountPaid > 0 &&
                        settlement.LastPaymentDay < settlement.CreatedDay ||
                    !Enum.IsDefined(
                        typeof(MilitaryLogisticsLiabilitySettlementStatus),
                        settlement.Status) ||
                    !validStatus)
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics liability settlement " +
                        $"{settlement.Id}.");
                }

                compensationByGoal.TryGetValue(
                    goal.Id, out var compensation);
                compensationByGoal[goal.Id] = checked(
                    compensation + settlement.AmountPaid);
            }

            foreach (var pair in goals)
            {
                var goal = pair.Value;
                compensationByGoal.TryGetValue(
                    goal.Id, out var compensation);
                var validSettlementHistory =
                    compensation == goal.CompensationReceived;
                for (var i = 0;
                     i < goal.CompletedLogisticsOrderIds.Count;
                     i++)
                {
                    var order = logisticsOrders[
                        goal.CompletedLogisticsOrderIds[i]];
                    if (order.LiabilityPolicyId !=
                            MilitaryLogisticsLiabilityPolicyIds
                                .LegacyNoRetroactiveSettlement &&
                        !settledOrderIds.Contains(order.Id))
                    {
                        validSettlementHistory = false;
                    }
                }

                if (!validSettlementHistory)
                {
                    throw new InvalidOperationException(
                        $"Military logistics liability history diverged for " +
                        $"{goal.Id}.");
                }
            }

            for (var i = 0;
                 i < MilitaryLogisticsDelegationReports.Count;
                 i++)
            {
                var report = MilitaryLogisticsDelegationReports[i] ??
                    throw new InvalidOperationException(
                        "A military logistics delegation report cannot be null.");
                _ = new StableId(report.TypeId);
                var isExceptionType = report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.NoOffer ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .OfferInvalidated ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .BudgetExceeded ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.AuthorityLost ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.DeadlineExpired ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.DispatchRejected ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .AssigneeUnavailable ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.ChildException ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.AllocationGap ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .DeliveryShortfall ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .LiabilityArrears ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .ReplacementAuthorizationRequired;
                var isNormalType = report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.GoalCreated ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.OfferSubmitted ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.Dispatched ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.OfferWithdrawn ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.OfferExpired ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.Progress ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.Fulfilled ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.SubgoalCreated ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .DelegatedProgress ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds.GoalCancelled ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .OfferClosedByCancellation ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .AllocationRecovered ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .ReplacementGoalCreated ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .SubgoalReassigned ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .AttemptCompleted ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .SupplementalDispatched ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .LiabilitySettled ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .LiabilityPayment ||
                    report.TypeId ==
                        MilitaryLogisticsDelegationReportTypeIds
                            .ReplacementAuthorized;
                if (!goals.TryGetValue(report.GoalId, out var goal) ||
                    !personIds.Contains(report.ActorPersonId) ||
                    report.Day < goal.CreatedDay || report.Day > AbsoluteDay ||
                    !isExceptionType && !isNormalType ||
                    report.IsException != isExceptionType ||
                    !string.IsNullOrEmpty(report.RelatedOfferId) &&
                    (!offers.TryGetValue(
                         report.RelatedOfferId, out var relatedOffer) ||
                     relatedOffer.GoalId != goal.Id) ||
                    !string.IsNullOrEmpty(report.LogisticsOrderId) &&
                    !logisticsOrders.ContainsKey(report.LogisticsOrderId) ||
                    !string.IsNullOrEmpty(report.RelatedGoalId) &&
                    (!goals.TryGetValue(
                         report.RelatedGoalId, out var relatedGoal) ||
                     relatedGoal.ParentGoalId != goal.Id &&
                     goal.ParentGoalId != relatedGoal.Id) ||
                    string.IsNullOrWhiteSpace(report.Summary))
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics delegation report {report.Id}.");
                }
            }
        }

        private static void ValidateMilitaryLogisticsDelegationHierarchy(
            IDictionary<string, MilitaryLogisticsDelegationGoalState> goals)
        {
            foreach (var pair in goals)
            {
                var goal = pair.Value;
                if (string.IsNullOrEmpty(goal.ParentGoalId))
                {
                    if (goal.DelegationDepth != 0 ||
                        goal.AssigneePersonId != goal.IssuerPersonId ||
                        !string.IsNullOrEmpty(goal.DelegatedByPersonId) ||
                        goal.AssigneeAuthorityAtDelegation !=
                            MilitaryAuthorityLevel.Army)
                    {
                        throw new InvalidOperationException(
                            $"Invalid root military logistics goal {goal.Id}.");
                    }
                }
                else
                {
                    if (!goals.TryGetValue(
                            goal.ParentGoalId, out var parent) ||
                        goal.DelegationDepth != parent.DelegationDepth + 1 ||
                        goal.DelegationDepth >
                            MilitaryLogisticsDelegationContract
                                .MaximumDelegationDepth ||
                        goal.DelegatedByPersonId != parent.AssigneePersonId ||
                        goal.AssigneeAuthorityAtDelegation >=
                            parent.AssigneeAuthorityAtDelegation ||
                        !parent.ChildGoalIds.Contains(goal.Id) ||
                        goal.IssuerPersonId != parent.IssuerPersonId ||
                        goal.TargetArmyId != parent.TargetArmyId ||
                        goal.DestinationLocationId !=
                            parent.DestinationLocationId ||
                        goal.ProductDefinitionId != parent.ProductDefinitionId ||
                        goal.CargoConsumptionPolicyId !=
                            parent.CargoConsumptionPolicyId ||
                        goal.RiskPolicyId != parent.RiskPolicyId ||
                        goal.ThreatOrganizationId !=
                            parent.ThreatOrganizationId ||
                        goal.MaximumUnitPrice > parent.MaximumUnitPrice ||
                        goal.DeadlineDay > parent.DeadlineDay)
                    {
                        throw new InvalidOperationException(
                            $"Military logistics child goal {goal.Id} " +
                            "diverged from its parent contract.");
                    }
                }

                if (!string.IsNullOrEmpty(goal.ReplacesGoalId))
                {
                    if (!goals.TryGetValue(
                            goal.ReplacesGoalId, out var replaced) ||
                        replaced.Status !=
                            MilitaryLogisticsDelegationStatus.Cancelled ||
                        replaced.ParentGoalId != goal.ParentGoalId ||
                        !replaced.ReplacementGoalIds.Contains(goal.Id))
                    {
                        throw new InvalidOperationException(
                            $"Invalid replaced allocation on military " +
                            $"logistics goal {goal.Id}.");
                    }
                }

                var replacementIds = new HashSet<string>(
                    StringComparer.Ordinal);
                long replacementQuantity = 0;
                long replacementBudget = 0;
                for (var i = 0; i < goal.ReplacementGoalIds.Count; i++)
                {
                    var replacementId = goal.ReplacementGoalIds[i];
                    if (!replacementIds.Add(replacementId) ||
                        !goals.TryGetValue(
                            replacementId, out var replacement) ||
                        goal.Status !=
                            MilitaryLogisticsDelegationStatus.Cancelled ||
                        replacement.ParentGoalId != goal.ParentGoalId ||
                        replacement.ReplacesGoalId != goal.Id)
                    {
                        throw new InvalidOperationException(
                            $"Invalid replacement index on military " +
                            $"logistics goal {goal.Id}.");
                    }

                    replacementQuantity = checked(
                        replacementQuantity +
                        replacement.RequestedCargoQuantity);
                    replacementBudget = checked(
                        replacementBudget + replacement.BudgetLimit);
                }
                if (goal.ReplacementGoalIds.Count != 0 &&
                    (replacementQuantity != goal.RequestedCargoQuantity ||
                     replacementBudget > goal.BudgetLimit))
                {
                    throw new InvalidOperationException(
                        $"Replacement allocation diverged for military " +
                        $"logistics goal {goal.Id}.");
                }

                if (goal.ChildGoalIds.Count == 0)
                {
                    if (goal.UnassignedCargoQuantity != 0 ||
                        goal.AvailableBudgetReserve != 0 ||
                        goal.Status ==
                            MilitaryLogisticsDelegationStatus.Delegated)
                    {
                        throw new InvalidOperationException(
                            $"Military logistics leaf goal {goal.Id} has an " +
                            "invalid aggregate status.");
                    }
                    continue;
                }

                if (goal.Status != MilitaryLogisticsDelegationStatus.Delegated &&
                    goal.Status !=
                        MilitaryLogisticsDelegationStatus.NeedsAttention &&
                    goal.Status != MilitaryLogisticsDelegationStatus.Fulfilled &&
                    goal.Status != MilitaryLogisticsDelegationStatus.Expired &&
                    goal.Status != MilitaryLogisticsDelegationStatus.Cancelled)
                {
                    throw new InvalidOperationException(
                        $"Invalid delegated parent goal {goal.Id}.");
                }

                var childIds = new HashSet<string>(StringComparer.Ordinal);
                long totalQuantity = 0;
                long totalBudget = 0;
                long totalReceived = 0;
                var allFulfilled = true;
                var activeChildCount = 0;
                for (var i = 0; i < goal.ChildGoalIds.Count; i++)
                {
                    var childId = goal.ChildGoalIds[i];
                    if (!childIds.Add(childId) ||
                        !goals.TryGetValue(childId, out var child) ||
                        child.ParentGoalId != goal.Id)
                    {
                        throw new InvalidOperationException(
                            $"Invalid child index on military logistics goal " +
                            $"{goal.Id}.");
                    }

                    if (child.Status ==
                        MilitaryLogisticsDelegationStatus.Cancelled)
                    {
                        continue;
                    }

                    activeChildCount++;
                    totalQuantity = checked(
                        totalQuantity + child.RequestedCargoQuantity);
                    totalBudget = checked(totalBudget + child.BudgetLimit);
                    totalReceived = checked(
                        totalReceived + child.ReceivedCargoQuantity);
                    allFulfilled &= child.Status ==
                        MilitaryLogisticsDelegationStatus.Fulfilled;
                }

                if (activeChildCount >
                        MilitaryLogisticsDelegationContract
                            .MaximumDirectSubgoals ||
                    totalQuantity + goal.UnassignedCargoQuantity !=
                        goal.RequestedCargoQuantity ||
                    totalBudget + goal.AvailableBudgetReserve !=
                        goal.BudgetLimit ||
                    totalReceived != goal.ReceivedCargoQuantity ||
                    goal.Status ==
                        MilitaryLogisticsDelegationStatus.Delegated &&
                        goal.UnassignedCargoQuantity != 0 ||
                    goal.Status ==
                        MilitaryLogisticsDelegationStatus.Fulfilled &&
                    (goal.UnassignedCargoQuantity != 0 ||
                     !allFulfilled ||
                     goal.FulfillmentPolicyId ==
                         MilitaryLogisticsDelegationFulfillmentPolicyIds
                             .FullReceiptRequired &&
                         goal.OutstandingCargoQuantity != 0))
                {
                    throw new InvalidOperationException(
                        $"Military logistics child allocation diverged for " +
                        $"{goal.Id}.");
                }
            }
        }

        private void ValidateMilitaryLogistics(
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
            var journeys = new Dictionary<string, JourneyState>(
                StringComparer.Ordinal);
            var marches = new Dictionary<string, ArmyMarchState>(
                StringComparer.Ordinal);
            for (var i = 0; i < ProductBatches.Count; i++)
            {
                batches.Add(ProductBatches[i].Id, ProductBatches[i]);
            }

            for (var i = 0; i < InventoryContainers.Count; i++)
            {
                containers.Add(InventoryContainers[i].Id, InventoryContainers[i]);
            }

            for (var i = 0; i < Journeys.Count; i++)
            {
                journeys.Add(Journeys[i].Id, Journeys[i]);
            }

            for (var i = 0; i < ArmyMarches.Count; i++)
            {
                marches.Add(ArmyMarches[i].Id, ArmyMarches[i]);
            }

            var orders = new Dictionary<string, MilitaryLogisticsOrderState>(
                StringComparer.Ordinal);
            var transportLoads = new Dictionary<string, long>(
                StringComparer.Ordinal);
            for (var i = 0; i < ProductBatches.Count; i++)
            {
                var batch = ProductBatches[i];
                if (string.IsNullOrEmpty(batch.InventoryContainerId))
                {
                    continue;
                }

                AddLong(
                    transportLoads,
                    batch.InventoryContainerId,
                    checked(batch.Quantity * batch.UnitWeight));
            }

            for (var i = 0; i < MilitaryLogisticsOrders.Count; i++)
            {
                var order = MilitaryLogisticsOrders[i] ??
                    throw new InvalidOperationException(
                        "A military logistics order cannot be null.");
                orders.Add(order.Id, order);
                _ = new StableId(order.AcquisitionMethodId);
                _ = new StableId(order.CargoConsumptionPolicyId);
                _ = new StableId(order.LiabilityPolicyId);
                var hasJourney = journeys.TryGetValue(
                    order.JourneyId, out var journey);
                var hasMarch = marches.TryGetValue(
                    order.ArmyMarchId, out var march);
                var validJourney = hasJourney && !hasMarch &&
                    string.IsNullOrEmpty(order.ArmyMarchId) &&
                    journey.PersonId == order.CarrierPersonId &&
                    journey.RouteId == order.RouteId &&
                    journey.OriginLocationId == order.OriginLocationId &&
                    journey.DestinationLocationId == order.DestinationLocationId;
                var validMarch = hasMarch && !hasJourney &&
                    string.IsNullOrEmpty(order.JourneyId) &&
                    march.ArmyId == order.TargetArmyId &&
                    march.RouteId == order.RouteId &&
                    march.OriginLocationId == order.OriginLocationId &&
                    march.DestinationLocationId == order.DestinationLocationId;
                var validStatus =
                    order.Status == MilitaryLogisticsStatus.InTransit &&
                    order.DeliveredDay == -1 &&
                    (validJourney || validMarch) ||
                    order.Status == MilitaryLogisticsStatus.AwaitingHandoff &&
                    order.DeliveredDay == -1 &&
                    !hasJourney && !hasMarch ||
                    order.Status == MilitaryLogisticsStatus.AwaitingArmy &&
                    order.DeliveredDay == -1 &&
                    !hasJourney && !hasMarch ||
                    order.Status == MilitaryLogisticsStatus.Delivered &&
                    order.DeliveredDay >= order.CreatedDay &&
                    order.DeliveredDay <= AbsoluteDay &&
                    !hasJourney && !hasMarch;
                var hasCargoBatch = batches.TryGetValue(
                    order.SourceCargoBatchId, out var cargoBatch);
                var hasProvisionBatch =
                    string.IsNullOrEmpty(order.SourceProvisionBatchId) ||
                    batches.TryGetValue(
                        order.SourceProvisionBatchId, out var provisionBatch);
                var hasSourceContainer = containers.TryGetValue(
                    order.SourceInventoryContainerId,
                    out var sourceContainer);
                var hasTransportContainer = containers.TryGetValue(
                    order.TransportInventoryContainerId,
                    out var transportContainer);
                var cargoBalanced = order.DispatchedCargoQuantity ==
                    order.RemainingCargoQuantity +
                    order.DeliveredCargoQuantity +
                    order.NaturalLossQuantity +
                    order.HostileLossQuantity +
                    order.CargoConsumedAsProvisionsQuantity;
                var provisionsBalanced = order.ConvoyProvisionsLoaded ==
                    order.ConvoyProvisionsRemaining +
                    order.ConvoyProvisionsConsumed;
                if (!Enum.IsDefined(
                        typeof(MilitaryLogisticsStatus), order.Status) ||
                    !personIds.Contains(order.IssuerPersonId) ||
                    !personIds.Contains(order.CarrierPersonId) ||
                    !organizationIds.Contains(order.BuyerOrganizationId) ||
                    !organizationIds.Contains(order.SourceOrganizationId) ||
                    !organizationIds.Contains(order.CarrierOrganizationId) ||
                    !organizationIds.Contains(order.LossBearerOrganizationId) ||
                    order.LiabilityPolicyId !=
                        MilitaryLogisticsLiabilityPolicyIds.BuyerRetainsRisk &&
                    order.LiabilityPolicyId !=
                        MilitaryLogisticsLiabilityPolicyIds
                            .LossBearerCompensates &&
                    order.LiabilityPolicyId !=
                        MilitaryLogisticsLiabilityPolicyIds
                            .LegacyNoRetroactiveSettlement ||
                    !armyIds.Contains(order.TargetArmyId) ||
                    FindArmy(Armies, order.TargetArmyId).OrganizationId !=
                        order.BuyerOrganizationId ||
                    !hasCargoBatch ||
                    cargoBatch.ProductDefinitionId !=
                        order.CargoProductDefinitionId ||
                    cargoBatch.OwnerOrganizationId !=
                        order.SourceOrganizationId ||
                    !hasProvisionBatch ||
                    !hasSourceContainer ||
                    cargoBatch.InventoryContainerId != sourceContainer.Id ||
                    !hasTransportContainer ||
                    transportContainer.CarrierPersonId !=
                        order.CarrierPersonId ||
                    transportContainer.OwnerOrganizationId !=
                        order.CarrierOrganizationId ||
                    !routeIds.Contains(order.RouteId) ||
                    !locationIds.Contains(order.OriginLocationId) ||
                    !locationIds.Contains(order.DestinationLocationId) ||
                    !locationIds.Contains(order.FinalDestinationLocationId) ||
                    order.PlannedLegCount < 0 ||
                    order.CurrentLegSequence < 0 ||
                    order.PlannedLegCount == 0 &&
                    order.CurrentLegSequence != 0 ||
                    order.PlannedLegCount > 0 &&
                    order.CurrentLegSequence >= order.PlannedLegCount ||
                    order.CreatedDay < 0 || order.CreatedDay > AbsoluteDay ||
                    order.DispatchedCargoQuantity <= 0 ||
                    order.RemainingCargoQuantity < 0 ||
                    order.DeliveredCargoQuantity < 0 ||
                    order.NaturalLossQuantity < 0 ||
                    order.HostileLossQuantity < 0 ||
                    order.RecoveredCargoQuantity < 0 ||
                    order.CargoConsumedAsProvisionsQuantity < 0 ||
                    !cargoBalanced ||
                    order.ConvoyProvisionsLoaded < 0 ||
                    order.ConvoyProvisionsRemaining < 0 ||
                    order.ConvoyProvisionsConsumed < 0 ||
                    !provisionsBalanced ||
                    order.DailyConvoyProvisionUse <= 0 ||
                    order.DailyNaturalLossBasisPoints < 0 ||
                    order.DailyNaturalLossBasisPoints > 10_000 ||
                    order.NaturalLossRemainderBasisPoints < 0 ||
                    order.NaturalLossRemainderBasisPoints >= 10_000 ||
                    order.CargoUnitWeightAtDispatch <= 0 ||
                    order.ConvoyProvisionsLoaded > 0 &&
                    order.ConvoyProvisionUnitWeightAtDispatch <= 0 ||
                    order.CargoQualityDimensionsAtDispatch == null ||
                    order.CargoQualityDimensionsAtDispatch.Count == 0 ||
                    order.CargoQualityBasisPointsAtDispatch !=
                    ProductQualityRules.CalculateSummary(
                        order.CargoQualityDimensionsAtDispatch) ||
                    order.CargoFreshnessBasisPointsAtDispatch < 0 ||
                    order.CargoFreshnessBasisPointsAtDispatch > 10_000 ||
                    order.UnitPrice < 0 || order.TotalPaid < 0 ||
                    order.TotalPaid != checked(
                        order.UnitPrice * order.DispatchedCargoQuantity) ||
                    order.OriginPublicOrderDelta > 0 ||
                    !validStatus)
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics order {order.Id}: " +
                        $"status={validStatus}/{order.Status}, " +
                        $"people={personIds.Contains(order.IssuerPersonId)}/" +
                        $"{personIds.Contains(order.CarrierPersonId)}, " +
                        $"organizations=" +
                        $"{organizationIds.Contains(order.BuyerOrganizationId)}/" +
                        $"{organizationIds.Contains(order.SourceOrganizationId)}/" +
                        $"{organizationIds.Contains(order.CarrierOrganizationId)}/" +
                        $"{organizationIds.Contains(order.LossBearerOrganizationId)}, " +
                        $"army={armyIds.Contains(order.TargetArmyId)}, " +
                        $"batches={hasCargoBatch}/{hasProvisionBatch}, " +
                        $"containers={hasSourceContainer}/{hasTransportContainer}, " +
                        $"cargo={cargoBalanced} " +
                        $"({order.DispatchedCargoQuantity}/" +
                        $"{order.RemainingCargoQuantity}/" +
                        $"{order.DeliveredCargoQuantity}/" +
                        $"{order.NaturalLossQuantity}/" +
                        $"{order.CargoConsumedAsProvisionsQuantity}), " +
                        $"provisions={provisionsBalanced} " +
                        $"({order.ConvoyProvisionsLoaded}/" +
                        $"{order.ConvoyProvisionsRemaining}/" +
                        $"{order.ConvoyProvisionsConsumed}), " +
                        $"quality={order.CargoQualityBasisPointsAtDispatch}/" +
                        $"{ProductQualityRules.CalculateSummary(order.CargoQualityDimensionsAtDispatch)}, " +
                        $"money={order.TotalPaid}/" +
                        $"{checked(order.UnitPrice * order.DispatchedCargoQuantity)}.");
                }

                AddLong(
                    transportLoads,
                    order.TransportInventoryContainerId,
                    checked(
                        (long)order.RemainingCargoQuantity *
                        order.CargoUnitWeightAtDispatch +
                        (long)order.ConvoyProvisionsRemaining *
                        order.ConvoyProvisionUnitWeightAtDispatch));
            }

            var legCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var legSequences = new HashSet<string>(StringComparer.Ordinal);
            var legsById = new Dictionary<string, MilitaryLogisticsLegState>(
                StringComparer.Ordinal);
            var plannedProvisionReservations =
                new Dictionary<string, long>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryLogisticsLegs.Count; i++)
            {
                var leg = MilitaryLogisticsLegs[i] ??
                    throw new InvalidOperationException(
                        "A military logistics leg cannot be null.");
                _ = new StableId(leg.RiskPolicyId);
                var validRisk =
                    leg.RiskPolicyId == MilitaryLogisticsRiskPolicyIds.None &&
                    string.IsNullOrEmpty(leg.ThreatOrganizationId) ||
                    leg.RiskPolicyId ==
                        MilitaryLogisticsRiskPolicyIds.Standard &&
                    organizationIds.Contains(leg.ThreatOrganizationId);
                if (!orders.TryGetValue(leg.LogisticsOrderId, out var order) ||
                    !Enum.IsDefined(
                        typeof(MilitaryLogisticsLegStatus), leg.Status) ||
                    leg.Sequence < 0 ||
                    leg.Sequence >= order.PlannedLegCount ||
                    !legSequences.Add($"{order.Id}#{leg.Sequence}") ||
                    !personIds.Contains(leg.CarrierPersonId) ||
                    !organizationIds.Contains(leg.CarrierOrganizationId) ||
                    !containers.TryGetValue(
                        leg.TransportInventoryContainerId,
                        out var legContainer) ||
                    legContainer.CarrierPersonId != leg.CarrierPersonId ||
                    legContainer.OwnerOrganizationId !=
                        leg.CarrierOrganizationId ||
                    !routeIds.Contains(leg.RouteId) ||
                    !locationIds.Contains(leg.OriginLocationId) ||
                    !locationIds.Contains(leg.DestinationLocationId) ||
                    !RouteConnects(
                        FindRoute(Routes, leg.RouteId),
                        leg.OriginLocationId,
                        leg.DestinationLocationId) ||
                    leg.PlannedProvisionQuantity < 0 ||
                    leg.LoadedProvisionQuantity < 0 ||
                    leg.ConsumedProvisionQuantity < 0 ||
                    leg.NaturalLossQuantity < 0 ||
                    leg.HostileLossQuantity < 0 ||
                    leg.RecoveredCargoQuantity < 0 ||
                    leg.CargoReceivedQuantity < 0 ||
                    leg.CargoTransferredQuantity < 0 ||
                    leg.CargoTransferredQuantity >
                        leg.CargoReceivedQuantity ||
                    leg.DailyProvisionUse <= 0 ||
                    !validRisk ||
                    leg.StartedDay < -1 || leg.StartedDay > AbsoluteDay ||
                    leg.CompletedDay < -1 || leg.CompletedDay > AbsoluteDay ||
                    leg.CompletedDay >= 0 && leg.StartedDay < 0 ||
                    leg.CompletedDay >= 0 &&
                        leg.CompletedDay < leg.StartedDay ||
                    leg.PlannedProvisionQuantity > 0 &&
                    leg.Status == MilitaryLogisticsLegStatus.Planned &&
                    (!batches.TryGetValue(
                        leg.ProvisionBatchId,
                        out var legProvisionBatch) ||
                     legProvisionBatch.OwnerOrganizationId !=
                        leg.CarrierOrganizationId ||
                     !containers.TryGetValue(
                        legProvisionBatch.InventoryContainerId,
                        out var legProvisionContainer) ||
                     legProvisionContainer.LocationId !=
                        leg.OriginLocationId))
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics leg {leg.Id}.");
                }

                var isCurrent = leg.Sequence == order.CurrentLegSequence;
                var validLegStatus =
                    leg.Sequence < order.CurrentLegSequence &&
                    leg.Status == MilitaryLogisticsLegStatus.Completed ||
                    leg.Sequence > order.CurrentLegSequence &&
                    leg.Status == MilitaryLogisticsLegStatus.Planned ||
                    isCurrent &&
                    order.Status == MilitaryLogisticsStatus.InTransit &&
                    leg.Status == MilitaryLogisticsLegStatus.InTransit ||
                    isCurrent &&
                    order.Status == MilitaryLogisticsStatus.AwaitingHandoff &&
                    leg.Status == MilitaryLogisticsLegStatus.AwaitingHandoff ||
                    isCurrent &&
                    order.Status == MilitaryLogisticsStatus.AwaitingArmy &&
                    leg.Status == MilitaryLogisticsLegStatus.AwaitingReceipt ||
                    isCurrent &&
                    order.Status == MilitaryLogisticsStatus.Delivered &&
                    leg.Status == MilitaryLogisticsLegStatus.Completed;
                if (!validLegStatus ||
                    isCurrent &&
                    (order.RouteId != leg.RouteId ||
                     order.OriginLocationId != leg.OriginLocationId ||
                     order.DestinationLocationId != leg.DestinationLocationId ||
                     order.CarrierPersonId != leg.CarrierPersonId ||
                     order.CarrierOrganizationId !=
                        leg.CarrierOrganizationId ||
                     order.TransportInventoryContainerId !=
                        leg.TransportInventoryContainerId) ||
                    leg.Sequence == order.PlannedLegCount - 1 &&
                    leg.DestinationLocationId !=
                        order.FinalDestinationLocationId)
                {
                    throw new InvalidOperationException(
                        $"Military logistics leg state diverged for {leg.Id}.");
                }

                legCounts.TryGetValue(order.Id, out var count);
                legCounts[order.Id] = count + 1;
                legsById.Add(leg.Id, leg);
                if (leg.Status == MilitaryLogisticsLegStatus.Planned &&
                    leg.PlannedProvisionQuantity > 0)
                {
                    AddLong(
                        plannedProvisionReservations,
                        leg.ProvisionBatchId,
                        leg.PlannedProvisionQuantity);
                }
            }

            foreach (var pair in orders)
            {
                legCounts.TryGetValue(pair.Key, out var count);
                if (pair.Value.PlannedLegCount != count)
                {
                    throw new InvalidOperationException(
                        $"Military logistics leg count diverged for {pair.Key}.");
                }
            }

            foreach (var pair in plannedProvisionReservations)
            {
                if (!batches.TryGetValue(pair.Key, out var batch) ||
                    batch.ReservedQuantity < pair.Value)
                {
                    throw new InvalidOperationException(
                        $"Military logistics provision reservation diverged for {pair.Key}.");
                }
            }

            var escortKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryLogisticsEscorts.Count; i++)
            {
                var escort = MilitaryLogisticsEscorts[i] ??
                    throw new InvalidOperationException(
                        "A military logistics escort cannot be null.");
                var hasLeg = legsById.TryGetValue(
                    escort.LogisticsLegId, out var leg);
                var hasJourney = journeys.TryGetValue(
                    escort.JourneyId, out var journey);
                var validStatus = hasLeg &&
                    escort.Status == MilitaryLogisticsEscortStatus.Planned &&
                    leg.Status == MilitaryLogisticsLegStatus.Planned &&
                    string.IsNullOrEmpty(escort.JourneyId) &&
                    escort.StartedDay == -1 && escort.ArrivedDay == -1 ||
                    hasLeg &&
                    escort.Status == MilitaryLogisticsEscortStatus.InTransit &&
                    leg.Status == MilitaryLogisticsLegStatus.InTransit &&
                    hasJourney &&
                    journey.PersonId == escort.PersonId &&
                    journey.RouteId == leg.RouteId &&
                    journey.DestinationLocationId == leg.DestinationLocationId &&
                    escort.StartedDay >= 0 && escort.ArrivedDay == -1 ||
                    hasLeg &&
                    escort.Status == MilitaryLogisticsEscortStatus.Arrived &&
                    leg.Status != MilitaryLogisticsLegStatus.Planned &&
                    !hasJourney && escort.StartedDay >= 0 &&
                    escort.ArrivedDay >= escort.StartedDay &&
                    escort.ArrivedDay <= AbsoluteDay;
                if (!hasLeg ||
                    !orders.ContainsKey(escort.LogisticsOrderId) ||
                    leg.LogisticsOrderId != escort.LogisticsOrderId ||
                    leg.Sequence != escort.LegSequence ||
                    !personIds.Contains(escort.PersonId) ||
                    !escortKeys.Add(
                        $"{escort.LogisticsLegId}#{escort.PersonId}") ||
                    escort.EscortPowerAtDeparture < 0 ||
                    !validStatus)
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics escort {escort.Id}.");
                }
            }

            var incidentsById = new Dictionary<string,
                MilitaryLogisticsIncidentState>(StringComparer.Ordinal);
            var incidentCustodyByOrder = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var incidentRecoveredByOrder = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var incidentCustodyByLeg = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var incidentRecoveredByLeg = new Dictionary<string, long>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryLogisticsIncidents.Count; i++)
            {
                var incident = MilitaryLogisticsIncidents[i] ??
                    throw new InvalidOperationException(
                        "A military logistics incident cannot be null.");
                _ = new StableId(incident.IncidentTypeId);
                _ = new StableId(incident.OutcomeId);
                var hasOrder = orders.TryGetValue(
                    incident.LogisticsOrderId, out var order);
                var hasLeg = legsById.TryGetValue(
                    incident.LogisticsLegId, out var leg);
                var validType = incident.IncidentTypeId ==
                    MilitaryLogisticsIncidentTypeIds.BanditAttack;
                var validOutcome = incident.OutcomeId ==
                    MilitaryLogisticsIncidentOutcomeIds.Avoided ||
                    incident.OutcomeId ==
                    MilitaryLogisticsIncidentOutcomeIds.Repelled ||
                    incident.OutcomeId ==
                    MilitaryLogisticsIncidentOutcomeIds.CargoSeized;
                var seizedOutcome = incident.OutcomeId ==
                    MilitaryLogisticsIncidentOutcomeIds.CargoSeized;
                var attackOccurred = incident.AttackRollBasisPoints <
                    incident.AttackChanceBasisPoints;
                var validResolution =
                    incident.OutcomeId ==
                        MilitaryLogisticsIncidentOutcomeIds.Avoided &&
                    !attackOccurred ||
                    incident.OutcomeId ==
                        MilitaryLogisticsIncidentOutcomeIds.Repelled &&
                    attackOccurred &&
                    incident.EscortPower >= incident.ThreatPower ||
                    seizedOutcome && attackOccurred &&
                    incident.EscortPower < incident.ThreatPower;
                if (!hasOrder || !hasLeg || !validType || !validOutcome ||
                    !validResolution ||
                    leg.LogisticsOrderId != order.Id ||
                    incident.RouteId != leg.RouteId ||
                    incident.ThreatOrganizationId !=
                        leg.ThreatOrganizationId ||
                    !organizationIds.Contains(
                        incident.ThreatOrganizationId) ||
                    incident.Day < order.CreatedDay ||
                    incident.Day > AbsoluteDay ||
                    incident.AttackChanceBasisPoints < 0 ||
                    incident.AttackChanceBasisPoints > 10_000 ||
                    incident.AttackRollBasisPoints < 0 ||
                    incident.AttackRollBasisPoints >= 10_000 ||
                    incident.EscortPower < 0 || incident.ThreatPower < 0 ||
                    incident.SeizedCargoQuantity < 0 ||
                    incident.RecoveredCargoQuantity < 0 ||
                    incident.RecoveredCargoQuantity >
                        incident.SeizedCargoQuantity ||
                    seizedOutcome != (incident.SeizedCargoQuantity > 0) ||
                    string.IsNullOrWhiteSpace(incident.Summary))
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics incident {incident.Id}.");
                }

                incidentsById.Add(incident.Id, incident);
                var custody = incident.SeizedCargoQuantity -
                    incident.RecoveredCargoQuantity;
                AddLong(incidentCustodyByOrder, order.Id, custody);
                AddLong(
                    incidentRecoveredByOrder,
                    order.Id,
                    incident.RecoveredCargoQuantity);
                AddLong(incidentCustodyByLeg, leg.Id, custody);
                AddLong(
                    incidentRecoveredByLeg,
                    leg.Id,
                    incident.RecoveredCargoQuantity);
            }

            foreach (var pair in orders)
            {
                incidentCustodyByOrder.TryGetValue(
                    pair.Key, out var custody);
                incidentRecoveredByOrder.TryGetValue(
                    pair.Key, out var recovered);
                if (custody != pair.Value.HostileLossQuantity ||
                    recovered != pair.Value.RecoveredCargoQuantity)
                {
                    throw new InvalidOperationException(
                        $"Hostile cargo custody diverged for {pair.Key}.");
                }
            }

            foreach (var pair in legsById)
            {
                incidentCustodyByLeg.TryGetValue(
                    pair.Key, out var custody);
                incidentRecoveredByLeg.TryGetValue(
                    pair.Key, out var recovered);
                if (custody != pair.Value.HostileLossQuantity ||
                    recovered != pair.Value.RecoveredCargoQuantity)
                {
                    throw new InvalidOperationException(
                        $"Hostile leg custody diverged for {pair.Key}.");
                }
            }

            var serviceById = new Dictionary<string, MilitaryServiceState>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryServices.Count; i++)
            {
                serviceById.Add(MilitaryServices[i].Id, MilitaryServices[i]);
            }
            var recoveryClashByIncident = new HashSet<string>(
                StringComparer.Ordinal);
            var initialClashByIncident = new HashSet<string>(
                StringComparer.Ordinal);
            var clashRecoveredByIncident = new Dictionary<string, long>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryLogisticsClashes.Count; i++)
            {
                var clash = MilitaryLogisticsClashes[i] ??
                    throw new InvalidOperationException(
                        "A military logistics clash cannot be null.");
                _ = new StableId(clash.TypeId);
                _ = new StableId(clash.OutcomeId);
                if (!incidentsById.TryGetValue(
                        clash.IncidentId, out var incident) ||
                    !orders.TryGetValue(
                        clash.LogisticsOrderId, out var order) ||
                    !legsById.TryGetValue(
                        clash.LogisticsLegId, out var leg) ||
                    incident.LogisticsOrderId != order.Id ||
                    incident.LogisticsLegId != leg.Id ||
                    !organizationIds.Contains(
                        clash.DefenderOrganizationId) ||
                    clash.Day < incident.Day || clash.Day > AbsoluteDay ||
                    clash.DefenderPersonIds == null ||
                    clash.DefenderPersonIds.Count == 0 ||
                    clash.DefenderPersonIds.Count > 20 ||
                    clash.DefenderPower < 0 || clash.ThreatPower <= 0 ||
                    clash.CargoRecoveredQuantity < 0 ||
                    clash.Injuries == null ||
                    string.IsNullOrWhiteSpace(clash.Summary))
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics clash {clash.Id}.");
                }

                var isInitial = clash.TypeId ==
                    MilitaryLogisticsClashTypeIds.InitialDefense;
                var isRecovery = clash.TypeId ==
                    MilitaryLogisticsClashTypeIds.RecoveryAttempt;
                var validInitial = isInitial &&
                    initialClashByIncident.Add(incident.Id) &&
                    string.IsNullOrEmpty(clash.IssuerPersonId) &&
                    clash.DefenderOrganizationId ==
                        order.CarrierOrganizationId &&
                    clash.DefenderPower == incident.EscortPower &&
                    clash.ThreatPower == incident.ThreatPower &&
                    clash.CargoRecoveredQuantity == 0 &&
                    (clash.OutcomeId ==
                        MilitaryLogisticsClashOutcomeIds.DefendersHeld &&
                     incident.OutcomeId ==
                        MilitaryLogisticsIncidentOutcomeIds.Repelled ||
                     clash.OutcomeId ==
                        MilitaryLogisticsClashOutcomeIds
                            .AttackersSeizedCargo &&
                     incident.OutcomeId ==
                        MilitaryLogisticsIncidentOutcomeIds.CargoSeized);
                var validRecovery = isRecovery &&
                    recoveryClashByIncident.Add(incident.Id) &&
                    personIds.Contains(clash.IssuerPersonId) &&
                    incident.OutcomeId ==
                        MilitaryLogisticsIncidentOutcomeIds.CargoSeized &&
                    clash.DefenderOrganizationId ==
                        FindArmy(Armies, order.TargetArmyId).OrganizationId &&
                    (clash.OutcomeId ==
                        MilitaryLogisticsClashOutcomeIds.CargoRecovered &&
                     clash.CargoRecoveredQuantity > 0 ||
                     clash.OutcomeId ==
                        MilitaryLogisticsClashOutcomeIds.RecoveryFailed &&
                     clash.CargoRecoveredQuantity == 0);
                if (!validInitial && !validRecovery)
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics clash resolution {clash.Id}.");
                }

                var clashPeople = new HashSet<string>(StringComparer.Ordinal);
                for (var participantIndex = 0;
                     participantIndex < clash.DefenderPersonIds.Count;
                     participantIndex++)
                {
                    var personId = clash.DefenderPersonIds[participantIndex];
                    _ = new StableId(personId);
                    if (!personIds.Contains(personId) ||
                        !clashPeople.Add(personId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid clash participant in {clash.Id}.");
                    }
                }

                var injuredPeople = new HashSet<string>(StringComparer.Ordinal);
                for (var injuryIndex = 0;
                     injuryIndex < clash.Injuries.Count;
                     injuryIndex++)
                {
                    var injury = clash.Injuries[injuryIndex];
                    var validService =
                        string.IsNullOrEmpty(injury.MilitaryServiceId) ||
                        serviceById.TryGetValue(
                            injury.MilitaryServiceId, out var service) &&
                        service.PersonId == injury.PersonId;
                    if (!clashPeople.Contains(injury.PersonId) ||
                        !injuredPeople.Add(injury.PersonId) ||
                        injury.HealthBeforeBasisPoints <=
                            injury.HealthAfterBasisPoints ||
                        injury.HealthBeforeBasisPoints > 10_000 ||
                        injury.HealthAfterBasisPoints < 1 ||
                        !validService)
                    {
                        throw new InvalidOperationException(
                            $"Invalid clash injury in {clash.Id}.");
                    }
                }

                AddLong(
                    clashRecoveredByIncident,
                    incident.Id,
                    clash.CargoRecoveredQuantity);
            }

            foreach (var pair in incidentsById)
            {
                clashRecoveredByIncident.TryGetValue(
                    pair.Key, out var recovered);
                if (recovered != pair.Value.RecoveredCargoQuantity)
                {
                    throw new InvalidOperationException(
                        $"Recovered clash custody diverged for {pair.Key}.");
                }
            }

            foreach (var pair in transportLoads)
            {
                if (containers.TryGetValue(pair.Key, out var container) &&
                    pair.Value > container.CapacityWeight)
                {
                    throw new InvalidOperationException(
                        $"Tracked freight exceeds container capacity for {pair.Key}.");
                }
            }

            var aggregates = new Dictionary<string, LogisticsLedgerBalance>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryLogisticsLedgerEntries.Count; i++)
            {
                var entry = MilitaryLogisticsLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A military logistics ledger entry cannot be null.");
                if (!orders.TryGetValue(
                        entry.LogisticsOrderId, out var order) ||
                    !Enum.IsDefined(
                        typeof(MilitaryLogisticsLedgerType), entry.Type) ||
                    entry.Day < order.CreatedDay || entry.Day > AbsoluteDay ||
                    !personIds.Contains(entry.ActorPersonId) ||
                    string.IsNullOrWhiteSpace(entry.Summary))
                {
                    throw new InvalidOperationException(
                        $"Invalid military logistics ledger entry {entry.Id}.");
                }

                if (!aggregates.TryGetValue(
                        order.Id, out var aggregate))
                {
                    aggregate = new LogisticsLedgerBalance();
                    aggregates.Add(order.Id, aggregate);
                }

                aggregate.Apply(entry);
            }

            foreach (var pair in orders)
            {
                if (!aggregates.TryGetValue(
                        pair.Key, out var balance) ||
                    balance.DispatchCount != 1 ||
                    balance.DeliveryCount == 0 &&
                    (pair.Value.DeliveredCargoQuantity > 0 ||
                     pair.Value.Status ==
                        MilitaryLogisticsStatus.Delivered) ||
                    balance.CargoDispatched !=
                        pair.Value.DispatchedCargoQuantity ||
                    balance.CargoRemaining != pair.Value.RemainingCargoQuantity ||
                    balance.CargoDelivered != pair.Value.DeliveredCargoQuantity ||
                    balance.NaturalLoss != pair.Value.NaturalLossQuantity ||
                    balance.HostileLoss != pair.Value.HostileLossQuantity ||
                    balance.RecoveredCargo !=
                        pair.Value.RecoveredCargoQuantity ||
                    balance.CargoConsumed !=
                        pair.Value.CargoConsumedAsProvisionsQuantity ||
                    balance.ProvisionsLoaded !=
                        pair.Value.ConvoyProvisionsLoaded ||
                    balance.ProvisionsRemaining !=
                        pair.Value.ConvoyProvisionsRemaining ||
                    balance.ProvisionsConsumed !=
                        pair.Value.ConvoyProvisionsConsumed ||
                    balance.BuyerMoney != -pair.Value.TotalPaid ||
                    balance.SourceMoney != pair.Value.TotalPaid ||
                    balance.PublicOrder != pair.Value.OriginPublicOrderDelta)
                {
                    throw new InvalidOperationException(
                        $"Unbalanced military logistics ledger for {pair.Key}.");
                }
            }
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
                    !HasCarrierContainer(
                        containers,
                        order.CarrierPersonId,
                        order.SupplierOrganizationId) ||
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
                    MilitaryEquipmentTransactions.Count != 0 ||
                    MilitaryEquipmentRepairOrders.Count != 0)
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
                var hasRepairContract =
                    !string.IsNullOrEmpty(
                        definition.RepairMaterialProductDefinitionId) ||
                    !string.IsNullOrEmpty(definition.RepairFacilityTag) ||
                    definition.RepairMaterialQuantityPerUnit != 0 ||
                    definition.RepairDurationDays != 0;
                if (hasRepairContract)
                {
                    ValidateContentReference(
                        definition.RepairMaterialProductDefinitionId,
                        "military equipment repair material",
                        definition.Id);
                    ValidateContentReference(
                        definition.RepairFacilityTag,
                        "military equipment repair facility",
                        definition.Id);
                }
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
                    definition.RequiredDexterityBasisPoints > 10_000 ||
                    hasRepairContract &&
                    (definition.RepairMaterialQuantityPerUnit <= 0 ||
                     definition.RepairDurationDays <= 0) ||
                    !hasRepairContract &&
                    (definition.RepairMaterialQuantityPerUnit != 0 ||
                     definition.RepairDurationDays != 0))
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
                    stock.ReservedDamagedQuantity < 0 ||
                    stock.ReservedDamagedQuantity > stock.DamagedQuantity ||
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
            var repairIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryEquipmentRepairOrders.Count; i++)
            {
                repairIds.Add(MilitaryEquipmentRepairOrders[i].Id);
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
                    !string.IsNullOrEmpty(transaction.SourceRepairOrderId) &&
                    !repairIds.Contains(transaction.SourceRepairOrderId) ||
                    (transaction.Type ==
                         MilitaryEquipmentTransactionType.ProcurementReceipt) !=
                    !string.IsNullOrEmpty(
                        transaction.SourceProcurementOrderId) ||
                    !string.IsNullOrEmpty(transaction.SourceRepairOrderId) &&
                    transaction.Type != MilitaryEquipmentTransactionType.Repair ||
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

        private void ValidatePersistentWorldExecution()
        {
            var commands =
                new Dictionary<string, PersistentWorldCommandState>(
                    StringComparer.Ordinal);
            var results =
                new Dictionary<string, WorldCommandBatchResultState>(
                    StringComparer.Ordinal);
            var events = new Dictionary<string, WorldEventOutboxState>(
                StringComparer.Ordinal);
            var attemptsByCommand = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var lastResultByCommand =
                new Dictionary<string, WorldCommandBatchResultState>(
                    StringComparer.Ordinal);
            var successByCommand = new Dictionary<
                string, WorldCommandBatchResultState>(StringComparer.Ordinal);
            var successfulTransactionIds = new HashSet<string>(
                StringComparer.Ordinal);
            var eventReferences = new Dictionary<string, int>(
                StringComparer.Ordinal);

            for (var i = 0; i < PersistentWorldCommands.Count; i++)
            {
                var command = PersistentWorldCommands[i] ??
                    throw new InvalidOperationException(
                        "A persistent world command cannot be null.");
                _ = new StableId(command.Id);
                _ = new StableId(command.CommandTypeId);
                _ = new StableId(command.IssuerId);
                if (!Enum.IsDefined(
                        typeof(PersistentWorldCommandStatus), command.Status) ||
                    command.CreatedDay < 0 ||
                    command.CreatedDay > AbsoluteDay ||
                    command.CreatedSegment > (byte)DaySegment.Night ||
                    command.DueDay < 0 ||
                    command.DueSegment > (byte)DaySegment.Night ||
                    command.AttemptCount < 0 ||
                    command.Arguments == null)
                {
                    throw new InvalidOperationException(
                        $"Invalid persistent world command {command.Id}.");
                }
                string previousKey = null;
                for (var argumentIndex = 0;
                     argumentIndex < command.Arguments.Count;
                     argumentIndex++)
                {
                    var argument = command.Arguments[argumentIndex] ??
                        throw new InvalidOperationException(
                            $"Command {command.Id} has a null argument.");
                    _ = new StableId(argument.Key);
                    if (argument.Value == null ||
                        previousKey != null && string.CompareOrdinal(
                            previousKey, argument.Key) >= 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid argument order on command {command.Id}.");
                    }
                    previousKey = argument.Key;
                }
                commands.Add(command.Id, command);
            }

            for (var i = 0; i < WorldEventOutbox.Count; i++)
            {
                var worldEvent = WorldEventOutbox[i] ??
                    throw new InvalidOperationException(
                        "A world event outbox entry cannot be null.");
                _ = new StableId(worldEvent.Id);
                _ = new StableId(worldEvent.EventTypeId);
                _ = new StableId(worldEvent.SourceTransactionId);
                if (!Enum.IsDefined(
                        typeof(WorldEventDispatchStatus),
                        worldEvent.DispatchStatus) ||
                    worldEvent.Day < 0 || worldEvent.Day > AbsoluteDay ||
                    worldEvent.Segment > (byte)DaySegment.Night ||
                    worldEvent.DeliveredHandlerIds == null ||
                    worldEvent.DispatchStatus ==
                        WorldEventDispatchStatus.Pending &&
                        worldEvent.DispatchedDay != -1 ||
                    worldEvent.DispatchStatus ==
                        WorldEventDispatchStatus.Dispatched &&
                        (worldEvent.DispatchedDay < worldEvent.Day ||
                         worldEvent.DispatchedDay > AbsoluteDay ||
                         worldEvent.DispatchedSegment >
                            (byte)DaySegment.Night))
                {
                    throw new InvalidOperationException(
                        $"Invalid world event outbox entry {worldEvent.Id}.");
                }
                string previousHandlerId = null;
                for (var handlerIndex = 0;
                     handlerIndex < worldEvent.DeliveredHandlerIds.Count;
                     handlerIndex++)
                {
                    var handlerId = worldEvent.DeliveredHandlerIds[handlerIndex];
                    _ = new StableId(handlerId);
                    if (previousHandlerId != null && string.CompareOrdinal(
                        previousHandlerId, handlerId) >= 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid handler acknowledgements on event {worldEvent.Id}.");
                    }
                    previousHandlerId = handlerId;
                }
                events.Add(worldEvent.Id, worldEvent);
            }

            for (var i = 0; i < WorldCommandBatchResults.Count; i++)
            {
                var result = WorldCommandBatchResults[i] ??
                    throw new InvalidOperationException(
                        "A world command batch result cannot be null.");
                _ = new StableId(result.Id);
                if (!Enum.IsDefined(
                        typeof(WorldCommandBatchOutcome), result.Outcome) ||
                    result.Day < 0 || result.Day > AbsoluteDay ||
                    result.Segment > (byte)DaySegment.Night ||
                    result.CommandIds == null ||
                    result.CommandIds.Count == 0 ||
                    result.Transactions == null ||
                    result.PublishedEventIds == null ||
                    result.Outcome == WorldCommandBatchOutcome.Succeeded &&
                        !string.IsNullOrEmpty(result.FailureCode) ||
                    result.Outcome == WorldCommandBatchOutcome.Rejected &&
                        string.IsNullOrEmpty(result.FailureCode) ||
                    result.Outcome == WorldCommandBatchOutcome.Rejected &&
                        result.PublishedEventIds.Count != 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid world command batch result {result.Id}.");
                }
                if (!string.IsNullOrEmpty(result.FailureCode))
                {
                    _ = new StableId(result.FailureCode);
                }

                string previousCommandId = null;
                for (var commandIndex = 0;
                     commandIndex < result.CommandIds.Count;
                     commandIndex++)
                {
                    var commandId = result.CommandIds[commandIndex];
                    if (!commands.ContainsKey(commandId) ||
                        previousCommandId != null && string.CompareOrdinal(
                            previousCommandId, commandId) >= 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid command reference on result {result.Id}.");
                    }
                    previousCommandId = commandId;
                    attemptsByCommand.TryGetValue(commandId, out var attempts);
                    attemptsByCommand[commandId] = attempts + 1;
                    if (!lastResultByCommand.TryGetValue(
                            commandId, out var previousResult) ||
                        CompareBatchResults(result, previousResult) > 0)
                    {
                        lastResultByCommand[commandId] = result;
                    }
                    if (result.Outcome == WorldCommandBatchOutcome.Succeeded &&
                        successByCommand.ContainsKey(commandId))
                    {
                        throw new InvalidOperationException(
                            $"Command {commandId} succeeded more than once.");
                    }
                    if (result.Outcome == WorldCommandBatchOutcome.Succeeded)
                    {
                        successByCommand.Add(commandId, result);
                    }
                }

                WorldTransactionExecutionState previousTransaction = null;
                var transactionIds = new HashSet<string>(StringComparer.Ordinal);
                for (var transactionIndex = 0;
                     transactionIndex < result.Transactions.Count;
                     transactionIndex++)
                {
                    var transaction = result.Transactions[transactionIndex] ??
                        throw new InvalidOperationException(
                            $"Result {result.Id} has a null transaction.");
                    _ = new StableId(transaction.TransactionId);
                    _ = new StableId(transaction.TransactionKindId);
                    if (!transactionIds.Add(transaction.TransactionId) ||
                        previousTransaction != null &&
                        CompareTransactionExecutions(
                            previousTransaction, transaction) >= 0 ||
                        result.Outcome ==
                            WorldCommandBatchOutcome.Succeeded &&
                            !successfulTransactionIds.Add(
                                transaction.TransactionId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid transaction summary on result {result.Id}.");
                    }
                    previousTransaction = transaction;
                }

                string previousEventId = null;
                for (var eventIndex = 0;
                     eventIndex < result.PublishedEventIds.Count;
                     eventIndex++)
                {
                    var eventId = result.PublishedEventIds[eventIndex];
                    if (!events.ContainsKey(eventId) ||
                        previousEventId != null && string.CompareOrdinal(
                            previousEventId, eventId) >= 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid event reference on result {result.Id}.");
                    }
                    previousEventId = eventId;
                    eventReferences.TryGetValue(eventId, out var references);
                    eventReferences[eventId] = references + 1;
                }
                results.Add(result.Id, result);
            }

            for (var i = 0; i < PersistentWorldCommands.Count; i++)
            {
                var command = PersistentWorldCommands[i];
                attemptsByCommand.TryGetValue(command.Id, out var attempts);
                lastResultByCommand.TryGetValue(command.Id, out var lastResult);
                successByCommand.TryGetValue(command.Id, out var successResult);
                if (command.AttemptCount != attempts ||
                    command.LastAttemptResultId !=
                        (lastResult?.Id ?? string.Empty) ||
                    command.Status == PersistentWorldCommandStatus.Pending &&
                        (command.CompletedDay != -1 ||
                         !string.IsNullOrEmpty(command.CompletionResultId) ||
                         successResult != null) ||
                    command.Status == PersistentWorldCommandStatus.Completed &&
                        (command.CompletedDay < command.CreatedDay ||
                         command.CompletedDay > AbsoluteDay ||
                         command.CompletedSegment >
                            (byte)DaySegment.Night ||
                         successResult == null ||
                         command.CompletionResultId != successResult.Id ||
                         command.LastAttemptResultId != successResult.Id) ||
                    command.Status == PersistentWorldCommandStatus.Cancelled &&
                        (command.CompletedDay < command.CreatedDay ||
                         command.CompletedDay > AbsoluteDay ||
                         !string.IsNullOrEmpty(command.CompletionResultId) ||
                         successResult != null))
                {
                    throw new InvalidOperationException(
                        $"Invalid persistent command lifecycle {command.Id}.");
                }
            }

            for (var i = 0; i < WorldEventOutbox.Count; i++)
            {
                var worldEvent = WorldEventOutbox[i];
                eventReferences.TryGetValue(worldEvent.Id, out var references);
                if (references != 1 ||
                    !successfulTransactionIds.Contains(
                        worldEvent.SourceTransactionId))
                {
                    throw new InvalidOperationException(
                        $"Invalid outbox source for event {worldEvent.Id}.");
                }
            }
        }

        private static int CompareBatchResults(
            WorldCommandBatchResultState left,
            WorldCommandBatchResultState right)
        {
            var day = left.Day.CompareTo(right.Day);
            if (day != 0)
            {
                return day;
            }
            var segment = left.Segment.CompareTo(right.Segment);
            return segment != 0
                ? segment
                : string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareTransactionExecutions(
            WorldTransactionExecutionState left,
            WorldTransactionExecutionState right)
        {
            var priority = left.Priority.CompareTo(right.Priority);
            return priority != 0
                ? priority
                : string.CompareOrdinal(
                    left.TransactionId, right.TransactionId);
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

        private static bool RouteConnects(
            RouteState route,
            string originLocationId,
            string destinationLocationId)
        {
            return route.FromLocationId == originLocationId &&
                   route.ToLocationId == destinationLocationId ||
                   route.Bidirectional &&
                   route.ToLocationId == originLocationId &&
                   route.FromLocationId == destinationLocationId;
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

                if (FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches
                    ? village.PublicGranaryGrain != 0 ||
                      string.IsNullOrEmpty(
                          village.PublicGranaryInventoryContainerId)
                    : !string.IsNullOrEmpty(
                        village.PublicGranaryInventoryContainerId))
                {
                    throw new InvalidOperationException(
                        $"Village {village.Id} disagrees with food inventory authority.");
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
            var formalMarketOrderIds = new HashSet<string>(StringComparer.Ordinal);
            var formalMarketOrders =
                new Dictionary<string, FormalMarketOrderState>(
                    StringComparer.Ordinal);
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

                if (FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches
                    ? governance.CountyGranaryGrain != 0 ||
                      string.IsNullOrEmpty(
                          governance.GranaryInventoryContainerId)
                    : !string.IsNullOrEmpty(
                        governance.GranaryInventoryContainerId))
                {
                    throw new InvalidOperationException(
                        $"County governance {governance.Id} disagrees with food inventory authority.");
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
                    entry.Type !=
                            CountyFiscalEntryType.GrainExternalFreightEscrow &&
                        entry.FamilyMoneyDelta +
                            entry.GovernmentMoneyDelta != 0 ||
                    entry.Type ==
                            CountyFiscalEntryType.GrainExternalFreightEscrow &&
                        (entry.FamilyMoneyDelta != 0 ||
                         entry.GovernmentMoneyDelta >= 0) ||
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
            var agricultureOrderIds = new HashSet<string>(StringComparer.Ordinal);
            var agricultureOrders =
                new Dictionary<string, AgricultureWorkOrderState>(
                    StringComparer.Ordinal);
            var resourceExtractionOrderIds = new HashSet<string>(
                StringComparer.Ordinal);
            var procurementOrderIds = new HashSet<string>(StringComparer.Ordinal);
            var logisticsOrderIds = new HashSet<string>(StringComparer.Ordinal);
            var repairOrderIds = new HashSet<string>(StringComparer.Ordinal);
            var transactionIds = new HashSet<string>(StringComparer.Ordinal);
            var transactions =
                new Dictionary<string, InventoryTransactionState>(
                    StringComparer.Ordinal);
            var formalMarketOrderIds = new HashSet<string>(
                StringComparer.Ordinal);
            var formalMarketOrders =
                new Dictionary<string, FormalMarketOrderState>(
                    StringComparer.Ordinal);
            var civilianFreights =
                new Dictionary<string, CivilianFreightState>(
                    StringComparer.Ordinal);
            var villageIds = new HashSet<string>(StringComparer.Ordinal);
            var governanceIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Families.Count; i++)
            {
                familyIds.Add(Families[i].Id);
            }

            for (var i = 0; i < Organizations.Count; i++)
            {
                organizationIds.Add(Organizations[i].Id);
            }

            for (var i = 0; i < Villages.Count; i++)
            {
                villageIds.Add(Villages[i].Id);
            }

            for (var i = 0; i < CountyGovernances.Count; i++)
            {
                governanceIds.Add(CountyGovernances[i].Id);
            }

            for (var i = 0; i < FormalMarketOrders.Count; i++)
            {
                formalMarketOrderIds.Add(FormalMarketOrders[i].Id);
                formalMarketOrders.Add(
                    FormalMarketOrders[i].Id, FormalMarketOrders[i]);
            }

            for (var i = 0; i < CivilianFreights.Count; i++)
            {
                civilianFreights.Add(
                    CivilianFreights[i].Id, CivilianFreights[i]);
            }

            for (var i = 0; i < VillageFacilities.Count; i++)
            {
                facilities.Add(VillageFacilities[i].Id, VillageFacilities[i]);
            }

            for (var i = 0; i < ProcessingWorkOrders.Count; i++)
            {
                processingOrderIds.Add(ProcessingWorkOrders[i].Id);
            }

            for (var i = 0; i < AgricultureWorkOrders.Count; i++)
            {
                agricultureOrderIds.Add(AgricultureWorkOrders[i].Id);
                agricultureOrders.Add(
                    AgricultureWorkOrders[i].Id, AgricultureWorkOrders[i]);
            }

            for (var i = 0; i < ResourceExtractionOrders.Count; i++)
            {
                resourceExtractionOrderIds.Add(ResourceExtractionOrders[i].Id);
            }

            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                transactionIds.Add(InventoryTransactions[i].Id);
                transactions.Add(
                    InventoryTransactions[i].Id,
                    InventoryTransactions[i]);
            }

            for (var i = 0; i < MilitaryProcurementOrders.Count; i++)
            {
                procurementOrderIds.Add(MilitaryProcurementOrders[i].Id);
            }

            for (var i = 0; i < MilitaryLogisticsOrders.Count; i++)
            {
                logisticsOrderIds.Add(MilitaryLogisticsOrders[i].Id);
            }

            for (var i = 0; i < MilitaryEquipmentRepairOrders.Count; i++)
            {
                repairOrderIds.Add(MilitaryEquipmentRepairOrders[i].Id);
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

            if (FoodInventoryAuthorityMode ==
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                for (var i = 0; i < Villages.Count; i++)
                {
                    var village = Villages[i];
                    if (!containers.TryGetValue(
                            village.PublicGranaryInventoryContainerId,
                            out var container) ||
                        container.LocationId != village.LocationId ||
                        string.IsNullOrEmpty(container.OwnerOrganizationId))
                    {
                        throw new InvalidOperationException(
                            $"Village {village.Id} has an invalid formal granary container.");
                    }
                }

                for (var i = 0; i < CountyGovernances.Count; i++)
                {
                    var governance = CountyGovernances[i];
                    if (!containers.TryGetValue(
                            governance.GranaryInventoryContainerId,
                            out var container) ||
                        container.LocationId != governance.CountyLocationId ||
                        container.OwnerOrganizationId !=
                            governance.GovernmentOrganizationId)
                    {
                        throw new InvalidOperationException(
                            $"County governance {governance.Id} has an invalid formal granary container.");
                    }
                }
            }

            var productionSites = new Dictionary<string, ProductionSiteState>(
                StringComparer.Ordinal);
            for (var i = 0; i < ProductionSites.Count; i++)
            {
                var site = ProductionSites[i] ??
                    throw new InvalidOperationException(
                        "A production site cannot be null.");
                productionSites.Add(site.Id, site);
                ValidateContentReference(site.KindId, "production site kind", site.Id);
                if (!organizationIds.Contains(site.OwnerOrganizationId) ||
                    !locationIds.Contains(site.LocationId) ||
                    !personIds.Contains(site.ManagerPersonId) ||
                    !containers.TryGetValue(
                        site.InventoryContainerId, out var siteContainer) ||
                    siteContainer.OwnerOrganizationId != site.OwnerOrganizationId ||
                    siteContainer.LocationId != site.LocationId ||
                    !string.IsNullOrEmpty(siteContainer.CarrierPersonId) ||
                    site.ConcurrentOrderCapacity <= 0 ||
                    site.ConditionBasisPoints <= 0 ||
                    site.ConditionBasisPoints > 10_000 ||
                    site.FacilityTags == null || site.FacilityTags.Count == 0 ||
                    !HasOrganizationMembership(
                        site.ManagerPersonId, site.OwnerOrganizationId))
                {
                    throw new InvalidOperationException(
                        $"Invalid production site {site.Id}.");
                }

                var tags = new HashSet<string>(StringComparer.Ordinal);
                for (var tagIndex = 0;
                     tagIndex < site.FacilityTags.Count;
                     tagIndex++)
                {
                    ValidateContentReference(
                        site.FacilityTags[tagIndex], "facility tag", site.Id);
                    if (!tags.Add(site.FacilityTags[tagIndex]))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate facility tag on {site.Id}.");
                    }
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

                if (batch.QualityDimensions == null ||
                    batch.QualityDimensions.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Product batch {batch.Id} has no quality dimensions.");
                }

                var qualityDimensionIds = new HashSet<string>(
                    StringComparer.Ordinal);
                for (var dimensionIndex = 0;
                     dimensionIndex < batch.QualityDimensions.Count;
                     dimensionIndex++)
                {
                    var dimension = batch.QualityDimensions[dimensionIndex];
                    if (dimension == null ||
                        dimension.ValueBasisPoints < 0 ||
                        dimension.ValueBasisPoints > 10_000 ||
                        !qualityDimensionIds.Add(
                            dimension.QualityDimensionId))
                    {
                        throw new InvalidOperationException(
                            $"Product batch {batch.Id} has invalid quality dimensions.");
                    }

                    ValidateContentReference(
                        dimension.QualityDimensionId,
                        "quality dimension",
                        batch.Id);
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
                var familyContainerStored =
                    !string.IsNullOrEmpty(batch.OwnerFamilyId) &&
                    string.IsNullOrEmpty(batch.OwnerOrganizationId) &&
                    string.IsNullOrEmpty(batch.StorageFacilityId) &&
                    !string.IsNullOrEmpty(batch.InventoryContainerId);
                var organizationContainerStored =
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
                var validCivilianFreightStorage = false;
                var validPublicReliefFreightStorage = false;
                InventoryContainerState freightContainer = null;
                InventoryTransactionState sourceTransaction = null;
                CivilianFreightState civilianFreight = null;
                if (familyContainerStored &&
                    containers.TryGetValue(
                        batch.InventoryContainerId, out freightContainer) &&
                    transactions.TryGetValue(
                        batch.SourceTransactionId,
                        out sourceTransaction) &&
                    sourceTransaction.Type ==
                        InventoryTransactionType.CivilianFreightDispatched &&
                    civilianFreights.TryGetValue(
                        sourceTransaction.SourceCivilianFreightId ??
                            string.Empty,
                        out civilianFreight))
                {
                    validCivilianFreightStorage =
                        civilianFreight.BuyerFamilyId == batch.OwnerFamilyId &&
                        civilianFreight.TransportInventoryContainerId ==
                            freightContainer.Id;
                }
                else if (organizationContainerStored &&
                    containers.TryGetValue(
                        batch.InventoryContainerId, out freightContainer) &&
                    transactions.TryGetValue(
                        batch.SourceTransactionId,
                        out sourceTransaction) &&
                    sourceTransaction.Type ==
                        InventoryTransactionType.CivilianFreightDispatched &&
                    civilianFreights.TryGetValue(
                        sourceTransaction.SourceCivilianFreightId ??
                            string.Empty,
                        out civilianFreight))
                {
                    validPublicReliefFreightStorage =
                        civilianFreight.BuyerOrganizationId ==
                            batch.OwnerOrganizationId &&
                        civilianFreight.TransportInventoryContainerId ==
                            freightContainer.Id;
                }
                if (!validFamilyStorage && !validOrganizationStorage &&
                    !validCivilianFreightStorage &&
                    !validPublicReliefFreightStorage ||
                    !locationIds.Contains(batch.OriginLocationId) ||
                    !transactionIds.Contains(batch.SourceTransactionId) ||
                    !string.IsNullOrEmpty(batch.SourceWorkOrderId) &&
                    !processingOrderIds.Contains(batch.SourceWorkOrderId) &&
                    !agricultureOrderIds.Contains(batch.SourceWorkOrderId) &&
                    !resourceExtractionOrderIds.Contains(
                        batch.SourceWorkOrderId) ||
                    batch.ProducedDay < 0 || batch.ProducedDay > AbsoluteDay ||
                    batch.UnitWeight <= 0 ||
                    batch.Quantity < 0 || batch.ReservedQuantity < 0 ||
                    batch.ReservedQuantity > batch.Quantity ||
                    batch.QualityBasisPoints < 0 ||
                    batch.QualityBasisPoints > 10_000 ||
                    batch.QualityBasisPoints !=
                        ProductQualityRules.CalculateSummary(
                            batch.QualityDimensions) ||
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

            ValidateResourceExtraction(
                personIds,
                locationIds,
                organizationIds,
                productionSites,
                containers,
                batches,
                transactionIds);

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
                ValidateContentReference(
                    order.PracticeSkillDefinitionId,
                    "processing practice skill",
                    order.Id);
                var familyOrder =
                    !string.IsNullOrEmpty(order.OwnerFamilyId) &&
                    string.IsNullOrEmpty(order.OwnerOrganizationId);
                var organizationOrder =
                    string.IsNullOrEmpty(order.OwnerFamilyId) &&
                    !string.IsNullOrEmpty(order.OwnerOrganizationId);
                var validFamilyOrder = familyOrder &&
                    familyIds.Contains(order.OwnerFamilyId) &&
                    facilities.TryGetValue(
                        order.StorageFacilityId, out var facility) &&
                    facility.OwnerFamilyId == order.OwnerFamilyId &&
                    string.IsNullOrEmpty(order.ProductionSiteId) &&
                    string.IsNullOrEmpty(order.InventoryContainerId);
                var validOrganizationOrder = organizationOrder &&
                    organizationIds.Contains(order.OwnerOrganizationId) &&
                    string.IsNullOrEmpty(order.StorageFacilityId) &&
                    productionSites.TryGetValue(
                        order.ProductionSiteId, out var productionSite) &&
                    productionSite.OwnerOrganizationId ==
                        order.OwnerOrganizationId &&
                    productionSite.InventoryContainerId ==
                        order.InventoryContainerId &&
                    containers.TryGetValue(
                        order.InventoryContainerId, out var orderContainer) &&
                    orderContainer.OwnerOrganizationId ==
                        order.OwnerOrganizationId;
                if (!validFamilyOrder && !validOrganizationOrder ||
                    !personIds.Contains(order.ManagerPersonId) ||
                    !Enum.IsDefined(typeof(ProductionControlMode), order.ControlMode) ||
                    !Enum.IsDefined(typeof(ProductionOrderStatus), order.Status) ||
                    order.CreatedDay < 0 || order.FinishDay <= order.CreatedDay ||
                    order.SettledDay < -1 || order.SettledDay > AbsoluteDay ||
                    order.RunCount <= 0 || order.InputReservations == null ||
                    order.InputReservations.Count == 0 ||
                    order.OutputBatchIds == null ||
                    order.ManagerSkillBasisPointsAtStart < 0 ||
                    order.ManagerSkillBasisPointsAtStart > 10_000 ||
                    order.PracticeGainBasisPoints < 0 ||
                    order.PracticeGainBasisPoints > 10_000 ||
                    order.OutputQualityBasisPoints < 0 ||
                    order.OutputQualityBasisPoints > 10_000 ||
                    order.Status == ProductionOrderStatus.Active &&
                    (order.SettledDay != -1 || order.OutputBatchIds.Count != 0 ||
                     order.PracticeGainBasisPoints != 0 ||
                     order.OutputQualityBasisPoints != 0) ||
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
                        !batches.TryGetValue(
                            reservation.BatchId, out var reservedBatch) ||
                        familyOrder &&
                        (reservedBatch.OwnerFamilyId != order.OwnerFamilyId ||
                         reservedBatch.StorageFacilityId !=
                            order.StorageFacilityId) ||
                        organizationOrder &&
                        (reservedBatch.OwnerOrganizationId !=
                            order.OwnerOrganizationId ||
                         reservedBatch.InventoryContainerId !=
                            order.InventoryContainerId) ||
                        !reservationKeys.Add(reservation.BatchId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid reservation on {order.Id}.");
                    }
                }

                var outputIds = new HashSet<string>(StringComparer.Ordinal);
                var minimumOutputQuality = 10_000;
                for (var outputIndex = 0;
                     outputIndex < order.OutputBatchIds.Count;
                     outputIndex++)
                {
                    if (!batches.TryGetValue(
                            order.OutputBatchIds[outputIndex], out var output) ||
                        output.SourceWorkOrderId != order.Id ||
                        familyOrder &&
                        (output.OwnerFamilyId != order.OwnerFamilyId ||
                         output.StorageFacilityId != order.StorageFacilityId) ||
                        organizationOrder &&
                        (output.OwnerOrganizationId !=
                            order.OwnerOrganizationId ||
                         output.InventoryContainerId !=
                            order.InventoryContainerId) ||
                        !outputIds.Add(output.Id))
                    {
                        throw new InvalidOperationException(
                            $"Invalid output batch on {order.Id}.");
                    }

                    minimumOutputQuality = Math.Min(
                        minimumOutputQuality, output.QualityBasisPoints);
                }

                if (order.PracticeTrackingEnabled &&
                    order.Status == ProductionOrderStatus.Completed &&
                    order.OutputQualityBasisPoints != minimumOutputQuality)
                {
                    throw new InvalidOperationException(
                        $"Processing work order {order.Id} has an invalid " +
                        "output quality summary.");
                }
            }

            var practiceOrders = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < ProductionPracticeLedgerEntries.Count; i++)
            {
                var entry = ProductionPracticeLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A production practice ledger entry cannot be null.");
                ValidateContentReference(
                    entry.SkillDefinitionId,
                    "practice ledger skill",
                    entry.Id);
                if (!processingOrders.TryGetValue(
                        entry.ProcessingWorkOrderId, out var order) ||
                    order.Status != ProductionOrderStatus.Completed ||
                    !order.PracticeTrackingEnabled ||
                    entry.PersonId != order.ManagerPersonId ||
                    entry.SkillDefinitionId !=
                        order.PracticeSkillDefinitionId ||
                    entry.Day != order.SettledDay ||
                    entry.GainBasisPoints != order.PracticeGainBasisPoints ||
                    entry.OutputQualityBasisPoints !=
                        order.OutputQualityBasisPoints ||
                    entry.MasteryBeforeBasisPoints < 0 ||
                    entry.MasteryBeforeBasisPoints > 10_000 ||
                    entry.GainBasisPoints < 0 ||
                    entry.MasteryAfterBasisPoints != checked(
                        entry.MasteryBeforeBasisPoints +
                        entry.GainBasisPoints) ||
                    entry.MasteryAfterBasisPoints > 10_000 ||
                    string.IsNullOrWhiteSpace(entry.Summary) ||
                    !practiceOrders.Add(order.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid production practice ledger entry {entry.Id}.");
                }
            }

            for (var i = 0; i < ProcessingWorkOrders.Count; i++)
            {
                var order = ProcessingWorkOrders[i];
                var requiresPractice = order.PracticeTrackingEnabled &&
                    order.Status == ProductionOrderStatus.Completed;
                if (requiresPractice != practiceOrders.Contains(order.Id) ||
                    !order.PracticeTrackingEnabled &&
                    (order.PracticeGainBasisPoints != 0 ||
                     order.OutputQualityBasisPoints != 0))
                {
                    throw new InvalidOperationException(
                        $"Processing work order {order.Id} has inconsistent practice history.");
                }
            }

            for (var i = 0; i < ProductionSites.Count; i++)
            {
                var active = 0;
                for (var orderIndex = 0;
                     orderIndex < ProcessingWorkOrders.Count;
                     orderIndex++)
                {
                    if (ProcessingWorkOrders[orderIndex].ProductionSiteId ==
                            ProductionSites[i].Id &&
                        ProcessingWorkOrders[orderIndex].Status ==
                            ProductionOrderStatus.Active)
                    {
                        active++;
                    }
                }

                for (var orderIndex = 0;
                     orderIndex < MilitaryEquipmentRepairOrders.Count;
                     orderIndex++)
                {
                    if (MilitaryEquipmentRepairOrders[orderIndex]
                            .ProductionSiteId == ProductionSites[i].Id &&
                        MilitaryEquipmentRepairOrders[orderIndex].Status ==
                            ProductionOrderStatus.Active)
                    {
                        active++;
                    }
                }

                for (var orderIndex = 0;
                     orderIndex < ResourceExtractionOrders.Count;
                     orderIndex++)
                {
                    if (ResourceExtractionOrders[orderIndex]
                            .ProductionSiteId == ProductionSites[i].Id &&
                        ResourceExtractionOrders[orderIndex].Status ==
                            ProductionOrderStatus.Active)
                    {
                        active++;
                    }
                }

                if (active > ProductionSites[i].ConcurrentOrderCapacity)
                {
                    throw new InvalidOperationException(
                        $"Production site {ProductionSites[i].Id} exceeds capacity.");
                }
            }

            var reservedDamagedByStock = new Dictionary<string, int>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryEquipmentRepairOrders.Count; i++)
            {
                var order = MilitaryEquipmentRepairOrders[i] ??
                    throw new InvalidOperationException(
                        "A military equipment repair order cannot be null.");
                var definition = FindEquipmentDefinition(
                    MilitaryEquipmentDefinitions,
                    order.EquipmentDefinitionId);
                var stock = FindArmoryStock(
                    MilitaryArmoryStocks,
                    order.ArmyId,
                    order.EquipmentDefinitionId);
                _ = FindArmy(Armies, order.ArmyId);
                var validSite = productionSites.TryGetValue(
                    order.ProductionSiteId, out var repairSite);
                var validContainer = containers.TryGetValue(
                    order.InventoryContainerId, out var repairContainer);
                if (!validSite || !validContainer ||
                    repairSite.InventoryContainerId != repairContainer.Id ||
                    repairSite.OwnerOrganizationId !=
                        repairContainer.OwnerOrganizationId ||
                    !repairSite.FacilityTags.Contains(
                        definition.RepairFacilityTag) ||
                    !personIds.Contains(order.ManagerPersonId) ||
                    repairSite.ManagerPersonId != order.ManagerPersonId ||
                    !Enum.IsDefined(
                        typeof(ProductionControlMode), order.ControlMode) ||
                    !Enum.IsDefined(
                        typeof(ProductionOrderStatus), order.Status) ||
                    order.Quantity <= 0 || order.CreatedDay < 0 ||
                    order.FinishDay <= order.CreatedDay ||
                    order.SettledDay < -1 || order.SettledDay > AbsoluteDay ||
                    order.MaterialReservations == null ||
                    order.MaterialReservations.Count == 0 ||
                    order.Status == ProductionOrderStatus.Active &&
                    order.SettledDay != -1 ||
                    order.Status == ProductionOrderStatus.Completed &&
                    order.SettledDay < order.FinishDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid military equipment repair order {order.Id}.");
                }

                var reservationIds = new HashSet<string>(StringComparer.Ordinal);
                long materialQuantity = 0;
                for (var reservationIndex = 0;
                     reservationIndex < order.MaterialReservations.Count;
                     reservationIndex++)
                {
                    var reservation =
                        order.MaterialReservations[reservationIndex];
                    if (reservation == null || reservation.Quantity <= 0 ||
                        !batches.TryGetValue(
                            reservation.BatchId, out var materialBatch) ||
                        materialBatch.ProductDefinitionId !=
                            definition.RepairMaterialProductDefinitionId ||
                        materialBatch.OwnerOrganizationId !=
                            repairSite.OwnerOrganizationId ||
                        materialBatch.InventoryContainerId !=
                            repairContainer.Id ||
                        !reservationIds.Add(reservation.BatchId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid repair material reservation on {order.Id}.");
                    }

                    materialQuantity = checked(
                        materialQuantity + reservation.Quantity);
                }

                if (materialQuantity != checked(
                        (long)definition.RepairMaterialQuantityPerUnit *
                        order.Quantity))
                {
                    throw new InvalidOperationException(
                        $"Repair material total is invalid on {order.Id}.");
                }

                var reservedTransactions = 0;
                var settledTransactions = 0;
                for (var transactionIndex = 0;
                     transactionIndex < InventoryTransactions.Count;
                     transactionIndex++)
                {
                    var transaction = InventoryTransactions[transactionIndex];
                    if (transaction.SourceEquipmentRepairOrderId != order.Id)
                    {
                        continue;
                    }

                    if (transaction.Type ==
                        InventoryTransactionType.EquipmentRepairReserved)
                    {
                        reservedTransactions++;
                    }
                    else if (transaction.Type ==
                             InventoryTransactionType.EquipmentRepairSettled)
                    {
                        settledTransactions++;
                    }
                }

                var equipmentTransactions = 0;
                for (var transactionIndex = 0;
                     transactionIndex < MilitaryEquipmentTransactions.Count;
                     transactionIndex++)
                {
                    var transaction =
                        MilitaryEquipmentTransactions[transactionIndex];
                    if (transaction.SourceRepairOrderId == order.Id &&
                        transaction.Type ==
                            MilitaryEquipmentTransactionType.Repair &&
                        transaction.EquipmentDefinitionId ==
                            order.EquipmentDefinitionId &&
                        transaction.FromArmyId == order.ArmyId &&
                        transaction.ToArmyId == order.ArmyId &&
                        transaction.Quantity == order.Quantity)
                    {
                        equipmentTransactions++;
                    }
                }

                if (reservedTransactions != 1 ||
                    order.Status == ProductionOrderStatus.Active &&
                    (settledTransactions != 0 || equipmentTransactions != 0) ||
                    order.Status == ProductionOrderStatus.Completed &&
                    (settledTransactions != 1 || equipmentTransactions != 1))
                {
                    throw new InvalidOperationException(
                        $"Repair provenance is incomplete for {order.Id}.");
                }

                if (order.Status == ProductionOrderStatus.Active)
                {
                    var key = order.ArmyId + "|" + order.EquipmentDefinitionId;
                    reservedDamagedByStock.TryGetValue(key, out var reserved);
                    reservedDamagedByStock[key] = checked(
                        reserved + order.Quantity);
                }
            }

            for (var i = 0; i < MilitaryArmoryStocks.Count; i++)
            {
                var stock = MilitaryArmoryStocks[i];
                var key = stock.ArmyId + "|" + stock.EquipmentDefinitionId;
                reservedDamagedByStock.TryGetValue(key, out var reserved);
                if (stock.ReservedDamagedQuantity != reserved)
                {
                    throw new InvalidOperationException(
                        $"Reserved damaged equipment mismatch for {stock.Id}.");
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
                    !processingOrders.ContainsKey(transaction.SourceWorkOrderId) &&
                    !agricultureOrderIds.Contains(transaction.SourceWorkOrderId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceMilitaryProcurementId) &&
                    !procurementOrderIds.Contains(
                        transaction.SourceMilitaryProcurementId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceEquipmentRepairOrderId) &&
                    !repairOrderIds.Contains(
                        transaction.SourceEquipmentRepairOrderId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceResourceExtractionOrderId) &&
                    !resourceExtractionOrderIds.Contains(
                        transaction.SourceResourceExtractionOrderId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceMilitaryLogisticsOrderId) &&
                    !logisticsOrderIds.Contains(
                        transaction.SourceMilitaryLogisticsOrderId) ||
                    !string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    !villageIds.Contains(transaction.SourceVillageId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceCountyGovernanceId) &&
                    !governanceIds.Contains(
                        transaction.SourceCountyGovernanceId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceFormalMarketOrderId) &&
                    !formalMarketOrderIds.Contains(
                        transaction.SourceFormalMarketOrderId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceCivilianFreightId) &&
                    !civilianFreights.ContainsKey(
                        transaction.SourceCivilianFreightId) ||
                    transaction.Lines == null || transaction.Lines.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid inventory transaction {transaction.Id}.");
                }


                var requiresOrder =
                    transaction.Type == InventoryTransactionType.Reserved ||
                    transaction.Type == InventoryTransactionType.ReservationReleased ||
                    transaction.Type == InventoryTransactionType.RecipeSettled;
                var hasProcessingOrder =
                    !string.IsNullOrEmpty(transaction.SourceWorkOrderId) &&
                    processingOrders.ContainsKey(transaction.SourceWorkOrderId);
                var hasAgricultureOrder =
                    !string.IsNullOrEmpty(transaction.SourceWorkOrderId) &&
                    agricultureOrderIds.Contains(transaction.SourceWorkOrderId);
                var foodHarvest = transaction.Type ==
                    InventoryTransactionType.FoodHarvested;
                var validAgricultureInventory =
                    (transaction.Type ==
                         InventoryTransactionType.LegacyBalanceConverted ||
                     foodHarvest) && hasAgricultureOrder;
                var requiresProcurement = transaction.Type ==
                    InventoryTransactionType.MilitaryProcurementDispatched;
                var requiresRepair =
                    transaction.Type ==
                        InventoryTransactionType.EquipmentRepairReserved ||
                    transaction.Type ==
                        InventoryTransactionType.EquipmentRepairSettled;
                var requiresResourceExtraction = transaction.Type ==
                    InventoryTransactionType.ResourceExtractionSettled;
                var requiresMilitaryLogistics = transaction.Type ==
                        InventoryTransactionType.MilitaryLogisticsDispatched ||
                    transaction.Type == InventoryTransactionType
                        .MilitaryLogisticsHandoffReserved ||
                    transaction.Type == InventoryTransactionType
                        .MilitaryLogisticsHandoffLoaded;
                var formalization = transaction.Type ==
                    InventoryTransactionType.LegacyFoodStockFormalized;
                var foodTax = transaction.Type ==
                    InventoryTransactionType.FoodTaxTransferred;
                var villageRelief = transaction.Type ==
                    InventoryTransactionType.FoodVillageReliefTransferred;
                var countyRelief = transaction.Type ==
                    InventoryTransactionType.FoodCountyReliefTransferred;
                var taxRemittance = transaction.Type ==
                    InventoryTransactionType.FoodTaxRemitted;
                var foodTransfer = foodTax || villageRelief ||
                    countyRelief || taxRemittance;
                var foodRuntime = foodHarvest || foodTransfer;
                var marketReserved = transaction.Type ==
                    InventoryTransactionType.FoodMarketReserved;
                var marketReleased = transaction.Type ==
                    InventoryTransactionType.FoodMarketReservationReleased;
                var marketTransferred = transaction.Type ==
                    InventoryTransactionType.FoodMarketTransferred;
                var publicReliefProcurement = transaction.Type ==
                    InventoryTransactionType
                        .FoodPublicReliefProcurementTransferred;
                var marketInventory = marketReserved || marketReleased ||
                    marketTransferred || publicReliefProcurement;
                var civilianDispatch = transaction.Type ==
                    InventoryTransactionType.CivilianFreightDispatched;
                var civilianLoss = transaction.Type ==
                    InventoryTransactionType.CivilianFreightNaturalLoss;
                var civilianDelivery = transaction.Type ==
                    InventoryTransactionType.CivilianFreightDelivered;
                var civilianInventory = civilianDispatch || civilianLoss ||
                    civilianDelivery;
                CivilianFreightState civilianFreight = null;
                var hasCivilianFreight = !string.IsNullOrEmpty(
                        transaction.SourceCivilianFreightId) &&
                    civilianFreights.TryGetValue(
                        transaction.SourceCivilianFreightId,
                        out civilianFreight);
                FormalMarketOrderState formalMarketOrder = null;
                var hasFormalMarketOrder =
                    !string.IsNullOrEmpty(
                        transaction.SourceFormalMarketOrderId) &&
                    formalMarketOrders.TryGetValue(
                        transaction.SourceFormalMarketOrderId,
                        out formalMarketOrder);
                var familyFormalization = formalization &&
                    transaction.LegacyFamilyGrainDelta < 0 &&
                    transaction.LegacyVillagePublicGranaryDelta == 0 &&
                    transaction.LegacyCountyGranaryDelta == 0 &&
                    string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceCountyGovernanceId);
                var villageFormalization = formalization &&
                    transaction.LegacyFamilyGrainDelta == 0 &&
                    transaction.LegacyVillagePublicGranaryDelta < 0 &&
                    transaction.LegacyCountyGranaryDelta == 0 &&
                    !string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceCountyGovernanceId);
                var countyFormalization = formalization &&
                    transaction.LegacyFamilyGrainDelta == 0 &&
                    transaction.LegacyVillagePublicGranaryDelta == 0 &&
                    transaction.LegacyCountyGranaryDelta < 0 &&
                    string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    !string.IsNullOrEmpty(
                        transaction.SourceCountyGovernanceId);
                var validFoodRuntimeProvenance =
                    foodHarvest && hasAgricultureOrder &&
                    !string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceCountyGovernanceId) ||
                    (foodTax || villageRelief) &&
                    !string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceCountyGovernanceId) ||
                    (countyRelief || taxRemittance) &&
                    !string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    !string.IsNullOrEmpty(
                        transaction.SourceCountyGovernanceId);
                var validMarketProvenance = marketInventory &&
                    hasFormalMarketOrder &&
                    formalMarketOrder.Side == FormalMarketOrderSide.Sell &&
                    transaction.SourceCountyGovernanceId ==
                        formalMarketOrder.CountyGovernanceId &&
                    string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    string.IsNullOrEmpty(transaction.SourceWorkOrderId) &&
                    transaction.LegacyFamilyGrainDelta == 0 &&
                    transaction.LegacyFamilySeedGrainDelta == 0 &&
                    transaction.LegacyVillagePublicGranaryDelta == 0 &&
                    transaction.LegacyCountyGranaryDelta == 0;
                var validCivilianProvenance = civilianInventory &&
                    hasCivilianFreight &&
                    string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    string.IsNullOrEmpty(transaction.SourceWorkOrderId) &&
                    transaction.LegacyFamilyGrainDelta == 0 &&
                    transaction.LegacyFamilySeedGrainDelta == 0 &&
                    transaction.LegacyVillagePublicGranaryDelta == 0 &&
                    transaction.LegacyCountyGranaryDelta == 0 &&
                    (civilianDispatch &&
                         transaction.SourceFormalMarketOrderId ==
                            civilianFreight.SellOrderId &&
                         transaction.SourceCountyGovernanceId ==
                            civilianFreight.OriginCountyGovernanceId ||
                     !civilianDispatch &&
                         string.IsNullOrEmpty(
                             transaction.SourceFormalMarketOrderId) &&
                         string.IsNullOrEmpty(
                             transaction.SourceCountyGovernanceId));
                if (requiresOrder != hasProcessingOrder ||
                    hasAgricultureOrder && !validAgricultureInventory ||
                    requiresProcurement !=
                    !string.IsNullOrEmpty(
                        transaction.SourceMilitaryProcurementId) ||
                    requiresRepair !=
                    !string.IsNullOrEmpty(
                        transaction.SourceEquipmentRepairOrderId) ||
                    requiresResourceExtraction !=
                    !string.IsNullOrEmpty(
                        transaction.SourceResourceExtractionOrderId) ||
                    requiresMilitaryLogistics !=
                    !string.IsNullOrEmpty(
                        transaction.SourceMilitaryLogisticsOrderId) ||
                    transaction.Type ==
                        InventoryTransactionType.LegacyBalanceConverted &&
                    transaction.LegacyFamilyGrainDelta == 0 &&
                    transaction.LegacyFamilySeedGrainDelta == 0 ||
                    transaction.Type == InventoryTransactionType.OpeningBalance &&
                    (transaction.LegacyFamilyGrainDelta != 0 ||
                     transaction.LegacyFamilySeedGrainDelta != 0) ||
                    formalization &&
                    (!familyFormalization &&
                     !villageFormalization &&
                     !countyFormalization ||
                     transaction.LegacyFamilySeedGrainDelta != 0) ||
                    foodRuntime &&
                    (!validFoodRuntimeProvenance ||
                     transaction.LegacyFamilyGrainDelta != 0 ||
                     transaction.LegacyFamilySeedGrainDelta != 0 ||
                     transaction.LegacyVillagePublicGranaryDelta != 0 ||
                     transaction.LegacyCountyGranaryDelta != 0) ||
                    marketInventory && !validMarketProvenance ||
                    civilianInventory && !validCivilianProvenance ||
                    !marketInventory && !civilianDispatch &&
                    !string.IsNullOrEmpty(
                        transaction.SourceFormalMarketOrderId) ||
                    !civilianInventory &&
                    !string.IsNullOrEmpty(
                        transaction.SourceCivilianFreightId) ||
                    !formalization && !foodRuntime && !marketInventory &&
                    !civilianInventory &&
                    (transaction.LegacyVillagePublicGranaryDelta != 0 ||
                     transaction.LegacyCountyGranaryDelta != 0 ||
                     !string.IsNullOrEmpty(transaction.SourceVillageId) ||
                     !string.IsNullOrEmpty(
                         transaction.SourceCountyGovernanceId)))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has invalid provenance.");
                }

                long formalizationQuantity = 0;
                var formalizationFamilyId = string.Empty;
                long transferWeightDelta = 0;
                var transferProductDeltas = new Dictionary<string, long>(
                    StringComparer.Ordinal);
                var hasTransferSource = false;
                var hasTransferDestination = false;
                var runtimeVillage = foodRuntime
                    ? FindVillageById(Villages, transaction.SourceVillageId)
                    : null;
                var runtimeCounty = countyRelief || taxRemittance ||
                    publicReliefProcurement
                    ? FindCountyGovernanceById(
                        CountyGovernances,
                        transaction.SourceCountyGovernanceId)
                    : null;
                agricultureOrders.TryGetValue(
                    transaction.SourceWorkOrderId ?? string.Empty,
                    out var harvestOrder);
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
                        line.QuantityDelta > 0 &&
                        batch.SourceTransactionId != transaction.Id ||
                        line.QuantityDelta == 0 && line.ReservedQuantityDelta == 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid line on inventory transaction {transaction.Id}.");
                    }

                    if (formalization)
                    {
                        if (line.QuantityDelta <= 0 ||
                            line.ReservedQuantityDelta != 0)
                        {
                            throw new InvalidOperationException(
                                $"Formalization transaction {transaction.Id} must only create stock.");
                        }

                        formalizationQuantity = checked(
                            formalizationQuantity + line.QuantityDelta);
                        if (familyFormalization)
                        {
                            if (string.IsNullOrEmpty(line.OwnerFamilyId) ||
                                !string.IsNullOrEmpty(
                                    line.OwnerOrganizationId) ||
                                formalizationFamilyId.Length != 0 &&
                                formalizationFamilyId != line.OwnerFamilyId)
                            {
                                throw new InvalidOperationException(
                                    $"Family formalization {transaction.Id} changes inventory owner.");
                            }

                            formalizationFamilyId = line.OwnerFamilyId;
                        }
                        else if (villageFormalization)
                        {
                            var village = FindVillageById(
                                Villages, transaction.SourceVillageId);
                            if (line.InventoryContainerId !=
                                village.PublicGranaryInventoryContainerId)
                            {
                                throw new InvalidOperationException(
                                    $"Village formalization {transaction.Id} targets the wrong granary.");
                            }
                        }
                        else
                        {
                            var governance = FindCountyGovernanceById(
                                CountyGovernances,
                                transaction.SourceCountyGovernanceId);
                            if (line.InventoryContainerId !=
                                governance.GranaryInventoryContainerId)
                            {
                                throw new InvalidOperationException(
                                    $"County formalization {transaction.Id} targets the wrong granary.");
                            }
                        }
                    }

                    if (foodHarvest)
                    {
                        if (line.QuantityDelta <= 0 ||
                            line.ReservedQuantityDelta != 0 ||
                            harvestOrder == null ||
                            transaction.SourceVillageId != harvestOrder.VillageId ||
                            line.OwnerFamilyId != harvestOrder.FamilyId ||
                            line.StorageFacilityId !=
                                harvestOrder.StorageFacilityId ||
                            !string.IsNullOrEmpty(line.InventoryContainerId) ||
                            batch.SourceWorkOrderId != harvestOrder.Id ||
                            line.ProductDefinitionId !=
                                harvestOrder.HarvestProductDefinitionId)
                        {
                            throw new InvalidOperationException(
                                $"Food harvest transaction {transaction.Id} has an invalid destination.");
                        }
                    }

                    if (foodTransfer)
                    {
                        if (line.ReservedQuantityDelta != 0)
                        {
                            throw new InvalidOperationException(
                                $"Food transfer {transaction.Id} cannot change reservations.");
                        }

                        var isSource = line.QuantityDelta < 0;
                        var isDestination = line.QuantityDelta > 0;
                        var villageContainerId = runtimeVillage
                            .PublicGranaryInventoryContainerId;
                        var countyContainerId = runtimeCounty == null
                            ? string.Empty
                            : runtimeCounty.GranaryInventoryContainerId;
                        var validLine =
                            foodTax &&
                            (isSource &&
                                 !string.IsNullOrEmpty(line.OwnerFamilyId) &&
                                 !string.IsNullOrEmpty(line.StorageFacilityId) ||
                             isDestination &&
                                 line.InventoryContainerId == villageContainerId) ||
                            villageRelief &&
                            (isSource &&
                                 line.InventoryContainerId == villageContainerId ||
                             isDestination &&
                                 !string.IsNullOrEmpty(line.OwnerFamilyId) &&
                                 !string.IsNullOrEmpty(line.StorageFacilityId)) ||
                            countyRelief &&
                            (isSource &&
                                 line.InventoryContainerId == countyContainerId ||
                             isDestination &&
                                 line.InventoryContainerId == villageContainerId) ||
                            taxRemittance &&
                            (isSource &&
                                 line.InventoryContainerId == villageContainerId ||
                             isDestination &&
                                 line.InventoryContainerId == countyContainerId);
                        if (!validLine)
                        {
                            throw new InvalidOperationException(
                                $"Food transfer {transaction.Id} crosses an invalid ownership boundary.");
                        }

                        hasTransferSource |= isSource;
                        hasTransferDestination |= isDestination;
                        AddDelta(
                            transferProductDeltas,
                            line.ProductDefinitionId,
                            line.QuantityDelta);
                        transferWeightDelta = checked(
                            transferWeightDelta +
                            line.QuantityDelta * batch.UnitWeight);
                    }

                    if (marketReserved || marketReleased)
                    {
                        var validReservationLine =
                            line.QuantityDelta == 0 &&
                            (marketReserved &&
                                 line.ReservedQuantityDelta > 0 ||
                             marketReleased &&
                                 line.ReservedQuantityDelta < 0) &&
                            line.OwnerFamilyId ==
                                formalMarketOrder.OwnerFamilyId &&
                            line.StorageFacilityId ==
                                formalMarketOrder.StorageFacilityId &&
                            line.ProductDefinitionId ==
                                formalMarketOrder.ProductDefinitionId &&
                            string.IsNullOrEmpty(line.InventoryContainerId);
                        if (!validReservationLine)
                        {
                            throw new InvalidOperationException(
                                $"Formal market reservation transaction {transaction.Id} is invalid.");
                        }
                    }

                    if (marketTransferred)
                    {
                        var isSource = line.QuantityDelta < 0;
                        var isDestination = line.QuantityDelta > 0;
                        var validMarketLine =
                            line.ProductDefinitionId ==
                                formalMarketOrder.ProductDefinitionId &&
                            (isSource &&
                                 line.OwnerFamilyId ==
                                     formalMarketOrder.OwnerFamilyId &&
                                 line.StorageFacilityId ==
                                     formalMarketOrder.StorageFacilityId &&
                                 line.ReservedQuantityDelta ==
                                     line.QuantityDelta ||
                             isDestination &&
                                 !string.IsNullOrEmpty(line.OwnerFamilyId) &&
                                 line.OwnerFamilyId !=
                                     formalMarketOrder.OwnerFamilyId &&
                                 !string.IsNullOrEmpty(
                                     line.StorageFacilityId) &&
                                 line.ReservedQuantityDelta == 0);
                        if (!validMarketLine)
                        {
                            throw new InvalidOperationException(
                                $"Formal market delivery {transaction.Id} crosses an invalid ownership boundary.");
                        }

                        hasTransferSource |= isSource;
                        hasTransferDestination |= isDestination;
                        AddDelta(
                            transferProductDeltas,
                            line.ProductDefinitionId,
                            line.QuantityDelta);
                        transferWeightDelta = checked(
                            transferWeightDelta +
                            line.QuantityDelta * batch.UnitWeight);
                    }

                    if (publicReliefProcurement)
                    {
                        var isSource = line.QuantityDelta < 0;
                        var isDestination = line.QuantityDelta > 0;
                        var validProcurementLine =
                            line.ProductDefinitionId ==
                                formalMarketOrder.ProductDefinitionId &&
                            (isSource &&
                                 line.OwnerFamilyId ==
                                     formalMarketOrder.OwnerFamilyId &&
                                 line.StorageFacilityId ==
                                     formalMarketOrder.StorageFacilityId &&
                                 string.IsNullOrEmpty(
                                     line.InventoryContainerId) &&
                                 line.ReservedQuantityDelta ==
                                     line.QuantityDelta ||
                             isDestination &&
                                 string.IsNullOrEmpty(line.OwnerFamilyId) &&
                                 line.OwnerOrganizationId ==
                                     runtimeCounty.GovernmentOrganizationId &&
                                 string.IsNullOrEmpty(
                                     line.StorageFacilityId) &&
                                 line.InventoryContainerId ==
                                     runtimeCounty
                                         .GranaryInventoryContainerId &&
                                 line.ReservedQuantityDelta == 0);
                        if (!validProcurementLine)
                        {
                            throw new InvalidOperationException(
                                $"Public relief procurement delivery {transaction.Id} crosses an invalid ownership boundary.");
                        }

                        hasTransferSource |= isSource;
                        hasTransferDestination |= isDestination;
                        AddDelta(
                            transferProductDeltas,
                            line.ProductDefinitionId,
                            line.QuantityDelta);
                        transferWeightDelta = checked(
                            transferWeightDelta +
                            line.QuantityDelta * batch.UnitWeight);
                    }

                    if (civilianInventory)
                    {
                        var isSource = line.QuantityDelta < 0;
                        var isDestination = line.QuantityDelta > 0;
                        var publicReliefFreight =
                            !string.IsNullOrEmpty(
                                civilianFreight.BuyerOrganizationId);
                        var validCargoOwner = publicReliefFreight
                            ? string.IsNullOrEmpty(line.OwnerFamilyId) &&
                              line.OwnerOrganizationId ==
                                  civilianFreight.BuyerOrganizationId
                            : line.OwnerFamilyId ==
                                  civilianFreight.BuyerFamilyId &&
                              string.IsNullOrEmpty(
                                  line.OwnerOrganizationId);
                        var validCivilianLine =
                            civilianDispatch &&
                            (isSource &&
                                 line.OwnerFamilyId ==
                                     civilianFreight.SellerFamilyId &&
                                 line.StorageFacilityId ==
                                     civilianFreight.SellerStorageFacilityId &&
                                 line.ReservedQuantityDelta ==
                                     line.QuantityDelta ||
                             isDestination &&
                                 validCargoOwner &&
                                 line.InventoryContainerId ==
                                     civilianFreight
                                         .TransportInventoryContainerId &&
                                 line.ReservedQuantityDelta == 0) ||
                            civilianLoss && isSource &&
                                validCargoOwner &&
                                line.InventoryContainerId ==
                                    civilianFreight
                                        .TransportInventoryContainerId &&
                                line.ReservedQuantityDelta == 0 ||
                            civilianDelivery &&
                            (isSource &&
                                 validCargoOwner &&
                                 line.InventoryContainerId ==
                                     civilianFreight
                                         .TransportInventoryContainerId &&
                                 line.ReservedQuantityDelta == 0 ||
                             isDestination &&
                                 (publicReliefFreight
                                    ? validCargoOwner &&
                                      line.InventoryContainerId ==
                                          civilianFreight
                                              .DestinationInventoryContainerId
                                    : validCargoOwner &&
                                      line.StorageFacilityId ==
                                          civilianFreight
                                              .BuyerStorageFacilityId) &&
                                 line.ReservedQuantityDelta == 0);
                        if (!validCivilianLine ||
                            line.ProductDefinitionId !=
                                civilianFreight.ProductDefinitionId)
                        {
                            throw new InvalidOperationException(
                                $"Civilian freight inventory line on {transaction.Id} is invalid.");
                        }

                        if (!civilianLoss)
                        {
                            hasTransferSource |= isSource;
                            hasTransferDestination |= isDestination;
                            AddDelta(
                                transferProductDeltas,
                                line.ProductDefinitionId,
                                line.QuantityDelta);
                            transferWeightDelta = checked(
                                transferWeightDelta +
                                line.QuantityDelta * batch.UnitWeight);
                        }
                    }

                    AddDelta(quantityDeltas, line.BatchId, line.QuantityDelta);
                    AddDelta(
                        reservationDeltas,
                        line.BatchId,
                        line.ReservedQuantityDelta);
                }

                if (formalization && formalizationQuantity != checked(-(
                        transaction.LegacyFamilyGrainDelta +
                        transaction.LegacyVillagePublicGranaryDelta +
                        transaction.LegacyCountyGranaryDelta)))
                {
                    throw new InvalidOperationException(
                        $"Formalization transaction {transaction.Id} is not conservative.");
                }

                if ((foodTransfer || marketTransferred ||
                     publicReliefProcurement || civilianDispatch ||
                     civilianDelivery) &&
                    (!hasTransferSource || !hasTransferDestination ||
                     transferWeightDelta != 0))
                {
                    throw new InvalidOperationException(
                        $"Food transfer {transaction.Id} is not physically conservative.");
                }
                if (foodTransfer || marketTransferred ||
                    publicReliefProcurement || civilianDispatch ||
                    civilianDelivery)
                {
                    foreach (var pair in transferProductDeltas)
                    {
                        if (pair.Value != 0)
                        {
                            throw new InvalidOperationException(
                                $"Food transfer {transaction.Id} changes product identity.");
                        }
                    }
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

        private void ValidateCivilianFreightPlanning(
            HashSet<string> personIds,
            IDictionary<string, FamilyState> families,
            IDictionary<string, InventoryContainerState> containers,
            IDictionary<string, RouteState> routes,
            IDictionary<string, FormalMarketOrderState> orders,
            IDictionary<string, CivilianFreightState> freights)
        {
            var demands = new Dictionary<string, CivilianFreightDemandState>(
                StringComparer.Ordinal);
            var registrations =
                new Dictionary<string, CivilianCarrierRegistrationState>(
                    StringComparer.Ordinal);
            var offers = new Dictionary<string, CivilianCarrierOfferState>(
                StringComparer.Ordinal);
            var activeOrderDemands = new HashSet<string>(
                StringComparer.Ordinal);
            var activeCarrierPeople = new HashSet<string>(
                StringComparer.Ordinal);
            var activeCarrierContainers = new HashSet<string>(
                StringComparer.Ordinal);

            for (var i = 0; i < CivilianFreightDemands.Count; i++)
            {
                demands.Add(CivilianFreightDemands[i].Id,
                    CivilianFreightDemands[i]);
            }
            for (var i = 0; i < CivilianCarrierRegistrations.Count; i++)
            {
                registrations.Add(CivilianCarrierRegistrations[i].Id,
                    CivilianCarrierRegistrations[i]);
            }
            for (var i = 0; i < CivilianCarrierOffers.Count; i++)
            {
                offers.Add(CivilianCarrierOffers[i].Id,
                    CivilianCarrierOffers[i]);
            }

            for (var i = 0; i < CivilianCarrierRegistrations.Count; i++)
            {
                var registration = CivilianCarrierRegistrations[i] ??
                    throw new InvalidOperationException(
                        "A civilian carrier registration cannot be null.");
                var carrier = FindPerson(
                    People, registration.CarrierPersonId);
                var knownRoutes = new HashSet<string>(StringComparer.Ordinal);
                if (carrier == null ||
                    !personIds.Contains(registration.CarrierPersonId) ||
                    !families.TryGetValue(
                        registration.CarrierFamilyId,
                        out var carrierFamily) ||
                    !containers.TryGetValue(
                        registration.TransportInventoryContainerId,
                        out var container) ||
                    carrier.FamilyId != carrierFamily.Id ||
                    container.OwnerFamilyId != carrierFamily.Id ||
                    !string.IsNullOrEmpty(container.OwnerOrganizationId) ||
                    container.CarrierPersonId != carrier.Id ||
                    registration.BaseFee < 0 ||
                    registration.FeePerKilometer < 0 ||
                    registration.FeePerHundredUnits < 0 ||
                    registration.MaximumDistanceKilometers <= 0 ||
                    string.IsNullOrWhiteSpace(registration.RoutePolicyId) ||
                    registration.RegisteredDay < 0 ||
                    registration.RegisteredDay > AbsoluteDay ||
                    registration.KnownRouteIds == null ||
                    registration.KnownRouteIds.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian carrier registration {registration.Id}.");
                }
                for (var routeIndex = 0;
                     routeIndex < registration.KnownRouteIds.Count;
                     routeIndex++)
                {
                    var routeId = registration.KnownRouteIds[routeIndex];
                    if (!routes.ContainsKey(routeId) ||
                        !knownRoutes.Add(routeId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid known route in carrier registration {registration.Id}.");
                    }
                }
                if (registration.Active &&
                    (!activeCarrierPeople.Add(registration.CarrierPersonId) ||
                     !activeCarrierContainers.Add(
                        registration.TransportInventoryContainerId)))
                {
                    throw new InvalidOperationException(
                        $"Duplicate active civilian carrier registration {registration.Id}.");
                }
            }

            for (var i = 0; i < CivilianFreightDemands.Count; i++)
            {
                var demand = CivilianFreightDemands[i];
                var hasBuy = orders.TryGetValue(demand.BuyOrderId, out var buy);
                var hasSell = orders.TryGetValue(demand.SellOrderId, out var sell);
                FamilyState buyerFamily = null;
                FamilyState sellerFamily = null;
                var hasBuyerFamily = hasBuy && families.TryGetValue(
                    buy.OwnerFamilyId, out buyerFamily);
                var hasSellerFamily = hasSell && families.TryGetValue(
                    sell.OwnerFamilyId, out sellerFamily);
                var active = demand.Status == CivilianFreightDemandStatus.Active;
                var dispatched = demand.Status ==
                    CivilianFreightDemandStatus.Dispatched;
                if (!Enum.IsDefined(
                        typeof(CivilianFreightDemandStatus), demand.Status) ||
                    !hasBuy || !hasSell || !hasBuyerFamily ||
                    !hasSellerFamily ||
                    buy.Side != FormalMarketOrderSide.Buy ||
                    sell.Side != FormalMarketOrderSide.Sell ||
                    buy.CountyGovernanceId == sell.CountyGovernanceId ||
                    buy.ProductDefinitionId != sell.ProductDefinitionId ||
                    sell.UnitPrice > buy.UnitPrice ||
                    demand.OriginCountyGovernanceId != sell.CountyGovernanceId ||
                    demand.DestinationCountyGovernanceId !=
                        buy.CountyGovernanceId ||
                    demand.ProductDefinitionId != buy.ProductDefinitionId ||
                    demand.OriginLocationId != sellerFamily.LocationId ||
                    demand.DestinationLocationId != buyerFamily.LocationId ||
                    demand.Quantity <= 0 ||
                    demand.Quantity > buy.OriginalQuantity ||
                    demand.Quantity > sell.OriginalQuantity ||
                    demand.MaximumFreightFee < 0 ||
                    string.IsNullOrWhiteSpace(demand.RoutePolicyId) ||
                    demand.CreatedDay < 0 ||
                    demand.ExpiryDay < demand.CreatedDay ||
                    demand.CreatedDay > AbsoluteDay ||
                    active &&
                        (demand.ClosedDay != -1 ||
                         !string.IsNullOrEmpty(demand.AcceptedOfferId) ||
                         !string.IsNullOrEmpty(demand.CivilianFreightId) ||
                         buy.Status != FormalMarketOrderStatus.Active ||
                         sell.Status != FormalMarketOrderStatus.Active ||
                         demand.Quantity > buy.RemainingQuantity ||
                         demand.Quantity > sell.RemainingQuantity) ||
                    dispatched &&
                        (demand.ClosedDay < demand.CreatedDay ||
                         !offers.TryGetValue(
                            demand.AcceptedOfferId, out var acceptedOffer) ||
                         !freights.TryGetValue(
                            demand.CivilianFreightId, out var freight) ||
                         acceptedOffer.Status !=
                            CivilianCarrierOfferStatus.Accepted ||
                         acceptedOffer.DemandId != demand.Id ||
                         acceptedOffer.CivilianFreightId != freight.Id ||
                         freight.DemandId != demand.Id ||
                         freight.CarrierOfferId != acceptedOffer.Id) ||
                    !active && !dispatched &&
                        (demand.ClosedDay < demand.CreatedDay ||
                         !string.IsNullOrEmpty(demand.AcceptedOfferId) ||
                         !string.IsNullOrEmpty(demand.CivilianFreightId)))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian freight demand {demand.Id}.");
                }
                if (active &&
                    (!activeOrderDemands.Add(demand.BuyOrderId) ||
                     !activeOrderDemands.Add(demand.SellOrderId)))
                {
                    throw new InvalidOperationException(
                        $"Civilian freight demand {demand.Id} reuses an active order.");
                }
            }

            for (var i = 0; i < CivilianCarrierOffers.Count; i++)
            {
                var offer = CivilianCarrierOffers[i] ??
                    throw new InvalidOperationException(
                        "A civilian carrier offer cannot be null.");
                var hasDemand = demands.TryGetValue(
                    offer.DemandId, out var demand);
                var hasRegistration = registrations.TryGetValue(
                    offer.CarrierRegistrationId, out var registration);
                var totalDistance = 0;
                var minimumSecurity = 0;
                var routePlanValid = hasDemand && TryBuildCivilianRoutePlan(
                    routes,
                    offer.PlannedRouteIds,
                    demand.OriginLocationId,
                    demand.DestinationLocationId,
                    out _,
                    out _,
                    out totalDistance,
                    out minimumSecurity);
                var knownRoutes = hasRegistration
                    ? new HashSet<string>(
                        registration.KnownRouteIds, StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal);
                var allKnown = routePlanValid;
                if (routePlanValid)
                {
                    for (var routeIndex = 0;
                         routeIndex < offer.PlannedRouteIds.Count;
                         routeIndex++)
                    {
                        allKnown &= knownRoutes.Contains(
                            offer.PlannedRouteIds[routeIndex]);
                    }
                }
                long expectedFee = -1;
                if (hasDemand && hasRegistration)
                {
                    expectedFee = checked(
                        registration.BaseFee +
                        registration.FeePerKilometer * totalDistance +
                        registration.FeePerHundredUnits *
                            ((demand.Quantity + 99) / 100));
                }
                var active = offer.Status == CivilianCarrierOfferStatus.Active;
                var accepted = offer.Status ==
                    CivilianCarrierOfferStatus.Accepted;
                if (!Enum.IsDefined(
                        typeof(CivilianCarrierOfferStatus), offer.Status) ||
                    !hasDemand || !hasRegistration || !routePlanValid ||
                    !allKnown ||
                    offer.CarrierPersonId != registration.CarrierPersonId ||
                    offer.CarrierFamilyId != registration.CarrierFamilyId ||
                    offer.TransportInventoryContainerId !=
                        registration.TransportInventoryContainerId ||
                    offer.RoutePolicyId != demand.RoutePolicyId ||
                    registration.RoutePolicyId != demand.RoutePolicyId ||
                    offer.TotalDistanceKilometers != totalDistance ||
                    offer.MinimumSecurityBasisPoints != minimumSecurity ||
                    totalDistance > registration.MaximumDistanceKilometers ||
                    offer.QuotedFreightFee != expectedFee ||
                    offer.QuotedFreightFee > demand.MaximumFreightFee ||
                    offer.CreatedDay < demand.CreatedDay ||
                    offer.CreatedDay > AbsoluteDay ||
                    active &&
                        (!registration.Active ||
                         demand.Status != CivilianFreightDemandStatus.Active ||
                         offer.ClosedDay != -1 ||
                         !string.IsNullOrEmpty(offer.CivilianFreightId)) ||
                    accepted &&
                        (demand.Status !=
                            CivilianFreightDemandStatus.Dispatched ||
                         demand.AcceptedOfferId != offer.Id ||
                         demand.CivilianFreightId !=
                            offer.CivilianFreightId ||
                         offer.ClosedDay < offer.CreatedDay) ||
                    !active && !accepted &&
                        (offer.ClosedDay < offer.CreatedDay ||
                         !string.IsNullOrEmpty(offer.CivilianFreightId)))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian carrier offer {offer.Id}.");
                }
            }

            for (var i = 0; i < CivilianFreights.Count; i++)
            {
                var freight = CivilianFreights[i];
                var hasPlanningLink = !string.IsNullOrEmpty(freight.DemandId) ||
                    !string.IsNullOrEmpty(freight.CarrierOfferId);
                if (hasPlanningLink &&
                    (!demands.TryGetValue(freight.DemandId, out var demand) ||
                     !offers.TryGetValue(
                        freight.CarrierOfferId, out var offer) ||
                     demand.Status != CivilianFreightDemandStatus.Dispatched ||
                     offer.Status != CivilianCarrierOfferStatus.Accepted ||
                     demand.CivilianFreightId != freight.Id ||
                     offer.CivilianFreightId != freight.Id) ||
                    !hasPlanningLink &&
                        (!string.IsNullOrEmpty(freight.DemandId) ||
                         !string.IsNullOrEmpty(freight.CarrierOfferId)))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian freight planning link {freight.Id}.");
                }
            }
        }

        private static bool TryBuildCivilianRoutePlan(
            IDictionary<string, RouteState> routes,
            IList<string> routeIds,
            string originLocationId,
            string destinationLocationId,
            out List<string> legOrigins,
            out List<string> legDestinations,
            out int totalDistance,
            out int minimumSecurity)
        {
            legOrigins = new List<string>();
            legDestinations = new List<string>();
            totalDistance = 0;
            minimumSecurity = 10_000;
            if (routeIds == null || routeIds.Count == 0 ||
                string.IsNullOrEmpty(originLocationId) ||
                string.IsNullOrEmpty(destinationLocationId))
            {
                return false;
            }

            var current = originLocationId;
            var visited = new HashSet<string>(StringComparer.Ordinal)
            {
                current
            };
            long distance = 0;
            for (var i = 0; i < routeIds.Count; i++)
            {
                if (!routes.TryGetValue(routeIds[i], out var route))
                {
                    return false;
                }
                string next;
                if (route.FromLocationId == current)
                {
                    next = route.ToLocationId;
                }
                else if (route.Bidirectional &&
                    route.ToLocationId == current)
                {
                    next = route.FromLocationId;
                }
                else
                {
                    return false;
                }
                if (!visited.Add(next))
                {
                    return false;
                }
                legOrigins.Add(current);
                legDestinations.Add(next);
                distance += route.DistanceKilometers;
                if (distance > int.MaxValue)
                {
                    return false;
                }
                minimumSecurity = Math.Min(
                    minimumSecurity, route.SecurityBasisPoints);
                current = next;
            }
            totalDistance = (int)distance;
            return current == destinationLocationId;
        }

        private void ValidateFormalMarket(HashSet<string> locationIds)
        {
            var families = new Dictionary<string, FamilyState>(
                StringComparer.Ordinal);
            var governances = new Dictionary<string, CountyGovernanceState>(
                StringComparer.Ordinal);
            var facilities = new Dictionary<string, VillageFacilityState>(
                StringComparer.Ordinal);
            var organizations = new Dictionary<string, OrganizationState>(
                StringComparer.Ordinal);
            var containers = new Dictionary<string, InventoryContainerState>(
                StringComparer.Ordinal);
            var commands =
                new Dictionary<string, PersistentWorldCommandState>(
                    StringComparer.Ordinal);
            var outbox = new Dictionary<string, WorldEventOutboxState>(
                StringComparer.Ordinal);
            var batches = new Dictionary<string, ProductBatchState>(
                StringComparer.Ordinal);
            var transactions =
                new Dictionary<string, InventoryTransactionState>(
                    StringComparer.Ordinal);
            var transactionSequenceById = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var orders = new Dictionary<string, FormalMarketOrderState>(
                StringComparer.Ordinal);
            var tradedQuantityByOrder = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var settledMoneyByOrder = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var reservedQuantityByBatch = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var tradedQuantityByMarket = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var turnoverByMarket = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var lastTradeDayByMarket = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var lastTradePriceByMarket = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var lastTradeTransactionSequenceByMarket =
                new Dictionary<string, int>(
                StringComparer.Ordinal);

            for (var i = 0; i < Families.Count; i++)
            {
                families.Add(Families[i].Id, Families[i]);
            }

            for (var i = 0; i < CountyGovernances.Count; i++)
            {
                governances.Add(CountyGovernances[i].Id, CountyGovernances[i]);
            }

            for (var i = 0; i < Organizations.Count; i++)
            {
                organizations.Add(Organizations[i].Id, Organizations[i]);
            }

            for (var i = 0; i < InventoryContainers.Count; i++)
            {
                containers.Add(InventoryContainers[i].Id, InventoryContainers[i]);
            }

            for (var i = 0; i < PersistentWorldCommands.Count; i++)
            {
                commands.Add(
                    PersistentWorldCommands[i].Id,
                    PersistentWorldCommands[i]);
            }

            for (var i = 0; i < WorldEventOutbox.Count; i++)
            {
                outbox.Add(WorldEventOutbox[i].Id, WorldEventOutbox[i]);
            }

            for (var i = 0; i < VillageFacilities.Count; i++)
            {
                facilities.Add(VillageFacilities[i].Id, VillageFacilities[i]);
            }

            for (var i = 0; i < ProductBatches.Count; i++)
            {
                batches.Add(ProductBatches[i].Id, ProductBatches[i]);
            }

            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                transactions.Add(
                    InventoryTransactions[i].Id,
                    InventoryTransactions[i]);
                transactionSequenceById.Add(
                    InventoryTransactions[i].Id, i);
            }

            for (var i = 0; i < FormalMarketOrders.Count; i++)
            {
                var order = FormalMarketOrders[i] ??
                    throw new InvalidOperationException(
                        "A formal market order cannot be null.");
                orders.Add(order.Id, order);
                ValidateContentReference(
                    order.ProductDefinitionId,
                    "formal market product",
                    order.Id);

                var hasGovernance = governances.TryGetValue(
                    order.CountyGovernanceId, out var governance);
                var hasFamily = families.ContainsKey(order.OwnerFamilyId);
                var hasFacility = facilities.TryGetValue(
                    order.StorageFacilityId, out var facility);
                var active = order.Status == FormalMarketOrderStatus.Active;
                var filled = order.Status == FormalMarketOrderStatus.Filled;
                var closed = order.Status == FormalMarketOrderStatus.Cancelled ||
                    order.Status == FormalMarketOrderStatus.Expired;
                if (!Enum.IsDefined(
                        typeof(FormalMarketOrderSide), order.Side) ||
                    !Enum.IsDefined(
                        typeof(FormalMarketOrderStatus), order.Status) ||
                    !hasGovernance || !hasFamily || !hasFacility ||
                    !locationIds.Contains(governance.CountyLocationId) ||
                    !FamilyBelongsToCounty(
                        order.OwnerFamilyId, governance.CountyLocationId) ||
                    facility.Kind != VillageFacilityKind.HouseholdGranary ||
                    facility.OwnerFamilyId != order.OwnerFamilyId ||
                    order.CreatedDay < 0 || order.CreatedDay > AbsoluteDay ||
                    order.ExpiryDay < order.CreatedDay ||
                    order.OriginalQuantity <= 0 ||
                    order.RemainingQuantity < 0 ||
                    order.RemainingQuantity > order.OriginalQuantity ||
                    order.FilledQuantity !=
                        order.OriginalQuantity - order.RemainingQuantity ||
                    order.UnitPrice <= 0 ||
                    order.MinimumQualityBasisPoints < 0 ||
                    order.MinimumQualityBasisPoints > 10_000 ||
                    order.EscrowMoney < 0 || order.SettledMoney < 0 ||
                    active && (order.RemainingQuantity <= 0 ||
                               order.ClosedDay != -1 ||
                               !string.IsNullOrEmpty(order.CloseReason)) ||
                    filled && (order.RemainingQuantity != 0 ||
                               order.ClosedDay < order.CreatedDay) ||
                    closed && (order.ClosedDay < order.CreatedDay ||
                               string.IsNullOrEmpty(order.CloseReason)) ||
                    !active && order.ClosedDay > AbsoluteDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid formal market order {order.Id}.");
                }

                if (order.Side == FormalMarketOrderSide.Buy)
                {
                    if (order.BatchReservations.Count != 0 ||
                        active && order.EscrowMoney < checked(
                            order.RemainingQuantity * order.UnitPrice) ||
                        !active && order.EscrowMoney != 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid formal market buy escrow {order.Id}.");
                    }

                    continue;
                }

                if (order.EscrowMoney != 0 ||
                    order.BatchReservations.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid formal market sell reservation {order.Id}.");
                }

                long originalReserved = 0;
                long remainingReserved = 0;
                var reservationBatchIds = new HashSet<string>(
                    StringComparer.Ordinal);
                for (var reservationIndex = 0;
                     reservationIndex < order.BatchReservations.Count;
                     reservationIndex++)
                {
                    var reservation = order.BatchReservations[reservationIndex];
                    if (reservation == null ||
                        !reservationBatchIds.Add(reservation.BatchId) ||
                        !batches.TryGetValue(
                            reservation.BatchId, out var batch) ||
                        reservation.OriginalQuantity <= 0 ||
                        reservation.RemainingQuantity < 0 ||
                        reservation.RemainingQuantity >
                            reservation.OriginalQuantity ||
                        batch.OwnerFamilyId != order.OwnerFamilyId ||
                        batch.StorageFacilityId != order.StorageFacilityId ||
                        batch.ProductDefinitionId !=
                            order.ProductDefinitionId)
                    {
                        throw new InvalidOperationException(
                            $"Invalid batch reservation on formal market order {order.Id}.");
                    }

                    originalReserved = checked(
                        originalReserved + reservation.OriginalQuantity);
                    remainingReserved = checked(
                        remainingReserved + reservation.RemainingQuantity);
                    if (active)
                    {
                        AddDelta(
                            reservedQuantityByBatch,
                            reservation.BatchId,
                            reservation.RemainingQuantity);
                    }
                }

                if (originalReserved != order.OriginalQuantity ||
                    active && remainingReserved != order.RemainingQuantity ||
                    !active && remainingReserved != 0)
                {
                    throw new InvalidOperationException(
                        $"Formal market reservation total mismatch on {order.Id}.");
                }
            }

            for (var i = 0; i < FormalMarketTrades.Count; i++)
            {
                var trade = FormalMarketTrades[i] ??
                    throw new InvalidOperationException(
                        "A formal market trade cannot be null.");
                var hasBuy = orders.TryGetValue(
                    trade.BuyOrderId, out var buy);
                var hasSell = orders.TryGetValue(
                    trade.SellOrderId, out var sell);
                var hasTransaction = transactions.TryGetValue(
                    trade.InventoryTransactionId, out var transaction);
                var crossCounty = !string.IsNullOrEmpty(
                    trade.CivilianFreightId);
                if (!hasBuy || !hasSell || !hasTransaction ||
                    trade.Day < 0 || trade.Day > AbsoluteDay ||
                    buy.Side != FormalMarketOrderSide.Buy ||
                    sell.Side != FormalMarketOrderSide.Sell ||
                    trade.CountyGovernanceId != sell.CountyGovernanceId ||
                    trade.DestinationCountyGovernanceId !=
                        buy.CountyGovernanceId ||
                    !governances.ContainsKey(
                        trade.DestinationCountyGovernanceId) ||
                    crossCounty !=
                        (buy.CountyGovernanceId != sell.CountyGovernanceId) ||
                    trade.BuyerFamilyId != buy.OwnerFamilyId ||
                    trade.SellerFamilyId != sell.OwnerFamilyId ||
                    trade.BuyerFamilyId == trade.SellerFamilyId ||
                    trade.ProductDefinitionId != buy.ProductDefinitionId ||
                    trade.ProductDefinitionId != sell.ProductDefinitionId ||
                    trade.Quantity <= 0 || trade.UnitPrice <= 0 ||
                    trade.UnitPrice > buy.UnitPrice ||
                    trade.UnitPrice != sell.UnitPrice ||
                    trade.MoneyTransferred != checked(
                        trade.Quantity * trade.UnitPrice) ||
                    trade.SellerProceeds != trade.MoneyTransferred ||
                    !crossCounty && transaction.Type !=
                        InventoryTransactionType.FoodMarketTransferred ||
                    crossCounty && transaction.Type !=
                        InventoryTransactionType.CivilianFreightDispatched ||
                    transaction.SourceFormalMarketOrderId != sell.Id ||
                    crossCounty &&
                        transaction.SourceCivilianFreightId !=
                            trade.CivilianFreightId ||
                    !crossCounty &&
                        !string.IsNullOrEmpty(
                            transaction.SourceCivilianFreightId) ||
                    transaction.Day != trade.Day)
                {
                    throw new InvalidOperationException(
                        $"Invalid formal market trade {trade.Id}.");
                }

                long sourceQuantity = 0;
                long destinationQuantity = 0;
                for (var lineIndex = 0;
                     lineIndex < transaction.Lines.Count;
                     lineIndex++)
                {
                    var line = transaction.Lines[lineIndex];
                    if (line.QuantityDelta < 0 &&
                        line.OwnerFamilyId == sell.OwnerFamilyId)
                    {
                        sourceQuantity = checked(
                            sourceQuantity - line.QuantityDelta);
                    }
                    else if (line.QuantityDelta > 0 &&
                             line.OwnerFamilyId == buy.OwnerFamilyId)
                    {
                        destinationQuantity = checked(
                            destinationQuantity + line.QuantityDelta);
                    }
                }

                if (sourceQuantity != trade.Quantity ||
                    destinationQuantity != trade.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Formal market trade {trade.Id} does not match its inventory delivery.");
                }

                AddDelta(tradedQuantityByOrder, buy.Id, trade.Quantity);
                AddDelta(tradedQuantityByOrder, sell.Id, trade.Quantity);
                AddDelta(settledMoneyByOrder, buy.Id, trade.MoneyTransferred);
                AddDelta(settledMoneyByOrder, sell.Id, trade.MoneyTransferred);
                var marketKey = FormalMarketKey(
                    trade.CountyGovernanceId,
                    trade.ProductDefinitionId);
                AddDelta(
                    tradedQuantityByMarket, marketKey, trade.Quantity);
                AddDelta(
                    turnoverByMarket, marketKey, trade.MoneyTransferred);
                var transactionSequence =
                    transactionSequenceById[trade.InventoryTransactionId];
                if (!lastTradeTransactionSequenceByMarket.TryGetValue(
                        marketKey, out var lastTradeSequence) ||
                    transactionSequence > lastTradeSequence)
                {
                    lastTradeDayByMarket[marketKey] = trade.Day;
                    lastTradePriceByMarket[marketKey] = trade.UnitPrice;
                    lastTradeTransactionSequenceByMarket[marketKey] =
                        transactionSequence;
                }
            }


            for (var i = 0; i < PublicReliefProcurementTrades.Count; i++)
            {
                var trade = PublicReliefProcurementTrades[i] ??
                    throw new InvalidOperationException(
                        "A public relief procurement trade cannot be null.");
                var hasGovernance = governances.TryGetValue(
                    trade.CountyGovernanceId, out var governance);
                var hasSourceGovernance = governances.TryGetValue(
                    trade.SourceCountyGovernanceId,
                    out var sourceGovernance);
                var hasSeller = families.TryGetValue(
                    trade.SellerFamilyId, out var seller);
                var hasOrder = orders.TryGetValue(
                    trade.SellOrderId, out var sell);
                var hasOrganization = organizations.TryGetValue(
                    trade.BuyerOrganizationId, out var government);
                var hasContainer = containers.TryGetValue(
                    trade.DestinationInventoryContainerId,
                    out var destination);
                var hasTransaction = transactions.TryGetValue(
                    trade.InventoryTransactionId, out var transaction);
                var hasCommand = commands.TryGetValue(
                    trade.SourceCommandId, out var command);
                var hasEvent = outbox.TryGetValue(
                    trade.SourceShortfallEventId, out var sourceEvent);
                var external = !string.IsNullOrEmpty(
                    trade.CivilianFreightId);
                var supplemental = external &&
                    trade.IsSupplementalPublicReliefProcurement;
                var hasRecoveryLink = false;
                if (!string.IsNullOrEmpty(
                        trade.PublicReliefRecoveryId))
                {
                    for (var recoveryIndex = 0;
                         recoveryIndex < PublicReliefRecoveries.Count;
                         recoveryIndex++)
                    {
                        if (PublicReliefRecoveries[recoveryIndex].Id ==
                            trade.PublicReliefRecoveryId)
                        {
                            hasRecoveryLink = true;
                            break;
                        }
                    }
                }
                if (!hasGovernance || !hasSourceGovernance ||
                    !hasSeller || !hasOrder ||
                    !hasOrganization || !hasContainer || !hasTransaction ||
                    !hasCommand || !hasEvent ||
                    trade.Day < 0 || trade.Day > AbsoluteDay ||
                    government.Type != OrganizationType.Government ||
                    government.Id != governance.GovernmentOrganizationId ||
                    destination.Id !=
                        governance.GranaryInventoryContainerId ||
                    destination.OwnerOrganizationId != government.Id ||
                    sell.Side != FormalMarketOrderSide.Sell ||
                    sell.CountyGovernanceId != sourceGovernance.Id ||
                    external != (sourceGovernance.Id != governance.Id) ||
                    sell.OwnerFamilyId != seller.Id ||
                    sell.ProductDefinitionId != trade.ProductDefinitionId ||
                    trade.Quantity <= 0 || trade.UnitPrice <= 0 ||
                    trade.UnitPrice != sell.UnitPrice ||
                    trade.MoneyTransferred != checked(
                        trade.Quantity * trade.UnitPrice) ||
                    command.CommandTypeId != (supplemental
                        ? PublicReliefProcurementContractIds
                            .ArrivalRecoveryCommandTypeId
                        : external
                            ? PublicReliefProcurementContractIds
                                .ExternalProcurementCommandTypeId
                            : PublicReliefProcurementContractIds
                                .CommandTypeId) ||
                    command.Status != PersistentWorldCommandStatus.Completed ||
                    sourceEvent.EventTypeId != (external
                        ? PublicReliefProcurementContractIds
                            .ExternalSourcingRequiredEventTypeId
                        : PublicReliefProcurementContractIds
                            .ShortfallEventTypeId) ||
                    (!supplemental &&
                        sourceEvent.Day != checked(trade.Day - 1) ||
                     supplemental && sourceEvent.Day >= trade.Day) ||
                    sourceEvent.SourceTransactionId != (external
                        ? string.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            "public_relief.procurement_transaction.{0:D10}.{1}",
                            sourceEvent.Day,
                            governance.Id)
                        : string.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            "formal_public_food.monthly_transaction.{0:D10}.{1}",
                            sourceEvent.Day,
                            governance.Id)) ||
                    transaction.Type != (external
                        ? InventoryTransactionType.CivilianFreightDispatched
                        : InventoryTransactionType
                            .FoodPublicReliefProcurementTransferred) ||
                    transaction.SourceFormalMarketOrderId != sell.Id ||
                    transaction.SourceCountyGovernanceId !=
                        sourceGovernance.Id ||
                    external && transaction.SourceCivilianFreightId !=
                        trade.CivilianFreightId ||
                    !external &&
                        !string.IsNullOrEmpty(
                            transaction.SourceCivilianFreightId) ||
                    trade.FreightFee < 0 ||
                    !external && trade.FreightFee != 0 ||
                    supplemental != hasRecoveryLink ||
                    !supplemental && !string.IsNullOrEmpty(
                        trade.PublicReliefRecoveryId) ||
                    transaction.Day != trade.Day)
                {
                    throw new InvalidOperationException(
                        $"Invalid public relief procurement trade {trade.Id}.");
                }

                long sourceQuantity = 0;
                long destinationQuantity = 0;
                for (var lineIndex = 0;
                     lineIndex < transaction.Lines.Count;
                     lineIndex++)
                {
                    var line = transaction.Lines[lineIndex];
                    if (line.QuantityDelta < 0 &&
                        line.OwnerFamilyId == seller.Id)
                    {
                        sourceQuantity = checked(
                            sourceQuantity - line.QuantityDelta);
                    }
                    else if (line.QuantityDelta > 0 &&
                             line.OwnerOrganizationId == government.Id &&
                             (external ||
                              line.InventoryContainerId == destination.Id))
                    {
                        destinationQuantity = checked(
                            destinationQuantity + line.QuantityDelta);
                    }
                }
                if (sourceQuantity != trade.Quantity ||
                    destinationQuantity != trade.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Public relief procurement trade {trade.Id} does not match its inventory delivery.");
                }

                AddDelta(
                    tradedQuantityByOrder, sell.Id, trade.Quantity);
                AddDelta(
                    settledMoneyByOrder, sell.Id, trade.MoneyTransferred);
                var marketKey = FormalMarketKey(
                    trade.SourceCountyGovernanceId,
                    trade.ProductDefinitionId);
                AddDelta(
                    tradedQuantityByMarket, marketKey, trade.Quantity);
                AddDelta(
                    turnoverByMarket, marketKey, trade.MoneyTransferred);
                var transactionSequence =
                    transactionSequenceById[trade.InventoryTransactionId];
                if (!lastTradeTransactionSequenceByMarket.TryGetValue(
                        marketKey, out var lastTradeSequence) ||
                    transactionSequence > lastTradeSequence)
                {
                    lastTradeDayByMarket[marketKey] = trade.Day;
                    lastTradePriceByMarket[marketKey] = trade.UnitPrice;
                    lastTradeTransactionSequenceByMarket[marketKey] =
                        transactionSequence;
                }
            }

            foreach (var pair in orders)
            {
                tradedQuantityByOrder.TryGetValue(pair.Key, out var quantity);
                settledMoneyByOrder.TryGetValue(pair.Key, out var money);
                if (quantity != pair.Value.FilledQuantity ||
                    money != pair.Value.SettledMoney)
                {
                    throw new InvalidOperationException(
                        $"Formal market settlement total mismatch on {pair.Key}.");
                }
            }

            foreach (var pair in reservedQuantityByBatch)
            {
                if (!batches.TryGetValue(pair.Key, out var batch) ||
                    pair.Value > batch.ReservedQuantity)
                {
                    throw new InvalidOperationException(
                        $"Formal market reservations exceed batch {pair.Key}.");
                }
            }

            var marketKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < FormalMarketPrices.Count; i++)
            {
                var price = FormalMarketPrices[i] ??
                    throw new InvalidOperationException(
                        "A formal market price cannot be null.");
                ValidateContentReference(
                    price.ProductDefinitionId,
                    "formal market price product",
                    price.Id);
                var marketKey = FormalMarketKey(
                    price.CountyGovernanceId,
                    price.ProductDefinitionId);
                tradedQuantityByMarket.TryGetValue(
                    marketKey, out var tradedQuantity);
                turnoverByMarket.TryGetValue(marketKey, out var turnover);
                lastTradeDayByMarket.TryGetValue(
                    marketKey, out var lastTradeDay);
                lastTradePriceByMarket.TryGetValue(
                    marketKey, out var lastTradePrice);
                var hasLastTrade =
                    lastTradeTransactionSequenceByMarket.ContainsKey(
                        marketKey);
                if (!governances.ContainsKey(price.CountyGovernanceId) ||
                    !marketKeys.Add(marketKey) ||
                    price.EquilibriumUnitPrice <= 0 ||
                    price.LastTradeUnitPrice <= 0 ||
                    price.LastTradeDay < -1 ||
                    price.LastTradeDay > AbsoluteDay ||
                    price.CumulativeTradedQuantity != tradedQuantity ||
                    price.CumulativeTurnover != turnover ||
                    !hasLastTrade && price.LastTradeDay != -1 ||
                    hasLastTrade &&
                    (price.LastTradeDay != lastTradeDay ||
                     price.LastTradeUnitPrice != lastTradePrice))
                {
                    throw new InvalidOperationException(
                        $"Invalid formal market price {price.Id}.");
                }
            }

            foreach (var pair in tradedQuantityByMarket)
            {
                if (pair.Value > 0 && !marketKeys.Contains(pair.Key))
                {
                    throw new InvalidOperationException(
                        $"Formal market {pair.Key} lacks a price record.");
                }
            }
        }

        private void ValidateCivilianFreight(
            HashSet<string> personIds,
            HashSet<string> locationIds)
        {
            var families = new Dictionary<string, FamilyState>(
                StringComparer.Ordinal);
            var governances = new Dictionary<string, CountyGovernanceState>(
                StringComparer.Ordinal);
            var facilities = new Dictionary<string, VillageFacilityState>(
                StringComparer.Ordinal);
            var containers = new Dictionary<string, InventoryContainerState>(
                StringComparer.Ordinal);
            var routes = new Dictionary<string, RouteState>(
                StringComparer.Ordinal);
            var journeys = new Dictionary<string, JourneyState>(
                StringComparer.Ordinal);
            var transactions =
                new Dictionary<string, InventoryTransactionState>(
                    StringComparer.Ordinal);
            var orders = new Dictionary<string, FormalMarketOrderState>(
                StringComparer.Ordinal);
            var trades = new Dictionary<string, FormalMarketTradeState>(
                StringComparer.Ordinal);
            var publicTrades =
                new Dictionary<string, PublicReliefProcurementTradeState>(
                    StringComparer.Ordinal);
            var organizations = new Dictionary<string, OrganizationState>(
                StringComparer.Ordinal);
            var freights = new Dictionary<string, CivilianFreightState>(
                StringComparer.Ordinal);
            var dispatchedByFreight = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var dispatchedMoneyByFreight = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var lostByFreight = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var deliveredByFreight = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var feePaidByFreight = new Dictionary<string, long>(
                StringComparer.Ordinal);

            for (var i = 0; i < Families.Count; i++)
            {
                families.Add(Families[i].Id, Families[i]);
            }
            for (var i = 0; i < CountyGovernances.Count; i++)
            {
                governances.Add(CountyGovernances[i].Id, CountyGovernances[i]);
            }
            for (var i = 0; i < VillageFacilities.Count; i++)
            {
                facilities.Add(VillageFacilities[i].Id, VillageFacilities[i]);
            }
            for (var i = 0; i < InventoryContainers.Count; i++)
            {
                containers.Add(InventoryContainers[i].Id, InventoryContainers[i]);
            }
            for (var i = 0; i < Routes.Count; i++)
            {
                routes.Add(Routes[i].Id, Routes[i]);
            }
            for (var i = 0; i < Journeys.Count; i++)
            {
                journeys.Add(Journeys[i].Id, Journeys[i]);
            }
            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                transactions.Add(
                    InventoryTransactions[i].Id,
                    InventoryTransactions[i]);
            }
            for (var i = 0; i < FormalMarketOrders.Count; i++)
            {
                orders.Add(FormalMarketOrders[i].Id, FormalMarketOrders[i]);
            }
            for (var i = 0; i < FormalMarketTrades.Count; i++)
            {
                trades.Add(FormalMarketTrades[i].Id, FormalMarketTrades[i]);
            }
            for (var i = 0; i < PublicReliefProcurementTrades.Count; i++)
            {
                publicTrades.Add(
                    PublicReliefProcurementTrades[i].Id,
                    PublicReliefProcurementTrades[i]);
            }
            for (var i = 0; i < Organizations.Count; i++)
            {
                organizations.Add(Organizations[i].Id, Organizations[i]);
            }
            for (var i = 0; i < CivilianFreights.Count; i++)
            {
                freights.Add(CivilianFreights[i].Id, CivilianFreights[i]);
            }

            for (var i = 0; i < CivilianFreightLedgerEntries.Count; i++)
            {
                var entry = CivilianFreightLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A civilian freight ledger entry cannot be null.");
                InventoryTransactionState transaction = null;
                var hasTransaction = !string.IsNullOrEmpty(
                        entry.InventoryTransactionId) &&
                    transactions.TryGetValue(
                        entry.InventoryTransactionId, out transaction);
                var inventoryType = entry.Type ==
                        CivilianFreightLedgerType.Dispatched ||
                    entry.Type == CivilianFreightLedgerType.NaturalLoss ||
                    entry.Type == CivilianFreightLedgerType.Delivered;
                long transactionSourceQuantity = 0;
                if (hasTransaction)
                {
                    for (var lineIndex = 0;
                         lineIndex < transaction.Lines.Count;
                         lineIndex++)
                    {
                        if (transaction.Lines[lineIndex].QuantityDelta < 0)
                        {
                            transactionSourceQuantity = checked(
                                transactionSourceQuantity -
                                transaction.Lines[lineIndex].QuantityDelta);
                        }
                    }
                }
                if (!freights.ContainsKey(entry.CivilianFreightId) ||
                    !Enum.IsDefined(
                        typeof(CivilianFreightLedgerType), entry.Type) ||
                    entry.Day < 0 || entry.Day > AbsoluteDay ||
                    !personIds.Contains(entry.ActorPersonId) ||
                    entry.Quantity < 0 || entry.Money < 0 ||
                    inventoryType != hasTransaction ||
                    inventoryType &&
                        transactionSourceQuantity != entry.Quantity ||
                    hasTransaction &&
                        transaction.SourceCivilianFreightId !=
                            entry.CivilianFreightId ||
                    entry.Type == CivilianFreightLedgerType.Dispatched &&
                        (entry.Quantity <= 0 || entry.Money <= 0 ||
                         transaction.Type != InventoryTransactionType
                             .CivilianFreightDispatched) ||
                    entry.Type == CivilianFreightLedgerType.NaturalLoss &&
                        (entry.Quantity <= 0 || entry.Money != 0 ||
                         transaction.Type != InventoryTransactionType
                             .CivilianFreightNaturalLoss) ||
                    entry.Type == CivilianFreightLedgerType.Delivered &&
                        (entry.Quantity <= 0 || entry.Money != 0 ||
                         transaction.Type != InventoryTransactionType
                             .CivilianFreightDelivered) ||
                    entry.Type == CivilianFreightLedgerType.FreightFeePaid &&
                        (entry.Quantity != 0 || entry.Money < 0 ||
                         !string.IsNullOrEmpty(entry.InventoryTransactionId)))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian freight ledger entry {entry.Id}.");
                }

                switch (entry.Type)
                {
                    case CivilianFreightLedgerType.Dispatched:
                        AddDelta(
                            dispatchedByFreight,
                            entry.CivilianFreightId,
                            entry.Quantity);
                        AddDelta(
                            dispatchedMoneyByFreight,
                            entry.CivilianFreightId,
                            entry.Money);
                        break;
                    case CivilianFreightLedgerType.NaturalLoss:
                        AddDelta(
                            lostByFreight,
                            entry.CivilianFreightId,
                            entry.Quantity);
                        break;
                    case CivilianFreightLedgerType.Delivered:
                        AddDelta(
                            deliveredByFreight,
                            entry.CivilianFreightId,
                            entry.Quantity);
                        break;
                    case CivilianFreightLedgerType.FreightFeePaid:
                        AddDelta(
                            feePaidByFreight,
                            entry.CivilianFreightId,
                            entry.Money);
                        break;
                }
            }

            for (var i = 0; i < CivilianFreights.Count; i++)
            {
                var freight = CivilianFreights[i] ??
                    throw new InvalidOperationException(
                        "A civilian freight cannot be null.");
                var hasBuy = orders.TryGetValue(freight.BuyOrderId, out var buy);
                var hasSell = orders.TryGetValue(
                    freight.SellOrderId, out var sell);
                var hasTrade = trades.TryGetValue(
                    freight.FormalMarketTradeId, out var trade);
                var hasPublicTrade = publicTrades.TryGetValue(
                    freight.PublicReliefProcurementTradeId ?? string.Empty,
                    out var publicTrade);
                var publicRelief = hasPublicTrade;
                var hasOrigin = governances.TryGetValue(
                    freight.OriginCountyGovernanceId, out var origin);
                var hasDestination = governances.TryGetValue(
                    freight.DestinationCountyGovernanceId,
                    out var destination);
                var hasCarrierFamily = families.TryGetValue(
                    freight.CarrierFamilyId, out var carrierFamily);
                var hasBuyerFamily = families.TryGetValue(
                    freight.BuyerFamilyId, out var buyerFamily);
                var hasBuyerOrganization = organizations.TryGetValue(
                    freight.BuyerOrganizationId ?? string.Empty,
                    out var buyerOrganization);
                var hasSellerFamily = families.TryGetValue(
                    freight.SellerFamilyId, out var sellerFamily);
                var hasBuyerStorage = facilities.TryGetValue(
                    freight.BuyerStorageFacilityId, out var buyerStorage);
                var hasSellerStorage = facilities.TryGetValue(
                    freight.SellerStorageFacilityId, out var sellerStorage);
                var hasContainer = containers.TryGetValue(
                    freight.TransportInventoryContainerId, out var container);
                var hasRoute = routes.TryGetValue(freight.RouteId, out var route);
                var hasDispatch = transactions.TryGetValue(
                    freight.DispatchInventoryTransactionId,
                    out var dispatch);
                var hasJourney = journeys.TryGetValue(
                    freight.JourneyId, out var journey);
                var carrier = FindPerson(People, freight.CarrierPersonId);
                var validRoutePlan = TryBuildCivilianRoutePlan(
                    routes,
                    freight.PlannedRouteIds,
                    freight.OriginLocationId,
                    freight.DestinationLocationId,
                    out var legOrigins,
                    out var legDestinations,
                    out _,
                    out _);
                var validCurrentRoute = validRoutePlan &&
                    freight.CurrentRouteIndex >= 0 &&
                    freight.CurrentRouteIndex < freight.PlannedRouteIds.Count &&
                    freight.RouteId ==
                        freight.PlannedRouteIds[freight.CurrentRouteIndex];
                var currentLegOrigin = validCurrentRoute
                    ? legOrigins[freight.CurrentRouteIndex]
                    : string.Empty;
                var currentLegDestination = validCurrentRoute
                    ? legDestinations[freight.CurrentRouteIndex]
                    : string.Empty;
                dispatchedByFreight.TryGetValue(
                    freight.Id, out var dispatchedQuantity);
                dispatchedMoneyByFreight.TryGetValue(
                    freight.Id, out var dispatchedMoney);
                lostByFreight.TryGetValue(freight.Id, out var lostQuantity);
                deliveredByFreight.TryGetValue(
                    freight.Id, out var deliveredQuantity);
                feePaidByFreight.TryGetValue(freight.Id, out var feePaid);
                long currentCargo = 0;
                for (var batchIndex = 0;
                     batchIndex < ProductBatches.Count;
                     batchIndex++)
                {
                    var batch = ProductBatches[batchIndex];
                    if (batch.SourceTransactionId ==
                            freight.DispatchInventoryTransactionId &&
                        (publicRelief
                            ? batch.OwnerOrganizationId ==
                                freight.BuyerOrganizationId &&
                              string.IsNullOrEmpty(batch.OwnerFamilyId)
                            : batch.OwnerFamilyId == freight.BuyerFamilyId &&
                              string.IsNullOrEmpty(
                                  batch.OwnerOrganizationId)) &&
                        batch.InventoryContainerId ==
                            freight.TransportInventoryContainerId)
                    {
                        currentCargo = checked(
                            currentCargo + batch.Quantity);
                    }
                }

                var validFamilyBuyer = !publicRelief && hasBuy && hasTrade &&
                    hasBuyerFamily && hasBuyerStorage &&
                    string.IsNullOrEmpty(freight.BuyerOrganizationId) &&
                    string.IsNullOrEmpty(
                        freight.DestinationInventoryContainerId) &&
                    string.IsNullOrEmpty(
                        freight.SourcePublicReliefEventId) &&
                    string.IsNullOrEmpty(
                        freight.SourcePublicReliefCommandId) &&
                    string.IsNullOrEmpty(
                        freight.PublicReliefRecoveryId) &&
                    !freight.IsSupplementalPublicReliefFreight &&
                    FamilyBelongsToCounty(
                        freight.BuyerFamilyId,
                        destination.CountyLocationId) &&
                    buyerFamily.LocationId == freight.DestinationLocationId &&
                    buyerStorage.Kind ==
                        VillageFacilityKind.HouseholdGranary &&
                    buyerStorage.OwnerFamilyId == freight.BuyerFamilyId &&
                    buy.OwnerFamilyId == freight.BuyerFamilyId &&
                    buy.StorageFacilityId ==
                        freight.BuyerStorageFacilityId &&
                    trade.CivilianFreightId == freight.Id &&
                    trade.BuyOrderId == buy.Id &&
                    trade.SellOrderId == sell.Id &&
                    trade.BuyerFamilyId == freight.BuyerFamilyId &&
                    trade.SellerFamilyId == freight.SellerFamilyId &&
                    trade.CountyGovernanceId ==
                        freight.OriginCountyGovernanceId &&
                    trade.DestinationCountyGovernanceId ==
                        freight.DestinationCountyGovernanceId &&
                    trade.InventoryTransactionId ==
                        freight.DispatchInventoryTransactionId &&
                    trade.Quantity == freight.DispatchedQuantity &&
                    trade.UnitPrice == freight.GoodsUnitPrice &&
                    trade.SellerProceeds ==
                        freight.GoodsMoneyTransferred &&
                    freight.ProductDefinitionId == buy.ProductDefinitionId;
                var validPublicBuyer = publicRelief && !hasBuy && !hasTrade &&
                    hasBuyerOrganization && !hasBuyerFamily &&
                    !hasBuyerStorage &&
                    buyerOrganization.Type == OrganizationType.Government &&
                    destination.GovernmentOrganizationId ==
                        buyerOrganization.Id &&
                    freight.DestinationInventoryContainerId ==
                        destination.GranaryInventoryContainerId &&
                    containers.TryGetValue(
                        freight.DestinationInventoryContainerId,
                        out var reliefDestination) &&
                    reliefDestination.OwnerOrganizationId ==
                        buyerOrganization.Id &&
                    string.IsNullOrEmpty(freight.BuyerFamilyId) &&
                    string.IsNullOrEmpty(freight.BuyerStorageFacilityId) &&
                    publicTrade.CivilianFreightId == freight.Id &&
                    publicTrade.SellOrderId == sell.Id &&
                    publicTrade.BuyerOrganizationId ==
                        freight.BuyerOrganizationId &&
                    publicTrade.SellerFamilyId == freight.SellerFamilyId &&
                    publicTrade.SourceCountyGovernanceId ==
                        freight.OriginCountyGovernanceId &&
                    publicTrade.CountyGovernanceId ==
                        freight.DestinationCountyGovernanceId &&
                    publicTrade.DestinationInventoryContainerId ==
                        freight.DestinationInventoryContainerId &&
                    publicTrade.SourceShortfallEventId ==
                        freight.SourcePublicReliefEventId &&
                    publicTrade.SourceCommandId ==
                        freight.SourcePublicReliefCommandId &&
                    publicTrade.InventoryTransactionId ==
                        freight.DispatchInventoryTransactionId &&
                    publicTrade.Quantity == freight.DispatchedQuantity &&
                    publicTrade.UnitPrice == freight.GoodsUnitPrice &&
                    publicTrade.MoneyTransferred ==
                        freight.GoodsMoneyTransferred &&
                    publicTrade.FreightFee == freight.FreightFee;

                validPublicBuyer = validPublicBuyer &&
                    publicTrade.PublicReliefRecoveryId ==
                        (freight.PublicReliefRecoveryId ?? string.Empty) &&
                    publicTrade.IsSupplementalPublicReliefProcurement ==
                        freight.IsSupplementalPublicReliefFreight &&
                    (freight.IsSupplementalPublicReliefFreight
                        ? !string.IsNullOrEmpty(
                            freight.PublicReliefRecoveryId)
                        : string.IsNullOrEmpty(
                            freight.PublicReliefRecoveryId));

                if (!Enum.IsDefined(
                        typeof(CivilianFreightStatus), freight.Status) ||
                    !hasSell || !hasOrigin ||
                    !hasDestination || !hasCarrierFamily ||
                    !hasSellerFamily || !hasSellerStorage || !hasContainer ||
                    !hasRoute || !hasDispatch || carrier == null ||
                    !validFamilyBuyer && !validPublicBuyer ||
                    !locationIds.Contains(freight.OriginLocationId) ||
                    !locationIds.Contains(freight.DestinationLocationId) ||
                    freight.OriginCountyGovernanceId ==
                        freight.DestinationCountyGovernanceId ||
                    origin.CountyLocationId == destination.CountyLocationId ||
                    !FamilyBelongsToCounty(
                        freight.SellerFamilyId, origin.CountyLocationId) ||
                    sellerFamily.LocationId != freight.OriginLocationId ||
                    sellerStorage.Kind !=
                        VillageFacilityKind.HouseholdGranary ||
                    sellerStorage.OwnerFamilyId != freight.SellerFamilyId ||
                    sell.OwnerFamilyId != freight.SellerFamilyId ||
                    sell.StorageFacilityId !=
                        freight.SellerStorageFacilityId ||
                    carrier.FamilyId != carrierFamily.Id ||
                    container.CarrierPersonId != carrier.Id ||
                    container.OwnerFamilyId != carrierFamily.Id ||
                    !validCurrentRoute || !hasRoute ||
                    dispatch.Type !=
                        InventoryTransactionType.CivilianFreightDispatched ||
                    dispatch.SourceCivilianFreightId != freight.Id ||
                    dispatch.SourceFormalMarketOrderId != sell.Id ||
                    freight.ProductDefinitionId != sell.ProductDefinitionId ||
                    freight.DispatchedQuantity <= 0 ||
                    freight.RemainingCargoQuantity < 0 ||
                    freight.DeliveredQuantity < 0 ||
                    freight.NaturalLossQuantity < 0 ||
                    freight.DispatchedQuantity != checked(
                        freight.RemainingCargoQuantity +
                        freight.DeliveredQuantity +
                        freight.NaturalLossQuantity) ||
                    currentCargo != freight.RemainingCargoQuantity ||
                    freight.GoodsUnitPrice <= 0 ||
                    freight.GoodsMoneyTransferred != checked(
                        freight.DispatchedQuantity *
                        freight.GoodsUnitPrice) ||
                    freight.FreightFee < 0 ||
                    freight.FreightFeeEscrow < 0 ||
                    freight.FreightFeePaid < 0 ||
                    freight.FreightFee != checked(
                        freight.FreightFeeEscrow +
                        freight.FreightFeePaid) ||
                    freight.ProductPerishabilityBasisPoints < 0 ||
                    freight.ProductPerishabilityBasisPoints > 10_000 ||
                    freight.FoodSpoilageSensitivityBasisPoints < 0 ||
                    freight.FoodSpoilageSensitivityBasisPoints > 10_000 ||
                    freight.CargoUnitWeight <= 0 ||
                    freight.CreatedDay < 0 ||
                    freight.DispatchedDay != freight.CreatedDay ||
                    freight.LastLossDay < freight.DispatchedDay ||
                    freight.LastLossDay > AbsoluteDay ||
                    dispatchedQuantity != freight.DispatchedQuantity ||
                    dispatchedMoney != freight.GoodsMoneyTransferred ||
                    lostQuantity != freight.NaturalLossQuantity ||
                    deliveredQuantity != freight.DeliveredQuantity ||
                    feePaid != freight.FreightFeePaid ||
                    freight.Status == CivilianFreightStatus.InTransit &&
                        (!hasJourney ||
                         journey.PersonId != freight.CarrierPersonId ||
                         journey.RouteId != freight.RouteId ||
                         journey.OriginLocationId != currentLegOrigin ||
                         journey.DestinationLocationId !=
                            currentLegDestination ||
                         freight.ArrivedDay != -1 ||
                         freight.CompletedDay != -1) ||
                    freight.Status ==
                        CivilianFreightStatus.AwaitingNextLeg &&
                        (hasJourney ||
                         freight.CurrentRouteIndex >=
                            freight.PlannedRouteIds.Count - 1 ||
                         carrier.LocationId != currentLegDestination ||
                         container.LocationId != currentLegDestination ||
                         freight.ArrivedDay != -1 ||
                         freight.CompletedDay != -1) ||
                    freight.Status == CivilianFreightStatus.AwaitingReceipt &&
                        (hasJourney ||
                         freight.CurrentRouteIndex !=
                            freight.PlannedRouteIds.Count - 1 ||
                         freight.ArrivedDay <
                            freight.DispatchedDay ||
                         freight.CompletedDay != -1 ||
                         carrier.LocationId !=
                            freight.DestinationLocationId ||
                         container.LocationId !=
                            freight.DestinationLocationId) ||
                    freight.Status == CivilianFreightStatus.Completed &&
                        (hasJourney ||
                         freight.CurrentRouteIndex !=
                            freight.PlannedRouteIds.Count - 1 ||
                         freight.RemainingCargoQuantity != 0 ||
                         freight.ArrivedDay < freight.DispatchedDay ||
                         freight.CompletedDay < freight.ArrivedDay ||
                         freight.FreightFeeEscrow != 0 ||
                         freight.FreightFeePaid != freight.FreightFee ||
                         carrier.LocationId !=
                            freight.DestinationLocationId ||
                         container.LocationId !=
                            freight.DestinationLocationId))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian freight {freight.Id}.");
                }
            }

            ValidateCivilianFreightPlanning(
                personIds,
                families,
                containers,
                routes,
                orders,
                freights);
        }

        private void ValidatePublicReliefRecovery()
        {
            var governances = new Dictionary<string, CountyGovernanceState>(
                StringComparer.Ordinal);
            var villages = new Dictionary<string, VillageState>(
                StringComparer.Ordinal);
            var freights = new Dictionary<string, CivilianFreightState>(
                StringComparer.Ordinal);
            var trades = new Dictionary<
                string, PublicReliefProcurementTradeState>(
                StringComparer.Ordinal);
            var events = new Dictionary<string, WorldEventOutboxState>(
                StringComparer.Ordinal);
            var transactions = new Dictionary<
                string, InventoryTransactionState>(
                StringComparer.Ordinal);
            for (var i = 0; i < CountyGovernances.Count; i++)
                governances.Add(CountyGovernances[i].Id, CountyGovernances[i]);
            for (var i = 0; i < Villages.Count; i++)
                villages.Add(Villages[i].Id, Villages[i]);
            for (var i = 0; i < CivilianFreights.Count; i++)
                freights.Add(CivilianFreights[i].Id, CivilianFreights[i]);
            for (var i = 0; i < PublicReliefProcurementTrades.Count; i++)
                trades.Add(
                    PublicReliefProcurementTrades[i].Id,
                    PublicReliefProcurementTrades[i]);
            for (var i = 0; i < WorldEventOutbox.Count; i++)
                events.Add(WorldEventOutbox[i].Id, WorldEventOutbox[i]);
            for (var i = 0; i < InventoryTransactions.Count; i++)
                transactions.Add(
                    InventoryTransactions[i].Id, InventoryTransactions[i]);

            var reportedFreights = new HashSet<string>(
                StringComparer.Ordinal);
            var reportIds = new HashSet<string>(StringComparer.Ordinal);
            var recoveryTransactionIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < PublicReliefRecoveries.Count; i++)
            {
                var recovery = PublicReliefRecoveries[i] ??
                    throw new InvalidOperationException(
                        "A public relief recovery cannot be null.");
                _ = new StableId(recovery.Id);
                if (!Enum.IsDefined(
                        typeof(PublicReliefRecoveryStatus), recovery.Status) ||
                    !governances.TryGetValue(
                        recovery.CountyGovernanceId, out var governance) ||
                    !events.TryGetValue(
                        recovery.SourceShortfallEventId,
                        out var shortfallEvent) ||
                    !events.TryGetValue(
                        recovery.SourceExternalSourcingEventId,
                        out var externalEvent) ||
                    shortfallEvent.EventTypeId !=
                        "mandate.event.formal_public_food.county_relief_shortfall_detected" ||
                    externalEvent.EventTypeId !=
                        PublicReliefProcurementContractIds
                            .ExternalSourcingRequiredEventTypeId ||
                    shortfallEvent.Day != recovery.SourceShortfallDay ||
                    externalEvent.Day != recovery.SourceShortfallDay + 1 ||
                    recovery.ExternalShortfallQuantity <= 0 ||
                    recovery.TotalDispatchedQuantity < 0 ||
                    recovery.TotalNaturalLossQuantity < 0 ||
                    recovery.TotalDeliveredQuantity < 0 ||
                    recovery.TotalRecoveredQuantity < 0 ||
                    recovery.RemainingQuantity < 0 ||
                    recovery.RemainingQuantity !=
                        recovery.ExternalShortfallQuantity -
                        recovery.TotalRecoveredQuantity ||
                    recovery.TotalRecoveredQuantity >
                        recovery.TotalDeliveredQuantity ||
                    recovery.LastRecoveryDay < recovery.SourceShortfallDay ||
                    recovery.LastRecoveryDay > AbsoluteDay ||
                    recovery.SupplementalAttemptCount < 0 ||
                    recovery.SupplementalAttemptCount > 1 ||
                    recovery.SupplementalAttemptCount == 0 &&
                        (recovery.SupplementalRequestedQuantity != 0 ||
                         !string.IsNullOrEmpty(
                            recovery.SupplementalFreightId)) ||
                    recovery.SupplementalAttemptCount == 1 &&
                        recovery.SupplementalRequestedQuantity <= 0 ||
                    recovery.Status == PublicReliefRecoveryStatus.Fulfilled &&
                        recovery.RemainingQuantity != 0 ||
                    recovery.Status != PublicReliefRecoveryStatus.Fulfilled &&
                        recovery.RemainingQuantity == 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid public relief recovery {recovery.Id}.");
                }

                long required = 0;
                long recovered = 0;
                var recoveryVillages = new HashSet<string>(
                    StringComparer.Ordinal);
                for (var villageIndex = 0;
                     villageIndex < recovery.VillageRecoveries.Count;
                     villageIndex++)
                {
                    var item = recovery.VillageRecoveries[villageIndex] ??
                        throw new InvalidOperationException(
                            "A village relief recovery cannot be null.");
                    if (!recoveryVillages.Add(item.VillageId) ||
                        !villages.TryGetValue(
                            item.VillageId, out var village) ||
                        village.ParentLocationId !=
                            governance.CountyLocationId ||
                        item.RequiredQuantity <= 0 ||
                        item.RecoveredQuantity < 0 ||
                        item.RecoveredQuantity > item.RequiredQuantity ||
                        item.RemainingQuantity !=
                            item.RequiredQuantity - item.RecoveredQuantity)
                    {
                        throw new InvalidOperationException(
                            $"Invalid village recovery in {recovery.Id}.");
                    }
                    required = checked(required + item.RequiredQuantity);
                    recovered = checked(
                        recovered + item.RecoveredQuantity);
                    for (var transactionIndex = 0;
                         transactionIndex <
                            item.InventoryTransactionIds.Count;
                         transactionIndex++)
                    {
                        var transactionId =
                            item.InventoryTransactionIds[transactionIndex];
                        if (!recoveryTransactionIds.Add(transactionId) ||
                            !transactions.TryGetValue(
                                transactionId, out var transaction) ||
                            transaction.Type != InventoryTransactionType
                                .FoodCountyReliefTransferred ||
                            transaction.SourceVillageId != item.VillageId ||
                            transaction.SourceCountyGovernanceId !=
                                recovery.CountyGovernanceId)
                        {
                            throw new InvalidOperationException(
                                $"Invalid recovery inventory transaction {transactionId}.");
                        }
                    }
                }

                long dispatched = 0;
                long lost = 0;
                long delivered = 0;
                long reportRecovered = 0;
                for (var reportIndex = 0;
                     reportIndex < recovery.FreightReports.Count;
                     reportIndex++)
                {
                    var report = recovery.FreightReports[reportIndex] ??
                        throw new InvalidOperationException(
                            "A public relief freight report cannot be null.");
                    if (!reportIds.Add(report.Id) ||
                        !reportedFreights.Add(report.CivilianFreightId) ||
                        !freights.TryGetValue(
                            report.CivilianFreightId, out var freight) ||
                        !trades.ContainsKey(
                            report.PublicReliefProcurementTradeId) ||
                        freight.Status != CivilianFreightStatus.Completed ||
                        freight.DestinationCountyGovernanceId !=
                            recovery.CountyGovernanceId ||
                        freight.PublicReliefProcurementTradeId !=
                            report.PublicReliefProcurementTradeId ||
                        report.IsSupplemental !=
                            freight.IsSupplementalPublicReliefFreight ||
                        report.IsSupplemental &&
                            freight.PublicReliefRecoveryId != recovery.Id ||
                        report.DispatchedQuantity !=
                            freight.DispatchedQuantity ||
                        report.NaturalLossQuantity !=
                            freight.NaturalLossQuantity ||
                        report.DeliveredQuantity !=
                            freight.DeliveredQuantity ||
                        report.DispatchedQuantity !=
                            report.NaturalLossQuantity +
                            report.DeliveredQuantity ||
                        report.RecoveryDistributedQuantity < 0 ||
                        report.RecoveryDistributedQuantity >
                            report.DeliveredQuantity ||
                        report.DispatchedDay != freight.DispatchedDay ||
                        report.ArrivedDay != freight.ArrivedDay ||
                        report.CompletedDay != freight.CompletedDay ||
                        report.ReconciledDay < report.CompletedDay ||
                        report.ReconciledDay > AbsoluteDay ||
                        report.TransitDays != Math.Max(
                            0,
                            report.CompletedDay - report.DispatchedDay) ||
                        report.ReceiptWaitingDays !=
                            (report.ArrivedDay < 0
                                ? 0
                                : Math.Max(
                                    0,
                                    report.CompletedDay - report.ArrivedDay)) ||
                        report.ExceptionCode == null)
                    {
                        throw new InvalidOperationException(
                            $"Invalid public relief freight report {report.Id}.");
                    }
                    dispatched = checked(
                        dispatched + report.DispatchedQuantity);
                    lost = checked(lost + report.NaturalLossQuantity);
                    delivered = checked(
                        delivered + report.DeliveredQuantity);
                    reportRecovered = checked(
                        reportRecovered +
                        report.RecoveryDistributedQuantity);
                }

                if (required != recovery.ExternalShortfallQuantity ||
                    recovered != recovery.TotalRecoveredQuantity ||
                    reportRecovered != recovery.TotalRecoveredQuantity ||
                    dispatched != recovery.TotalDispatchedQuantity ||
                    lost != recovery.TotalNaturalLossQuantity ||
                    delivered != recovery.TotalDeliveredQuantity)
                {
                    throw new InvalidOperationException(
                        $"Public relief recovery totals do not close for {recovery.Id}.");
                }

                if (!string.IsNullOrEmpty(recovery.SupplementalFreightId))
                {
                    if (!freights.TryGetValue(
                            recovery.SupplementalFreightId,
                            out var supplemental) ||
                        !supplemental.IsSupplementalPublicReliefFreight ||
                        supplemental.PublicReliefRecoveryId != recovery.Id ||
                        recovery.SupplementalAttemptCount != 1)
                    {
                        throw new InvalidOperationException(
                            $"Invalid supplemental freight link for {recovery.Id}.");
                    }
                }
                if (recovery.Status ==
                        PublicReliefRecoveryStatus.SupplementalInTransit &&
                    string.IsNullOrEmpty(recovery.SupplementalFreightId))
                {
                    throw new InvalidOperationException(
                        $"Recovery {recovery.Id} lacks its supplemental freight.");
                }
            }
        }

        private static string FormalMarketKey(
            string governanceId,
            string productDefinitionId)
        {
            return governanceId + "@" + productDefinitionId;
        }

        private void ValidateResourceExtraction(
            HashSet<string> personIds,
            HashSet<string> locationIds,
            HashSet<string> organizationIds,
            IDictionary<string, ProductionSiteState> productionSites,
            IDictionary<string, InventoryContainerState> containers,
            IDictionary<string, ProductBatchState> batches,
            HashSet<string> transactionIds)
        {
            var resources = new Dictionary<string, ResourceBodyState>(
                StringComparer.Ordinal);
            var orders = new Dictionary<string, ResourceExtractionOrderState>(
                StringComparer.Ordinal);
            var remainingDeltas = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var reservedDeltas = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var reserveEntries = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var settleEntries = new Dictionary<string, int>(
                StringComparer.Ordinal);

            for (var i = 0; i < ResourceBodies.Count; i++)
            {
                var resource = ResourceBodies[i] ??
                    throw new InvalidOperationException(
                        "A resource body cannot be null.");
                resources.Add(resource.Id, resource);
                ValidateContentReference(
                    resource.ResourceKindId, "resource kind", resource.Id);
                ValidateContentReference(
                    resource.OutputProductDefinitionId,
                    "resource output product",
                    resource.Id);
                ValidateContentReference(
                    resource.RequiredFacilityTag,
                    "resource facility tag",
                    resource.Id);
                if (!locationIds.Contains(resource.LocationId) ||
                    string.IsNullOrWhiteSpace(resource.Provenance) ||
                    string.IsNullOrWhiteSpace(resource.GenerationRuleVersion) ||
                    resource.InitialQuantity <= 0 ||
                    resource.RemainingQuantity < 0 ||
                    resource.RemainingQuantity > resource.InitialQuantity ||
                    resource.ReservedQuantity < 0 ||
                    resource.ReservedQuantity > resource.RemainingQuantity ||
                    resource.QualityBasisPoints <= 0 ||
                    resource.QualityBasisPoints > 10_000 ||
                    resource.ExtractionDifficultyBasisPoints <= 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid resource body {resource.Id}.");
                }
            }

            for (var i = 0; i < ResourceExtractionOrders.Count; i++)
            {
                var order = ResourceExtractionOrders[i] ??
                    throw new InvalidOperationException(
                        "A resource extraction order cannot be null.");
                orders.Add(order.Id, order);
                var validResource = resources.TryGetValue(
                    order.ResourceBodyId, out var resource);
                var validSite = productionSites.TryGetValue(
                    order.ProductionSiteId, out var site);
                var validContainer = containers.TryGetValue(
                    order.InventoryContainerId, out var container);
                if (!validResource || !validSite || !validContainer ||
                    !organizationIds.Contains(order.OwnerOrganizationId) ||
                    site.OwnerOrganizationId != order.OwnerOrganizationId ||
                    site.InventoryContainerId != container.Id ||
                    container.OwnerOrganizationId != order.OwnerOrganizationId ||
                    site.LocationId != resource.LocationId ||
                    !site.FacilityTags.Contains(resource.RequiredFacilityTag) ||
                    site.ManagerPersonId != order.ManagerPersonId ||
                    !personIds.Contains(order.ManagerPersonId) ||
                    !HasOrganizationMembership(
                        order.ManagerPersonId, order.OwnerOrganizationId) ||
                    !Enum.IsDefined(
                        typeof(ProductionControlMode), order.ControlMode) ||
                    (order.Status != ProductionOrderStatus.Active &&
                     order.Status != ProductionOrderStatus.Completed) ||
                    order.CreatedDay < 0 || order.FinishDay <= order.CreatedDay ||
                    order.SettledDay < -1 || order.SettledDay > AbsoluteDay ||
                    order.RequestedQuantity <= 0 ||
                    order.ExtractedQuantity < 0 ||
                    order.ExtractedQuantity > order.RequestedQuantity ||
                    order.WorkerPersonIds == null ||
                    order.WorkerPersonIds.Count == 0 ||
                    order.Status == ProductionOrderStatus.Active &&
                    (order.SettledDay != -1 ||
                     order.ExtractedQuantity != 0 ||
                     !string.IsNullOrEmpty(order.OutputBatchId)) ||
                    order.Status == ProductionOrderStatus.Completed &&
                    (order.SettledDay < order.FinishDay ||
                     order.ExtractedQuantity != order.RequestedQuantity ||
                     string.IsNullOrEmpty(order.OutputBatchId)))
                {
                    throw new InvalidOperationException(
                        $"Invalid resource extraction order {order.Id}.");
                }

                var workers = new HashSet<string>(StringComparer.Ordinal);
                string previous = null;
                for (var workerIndex = 0;
                     workerIndex < order.WorkerPersonIds.Count;
                     workerIndex++)
                {
                    var workerId = order.WorkerPersonIds[workerIndex];
                    if (!personIds.Contains(workerId) ||
                        !HasOrganizationMembership(
                            workerId, order.OwnerOrganizationId) ||
                        !workers.Add(workerId) ||
                        previous != null &&
                        string.CompareOrdinal(previous, workerId) >= 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid worker assignment on {order.Id}.");
                    }

                    previous = workerId;
                }

                if (order.Status == ProductionOrderStatus.Completed)
                {
                    if (!batches.TryGetValue(
                            order.OutputBatchId, out var output) ||
                        output.SourceWorkOrderId != order.Id ||
                        output.ProductDefinitionId !=
                            resource.OutputProductDefinitionId ||
                        output.OwnerOrganizationId != order.OwnerOrganizationId ||
                        output.InventoryContainerId != order.InventoryContainerId ||
                        !transactionIds.Contains(output.SourceTransactionId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid extraction output on {order.Id}.");
                    }
                }

                var extractionTransactions = 0;
                for (var transactionIndex = 0;
                     transactionIndex < InventoryTransactions.Count;
                     transactionIndex++)
                {
                    var transaction = InventoryTransactions[transactionIndex];
                    if (transaction.SourceResourceExtractionOrderId == order.Id &&
                        transaction.Type ==
                            InventoryTransactionType.ResourceExtractionSettled)
                    {
                        if (transaction.Lines.Count != 1 ||
                            transaction.Lines[0].BatchId != order.OutputBatchId ||
                            transaction.Lines[0].QuantityDelta !=
                                order.ExtractedQuantity ||
                            transaction.Lines[0].ReservedQuantityDelta != 0)
                        {
                            throw new InvalidOperationException(
                                $"Invalid extraction transaction for {order.Id}.");
                        }

                        extractionTransactions++;
                    }
                }

                if (order.Status == ProductionOrderStatus.Active &&
                        extractionTransactions != 0 ||
                    order.Status == ProductionOrderStatus.Completed &&
                        extractionTransactions != 1)
                {
                    throw new InvalidOperationException(
                        $"Invalid extraction transaction count for {order.Id}.");
                }
            }

            for (var i = 0; i < ResourceExtractionLedgerEntries.Count; i++)
            {
                var entry = ResourceExtractionLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A resource extraction ledger entry cannot be null.");
                if (!resources.ContainsKey(entry.ResourceBodyId) ||
                    !orders.TryGetValue(
                        entry.ResourceExtractionOrderId, out var order) ||
                    order.ResourceBodyId != entry.ResourceBodyId ||
                    !personIds.Contains(entry.ActorPersonId) ||
                    entry.ActorPersonId != order.ManagerPersonId ||
                    entry.Day < 0 || entry.Day > AbsoluteDay ||
                    !Enum.IsDefined(
                        typeof(ResourceExtractionLedgerEntryType), entry.Type))
                {
                    throw new InvalidOperationException(
                        $"Invalid resource extraction ledger entry {entry.Id}.");
                }

                AddDelta(
                    remainingDeltas,
                    entry.ResourceBodyId,
                    entry.RemainingQuantityDelta);
                AddDelta(
                    reservedDeltas,
                    entry.ResourceBodyId,
                    entry.ReservedQuantityDelta);
                if (entry.Type == ResourceExtractionLedgerEntryType.Reserved)
                {
                    if (entry.RemainingQuantityDelta != 0 ||
                        entry.ReservedQuantityDelta != order.RequestedQuantity ||
                        !string.IsNullOrEmpty(entry.OutputBatchId) ||
                        entry.OutputQuantity != 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid extraction reservation entry {entry.Id}.");
                    }

                    reserveEntries.TryGetValue(order.Id, out var count);
                    reserveEntries[order.Id] = count + 1;
                }
                else
                {
                    if (entry.RemainingQuantityDelta !=
                            -order.RequestedQuantity ||
                        entry.ReservedQuantityDelta !=
                            -order.RequestedQuantity ||
                        entry.OutputBatchId != order.OutputBatchId ||
                        entry.OutputQuantity != order.ExtractedQuantity ||
                        !batches.ContainsKey(entry.OutputBatchId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid extraction settlement entry {entry.Id}.");
                    }

                    settleEntries.TryGetValue(order.Id, out var count);
                    settleEntries[order.Id] = count + 1;
                }
            }

            foreach (var pair in orders)
            {
                reserveEntries.TryGetValue(pair.Key, out var reservations);
                settleEntries.TryGetValue(pair.Key, out var settlements);
                if (reservations != 1 ||
                    pair.Value.Status == ProductionOrderStatus.Active &&
                        settlements != 0 ||
                    pair.Value.Status == ProductionOrderStatus.Completed &&
                        settlements != 1)
                {
                    throw new InvalidOperationException(
                        $"Incomplete resource provenance for {pair.Key}.");
                }
            }

            foreach (var pair in resources)
            {
                remainingDeltas.TryGetValue(pair.Key, out var remaining);
                reservedDeltas.TryGetValue(pair.Key, out var reserved);
                if (pair.Value.RemainingQuantity !=
                        pair.Value.InitialQuantity + remaining ||
                    pair.Value.ReservedQuantity != reserved)
                {
                    throw new InvalidOperationException(
                        $"Resource ledger mismatch for {pair.Key}.");
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

        private static bool HasCarrierContainer(
            IDictionary<string, InventoryContainerState> containers,
            string carrierPersonId,
            string organizationId)
        {
            foreach (var pair in containers)
            {
                if (pair.Value.CarrierPersonId == carrierPersonId &&
                    pair.Value.OwnerOrganizationId == organizationId)
                {
                    return true;
                }
            }

            return false;
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
            if (manifest == null || manifest.ContentSchemaVersion != 3 ||
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

        private bool HasOrganizationMembership(
            string personId,
            string organizationId)
        {
            for (var i = 0; i < Memberships.Count; i++)
            {
                if (Memberships[i].PersonId == personId &&
                    Memberships[i].OrganizationId == organizationId)
                {
                    return true;
                }
            }

            return false;
        }

        private static MilitaryEquipmentDefinitionState FindEquipmentDefinition(
            IList<MilitaryEquipmentDefinitionState> definitions,
            string definitionId)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].Id == definitionId)
                {
                    return definitions[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing equipment definition {definitionId}.");
        }

        private static MilitaryArmoryStockState FindArmoryStock(
            IList<MilitaryArmoryStockState> stocks,
            string armyId,
            string definitionId)
        {
            for (var i = 0; i < stocks.Count; i++)
            {
                if (stocks[i].ArmyId == armyId &&
                    stocks[i].EquipmentDefinitionId == definitionId)
                {
                    return stocks[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing armory stock {armyId}/{definitionId}.");
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

        private static VillageState FindVillageById(
            IList<VillageState> villages,
            string villageId)
        {
            for (var i = 0; i < villages.Count; i++)
            {
                if (villages[i].Id == villageId)
                {
                    return villages[i];
                }
            }

            throw new InvalidOperationException($"Missing village {villageId}.");
        }

        private static CountyGovernanceState FindCountyGovernanceById(
            IList<CountyGovernanceState> governances,
            string governanceId)
        {
            for (var i = 0; i < governances.Count; i++)
            {
                if (governances[i].Id == governanceId)
                {
                    return governances[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing county governance {governanceId}.");
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

        private static void AddLong(
            IDictionary<string, long> values,
            string key,
            long delta)
        {
            values.TryGetValue(key, out var current);
            values[key] = checked(current + delta);
        }

        private sealed class LogisticsLedgerBalance
        {
            public int DispatchCount;
            public int DeliveryCount;
            public int CargoDispatched;
            public int CargoRemaining;
            public int CargoDelivered;
            public int NaturalLoss;
            public int HostileLoss;
            public int RecoveredCargo;
            public int CargoConsumed;
            public int ProvisionsLoaded;
            public int ProvisionsRemaining;
            public int ProvisionsConsumed;
            public long BuyerMoney;
            public long SourceMoney;
            public int PublicOrder;

            public void Apply(MilitaryLogisticsLedgerEntryState entry)
            {
                if (entry.Type == MilitaryLogisticsLedgerType.Dispatch)
                {
                    DispatchCount++;
                }

                if (entry.Type == MilitaryLogisticsLedgerType.Delivery)
                {
                    DeliveryCount++;
                }

                CargoDispatched = checked(
                    CargoDispatched + entry.CargoDispatchedDelta);
                CargoRemaining = checked(
                    CargoRemaining + entry.CargoRemainingDelta);
                CargoDelivered = checked(
                    CargoDelivered + entry.CargoDeliveredDelta);
                NaturalLoss = checked(
                    NaturalLoss + entry.CargoNaturalLossDelta);
                HostileLoss = checked(
                    HostileLoss + entry.CargoHostileLossDelta);
                RecoveredCargo = checked(
                    RecoveredCargo + entry.CargoRecoveredDelta);
                CargoConsumed = checked(
                    CargoConsumed + entry.CargoConsumedAsProvisionsDelta);
                ProvisionsLoaded = checked(
                    ProvisionsLoaded + entry.ConvoyProvisionsLoadedDelta);
                ProvisionsRemaining = checked(
                    ProvisionsRemaining +
                    entry.ConvoyProvisionsRemainingDelta);
                ProvisionsConsumed = checked(
                    ProvisionsConsumed + entry.ConvoyProvisionsConsumedDelta);
                BuyerMoney = checked(BuyerMoney + entry.BuyerMoneyDelta);
                SourceMoney = checked(SourceMoney + entry.SourceMoneyDelta);
                PublicOrder = checked(
                    PublicOrder + entry.OriginPublicOrderDelta);
            }
        }
    }
}
