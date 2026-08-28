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
        public int PermanentLaborCapacityPenaltyBasisPoints;
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
        public const int CurrentSchemaVersion = 75;

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
        public List<WorldDecisionAgentState> WorldDecisionAgents =
            new List<WorldDecisionAgentState>();
        public List<WorldSimulationLodState> WorldSimulationLodStates =
            new List<WorldSimulationLodState>();
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
        public List<CanonicalPlaceCrosswalkState> CanonicalPlaceCrosswalks =
            new List<CanonicalPlaceCrosswalkState>();
        public List<HistoricalIdentityState> HistoricalIdentities =
            new List<HistoricalIdentityState>();
        public List<PersonLineageState> PersonLineages =
            new List<PersonLineageState>();
        public List<FamilyOrganizationProfileState> FamilyOrganizationProfiles =
            new List<FamilyOrganizationProfileState>();
        public List<FamilyOrganizationMemberState> FamilyOrganizationMembers =
            new List<FamilyOrganizationMemberState>();
        public List<FamilyCenterState> FamilyCenters =
            new List<FamilyCenterState>();
        public List<OrganizationAssetState> OrganizationAssets =
            new List<OrganizationAssetState>();
        public List<CivilMilitaryOfficeDefinitionState>
            CivilMilitaryOfficeDefinitions =
                new List<CivilMilitaryOfficeDefinitionState>();
        public List<CivilMilitaryOfficeAssignmentState>
            CivilMilitaryOfficeAssignments =
                new List<CivilMilitaryOfficeAssignmentState>();
        public List<PersonPrimaryActivityState> PersonPrimaryActivities =
            new List<PersonPrimaryActivityState>();
        public List<HistoricalPersonFamilyIntegrationState>
            HistoricalPersonFamilyIntegrations =
                new List<HistoricalPersonFamilyIntegrationState>();
        public List<Luoyang184LivingWorldState> LuoyangLivingWorlds =
            new List<Luoyang184LivingWorldState>();
        public List<LuoyangPassageTraversalWorldState>
            LuoyangPassageTraversals =
                new List<LuoyangPassageTraversalWorldState>();
        public List<LuoyangPassageOperationalControlState>
            LuoyangPassageOperationalControls =
                new List<LuoyangPassageOperationalControlState>();
        public List<LuoyangPassageDamageRecordState>
            LuoyangPassageDamageRecords =
                new List<LuoyangPassageDamageRecordState>();
        public List<LuoyangPassageRepairOrderState>
            LuoyangPassageRepairOrders =
                new List<LuoyangPassageRepairOrderState>();
        public List<FacilityDefinitionState> FacilityDefinitions =
            new List<FacilityDefinitionState>();
        public List<FacilityState> Facilities = new List<FacilityState>();
        public List<TownFacilityState> TownFacilities =
            new List<TownFacilityState>();
        public List<MerchantBranchState> MerchantBranches =
            new List<MerchantBranchState>();
        public List<StrategicDelegationMandateState>
            StrategicDelegationMandates =
                new List<StrategicDelegationMandateState>();
        public List<StrategicDelegationCommandProposalState>
            StrategicDelegationCommandProposals =
                new List<StrategicDelegationCommandProposalState>();
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
        public List<HouseholdReliefPickupState> HouseholdReliefPickups =
            new List<HouseholdReliefPickupState>();
        public List<HouseholdReliefConsumptionState>
            HouseholdReliefConsumptions =
                new List<HouseholdReliefConsumptionState>();
        public List<HouseholdReliefCareDeliveryState>
            HouseholdReliefCareDeliveries =
                new List<HouseholdReliefCareDeliveryState>();
        public List<PersonNutritionProfileState> PersonNutritionProfiles =
            new List<PersonNutritionProfileState>();
        public List<PersonNutritionLedgerEntryState> PersonNutritionLedgerEntries =
            new List<PersonNutritionLedgerEntryState>();
        public List<NutritionConditionEpisodeState> NutritionConditionEpisodes =
            new List<NutritionConditionEpisodeState>();
        public List<CivilianMedicalCaseState> CivilianMedicalCases =
            new List<CivilianMedicalCaseState>();
        public List<CivilianMedicalPrescriptionState>
            CivilianMedicalPrescriptions =
                new List<CivilianMedicalPrescriptionState>();
        public List<CivilianMedicalTreatmentState> CivilianMedicalTreatments =
            new List<CivilianMedicalTreatmentState>();
        public List<CivilianMedicalServiceState> CivilianMedicalServices =
            new List<CivilianMedicalServiceState>();
        public long CivilianMedicalServiceContractActivationDay;
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
        public List<MilitaryMedicalCaseState> MilitaryMedicalCases =
            new List<MilitaryMedicalCaseState>();
        public List<MilitaryMedicalServiceState> MilitaryMedicalServices =
            new List<MilitaryMedicalServiceState>();
        public List<MilitaryMedicalEvacuationState> MilitaryMedicalEvacuations =
            new List<MilitaryMedicalEvacuationState>();
        public List<MilitaryRearMedicalSiteState> MilitaryRearMedicalSites =
            new List<MilitaryRearMedicalSiteState>();
        public List<MilitaryRearMedicalAdmissionState>
            MilitaryRearMedicalAdmissions =
                new List<MilitaryRearMedicalAdmissionState>();
        public List<MilitaryRearMedicalTreatmentState>
            MilitaryRearMedicalTreatments =
                new List<MilitaryRearMedicalTreatmentState>();
        public List<MilitaryMedicalTransferState> MilitaryMedicalTransfers =
            new List<MilitaryMedicalTransferState>();
        public long MilitaryMedicalTransferContractActivationDay;
        public long MilitaryPostTreatmentTransferContractActivationDay;
        public long MilitaryRepeatedMedicalTransferContractActivationDay;
        public List<MilitaryInjuryEpisodeState> MilitaryInjuryEpisodes =
            new List<MilitaryInjuryEpisodeState>();
        public List<MilitaryInjuryProfileDefinitionState>
            MilitaryInjuryProfiles =
                new List<MilitaryInjuryProfileDefinitionState>();
        public long MilitaryInjuryContractActivationDay;
        public List<MilitaryWoundDeathPolicyDefinitionState>
            MilitaryWoundDeathPolicies =
                new List<MilitaryWoundDeathPolicyDefinitionState>();
        public List<MilitaryWoundDeathState> MilitaryWoundDeaths =
            new List<MilitaryWoundDeathState>();
        public List<MilitaryFamilyInheritanceState>
            MilitaryFamilyInheritances =
                new List<MilitaryFamilyInheritanceState>();
        public List<MilitarySurvivorCompensationState>
            MilitarySurvivorCompensations =
                new List<MilitarySurvivorCompensationState>();
        public long MilitaryWoundDeathContractActivationDay;
        public List<MilitaryMedicalDeathResponsibilityState>
            MilitaryMedicalDeathResponsibilities =
                new List<MilitaryMedicalDeathResponsibilityState>();
        public long MilitaryMedicalDeathResponsibilityContractActivationDay;
        public List<MilitaryInpatientDeteriorationPolicyDefinitionState>
            MilitaryInpatientDeteriorationPolicies =
                new List<MilitaryInpatientDeteriorationPolicyDefinitionState>();
        public List<MilitaryInpatientDeathClosureState>
            MilitaryInpatientDeathClosures =
                new List<MilitaryInpatientDeathClosureState>();
        public long MilitaryInpatientDeathContractActivationDay;
        public List<MilitaryMedicalTransferDeathClosureState>
            MilitaryMedicalTransferDeathClosures =
                new List<MilitaryMedicalTransferDeathClosureState>();
        public long MilitaryMedicalTransferDeathContractActivationDay;
        public List<
            MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState>
            MilitaryOriginalEvacuationDeteriorationPolicies =
                new List<
                    MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState>();
        public List<MilitaryOriginalEvacuationDeathClosureState>
            MilitaryOriginalEvacuationDeathClosures =
                new List<MilitaryOriginalEvacuationDeathClosureState>();
        public long MilitaryOriginalEvacuationDeathContractActivationDay;
        public List<
            MilitaryPatientReturnDeteriorationPolicyDefinitionState>
            MilitaryPatientReturnDeteriorationPolicies =
                new List<
                    MilitaryPatientReturnDeteriorationPolicyDefinitionState>();
        public List<MilitaryPatientReturnDeathClosureState>
            MilitaryPatientReturnDeathClosures =
                new List<MilitaryPatientReturnDeathClosureState>();
        public long MilitaryPatientReturnDeathContractActivationDay;
        public long MilitaryPatientArrivalWaitingTeamDeathContractActivationDay;
        public List<MilitaryReturnTeamDeathPolicyDefinitionState>
            MilitaryReturnTeamDeathPolicies =
                new List<MilitaryReturnTeamDeathPolicyDefinitionState>();
        public List<MilitaryReturnTeamDeathState> MilitaryReturnTeamDeaths =
            new List<MilitaryReturnTeamDeathState>();
        public long MilitaryReturnTeamDeathContractActivationDay;
        public List<MilitarySurgicalProcedureDefinitionState>
            MilitarySurgicalProcedures =
                new List<MilitarySurgicalProcedureDefinitionState>();
        public long MilitarySurgeryContractActivationDay;
        public List<MilitaryFieldHospitalConstructionProjectState>
            MilitaryFieldHospitalConstructionProjects =
                new List<MilitaryFieldHospitalConstructionProjectState>();
        public List<MilitaryFieldHospitalConstructionWorkState>
            MilitaryFieldHospitalConstructionWork =
                new List<MilitaryFieldHospitalConstructionWorkState>();
        public List<MilitaryFieldHospitalMaintenanceState>
            MilitaryFieldHospitalMaintenance =
                new List<MilitaryFieldHospitalMaintenanceState>();
        public bool MilitaryMedicalInitialized;
        public long MilitaryMedicalContractActivationDay;
        public List<ConstructionProjectState> ConstructionProjects =
            new List<ConstructionProjectState>();
        public List<WorldCellPropertyState> CellProperties =
            new List<WorldCellPropertyState>();
        public List<CellPropertyTransferState> CellPropertyTransfers =
            new List<CellPropertyTransferState>();
        public List<FacilityConstructionProjectState>
            FacilityConstructionProjects =
                new List<FacilityConstructionProjectState>();
        public List<FacilityConstructionLaborState> FacilityConstructionLabor =
            new List<FacilityConstructionLaborState>();
        public List<HouseholdMigrationState> HouseholdMigrations =
            new List<HouseholdMigrationState>();
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
        public List<FoodStorageLossState> FoodStorageLosses =
            new List<FoodStorageLossState>();
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
            LivingWorldRuntimeRules.ValidateWorld(this);
            HistoricalEventContractRules.ValidateWorld(this);
            HistoricalPersonFamilyIntegrationRules.ValidateWorld(this);
            Luoyang184LivingWorldRules.ValidateWorld(this);
            PropertyConstructionRules.ValidateWorld(this);
            LuoyangPassageTraversalWorldRules.ValidateWorld(this);
            LuoyangPassageOperationalRules.ValidateWorld(this);
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
                TownFacilities, item => item.Id, "town facility");
            ValidateUniqueIds(
                MerchantBranches, item => item.Id, "merchant branch");
            ValidateUniqueIds(
                StrategicDelegationMandates,
                item => item.Id,
                "strategic delegation mandate");
            ValidateUniqueIds(
                StrategicDelegationCommandProposals,
                item => item.Id,
                "strategic delegation command proposal");
            ValidateUniqueIds(
                LuoyangPassageTraversals,
                item => item.Id,
                "Luoyang passage traversal world state");
            ValidateUniqueIds(
                LuoyangPassageOperationalControls,
                item => item.Id,
                "Luoyang passage operational control");
            ValidateUniqueIds(
                LuoyangPassageDamageRecords,
                item => item.Id,
                "Luoyang passage damage record");
            ValidateUniqueIds(
                LuoyangPassageRepairOrders,
                item => item.Id,
                "Luoyang passage repair order");
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
                HouseholdReliefPickups,
                item => item.Id,
                "household relief pickup");
            ValidateUniqueIds(
                HouseholdReliefCareDeliveries,
                item => item.Id,
                "household relief care delivery");
            ValidateUniqueIds(
                PersonNutritionProfiles,
                item => item.Id,
                "person nutrition profile");
            ValidateUniqueIds(
                PersonNutritionLedgerEntries,
                item => item.Id,
                "person nutrition ledger entry");
            ValidateUniqueIds(
                NutritionConditionEpisodes,
                item => item.Id,
                "nutrition condition episode");
            ValidateUniqueIds(
                CivilianMedicalCases,
                item => item.Id,
                "civilian medical case");
            ValidateUniqueIds(
                CivilianMedicalPrescriptions,
                item => item.Id,
                "civilian medical prescription");
            ValidateUniqueIds(
                CivilianMedicalTreatments,
                item => item.Id,
                "civilian medical treatment");
            ValidateUniqueIds(
                CivilianMedicalServices,
                item => item.Id,
                "civilian medical service");
            ValidateUniqueIds(
                MilitaryMedicalCases,
                item => item.Id,
                "military medical case");
            ValidateUniqueIds(
                MilitaryMedicalServices,
                item => item.Id,
                "military medical service");
            ValidateUniqueIds(
                MilitaryMedicalEvacuations,
                item => item.Id,
                "military medical evacuation");
            ValidateUniqueIds(
                MilitaryRearMedicalSites,
                item => item.Id,
                "military rear medical site");
            ValidateUniqueIds(
                MilitaryRearMedicalAdmissions,
                item => item.Id,
                "military rear medical admission");
            ValidateUniqueIds(
                MilitaryRearMedicalTreatments,
                item => item.Id,
                "military rear medical treatment");
            ValidateUniqueIds(
                MilitaryMedicalTransfers,
                item => item.Id,
                "military medical transfer");
            ValidateUniqueIds(
                MilitaryInjuryEpisodes,
                item => item.Id,
                "military injury episode");
            ValidateUniqueIds(
                MilitaryInjuryProfiles,
                item => item.Id,
                "military injury profile");
            ValidateUniqueIds(
                MilitarySurgicalProcedures,
                item => item.Id,
                "military surgical procedure");
            ValidateUniqueIds(
                MilitaryFieldHospitalConstructionProjects,
                item => item.Id,
                "military field hospital construction project");
            ValidateUniqueIds(
                MilitaryFieldHospitalConstructionWork,
                item => item.Id,
                "military field hospital construction work");
            ValidateUniqueIds(
                MilitaryFieldHospitalMaintenance,
                item => item.Id,
                "military field hospital maintenance");
            ValidateUniqueIds(
                FoodStorageLosses,
                item => item.Id,
                "food storage loss");
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
                    person.PermanentLaborCapacityPenaltyBasisPoints < 0 ||
                    person.PermanentLaborCapacityPenaltyBasisPoints > 10_000 ||
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
            ValidateFoodStorageLosses();
            ValidateFormalMarket(locationIds);
            ValidateCivilianFreight(personIds, locationIds);
            ValidatePublicReliefRecovery();
            ValidateHouseholdReliefPickups();
            ValidateHouseholdReliefConsumptions();
            ValidateHouseholdReliefCareDeliveries();
            ValidateLongTermNutrition();
            ValidateCivilianMedicalCare();
            ValidateMilitaryMedicalCare();
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
            ValidateVillageReliefPolicies(personIds, organizationIds);
            ValidateMerchantTownFacilities(
                personIds,
                locationIds,
                organizationIds);

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
                if (!string.IsNullOrEmpty(commodity.ProductDefinitionId))
                {
                    ValidateContentReference(
                        commodity.ProductDefinitionId,
                        "commodity product",
                        commodity.Id);
                }
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

            ValidateStrategicDelegation(
                personIds,
                locationIds,
                organizationIds,
                positionIds);
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

        private void ValidateStrategicDelegation(
            HashSet<string> personIds,
            HashSet<string> locationIds,
            HashSet<string> organizationIds,
            HashSet<string> positionIds)
        {
            var mandates = new Dictionary<
                string,
                StrategicDelegationMandateState>(StringComparer.Ordinal);
            for (var i = 0; i < StrategicDelegationMandates.Count; i++)
            {
                var mandate = StrategicDelegationMandates[i] ??
                    throw new InvalidOperationException(
                        "A strategic delegation mandate cannot be null.");
                mandate.ValidateContract();
                if (!personIds.Contains(mandate.IssuerPersonId) ||
                    !personIds.Contains(mandate.AssigneePersonId) ||
                    !organizationIds.Contains(mandate.OrganizationId) ||
                    !positionIds.Contains(mandate.AssigneePositionId) ||
                    !locationIds.Contains(mandate.JurisdictionLocationId))
                {
                    throw new InvalidOperationException(
                        $"Mandate {mandate.Id} contains a missing reference.");
                }

                var assigneePosition = FindPosition(
                    Positions,
                    mandate.AssigneePositionId);
                if (assigneePosition.OrganizationId != mandate.OrganizationId)
                {
                    throw new InvalidOperationException(
                        $"Mandate {mandate.Id} uses an assignee position from another organization.");
                }

                if (!string.IsNullOrEmpty(mandate.IssuerPositionId))
                {
                    if (!positionIds.Contains(mandate.IssuerPositionId) ||
                        FindPosition(Positions, mandate.IssuerPositionId)
                            .OrganizationId != mandate.OrganizationId)
                    {
                        throw new InvalidOperationException(
                            $"Mandate {mandate.Id} has an invalid issuer position snapshot.");
                    }
                }

                var organization = FindOrganization(
                    Organizations,
                    mandate.OrganizationId);
                var capabilities = new HashSet<string>(
                    OrganizationStrategicDelegationCapabilityCatalog
                        .CreateAllowedOrders(organization.Type),
                    StringComparer.Ordinal);
                for (var orderIndex = 0;
                     orderIndex < mandate.AllowedOrderIdsSnapshot.Count;
                     orderIndex++)
                {
                    if (!capabilities.Contains(
                            mandate.AllowedOrderIdsSnapshot[orderIndex]))
                    {
                        throw new InvalidOperationException(
                            $"Mandate {mandate.Id} exceeds organization capability.");
                    }
                }

                mandates.Add(mandate.Id, mandate);
            }

            for (var i = 0;
                 i < StrategicDelegationCommandProposals.Count;
                 i++)
            {
                var proposal = StrategicDelegationCommandProposals[i] ??
                    throw new InvalidOperationException(
                        "A strategic delegation proposal cannot be null.");
                proposal.ValidateContract();
                if (!mandates.TryGetValue(
                        proposal.MandateId,
                        out var mandate) ||
                    proposal.ActorPersonId != mandate.AssigneePersonId ||
                    proposal.OrganizationId != mandate.OrganizationId ||
                    !locationIds.Contains(
                        proposal.JurisdictionLocationId) ||
                    proposal.CreatedDay < mandate.IssuedDay ||
                    proposal.CreatedDay > AbsoluteDay ||
                    proposal.EstimatedCost > mandate.BudgetLimit ||
                    !mandate.AllowedOrderIdsSnapshot.Contains(
                        proposal.OrderId) ||
                    !IsSameOrDescendantLocation(
                        proposal.JurisdictionLocationId,
                        mandate.JurisdictionLocationId))
                {
                    throw new InvalidOperationException(
                        $"Invalid strategic delegation proposal {proposal.Id}.");
                }
            }
        }

        private bool IsSameOrDescendantLocation(
            string locationId,
            string rootLocationId)
        {
            var cursor = FindLocation(Locations, locationId);
            while (cursor != null)
            {
                if (cursor.Id == rootLocationId)
                {
                    return true;
                }

                cursor = string.IsNullOrEmpty(cursor.ParentLocationId)
                    ? null
                    : FindLocation(Locations, cursor.ParentLocationId);
            }

            return false;
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
                _ = new StableId(order.DeliveryPolicyId);
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
                var targetArmy = FindArmy(Armies, order.TargetArmyId);
                InventoryContainerState targetInventoryContainer = null;
                var hasTargetContainer = string.IsNullOrEmpty(
                        order.TargetInventoryContainerId) ||
                    containers.TryGetValue(
                        order.TargetInventoryContainerId,
                        out targetInventoryContainer);
                var validDeliveryContract = order.DeliveryPolicyId ==
                        MilitaryLogisticsDeliveryPolicyIds.ArmyProvisions &&
                    string.IsNullOrEmpty(order.TargetInventoryContainerId) ||
                    order.DeliveryPolicyId == MilitaryLogisticsDeliveryPolicyIds
                        .ArmyInventoryContainer &&
                    hasTargetContainer &&
                    targetInventoryContainer != null &&
                    targetInventoryContainer.Id ==
                        targetArmy.MedicalInventoryContainerId &&
                    targetInventoryContainer.OwnerOrganizationId ==
                        order.BuyerOrganizationId;
                long inventoryReceiptQuantity = 0;
                var validInventoryReceipts = true;
                for (var transactionIndex = 0;
                     transactionIndex < InventoryTransactions.Count;
                     transactionIndex++)
                {
                    var receipt = InventoryTransactions[transactionIndex];
                    if (receipt.Type !=
                            InventoryTransactionType
                                .MilitaryLogisticsDelivered ||
                        receipt.SourceMilitaryLogisticsOrderId != order.Id)
                    {
                        continue;
                    }

                    validInventoryReceipts &=
                        receipt.Lines != null && receipt.Lines.Count == 1 &&
                        receipt.Lines[0].QuantityDelta > 0 &&
                        receipt.Lines[0].ReservedQuantityDelta == 0 &&
                        receipt.Lines[0].ProductDefinitionId ==
                            order.CargoProductDefinitionId &&
                        receipt.Lines[0].OwnerOrganizationId ==
                            order.BuyerOrganizationId &&
                        receipt.Lines[0].InventoryContainerId ==
                            order.TargetInventoryContainerId;
                    if (receipt.Lines != null && receipt.Lines.Count == 1)
                    {
                        inventoryReceiptQuantity = checked(
                            inventoryReceiptQuantity +
                            receipt.Lines[0].QuantityDelta);
                    }
                }
                validInventoryReceipts &= order.DeliveryPolicyId ==
                        MilitaryLogisticsDeliveryPolicyIds
                            .ArmyInventoryContainer
                    ? inventoryReceiptQuantity ==
                        order.DeliveredCargoQuantity
                    : inventoryReceiptQuantity == 0;
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
                    targetArmy.OrganizationId !=
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
                    !validDeliveryContract ||
                    !validInventoryReceipts ||
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
                        $"delivery={validDeliveryContract}/" +
                        $"{validInventoryReceipts}/" +
                        $"{inventoryReceiptQuantity}, " +
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
                    service.Status == MilitaryServiceStatus.Wounded ||
                    service.Status ==
                        MilitaryServiceStatus.MedicalEvacuationDuty;
                var evacuationDetached =
                    IsMilitaryMedicalEvacuationService(service.Id);
                if (available &&
                        (!person.IsAlive ||
                         person.LocationId != army.LocationId &&
                         !evacuationDetached) ||
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

        private static OrganizationState FindOrganization(
            IList<OrganizationState> organizations,
            string organizationId)
        {
            for (var i = 0; i < organizations.Count; i++)
            {
                if (organizations[i].Id == organizationId)
                {
                    return organizations[i];
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

        private void ValidateMerchantTownFacilities(
            HashSet<string> personIds,
            HashSet<string> locationIds,
            HashSet<string> organizationIds)
        {
            if (TownFacilities == null || MerchantBranches == null)
            {
                throw new InvalidOperationException(
                    "Merchant town collections cannot be null.");
            }

            var familyIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Families.Count; i++)
            {
                familyIds.Add(Families[i].Id);
            }

            var organizations = new Dictionary<string, OrganizationState>(
                StringComparer.Ordinal);
            for (var i = 0; i < Organizations.Count; i++)
            {
                organizations.Add(Organizations[i].Id, Organizations[i]);
            }

            var containers = new Dictionary<string, InventoryContainerState>(
                StringComparer.Ordinal);
            for (var i = 0; i < InventoryContainers.Count; i++)
            {
                containers.Add(InventoryContainers[i].Id, InventoryContainers[i]);
            }

            var facilities = new Dictionary<string, TownFacilityState>(
                StringComparer.Ordinal);
            var placedFacilityCoordinates = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < TownFacilities.Count; i++)
            {
                var facility = TownFacilities[i] ??
                    throw new InvalidOperationException(
                        "A town facility cannot be null.");
                _ = new StableId(facility.Id);
                _ = new StableId(facility.KindId);
                _ = new StableId(facility.AccessPolicyId);
                facilities.Add(facility.Id, facility);

                var organizationOwned =
                    !string.IsNullOrEmpty(facility.OwnerOrganizationId);
                var familyOwned = !string.IsNullOrEmpty(facility.OwnerFamilyId);
                if (string.IsNullOrWhiteSpace(facility.DisplayName) ||
                    !locationIds.Contains(facility.LocationId) ||
                    organizationOwned &&
                    !organizationIds.Contains(facility.OwnerOrganizationId) ||
                    familyOwned && !familyIds.Contains(facility.OwnerFamilyId) ||
                    organizationOwned && familyOwned ||
                    !string.IsNullOrEmpty(facility.ManagerPersonId) &&
                    !personIds.Contains(facility.ManagerPersonId))
                {
                    throw new InvalidOperationException(
                        $"Invalid town facility {facility.Id}.");
                }

                if (!string.IsNullOrEmpty(facility.InventoryContainerId))
                {
                    if (!containers.TryGetValue(
                            facility.InventoryContainerId,
                            out var container) ||
                        container.LocationId != facility.LocationId ||
                        organizationOwned &&
                        container.OwnerOrganizationId !=
                            facility.OwnerOrganizationId ||
                        familyOwned &&
                        container.OwnerFamilyId != facility.OwnerFamilyId)
                    {
                        throw new InvalidOperationException(
                            $"Town facility {facility.Id} has an invalid inventory container.");
                    }
                }

                if (organizationOwned &&
                    !string.IsNullOrEmpty(facility.ManagerPersonId) &&
                    !HasOrganizationMembership(
                        facility.ManagerPersonId,
                        facility.OwnerOrganizationId))
                {
                    throw new InvalidOperationException(
                        $"Town facility {facility.Id} manager is not an organization member.");
                }

                if (facility.HasMapPlacement)
                {
                    if (string.IsNullOrWhiteSpace(facility.DistrictId) ||
                        facility.MapXBasisPoints <= 0 ||
                        facility.MapXBasisPoints >= 10_000 ||
                        facility.MapYBasisPoints <= 0 ||
                        facility.MapYBasisPoints >= 10_000 ||
                        facility.FootprintWidthBasisPoints <= 0 ||
                        facility.FootprintWidthBasisPoints > 5_000 ||
                        facility.FootprintHeightBasisPoints <= 0 ||
                        facility.FootprintHeightBasisPoints > 5_000)
                    {
                        throw new InvalidOperationException(
                            $"Town facility {facility.Id} has an invalid map placement.");
                    }
                    _ = new StableId(facility.DistrictId);
                    var coordinateKey = facility.LocationId + "|" +
                        facility.MapXBasisPoints + "|" +
                        facility.MapYBasisPoints;
                    if (!placedFacilityCoordinates.Add(coordinateKey))
                    {
                        throw new InvalidOperationException(
                            $"Town facility {facility.Id} duplicates a map placement.");
                    }
                }
                else if (!string.IsNullOrEmpty(facility.DistrictId) ||
                         facility.MapXBasisPoints != 0 ||
                         facility.MapYBasisPoints != 0 ||
                         facility.FootprintWidthBasisPoints != 0 ||
                         facility.FootprintHeightBasisPoints != 0)
                {
                    throw new InvalidOperationException(
                        $"Unplaced town facility {facility.Id} carries placement data.");
                }
            }

            var branchLocations = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MerchantBranches.Count; i++)
            {
                var branch = MerchantBranches[i] ??
                    throw new InvalidOperationException(
                        "A merchant branch cannot be null.");
                _ = new StableId(branch.Id);
                if (!organizations.TryGetValue(
                        branch.OrganizationId,
                        out var organization) ||
                    organization.Type != OrganizationType.Merchant ||
                    string.IsNullOrWhiteSpace(branch.DisplayName) ||
                    !locationIds.Contains(branch.LocationId) ||
                    !personIds.Contains(branch.ManagerPersonId) ||
                    !HasOrganizationMembership(
                        branch.ManagerPersonId,
                        branch.OrganizationId) ||
                    branch.FacilityIds == null ||
                    branch.FacilityIds.Count == 0 ||
                    !branchLocations.Add(
                        branch.OrganizationId + "|" + branch.LocationId))
                {
                    throw new InvalidOperationException(
                        $"Invalid merchant branch {branch.Id}.");
                }

                if (!containers.TryGetValue(
                        branch.InventoryContainerId,
                        out var branchContainer) ||
                    branchContainer.OwnerOrganizationId !=
                        branch.OrganizationId ||
                    branchContainer.LocationId != branch.LocationId ||
                    !string.IsNullOrEmpty(branchContainer.CarrierPersonId) ||
                    branch.IsHeadquarters &&
                    organization.HeadquartersLocationId != branch.LocationId)
                {
                    throw new InvalidOperationException(
                        $"Merchant branch {branch.Id} has an invalid warehouse.");
                }

                var facilityIds = new HashSet<string>(StringComparer.Ordinal);
                for (var facilityIndex = 0;
                     facilityIndex < branch.FacilityIds.Count;
                     facilityIndex++)
                {
                    var facilityId = branch.FacilityIds[facilityIndex];
                    if (!facilityIds.Add(facilityId) ||
                        !facilities.TryGetValue(facilityId, out var facility) ||
                        facility.LocationId != branch.LocationId ||
                        facility.OwnerOrganizationId != branch.OrganizationId)
                    {
                        throw new InvalidOperationException(
                            $"Merchant branch {branch.Id} references an invalid facility.");
                    }
                }
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

                var usesCountyReliefAuthority =
                    village.HouseholdReliefAuthorizationPolicyId ==
                    HouseholdReliefAuthorizationPolicyIds.CountyGovernmentLeader;
                var usesEmergencyReliefAuthority =
                    village.HouseholdReliefAuthorizationPolicyId ==
                    HouseholdReliefAuthorizationPolicyIds.EmergencySystem;
                if (village.HouseholdReliefPriorityPolicyId !=
                        HouseholdReliefPriorityPolicyIds
                            .NeedSeverityVulnerability ||
                    (!usesCountyReliefAuthority &&
                     !usesEmergencyReliefAuthority) ||
                    usesCountyReliefAuthority && string.IsNullOrEmpty(
                        village.HouseholdReliefAuthorityOrganizationId) ||
                    usesEmergencyReliefAuthority && !string.IsNullOrEmpty(
                        village.HouseholdReliefAuthorityOrganizationId))
                {
                    throw new InvalidOperationException(
                        $"Village {village.Id} has an invalid relief policy.");
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

        private void ValidateVillageReliefPolicies(
            HashSet<string> personIds,
            HashSet<string> organizationIds)
        {
            var organizations = new Dictionary<string, OrganizationState>(
                StringComparer.Ordinal);
            for (var i = 0; i < Organizations.Count; i++)
            {
                organizations.Add(Organizations[i].Id, Organizations[i]);
            }

            for (var i = 0; i < Villages.Count; i++)
            {
                var village = Villages[i];
                if (village.HouseholdReliefAuthorizationPolicyId ==
                    HouseholdReliefAuthorizationPolicyIds.EmergencySystem)
                {
                    continue;
                }
                if (village.HouseholdReliefAuthorizationPolicyId !=
                        HouseholdReliefAuthorizationPolicyIds
                            .CountyGovernmentLeader ||
                    !organizationIds.Contains(
                        village.HouseholdReliefAuthorityOrganizationId) ||
                    !organizations.TryGetValue(
                        village.HouseholdReliefAuthorityOrganizationId,
                        out var organization) ||
                    organization.Type != OrganizationType.Government ||
                    string.IsNullOrEmpty(organization.LeaderPersonId) ||
                    !personIds.Contains(organization.LeaderPersonId) ||
                    !CountyGovernances.Exists(item =>
                        item.CountyLocationId == village.ParentLocationId &&
                        item.GovernmentOrganizationId == organization.Id))
                {
                    throw new InvalidOperationException(
                        $"Village {village.Id} has an invalid county relief authority.");
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
                var facility = VillageFacilities[i] ??
                    throw new InvalidOperationException(
                        "A village facility cannot be null.");
                facilities.Add(facility.Id, facility);
                if (facility.CapabilityTags == null ||
                    string.IsNullOrEmpty(
                        facility.FoodStorageEnvironmentId) ||
                    facility.FoodStorageProtectionBasisPoints < 0 ||
                    facility.FoodStorageProtectionBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        $"Invalid food storage environment on {facility.Id}.");
                }

                var capabilityTags = new HashSet<string>(
                    StringComparer.Ordinal);
                for (var tagIndex = 0;
                     tagIndex < facility.CapabilityTags.Count;
                     tagIndex++)
                {
                    ValidateContentReference(
                        facility.CapabilityTags[tagIndex],
                        "village facility capability",
                        facility.Id);
                    if (!capabilityTags.Add(
                            facility.CapabilityTags[tagIndex]))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate capability on {facility.Id}.");
                    }
                }
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
                    container.CapacityWeight <= 0 ||
                    string.IsNullOrEmpty(
                        container.FoodStorageEnvironmentId) ||
                    container.FoodStorageProtectionBasisPoints < 0 ||
                    container.FoodStorageProtectionBasisPoints > 10_000)
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
                var validFamilyContainerStorage = familyContainerStored &&
                    familyIds.Contains(batch.OwnerFamilyId) &&
                    containers.TryGetValue(
                        batch.InventoryContainerId, out var familyContainer) &&
                    familyContainer.OwnerFamilyId == batch.OwnerFamilyId &&
                    string.IsNullOrEmpty(familyContainer.CarrierPersonId);
                var validCivilianFreightStorage = false;
                var validPublicReliefFreightStorage = false;
                var validMerchantCarrierStorage = false;
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
                if (familyContainerStored &&
                    containers.TryGetValue(
                        batch.InventoryContainerId, out var merchantContainer) &&
                    !string.IsNullOrEmpty(merchantContainer.CarrierPersonId) &&
                    transactions.TryGetValue(
                        batch.SourceTransactionId, out var merchantTransaction) &&
                    merchantTransaction.Type ==
                        InventoryTransactionType.MerchantMarketPurchased &&
                    merchantTransaction.ActorPersonId ==
                        merchantContainer.CarrierPersonId)
                {
                    var merchant = People.Find(item =>
                        item.Id == merchantContainer.CarrierPersonId);
                    validMerchantCarrierStorage = merchant != null &&
                        merchant.FamilyId == batch.OwnerFamilyId &&
                        merchantContainer.OwnerFamilyId == batch.OwnerFamilyId;
                }
                if (!validFamilyStorage && !validOrganizationStorage &&
                    !validFamilyContainerStorage &&
                    !validCivilianFreightStorage &&
                    !validPublicReliefFreightStorage &&
                    !validMerchantCarrierStorage ||
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
                    batch.SeedPurityBasisPoints > 10_000 ||
                    batch.NextFoodStorageAssessmentDay < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid product batch {batch.Id}.");
                }
            }

            ValidateResourceExtraction(
                personIds,
                locationIds,
                familyIds,
                organizationIds,
                facilities,
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
                    !string.IsNullOrEmpty(
                        transaction.SourceFacilityConstructionProjectId) &&
                    !FacilityConstructionProjects.Exists(project =>
                        project != null && project.Id ==
                            transaction.SourceFacilityConstructionProjectId) ||
                    !string.IsNullOrEmpty(
                        transaction.SourceFacilityConstructionProjectId) &&
                    transaction.Type != InventoryTransactionType
                        .FacilityConstructionMaterialReserved &&
                    transaction.Type != InventoryTransactionType
                        .FacilityConstructionMaterialConsumed &&
                    transaction.Type != InventoryTransactionType
                        .FacilityConstructionMaterialReleased ||
                    !string.IsNullOrEmpty(
                        transaction.HouseholdReliefRecipientPersonId) &&
                    (!personIds.Contains(
                         transaction.HouseholdReliefRecipientPersonId) ||
                     transaction.Type !=
                         InventoryTransactionType.FoodConsumed ||
                     string.IsNullOrEmpty(
                         transaction.SourceHouseholdReliefConsumptionId)) ||
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
                        .MilitaryLogisticsHandoffLoaded ||
                    transaction.Type == InventoryTransactionType
                        .MilitaryLogisticsDelivered;
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
                var merchantPurchased = transaction.Type ==
                    InventoryTransactionType.MerchantMarketPurchased;
                var merchantDamaged = transaction.Type ==
                    InventoryTransactionType.MerchantCargoDamaged;
                var merchantSold = transaction.Type ==
                    InventoryTransactionType.MerchantMarketSold;
                var merchantInventory = merchantPurchased ||
                    merchantDamaged || merchantSold;
                var medicalTransferReservation = transaction.Type ==
                    InventoryTransactionType
                        .MilitaryMedicalTransferMedicineReserved;
                var medicalTransferRelease = transaction.Type ==
                    InventoryTransactionType
                        .MilitaryMedicalTransferMedicineReleased;
                var medicalTransferReservationChange =
                    medicalTransferReservation || medicalTransferRelease;
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
                var validMerchantProvenance = merchantInventory &&
                    !string.IsNullOrEmpty(transaction.ActorPersonId) &&
                    string.IsNullOrEmpty(transaction.SourceWorkOrderId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceMilitaryProcurementId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceEquipmentRepairOrderId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceResourceExtractionOrderId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceMilitaryLogisticsOrderId) &&
                    string.IsNullOrEmpty(transaction.SourceVillageId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceCountyGovernanceId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceFormalMarketOrderId) &&
                    string.IsNullOrEmpty(
                        transaction.SourceCivilianFreightId) &&
                    transaction.LegacyFamilyGrainDelta == 0 &&
                    transaction.LegacyFamilySeedGrainDelta == 0 &&
                    transaction.LegacyVillagePublicGranaryDelta == 0 &&
                    transaction.LegacyCountyGranaryDelta == 0;
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
                    medicalTransferReservationChange !=
                    !string.IsNullOrEmpty(
                        transaction.SourceMilitaryMedicalTransferId) ||
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
                    merchantInventory && !validMerchantProvenance ||
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

                    if (medicalTransferReservation &&
                        (line.QuantityDelta != 0 ||
                         line.ReservedQuantityDelta <= 0))
                    {
                        throw new InvalidOperationException(
                            $"Medical transfer reservation {transaction.Id} has an invalid line.");
                    }
                    if (medicalTransferRelease &&
                        (line.QuantityDelta != 0 ||
                         line.ReservedQuantityDelta >= 0))
                    {
                        throw new InvalidOperationException(
                            $"Medical transfer release {transaction.Id} has an invalid line.");
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

                    if (merchantInventory)
                    {
                        containers.TryGetValue(
                            line.InventoryContainerId ?? string.Empty,
                            out var merchantContainer);
                        var merchant = People.Find(item =>
                            item.Id == transaction.ActorPersonId);
                        var validMerchantLine =
                            merchantContainer != null &&
                            merchant != null &&
                            merchantContainer.CarrierPersonId == merchant.Id &&
                            merchantContainer.OwnerFamilyId == merchant.FamilyId &&
                            line.OwnerFamilyId == merchant.FamilyId &&
                            string.IsNullOrEmpty(line.OwnerOrganizationId) &&
                            string.IsNullOrEmpty(line.StorageFacilityId) &&
                            line.ReservedQuantityDelta == 0 &&
                            (merchantPurchased && line.QuantityDelta > 0 ||
                             (merchantDamaged || merchantSold) &&
                             line.QuantityDelta < 0);
                        if (!validMerchantLine)
                        {
                            throw new InvalidOperationException(
                                $"Merchant inventory line on {transaction.Id} is invalid.");
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

            for (var personIndex = 0;
                 personIndex < People.Count;
                 personIndex++)
            {
                var person = People[personIndex];
                long totalWeight = 0;
                for (var inventoryIndex = 0;
                     inventoryIndex < Inventories.Count;
                     inventoryIndex++)
                {
                    var stack = Inventories[inventoryIndex];
                    if (stack.OwnerPersonId == person.Id)
                    {
                        totalWeight = checked(
                            totalWeight +
                            (long)stack.Quantity *
                            FindCommodity(Commodities, stack.CommodityId)
                                .UnitWeight);
                    }
                }
                for (var containerIndex = 0;
                     containerIndex < InventoryContainers.Count;
                     containerIndex++)
                {
                    var container = InventoryContainers[containerIndex];
                    if (container.CarrierPersonId != person.Id ||
                        container.KindId !=
                            "inventory_container.merchant_caravan")
                    {
                        continue;
                    }
                    for (var batchIndex = 0;
                         batchIndex < ProductBatches.Count;
                         batchIndex++)
                    {
                        var batch = ProductBatches[batchIndex];
                        if (batch.InventoryContainerId == container.Id)
                        {
                            totalWeight = checked(
                                totalWeight +
                                batch.Quantity * batch.UnitWeight);
                        }
                    }
                }
                if (totalWeight > person.CargoCapacity)
                {
                    throw new InvalidOperationException(
                        $"Person {person.Id} exceeds merchant cargo capacity.");
                }
            }

        }

        private void ValidateFoodStorageLosses()
        {
            var batches = new Dictionary<string, ProductBatchState>(
                StringComparer.Ordinal);
            for (var i = 0; i < ProductBatches.Count; i++)
            {
                batches.Add(ProductBatches[i].Id, ProductBatches[i]);
            }
            var transactions = new Dictionary<string, InventoryTransactionState>(
                StringComparer.Ordinal);
            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                transactions.Add(
                    InventoryTransactions[i].Id,
                    InventoryTransactions[i]);
            }
            var recordIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < FoodStorageLosses.Count; i++)
            {
                var record = FoodStorageLosses[i] ??
                    throw new InvalidOperationException(
                        "A food storage loss cannot be null.");
                recordIds.Add(record.Id);
                if (!batches.TryGetValue(record.BatchId, out var batch) ||
                    record.ProductDefinitionId != batch.ProductDefinitionId ||
                    record.StorageFacilityId != batch.StorageFacilityId ||
                    record.InventoryContainerId != batch.InventoryContainerId ||
                    string.IsNullOrEmpty(record.StorageEnvironmentId) ||
                    record.StorageProtectionBasisPoints < 0 ||
                    record.StorageProtectionBasisPoints > 10_000 ||
                    record.FoodSpoilageSensitivityBasisPoints < 0 ||
                    record.FoodSpoilageSensitivityBasisPoints > 30_000 ||
                    record.Day < 0 || record.Day > AbsoluteDay ||
                    record.QuantityBefore < 0 ||
                    record.ReservedQuantity < 0 ||
                    record.ReservedQuantity > record.QuantityBefore ||
                    record.QuantityLost < 0 ||
                    record.QuantityAfter != checked(
                        record.QuantityBefore - record.QuantityLost) ||
                    record.QuantityAfter < record.ReservedQuantity ||
                    record.FreshnessBeforeBasisPoints < 0 ||
                    record.FreshnessBeforeBasisPoints > 10_000 ||
                    record.FreshnessAfterBasisPoints != Math.Max(
                        0,
                        record.FreshnessBeforeBasisPoints -
                        record.EffectiveLossBasisPoints) ||
                    record.EffectiveLossBasisPoints != Math.Min(
                        10_000L,
                        200L * record.FoodSpoilageSensitivityBasisPoints *
                        (10_000 - record.StorageProtectionBasisPoints) /
                        100_000_000L))
                {
                    throw new InvalidOperationException(
                        $"Invalid food storage loss {record.Id}.");
                }
                _ = new StableId(record.StorageEnvironmentId);

                if (record.QuantityLost == 0)
                {
                    if (!string.IsNullOrEmpty(record.InventoryTransactionId))
                    {
                        throw new InvalidOperationException(
                            $"Zero food storage loss {record.Id} has an inventory transaction.");
                    }
                    continue;
                }

                if (!transactions.TryGetValue(
                        record.InventoryTransactionId,
                        out var transaction) ||
                    transaction.Type !=
                        InventoryTransactionType.FoodStorageNaturalLoss ||
                    transaction.SourceFoodStorageLossId != record.Id ||
                    transaction.Day != record.Day ||
                    transaction.Lines.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"Food storage loss {record.Id} lacks its inventory transaction.");
                }
                var line = transaction.Lines[0];
                if (line.BatchId != batch.Id ||
                    line.ProductDefinitionId != batch.ProductDefinitionId ||
                    line.OwnerFamilyId != batch.OwnerFamilyId ||
                    line.OwnerOrganizationId != batch.OwnerOrganizationId ||
                    line.StorageFacilityId != batch.StorageFacilityId ||
                    line.InventoryContainerId != batch.InventoryContainerId ||
                    line.UnitId != batch.UnitId ||
                    line.QuantityDelta != -record.QuantityLost ||
                    line.ReservedQuantityDelta != 0)
                {
                    throw new InvalidOperationException(
                        $"Food storage loss transaction {transaction.Id} is inconsistent.");
                }
            }

            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                var transaction = InventoryTransactions[i];
                var storageLoss = transaction.Type ==
                    InventoryTransactionType.FoodStorageNaturalLoss;
                if (storageLoss != !string.IsNullOrEmpty(
                        transaction.SourceFoodStorageLossId) ||
                    storageLoss && !recordIds.Contains(
                        transaction.SourceFoodStorageLossId))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has invalid storage-loss provenance.");
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

        private void ValidateHouseholdReliefPickups()
        {
            var villages = new Dictionary<string, VillageState>(
                StringComparer.Ordinal);
            var families = new Dictionary<string, FamilyState>(
                StringComparer.Ordinal);
            var people = new HashSet<string>(StringComparer.Ordinal);
            var events = new Dictionary<string, WorldEventOutboxState>(
                StringComparer.Ordinal);
            var transactions = new Dictionary<
                string, InventoryTransactionState>(StringComparer.Ordinal);
            var organizations = new Dictionary<string, OrganizationState>(
                StringComparer.Ordinal);
            for (var i = 0; i < Villages.Count; i++)
                villages.Add(Villages[i].Id, Villages[i]);
            for (var i = 0; i < Families.Count; i++)
                families.Add(Families[i].Id, Families[i]);
            for (var i = 0; i < People.Count; i++)
                people.Add(People[i].Id);
            for (var i = 0; i < WorldEventOutbox.Count; i++)
                events.Add(WorldEventOutbox[i].Id, WorldEventOutbox[i]);
            for (var i = 0; i < InventoryTransactions.Count; i++)
                transactions.Add(
                    InventoryTransactions[i].Id, InventoryTransactions[i]);
            for (var i = 0; i < Organizations.Count; i++)
                organizations.Add(Organizations[i].Id, Organizations[i]);

            var referencedTransactions = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < HouseholdReliefPickups.Count; i++)
            {
                var pickup = HouseholdReliefPickups[i] ??
                    throw new InvalidOperationException(
                        "A household relief pickup cannot be null.");
                _ = new StableId(pickup.Id);
                var usesLegacyPriority = pickup.PriorityPolicyId ==
                    HouseholdReliefPriorityPolicyIds
                        .LegacySettlementFamilyOrder;
                var usesNeedPriority = pickup.PriorityPolicyId ==
                    HouseholdReliefPriorityPolicyIds.NeedSeverityVulnerability;
                var hasValidLegacySnapshot = usesLegacyPriority &&
                    pickup.AuthorizationPolicyId ==
                        HouseholdReliefAuthorizationPolicyIds.LegacySystem &&
                    string.IsNullOrEmpty(pickup.AuthorizingOrganizationId) &&
                    string.IsNullOrEmpty(pickup.AuthorizingPersonId) &&
                    pickup.AuthorizedDay == -1 &&
                    pickup.ShortfallSeverityBasisPoints == -1 &&
                    pickup.VulnerableAffectedPersonCount == -1 &&
                    pickup.AffectedPersonCountAtAuthorization == -1;
                var hasCountyAuthority = usesNeedPriority &&
                    pickup.AuthorizationPolicyId ==
                        HouseholdReliefAuthorizationPolicyIds
                            .CountyGovernmentLeader &&
                    organizations.TryGetValue(
                        pickup.AuthorizingOrganizationId,
                        out var authorizingOrganization) &&
                    authorizingOrganization.Type == OrganizationType.Government &&
                    people.Contains(pickup.AuthorizingPersonId);
                var hasEmergencyAuthority = usesNeedPriority &&
                    pickup.AuthorizationPolicyId ==
                        HouseholdReliefAuthorizationPolicyIds.EmergencySystem &&
                    string.IsNullOrEmpty(pickup.AuthorizingOrganizationId) &&
                    string.IsNullOrEmpty(pickup.AuthorizingPersonId);
                var hasValidNeedSnapshot = usesNeedPriority &&
                    pickup.AuthorizedDay == pickup.SettlementDay &&
                    pickup.ShortfallSeverityBasisPoints > 0 &&
                    pickup.ShortfallSeverityBasisPoints <= 10_000 &&
                    pickup.AffectedPersonCountAtAuthorization > 0 &&
                    pickup.VulnerableAffectedPersonCount >= 0 &&
                    pickup.VulnerableAffectedPersonCount <=
                        pickup.AffectedPersonCountAtAuthorization &&
                    (hasCountyAuthority || hasEmergencyAuthority);
                if (!Enum.IsDefined(
                        typeof(HouseholdReliefPickupStatus), pickup.Status) ||
                    !villages.TryGetValue(
                        pickup.VillageId, out var village) ||
                    !families.TryGetValue(
                        pickup.FamilyId, out var family) ||
                    family.VillageId != pickup.VillageId ||
                    !village.HouseholdIds.Contains(pickup.FamilyId) ||
                    !events.TryGetValue(
                        pickup.SourceShortfallEventId,
                        out var sourceEvent) ||
                    sourceEvent.EventTypeId !=
                        "mandate.event.formal_food.household_shortfall_detected" ||
                    sourceEvent.Day != pickup.SettlementDay ||
                    pickup.SettlementDay <= 0 ||
                    pickup.SettlementDay > AbsoluteDay ||
                    pickup.RequestedNutritionBasisUnits <= 0 ||
                    pickup.DeliveredNutritionBasisUnits < 0 ||
                    pickup.DeliveredPhysicalQuantity < 0 ||
                    (!hasValidLegacySnapshot && !hasValidNeedSnapshot) ||
                    pickup.RemainingNutritionBasisUnits != Math.Max(
                        0L,
                        pickup.RequestedNutritionBasisUnits -
                        pickup.DeliveredNutritionBasisUnits) ||
                    pickup.Status == HouseholdReliefPickupStatus.Waiting &&
                        (pickup.DeliveredNutritionBasisUnits != 0 ||
                         pickup.DeliveredPhysicalQuantity != 0 ||
                         pickup.RemainingNutritionBasisUnits !=
                            pickup.RequestedNutritionBasisUnits ||
                         pickup.LastPickupDay != -1 ||
                         !string.IsNullOrEmpty(
                            pickup.LastCollectorPersonId) ||
                         pickup.InventoryTransactionIds.Count != 0) ||
                    pickup.Status ==
                        HouseholdReliefPickupStatus.PartiallyDelivered &&
                        (pickup.DeliveredNutritionBasisUnits <= 0 ||
                         pickup.DeliveredPhysicalQuantity <= 0 ||
                         pickup.RemainingNutritionBasisUnits <= 0) ||
                    pickup.Status == HouseholdReliefPickupStatus.Fulfilled &&
                        (pickup.DeliveredNutritionBasisUnits <
                            pickup.RequestedNutritionBasisUnits ||
                         pickup.DeliveredPhysicalQuantity <= 0 ||
                         pickup.RemainingNutritionBasisUnits != 0) ||
                    pickup.Status != HouseholdReliefPickupStatus.Waiting &&
                        (pickup.LastPickupDay < pickup.SettlementDay ||
                         pickup.LastPickupDay > AbsoluteDay ||
                         !people.Contains(pickup.LastCollectorPersonId) ||
                         !family.MemberIds.Contains(
                            pickup.LastCollectorPersonId) ||
                         pickup.InventoryTransactionIds.Count == 0))
                {
                    throw new InvalidOperationException(
                        $"Invalid household relief pickup {pickup.Id}.");
                }

                long deliveredPhysical = 0;
                for (var transactionIndex = 0;
                     transactionIndex < pickup.InventoryTransactionIds.Count;
                     transactionIndex++)
                {
                    var transactionId =
                        pickup.InventoryTransactionIds[transactionIndex];
                    if (!referencedTransactions.Add(transactionId) ||
                        !transactions.TryGetValue(
                            transactionId, out var transaction) ||
                        transaction.Type != InventoryTransactionType
                            .FoodVillageReliefTransferred ||
                        transaction.SourceVillageId != pickup.VillageId ||
                        transaction.Day < pickup.SettlementDay ||
                        transaction.Day > pickup.LastPickupDay ||
                        !family.MemberIds.Contains(transaction.ActorPersonId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid household relief inventory transaction {transactionId}.");
                    }
                    for (var lineIndex = 0;
                         lineIndex < transaction.Lines.Count;
                         lineIndex++)
                    {
                        var line = transaction.Lines[lineIndex];
                        if (line.OwnerFamilyId == pickup.FamilyId &&
                            line.QuantityDelta > 0)
                        {
                            deliveredPhysical = checked(
                                deliveredPhysical + line.QuantityDelta);
                        }
                    }
                }
                if (deliveredPhysical != pickup.DeliveredPhysicalQuantity)
                {
                    throw new InvalidOperationException(
                        $"Household relief pickup {pickup.Id} has inconsistent delivered stock.");
                }
            }
        }

        private void ValidateHouseholdReliefConsumptions()
        {
            var pickups = new Dictionary<string, HouseholdReliefPickupState>(
                StringComparer.Ordinal);
            var families = new Dictionary<string, FamilyState>(
                StringComparer.Ordinal);
            var villages = new Dictionary<string, VillageState>(
                StringComparer.Ordinal);
            var people = new HashSet<string>(StringComparer.Ordinal);
            var transactions = new Dictionary<string, InventoryTransactionState>(
                StringComparer.Ordinal);
            var batches = new Dictionary<string, ProductBatchState>(
                StringComparer.Ordinal);
            for (var i = 0; i < HouseholdReliefPickups.Count; i++)
                pickups.Add(HouseholdReliefPickups[i].Id,
                    HouseholdReliefPickups[i]);
            for (var i = 0; i < Families.Count; i++)
                families.Add(Families[i].Id, Families[i]);
            for (var i = 0; i < Villages.Count; i++)
                villages.Add(Villages[i].Id, Villages[i]);
            for (var i = 0; i < People.Count; i++)
                people.Add(People[i].Id);
            for (var i = 0; i < InventoryTransactions.Count; i++)
                transactions.Add(InventoryTransactions[i].Id,
                    InventoryTransactions[i]);
            for (var i = 0; i < ProductBatches.Count; i++)
                batches.Add(ProductBatches[i].Id, ProductBatches[i]);

            var consumptionIds = new HashSet<string>(StringComparer.Ordinal);
            var pickupIds = new HashSet<string>(StringComparer.Ordinal);
            var referencedTransactions = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < HouseholdReliefConsumptions.Count; i++)
            {
                var consumption = HouseholdReliefConsumptions[i] ??
                    throw new InvalidOperationException(
                        "A household relief consumption cannot be null.");
                _ = new StableId(consumption.Id);
                var usesIndividualAllocation =
                    consumption.AllocationPolicyId ==
                    HouseholdReliefAllocationPolicyIds
                        .ProportionalIndividualNeed;
                var usesLegacyAllocation =
                    consumption.AllocationPolicyId ==
                    HouseholdReliefAllocationPolicyIds
                        .LegacyHouseholdShared;
                var usesCareDelivery =
                    consumption.CareDeliveryPolicyId ==
                    HouseholdReliefCareDeliveryPolicyIds
                        .AgeHealthDependency;
                var usesLegacyCare =
                    consumption.CareDeliveryPolicyId ==
                    HouseholdReliefCareDeliveryPolicyIds
                        .LegacySelfService;
                if (!consumptionIds.Add(consumption.Id) ||
                    !pickupIds.Add(consumption.PickupId) ||
                    !Enum.IsDefined(
                        typeof(HouseholdReliefConsumptionStatus),
                        consumption.Status) ||
                    !pickups.TryGetValue(
                        consumption.PickupId, out var pickup) ||
                    pickup.SourceShortfallEventId !=
                        consumption.SourceShortfallEventId ||
                    pickup.VillageId != consumption.VillageId ||
                    pickup.FamilyId != consumption.FamilyId ||
                    pickup.SettlementDay != consumption.SettlementDay ||
                    pickup.RequestedNutritionBasisUnits !=
                        consumption.RequestedNutritionBasisUnits ||
                    !families.TryGetValue(
                        consumption.FamilyId, out var family) ||
                    !villages.ContainsKey(consumption.VillageId) ||
                    (!usesIndividualAllocation && !usesLegacyAllocation) ||
                    (!usesCareDelivery && !usesLegacyCare) ||
                    consumption.RequestedNutritionBasisUnits <= 0 ||
                    consumption.ConsumedNutritionBasisUnits < 0 ||
                    usesIndividualAllocation &&
                        consumption.PreparedNutritionBasisUnits < 0 ||
                    usesLegacyAllocation &&
                        consumption.PreparedNutritionBasisUnits != -1 ||
                    consumption.ConsumedPhysicalQuantity < 0 ||
                    consumption.ConsumedNutritionBasisUnits >
                        pickup.DeliveredNutritionBasisUnits ||
                    consumption.Status ==
                        HouseholdReliefConsumptionStatus.Waiting &&
                        (consumption.ConsumedNutritionBasisUnits != 0 ||
                         consumption.ConsumedPhysicalQuantity != 0 ||
                         consumption.RemainingNutritionBasisUnits !=
                            consumption.RequestedNutritionBasisUnits ||
                         consumption.LastConsumptionDay != -1 ||
                         !string.IsNullOrEmpty(
                            consumption.LastConsumerPersonId) ||
                         consumption.InventoryTransactionIds.Count != 0) ||
                    consumption.Status ==
                        HouseholdReliefConsumptionStatus.PartiallyConsumed &&
                        (consumption.ConsumedNutritionBasisUnits <= 0 ||
                         consumption.ConsumedPhysicalQuantity <= 0 ||
                         consumption.RemainingNutritionBasisUnits <= 0) ||
                    consumption.Status ==
                        HouseholdReliefConsumptionStatus.Fulfilled &&
                        (consumption.ConsumedNutritionBasisUnits <
                            consumption.RequestedNutritionBasisUnits ||
                         consumption.ConsumedPhysicalQuantity <= 0 ||
                         consumption.RemainingNutritionBasisUnits != 0) ||
                    consumption.Status !=
                        HouseholdReliefConsumptionStatus.Waiting &&
                        (consumption.LastConsumptionDay <
                            consumption.SettlementDay ||
                         consumption.LastConsumptionDay > AbsoluteDay ||
                         !people.Contains(
                            consumption.LastConsumerPersonId) ||
                         !family.MemberIds.Contains(
                            consumption.LastConsumerPersonId) ||
                         consumption.InventoryTransactionIds.Count == 0) ||
                    consumption.AffectedPeople == null ||
                    consumption.AffectedPeople.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid household relief consumption {consumption.Id}.");
                }

                var affectedIds = new HashSet<string>(StringComparer.Ordinal);
                long allocatedNutrition = 0;
                long affectedConsumedNutrition = 0;
                long individualRemainingNutrition = 0;
                long affectedRequiredNutrition = 0;
                var vulnerableAffectedPeople = 0;
                for (var affectedIndex = 0;
                     affectedIndex < consumption.AffectedPeople.Count;
                     affectedIndex++)
                {
                    var affected = consumption.AffectedPeople[affectedIndex] ??
                        throw new InvalidOperationException(
                            "A household relief affected person cannot be null.");
                    if (!affectedIds.Add(affected.PersonId) ||
                        !people.Contains(affected.PersonId) ||
                        !family.MemberIds.Contains(affected.PersonId) ||
                        usesIndividualAllocation &&
                            (affected.RequiredNutritionBasisUnits <= 0 ||
                             affected.AllocatedNutritionBasisUnits < 0 ||
                             affected.ConsumedNutritionBasisUnits < 0) ||
                        usesLegacyAllocation &&
                            (affected.RequiredNutritionBasisUnits != -1 ||
                             affected.AllocatedNutritionBasisUnits != -1 ||
                             affected.ConsumedNutritionBasisUnits != -1) ||
                        affected.AppliedHealthDamageBasisPoints < 0 ||
                        affected.AppliedLivelihoodPressureBasisPoints < 0 ||
                        affected.RecoveredHealthBasisPoints < 0 ||
                        affected.RecoveredHealthBasisPoints >
                            affected.AppliedHealthDamageBasisPoints ||
                        affected.RecoveredLivelihoodBasisPoints < 0 ||
                        affected.RecoveredLivelihoodBasisPoints >
                            affected.AppliedLivelihoodPressureBasisPoints ||
                        usesLegacyCare &&
                            affected.RequiresCaregiverDelivery)
                    {
                        throw new InvalidOperationException(
                            $"Invalid affected person in {consumption.Id}.");
                    }
                    if (usesIndividualAllocation)
                    {
                        affectedRequiredNutrition = checked(
                            affectedRequiredNutrition +
                            affected.RequiredNutritionBasisUnits);
                        if (affected.RequiredNutritionBasisUnits == 20_000)
                        {
                            vulnerableAffectedPeople++;
                        }
                        allocatedNutrition = checked(
                            allocatedNutrition +
                            affected.AllocatedNutritionBasisUnits);
                        affectedConsumedNutrition = checked(
                            affectedConsumedNutrition +
                            affected.ConsumedNutritionBasisUnits);
                        individualRemainingNutrition = checked(
                            individualRemainingNutrition + Math.Max(
                                0L,
                                affected.AllocatedNutritionBasisUnits -
                                affected.ConsumedNutritionBasisUnits));
                    }
                }
                var expectedRemainingNutrition = usesIndividualAllocation
                    ? individualRemainingNutrition
                    : Math.Max(
                        0L,
                        consumption.RequestedNutritionBasisUnits -
                        consumption.ConsumedNutritionBasisUnits);
                if (consumption.RemainingNutritionBasisUnits !=
                        expectedRemainingNutrition ||
                    usesIndividualAllocation &&
                        (allocatedNutrition !=
                            consumption.RequestedNutritionBasisUnits ||
                         affectedConsumedNutrition +
                            consumption.PreparedNutritionBasisUnits !=
                                consumption.ConsumedNutritionBasisUnits))
                {
                    throw new InvalidOperationException(
                        $"Household relief allocation {consumption.Id} does not close.");
                }
                if (usesCareDelivery &&
                    consumption.Status !=
                        HouseholdReliefConsumptionStatus.Waiting &&
                    !affectedIds.Contains(consumption.LastConsumerPersonId))
                {
                    throw new InvalidOperationException(
                        $"Household relief consumer {consumption.LastConsumerPersonId} is not affected by {consumption.Id}.");
                }
                if (pickup.PriorityPolicyId ==
                        HouseholdReliefPriorityPolicyIds
                            .NeedSeverityVulnerability)
                {
                    if (!usesIndividualAllocation ||
                        affectedRequiredNutrition <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Household relief priority snapshot {pickup.Id} has no individual basis.");
                    }
                    var expectedSeverity = checked((int)Math.Max(
                        1L,
                        Math.Min(
                            10_000L,
                            pickup.RequestedNutritionBasisUnits * 10_000L /
                            affectedRequiredNutrition)));
                    if (pickup.AffectedPersonCountAtAuthorization !=
                            consumption.AffectedPeople.Count ||
                        pickup.VulnerableAffectedPersonCount !=
                            vulnerableAffectedPeople ||
                        pickup.ShortfallSeverityBasisPoints != expectedSeverity)
                    {
                        throw new InvalidOperationException(
                            $"Household relief priority snapshot {pickup.Id} is inconsistent.");
                    }
                }

                long consumedPhysical = 0;
                var allowedPickupTransactions = new HashSet<string>(
                    pickup.InventoryTransactionIds, StringComparer.Ordinal);
                for (var transactionIndex = 0;
                     transactionIndex < consumption.InventoryTransactionIds.Count;
                     transactionIndex++)
                {
                    var transactionId =
                        consumption.InventoryTransactionIds[transactionIndex];
                    if (!referencedTransactions.Add(transactionId) ||
                        !transactions.TryGetValue(
                            transactionId, out var transaction) ||
                        transaction.Type != InventoryTransactionType.FoodConsumed ||
                        transaction.SourceHouseholdReliefConsumptionId !=
                            consumption.Id ||
                        transaction.Day < consumption.SettlementDay ||
                        transaction.Day > consumption.LastConsumptionDay ||
                        !family.MemberIds.Contains(transaction.ActorPersonId) ||
                        usesLegacyCare &&
                            !string.IsNullOrEmpty(
                                transaction.HouseholdReliefRecipientPersonId) ||
                        usesCareDelivery &&
                            !affectedIds.Contains(
                                transaction.HouseholdReliefRecipientPersonId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid household relief consumption transaction {transactionId}.");
                    }
                    if (usesCareDelivery)
                    {
                        var recipient = consumption.AffectedPeople.Find(item =>
                            item.PersonId == transaction
                                .HouseholdReliefRecipientPersonId);
                        if (recipient.RequiresCaregiverDelivery
                                ? transaction.ActorPersonId ==
                                    recipient.PersonId
                                : transaction.ActorPersonId !=
                                    recipient.PersonId)
                        {
                            throw new InvalidOperationException(
                                $"Invalid relief meal actor for transaction {transactionId}.");
                        }
                    }
                    for (var lineIndex = 0;
                         lineIndex < transaction.Lines.Count;
                         lineIndex++)
                    {
                        var line = transaction.Lines[lineIndex];
                        if (line.OwnerFamilyId == consumption.FamilyId &&
                            line.QuantityDelta < 0)
                        {
                            if (!batches.TryGetValue(
                                    line.BatchId, out var batch) ||
                                !allowedPickupTransactions.Contains(
                                    batch.SourceTransactionId))
                            {
                                throw new InvalidOperationException(
                                    $"Consumption {consumption.Id} used untraced food.");
                            }
                            consumedPhysical = checked(
                                consumedPhysical - line.QuantityDelta);
                        }
                    }
                }
                if (consumedPhysical != consumption.ConsumedPhysicalQuantity)
                {
                    throw new InvalidOperationException(
                        $"Household relief consumption {consumption.Id} has inconsistent physical stock.");
                }
            }
        }

        private void ValidateHouseholdReliefCareDeliveries()
        {
            var claims = new Dictionary<
                string, HouseholdReliefConsumptionState>(
                StringComparer.Ordinal);
            var families = new Dictionary<string, FamilyState>(
                StringComparer.Ordinal);
            var people = new HashSet<string>(StringComparer.Ordinal);
            var transactions = new Dictionary<
                string, InventoryTransactionState>(
                StringComparer.Ordinal);
            for (var i = 0; i < HouseholdReliefConsumptions.Count; i++)
                claims.Add(
                    HouseholdReliefConsumptions[i].Id,
                    HouseholdReliefConsumptions[i]);
            for (var i = 0; i < Families.Count; i++)
                families.Add(Families[i].Id, Families[i]);
            for (var i = 0; i < People.Count; i++)
                people.Add(People[i].Id);
            for (var i = 0; i < InventoryTransactions.Count; i++)
                transactions.Add(
                    InventoryTransactions[i].Id,
                    InventoryTransactions[i]);

            var tracedTransactions = new HashSet<string>(
                StringComparer.Ordinal);
            var deliveredNutrition = new Dictionary<string, long>(
                StringComparer.Ordinal);
            for (var i = 0; i < HouseholdReliefCareDeliveries.Count; i++)
            {
                var delivery = HouseholdReliefCareDeliveries[i] ??
                    throw new InvalidOperationException(
                        "A household relief care delivery cannot be null.");
                _ = new StableId(delivery.Id);
                if (!claims.TryGetValue(
                        delivery.HouseholdReliefConsumptionId,
                        out var claim) ||
                    claim.CareDeliveryPolicyId !=
                        HouseholdReliefCareDeliveryPolicyIds
                            .AgeHealthDependency ||
                    !families.TryGetValue(claim.FamilyId, out var family) ||
                    delivery.Day < claim.SettlementDay ||
                    delivery.Day > AbsoluteDay ||
                    delivery.NutritionBasisUnits <= 0 ||
                    delivery.CaregiverPersonId == delivery.RecipientPersonId ||
                    !people.Contains(delivery.CaregiverPersonId) ||
                    !people.Contains(delivery.RecipientPersonId) ||
                    !family.MemberIds.Contains(delivery.CaregiverPersonId) ||
                    !family.MemberIds.Contains(delivery.RecipientPersonId))
                {
                    throw new InvalidOperationException(
                        $"Invalid household relief care delivery {delivery.Id}.");
                }

                var affected = claim.AffectedPeople.Find(item =>
                    item.PersonId == delivery.RecipientPersonId);
                if (affected == null || !affected.RequiresCaregiverDelivery)
                {
                    throw new InvalidOperationException(
                        $"Care delivery {delivery.Id} has no dependent recipient.");
                }

                if (delivery.SourceKindId ==
                    HouseholdReliefCareDeliverySourceIds
                        .TracedFoodTransaction)
                {
                    if (string.IsNullOrEmpty(
                            delivery.SourceInventoryTransactionId) ||
                        !tracedTransactions.Add(
                            delivery.SourceInventoryTransactionId) ||
                        !transactions.TryGetValue(
                            delivery.SourceInventoryTransactionId,
                            out var transaction) ||
                        transaction.Type !=
                            InventoryTransactionType.FoodConsumed ||
                        transaction.SourceHouseholdReliefConsumptionId !=
                            claim.Id ||
                        transaction.HouseholdReliefRecipientPersonId !=
                            delivery.RecipientPersonId ||
                        transaction.ActorPersonId !=
                            delivery.CaregiverPersonId ||
                        transaction.Day != delivery.Day)
                    {
                        throw new InvalidOperationException(
                            $"Care delivery {delivery.Id} has invalid traced food provenance.");
                    }
                }
                else if (delivery.SourceKindId ==
                    HouseholdReliefCareDeliverySourceIds.PreparedNutrition)
                {
                    if (!string.IsNullOrEmpty(
                            delivery.SourceInventoryTransactionId))
                    {
                        throw new InvalidOperationException(
                            $"Prepared care delivery {delivery.Id} cannot reference inventory.");
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Care delivery {delivery.Id} has an unknown source.");
                }

                var key = claim.Id + "|" + delivery.RecipientPersonId;
                deliveredNutrition.TryGetValue(key, out var delivered);
                deliveredNutrition[key] = checked(
                    delivered + delivery.NutritionBasisUnits);
            }

            for (var claimIndex = 0;
                 claimIndex < HouseholdReliefConsumptions.Count;
                 claimIndex++)
            {
                var claim = HouseholdReliefConsumptions[claimIndex];
                for (var affectedIndex = 0;
                     affectedIndex < claim.AffectedPeople.Count;
                     affectedIndex++)
                {
                    var affected = claim.AffectedPeople[affectedIndex];
                    var key = claim.Id + "|" + affected.PersonId;
                    deliveredNutrition.TryGetValue(key, out var delivered);
                    if (claim.CareDeliveryPolicyId ==
                            HouseholdReliefCareDeliveryPolicyIds
                                .AgeHealthDependency &&
                        affected.RequiresCaregiverDelivery
                            ? delivered !=
                                affected.ConsumedNutritionBasisUnits
                            : delivered != 0)
                    {
                        throw new InvalidOperationException(
                            $"Care delivery nutrition does not close for {key}.");
                    }
                }
            }
        }

        private void ValidateLongTermNutrition()
        {
            var people = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < People.Count; i++)
                people.Add(People[i].Id);

            var profiles = new Dictionary<
                string, PersonNutritionProfileState>(StringComparer.Ordinal);
            for (var i = 0; i < PersonNutritionProfiles.Count; i++)
            {
                var profile = PersonNutritionProfiles[i] ??
                    throw new InvalidOperationException(
                        "A person nutrition profile cannot be null.");
                _ = new StableId(profile.Id);
                if (!people.Contains(profile.PersonId) ||
                    profile.PolicyId !=
                        NutritionPolicyIds.LongitudinalHouseholdNutrition ||
                    profile.FirstObservedDay < 0 ||
                    profile.LastUpdatedDay < profile.FirstObservedDay ||
                    profile.LastUpdatedDay > AbsoluteDay ||
                    profile.ReferenceMonthlyNutritionBasisUnits <= 0 ||
                    profile.NutritionDebtBasisUnits < 0 ||
                    profile.DiseaseRiskBasisPoints < 0 ||
                    profile.DiseaseRiskBasisPoints > 10_000 ||
                    profile.ConsecutiveDeficitMonths < 0 ||
                    profile.ConsecutiveAdequateMonths < 0 ||
                    profile.ConsecutiveDeficitMonths > 0 &&
                        profile.ConsecutiveAdequateMonths > 0 ||
                    profiles.ContainsKey(profile.PersonId))
                {
                    throw new InvalidOperationException(
                        $"Invalid person nutrition profile {profile.Id}.");
                }
                profiles.Add(profile.PersonId, profile);
            }

            var episodes = new Dictionary<
                string, NutritionConditionEpisodeState>(StringComparer.Ordinal);
            for (var i = 0; i < NutritionConditionEpisodes.Count; i++)
            {
                var episode = NutritionConditionEpisodes[i] ??
                    throw new InvalidOperationException(
                        "A nutrition condition episode cannot be null.");
                _ = new StableId(episode.Id);
                if (!profiles.ContainsKey(episode.PersonId) ||
                    episode.PolicyId !=
                        NutritionPolicyIds.LongitudinalHouseholdNutrition ||
                    episode.ConditionId !=
                        NutritionConditionIds.MalnutritionIllness ||
                    episode.StartDay < 0 ||
                    episode.LastEvaluatedDay < episode.StartDay ||
                    episode.LastEvaluatedDay > AbsoluteDay ||
                    episode.EndDay != -1 &&
                        (episode.EndDay < episode.StartDay ||
                         episode.EndDay > episode.LastEvaluatedDay) ||
                    episode.PeakDiseaseRiskBasisPoints <
                        LongTermNutritionRules
                            .IllnessRiskThresholdBasisPoints ||
                    episode.PeakDiseaseRiskBasisPoints > 10_000 ||
                    episode.AppliedHealthDamageBasisPoints < 0 ||
                    episode.RecoveredHealthBasisPoints < 0 ||
                    episode.RecoveredHealthBasisPoints >
                        episode.AppliedHealthDamageBasisPoints ||
                    episodes.ContainsKey(episode.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid nutrition condition episode {episode.Id}.");
                }
                episodes.Add(episode.Id, episode);
            }

            var claims = new Dictionary<
                string, HouseholdReliefConsumptionState>(StringComparer.Ordinal);
            for (var i = 0; i < HouseholdReliefConsumptions.Count; i++)
                claims.Add(HouseholdReliefConsumptions[i].Id,
                    HouseholdReliefConsumptions[i]);
            var transactions = new Dictionary<
                string, InventoryTransactionState>(StringComparer.Ordinal);
            for (var i = 0; i < InventoryTransactions.Count; i++)
                transactions.Add(InventoryTransactions[i].Id,
                    InventoryTransactions[i]);

            var previous = new Dictionary<
                string, PersonNutritionLedgerEntryState>(StringComparer.Ordinal);
            var appliedByEpisode = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var recoveredByEpisode = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var lastEpisodeDay = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var creditedByClaimPerson = new Dictionary<string, long>(
                StringComparer.Ordinal);
            for (var i = 0; i < PersonNutritionLedgerEntries.Count; i++)
            {
                var entry = PersonNutritionLedgerEntries[i] ??
                    throw new InvalidOperationException(
                        "A person nutrition ledger entry cannot be null.");
                _ = new StableId(entry.Id);
                if (!profiles.TryGetValue(entry.PersonId, out var profile) ||
                    entry.PolicyId != profile.PolicyId ||
                    !Enum.IsDefined(typeof(NutritionLedgerEntryKind),
                        entry.Kind) ||
                    entry.Day < profile.FirstObservedDay ||
                    entry.Day > AbsoluteDay ||
                    entry.ReferenceMonthlyNutritionBasisUnits <= 0 ||
                    entry.NutritionBasisUnits < 0 ||
                    entry.OpeningNutritionDebtBasisUnits < 0 ||
                    entry.ClosingNutritionDebtBasisUnits < 0 ||
                    entry.OpeningDiseaseRiskBasisPoints < 0 ||
                    entry.OpeningDiseaseRiskBasisPoints > 10_000 ||
                    entry.ClosingDiseaseRiskBasisPoints < 0 ||
                    entry.ClosingDiseaseRiskBasisPoints > 10_000 ||
                    entry.OpeningConsecutiveDeficitMonths < 0 ||
                    entry.ClosingConsecutiveDeficitMonths < 0 ||
                    entry.OpeningConsecutiveAdequateMonths < 0 ||
                    entry.ClosingConsecutiveAdequateMonths < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid person nutrition ledger entry {entry.Id}.");
                }

                previous.TryGetValue(entry.PersonId, out var prior);
                if (prior == null
                        ? entry.OpeningNutritionDebtBasisUnits != 0 ||
                          entry.OpeningDiseaseRiskBasisPoints != 0 ||
                          entry.OpeningConsecutiveDeficitMonths != 0 ||
                          entry.OpeningConsecutiveAdequateMonths != 0
                        : entry.Day < prior.Day ||
                          entry.OpeningNutritionDebtBasisUnits !=
                              prior.ClosingNutritionDebtBasisUnits ||
                          entry.OpeningDiseaseRiskBasisPoints !=
                              prior.ClosingDiseaseRiskBasisPoints ||
                          entry.OpeningConsecutiveDeficitMonths !=
                              prior.ClosingConsecutiveDeficitMonths ||
                          entry.OpeningConsecutiveAdequateMonths !=
                              prior.ClosingConsecutiveAdequateMonths)
                {
                    throw new InvalidOperationException(
                        $"Nutrition ledger opening does not close for {entry.Id}.");
                }

                if (entry.Kind == NutritionLedgerEntryKind.MonthlyDeficit)
                {
                    if (entry.NutritionBasisUnits <= 0 ||
                        entry.ClosingNutritionDebtBasisUnits != checked(
                            entry.OpeningNutritionDebtBasisUnits +
                            entry.NutritionBasisUnits) ||
                        entry.ClosingConsecutiveDeficitMonths !=
                            entry.OpeningConsecutiveDeficitMonths + 1 ||
                        entry.ClosingConsecutiveAdequateMonths != 0 ||
                        !string.IsNullOrEmpty(
                            entry.SourceHouseholdReliefConsumptionId) ||
                        !string.IsNullOrEmpty(
                            entry.SourceInventoryTransactionId) ||
                        entry.HealthBasisPointsDelta > 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid monthly nutrition deficit {entry.Id}.");
                    }
                }
                else if (entry.Kind ==
                    NutritionLedgerEntryKind.MonthlyRecovery)
                {
                    if (entry.NutritionBasisUnits >
                            entry.OpeningNutritionDebtBasisUnits ||
                        entry.ClosingNutritionDebtBasisUnits !=
                            entry.OpeningNutritionDebtBasisUnits -
                            entry.NutritionBasisUnits ||
                        entry.ClosingConsecutiveDeficitMonths != 0 ||
                        entry.ClosingConsecutiveAdequateMonths !=
                            entry.OpeningConsecutiveAdequateMonths + 1 ||
                        !string.IsNullOrEmpty(
                            entry.SourceHouseholdReliefConsumptionId) ||
                        !string.IsNullOrEmpty(
                            entry.SourceInventoryTransactionId) ||
                        entry.HealthBasisPointsDelta < 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid monthly nutrition recovery {entry.Id}.");
                    }
                }
                else
                {
                    if (entry.NutritionBasisUnits <= 0 ||
                        entry.NutritionBasisUnits >
                            entry.OpeningNutritionDebtBasisUnits ||
                        entry.ClosingNutritionDebtBasisUnits !=
                            entry.OpeningNutritionDebtBasisUnits -
                            entry.NutritionBasisUnits ||
                        entry.ClosingConsecutiveDeficitMonths !=
                            (entry.ClosingNutritionDebtBasisUnits == 0
                                ? 0
                                : entry.OpeningConsecutiveDeficitMonths) ||
                        entry.ClosingConsecutiveAdequateMonths !=
                            entry.OpeningConsecutiveAdequateMonths ||
                        entry.HealthBasisPointsDelta != 0 ||
                        string.IsNullOrEmpty(
                            entry.SourceHouseholdReliefConsumptionId) ||
                        !claims.TryGetValue(
                            entry.SourceHouseholdReliefConsumptionId,
                            out var claim) ||
                        !claim.AffectedPeople.Exists(item =>
                            item.PersonId == entry.PersonId) ||
                        entry.Day < claim.SettlementDay)
                    {
                        throw new InvalidOperationException(
                            $"Invalid relief nutrition credit {entry.Id}.");
                    }
                    if (!string.IsNullOrEmpty(
                            entry.SourceInventoryTransactionId) &&
                        (!transactions.TryGetValue(
                                entry.SourceInventoryTransactionId,
                                out var transaction) ||
                         transaction.SourceHouseholdReliefConsumptionId !=
                            claim.Id ||
                         transaction.HouseholdReliefRecipientPersonId !=
                            entry.PersonId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid nutrition credit inventory source {entry.Id}.");
                    }
                    var creditKey = claim.Id + "|" + entry.PersonId;
                    creditedByClaimPerson.TryGetValue(
                        creditKey, out var credited);
                    creditedByClaimPerson[creditKey] = checked(
                        credited + entry.NutritionBasisUnits);
                }

                var expectedRisk = LongTermNutritionRules
                    .CalculateDiseaseRiskBasisPoints(
                        entry.ClosingNutritionDebtBasisUnits,
                        entry.ReferenceMonthlyNutritionBasisUnits,
                        entry.ClosingConsecutiveDeficitMonths);
                if (entry.ClosingDiseaseRiskBasisPoints != expectedRisk)
                {
                    throw new InvalidOperationException(
                        $"Nutrition risk does not close for {entry.Id}.");
                }

                if (string.IsNullOrEmpty(entry.ConditionEpisodeId))
                {
                    if (entry.HealthBasisPointsDelta != 0)
                    {
                        throw new InvalidOperationException(
                            $"Nutrition health change lacks an episode in {entry.Id}.");
                    }
                }
                else
                {
                    if (!episodes.TryGetValue(
                            entry.ConditionEpisodeId, out var episode) ||
                        episode.PersonId != entry.PersonId ||
                        entry.Day < episode.StartDay ||
                        episode.EndDay != -1 && entry.Day > episode.EndDay)
                    {
                        throw new InvalidOperationException(
                            $"Invalid nutrition episode link in {entry.Id}.");
                    }
                    appliedByEpisode.TryGetValue(
                        episode.Id, out var applied);
                    recoveredByEpisode.TryGetValue(
                        episode.Id, out var recovered);
                    if (entry.HealthBasisPointsDelta < 0)
                        applied = checked(applied -
                            entry.HealthBasisPointsDelta);
                    else
                        recovered = checked(recovered +
                            entry.HealthBasisPointsDelta);
                    appliedByEpisode[episode.Id] = applied;
                    recoveredByEpisode[episode.Id] = recovered;
                    lastEpisodeDay[episode.Id] = entry.Day;
                }
                previous[entry.PersonId] = entry;
            }

            foreach (var pair in profiles)
            {
                var profile = pair.Value;
                if (!previous.TryGetValue(pair.Key, out var last) ||
                    profile.FirstObservedDay > last.Day ||
                    profile.LastUpdatedDay != last.Day ||
                    profile.ReferenceMonthlyNutritionBasisUnits !=
                        last.ReferenceMonthlyNutritionBasisUnits ||
                    profile.NutritionDebtBasisUnits !=
                        last.ClosingNutritionDebtBasisUnits ||
                    profile.DiseaseRiskBasisPoints !=
                        last.ClosingDiseaseRiskBasisPoints ||
                    profile.ConsecutiveDeficitMonths !=
                        last.ClosingConsecutiveDeficitMonths ||
                    profile.ConsecutiveAdequateMonths !=
                        last.ClosingConsecutiveAdequateMonths)
                {
                    throw new InvalidOperationException(
                        $"Nutrition profile {profile.Id} does not close to its ledger.");
                }
                if (!string.IsNullOrEmpty(profile.ActiveConditionEpisodeId) &&
                    (!episodes.TryGetValue(
                            profile.ActiveConditionEpisodeId,
                            out var activeEpisode) ||
                     activeEpisode.PersonId != profile.PersonId ||
                     activeEpisode.EndDay != -1))
                {
                    throw new InvalidOperationException(
                        $"Nutrition profile {profile.Id} has an invalid active episode.");
                }
            }

            foreach (var pair in episodes)
            {
                var episode = pair.Value;
                appliedByEpisode.TryGetValue(pair.Key, out var applied);
                recoveredByEpisode.TryGetValue(pair.Key, out var recovered);
                lastEpisodeDay.TryGetValue(pair.Key, out var lastDay);
                if (applied != episode.AppliedHealthDamageBasisPoints ||
                    recovered != episode.RecoveredHealthBasisPoints ||
                    lastDay != episode.LastEvaluatedDay ||
                    episode.EndDay == -1 !=
                        (profiles[episode.PersonId]
                            .ActiveConditionEpisodeId == episode.Id))
                {
                    throw new InvalidOperationException(
                        $"Nutrition condition episode {episode.Id} does not close.");
                }
            }

            foreach (var pair in creditedByClaimPerson)
            {
                var split = pair.Key.IndexOf('|');
                var claimId = pair.Key.Substring(0, split);
                var personId = pair.Key.Substring(split + 1);
                var affected = claims[claimId].AffectedPeople.Find(item =>
                    item.PersonId == personId);
                if (pair.Value > affected.ConsumedNutritionBasisUnits)
                {
                    throw new InvalidOperationException(
                        $"Nutrition relief credits exceed consumption for {pair.Key}.");
                }
            }
        }

        private void ValidateMilitaryMedicalCare()
        {
            if (MilitaryMedicalContractActivationDay < 0 ||
                MilitaryMedicalContractActivationDay > checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military medical contract activation day is invalid.");
            }
            if (MilitaryInjuryContractActivationDay < 0 ||
                MilitaryInjuryContractActivationDay > checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military injury contract activation day is invalid.");
            }
            if (MilitarySurgeryContractActivationDay < 0 ||
                MilitarySurgeryContractActivationDay > checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military surgery contract activation day is invalid.");
            }
            if (MilitaryMedicalTransferContractActivationDay < 0 ||
                MilitaryMedicalTransferContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military medical transfer contract activation day is invalid.");
            }
            if (MilitaryPostTreatmentTransferContractActivationDay < 0 ||
                MilitaryPostTreatmentTransferContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military post-treatment transfer contract activation day is invalid.");
            }
            if (MilitaryRepeatedMedicalTransferContractActivationDay < 0 ||
                MilitaryRepeatedMedicalTransferContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military repeated-transfer contract activation day is invalid.");
            }
            if (MilitaryWoundDeathContractActivationDay < 0 ||
                MilitaryWoundDeathContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military wound-death contract activation day is invalid.");
            }
            if (MilitaryMedicalDeathResponsibilityContractActivationDay < 0 ||
                MilitaryMedicalDeathResponsibilityContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military medical death-responsibility contract activation day is invalid.");
            }
            if (MilitaryInpatientDeathContractActivationDay < 0 ||
                MilitaryInpatientDeathContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military inpatient-death contract activation day is invalid.");
            }
            if (MilitaryMedicalTransferDeathContractActivationDay < 0 ||
                MilitaryMedicalTransferDeathContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military medical-transfer death contract activation day is invalid.");
            }
            if (MilitaryOriginalEvacuationDeathContractActivationDay < 0 ||
                MilitaryOriginalEvacuationDeathContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military original-evacuation death contract activation day is invalid.");
            }
            if (MilitaryPatientReturnDeathContractActivationDay < 0 ||
                MilitaryPatientReturnDeathContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military patient-return death contract activation day is invalid.");
            }
            if (MilitaryPatientArrivalWaitingTeamDeathContractActivationDay < 0 ||
                MilitaryPatientArrivalWaitingTeamDeathContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military patient-arrival waiting-team death contract activation day is invalid.");
            }
            if (MilitaryReturnTeamDeathContractActivationDay < 0 ||
                MilitaryReturnTeamDeathContractActivationDay >
                    checked(AbsoluteDay + 1))
            {
                throw new InvalidOperationException(
                    "Military return-team death contract activation day is invalid.");
            }

            var armies = new Dictionary<string, ArmyState>(StringComparer.Ordinal);
            for (var i = 0; i < Armies.Count; i++)
                armies.Add(Armies[i].Id, Armies[i]);
            var people = new Dictionary<string, PersonState>(StringComparer.Ordinal);
            for (var i = 0; i < People.Count; i++)
                people.Add(People[i].Id, People[i]);
            var militaryServices =
                new Dictionary<string, MilitaryServiceState>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryServices.Count; i++)
                militaryServices.Add(MilitaryServices[i].Id, MilitaryServices[i]);
            var containers =
                new Dictionary<string, InventoryContainerState>(StringComparer.Ordinal);
            for (var i = 0; i < InventoryContainers.Count; i++)
                containers.Add(InventoryContainers[i].Id, InventoryContainers[i]);
            var batches =
                new Dictionary<string, ProductBatchState>(StringComparer.Ordinal);
            for (var i = 0; i < ProductBatches.Count; i++)
                batches.Add(ProductBatches[i].Id, ProductBatches[i]);
            var transactions =
                new Dictionary<string, InventoryTransactionState>(StringComparer.Ordinal);
            for (var i = 0; i < InventoryTransactions.Count; i++)
                transactions.Add(InventoryTransactions[i].Id, InventoryTransactions[i]);

            if (MilitaryMedicalInitialized)
            {
                var assignedContainers = new HashSet<string>(StringComparer.Ordinal);
                foreach (var pair in armies)
                {
                    var army = pair.Value;
                    if (string.IsNullOrEmpty(army.MedicalInventoryContainerId) ||
                        !containers.TryGetValue(
                            army.MedicalInventoryContainerId,
                            out var container) ||
                        container.KindId !=
                            "inventory_container.military_medical_store" ||
                        container.OwnerOrganizationId != army.OrganizationId ||
                        !string.IsNullOrEmpty(container.OwnerFamilyId) ||
                        !string.IsNullOrEmpty(container.CarrierPersonId) ||
                        container.LocationId != army.LocationId ||
                        !assignedContainers.Add(container.Id))
                    {
                        throw new InvalidOperationException(
                            $"Invalid military medical inventory for {army.Id}.");
                    }
                }
            }
            else if (MilitaryMedicalCases.Count != 0 ||
                     MilitaryMedicalServices.Count != 0 ||
                     MilitaryMedicalEvacuations.Count != 0 ||
                     MilitaryRearMedicalSites.Count != 0 ||
                     MilitaryRearMedicalAdmissions.Count != 0 ||
                     MilitaryRearMedicalTreatments.Count != 0 ||
                     MilitaryMedicalTransfers.Count != 0 ||
                     MilitaryInjuryEpisodes.Count != 0 ||
                     MilitaryWoundDeaths.Count != 0 ||
                     MilitaryMedicalDeathResponsibilities.Count != 0 ||
                     MilitaryInpatientDeathClosures.Count != 0 ||
                     MilitaryMedicalTransferDeathClosures.Count != 0 ||
                     MilitaryOriginalEvacuationDeathClosures.Count != 0 ||
                     MilitaryPatientReturnDeathClosures.Count != 0 ||
                     MilitaryReturnTeamDeaths.Count != 0 ||
                     MilitaryFamilyInheritances.Count != 0 ||
                     MilitarySurvivorCompensations.Count != 0 ||
                     MilitaryFieldHospitalConstructionProjects.Count != 0 ||
                     MilitaryFieldHospitalConstructionWork.Count != 0 ||
                     MilitaryFieldHospitalMaintenance.Count != 0)
            {
                throw new InvalidOperationException(
                    "Military medical history requires initialized military medicine.");
            }

            var cases =
                new Dictionary<string, MilitaryMedicalCaseState>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryMedicalCases.Count; i++)
            {
                var medicalCase = MilitaryMedicalCases[i] ??
                    throw new InvalidOperationException(
                        "A military medical case cannot be null.");
                _ = new StableId(medicalCase.Id);
                _ = new StableId(medicalCase.TriageId);
                _ = new StableId(medicalCase.TreatmentProtocolId);
                _ = new StableId(medicalCase.AuthorizationPolicyId);
                var validTriage = medicalCase.TriageId ==
                        MilitaryMedicalTriageIds.Critical ||
                    medicalCase.TriageId == MilitaryMedicalTriageIds.Severe ||
                    medicalCase.TriageId == MilitaryMedicalTriageIds.Moderate;
                var hasArmy = armies.TryGetValue(
                    medicalCase.ArmyId, out var army);
                var hasMilitaryService = militaryServices.TryGetValue(
                    medicalCase.MilitaryServiceId, out var militaryService);
                if (!hasArmy || !hasMilitaryService || !validTriage ||
                    !people.ContainsKey(medicalCase.PatientPersonId) ||
                    !people.ContainsKey(medicalCase.PhysicianPersonId) ||
                    !people.ContainsKey(medicalCase.AuthorizingPersonId) ||
                    militaryService.ArmyId != army.Id ||
                    militaryService.PersonId != medicalCase.PatientPersonId ||
                    !HasCommanderService(
                        medicalCase.AuthorizingPersonId,
                        medicalCase.ArmyId,
                        militaryServices) ||
                    medicalCase.TreatmentProtocolId !=
                        MilitaryMedicalTreatmentProtocolIds.FieldHerbalCare ||
                    medicalCase.DiagnosedDay <
                        MilitaryMedicalContractActivationDay ||
                    medicalCase.DiagnosedDay > AbsoluteDay ||
                    medicalCase.Status != MilitaryMedicalCaseStatus.Closed ||
                    medicalCase.ClosedDay != medicalCase.DiagnosedDay ||
                    medicalCase.ClosureReasonId !=
                        MilitaryMedicalCaseClosureReasonIds.ReturnedToDuty ||
                    string.IsNullOrEmpty(
                        medicalCase.MilitaryMedicalServiceId) ||
                    !ValidMilitaryMedicalAuthorization(
                        medicalCase.AuthorizationPolicyId,
                        medicalCase.PhysicianPersonId,
                        medicalCase.ArmyId,
                        militaryServices))
                {
                    throw new InvalidOperationException(
                        $"Invalid military medical case {medicalCase.Id}.");
                }
                cases.Add(medicalCase.Id, medicalCase);
            }

            var serviceIds = new HashSet<string>(StringComparer.Ordinal);
            var workByPhysicianDay = new Dictionary<string, int>(
                StringComparer.Ordinal);
            for (var i = 0; i < CivilianMedicalServices.Count; i++)
            {
                var civilian = CivilianMedicalServices[i];
                AddInt(
                    workByPhysicianDay,
                    civilian.PhysicianPersonId + "|" + civilian.Day,
                    civilian.WorkMinutes);
            }
            for (var i = 0; i < MilitaryMedicalServices.Count; i++)
            {
                var service = MilitaryMedicalServices[i] ??
                    throw new InvalidOperationException(
                        "A military medical service cannot be null.");
                var hasCase = cases.TryGetValue(
                    service.MedicalCaseId, out var medicalCase);
                var hasMilitaryService = militaryServices.TryGetValue(
                    service.MilitaryServiceId, out var militaryService);
                var hasBatch = batches.TryGetValue(
                    service.SourceMedicineBatchId, out var batch);
                var hasTransaction = transactions.TryGetValue(
                    service.InventoryTransactionId, out var transaction);
                var hasArmy = armies.TryGetValue(service.ArmyId, out var army);
                if (!serviceIds.Add(service.Id) || !hasCase ||
                    !hasMilitaryService || !hasBatch || !hasTransaction ||
                    !hasArmy ||
                    medicalCase.MilitaryMedicalServiceId != service.Id ||
                    service.Day != medicalCase.DiagnosedDay ||
                    service.ArmyId != medicalCase.ArmyId ||
                    service.MilitaryServiceId != medicalCase.MilitaryServiceId ||
                    service.PatientPersonId != medicalCase.PatientPersonId ||
                    service.PhysicianPersonId != medicalCase.PhysicianPersonId ||
                    service.AuthorizingPersonId != medicalCase.AuthorizingPersonId ||
                    service.AuthorizationPolicyId !=
                        medicalCase.AuthorizationPolicyId ||
                    militaryService.ArmyId != service.ArmyId ||
                    militaryService.PersonId != service.PatientPersonId ||
                    service.VenuePolicyId !=
                        MilitaryMedicalVenuePolicyIds.ArmyFieldUnit ||
                    service.WorkMinutes !=
                        MilitaryMedicalRules.TreatmentWorkMinutes ||
                    service.MedicineProductDefinitionId !=
                        CoreProductionContent.HerbalMedicineMaterialProductId ||
                    batch.ProductDefinitionId !=
                        service.MedicineProductDefinitionId ||
                    batch.InventoryContainerId !=
                        army.MedicalInventoryContainerId ||
                    batch.OwnerOrganizationId != army.OrganizationId ||
                    service.MedicineUnitsConsumed !=
                        MilitaryMedicalRules.MedicineUnitsPerTreatment ||
                    service.OpeningHealthBasisPoints < 0 ||
                    service.OpeningHealthBasisPoints > 10_000 ||
                    service.ClosingHealthBasisPoints != Math.Max(
                        service.OpeningHealthBasisPoints,
                        MilitaryMedicalRules.ReturnToDutyHealthBasisPoints) ||
                    service.RecoveredHealthBasisPoints !=
                        service.ClosingHealthBasisPoints -
                        service.OpeningHealthBasisPoints ||
                    service.OpeningMilitaryStatus !=
                        MilitaryServiceStatus.Wounded ||
                    service.ClosingMilitaryStatus !=
                        MilitaryServiceStatus.Active ||
                    service.PhysicianMedicalSkillBeforeBasisPoints <
                        MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints ||
                    service.PhysicianMedicalSkillAfterBasisPoints != checked(
                        service.PhysicianMedicalSkillBeforeBasisPoints +
                        service.PhysicianMedicalSkillGainBasisPoints) ||
                    service.PhysicianMedicalSkillGainBasisPoints <= 0 ||
                    service.PhysicianMedicalSkillAfterBasisPoints > 10_000 ||
                    transaction.Type != InventoryTransactionType
                        .MilitaryMedicalTreatmentConsumed ||
                    transaction.SourceMilitaryMedicalServiceId != service.Id ||
                    transaction.ActorPersonId != service.PhysicianPersonId ||
                    transaction.Lines.Count != 1 ||
                    transaction.Lines[0].BatchId != batch.Id ||
                    transaction.Lines[0].QuantityDelta !=
                        -service.MedicineUnitsConsumed ||
                    transaction.Lines[0].ReservedQuantityDelta != 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid military medical service {service.Id}.");
                }
                AddInt(
                    workByPhysicianDay,
                    service.PhysicianPersonId + "|" + service.Day,
                    service.WorkMinutes);
            }

            foreach (var pair in cases)
            {
                if (!serviceIds.Contains(
                    pair.Value.MilitaryMedicalServiceId))
                {
                    throw new InvalidOperationException(
                        $"Military medical case {pair.Key} lacks its service.");
                }
            }
            var rearTreatmentIds = ValidateMilitaryRearMedicalCare(
                armies,
                people,
                militaryServices,
                containers,
                batches,
                transactions,
                workByPhysicianDay);
            var fieldHospitalProjectIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryFieldHospitalConstructionProjects.Count;
                 i++)
            {
                fieldHospitalProjectIds.Add(
                    MilitaryFieldHospitalConstructionProjects[i].Id);
            }
            var fieldHospitalMaintenanceIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryFieldHospitalMaintenance.Count; i++)
            {
                fieldHospitalMaintenanceIds.Add(
                    MilitaryFieldHospitalMaintenance[i].Id);
            }
            foreach (var pair in workByPhysicianDay)
            {
                if (pair.Value >
                    MilitaryMedicalRules.MaximumDailyPhysicianWorkMinutes)
                {
                    throw new InvalidOperationException(
                        $"Physician work exceeds the daily limit for {pair.Key}.");
                }
            }
            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                var transaction = InventoryTransactions[i];
                var formalMilitaryConsumption = transaction.Type ==
                    InventoryTransactionType.MilitaryMedicalTreatmentConsumed;
                var hasSource = !string.IsNullOrWhiteSpace(
                    transaction.SourceMilitaryMedicalServiceId);
                if (formalMilitaryConsumption != hasSource ||
                    hasSource && !serviceIds.Contains(
                        transaction.SourceMilitaryMedicalServiceId))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has an invalid " +
                        "military medical source.");
                }
                var rearConsumption = transaction.Type ==
                    InventoryTransactionType
                        .MilitaryRearMedicalTreatmentConsumed;
                var hasRearSource = !string.IsNullOrWhiteSpace(
                    transaction.SourceMilitaryRearMedicalTreatmentId);
                if (rearConsumption != hasRearSource ||
                    hasRearSource && !rearTreatmentIds.Contains(
                        transaction.SourceMilitaryRearMedicalTreatmentId))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has an invalid " +
                        "rear medical source.");
                }
                var transferReservation = transaction.Type ==
                        InventoryTransactionType
                            .MilitaryMedicalTransferMedicineReserved ||
                    transaction.Type == InventoryTransactionType
                        .MilitaryMedicalTransferMedicineReleased;
                var hasTransferSource = !string.IsNullOrWhiteSpace(
                    transaction.SourceMilitaryMedicalTransferId);
                if (transferReservation != hasTransferSource ||
                    hasTransferSource && !ContainsMilitaryMedicalTransfer(
                        transaction.SourceMilitaryMedicalTransferId))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has an invalid " +
                        "military medical transfer source.");
                }
                var constructionConsumption = transaction.Type ==
                    InventoryTransactionType
                        .MilitaryFieldHospitalConstructionConsumed;
                var hasConstructionSource = !string.IsNullOrWhiteSpace(
                    transaction
                        .SourceMilitaryFieldHospitalConstructionProjectId);
                if (constructionConsumption != hasConstructionSource ||
                    hasConstructionSource && !fieldHospitalProjectIds.Contains(
                        transaction
                            .SourceMilitaryFieldHospitalConstructionProjectId))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has an invalid " +
                        "field hospital construction source.");
                }
                var maintenanceConsumption = transaction.Type ==
                    InventoryTransactionType
                        .MilitaryFieldHospitalMaintenanceConsumed;
                var hasMaintenanceSource = !string.IsNullOrWhiteSpace(
                    transaction.SourceMilitaryFieldHospitalMaintenanceId);
                if (maintenanceConsumption != hasMaintenanceSource ||
                    hasMaintenanceSource &&
                    !fieldHospitalMaintenanceIds.Contains(
                        transaction.SourceMilitaryFieldHospitalMaintenanceId))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has an invalid " +
                        "field hospital maintenance source.");
                }
            }

            ValidateMilitaryMedicalEvacuations(
                armies, people, militaryServices);
        }

        private HashSet<string> ValidateMilitaryRearMedicalCare(
            Dictionary<string, ArmyState> armies,
            Dictionary<string, PersonState> people,
            Dictionary<string, MilitaryServiceState> militaryServices,
            Dictionary<string, InventoryContainerState> containers,
            Dictionary<string, ProductBatchState> batches,
            Dictionary<string, InventoryTransactionState> transactions,
            Dictionary<string, int> workByPhysicianDay)
        {
            var organizations = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Organizations.Count; i++)
            {
                organizations.Add(Organizations[i].Id);
            }
            var fieldHospitalProjects =
                ValidateMilitaryFieldHospitalConstruction(
                    armies,
                    people,
                    militaryServices,
                    organizations,
                    containers,
                    batches,
                    transactions);
            var sites = new Dictionary<string, MilitaryRearMedicalSiteState>(
                StringComparer.Ordinal);
            var siteContainers = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryRearMedicalSites.Count; i++)
            {
                var site = MilitaryRearMedicalSites[i] ??
                    throw new InvalidOperationException(
                        "A rear medical site cannot be null.");
                _ = new StableId(site.Id);
                _ = new StableId(site.KindId);
                var location = FindLocation(Locations, site.LocationId);
                var hasContainer = containers.TryGetValue(
                    site.MedicineInventoryContainerId, out var container);
                var existingClinic = site.KindId ==
                    MilitaryRearMedicalSiteKindIds.ExistingClinic;
                var fieldHospital = site.KindId ==
                    MilitaryRearMedicalSiteKindIds.FieldHospital;
                var validKindFacts = existingClinic
                    ? (location != null &&
                       (location.Features & LocationFeature.Clinic) != 0 &&
                       string.IsNullOrEmpty(site.SourceConstructionProjectId) &&
                       string.IsNullOrEmpty(site.SupportInventoryContainerId) &&
                       string.IsNullOrEmpty(site.MaintenancePolicyId) &&
                       site.LastMaintenanceDay == -1 &&
                       site.NextMaintenanceDay == -1)
                    : fieldHospital && location != null &&
                      fieldHospitalProjects.TryGetValue(
                          site.SourceConstructionProjectId,
                          out var sourceProject) &&
                      sourceProject.Status ==
                          MilitaryFieldHospitalConstructionStatus.Completed &&
                      sourceProject.RearMedicalSiteId == site.Id &&
                      sourceProject.LocationId == site.LocationId &&
                      sourceProject.OwnerOrganizationId ==
                          site.OwnerOrganizationId &&
                      site.SupportInventoryContainerId ==
                          sourceProject.MaterialInventoryContainerId &&
                      containers.ContainsKey(site.SupportInventoryContainerId) &&
                      site.MaintenancePolicyId ==
                          MilitaryFieldHospitalMaintenancePolicyIds
                              .TenDayTimberUpkeep &&
                      site.BedCapacity ==
                          MilitaryMedicalRules.FieldHospitalBedCapacity &&
                      site.LastMaintenanceDay >= site.RegisteredDay &&
                      site.NextMaintenanceDay == checked(
                          site.LastMaintenanceDay +
                          MilitaryMedicalRules
                              .FieldHospitalMaintenanceIntervalDays);
                if (!validKindFacts ||
                    !organizations.Contains(site.OwnerOrganizationId) ||
                    !hasContainer ||
                    container.KindId !=
                        "inventory_container.military_rear_medical_store" ||
                    container.OwnerOrganizationId != site.OwnerOrganizationId ||
                    !string.IsNullOrEmpty(container.OwnerFamilyId) ||
                    !string.IsNullOrEmpty(container.CarrierPersonId) ||
                    container.LocationId != site.LocationId ||
                    !siteContainers.Add(container.Id) ||
                    site.BedCapacity <= 0 ||
                    site.RegisteredDay < MilitaryMedicalContractActivationDay ||
                    site.RegisteredDay > AbsoluteDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid rear medical site {site.Id}.");
                }
                sites.Add(site.Id, site);
            }
            ValidateMilitaryFieldHospitalMaintenance(
                people,
                sites,
                fieldHospitalProjects,
                containers,
                batches,
                transactions);

            var evacuations = new Dictionary<
                string, MilitaryMedicalEvacuationState>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryMedicalEvacuations.Count; i++)
            {
                evacuations.Add(
                    MilitaryMedicalEvacuations[i].Id,
                    MilitaryMedicalEvacuations[i]);
            }
            var surgicalProcedures = new Dictionary<
                string, MilitarySurgicalProcedureDefinitionState>(
                    StringComparer.Ordinal);
            for (var i = 0; i < MilitarySurgicalProcedures.Count; i++)
            {
                var procedure = MilitarySurgicalProcedures[i] ??
                    throw new InvalidOperationException(
                        "A military surgical procedure cannot be null.");
                _ = new StableId(procedure.Id);
                if (string.IsNullOrWhiteSpace(procedure.DisplayName) ||
                    procedure.MinimumSeverityBasisPoints < 0 ||
                    procedure.MinimumSeverityBasisPoints > 10_000 ||
                    procedure.MinimumPhysicianSkillBasisPoints <
                        MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints ||
                    procedure.MinimumPhysicianSkillBasisPoints > 10_000 ||
                    procedure.WorkMinutes <= 0 ||
                    procedure.WorkMinutes >
                        MilitaryMedicalRules.MaximumDailyPhysicianWorkMinutes ||
                    procedure.MedicineUnits <= 0 ||
                    procedure.TargetHealthBasisPoints < 0 ||
                    procedure.TargetHealthBasisPoints > 10_000 ||
                    procedure.PermanentImpairmentSeverityBasisPoints <
                        procedure.MinimumSeverityBasisPoints ||
                    procedure.PermanentImpairmentSeverityBasisPoints > 10_000 ||
                    procedure.PermanentImpairmentLaborPenaltyBasisPoints <= 0 ||
                    procedure.PermanentImpairmentLaborPenaltyBasisPoints >
                        10_000 ||
                    surgicalProcedures.ContainsKey(procedure.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military surgical procedure {procedure.Id}.");
                }
                surgicalProcedures.Add(procedure.Id, procedure);
            }
            var injuryProfiles = new Dictionary<
                string, MilitaryInjuryProfileDefinitionState>(
                    StringComparer.Ordinal);
            for (var i = 0; i < MilitaryInjuryProfiles.Count; i++)
            {
                var profile = MilitaryInjuryProfiles[i] ??
                    throw new InvalidOperationException(
                        "A military injury profile cannot be null.");
                _ = new StableId(profile.Id);
                if (string.IsNullOrWhiteSpace(profile.DisplayName) ||
                    profile.MinimumAdmissionHealthBasisPoints < 0 ||
                    profile.MaximumAdmissionHealthBasisPoints > 10_000 ||
                    profile.MinimumAdmissionHealthBasisPoints >
                        profile.MaximumAdmissionHealthBasisPoints ||
                    !string.IsNullOrEmpty(profile.SurgicalProcedureId) &&
                        !surgicalProcedures.ContainsKey(
                            profile.SurgicalProcedureId) ||
                    injuryProfiles.ContainsKey(profile.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military injury profile {profile.Id}.");
                }
                injuryProfiles.Add(profile.Id, profile);
            }
            var woundDeathPolicies = new Dictionary<
                string, MilitaryWoundDeathPolicyDefinitionState>(
                    StringComparer.Ordinal);
            for (var i = 0; i < MilitaryWoundDeathPolicies.Count; i++)
            {
                var policy = MilitaryWoundDeathPolicies[i] ??
                    throw new InvalidOperationException(
                        "A military wound-death policy cannot be null.");
                _ = new StableId(policy.Id);
                if (string.IsNullOrWhiteSpace(policy.DisplayName) ||
                    policy.MinimumSeverityBasisPoints < 0 ||
                    policy.MinimumSeverityBasisPoints > 10_000 ||
                    policy.MaximumPostTreatmentHealthBasisPoints < 0 ||
                    policy.MaximumPostTreatmentHealthBasisPoints > 10_000 ||
                    policy.MinimumDaysAfterCareCompletion < 0 ||
                    policy.BaseCompensationMoney < 0 ||
                    policy.CompensationPerRankMoney < 0 ||
                    woundDeathPolicies.ContainsKey(policy.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military wound-death policy {policy.Id}.");
                }
                woundDeathPolicies.Add(policy.Id, policy);
            }
            var inpatientDeteriorationPolicies = new Dictionary<
                string, MilitaryInpatientDeteriorationPolicyDefinitionState>(
                    StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryInpatientDeteriorationPolicies.Count;
                 i++)
            {
                var policy = MilitaryInpatientDeteriorationPolicies[i] ??
                    throw new InvalidOperationException(
                        "A military inpatient deterioration policy cannot be null.");
                _ = new StableId(policy.Id);
                if (string.IsNullOrWhiteSpace(policy.DisplayName) ||
                    policy.MinimumSeverityBasisPoints < 0 ||
                    policy.MinimumSeverityBasisPoints > 10_000 ||
                    policy.MinimumDaysAfterAdmission < 0 ||
                    policy.HealthLossBasisPoints <= 0 ||
                    policy.HealthLossBasisPoints > 10_000 ||
                    policy.MaximumClosingHealthBasisPoints < 0 ||
                    policy.MaximumClosingHealthBasisPoints > 10_000 ||
                    inpatientDeteriorationPolicies.ContainsKey(policy.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military inpatient deterioration policy {policy.Id}.");
                }
                inpatientDeteriorationPolicies.Add(policy.Id, policy);
            }
            var originalEvacuationDeteriorationPolicies = new Dictionary<
                string,
                MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState>(
                    StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryOriginalEvacuationDeteriorationPolicies.Count;
                 i++)
            {
                var policy =
                    MilitaryOriginalEvacuationDeteriorationPolicies[i] ??
                    throw new InvalidOperationException(
                        "A military original-evacuation deterioration policy cannot be null.");
                _ = new StableId(policy.Id);
                if (string.IsNullOrWhiteSpace(policy.DisplayName) ||
                    policy.MinimumDaysAfterDispatch < 0 ||
                    policy.MaximumOpeningHealthBasisPoints < 0 ||
                    policy.MaximumOpeningHealthBasisPoints > 10_000 ||
                    policy.HealthLossBasisPoints <= 0 ||
                    policy.HealthLossBasisPoints > 10_000 ||
                    policy.MaximumClosingHealthBasisPoints < 0 ||
                    policy.MaximumClosingHealthBasisPoints > 10_000 ||
                    originalEvacuationDeteriorationPolicies.ContainsKey(
                        policy.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military original-evacuation deterioration policy {policy.Id}.");
                }
                originalEvacuationDeteriorationPolicies.Add(
                    policy.Id, policy);
            }
            var patientReturnDeteriorationPolicies = new Dictionary<
                string,
                MilitaryPatientReturnDeteriorationPolicyDefinitionState>(
                    StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryPatientReturnDeteriorationPolicies.Count;
                 i++)
            {
                var policy =
                    MilitaryPatientReturnDeteriorationPolicies[i] ??
                    throw new InvalidOperationException(
                        "A military patient-return deterioration policy cannot be null.");
                _ = new StableId(policy.Id);
                if (string.IsNullOrWhiteSpace(policy.DisplayName) ||
                    policy.MinimumSeverityBasisPoints < 0 ||
                    policy.MinimumSeverityBasisPoints > 10_000 ||
                    policy.MinimumDaysAfterReturnStart < 0 ||
                    policy.HealthLossBasisPoints <= 0 ||
                    policy.HealthLossBasisPoints > 10_000 ||
                    policy.MaximumClosingHealthBasisPoints < 0 ||
                    policy.MaximumClosingHealthBasisPoints > 10_000 ||
                    patientReturnDeteriorationPolicies.ContainsKey(
                        policy.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military patient-return deterioration policy {policy.Id}.");
                }
                patientReturnDeteriorationPolicies.Add(policy.Id, policy);
            }
            var returnTeamDeathPolicies = new Dictionary<
                string, MilitaryReturnTeamDeathPolicyDefinitionState>(
                    StringComparer.Ordinal);
            for (var i = 0; i < MilitaryReturnTeamDeathPolicies.Count; i++)
            {
                var policy = MilitaryReturnTeamDeathPolicies[i] ??
                    throw new InvalidOperationException(
                        "A military return-team death policy cannot be null.");
                _ = new StableId(policy.Id);
                if (string.IsNullOrWhiteSpace(policy.DisplayName) ||
                    policy.MinimumDaysAfterReturnStart < 0 ||
                    policy.HealthLossBasisPoints <= 0 ||
                    policy.HealthLossBasisPoints > 10_000 ||
                    policy.MaximumClosingHealthBasisPoints < 0 ||
                    policy.MaximumClosingHealthBasisPoints > 10_000 ||
                    policy.BaseCompensationMoney < 0 ||
                    policy.CompensationPerRankMoney < 0 ||
                    returnTeamDeathPolicies.ContainsKey(policy.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military return-team death policy {policy.Id}.");
                }
                returnTeamDeathPolicies.Add(policy.Id, policy);
            }
            var injuryEpisodes = new Dictionary<
                string, MilitaryInjuryEpisodeState>(StringComparer.Ordinal);
            var injuryAdmissions = new HashSet<string>(StringComparer.Ordinal);
            var permanentPenaltyByPerson = new Dictionary<string, int>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryInjuryEpisodes.Count; i++)
            {
                var injury = MilitaryInjuryEpisodes[i] ??
                    throw new InvalidOperationException(
                        "A military injury episode cannot be null.");
                _ = new StableId(injury.Id);
                _ = new StableId(injury.InjuryProfileId);
                var hasEvacuation = evacuations.TryGetValue(
                    injury.EvacuationId, out var evacuation);
                var hasService = militaryServices.TryGetValue(
                    injury.PatientMilitaryServiceId, out var service);
                var expectedSeverity = 10_000 -
                    injury.AdmissionHealthBasisPoints;
                var expectedTransit = hasEvacuation
                    ? checked((int)Math.Max(
                        0, evacuation.ReceivedDay - evacuation.CreatedDay))
                    : -1;
                var expectedContamination = expectedTransit < 0
                    ? -1
                    : Math.Min(
                        10_000,
                        checked(expectedSeverity / 2 +
                            expectedTransit * 400));
                var expectedProfile = MilitaryInjuryProfileCatalog.Select(
                    MilitaryInjuryProfiles,
                    injury.AdmissionHealthBasisPoints).Id;
                var profile = injuryProfiles.ContainsKey(expectedProfile)
                    ? injuryProfiles[expectedProfile]
                    : null;
                MilitarySurgicalProcedureDefinitionState procedure = null;
                var surgeryContractApplies = injury.AssessedDay >=
                    MilitarySurgeryContractActivationDay;
                var surgeryRequired = surgeryContractApplies &&
                    profile != null &&
                    !string.IsNullOrEmpty(profile.SurgicalProcedureId) &&
                    surgicalProcedures.TryGetValue(
                        profile.SurgicalProcedureId, out procedure) &&
                    injury.SeverityBasisPoints >=
                        procedure.MinimumSeverityBasisPoints;
                var expectedSurgicalProcedureId = surgeryRequired
                    ? procedure.Id
                    : string.Empty;
                var infected = injury.InfectionRiskBasisPoints >=
                    MilitaryMedicalRules.InfectionRiskThresholdBasisPoints;
                var controlled = injury.InfectionStatus ==
                    MilitaryInfectionStatus.Controlled;
                var surgeryCompleted = !string.IsNullOrEmpty(
                    injury.SurgeryTreatmentId);
                var expectedImpairment = surgeryCompleted &&
                    procedure != null &&
                    injury.SeverityBasisPoints >=
                        procedure.PermanentImpairmentSeverityBasisPoints;
                var expectedPenalty = expectedImpairment
                    ? procedure.PermanentImpairmentLaborPenaltyBasisPoints
                    : 0;
                if (injuryEpisodes.ContainsKey(injury.Id) ||
                    !injuryAdmissions.Add(injury.AdmissionId) ||
                    !hasEvacuation || !hasService ||
                    !people.ContainsKey(injury.PatientPersonId) ||
                    service.PersonId != injury.PatientPersonId ||
                    injury.PatientPersonId != evacuation.PatientPersonId ||
                    injury.PatientMilitaryServiceId !=
                        evacuation.PatientMilitaryServiceId ||
                    injury.AssessedDay < evacuation.ReceivedDay ||
                    injury.AssessedDay > AbsoluteDay ||
                    injury.AdmissionHealthBasisPoints < 0 ||
                    injury.AdmissionHealthBasisPoints > 10_000 ||
                    injury.SeverityBasisPoints != expectedSeverity ||
                    injury.TransitDays != expectedTransit ||
                    injury.ContaminationBasisPoints != expectedContamination ||
                    injury.InfectionRiskBasisPoints != expectedContamination ||
                    !injuryProfiles.ContainsKey(injury.InjuryProfileId) ||
                    injury.InjuryProfileId != expectedProfile ||
                    !Enum.IsDefined(
                        typeof(MilitaryInfectionStatus),
                        injury.InfectionStatus) ||
                    infected != (injury.InfectionStatus !=
                        MilitaryInfectionStatus.AtRisk) ||
                    controlled != !string.IsNullOrEmpty(
                        injury.InfectionControlTreatmentId) ||
                    controlled != (injury.InfectionControlledDay >= 0) ||
                    controlled && injury.InfectionControlledDay <
                        injury.AssessedDay ||
                    controlled && injury.InfectionControlledDay > AbsoluteDay ||
                    injury.SurgicalProcedureId !=
                        expectedSurgicalProcedureId ||
                    !surgeryRequired &&
                        (surgeryCompleted ||
                         injury.SurgeryCompletedDay != -1 ||
                         !string.IsNullOrEmpty(injury.PermanentOutcomeId) ||
                         injury.LaborCapacityBeforeBasisPoints != -1 ||
                         injury.LaborCapacityAfterBasisPoints != -1 ||
                         injury.PermanentLaborCapacityPenaltyBasisPoints != 0 ||
                         injury.RequiresMedicalRetirement) ||
                    surgeryRequired && !surgeryCompleted &&
                        (injury.SurgeryCompletedDay != -1 ||
                         !string.IsNullOrEmpty(injury.PermanentOutcomeId) ||
                         injury.LaborCapacityBeforeBasisPoints != -1 ||
                         injury.LaborCapacityAfterBasisPoints != -1 ||
                         injury.PermanentLaborCapacityPenaltyBasisPoints != 0 ||
                         injury.RequiresMedicalRetirement) ||
                    surgeryCompleted &&
                        (injury.SurgeryCompletedDay < injury.AssessedDay ||
                         injury.SurgeryCompletedDay > AbsoluteDay ||
                         injury.PermanentOutcomeId !=
                             (expectedImpairment
                                 ? MilitaryInjuryOutcomeIds
                                     .PermanentMobilityImpairment
                                 : MilitaryInjuryOutcomeIds
                                     .NoPermanentImpairment) ||
                         injury.LaborCapacityBeforeBasisPoints < 0 ||
                         injury.LaborCapacityBeforeBasisPoints > 10_000 ||
                         injury.LaborCapacityAfterBasisPoints != Math.Max(
                             0,
                             injury.LaborCapacityBeforeBasisPoints -
                                 expectedPenalty) ||
                         injury.PermanentLaborCapacityPenaltyBasisPoints !=
                             expectedPenalty ||
                         expectedImpairment !=
                             injury.RequiresMedicalRetirement))
                {
                    throw new InvalidOperationException(
                        $"Invalid military injury episode {injury.Id}.");
                }
                if (expectedPenalty > 0)
                {
                    AddInt(
                        permanentPenaltyByPerson,
                        injury.PatientPersonId,
                        expectedPenalty);
                }
                injuryEpisodes.Add(injury.Id, injury);
            }
            foreach (var pair in people)
            {
                permanentPenaltyByPerson.TryGetValue(
                    pair.Key, out var expectedPermanentPenalty);
                if (pair.Value.PermanentLaborCapacityPenaltyBasisPoints !=
                    expectedPermanentPenalty)
                {
                    throw new InvalidOperationException(
                        $"Person {pair.Key} has an invalid permanent labor penalty.");
                }
            }
            var admissions = new Dictionary<
                string, MilitaryRearMedicalAdmissionState>(StringComparer.Ordinal);
            var medicalTransfers = new Dictionary<
                string, MilitaryMedicalTransferState>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryMedicalTransfers.Count; i++)
            {
                var transfer = MilitaryMedicalTransfers[i] ??
                    throw new InvalidOperationException(
                        "A military medical transfer cannot be null.");
                if (medicalTransfers.ContainsKey(transfer.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate military medical transfer {transfer.Id}.");
                }
                medicalTransfers.Add(transfer.Id, transfer);
            }
            var occupiedBeds = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryRearMedicalAdmissions.Count; i++)
            {
                var admission = MilitaryRearMedicalAdmissions[i] ??
                    throw new InvalidOperationException(
                        "A rear medical admission cannot be null.");
                var hasEvacuation = evacuations.TryGetValue(
                    admission.EvacuationId, out var evacuation);
                var hasSite = sites.TryGetValue(
                    admission.RearMedicalSiteId, out var site);
                var hasPatient = people.ContainsKey(admission.PatientPersonId);
                var hasService = militaryServices.TryGetValue(
                    admission.PatientMilitaryServiceId, out var service);
                var hasPhysician = people.ContainsKey(
                    admission.PhysicianPersonId);
                MilitaryMedicalTransferState medicalTransfer = null;
                var hasMedicalTransfer = !string.IsNullOrEmpty(
                        admission.MedicalTransferId) &&
                    medicalTransfers.TryGetValue(
                        admission.MedicalTransferId, out medicalTransfer);
                var activeMedicalTransfer = hasMedicalTransfer &&
                    (medicalTransfer.Status ==
                         MilitaryMedicalTransferStatus.InTransit ||
                     medicalTransfer.Status ==
                         MilitaryMedicalTransferStatus.AwaitingReception ||
                     medicalTransfer.Status ==
                         MilitaryMedicalTransferStatus.DeceasedInTransit);
                var expectedPhysicianPersonId = hasMedicalTransfer
                    ? medicalTransfer.Status ==
                        MilitaryMedicalTransferStatus.Completed
                            ? medicalTransfer.DesignatedReceivingPersonId
                            : medicalTransfer.SourcePhysicianPersonId
                    : evacuation?.ReceivingPersonId;
                var expectedEvacuationStatus =
                    admission.Status ==
                        MilitaryRearMedicalAdmissionStatus.InTreatment
                        ? MilitaryMedicalEvacuationStatus.Admitted
                        : admission.Status ==
                            MilitaryRearMedicalAdmissionStatus.ReadyForReturn
                            ? MilitaryMedicalEvacuationStatus.ReadyForReturn
                            : admission.Status ==
                                MilitaryRearMedicalAdmissionStatus.Discharged
                                ? MilitaryMedicalEvacuationStatus.ReturningToArmy
                                : MilitaryMedicalEvacuationStatus.Completed;
                var hasInjury = injuryEpisodes.TryGetValue(
                    admission.InjuryEpisodeId, out var injury);
                var woundDeath = FindMilitaryWoundDeath(
                    admission.Id, admission.PatientPersonId);
                var diedDuringTreatment = woundDeath != null &&
                    woundDeath.DeathContextId ==
                        MilitaryWoundDeathContextIds.InTreatmentAtCareSite;
                var diedDuringTransfer = woundDeath != null &&
                    woundDeath.DeathContextId ==
                        MilitaryWoundDeathContextIds
                            .DuringCrossFacilityTransfer;
                var diedDuringPatientReturn = woundDeath != null &&
                    (woundDeath.DeathContextId ==
                         MilitaryWoundDeathContextIds
                             .DuringPatientReturnJourney ||
                     woundDeath.DeathContextId ==
                         MilitaryWoundDeathContextIds
                             .AwaitingReturnTeamRejoinAtArmy);
                var diedAwaitingReturnTeam = woundDeath != null &&
                    woundDeath.DeathContextId ==
                        MilitaryWoundDeathContextIds
                            .AwaitingReturnTeamRejoinAtArmy;
                var diedAtCareSite = woundDeath != null &&
                    woundDeath.DeathContextId ==
                        MilitaryWoundDeathContextIds.ReadyForReturnAtCareSite ||
                    diedDuringTreatment;
                var validEvacuationStatus =
                    diedDuringTreatment && admission.Status ==
                        MilitaryRearMedicalAdmissionStatus.Discharged &&
                        evacuation != null && evacuation.Status ==
                            MilitaryMedicalEvacuationStatus.ReadyForReturn ||
                    diedDuringTransfer && admission.Status ==
                        MilitaryRearMedicalAdmissionStatus.Discharged &&
                        evacuation != null &&
                        (medicalTransfer.Status ==
                             MilitaryMedicalTransferStatus
                                 .DeceasedInTransit &&
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.Admitted ||
                         medicalTransfer.Status ==
                             MilitaryMedicalTransferStatus
                                 .ClosedAfterPatientDeath &&
                         evacuation.Status >=
                             MilitaryMedicalEvacuationStatus
                                 .ReadyForReturn) ||
                    diedDuringPatientReturn &&
                        admission.Status ==
                            MilitaryRearMedicalAdmissionStatus.Discharged &&
                        evacuation != null && evacuation.Status ==
                            MilitaryMedicalEvacuationStatus
                                .PatientDeceasedReturningToArmy ||
                    diedDuringPatientReturn &&
                        admission.Status ==
                            MilitaryRearMedicalAdmissionStatus.Discharged &&
                        evacuation != null && evacuation.Status ==
                            MilitaryMedicalEvacuationStatus
                                .PatientDeceasedAwaitingTeamRejoin ||
                    evacuation != null &&
                        evacuation.Status == expectedEvacuationStatus;
                var fieldHospital = admission.TreatmentPlanOriginSiteKindId ==
                    MilitaryRearMedicalSiteKindIds.FieldHospital;
                var surgeryStageIndex = fieldHospital ? 1 : 0;
                var surgeryPlanned = admission.TreatmentPlanProtocolIds !=
                        null &&
                    admission.TreatmentPlanProtocolIds.Count >
                        surgeryStageIndex &&
                    admission.TreatmentPlanProtocolIds[surgeryStageIndex] ==
                        MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery;
                var infectionStageIndex = (fieldHospital ? 1 : 0) +
                    (surgeryPlanned ? 1 : 0);
                var infectionPlanned = admission.TreatmentPlanProtocolIds !=
                        null &&
                    admission.TreatmentPlanProtocolIds.Count >
                        infectionStageIndex &&
                    admission.TreatmentPlanProtocolIds[infectionStageIndex] ==
                        MilitaryRearMedicalTreatmentProtocolIds
                            .InfectionControl;
                var expectedStages = (fieldHospital ? 2 : 1) +
                    (surgeryPlanned ? 1 : 0) +
                    (infectionPlanned ? 1 : 0);
                var validPlan = admission.TreatmentPlanProtocolIds != null &&
                    admission.TreatmentPlanProtocolIds.Count == expectedStages &&
                    (!surgeryPlanned ||
                     admission.TreatmentPlanProtocolIds[surgeryStageIndex] ==
                        MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery) &&
                    (fieldHospital
                        ? admission.TreatmentPlanProtocolIds[0] ==
                              MilitaryRearMedicalTreatmentProtocolIds
                                  .FieldStabilization &&
                          admission.TreatmentPlanProtocolIds[
                              expectedStages - 1] ==
                              MilitaryRearMedicalTreatmentProtocolIds
                                  .FieldRecovery
                        : admission.TreatmentPlanProtocolIds[
                              expectedStages - 1] ==
                              MilitaryRearMedicalTreatmentProtocolIds
                                  .InpatientHerbalRecovery);
                var injuryRequiresControl = hasInjury &&
                    injury.InfectionStatus != MilitaryInfectionStatus.AtRisk;
                var injuryRequiresSurgery = hasInjury &&
                    !string.IsNullOrEmpty(injury.SurgicalProcedureId);
                var stagesComplete =
                    admission.CompletedTreatmentStages == expectedStages;
                var careClosed = stagesComplete || diedDuringTreatment ||
                    diedDuringTransfer;
                var expectedDischargePolicy = diedDuringTransfer
                    ? MilitaryRearMedicalDischargePolicyIds
                        .DeathDuringMedicalTransfer
                    : diedAtCareSite
                    ? MilitaryRearMedicalDischargePolicyIds.DeathAtCareSite
                    : hasInjury && injury.RequiresMedicalRetirement
                        ? MilitaryRearMedicalDischargePolicyIds
                            .MedicalRetirementAtCareSite
                        : MilitaryRearMedicalDischargePolicyIds
                            .ReturnToSourceArmy;
                var expectedPatientReturnPolicy = diedAtCareSite ||
                    diedDuringTransfer
                    ? MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .RemainAtCareSiteAfterDeath
                    : diedDuringPatientReturn
                        ? diedAwaitingReturnTeam
                            ? MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .CorpseAtArmyAwaitingTeamRejoin
                            : MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .ReturnCorpseWithTeam
                    : hasInjury && injury.RequiresMedicalRetirement
                        ? MilitaryMedicalEvacuationPatientReturnPolicyIds
                            .RemainAtCareSiteForMedicalRetirement
                        : MilitaryMedicalEvacuationPatientReturnPolicyIds
                            .ReturnWithTeam;
                var discharged = admission.Status ==
                        MilitaryRearMedicalAdmissionStatus.Discharged ||
                    admission.Status ==
                        MilitaryRearMedicalAdmissionStatus.Completed;
                var completed = admission.Status ==
                    MilitaryRearMedicalAdmissionStatus.Completed;
                if (!hasEvacuation || !hasSite || !hasPatient || !hasService ||
                    !hasPhysician ||
                    !Enum.IsDefined(
                        typeof(MilitaryRearMedicalAdmissionStatus),
                        admission.Status) ||
                    admission.TreatmentPlanOriginSiteKindId !=
                        MilitaryRearMedicalSiteKindIds.FieldHospital &&
                    admission.TreatmentPlanOriginSiteKindId !=
                        MilitaryRearMedicalSiteKindIds.ExistingClinic ||
                    evacuation.RearMedicalAdmissionId != admission.Id ||
                    evacuation.RearMedicalSiteId != site.Id ||
                    !validEvacuationStatus ||
                    admission.PatientPersonId != evacuation.PatientPersonId ||
                    admission.PatientMilitaryServiceId !=
                        evacuation.PatientMilitaryServiceId ||
                    admission.PhysicianPersonId != expectedPhysicianPersonId ||
                    !string.IsNullOrEmpty(admission.MedicalTransferId) !=
                        hasMedicalTransfer ||
                    hasMedicalTransfer &&
                        (medicalTransfer.AdmissionId != admission.Id ||
                         medicalTransfer.EvacuationId != admission.EvacuationId) ||
                    service.PersonId != admission.PatientPersonId ||
                    admission.AdmittedDay < evacuation.ReceivedDay ||
                    admission.AdmittedDay > AbsoluteDay ||
                    !validPlan ||
                    hasInjury != !string.IsNullOrEmpty(
                        admission.InjuryEpisodeId) ||
                    !hasInjury && admission.AdmittedDay >=
                        MilitaryInjuryContractActivationDay ||
                    hasInjury &&
                        (injury.AdmissionId != admission.Id ||
                         injury.EvacuationId != admission.EvacuationId ||
                         injury.PatientPersonId != admission.PatientPersonId ||
                         injury.PatientMilitaryServiceId !=
                             admission.PatientMilitaryServiceId ||
                         injury.AssessedDay != admission.AdmittedDay) ||
                    injuryRequiresSurgery != surgeryPlanned ||
                    injuryRequiresControl != infectionPlanned ||
                    injuryRequiresSurgery && !string.IsNullOrEmpty(
                        injury.SurgeryTreatmentId) !=
                            (admission.CompletedTreatmentStages >
                                surgeryStageIndex) ||
                    hasInjury && injury.InfectionStatus ==
                        MilitaryInfectionStatus.Controlled &&
                        admission.CompletedTreatmentStages <=
                            infectionStageIndex ||
                    hasInjury && injury.InfectionStatus ==
                        MilitaryInfectionStatus.Active &&
                        admission.CompletedTreatmentStages >
                            infectionStageIndex ||
                    admission.TreatmentIds == null ||
                    admission.RequiredTreatmentStages != expectedStages ||
                    admission.CompletedTreatmentStages < 0 ||
                    admission.CompletedTreatmentStages > expectedStages ||
                    admission.TreatmentIds.Count !=
                        admission.CompletedTreatmentStages ||
                    (admission.CompletedTreatmentStages == 0) !=
                        string.IsNullOrEmpty(admission.TreatmentId) ||
                    admission.CompletedTreatmentStages > 0 &&
                    admission.TreatmentId != admission.TreatmentIds[
                            admission.TreatmentIds.Count - 1] ||
                    (admission.Status ==
                        MilitaryRearMedicalAdmissionStatus.InTreatment) !=
                        (!stagesComplete && !diedDuringTreatment &&
                         !diedDuringTransfer) ||
                    careClosed != (admission.ReadyForReturnDay >= 0) ||
                    careClosed != !string.IsNullOrEmpty(
                        admission.DischargePolicyId) ||
                    careClosed && admission.DischargePolicyId !=
                        expectedDischargePolicy ||
                    careClosed && evacuation.PatientReturnPolicyId !=
                        expectedPatientReturnPolicy ||
                    diedDuringTreatment != !string.IsNullOrEmpty(
                        admission.InpatientDeathClosureId) ||
                    diedDuringTreatment && admission.InpatientDeathClosureId !=
                        woundDeath.InpatientDeathClosureId ||
                    diedDuringTransfer != !string.IsNullOrEmpty(
                        admission.MedicalTransferDeathClosureId) ||
                    diedDuringTransfer &&
                        admission.MedicalTransferDeathClosureId !=
                            woundDeath.MedicalTransferDeathClosureId ||
                    diedDuringPatientReturn != !string.IsNullOrEmpty(
                        admission.PatientReturnDeathClosureId) ||
                    diedDuringPatientReturn &&
                        admission.PatientReturnDeathClosureId !=
                            woundDeath.PatientReturnDeathClosureId ||
                    discharged != (admission.DischargedDay >= 0) ||
                    discharged && admission.DischargedDay <
                        admission.ReadyForReturnDay ||
                    completed != (admission.CompletedDay >= 0) ||
                    completed && admission.CompletedDay < admission.DischargedDay ||
                    completed &&
                        woundDeath == null &&
                        (admission.DischargePolicyId ==
                            MilitaryRearMedicalDischargePolicyIds
                                .MedicalRetirementAtCareSite) !=
                            (service.Status == MilitaryServiceStatus.Retired) ||
                    completed &&
                        woundDeath == null &&
                        (admission.DischargePolicyId ==
                            MilitaryRearMedicalDischargePolicyIds
                                .ReturnToSourceArmy) !=
                            (service.Status == MilitaryServiceStatus.Active) ||
                    completed &&
                        woundDeath != null &&
                        service.Status != MilitaryServiceStatus.Dead)
                {
                    throw new InvalidOperationException(
                        $"Invalid rear medical admission {admission.Id}: " +
                        $"status={admission.Status}, evacuation=" +
                        $"{evacuation?.Status}, hasInjury={hasInjury}, " +
                        $"planValid={validPlan}, required=" +
                        $"{admission.RequiredTreatmentStages}, expected=" +
                        $"{expectedStages}, completed=" +
                        $"{admission.CompletedTreatmentStages}, readyDay=" +
                        $"{admission.ReadyForReturnDay}, policy=" +
                        $"{admission.DischargePolicyId}, patientPolicy=" +
                        $"{evacuation?.PatientReturnPolicyId}, service=" +
                        $"{service?.Status}.");
                }
                if (!discharged && !activeMedicalTransfer)
                {
                    AddInt(occupiedBeds, site.Id, 1);
                }
                admissions.Add(admission.Id, admission);
            }
            foreach (var pair in medicalTransfers)
            {
                if (pair.Value.Status ==
                        MilitaryMedicalTransferStatus.InTransit ||
                    pair.Value.Status ==
                        MilitaryMedicalTransferStatus.AwaitingReception)
                {
                    AddInt(
                        occupiedBeds,
                        pair.Value.DestinationRearMedicalSiteId,
                        1);
                }
            }
            foreach (var pair in injuryEpisodes)
            {
                if (!admissions.TryGetValue(
                        pair.Value.AdmissionId, out var admission) ||
                    admission.InjuryEpisodeId != pair.Key)
                {
                    throw new InvalidOperationException(
                        $"Military injury episode {pair.Key} lacks its admission.");
                }
            }
            ValidateMilitaryWoundDeaths(
                woundDeathPolicies,
                inpatientDeteriorationPolicies,
                originalEvacuationDeteriorationPolicies,
                patientReturnDeteriorationPolicies,
                returnTeamDeathPolicies,
                injuryEpisodes,
                admissions,
                evacuations,
                armies,
                people,
                militaryServices);
            ValidateMilitaryMedicalTransfers(
                medicalTransfers,
                admissions,
                evacuations,
                sites,
                armies,
                people,
                militaryServices,
                batches,
                transactions);
            foreach (var pair in occupiedBeds)
            {
                if (!sites.TryGetValue(pair.Key, out var occupiedSite) ||
                    pair.Value > occupiedSite.BedCapacity)
                {
                    throw new InvalidOperationException(
                        $"Rear medical site {pair.Key} exceeds bed capacity.");
                }
            }

            var treatmentIds = new HashSet<string>(StringComparer.Ordinal);
            var treatmentStages = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryRearMedicalTreatments.Count; i++)
            {
                var treatment = MilitaryRearMedicalTreatments[i] ??
                    throw new InvalidOperationException(
                        "A rear medical treatment cannot be null.");
                var hasAdmission = admissions.TryGetValue(
                    treatment.AdmissionId, out var admission);
                var hasSite = sites.TryGetValue(
                    treatment.RearMedicalSiteId, out var site);
                var hasBatch = batches.TryGetValue(
                    treatment.SourceMedicineBatchId, out var batch);
                var hasTransaction = transactions.TryGetValue(
                    treatment.InventoryTransactionId, out var transaction);
                var expectedStageCount = hasAdmission
                    ? admission.RequiredTreatmentStages
                    : 0;
                var validStage = hasAdmission &&
                    admission.TreatmentPlanProtocolIds != null &&
                    treatment.StageIndex >= 0 &&
                    treatment.StageIndex <
                        admission.TreatmentPlanProtocolIds.Count;
                var expectedProtocol = validStage
                    ? admission.TreatmentPlanProtocolIds[treatment.StageIndex]
                    : string.Empty;
                var infectionControl = expectedProtocol ==
                    MilitaryRearMedicalTreatmentProtocolIds.InfectionControl;
                var surgery = expectedProtocol ==
                    MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery;
                var stabilization = expectedProtocol ==
                    MilitaryRearMedicalTreatmentProtocolIds.FieldStabilization;
                MilitaryInjuryEpisodeState treatmentInjury = null;
                MilitarySurgicalProcedureDefinitionState treatmentProcedure =
                    null;
                var hasSurgicalProcedure = surgery &&
                    hasAdmission &&
                    injuryEpisodes.TryGetValue(
                        admission.InjuryEpisodeId, out treatmentInjury) &&
                    surgicalProcedures.TryGetValue(
                        treatmentInjury.SurgicalProcedureId,
                        out treatmentProcedure);
                var expectedWorkMinutes = surgery && hasSurgicalProcedure
                    ? treatmentProcedure.WorkMinutes
                    : infectionControl
                    ? MilitaryMedicalRules.InfectionControlWorkMinutes
                    : stabilization
                        ? MilitaryMedicalRules.FieldStabilizationWorkMinutes
                        : MilitaryMedicalRules.RearTreatmentWorkMinutes;
                var expectedHealth = surgery && hasSurgicalProcedure
                    ? treatmentProcedure.TargetHealthBasisPoints
                    : infectionControl
                    ? MilitaryMedicalRules.InfectionControlHealthBasisPoints
                    : stabilization
                        ? MilitaryMedicalRules
                            .FieldStabilizationHealthBasisPoints
                        : MilitaryMedicalRules.ReturnToDutyHealthBasisPoints;
                var expectedMedicineUnits = surgery && hasSurgicalProcedure
                    ? treatmentProcedure.MedicineUnits
                    : infectionControl
                    ? MilitaryMedicalRules.InfectionControlMedicineUnits
                    : MilitaryMedicalRules.MedicineUnitsPerTreatment;
                var expectedMinimumSkill = surgery && hasSurgicalProcedure
                    ? treatmentProcedure.MinimumPhysicianSkillBasisPoints
                    : MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints;
                var firstTreatmentTransfer = hasAdmission
                    ? FindFirstMedicalTransferForAdmission(
                        admission.Id, medicalTransfers)
                    : null;
                var treatmentTransfer = hasAdmission
                    ? FindMedicalTransferForTreatmentStage(
                        admission.Id,
                        treatment.StageIndex,
                        medicalTransfers)
                    : null;
                var hasTreatmentTransfer = treatmentTransfer != null;
                var usesTransferReservation = hasTreatmentTransfer;
                var expectedTreatmentSiteId = hasTreatmentTransfer
                    ? treatmentTransfer.DestinationRearMedicalSiteId
                    : firstTreatmentTransfer != null
                        ? firstTreatmentTransfer.SourceRearMedicalSiteId
                        : admission?.RearMedicalSiteId;
                var expectedTreatmentPhysicianId = hasTreatmentTransfer
                    ? treatmentTransfer.DesignatedReceivingPersonId
                    : firstTreatmentTransfer != null
                        ? firstTreatmentTransfer.SourcePhysicianPersonId
                        : admission?.PhysicianPersonId;
                if (!treatmentIds.Add(treatment.Id) ||
                    !hasAdmission || !hasSite || !hasBatch || !hasTransaction ||
                    !treatmentStages.Add(
                        treatment.AdmissionId + "|" + treatment.StageIndex) ||
                    treatment.RequiredStageCount != expectedStageCount ||
                    treatment.StageIndex < 0 ||
                    treatment.StageIndex >= expectedStageCount ||
                    treatment.StageIndex >= admission.TreatmentIds.Count ||
                    admission.TreatmentIds[treatment.StageIndex] != treatment.Id ||
                    treatment.EvacuationId != admission.EvacuationId ||
                    treatment.PatientPersonId != admission.PatientPersonId ||
                    treatment.PatientMilitaryServiceId !=
                        admission.PatientMilitaryServiceId ||
                    treatment.RearMedicalSiteId != expectedTreatmentSiteId ||
                    treatment.PhysicianPersonId !=
                        expectedTreatmentPhysicianId ||
                    usesTransferReservation &&
                        (treatmentTransfer.Status !=
                             MilitaryMedicalTransferStatus.Completed ||
                         treatment.SourceMedicineBatchId !=
                             treatmentTransfer.ReservedMedicineBatchId ||
                         treatment.Day < treatmentTransfer.ReceivedDay) ||
                    treatment.TreatmentProtocolId != expectedProtocol ||
                    surgery && !hasSurgicalProcedure ||
                    treatment.MedicineProductDefinitionId !=
                        CoreProductionContent.HerbalMedicineMaterialProductId ||
                    batch.ProductDefinitionId !=
                        treatment.MedicineProductDefinitionId ||
                    batch.InventoryContainerId !=
                        site.MedicineInventoryContainerId ||
                    batch.OwnerOrganizationId != site.OwnerOrganizationId ||
                    treatment.MedicineUnitsConsumed != expectedMedicineUnits ||
                    treatment.WorkMinutes != expectedWorkMinutes ||
                    treatment.OpeningHealthBasisPoints < 0 ||
                    treatment.OpeningHealthBasisPoints > 10_000 ||
                    treatment.ClosingHealthBasisPoints != Math.Max(
                        treatment.OpeningHealthBasisPoints,
                        expectedHealth) ||
                    treatment.RecoveredHealthBasisPoints !=
                        treatment.ClosingHealthBasisPoints -
                        treatment.OpeningHealthBasisPoints ||
                    treatment.PhysicianMedicalSkillBeforeBasisPoints <
                        expectedMinimumSkill ||
                    treatment.PhysicianMedicalSkillAfterBasisPoints != checked(
                        treatment.PhysicianMedicalSkillBeforeBasisPoints +
                        treatment.PhysicianMedicalSkillGainBasisPoints) ||
                    treatment.PhysicianMedicalSkillGainBasisPoints <= 0 ||
                    treatment.PhysicianMedicalSkillAfterBasisPoints > 10_000 ||
                    treatment.Day < admission.AdmittedDay ||
                    treatment.Day > AbsoluteDay ||
                    treatment.StageIndex == expectedStageCount - 1 &&
                        treatment.Day != admission.ReadyForReturnDay ||
                    transaction.Type != InventoryTransactionType
                        .MilitaryRearMedicalTreatmentConsumed ||
                    transaction.SourceMilitaryRearMedicalTreatmentId !=
                        treatment.Id ||
                    transaction.ActorPersonId != treatment.PhysicianPersonId ||
                    transaction.Lines.Count != 1 ||
                    transaction.Lines[0].BatchId != batch.Id ||
                    transaction.Lines[0].QuantityDelta !=
                        -treatment.MedicineUnitsConsumed ||
                    transaction.Lines[0].ReservedQuantityDelta !=
                        (usesTransferReservation
                            ? -treatment.MedicineUnitsConsumed
                            : 0))
                {
                    throw new InvalidOperationException(
                        $"Invalid rear medical treatment {treatment.Id}.");
                }
                if (infectionControl &&
                    (!injuryEpisodes.TryGetValue(
                         admission.InjuryEpisodeId, out var injury) ||
                     injury.InfectionStatus !=
                         MilitaryInfectionStatus.Controlled ||
                     injury.InfectionControlTreatmentId != treatment.Id ||
                     injury.InfectionControlledDay != treatment.Day))
                {
                    throw new InvalidOperationException(
                        $"Infection-control treatment {treatment.Id} lacks " +
                        "its resolved injury episode.");
                }
                if (surgery &&
                    (treatmentInjury.SurgeryTreatmentId != treatment.Id ||
                     treatmentInjury.SurgeryCompletedDay != treatment.Day))
                {
                    throw new InvalidOperationException(
                        $"Surgical treatment {treatment.Id} lacks its " +
                        "resolved injury outcome.");
                }
                AddInt(
                    workByPhysicianDay,
                    treatment.PhysicianPersonId + "|" + treatment.Day,
                    treatment.WorkMinutes);
            }
            foreach (var pair in injuryEpisodes)
            {
                if (pair.Value.InfectionStatus ==
                        MilitaryInfectionStatus.Controlled &&
                    !treatmentIds.Contains(
                        pair.Value.InfectionControlTreatmentId))
                {
                    throw new InvalidOperationException(
                        $"Controlled injury episode {pair.Key} lacks treatment evidence.");
                }
                if (!string.IsNullOrEmpty(pair.Value.SurgeryTreatmentId) &&
                    !treatmentIds.Contains(pair.Value.SurgeryTreatmentId))
                {
                    throw new InvalidOperationException(
                        $"Resolved surgery {pair.Key} lacks treatment evidence.");
                }
            }
            foreach (var pair in admissions)
            {
                var admission = pair.Value;
                for (var stage = 0;
                     stage < admission.CompletedTreatmentStages;
                     stage++)
                {
                    if (!treatmentStages.Contains(pair.Key + "|" + stage))
                    {
                        throw new InvalidOperationException(
                            $"Rear medical admission {pair.Key} lacks " +
                            $"treatment stage {stage} evidence.");
                    }
                }
            }
            return treatmentIds;
        }

        private void ValidateMilitaryWoundDeaths(
            Dictionary<string, MilitaryWoundDeathPolicyDefinitionState>
                policies,
            Dictionary<string,
                MilitaryInpatientDeteriorationPolicyDefinitionState>
                deteriorationPolicies,
            Dictionary<string,
                MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState>
                originalEvacuationDeteriorationPolicies,
            Dictionary<string,
                MilitaryPatientReturnDeteriorationPolicyDefinitionState>
                patientReturnDeteriorationPolicies,
            Dictionary<string, MilitaryReturnTeamDeathPolicyDefinitionState>
                returnTeamDeathPolicies,
            Dictionary<string, MilitaryInjuryEpisodeState> injuries,
            Dictionary<string, MilitaryRearMedicalAdmissionState> admissions,
            Dictionary<string, MilitaryMedicalEvacuationState> evacuations,
            Dictionary<string, ArmyState> armies,
            Dictionary<string, PersonState> people,
            Dictionary<string, MilitaryServiceState> services)
        {
            var inheritances = new Dictionary<
                string, MilitaryFamilyInheritanceState>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryFamilyInheritances.Count; i++)
            {
                var inheritance = MilitaryFamilyInheritances[i] ??
                    throw new InvalidOperationException(
                        "A military family inheritance cannot be null.");
                _ = new StableId(inheritance.Id);
                if (inheritances.ContainsKey(inheritance.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate military family inheritance {inheritance.Id}.");
                }
                inheritances.Add(inheritance.Id, inheritance);
            }
            var compensations = new Dictionary<
                string, MilitarySurvivorCompensationState>(StringComparer.Ordinal);
            for (var i = 0; i < MilitarySurvivorCompensations.Count; i++)
            {
                var compensation = MilitarySurvivorCompensations[i] ??
                    throw new InvalidOperationException(
                        "A military survivor compensation cannot be null.");
                _ = new StableId(compensation.Id);
                if (compensations.ContainsKey(compensation.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate military survivor compensation {compensation.Id}.");
                }
                compensations.Add(compensation.Id, compensation);
            }
            var organizations = new Dictionary<string, OrganizationState>(
                StringComparer.Ordinal);
            for (var i = 0; i < Organizations.Count; i++)
                organizations.Add(Organizations[i].Id, Organizations[i]);
            var families = new Dictionary<string, FamilyState>(
                StringComparer.Ordinal);
            for (var i = 0; i < Families.Count; i++)
                families.Add(Families[i].Id, Families[i]);
            var lifeEvents = new Dictionary<string, LifeEventRecordState>(
                StringComparer.Ordinal);
            for (var i = 0; i < LifeEvents.Count; i++)
                lifeEvents.Add(LifeEvents[i].Id, LifeEvents[i]);
            var sites = new Dictionary<string, MilitaryRearMedicalSiteState>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryRearMedicalSites.Count; i++)
                sites.Add(MilitaryRearMedicalSites[i].Id,
                    MilitaryRearMedicalSites[i]);
            var responsibilities = new Dictionary<
                string, MilitaryMedicalDeathResponsibilityState>(
                    StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryMedicalDeathResponsibilities.Count;
                 i++)
            {
                var responsibility =
                    MilitaryMedicalDeathResponsibilities[i] ??
                    throw new InvalidOperationException(
                        "A military medical death responsibility cannot be null.");
                _ = new StableId(responsibility.Id);
                if (responsibilities.ContainsKey(responsibility.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate military medical death responsibility " +
                        $"{responsibility.Id}.");
                }
                responsibilities.Add(responsibility.Id, responsibility);
            }
            var inpatientClosures = new Dictionary<
                string, MilitaryInpatientDeathClosureState>(
                    StringComparer.Ordinal);
            for (var i = 0; i < MilitaryInpatientDeathClosures.Count; i++)
            {
                var closure = MilitaryInpatientDeathClosures[i] ??
                    throw new InvalidOperationException(
                        "A military inpatient death closure cannot be null.");
                _ = new StableId(closure.Id);
                if (inpatientClosures.ContainsKey(closure.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate military inpatient death closure {closure.Id}.");
                }
                inpatientClosures.Add(closure.Id, closure);
            }
            var transferDeathClosures = new Dictionary<
                string, MilitaryMedicalTransferDeathClosureState>(
                    StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryMedicalTransferDeathClosures.Count;
                 i++)
            {
                var closure = MilitaryMedicalTransferDeathClosures[i] ??
                    throw new InvalidOperationException(
                        "A military medical-transfer death closure cannot be null.");
                _ = new StableId(closure.Id);
                if (transferDeathClosures.ContainsKey(closure.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate military medical-transfer death closure {closure.Id}.");
                }
                transferDeathClosures.Add(closure.Id, closure);
            }
            var originalEvacuationDeathClosures = new Dictionary<
                string, MilitaryOriginalEvacuationDeathClosureState>(
                    StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryOriginalEvacuationDeathClosures.Count;
                 i++)
            {
                var closure = MilitaryOriginalEvacuationDeathClosures[i] ??
                    throw new InvalidOperationException(
                        "A military original-evacuation death closure cannot be null.");
                _ = new StableId(closure.Id);
                if (originalEvacuationDeathClosures.ContainsKey(closure.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate military original-evacuation death closure {closure.Id}.");
                }
                originalEvacuationDeathClosures.Add(closure.Id, closure);
            }
            var patientReturnDeathClosures = new Dictionary<
                string, MilitaryPatientReturnDeathClosureState>(
                    StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryPatientReturnDeathClosures.Count;
                 i++)
            {
                var closure = MilitaryPatientReturnDeathClosures[i] ??
                    throw new InvalidOperationException(
                        "A military patient-return death closure cannot be null.");
                _ = new StableId(closure.Id);
                if (patientReturnDeathClosures.ContainsKey(closure.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate military patient-return death closure {closure.Id}.");
                }
                patientReturnDeathClosures.Add(closure.Id, closure);
            }
            var productBatches = new Dictionary<string, ProductBatchState>(
                StringComparer.Ordinal);
            for (var i = 0; i < ProductBatches.Count; i++)
                productBatches.Add(ProductBatches[i].Id, ProductBatches[i]);
            var inventoryTransactions = new Dictionary<
                string, InventoryTransactionState>(StringComparer.Ordinal);
            for (var i = 0; i < InventoryTransactions.Count; i++)
                inventoryTransactions.Add(
                    InventoryTransactions[i].Id, InventoryTransactions[i]);
            var medicalTransfers = new Dictionary<
                string, MilitaryMedicalTransferState>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryMedicalTransfers.Count; i++)
                medicalTransfers.Add(
                    MilitaryMedicalTransfers[i].Id,
                    MilitaryMedicalTransfers[i]);

            var deathIds = new HashSet<string>(StringComparer.Ordinal);
            var deathPatients = new HashSet<string>(StringComparer.Ordinal);
            var deathInjuries = new HashSet<string>(StringComparer.Ordinal);
            var usedInheritances = new HashSet<string>(StringComparer.Ordinal);
            var usedCompensations = new HashSet<string>(StringComparer.Ordinal);
            var usedResponsibilities = new HashSet<string>(
                StringComparer.Ordinal);
            var usedInpatientClosures = new HashSet<string>(
                StringComparer.Ordinal);
            var usedTransferDeathClosures = new HashSet<string>(
                StringComparer.Ordinal);
            var usedOriginalEvacuationDeathClosures = new HashSet<string>(
                StringComparer.Ordinal);
            var usedPatientReturnDeathClosures = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < MilitaryWoundDeaths.Count; i++)
            {
                var death = MilitaryWoundDeaths[i] ??
                    throw new InvalidOperationException(
                        "A military wound death cannot be null.");
                _ = new StableId(death.Id);
                _ = new StableId(death.PolicyId);
                _ = new StableId(death.DeathContextId);
                var postReturnDeath = death.DeathContextId ==
                    MilitaryWoundDeathContextIds.PostReturnMedicalRetirement;
                var readyForReturnDeath = death.DeathContextId ==
                    MilitaryWoundDeathContextIds.ReadyForReturnAtCareSite;
                var inpatientDeath = death.DeathContextId ==
                    MilitaryWoundDeathContextIds.InTreatmentAtCareSite;
                var transferDeath = death.DeathContextId ==
                    MilitaryWoundDeathContextIds.DuringCrossFacilityTransfer;
                var originalEvacuationDeath = death.DeathContextId ==
                    MilitaryWoundDeathContextIds.DuringOriginalEvacuation;
                var patientReturnJourneyDeath = death.DeathContextId ==
                    MilitaryWoundDeathContextIds.DuringPatientReturnJourney;
                var patientAwaitingTeamDeath = death.DeathContextId ==
                    MilitaryWoundDeathContextIds
                        .AwaitingReturnTeamRejoinAtArmy;
                var patientReturnDeath = patientReturnJourneyDeath ||
                    patientAwaitingTeamDeath;
                var careSiteDeath = readyForReturnDeath || inpatientDeath;
                var hasPolicy = policies.TryGetValue(
                    death.PolicyId, out var policy);
                var hasInjury = injuries.TryGetValue(
                    death.InjuryEpisodeId, out var injury);
                var hasAdmission = admissions.TryGetValue(
                    death.AdmissionId, out var admission);
                var hasEvacuation = evacuations.TryGetValue(
                    death.EvacuationId, out var evacuation);
                var hasService = services.TryGetValue(
                    death.PatientMilitaryServiceId, out var service);
                var hasArmy = armies.TryGetValue(death.ArmyId, out var army);
                var hasOrganization = organizations.TryGetValue(
                    death.OrganizationId, out var organization);
                var hasPatient = people.TryGetValue(
                    death.PatientPersonId, out var patient);
                var hasFamily = families.TryGetValue(
                    death.FamilyId, out var family);
                var hasInheritance = inheritances.TryGetValue(
                    death.FamilyInheritanceId, out var inheritance);
                var hasCompensation = compensations.TryGetValue(
                    death.SurvivorCompensationId, out var compensation);
                var hasDeathEvent = lifeEvents.TryGetValue(
                    death.DeathLifeEventId, out var deathEvent);
                MilitaryMedicalDeathResponsibilityState responsibility = null;
                var hasResponsibility = !string.IsNullOrEmpty(
                        death.MedicalResponsibilityId) &&
                    responsibilities.TryGetValue(
                        death.MedicalResponsibilityId, out responsibility);
                MilitaryRearMedicalSiteState responsibilitySite = null;
                var hasResponsibilitySite = hasResponsibility &&
                    sites.TryGetValue(
                        responsibility.RearMedicalSiteId,
                        out responsibilitySite);
                var hasResponsiblePhysician = hasResponsibility &&
                    people.ContainsKey(
                        responsibility.ResponsiblePhysicianPersonId);
                MilitaryInpatientDeathClosureState inpatientClosure = null;
                var hasInpatientClosure = !string.IsNullOrEmpty(
                        death.InpatientDeathClosureId) &&
                    inpatientClosures.TryGetValue(
                        death.InpatientDeathClosureId, out inpatientClosure);
                MilitaryInpatientDeteriorationPolicyDefinitionState
                    deteriorationPolicy = null;
                var hasDeteriorationPolicy = hasInpatientClosure &&
                    deteriorationPolicies.TryGetValue(
                        inpatientClosure.DeteriorationPolicyId,
                        out deteriorationPolicy);
                MilitaryMedicalTransferDeathClosureState transferClosure =
                    null;
                var hasTransferClosure = !string.IsNullOrEmpty(
                        death.MedicalTransferDeathClosureId) &&
                    transferDeathClosures.TryGetValue(
                        death.MedicalTransferDeathClosureId,
                        out transferClosure);
                if (!hasDeteriorationPolicy && hasTransferClosure)
                {
                    hasDeteriorationPolicy =
                        deteriorationPolicies.TryGetValue(
                            transferClosure.DeteriorationPolicyId,
                            out deteriorationPolicy);
                }
                MilitaryOriginalEvacuationDeathClosureState
                    originalEvacuationClosure = null;
                var hasOriginalEvacuationClosure = !string.IsNullOrEmpty(
                        death.OriginalEvacuationDeathClosureId) &&
                    originalEvacuationDeathClosures.TryGetValue(
                        death.OriginalEvacuationDeathClosureId,
                        out originalEvacuationClosure);
                MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState
                    originalEvacuationPolicy = null;
                var hasOriginalEvacuationPolicy =
                    hasOriginalEvacuationClosure &&
                    originalEvacuationDeteriorationPolicies.TryGetValue(
                        originalEvacuationClosure.DeteriorationPolicyId,
                        out originalEvacuationPolicy);
                MilitaryPatientReturnDeathClosureState patientReturnClosure =
                    null;
                var hasPatientReturnClosure = !string.IsNullOrEmpty(
                        death.PatientReturnDeathClosureId) &&
                    patientReturnDeathClosures.TryGetValue(
                        death.PatientReturnDeathClosureId,
                        out patientReturnClosure);
                MilitaryPatientReturnDeteriorationPolicyDefinitionState
                    patientReturnPolicy = null;
                var hasPatientReturnPolicy = hasPatientReturnClosure &&
                    patientReturnDeteriorationPolicies.TryGetValue(
                        patientReturnClosure.DeteriorationPolicyId,
                        out patientReturnPolicy);
                var headChanged = hasInheritance && inheritance.HeadChanged;
                LifeEventRecordState successionEvent = null;
                var hasSuccessionEvent = headChanged &&
                    lifeEvents.TryGetValue(
                        death.SuccessionLifeEventId, out successionEvent);
                var expectedCompensation = hasPolicy && hasService
                    ? checked(
                        policy.BaseCompensationMoney +
                        policy.CompensationPerRankMoney * service.Rank)
                    : -1;
                var validDeathPhase = hasEvacuation &&
                    (originalEvacuationDeath && !hasAdmission &&
                        !hasInjury &&
                        (evacuation.Status ==
                             MilitaryMedicalEvacuationStatus
                                 .DeceasedInTransit ||
                         evacuation.Status >=
                             MilitaryMedicalEvacuationStatus.ReadyForReturn) ||
                     hasAdmission && postReturnDeath &&
                        admission.Status ==
                            MilitaryRearMedicalAdmissionStatus.Completed &&
                        evacuation.Status ==
                            MilitaryMedicalEvacuationStatus.Completed ||
                     hasAdmission && readyForReturnDeath &&
                        admission.Status !=
                            MilitaryRearMedicalAdmissionStatus.InTreatment &&
                        (evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.ReadyForReturn ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.Completed) ||
                     hasAdmission && inpatientDeath &&
                        admission.Status >=
                            MilitaryRearMedicalAdmissionStatus.Discharged &&
                        (evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.ReadyForReturn ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.Completed) ||
                     hasAdmission && transferDeath &&
                        admission.Status >=
                            MilitaryRearMedicalAdmissionStatus.Discharged &&
                        (medicalTransfers.TryGetValue(
                             admission.MedicalTransferId,
                             out var deathTransfer) &&
                         (deathTransfer.Status ==
                              MilitaryMedicalTransferStatus
                                  .DeceasedInTransit &&
                          evacuation.Status ==
                              MilitaryMedicalEvacuationStatus.Admitted ||
                          deathTransfer.Status ==
                              MilitaryMedicalTransferStatus
                                  .ClosedAfterPatientDeath &&
                          (evacuation.Status ==
                               MilitaryMedicalEvacuationStatus.ReadyForReturn ||
                           evacuation.Status ==
                               MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                           evacuation.Status ==
                               MilitaryMedicalEvacuationStatus.Completed))) ||
                      hasAdmission && patientReturnDeath &&
                         (admission.Status ==
                              MilitaryRearMedicalAdmissionStatus.Discharged &&
                          (evacuation.Status ==
                               MilitaryMedicalEvacuationStatus
                                   .PatientDeceasedReturningToArmy ||
                           evacuation.Status ==
                               MilitaryMedicalEvacuationStatus
                                   .PatientDeceasedAwaitingTeamRejoin) ||
                          admission.Status ==
                              MilitaryRearMedicalAdmissionStatus.Completed &&
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.Completed));
                var waitingPeriodStartDay = hasAdmission
                    ? readyForReturnDeath
                        ? admission.ReadyForReturnDay
                        : inpatientDeath
                            ? admission.AdmittedDay
                        : transferDeath
                            ? admission.AdmittedDay
                        : patientReturnDeath
                            ? evacuation.ReturnStartedDay
                        : admission.CompletedDay
                    : -1;
                MilitaryMedicalTransferState closureTransfer = null;
                var hasClosureTransfer = hasInpatientClosure &&
                    !string.IsNullOrEmpty(inpatientClosure.MedicalTransferId) &&
                    medicalTransfers.TryGetValue(
                        inpatientClosure.MedicalTransferId,
                        out closureTransfer);
                ProductBatchState closureBatch = null;
                var hasClosureBatch = hasInpatientClosure &&
                    !string.IsNullOrEmpty(
                        inpatientClosure.ReservedMedicineBatchId) &&
                    productBatches.TryGetValue(
                        inpatientClosure.ReservedMedicineBatchId,
                        out closureBatch);
                InventoryTransactionState closureReleaseTransaction = null;
                var hasClosureReleaseTransaction = hasInpatientClosure &&
                    !string.IsNullOrEmpty(
                        inpatientClosure
                            .ReservationReleaseInventoryTransactionId) &&
                    inventoryTransactions.TryGetValue(
                        inpatientClosure
                            .ReservationReleaseInventoryTransactionId,
                        out closureReleaseTransaction);
                var currentPatientReturnJourney = hasPatientReturnClosure
                    ? FindJourneyById(
                        patientReturnClosure.PatientReturnJourneyId)
                    : null;
                var validPatientReturnTeamSnapshots =
                    !hasPatientReturnClosure ||
                    ValidatePatientReturnTeamJourneySnapshots(
                        evacuation, patientReturnClosure);
                if (!deathIds.Add(death.Id) ||
                    !deathPatients.Add(death.PatientPersonId) ||
                    !originalEvacuationDeath &&
                        !deathInjuries.Add(death.InjuryEpisodeId) ||
                    !usedInheritances.Add(death.FamilyInheritanceId) ||
                    !usedCompensations.Add(death.SurvivorCompensationId) ||
                    !hasPolicy || !hasEvacuation || !hasService || !hasArmy ||
                    !hasOrganization || !hasPatient || !hasFamily ||
                    !hasInheritance || !hasCompensation || !hasDeathEvent ||
                    !originalEvacuationDeath && (!hasInjury || !hasAdmission) ||
                    !postReturnDeath && !readyForReturnDeath &&
                        !inpatientDeath && !transferDeath &&
                        !originalEvacuationDeath && !patientReturnDeath ||
                    !validDeathPhase ||
                    death.Day < MilitaryWoundDeathContractActivationDay ||
                    death.Day > AbsoluteDay ||
                    !originalEvacuationDeath &&
                        (injury.AdmissionId != admission.Id ||
                         injury.EvacuationId != evacuation.Id ||
                         injury.PatientPersonId != patient.Id ||
                         injury.PatientMilitaryServiceId != service.Id ||
                         admission.EvacuationId != evacuation.Id ||
                         admission.PatientPersonId != patient.Id ||
                         admission.PatientMilitaryServiceId != service.Id) ||
                    service.PersonId != patient.Id ||
                    service.ArmyId != army.Id ||
                    service.Status != MilitaryServiceStatus.Dead ||
                    army.OrganizationId != organization.Id ||
                    !originalEvacuationDeath && !inpatientDeath &&
                        !transferDeath && !patientReturnDeath &&
                        !injury.RequiresMedicalRetirement ||
                    !originalEvacuationDeath &&
                        death.SeverityBasisPoints !=
                            injury.SeverityBasisPoints ||
                    death.SeverityBasisPoints <
                        policy.MinimumSeverityBasisPoints ||
                    death.HealthAtDeathBasisPoints < 0 ||
                    death.HealthAtDeathBasisPoints >
                        policy.MaximumPostTreatmentHealthBasisPoints ||
                    patient.HealthBasisPoints !=
                        death.HealthAtDeathBasisPoints ||
                    patient.IsAlive || patient.Wealth != 0 ||
                    !transferDeath && !originalEvacuationDeath &&
                        !patientReturnDeath &&
                        (death.DeathLocationId != patient.LocationId ||
                         death.DeathLocationId !=
                            evacuation.CurrentCareLocationId) ||
                    !originalEvacuationDeath && !inpatientDeath &&
                        !transferDeath && !patientReturnDeath &&
                        death.Day < checked(
                        waitingPeriodStartDay +
                        policy.MinimumDaysAfterCareCompletion) ||
                    !inpatientDeath && !string.IsNullOrEmpty(
                        death.InpatientDeathClosureId) ||
                    !transferDeath && !string.IsNullOrEmpty(
                        death.MedicalTransferDeathClosureId) ||
                    !originalEvacuationDeath && !string.IsNullOrEmpty(
                        death.OriginalEvacuationDeathClosureId) ||
                    !patientReturnDeath && !string.IsNullOrEmpty(
                        death.PatientReturnDeathClosureId) ||
                    postReturnDeath &&
                        (!string.IsNullOrEmpty(
                            death.MedicalResponsibilityId) ||
                         admission.DischargePolicyId !=
                            MilitaryRearMedicalDischargePolicyIds
                                .MedicalRetirementAtCareSite ||
                         evacuation.PatientReturnPolicyId !=
                            MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .RemainAtCareSiteForMedicalRetirement) ||
                     patientReturnDeath &&
                         (!hasResponsibility || !hasResponsibilitySite ||
                         !hasResponsiblePhysician ||
                         !hasPatientReturnClosure ||
                         !hasPatientReturnPolicy ||
                         !usedResponsibilities.Add(
                             death.MedicalResponsibilityId) ||
                         !usedPatientReturnDeathClosures.Add(
                             death.PatientReturnDeathClosureId) ||
                          death.Day <
                              (patientAwaitingTeamDeath
                                  ? MilitaryPatientArrivalWaitingTeamDeathContractActivationDay
                                  : MilitaryPatientReturnDeathContractActivationDay) ||
                          death.DeathLocationId !=
                              (patientAwaitingTeamDeath
                                  ? evacuation.ReturnDestinationLocationId
                                  : string.Empty) ||
                         admission.Status !=
                             (evacuation.Status ==
                                  MilitaryMedicalEvacuationStatus.Completed
                                 ? MilitaryRearMedicalAdmissionStatus.Completed
                                 : MilitaryRearMedicalAdmissionStatus.Discharged) ||
                         admission.PatientReturnDeathClosureId !=
                             patientReturnClosure.Id ||
                         evacuation.PatientReturnDeathClosureId !=
                             patientReturnClosure.Id ||
                          evacuation.PatientReturnPolicyId !=
                              (patientAwaitingTeamDeath
                                  ? MilitaryMedicalEvacuationPatientReturnPolicyIds
                                      .CorpseAtArmyAwaitingTeamRejoin
                                  : MilitaryMedicalEvacuationPatientReturnPolicyIds
                                      .ReturnCorpseWithTeam) ||
                         responsibility.Day != death.Day ||
                         responsibility.WoundDeathId != death.Id ||
                         responsibility.DeathContextId != death.DeathContextId ||
                         responsibility.ResponsibilityPolicyId !=
                             MilitaryMedicalDeathResponsibilityPolicyIds
                                 .LastCareTeamDuringAuthorizedReturn ||
                         responsibility.AdmissionId != admission.Id ||
                         responsibility.EvacuationId != evacuation.Id ||
                         responsibility.InjuryEpisodeId != injury.Id ||
                         responsibility.PatientPersonId != patient.Id ||
                         responsibility.RearMedicalSiteId !=
                             admission.RearMedicalSiteId ||
                         responsibility.CareOrganizationId !=
                             responsibilitySite.OwnerOrganizationId ||
                         !string.IsNullOrEmpty(responsibility.SourceArmyId) ||
                         responsibility.ResponsiblePhysicianPersonId !=
                             admission.PhysicianPersonId ||
                         responsibility.ResponsiblePhysicianMedicalSkillBasisPoints <
                             MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints ||
                         responsibility.ResponsiblePhysicianMedicalSkillBasisPoints >
                             10_000 ||
                         responsibility.AuthorizingPersonId !=
                             death.AuthorizingPersonId ||
                         responsibility.AuthorizingAuthority !=
                             death.AuthorizingAuthority ||
                         patientReturnClosure.Day != death.Day ||
                         patientReturnClosure.WoundDeathId != death.Id ||
                         patientReturnClosure.AdmissionId != admission.Id ||
                         patientReturnClosure.EvacuationId != evacuation.Id ||
                         patientReturnClosure.InjuryEpisodeId != injury.Id ||
                         patientReturnClosure.PatientPersonId != patient.Id ||
                         patientReturnClosure.PatientMilitaryServiceId !=
                             service.Id ||
                         patientReturnClosure.SourceArmyId != army.Id ||
                         patientReturnClosure.SourceRearMedicalSiteId !=
                             admission.RearMedicalSiteId ||
                         patientReturnClosure.SourcePhysicianPersonId !=
                             admission.PhysicianPersonId ||
                         patientReturnClosure.ReturnRouteId !=
                             evacuation.ReturnRouteId ||
                         patientReturnClosure.ReturnOriginLocationId !=
                             evacuation.CurrentCareLocationId ||
                         patientReturnClosure.ReturnDestinationLocationId !=
                             evacuation.ReturnDestinationLocationId ||
                         patientReturnClosure.PatientReturnJourneyId !=
                             evacuation.PatientReturnJourneyId ||
                          patientReturnClosure.ReturnStartedDay !=
                              evacuation.ReturnStartedDay ||
                          patientReturnClosure
                              .PatientJourneyCompletedBeforeDeath !=
                                  patientAwaitingTeamDeath ||
                          patientAwaitingTeamDeath &&
                              patientReturnClosure
                                  .RemainingKilometersAtDeath != 0 ||
                          !patientAwaitingTeamDeath &&
                              patientReturnClosure
                                  .RemainingKilometersAtDeath <= 0 ||
                          !validPatientReturnTeamSnapshots ||
                         patientReturnClosure.OpeningHealthBasisPoints < 0 ||
                         patientReturnClosure.OpeningHealthBasisPoints >
                             10_000 ||
                         patientReturnClosure.HealthLossBasisPoints !=
                             patientReturnPolicy.HealthLossBasisPoints ||
                         patientReturnClosure.ClosingHealthBasisPoints !=
                             Math.Max(
                                 0,
                                 patientReturnClosure
                                     .OpeningHealthBasisPoints -
                                 patientReturnClosure
                                     .HealthLossBasisPoints) ||
                         patientReturnClosure.ClosingHealthBasisPoints !=
                             death.HealthAtDeathBasisPoints ||
                         patientReturnClosure.ClosingHealthBasisPoints >
                             patientReturnPolicy
                                 .MaximumClosingHealthBasisPoints ||
                         injury.SeverityBasisPoints <
                             patientReturnPolicy
                                 .MinimumSeverityBasisPoints ||
                         death.Day < checked(
                             evacuation.ReturnStartedDay +
                             patientReturnPolicy
                                 .MinimumDaysAfterReturnStart) ||
                          evacuation.Status ==
                              MilitaryMedicalEvacuationStatus
                                  .PatientDeceasedReturningToArmy &&
                              (patientAwaitingTeamDeath ||
                               currentPatientReturnJourney == null ||
                               currentPatientReturnJourney.PersonId !=
                                   patient.Id ||
                              currentPatientReturnJourney.RemainingKilometers <=
                                  0 ||
                              currentPatientReturnJourney.RemainingKilometers >
                                  patientReturnClosure
                                       .RemainingKilometersAtDeath) ||
                          evacuation.Status ==
                              MilitaryMedicalEvacuationStatus
                                  .PatientDeceasedAwaitingTeamRejoin &&
                              (!patientAwaitingTeamDeath ||
                               currentPatientReturnJourney != null ||
                               patient.LocationId !=
                                   evacuation.ReturnDestinationLocationId) ||
                          evacuation.Status ==
                              MilitaryMedicalEvacuationStatus.Completed &&
                             (currentPatientReturnJourney != null ||
                              patient.LocationId !=
                                  evacuation.ReturnDestinationLocationId)) ||
                    careSiteDeath &&
                        (!hasResponsibility || !hasResponsibilitySite ||
                         !hasResponsiblePhysician ||
                         !usedResponsibilities.Add(
                            death.MedicalResponsibilityId) ||
                         death.Day <
                            MilitaryMedicalDeathResponsibilityContractActivationDay ||
                         admission.DischargePolicyId !=
                            MilitaryRearMedicalDischargePolicyIds
                                .DeathAtCareSite ||
                         evacuation.PatientReturnPolicyId !=
                            MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .RemainAtCareSiteAfterDeath ||
                         responsibility.Day != death.Day ||
                         responsibility.WoundDeathId != death.Id ||
                         responsibility.DeathContextId != death.DeathContextId ||
                         responsibility.ResponsibilityPolicyId !=
                            MilitaryMedicalDeathResponsibilityPolicyIds
                                .CurrentCareTeamDocumented ||
                         responsibility.AdmissionId != admission.Id ||
                         responsibility.EvacuationId != evacuation.Id ||
                         responsibility.InjuryEpisodeId != injury.Id ||
                         responsibility.PatientPersonId != patient.Id ||
                         responsibility.RearMedicalSiteId !=
                            admission.RearMedicalSiteId ||
                         responsibility.CareOrganizationId !=
                            responsibilitySite.OwnerOrganizationId ||
                         responsibility.CareOrganizationId !=
                            organization.Id ||
                         !string.IsNullOrEmpty(
                            responsibility.SourceArmyId) ||
                         responsibility.ResponsiblePhysicianPersonId !=
                            admission.PhysicianPersonId ||
                         responsibility.ResponsiblePhysicianMedicalSkillBasisPoints <
                            MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints ||
                         responsibility.ResponsiblePhysicianMedicalSkillBasisPoints >
                            10000 ||
                         responsibility.AuthorizingPersonId !=
                            death.AuthorizingPersonId ||
                         responsibility.AuthorizingAuthority !=
                            death.AuthorizingAuthority) ||
                    transferDeath &&
                        (!hasResponsibility || !hasTransferClosure ||
                         !hasDeteriorationPolicy ||
                         !sites.TryGetValue(
                             transferClosure.SourceRearMedicalSiteId,
                             out var transferResponsibilitySite) ||
                         !people.ContainsKey(
                             transferClosure.SourcePhysicianPersonId) ||
                         !usedResponsibilities.Add(
                             death.MedicalResponsibilityId) ||
                         !usedTransferDeathClosures.Add(
                             death.MedicalTransferDeathClosureId) ||
                         death.Day <
                            MilitaryMedicalTransferDeathContractActivationDay ||
                         admission.DischargePolicyId !=
                            MilitaryRearMedicalDischargePolicyIds
                                .DeathDuringMedicalTransfer ||
                         evacuation.PatientReturnPolicyId !=
                            MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .RemainAtCareSiteAfterDeath ||
                         responsibility.Day != death.Day ||
                         responsibility.WoundDeathId != death.Id ||
                         responsibility.DeathContextId != death.DeathContextId ||
                         responsibility.ResponsibilityPolicyId !=
                            MilitaryMedicalDeathResponsibilityPolicyIds
                                .SourceCareUntilTransferHandoff ||
                         responsibility.AdmissionId != admission.Id ||
                         responsibility.EvacuationId != evacuation.Id ||
                         responsibility.InjuryEpisodeId != injury.Id ||
                         responsibility.PatientPersonId != patient.Id ||
                         responsibility.RearMedicalSiteId !=
                            transferClosure.SourceRearMedicalSiteId ||
                         responsibility.CareOrganizationId !=
                            transferResponsibilitySite.OwnerOrganizationId ||
                         responsibility.CareOrganizationId != organization.Id ||
                         !string.IsNullOrEmpty(
                            responsibility.SourceArmyId) ||
                         responsibility.ResponsiblePhysicianPersonId !=
                            transferClosure.SourcePhysicianPersonId ||
                         responsibility.AuthorizingPersonId !=
                            death.AuthorizingPersonId ||
                         responsibility.AuthorizingAuthority !=
                            death.AuthorizingAuthority ||
                         transferClosure.Day != death.Day ||
                         transferClosure.WoundDeathId != death.Id ||
                         transferClosure.MedicalTransferId !=
                            admission.MedicalTransferId ||
                         transferClosure.AdmissionId != admission.Id ||
                         transferClosure.EvacuationId != evacuation.Id ||
                         transferClosure.InjuryEpisodeId != injury.Id ||
                         transferClosure.PatientPersonId != patient.Id ||
                         !medicalTransfers.TryGetValue(
                             transferClosure.MedicalTransferId,
                             out var closureDeathTransfer) ||
                         closureDeathTransfer.DeathClosureId !=
                            transferClosure.Id ||
                         closureDeathTransfer.Status !=
                                MilitaryMedicalTransferStatus
                                    .DeceasedInTransit &&
                            closureDeathTransfer.Status !=
                                MilitaryMedicalTransferStatus
                                    .ClosedAfterPatientDeath ||
                         transferClosure.SourceRearMedicalSiteId !=
                            closureDeathTransfer.SourceRearMedicalSiteId ||
                         transferClosure.DestinationRearMedicalSiteId !=
                            closureDeathTransfer
                                .DestinationRearMedicalSiteId ||
                         transferClosure.SourcePhysicianPersonId !=
                            closureDeathTransfer.SourcePhysicianPersonId ||
                         transferClosure.DesignatedReceivingPersonId !=
                            closureDeathTransfer
                                .DesignatedReceivingPersonId ||
                         transferClosure.RouteId !=
                            closureDeathTransfer.RouteId ||
                         transferClosure.OccurredInTransit &&
                            (transferClosure.RemainingKilometersAtDeath <= 0 ||
                             !string.IsNullOrEmpty(death.DeathLocationId)) ||
                         !transferClosure.OccurredInTransit &&
                            (transferClosure.RemainingKilometersAtDeath != 0 ||
                             !sites.TryGetValue(
                                 transferClosure
                                    .DestinationRearMedicalSiteId,
                                 out var transferDestinationSite) ||
                             death.DeathLocationId !=
                                 transferDestinationSite.LocationId) ||
                         transferClosure.AuthorizingPersonId !=
                            death.AuthorizingPersonId ||
                         transferClosure.AuthorizingAuthority !=
                            death.AuthorizingAuthority ||
                         transferClosure.OpeningHealthBasisPoints !=
                            ExpectedAdmissionHealthBeforeDeath(
                                admission, injury) ||
                         transferClosure.HealthLossBasisPoints !=
                            deteriorationPolicy.HealthLossBasisPoints ||
                         transferClosure.ClosingHealthBasisPoints != Math.Max(
                            0,
                            transferClosure.OpeningHealthBasisPoints -
                            transferClosure.HealthLossBasisPoints) ||
                         transferClosure.ClosingHealthBasisPoints !=
                            death.HealthAtDeathBasisPoints ||
                         transferClosure.ClosingHealthBasisPoints >
                            deteriorationPolicy
                                .MaximumClosingHealthBasisPoints ||
                         injury.SeverityBasisPoints <
                            deteriorationPolicy.MinimumSeverityBasisPoints ||
                         death.Day < checked(
                            admission.AdmittedDay +
                            deteriorationPolicy.MinimumDaysAfterAdmission) ||
                         admission.MedicalTransferDeathClosureId !=
                            transferClosure.Id ||
                         admission.ReadyForReturnDay != death.Day ||
                         admission.DischargedDay != death.Day) ||
                    originalEvacuationDeath &&
                        (!hasResponsibility ||
                         !hasOriginalEvacuationClosure ||
                         !hasOriginalEvacuationPolicy ||
                         !usedResponsibilities.Add(
                            death.MedicalResponsibilityId) ||
                         !usedOriginalEvacuationDeathClosures.Add(
                            death.OriginalEvacuationDeathClosureId) ||
                         death.Day <
                            MilitaryOriginalEvacuationDeathContractActivationDay ||
                         responsibility.Day != death.Day ||
                         responsibility.WoundDeathId != death.Id ||
                         responsibility.DeathContextId != death.DeathContextId ||
                         responsibility.ResponsibilityPolicyId !=
                            MilitaryMedicalDeathResponsibilityPolicyIds
                                .SourceArmyUntilRearHandoff ||
                         !string.IsNullOrEmpty(responsibility.AdmissionId) ||
                         responsibility.EvacuationId != evacuation.Id ||
                         !string.IsNullOrEmpty(
                            responsibility.InjuryEpisodeId) ||
                         responsibility.PatientPersonId != patient.Id ||
                         !string.IsNullOrEmpty(
                            responsibility.RearMedicalSiteId) ||
                         responsibility.CareOrganizationId != organization.Id ||
                         responsibility.SourceArmyId != army.Id ||
                         !string.IsNullOrEmpty(
                            responsibility.ResponsiblePhysicianPersonId) ||
                         responsibility
                            .ResponsiblePhysicianMedicalSkillBasisPoints != 0 ||
                         responsibility.AuthorizingPersonId !=
                            death.AuthorizingPersonId ||
                         responsibility.AuthorizingAuthority !=
                            death.AuthorizingAuthority ||
                         originalEvacuationClosure.Day != death.Day ||
                         originalEvacuationClosure.WoundDeathId != death.Id ||
                         originalEvacuationClosure.EvacuationId !=
                            evacuation.Id ||
                         originalEvacuationClosure.PatientPersonId !=
                            patient.Id ||
                         originalEvacuationClosure.PatientMilitaryServiceId !=
                            service.Id ||
                         originalEvacuationClosure.SourceArmyId != army.Id ||
                         originalEvacuationClosure.SourceOrganizationId !=
                            organization.Id ||
                         originalEvacuationClosure
                            .EvacuationAuthorizingPersonId !=
                            evacuation.AuthorizingPersonId ||
                         originalEvacuationClosure
                            .EvacuationAuthorizingAuthority !=
                            evacuation.AuthorizingAuthority ||
                         originalEvacuationClosure.DeathAuthorizingPersonId !=
                            death.AuthorizingPersonId ||
                         originalEvacuationClosure
                            .DeathAuthorizingAuthority !=
                            death.AuthorizingAuthority ||
                         originalEvacuationClosure.OriginLocationId !=
                            evacuation.OriginLocationId ||
                         originalEvacuationClosure.DestinationLocationId !=
                            evacuation.DestinationLocationId ||
                         originalEvacuationClosure
                            .DesignatedReceivingPersonId !=
                            evacuation.DesignatedReceivingPersonId ||
                         originalEvacuationClosure.RouteId !=
                            evacuation.RouteId ||
                         originalEvacuationClosure.OccurredInTransit &&
                            (originalEvacuationClosure
                                 .RemainingKilometersAtDeath <= 0 ||
                             !string.IsNullOrEmpty(death.DeathLocationId)) ||
                         !originalEvacuationClosure.OccurredInTransit &&
                            (originalEvacuationClosure
                                 .RemainingKilometersAtDeath != 0 ||
                             death.DeathLocationId !=
                                 evacuation.DestinationLocationId) ||
                         originalEvacuationClosure.OpeningHealthBasisPoints < 0 ||
                         originalEvacuationClosure.OpeningHealthBasisPoints >
                            originalEvacuationPolicy
                                .MaximumOpeningHealthBasisPoints ||
                         originalEvacuationClosure.HealthLossBasisPoints !=
                            originalEvacuationPolicy.HealthLossBasisPoints ||
                         originalEvacuationClosure.ClosingHealthBasisPoints !=
                            Math.Max(
                                0,
                                originalEvacuationClosure
                                    .OpeningHealthBasisPoints -
                                originalEvacuationClosure
                                    .HealthLossBasisPoints) ||
                         originalEvacuationClosure.ClosingHealthBasisPoints >
                            originalEvacuationPolicy
                                .MaximumClosingHealthBasisPoints ||
                         originalEvacuationClosure.ClosingHealthBasisPoints !=
                            death.HealthAtDeathBasisPoints ||
                         originalEvacuationClosure.DerivedSeverityBasisPoints !=
                            10_000 - originalEvacuationClosure
                                .OpeningHealthBasisPoints ||
                         originalEvacuationClosure.DerivedSeverityBasisPoints !=
                            death.SeverityBasisPoints ||
                         death.Day < checked(
                            evacuation.CreatedDay +
                            originalEvacuationPolicy
                                .MinimumDaysAfterDispatch) ||
                         evacuation.OriginalEvacuationDeathClosureId !=
                            originalEvacuationClosure.Id ||
                         evacuation.PatientReturnPolicyId !=
                            MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .RemainAtCareSiteAfterDeath ||
                         !string.IsNullOrEmpty(
                            evacuation.ReceivingPersonId) ||
                         evacuation.ReceivedDay != -1 ||
                         evacuation.ReceivingMedicalSkillBasisPoints != 0 ||
                         !string.IsNullOrEmpty(
                            evacuation.RearMedicalSiteId) ||
                         !string.IsNullOrEmpty(
                            evacuation.RearMedicalAdmissionId)) ||
                    inpatientDeath &&
                        (!hasInpatientClosure ||
                         !hasDeteriorationPolicy ||
                         !usedInpatientClosures.Add(
                            death.InpatientDeathClosureId) ||
                         death.Day <
                            MilitaryInpatientDeathContractActivationDay ||
                         inpatientClosure.Day != death.Day ||
                         inpatientClosure.WoundDeathId != death.Id ||
                         inpatientClosure.AdmissionId != admission.Id ||
                         inpatientClosure.EvacuationId != evacuation.Id ||
                         inpatientClosure.InjuryEpisodeId != injury.Id ||
                         inpatientClosure.PatientPersonId != patient.Id ||
                         inpatientClosure.RearMedicalSiteId !=
                            admission.RearMedicalSiteId ||
                         inpatientClosure.PhysicianPersonId !=
                            admission.PhysicianPersonId ||
                         inpatientClosure.CompletedTreatmentStagesAtDeath !=
                            admission.CompletedTreatmentStages ||
                         inpatientClosure.RequiredTreatmentStagesAtDeath !=
                            admission.RequiredTreatmentStages ||
                         inpatientClosure.CompletedTreatmentStagesAtDeath < 0 ||
                         inpatientClosure.CompletedTreatmentStagesAtDeath >=
                            inpatientClosure.RequiredTreatmentStagesAtDeath ||
                         inpatientClosure.NextTreatmentProtocolId !=
                            admission.TreatmentPlanProtocolIds[
                                admission.CompletedTreatmentStages] ||
                         inpatientClosure.OpeningHealthBasisPoints !=
                            ExpectedAdmissionHealthBeforeDeath(
                                admission, injury) ||
                         inpatientClosure.OpeningHealthBasisPoints < 0 ||
                         inpatientClosure.OpeningHealthBasisPoints > 10_000 ||
                         inpatientClosure.HealthLossBasisPoints !=
                            deteriorationPolicy.HealthLossBasisPoints ||
                         inpatientClosure.ClosingHealthBasisPoints != Math.Max(
                            0,
                            inpatientClosure.OpeningHealthBasisPoints -
                            inpatientClosure.HealthLossBasisPoints) ||
                         inpatientClosure.ClosingHealthBasisPoints !=
                            death.HealthAtDeathBasisPoints ||
                         inpatientClosure.ClosingHealthBasisPoints >
                            deteriorationPolicy
                                .MaximumClosingHealthBasisPoints ||
                         injury.SeverityBasisPoints <
                            deteriorationPolicy.MinimumSeverityBasisPoints ||
                         death.Day < checked(
                            admission.AdmittedDay +
                            deteriorationPolicy.MinimumDaysAfterAdmission) ||
                         admission.InpatientDeathClosureId !=
                            inpatientClosure.Id ||
                         admission.ReadyForReturnDay != death.Day ||
                         admission.DischargedDay != death.Day ||
                         string.IsNullOrEmpty(
                                inpatientClosure.MedicalTransferId) &&
                            (!string.IsNullOrEmpty(
                                inpatientClosure.ReservedMedicineBatchId) ||
                             inpatientClosure
                                .ReservedMedicineUnitsBeforeRelease != 0 ||
                             inpatientClosure
                                .ReleasedReservedMedicineUnits != 0 ||
                             inpatientClosure
                                .ReservedMedicineUnitsAfterRelease != 0 ||
                             !string.IsNullOrEmpty(
                                inpatientClosure
                                    .ReservationReleaseInventoryTransactionId)) ||
                         !string.IsNullOrEmpty(
                                inpatientClosure.MedicalTransferId) &&
                            (!hasClosureTransfer || !hasClosureBatch ||
                             closureTransfer.Status !=
                                MilitaryMedicalTransferStatus.Completed ||
                             closureTransfer.Id != admission.MedicalTransferId ||
                             closureTransfer.ReservedMedicineBatchId !=
                                closureBatch.Id ||
                             inpatientClosure.ReservedMedicineBatchId !=
                                closureBatch.Id ||
                             inpatientClosure.ReleasedReservedMedicineUnits !=
                                closureTransfer.ReleasedReservedMedicineUnits ||
                             inpatientClosure.ReleasedReservedMedicineUnits !=
                                closureTransfer.ReservedMedicineUnits -
                                closureTransfer
                                    .ConsumedReservedMedicineUnits ||
                             inpatientClosure.ReservedMedicineUnitsBeforeRelease <
                                inpatientClosure
                                    .ReleasedReservedMedicineUnits ||
                             inpatientClosure.ReservedMedicineUnitsAfterRelease !=
                                inpatientClosure
                                    .ReservedMedicineUnitsBeforeRelease -
                                inpatientClosure
                                    .ReleasedReservedMedicineUnits ||
                             (inpatientClosure
                                .ReleasedReservedMedicineUnits > 0) !=
                                hasClosureReleaseTransaction ||
                             closureTransfer
                                .ReservationReleaseInventoryTransactionId !=
                                inpatientClosure
                                    .ReservationReleaseInventoryTransactionId ||
                             hasClosureReleaseTransaction &&
                                (closureReleaseTransaction.Type !=
                                    InventoryTransactionType
                                        .MilitaryMedicalTransferMedicineReleased ||
                                 closureReleaseTransaction
                                    .SourceMilitaryMedicalTransferId !=
                                        closureTransfer.Id ||
                                 closureReleaseTransaction.ActorPersonId !=
                                    inpatientClosure.PhysicianPersonId ||
                                 closureReleaseTransaction.Day != death.Day ||
                                 closureReleaseTransaction.Lines == null ||
                                 closureReleaseTransaction.Lines.Count != 1 ||
                                 closureReleaseTransaction.Lines[0].BatchId !=
                                    closureBatch.Id ||
                                 closureReleaseTransaction.Lines[0]
                                    .QuantityDelta != 0 ||
                                 closureReleaseTransaction.Lines[0]
                                    .ReservedQuantityDelta !=
                                        -inpatientClosure
                                            .ReleasedReservedMedicineUnits))) ||
                    !people.ContainsKey(death.AuthorizingPersonId) ||
                    death.AuthorizingAuthority < MilitaryAuthorityLevel.Army ||
                    !Enum.IsDefined(
                        typeof(MilitaryAuthorityLevel),
                        death.AuthorizingAuthority) ||
                    !family.MemberIds.Contains(patient.Id) ||
                    inheritance.WoundDeathId != death.Id ||
                    !string.IsNullOrEmpty(inheritance.ReturnTeamDeathId) ||
                    inheritance.Day != death.Day ||
                    inheritance.FamilyId != family.Id ||
                    inheritance.DeceasedPersonId != patient.Id ||
                    !family.MemberIds.Contains(
                        inheritance.FormerHeadPersonId) ||
                    !family.MemberIds.Contains(
                        inheritance.SuccessorPersonId) ||
                    inheritance.SuccessorPersonId == patient.Id ||
                    inheritance.HeadChanged !=
                        (inheritance.FormerHeadPersonId == patient.Id) ||
                    !inheritance.HeadChanged &&
                        inheritance.SuccessorPersonId !=
                            inheritance.FormerHeadPersonId ||
                    inheritance.DeceasedWealthBefore < 0 ||
                    inheritance.DeceasedWealthAfter != 0 ||
                    inheritance.FamilyWealthAfter != checked(
                        inheritance.FamilyWealthBefore +
                        inheritance.DeceasedWealthBefore) ||
                    compensation.WoundDeathId != death.Id ||
                    !string.IsNullOrEmpty(compensation.ReturnTeamDeathId) ||
                    compensation.Day != death.Day ||
                    compensation.PolicyId != policy.Id ||
                    compensation.ArmyId != army.Id ||
                    compensation.OrganizationId != organization.Id ||
                    compensation.FamilyId != family.Id ||
                    compensation.DeceasedPersonId != patient.Id ||
                    compensation.AuthorizingPersonId !=
                        death.AuthorizingPersonId ||
                    compensation.AuthorizingAuthority !=
                        death.AuthorizingAuthority ||
                    compensation.MilitaryRankAtDeath != service.Rank ||
                    compensation.Amount != expectedCompensation ||
                    compensation.OrganizationTreasuryBefore <
                        compensation.Amount ||
                    compensation.OrganizationTreasuryAfter != checked(
                        compensation.OrganizationTreasuryBefore -
                        compensation.Amount) ||
                    compensation.FamilyWealthBefore !=
                        inheritance.FamilyWealthAfter ||
                    compensation.FamilyWealthAfter != checked(
                        compensation.FamilyWealthBefore +
                        compensation.Amount) ||
                    deathEvent.Type != LifeEventType.Death ||
                    deathEvent.Day != death.Day ||
                    deathEvent.PrimaryPersonId != patient.Id ||
                    deathEvent.FamilyId != family.Id ||
                    headChanged !=
                        !string.IsNullOrEmpty(death.SuccessionLifeEventId) ||
                    headChanged &&
                        (!hasSuccessionEvent ||
                         successionEvent.Type != LifeEventType.Succession ||
                         successionEvent.Day != death.Day ||
                         successionEvent.PrimaryPersonId !=
                            inheritance.SuccessorPersonId ||
                         successionEvent.SecondaryPersonId != patient.Id ||
                         successionEvent.FamilyId != family.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military wound death {death.Id}: " +
                        $"context={death.DeathContextId}, phase={validDeathPhase}, " +
                        $"responsibility={hasResponsibility}, inpatientClosure=" +
                        $"{hasInpatientClosure}, transferClosure=" +
                        $"{hasTransferClosure}, policy={hasPolicy}, " +
                        $"deterioration={hasDeteriorationPolicy}, " +
                        $"admission={admission?.Status}, evacuation=" +
                        $"{evacuation?.Status}, service={service?.Status}, " +
                        $"patientAlive={patient?.IsAlive}, patientHealth=" +
                        $"{patient?.HealthBasisPoints}, deathHealth=" +
                        $"{death.HealthAtDeathBasisPoints}.");
                }
            }
            ValidateMilitaryReturnTeamDeaths(
                returnTeamDeathPolicies,
                evacuations,
                armies,
                organizations,
                families,
                people,
                services,
                lifeEvents,
                inheritances,
                compensations,
                deathIds,
                deathPatients,
                usedInheritances,
                usedCompensations);
            if (usedInheritances.Count != inheritances.Count ||
                usedCompensations.Count != compensations.Count ||
                usedResponsibilities.Count != responsibilities.Count ||
                usedInpatientClosures.Count != inpatientClosures.Count ||
                usedTransferDeathClosures.Count !=
                    transferDeathClosures.Count ||
                usedOriginalEvacuationDeathClosures.Count !=
                    originalEvacuationDeathClosures.Count ||
                usedPatientReturnDeathClosures.Count !=
                    patientReturnDeathClosures.Count)
            {
                throw new InvalidOperationException(
                    "Military wound-death ledgers contain orphan records.");
            }
        }

        private void ValidateMilitaryReturnTeamDeaths(
            Dictionary<string, MilitaryReturnTeamDeathPolicyDefinitionState>
                policies,
            Dictionary<string, MilitaryMedicalEvacuationState> evacuations,
            Dictionary<string, ArmyState> armies,
            Dictionary<string, OrganizationState> organizations,
            Dictionary<string, FamilyState> families,
            Dictionary<string, PersonState> people,
            Dictionary<string, MilitaryServiceState> services,
            Dictionary<string, LifeEventRecordState> lifeEvents,
            Dictionary<string, MilitaryFamilyInheritanceState> inheritances,
            Dictionary<string, MilitarySurvivorCompensationState>
                compensations,
            HashSet<string> deathIds,
            HashSet<string> deathPeople,
            HashSet<string> usedInheritances,
            HashSet<string> usedCompensations)
        {
            for (var i = 0; i < MilitaryReturnTeamDeaths.Count; i++)
            {
                var death = MilitaryReturnTeamDeaths[i] ??
                    throw new InvalidOperationException(
                        "A military return-team death cannot be null.");
                _ = new StableId(death.Id);
                _ = new StableId(death.PolicyId);
                _ = new StableId(death.CorpsePolicyId);
                _ = new StableId(death.ReturnJourneyId);
                var hasPolicy = policies.TryGetValue(
                    death.PolicyId, out var policy);
                var hasEvacuation = evacuations.TryGetValue(
                    death.EvacuationId, out var evacuation);
                var hasPerson = people.TryGetValue(
                    death.PersonId, out var person);
                var hasService = services.TryGetValue(
                    death.MilitaryServiceId, out var service);
                var hasArmy = armies.TryGetValue(
                    death.SourceArmyId, out var army);
                var hasOrganization = organizations.TryGetValue(
                    death.OrganizationId, out var organization);
                var hasFamily = families.TryGetValue(
                    death.FamilyId, out var family);
                var hasInheritance = inheritances.TryGetValue(
                    death.FamilyInheritanceId, out var inheritance);
                var hasCompensation = compensations.TryGetValue(
                    death.SurvivorCompensationId, out var compensation);
                var hasDeathEvent = lifeEvents.TryGetValue(
                    death.DeathLifeEventId, out var deathEvent);
                LifeEventRecordState successionEvent = null;
                var hasSuccessionEvent = !string.IsNullOrEmpty(
                        death.SuccessionLifeEventId) &&
                    lifeEvents.TryGetValue(
                        death.SuccessionLifeEventId, out successionEvent);
                var member = hasEvacuation && evacuation.TeamMembers != null
                    ? evacuation.TeamMembers.Find(item =>
                        item.PersonId == death.PersonId)
                    : null;
                var journey = FindJourneyById(death.ReturnJourneyId);
                var inReturnPhase = hasEvacuation &&
                    (evacuation.Status ==
                         MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                     evacuation.Status == MilitaryMedicalEvacuationStatus
                         .PatientDeceasedReturningToArmy ||
                     evacuation.Status == MilitaryMedicalEvacuationStatus
                         .PatientDeceasedAwaitingTeamRejoin ||
                     evacuation.Status ==
                         MilitaryMedicalEvacuationStatus.Completed);
                var expectedCompensation = hasPolicy && hasService
                    ? checked(
                        policy.BaseCompensationMoney +
                        policy.CompensationPerRankMoney * service.Rank)
                    : -1;
                var headChanged = hasInheritance &&
                    inheritance.FormerHeadPersonId == death.PersonId;
                if (!deathIds.Add(death.Id) ||
                    !deathPeople.Add(death.PersonId) ||
                    !usedInheritances.Add(death.FamilyInheritanceId) ||
                    !usedCompensations.Add(death.SurvivorCompensationId) ||
                    !hasPolicy || !hasEvacuation || !hasPerson ||
                    !hasService || !hasArmy || !hasOrganization ||
                    !hasFamily || !hasInheritance || !hasCompensation ||
                    !hasDeathEvent || member == null || !inReturnPhase ||
                    death.Day < MilitaryReturnTeamDeathContractActivationDay ||
                    death.Day > AbsoluteDay ||
                    death.CorpsePolicyId !=
                        MilitaryReturnTeamCorpsePolicyIds
                            .ContinueExistingJourneyToSourceArmy ||
                    member.ReturnDeathId != death.Id ||
                    member.MilitaryServiceId != service.Id ||
                    member.ReturnJourneyId != death.ReturnJourneyId ||
                    service.PersonId != person.Id ||
                    service.ArmyId != army.Id ||
                    service.Status != MilitaryServiceStatus.Dead ||
                    army.OrganizationId != organization.Id ||
                    death.ReturnRouteId != evacuation.ReturnRouteId ||
                    death.ReturnOriginLocationId !=
                        evacuation.CurrentCareLocationId ||
                    death.ReturnDestinationLocationId !=
                        evacuation.ReturnDestinationLocationId ||
                    death.ReturnStartedDay != evacuation.ReturnStartedDay ||
                    death.Day < checked(
                        death.ReturnStartedDay +
                        policy.MinimumDaysAfterReturnStart) ||
                    death.RemainingKilometersAtDeath <= 0 ||
                    death.OpeningHealthBasisPoints < 0 ||
                    death.OpeningHealthBasisPoints > 10_000 ||
                    death.HealthLossBasisPoints !=
                        policy.HealthLossBasisPoints ||
                    death.ClosingHealthBasisPoints != Math.Max(
                        0,
                        death.OpeningHealthBasisPoints -
                        death.HealthLossBasisPoints) ||
                    death.ClosingHealthBasisPoints >
                        policy.MaximumClosingHealthBasisPoints ||
                    person.IsAlive ||
                    person.HealthBasisPoints !=
                        death.ClosingHealthBasisPoints ||
                    person.Wealth != 0 ||
                    journey != null &&
                        (death.CorpseArrivedDay != -1 ||
                         journey.PersonId != person.Id ||
                         journey.RouteId != death.ReturnRouteId ||
                         journey.OriginLocationId !=
                            death.ReturnOriginLocationId ||
                         journey.DestinationLocationId !=
                            death.ReturnDestinationLocationId ||
                         journey.Mode != TravelMode.Foot ||
                         journey.RemainingKilometers <= 0 ||
                         journey.RemainingKilometers >
                            death.RemainingKilometersAtDeath ||
                         person.LocationId !=
                            death.ReturnOriginLocationId) ||
                    journey == null &&
                        (person.LocationId !=
                             death.ReturnDestinationLocationId ||
                         death.CorpseArrivedDay < death.Day ||
                         death.CorpseArrivedDay > AbsoluteDay) ||
                    !people.ContainsKey(death.AuthorizingPersonId) ||
                    death.AuthorizingAuthority <
                        MilitaryAuthorityLevel.Army ||
                    !Enum.IsDefined(
                        typeof(MilitaryAuthorityLevel),
                        death.AuthorizingAuthority) ||
                    !family.MemberIds.Contains(person.Id) ||
                    inheritance.WoundDeathId != string.Empty ||
                    inheritance.ReturnTeamDeathId != death.Id ||
                    inheritance.Day != death.Day ||
                    inheritance.FamilyId != family.Id ||
                    inheritance.DeceasedPersonId != person.Id ||
                    !family.MemberIds.Contains(
                        inheritance.FormerHeadPersonId) ||
                    !family.MemberIds.Contains(
                        inheritance.SuccessorPersonId) ||
                    inheritance.SuccessorPersonId == person.Id ||
                    inheritance.HeadChanged != headChanged ||
                    !inheritance.HeadChanged &&
                        inheritance.SuccessorPersonId !=
                            inheritance.FormerHeadPersonId ||
                    inheritance.DeceasedWealthBefore < 0 ||
                    inheritance.DeceasedWealthAfter != 0 ||
                    inheritance.FamilyWealthAfter != checked(
                        inheritance.FamilyWealthBefore +
                        inheritance.DeceasedWealthBefore) ||
                    compensation.WoundDeathId != string.Empty ||
                    compensation.ReturnTeamDeathId != death.Id ||
                    compensation.Day != death.Day ||
                    compensation.PolicyId != policy.Id ||
                    compensation.ArmyId != army.Id ||
                    compensation.OrganizationId != organization.Id ||
                    compensation.FamilyId != family.Id ||
                    compensation.DeceasedPersonId != person.Id ||
                    compensation.AuthorizingPersonId !=
                        death.AuthorizingPersonId ||
                    compensation.AuthorizingAuthority !=
                        death.AuthorizingAuthority ||
                    compensation.MilitaryRankAtDeath != service.Rank ||
                    compensation.Amount != expectedCompensation ||
                    compensation.OrganizationTreasuryBefore <
                        compensation.Amount ||
                    compensation.OrganizationTreasuryAfter != checked(
                        compensation.OrganizationTreasuryBefore -
                        compensation.Amount) ||
                    compensation.FamilyWealthBefore !=
                        inheritance.FamilyWealthAfter ||
                    compensation.FamilyWealthAfter != checked(
                        compensation.FamilyWealthBefore +
                        compensation.Amount) ||
                    deathEvent.Type != LifeEventType.Death ||
                    deathEvent.Day != death.Day ||
                    deathEvent.PrimaryPersonId != person.Id ||
                    deathEvent.FamilyId != family.Id ||
                    headChanged != hasSuccessionEvent ||
                    headChanged &&
                        (successionEvent.Type != LifeEventType.Succession ||
                         successionEvent.Day != death.Day ||
                         successionEvent.PrimaryPersonId !=
                            inheritance.SuccessorPersonId ||
                         successionEvent.SecondaryPersonId != person.Id ||
                         successionEvent.FamilyId != family.Id))
                {
                    throw new InvalidOperationException(
                        $"Invalid military return-team death {death.Id}: " +
                        $"policy={hasPolicy}, evacuation={hasEvacuation}, " +
                        $"person={hasPerson}, service={service?.Status}, " +
                        $"member={member != null}, phase={inReturnPhase}, " +
                        $"day={death.Day}/{AbsoluteDay}, activation=" +
                        $"{MilitaryReturnTeamDeathContractActivationDay}, " +
                        $"journey={journey?.RemainingKilometers}, " +
                        $"snapshot={death.RemainingKilometersAtDeath}, " +
                        $"arrival={death.CorpseArrivedDay}, location=" +
                        $"{person?.LocationId}, destination=" +
                        $"{death.ReturnDestinationLocationId}, health=" +
                        $"{person?.HealthBasisPoints}/" +
                        $"{death.ClosingHealthBasisPoints}."
                    );
                }
            }
        }

        private void ValidateMilitaryMedicalTransfers(
            Dictionary<string, MilitaryMedicalTransferState> transfers,
            Dictionary<string, MilitaryRearMedicalAdmissionState> admissions,
            Dictionary<string, MilitaryMedicalEvacuationState> evacuations,
            Dictionary<string, MilitaryRearMedicalSiteState> sites,
            Dictionary<string, ArmyState> armies,
            Dictionary<string, PersonState> people,
            Dictionary<string, MilitaryServiceState> militaryServices,
            Dictionary<string, ProductBatchState> batches,
            Dictionary<string, InventoryTransactionState> transactions)
        {
            var transferSegments = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in transfers)
            {
                var transfer = pair.Value;
                _ = new StableId(transfer.Id);
                _ = new StableId(transfer.PatientJourneyId);
                MilitaryMedicalTransferState previousTransfer = null;
                var hasPreviousReference = !string.IsNullOrEmpty(
                    transfer.PreviousMedicalTransferId);
                var hasPreviousTransfer = hasPreviousReference &&
                    transfers.TryGetValue(
                        transfer.PreviousMedicalTransferId,
                        out previousTransfer);
                MilitaryMedicalTransferState nextTransfer = null;
                var hasNextReference = !string.IsNullOrEmpty(
                    transfer.NextMedicalTransferId);
                var hasNextTransfer = hasNextReference &&
                    transfers.TryGetValue(
                        transfer.NextMedicalTransferId,
                        out nextTransfer);
                var isLatestTransfer = !hasNextReference;
                var hasAdmission = admissions.TryGetValue(
                    transfer.AdmissionId, out var admission);
                var hasEvacuation = evacuations.TryGetValue(
                    transfer.EvacuationId, out var evacuation);
                var hasSourceSite = sites.TryGetValue(
                    transfer.SourceRearMedicalSiteId, out var sourceSite);
                var hasDestinationSite = sites.TryGetValue(
                    transfer.DestinationRearMedicalSiteId,
                    out var destinationSite);
                ArmyState army = null;
                var hasArmy = hasEvacuation && armies.TryGetValue(
                    evacuation.SourceArmyId, out army);
                PersonState patient = null;
                var hasPatient = hasAdmission && people.TryGetValue(
                    admission.PatientPersonId, out patient);
                var hasSourcePhysician = people.ContainsKey(
                    transfer.SourcePhysicianPersonId);
                var hasReceiver = people.ContainsKey(
                    transfer.DesignatedReceivingPersonId);
                var hasAuthorizer = people.ContainsKey(
                    transfer.AuthorizingPersonId);
                var hasBatch = batches.TryGetValue(
                    transfer.ReservedMedicineBatchId, out var batch);
                var hasTransaction = transactions.TryGetValue(
                    transfer.ReservationInventoryTransactionId,
                    out var transaction);
                InventoryTransactionState releaseTransaction = null;
                var hasReleaseTransaction = !string.IsNullOrEmpty(
                        transfer.ReservationReleaseInventoryTransactionId) &&
                    transactions.TryGetValue(
                        transfer.ReservationReleaseInventoryTransactionId,
                        out releaseTransaction);
                var route = FindRouteById(transfer.RouteId);
                var validRoute = hasSourceSite && hasDestinationSite &&
                    route != null &&
                    (route.FromLocationId == sourceSite.LocationId &&
                         route.ToLocationId == destinationSite.LocationId ||
                     route.Bidirectional &&
                         route.ToLocationId == sourceSite.LocationId &&
                         route.FromLocationId == destinationSite.LocationId);
                var expectedMedicineUnits = hasAdmission
                    ? RequiredMedicalTransferMedicineUnits(
                        admission,
                        transfer.CompletedTreatmentStagesAtDispatch)
                    : -1;
                var completed = transfer.Status ==
                    MilitaryMedicalTransferStatus.Completed;
                var deceasedInTransit = transfer.Status ==
                    MilitaryMedicalTransferStatus.DeceasedInTransit;
                var closedAfterDeath = transfer.Status ==
                    MilitaryMedicalTransferStatus.ClosedAfterPatientDeath;
                var deathClosed = deceasedInTransit || closedAfterDeath;
                var transferDeathClosure = string.IsNullOrEmpty(
                        transfer.DeathClosureId)
                    ? null
                    : MilitaryMedicalTransferDeathClosures.Find(item =>
                        item.Id == transfer.DeathClosureId);
                var arrived = transfer.Status !=
                        MilitaryMedicalTransferStatus.InTransit &&
                    !deceasedInTransit;
                var validReceipt = completed
                    ? transfer.ReceivingPersonId ==
                          transfer.DesignatedReceivingPersonId &&
                      transfer.ReceivingMedicalSkillBasisPoints >=
                          MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints &&
                      transfer.ReceivingMedicalSkillBasisPoints <= 10_000 &&
                      transfer.ReceivedDay >= transfer.ArrivedDay &&
                      transfer.ReceivedDay <= AbsoluteDay &&
                      transfer.ResponsibilityTransferredDay ==
                          transfer.ReceivedDay
                    : string.IsNullOrEmpty(transfer.ReceivingPersonId) &&
                      transfer.ReceivingMedicalSkillBasisPoints == 0 &&
                      transfer.ReceivedDay == -1 &&
                      transfer.ResponsibilityTransferredDay == -1;
                if (!hasAdmission || !hasEvacuation || !hasSourceSite ||
                    !hasDestinationSite || !hasArmy || !hasPatient ||
                    !hasSourcePhysician || !hasReceiver || !hasAuthorizer ||
                    !hasBatch || !hasTransaction || !validRoute ||
                    !transferSegments.Add(
                        transfer.AdmissionId + "|" + transfer.SequenceIndex) ||
                    sourceSite.Id == destinationSite.Id ||
                    sourceSite.OwnerOrganizationId !=
                        destinationSite.OwnerOrganizationId ||
                    transfer.SequenceIndex < 0 ||
                    transfer.SequenceIndex >= MilitaryMedicalRules
                        .MaximumMedicalTransfersPerAdmission ||
                    (transfer.SequenceIndex == 0) != !hasPreviousReference ||
                    hasPreviousReference != hasPreviousTransfer ||
                    hasNextReference != hasNextTransfer ||
                    isLatestTransfer !=
                        (admission.MedicalTransferId == transfer.Id) ||
                    hasPreviousTransfer &&
                        (previousTransfer.NextMedicalTransferId != transfer.Id ||
                         previousTransfer.SequenceIndex + 1 !=
                            transfer.SequenceIndex ||
                         previousTransfer.AdmissionId != transfer.AdmissionId ||
                         previousTransfer.EvacuationId != transfer.EvacuationId ||
                         previousTransfer.Status !=
                            MilitaryMedicalTransferStatus.Completed ||
                         previousTransfer.DestinationRearMedicalSiteId !=
                            transfer.SourceRearMedicalSiteId ||
                         previousTransfer.DesignatedReceivingPersonId !=
                            transfer.SourcePhysicianPersonId ||
                         transfer.CreatedDay < previousTransfer.ReceivedDay ||
                         transfer.CompletedTreatmentStagesAtDispatch <
                            previousTransfer
                                .CompletedTreatmentStagesAtDispatch) ||
                    hasNextTransfer &&
                        (nextTransfer.PreviousMedicalTransferId != transfer.Id ||
                         nextTransfer.SequenceIndex !=
                            transfer.SequenceIndex + 1 ||
                         nextTransfer.AdmissionId != transfer.AdmissionId ||
                         nextTransfer.EvacuationId != transfer.EvacuationId ||
                         nextTransfer.SourceRearMedicalSiteId !=
                            transfer.DestinationRearMedicalSiteId ||
                         nextTransfer.SourcePhysicianPersonId !=
                            transfer.DesignatedReceivingPersonId ||
                         nextTransfer.CreatedDay < transfer.ReceivedDay ||
                         nextTransfer.CompletedTreatmentStagesAtDispatch <
                            transfer.CompletedTreatmentStagesAtDispatch ||
                         transfer.Status !=
                            MilitaryMedicalTransferStatus.Completed) ||
                    transfer.SequenceIndex > 0 && transfer.CreatedDay <
                        MilitaryRepeatedMedicalTransferContractActivationDay ||
                    evacuation.RearMedicalAdmissionId != admission.Id ||
                    transfer.CompletedTreatmentStagesAtDispatch < 0 ||
                    transfer.CompletedTreatmentStagesAtDispatch >=
                        admission.RequiredTreatmentStages ||
                    transfer.CompletedTreatmentStagesAtDispatch >
                        admission.CompletedTreatmentStages ||
                    transfer.CompletedTreatmentStagesAtDispatch > 0 &&
                        transfer.CreatedDay <
                            MilitaryPostTreatmentTransferContractActivationDay ||
                    transfer.CreatedDay <
                        MilitaryMedicalTransferContractActivationDay ||
                    transfer.CreatedDay < admission.AdmittedDay ||
                    transfer.CreatedDay > AbsoluteDay ||
                    transfer.AuthorizingAuthority < MilitaryAuthorityLevel.Army ||
                    !Enum.IsDefined(
                        typeof(MilitaryAuthorityLevel),
                        transfer.AuthorizingAuthority) ||
                    !Enum.IsDefined(
                        typeof(MilitaryMedicalTransferStatus),
                        transfer.Status) ||
                    transfer.ReservedMedicineUnits != expectedMedicineUnits ||
                    transfer.ReservedMedicineUnits <= 0 ||
                    transfer.ConsumedReservedMedicineUnits < 0 ||
                    transfer.ConsumedReservedMedicineUnits >
                        transfer.ReservedMedicineUnits ||
                    transfer.ReleasedReservedMedicineUnits < 0 ||
                    transfer.ReleasedReservedMedicineUnits >
                        transfer.ReservedMedicineUnits -
                        transfer.ConsumedReservedMedicineUnits ||
                    deathClosed != !string.IsNullOrEmpty(
                        transfer.DeathClosureId) ||
                    deathClosed != (transferDeathClosure != null) ||
                    deathClosed &&
                        (transferDeathClosure.MedicalTransferId != transfer.Id ||
                         transferDeathClosure.AdmissionId != admission.Id ||
                         transferDeathClosure.EvacuationId != evacuation.Id ||
                         transferDeathClosure.SourceRearMedicalSiteId !=
                            sourceSite.Id ||
                         transferDeathClosure.DestinationRearMedicalSiteId !=
                            destinationSite.Id ||
                         transferDeathClosure.SourcePhysicianPersonId !=
                            transfer.SourcePhysicianPersonId ||
                         transferDeathClosure.DesignatedReceivingPersonId !=
                            transfer.DesignatedReceivingPersonId ||
                         transferDeathClosure.RouteId != transfer.RouteId ||
                         transferDeathClosure.OccurredInTransit !=
                            (deceasedInTransit || closedAfterDeath &&
                             transferDeathClosure
                                .RemainingKilometersAtDeath > 0) ||
                         transferDeathClosure.RemainingKilometersAtDeath < 0 ||
                         transferDeathClosure.ReservedMedicineBatchId !=
                            transfer.ReservedMedicineBatchId ||
                         transferDeathClosure.ReleasedReservedMedicineUnits !=
                            transfer.ReleasedReservedMedicineUnits ||
                         transferDeathClosure.ReservedMedicineUnitsAfterRelease !=
                            transferDeathClosure
                                .ReservedMedicineUnitsBeforeRelease -
                            transferDeathClosure
                                .ReleasedReservedMedicineUnits ||
                         transferDeathClosure
                            .ReservationReleaseInventoryTransactionId !=
                            transfer.ReservationReleaseInventoryTransactionId) ||
                    (transfer.ReleasedReservedMedicineUnits > 0) !=
                        hasReleaseTransaction ||
                    batch.ProductDefinitionId != CoreProductionContent
                        .HerbalMedicineMaterialProductId ||
                    batch.InventoryContainerId !=
                        destinationSite.MedicineInventoryContainerId ||
                    batch.OwnerOrganizationId !=
                        destinationSite.OwnerOrganizationId ||
                    transaction.Type != InventoryTransactionType
                        .MilitaryMedicalTransferMedicineReserved ||
                    transaction.SourceMilitaryMedicalTransferId != transfer.Id ||
                    transaction.ActorPersonId !=
                        transfer.DesignatedReceivingPersonId ||
                    transaction.Day != transfer.CreatedDay ||
                    transaction.FacilityInventoryDelta != 0 ||
                    transaction.Lines == null || transaction.Lines.Count != 1 ||
                    transaction.Lines[0].BatchId != batch.Id ||
                    transaction.Lines[0].QuantityDelta != 0 ||
                    transaction.Lines[0].ReservedQuantityDelta !=
                        transfer.ReservedMedicineUnits ||
                    hasReleaseTransaction &&
                        (releaseTransaction.Type !=
                            InventoryTransactionType
                                .MilitaryMedicalTransferMedicineReleased ||
                         releaseTransaction.SourceMilitaryMedicalTransferId !=
                            transfer.Id ||
                         releaseTransaction.Day < transfer.ReceivedDay ||
                         releaseTransaction.Day > AbsoluteDay ||
                         releaseTransaction.FacilityInventoryDelta != 0 ||
                         releaseTransaction.Lines == null ||
                         releaseTransaction.Lines.Count != 1 ||
                         releaseTransaction.Lines[0].BatchId != batch.Id ||
                         releaseTransaction.Lines[0].QuantityDelta != 0 ||
                         releaseTransaction.Lines[0].ReservedQuantityDelta !=
                            -transfer.ReleasedReservedMedicineUnits) ||
                    deathClosed &&
                        (transfer.ConsumedReservedMedicineUnits != 0 ||
                         transfer.ReleasedReservedMedicineUnits !=
                            transfer.ReservedMedicineUnits ||
                         !hasReleaseTransaction ||
                         releaseTransaction.ActorPersonId !=
                            transfer.SourcePhysicianPersonId ||
                         releaseTransaction.Day !=
                            transferDeathClosure.Day ||
                         transferDeathClosure
                            .ReservedMedicineUnitsBeforeRelease <
                            transfer.ReservedMedicineUnits ||
                         transferDeathClosure
                            .ReleasedReservedMedicineUnits !=
                            transfer.ReservedMedicineUnits) ||
                    hasNextTransfer &&
                        (transfer.ReleasedReservedMedicineUnits !=
                            transfer.ReservedMedicineUnits -
                                transfer.ConsumedReservedMedicineUnits ||
                         !hasReleaseTransaction ||
                         releaseTransaction.ActorPersonId !=
                            transfer.DesignatedReceivingPersonId ||
                         releaseTransaction.Day != nextTransfer.CreatedDay) ||
                    !validReceipt)
                {
                    throw new InvalidOperationException(
                        $"Invalid military medical transfer {transfer.Id}.");
                }

                var expectedCurrentSite = completed || closedAfterDeath
                    ? destinationSite
                    : sourceSite;
                var expectedCurrentPhysician = completed
                    ? transfer.DesignatedReceivingPersonId
                    : transfer.SourcePhysicianPersonId;
                if (transfer.SequenceIndex == 0 &&
                        transfer.SourcePhysicianPersonId !=
                            evacuation.ReceivingPersonId ||
                    isLatestTransfer &&
                        (admission.RearMedicalSiteId != expectedCurrentSite.Id ||
                         admission.PhysicianPersonId !=
                            expectedCurrentPhysician ||
                         evacuation.RearMedicalSiteId != expectedCurrentSite.Id ||
                         evacuation.CurrentCareLocationId !=
                            expectedCurrentSite.LocationId))
                {
                    throw new InvalidOperationException(
                        $"Medical transfer {transfer.Id} has an invalid responsibility chain.");
                }

                ValidateMedicalTransferJourney(
                    transfer,
                    patient,
                    transfer.PatientJourneyId,
                    sourceSite.LocationId,
                    destinationSite.LocationId,
                    arrived,
                    isLatestTransfer && evacuation.Status <
                        MilitaryMedicalEvacuationStatus.ReturningToArmy);
                if (transfer.TeamMembers == null ||
                    transfer.TeamMembers.Count != evacuation.TeamMembers.Count)
                {
                    throw new InvalidOperationException(
                        $"Medical transfer {transfer.Id} has an invalid escort team.");
                }
                var teamPeople = new HashSet<string>(StringComparer.Ordinal);
                for (var memberIndex = 0;
                     memberIndex < transfer.TeamMembers.Count;
                     memberIndex++)
                {
                    var member = transfer.TeamMembers[memberIndex] ??
                        throw new InvalidOperationException(
                            $"Medical transfer {transfer.Id} has a null escort.");
                    var evacuationMember = evacuation.TeamMembers[memberIndex];
                    if (!people.TryGetValue(member.PersonId, out var person) ||
                        !militaryServices.TryGetValue(
                            member.MilitaryServiceId, out var service) ||
                        !teamPeople.Add(member.PersonId) ||
                        member.PersonId != evacuationMember.PersonId ||
                        member.MilitaryServiceId !=
                            evacuationMember.MilitaryServiceId ||
                        service.PersonId != member.PersonId)
                    {
                        throw new InvalidOperationException(
                            $"Medical transfer {transfer.Id} has an invalid escort.");
                    }
                    ValidateMedicalTransferJourney(
                        transfer,
                        person,
                        member.JourneyId,
                        sourceSite.LocationId,
                        destinationSite.LocationId,
                        arrived,
                        isLatestTransfer && evacuation.Status <
                            MilitaryMedicalEvacuationStatus.ReturningToArmy);
                }
                if (arrived != (transfer.ArrivedDay >= 0) ||
                    arrived && (transfer.ArrivedDay < transfer.CreatedDay ||
                                transfer.ArrivedDay > AbsoluteDay))
                {
                    throw new InvalidOperationException(
                        $"Medical transfer {transfer.Id} has invalid arrival facts.");
                }

                var consumed = 0;
                for (var treatmentIndex = 0;
                     treatmentIndex < MilitaryRearMedicalTreatments.Count;
                     treatmentIndex++)
                {
                    var treatment = MilitaryRearMedicalTreatments[treatmentIndex];
                    if (treatment.AdmissionId == admission.Id &&
                        treatment.StageIndex >=
                            transfer.CompletedTreatmentStagesAtDispatch &&
                        (!hasNextTransfer || treatment.StageIndex <
                            nextTransfer.CompletedTreatmentStagesAtDispatch))
                    {
                        consumed = checked(
                            consumed + treatment.MedicineUnitsConsumed);
                    }
                }
                if (transfer.ConsumedReservedMedicineUnits != consumed ||
                    batch.ReservedQuantity < checked(
                        transfer.ReservedMedicineUnits - consumed -
                        transfer.ReleasedReservedMedicineUnits))
                {
                    throw new InvalidOperationException(
                        $"Medical transfer {transfer.Id} does not close its medicine reservation.");
                }
            }
        }

        private void ValidateMedicalTransferJourney(
            MilitaryMedicalTransferState transfer,
            PersonState person,
            string journeyId,
            string sourceLocationId,
            string destinationLocationId,
            bool arrived,
            bool stillAtCareSite)
        {
            _ = new StableId(journeyId);
            var journey = FindJourneyById(journeyId);
            if (!arrived)
            {
                if (journey != null)
                {
                    if (journey.PersonId != person.Id ||
                        journey.RouteId != transfer.RouteId ||
                        journey.OriginLocationId != sourceLocationId ||
                        journey.DestinationLocationId != destinationLocationId ||
                        journey.Mode != TravelMode.Foot ||
                        person.LocationId != sourceLocationId)
                    {
                        throw new InvalidOperationException(
                            $"Medical transfer {transfer.Id} has an invalid journey.");
                    }
                }
                else if (person.LocationId != destinationLocationId)
                {
                    throw new InvalidOperationException(
                        $"Medical transfer traveler {person.Id} has invalid progress.");
                }
            }
            else if (journey != null ||
                     stillAtCareSite && person.LocationId != destinationLocationId)
            {
                throw new InvalidOperationException(
                    $"Medical transfer traveler {person.Id} has invalid arrival state.");
            }
        }

        private int RequiredMedicalTransferMedicineUnits(
            MilitaryRearMedicalAdmissionState admission,
            int firstStageIndex)
        {
            var total = 0;
            if (firstStageIndex < 0 ||
                firstStageIndex >= admission.TreatmentPlanProtocolIds.Count)
            {
                return -1;
            }
            for (var i = firstStageIndex;
                 i < admission.TreatmentPlanProtocolIds.Count;
                 i++)
            {
                var protocol = admission.TreatmentPlanProtocolIds[i];
                if (protocol ==
                    MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery)
                {
                    MilitaryInjuryEpisodeState injury = null;
                    for (var injuryIndex = 0;
                         injuryIndex < MilitaryInjuryEpisodes.Count;
                         injuryIndex++)
                    {
                        if (MilitaryInjuryEpisodes[injuryIndex].Id ==
                            admission.InjuryEpisodeId)
                        {
                            injury = MilitaryInjuryEpisodes[injuryIndex];
                            break;
                        }
                    }
                    MilitarySurgicalProcedureDefinitionState procedure = null;
                    for (var procedureIndex = 0;
                         procedureIndex < MilitarySurgicalProcedures.Count;
                         procedureIndex++)
                    {
                        if (MilitarySurgicalProcedures[procedureIndex].Id ==
                            injury?.SurgicalProcedureId)
                        {
                            procedure = MilitarySurgicalProcedures[procedureIndex];
                            break;
                        }
                    }
                    if (procedure == null)
                    {
                        return -1;
                    }
                    total = checked(total + procedure.MedicineUnits);
                }
                else if (protocol ==
                    MilitaryRearMedicalTreatmentProtocolIds.InfectionControl)
                {
                    total = checked(total +
                        MilitaryMedicalRules.InfectionControlMedicineUnits);
                }
                else
                {
                    total = checked(total +
                        MilitaryMedicalRules.MedicineUnitsPerTreatment);
                }
            }
            return total;
        }

        private static MilitaryMedicalTransferState
            FindFirstMedicalTransferForAdmission(
                string admissionId,
                Dictionary<string, MilitaryMedicalTransferState> transfers)
        {
            MilitaryMedicalTransferState first = null;
            foreach (var pair in transfers)
            {
                var candidate = pair.Value;
                if (candidate.AdmissionId == admissionId &&
                    (first == null ||
                     candidate.SequenceIndex < first.SequenceIndex))
                {
                    first = candidate;
                }
            }
            return first;
        }

        private static MilitaryMedicalTransferState
            FindMedicalTransferForTreatmentStage(
                string admissionId,
                int stageIndex,
                Dictionary<string, MilitaryMedicalTransferState> transfers)
        {
            MilitaryMedicalTransferState selected = null;
            foreach (var pair in transfers)
            {
                var candidate = pair.Value;
                if (candidate.AdmissionId != admissionId ||
                    candidate.CompletedTreatmentStagesAtDispatch > stageIndex)
                {
                    continue;
                }
                if (selected == null ||
                    candidate.SequenceIndex > selected.SequenceIndex)
                {
                    selected = candidate;
                }
            }
            return selected;
        }

        private int ExpectedAdmissionHealthBeforeDeath(
            MilitaryRearMedicalAdmissionState admission,
            MilitaryInjuryEpisodeState injury)
        {
            if (admission.CompletedTreatmentStages == 0)
            {
                return injury.AdmissionHealthBasisPoints;
            }
            for (var i = 0; i < MilitaryRearMedicalTreatments.Count; i++)
            {
                var treatment = MilitaryRearMedicalTreatments[i];
                if (treatment.Id == admission.TreatmentId)
                {
                    return treatment.ClosingHealthBasisPoints;
                }
            }
            return -1;
        }

        private Dictionary<string, MilitaryFieldHospitalConstructionProjectState>
            ValidateMilitaryFieldHospitalConstruction(
                Dictionary<string, ArmyState> armies,
                Dictionary<string, PersonState> people,
                Dictionary<string, MilitaryServiceState> militaryServices,
                HashSet<string> organizations,
                Dictionary<string, InventoryContainerState> containers,
                Dictionary<string, ProductBatchState> batches,
                Dictionary<string, InventoryTransactionState> transactions)
        {
            var projects = new Dictionary<
                string,
                MilitaryFieldHospitalConstructionProjectState>(
                    StringComparer.Ordinal);
            var locations = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryFieldHospitalConstructionProjects.Count;
                 i++)
            {
                var project = MilitaryFieldHospitalConstructionProjects[i] ??
                    throw new InvalidOperationException(
                        "A field-hospital construction project cannot be null.");
                _ = new StableId(project.Id);
                _ = new StableId(project.ProfileId);
                var hasArmy = armies.TryGetValue(
                    project.SourceArmyId, out var army);
                var hasContainer = containers.TryGetValue(
                    project.MaterialInventoryContainerId, out var container);
                var hasTransaction = transactions.TryGetValue(
                    project.InventoryTransactionId, out var transaction);
                if (projects.ContainsKey(project.Id) ||
                    !locations.Add(
                        project.OwnerOrganizationId + "|" +
                        project.LocationId) ||
                    !hasArmy || !hasContainer || !hasTransaction ||
                    !people.ContainsKey(project.AuthorizingPersonId) ||
                    !people.ContainsKey(project.ManagerPersonId) ||
                    !organizations.Contains(project.OwnerOrganizationId) ||
                    !ContainsLocation(project.LocationId) ||
                    army.OrganizationId != project.OwnerOrganizationId ||
                    project.ProfileId !=
                        MilitaryFieldHospitalConstructionProfileIds
                            .TimberLeatherCamp ||
                    project.AuthorizingAuthority < MilitaryAuthorityLevel.Army ||
                    !Enum.IsDefined(
                        typeof(MilitaryAuthorityLevel),
                        project.AuthorizingAuthority) ||
                    project.RequiredTimberUnits !=
                        MilitaryMedicalRules.FieldHospitalRequiredTimberUnits ||
                    project.RequiredLeatherUnits !=
                        MilitaryMedicalRules.FieldHospitalRequiredLeatherUnits ||
                    project.RequiredMoney !=
                        MilitaryMedicalRules.FieldHospitalRequiredMoney ||
                    project.RequiredLaborDays !=
                        MilitaryMedicalRules.FieldHospitalRequiredLaborDays ||
                    project.CompletedLaborDays < 0 ||
                    project.CompletedLaborDays > project.RequiredLaborDays ||
                    project.OwnerTreasuryBefore < project.RequiredMoney ||
                    project.OwnerTreasuryAfter != checked(
                        project.OwnerTreasuryBefore - project.RequiredMoney) ||
                    project.StartedDay < MilitaryMedicalContractActivationDay ||
                    project.StartedDay > AbsoluteDay ||
                    container.OwnerOrganizationId !=
                        project.OwnerOrganizationId ||
                    !string.IsNullOrEmpty(container.OwnerFamilyId) ||
                    !string.IsNullOrEmpty(container.CarrierPersonId) ||
                    container.LocationId != project.LocationId ||
                    transaction.Type != InventoryTransactionType
                        .MilitaryFieldHospitalConstructionConsumed ||
                    transaction
                        .SourceMilitaryFieldHospitalConstructionProjectId !=
                        project.Id ||
                    transaction.ActorPersonId != project.ManagerPersonId ||
                    transaction.Day != project.StartedDay)
                {
                    throw new InvalidOperationException(
                        $"Invalid field-hospital construction project {project.Id}.");
                }
                projects.Add(project.Id, project);

                long timber = 0;
                long leather = 0;
                long weight = 0;
                if (transaction.Lines == null || transaction.Lines.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Field-hospital project {project.Id} lacks material evidence.");
                }
                for (var lineIndex = 0;
                     lineIndex < transaction.Lines.Count;
                     lineIndex++)
                {
                    var line = transaction.Lines[lineIndex];
                    if (!batches.TryGetValue(line.BatchId, out var batch) ||
                        line.QuantityDelta >= 0 ||
                        line.ReservedQuantityDelta != 0 ||
                        line.InventoryContainerId != container.Id ||
                        line.OwnerOrganizationId !=
                            project.OwnerOrganizationId ||
                        batch.InventoryContainerId != container.Id ||
                        batch.OwnerOrganizationId !=
                            project.OwnerOrganizationId ||
                        batch.ProductDefinitionId !=
                            line.ProductDefinitionId)
                    {
                        throw new InvalidOperationException(
                            $"Invalid construction material line for {project.Id}.");
                    }
                    var consumed = checked(-line.QuantityDelta);
                    if (line.ProductDefinitionId ==
                        CoreProductionContent.TimberMaterialProductId)
                    {
                        timber = checked(timber + consumed);
                    }
                    else if (line.ProductDefinitionId ==
                        CoreProductionContent.LeatherMaterialProductId)
                    {
                        leather = checked(leather + consumed);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Unsupported construction material for {project.Id}.");
                    }
                    weight = checked(weight + consumed * batch.UnitWeight);
                }
                if (timber != project.RequiredTimberUnits ||
                    leather != project.RequiredLeatherUnits ||
                    transaction.FacilityInventoryDelta != -weight)
                {
                    throw new InvalidOperationException(
                        $"Construction material totals are invalid for {project.Id}.");
                }
            }

            var laborByProject = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var workerDays = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0;
                 i < MilitaryFieldHospitalConstructionWork.Count;
                 i++)
            {
                var work = MilitaryFieldHospitalConstructionWork[i] ??
                    throw new InvalidOperationException(
                        "Field-hospital construction work cannot be null.");
                _ = new StableId(work.Id);
                if (!projects.TryGetValue(work.ProjectId, out var project) ||
                    !people.ContainsKey(work.WorkerPersonId) ||
                    !workerDays.Add(
                        work.ProjectId + "|" + work.WorkerPersonId + "|" +
                        work.Day) ||
                    work.Day < project.StartedDay ||
                    work.Day > AbsoluteDay ||
                    work.LaborDays != 1)
                {
                    throw new InvalidOperationException(
                        $"Invalid field-hospital construction work {work.Id}.");
                }
                AddInt(laborByProject, work.ProjectId, work.LaborDays);
            }
            foreach (var pair in projects)
            {
                var project = pair.Value;
                laborByProject.TryGetValue(project.Id, out var labor);
                var completed = project.Status ==
                    MilitaryFieldHospitalConstructionStatus.Completed;
                if (!Enum.IsDefined(
                        typeof(MilitaryFieldHospitalConstructionStatus),
                        project.Status) ||
                    labor != project.CompletedLaborDays ||
                    completed !=
                        (project.CompletedLaborDays == project.RequiredLaborDays) ||
                    completed != (project.CompletedDay >= project.StartedDay) ||
                    completed != !string.IsNullOrEmpty(
                        project.RearMedicalSiteId) ||
                    completed && project.CompletedDay > AbsoluteDay ||
                    !completed && project.CompletedDay != -1)
                {
                    throw new InvalidOperationException(
                        $"Invalid construction closure for {project.Id}.");
                }
            }
            return projects;
        }

        private void ValidateMilitaryFieldHospitalMaintenance(
            Dictionary<string, PersonState> people,
            Dictionary<string, MilitaryRearMedicalSiteState> sites,
            Dictionary<string, MilitaryFieldHospitalConstructionProjectState>
                projects,
            Dictionary<string, InventoryContainerState> containers,
            Dictionary<string, ProductBatchState> batches,
            Dictionary<string, InventoryTransactionState> transactions)
        {
            foreach (var pair in projects)
            {
                var project = pair.Value;
                if (project.Status ==
                        MilitaryFieldHospitalConstructionStatus.Completed &&
                    (!sites.TryGetValue(project.RearMedicalSiteId, out var site) ||
                     site.SourceConstructionProjectId != project.Id ||
                     site.RegisteredDay != project.CompletedDay))
                {
                    throw new InvalidOperationException(
                        $"Completed field-hospital project {project.Id} lacks its site.");
                }
            }

            var bySite = new Dictionary<
                string,
                List<MilitaryFieldHospitalMaintenanceState>>(
                    StringComparer.Ordinal);
            var siteDays = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryFieldHospitalMaintenance.Count; i++)
            {
                var maintenance = MilitaryFieldHospitalMaintenance[i] ??
                    throw new InvalidOperationException(
                        "Field-hospital maintenance cannot be null.");
                _ = new StableId(maintenance.Id);
                var hasSite = sites.TryGetValue(
                    maintenance.RearMedicalSiteId, out var site);
                InventoryContainerState container = null;
                var hasContainer = hasSite && containers.TryGetValue(
                    site.SupportInventoryContainerId, out container);
                var hasBatch = batches.TryGetValue(
                    maintenance.SourceTimberBatchId, out var batch);
                var hasTransaction = transactions.TryGetValue(
                    maintenance.InventoryTransactionId, out var transaction);
                if (!hasSite || !hasContainer || !hasBatch || !hasTransaction ||
                    site.KindId != MilitaryRearMedicalSiteKindIds.FieldHospital ||
                    !people.ContainsKey(maintenance.ManagerPersonId) ||
                    !siteDays.Add(site.Id + "|" + maintenance.Day) ||
                    maintenance.Day < site.RegisteredDay ||
                    maintenance.Day > AbsoluteDay ||
                    maintenance.TimberUnitsConsumed !=
                        MilitaryMedicalRules
                            .FieldHospitalMaintenanceTimberUnits ||
                    maintenance.MoneyPaid !=
                        MilitaryMedicalRules.FieldHospitalMaintenanceMoney ||
                    maintenance.OwnerTreasuryBefore < maintenance.MoneyPaid ||
                    maintenance.OwnerTreasuryAfter != checked(
                        maintenance.OwnerTreasuryBefore -
                        maintenance.MoneyPaid) ||
                    maintenance.Day < maintenance.PreviousNextMaintenanceDay ||
                    maintenance.NewNextMaintenanceDay != checked(
                        maintenance.Day + MilitaryMedicalRules
                            .FieldHospitalMaintenanceIntervalDays) ||
                    batch.ProductDefinitionId !=
                        CoreProductionContent.TimberMaterialProductId ||
                    batch.InventoryContainerId != container.Id ||
                    batch.OwnerOrganizationId != site.OwnerOrganizationId ||
                    transaction.Type != InventoryTransactionType
                        .MilitaryFieldHospitalMaintenanceConsumed ||
                    transaction.SourceMilitaryFieldHospitalMaintenanceId !=
                        maintenance.Id ||
                    transaction.ActorPersonId != maintenance.ManagerPersonId ||
                    transaction.Day != maintenance.Day ||
                    transaction.Lines == null ||
                    transaction.Lines.Count != 1 ||
                    transaction.Lines[0].BatchId != batch.Id ||
                    transaction.Lines[0].QuantityDelta !=
                        -maintenance.TimberUnitsConsumed ||
                    transaction.Lines[0].ReservedQuantityDelta != 0 ||
                    transaction.FacilityInventoryDelta !=
                        -checked((long)maintenance.TimberUnitsConsumed *
                            batch.UnitWeight))
                {
                    throw new InvalidOperationException(
                        $"Invalid field-hospital maintenance {maintenance.Id}.");
                }
                if (!bySite.TryGetValue(site.Id, out var records))
                {
                    records = new List<MilitaryFieldHospitalMaintenanceState>();
                    bySite.Add(site.Id, records);
                }
                records.Add(maintenance);
            }

            foreach (var pair in sites)
            {
                var site = pair.Value;
                if (site.KindId != MilitaryRearMedicalSiteKindIds.FieldHospital)
                {
                    continue;
                }
                var expectedNext = checked(
                    site.RegisteredDay + MilitaryMedicalRules
                        .FieldHospitalMaintenanceIntervalDays);
                var expectedLast = site.RegisteredDay;
                if (bySite.TryGetValue(site.Id, out var records))
                {
                    records.Sort((left, right) =>
                    {
                        var day = left.Day.CompareTo(right.Day);
                        return day != 0
                            ? day
                            : string.CompareOrdinal(left.Id, right.Id);
                    });
                    for (var i = 0; i < records.Count; i++)
                    {
                        if (records[i].PreviousNextMaintenanceDay != expectedNext)
                        {
                            throw new InvalidOperationException(
                                $"Maintenance chain is invalid for {site.Id}.");
                        }
                        expectedLast = records[i].Day;
                        expectedNext = records[i].NewNextMaintenanceDay;
                    }
                }
                if (site.LastMaintenanceDay != expectedLast ||
                    site.NextMaintenanceDay != expectedNext)
                {
                    throw new InvalidOperationException(
                        $"Maintenance summary is invalid for {site.Id}.");
                }
            }
        }

        private void ValidateMilitaryMedicalEvacuations(
            Dictionary<string, ArmyState> armies,
            Dictionary<string, PersonState> people,
            Dictionary<string, MilitaryServiceState> militaryServices)
        {
            var evacuationServices = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < MilitaryMedicalEvacuations.Count; i++)
            {
                var evacuation = MilitaryMedicalEvacuations[i] ??
                    throw new InvalidOperationException(
                        "A military medical evacuation cannot be null.");
                _ = new StableId(evacuation.Id);
                _ = new StableId(evacuation.TransportPolicyId);
                _ = new StableId(evacuation.ReceptionPolicyId);
                _ = new StableId(evacuation.PatientReturnPolicyId);
                var hasArmy = armies.TryGetValue(
                    evacuation.SourceArmyId, out var army);
                var hasPatientService = militaryServices.TryGetValue(
                    evacuation.PatientMilitaryServiceId,
                    out var patientService);
                var hasPatient = people.TryGetValue(
                    evacuation.PatientPersonId, out var patient);
                var hasAuthorizer = people.ContainsKey(
                    evacuation.AuthorizingPersonId);
                var hasReceiver = people.ContainsKey(
                    evacuation.DesignatedReceivingPersonId);
                var route = FindRouteById(evacuation.RouteId);
                var routeValid = route != null &&
                    (route.FromLocationId == evacuation.OriginLocationId &&
                     route.ToLocationId == evacuation.DestinationLocationId ||
                     route.Bidirectional &&
                     route.ToLocationId == evacuation.OriginLocationId &&
                     route.FromLocationId == evacuation.DestinationLocationId);
                var completed = evacuation.Status ==
                    MilitaryMedicalEvacuationStatus.Completed;
                var deceasedInOriginalTransit = evacuation.Status ==
                    MilitaryMedicalEvacuationStatus.DeceasedInTransit;
                var originalDeathClosure = string.IsNullOrEmpty(
                        evacuation.OriginalEvacuationDeathClosureId)
                    ? null
                    : MilitaryOriginalEvacuationDeathClosures.Find(item =>
                        item.Id ==
                            evacuation.OriginalEvacuationDeathClosureId);
                var originalEvacuationDeath = originalDeathClosure != null;
                var patientDied = HasMilitaryWoundDeath(
                    evacuation.RearMedicalAdmissionId,
                    evacuation.PatientPersonId);
                var patientReturns = evacuation.PatientReturnPolicyId ==
                    MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .ReturnWithTeam;
                var patientCorpseReturns = evacuation.PatientReturnPolicyId ==
                    MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .ReturnCorpseWithTeam;
                var patientCorpseAwaitsTeam =
                    evacuation.PatientReturnPolicyId ==
                        MilitaryMedicalEvacuationPatientReturnPolicyIds
                            .CorpseAtArmyAwaitingTeamRejoin;
                var patientOrCorpseReturns =
                    patientReturns || patientCorpseReturns ||
                    patientCorpseAwaitsTeam;
                var patientRetires = evacuation.PatientReturnPolicyId ==
                    MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .RemainAtCareSiteForMedicalRetirement;
                var patientRemainsAfterDeath =
                    evacuation.PatientReturnPolicyId ==
                    MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .RemainAtCareSiteAfterDeath;
                var patientReturnDeathClosure = string.IsNullOrEmpty(
                        evacuation.PatientReturnDeathClosureId)
                    ? null
                    : MilitaryPatientReturnDeathClosures.Find(item =>
                        item.Id == evacuation.PatientReturnDeathClosureId);
                var patientReturnDeath =
                    patientReturnDeathClosure != null;
                MilitaryMedicalTransferState activeTransfer = null;
                var hasAnyMedicalTransfer = false;
                for (var transferIndex = 0;
                     transferIndex < MilitaryMedicalTransfers.Count;
                     transferIndex++)
                {
                    var candidate = MilitaryMedicalTransfers[transferIndex];
                    if (candidate.EvacuationId != evacuation.Id)
                    {
                        continue;
                    }
                    hasAnyMedicalTransfer = true;
                    if (candidate.Status ==
                            MilitaryMedicalTransferStatus.InTransit ||
                        candidate.Status ==
                            MilitaryMedicalTransferStatus.AwaitingReception ||
                        candidate.Status ==
                            MilitaryMedicalTransferStatus.DeceasedInTransit)
                    {
                        if (activeTransfer != null)
                        {
                            throw new InvalidOperationException(
                                $"Evacuation {evacuation.Id} has multiple active transfers.");
                        }
                        activeTransfer = candidate;
                    }
                }
                if (!hasArmy || !hasPatientService || !hasPatient ||
                    !hasAuthorizer || !hasReceiver || !routeValid ||
                    army.Id != patientService.ArmyId ||
                    patientService.PersonId != patient.Id ||
                    patientService.Status != (patientDied
                        ? MilitaryServiceStatus.Dead
                        : completed
                            ? patientRetires
                                ? MilitaryServiceStatus.Retired
                                : MilitaryServiceStatus.Active
                            : MilitaryServiceStatus.Wounded) ||
                    !patientReturns && !patientCorpseReturns &&
                        !patientCorpseAwaitsTeam &&
                        !patientRetires &&
                        !patientRemainsAfterDeath ||
                    originalEvacuationDeath != !string.IsNullOrEmpty(
                        evacuation.OriginalEvacuationDeathClosureId) ||
                    deceasedInOriginalTransit !=
                        (originalEvacuationDeath &&
                         originalDeathClosure.OccurredInTransit &&
                         evacuation.ArrivedDay == -1) ||
                    originalEvacuationDeath &&
                        (!patientRemainsAfterDeath || !patientDied ||
                         originalDeathClosure.EvacuationId != evacuation.Id) ||
                    patientReturnDeath != !string.IsNullOrEmpty(
                        evacuation.PatientReturnDeathClosureId) ||
                    patientReturnDeath &&
                        (!patientCorpseReturns && !patientCorpseAwaitsTeam ||
                         !patientDied ||
                         patientReturnDeathClosure.EvacuationId !=
                             evacuation.Id ||
                         evacuation.Status !=
                             MilitaryMedicalEvacuationStatus
                                  .PatientDeceasedReturningToArmy &&
                         evacuation.Status !=
                             MilitaryMedicalEvacuationStatus
                                 .PatientDeceasedAwaitingTeamRejoin &&
                         evacuation.Status !=
                             MilitaryMedicalEvacuationStatus.Completed) ||
                    evacuation.OriginLocationId != route.FromLocationId &&
                        evacuation.OriginLocationId != route.ToLocationId ||
                    !ContainsLocation(evacuation.OriginLocationId) ||
                    !ContainsLocation(evacuation.DestinationLocationId) ||
                    !ContainsLocation(evacuation.CurrentCareLocationId) ||
                    !hasAnyMedicalTransfer &&
                        evacuation.CurrentCareLocationId !=
                            evacuation.DestinationLocationId ||
                    evacuation.AuthorizingAuthority <
                        MilitaryAuthorityLevel.Army ||
                    !Enum.IsDefined(
                        typeof(MilitaryAuthorityLevel),
                        evacuation.AuthorizingAuthority) ||
                    evacuation.TransportPolicyId !=
                        MilitaryMedicalEvacuationTransportPolicyIds
                            .StretcherTeamFoot ||
                    evacuation.ReceptionPolicyId !=
                        MilitaryMedicalEvacuationReceptionPolicyIds
                            .DesignatedPractitionerHandoff ||
                    evacuation.CreatedDay <
                        MilitaryMedicalContractActivationDay ||
                    evacuation.CreatedDay > AbsoluteDay ||
                    !Enum.IsDefined(
                        typeof(MilitaryMedicalEvacuationStatus),
                        evacuation.Status) ||
                    !completed && !evacuationServices.Add(patientService.Id) ||
                    evacuation.TeamMembers == null ||
                    evacuation.TeamMembers.Count <
                        MilitaryMedicalRules.MinimumEvacuationTeamMembers ||
                    evacuation.TeamMembers.Count >
                        MilitaryMedicalRules.MaximumEvacuationTeamMembers)
                {
                    throw new InvalidOperationException(
                        $"Invalid military medical evacuation {evacuation.Id}.");
                }

                ValidateEvacuationTraveler(
                    evacuation,
                    patient,
                    evacuation.PatientJourneyId,
                    evacuation.PatientReturnJourneyId,
                    patientService.Id,
                    evacuationServices,
                    false,
                    patientOrCorpseReturns,
                    activeTransfer,
                    activeTransfer?.PatientJourneyId);
                var memberPeople = new HashSet<string>(StringComparer.Ordinal);
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    var member = evacuation.TeamMembers[memberIndex] ??
                        throw new InvalidOperationException(
                            $"Evacuation {evacuation.Id} has a null team member.");
                    var hasService = militaryServices.TryGetValue(
                        member.MilitaryServiceId, out var memberService);
                    var hasPerson = people.TryGetValue(
                        member.PersonId, out var memberPerson);
                    var returnTeamDeath = string.IsNullOrEmpty(
                            member.ReturnDeathId)
                        ? null
                        : MilitaryReturnTeamDeaths.Find(item =>
                            item.Id == member.ReturnDeathId);
                    var memberDied = returnTeamDeath != null;
                    if (!hasService || !hasPerson ||
                        !memberPeople.Add(member.PersonId) ||
                        memberService.PersonId != member.PersonId ||
                        memberService.ArmyId != army.Id ||
                        memberDied != !string.IsNullOrEmpty(
                            member.ReturnDeathId) ||
                        memberDied &&
                            (returnTeamDeath.EvacuationId != evacuation.Id ||
                             returnTeamDeath.PersonId != member.PersonId ||
                             returnTeamDeath.MilitaryServiceId !=
                                member.MilitaryServiceId) ||
                        memberService.Status != (memberDied
                            ? MilitaryServiceStatus.Dead
                            : completed
                                ? MilitaryServiceStatus.Active
                                : MilitaryServiceStatus
                                    .MedicalEvacuationDuty) ||
                        member.RoleId !=
                            MilitaryMedicalEvacuationTeamRoleIds.StretcherBearer ||
                        (evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus
                                 .PatientDeceasedReturningToArmy ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus
                                 .PatientDeceasedAwaitingTeamRejoin ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus.Completed) !=
                            !string.IsNullOrEmpty(member.ReturnJourneyId) ||
                        member.PersonId == patient.Id)
                    {
                        throw new InvalidOperationException(
                            $"Invalid team member in evacuation {evacuation.Id}.");
                    }
                    ValidateEvacuationTraveler(
                        evacuation,
                        memberPerson,
                        member.JourneyId,
                        member.ReturnJourneyId,
                        memberService.Id,
                        evacuationServices,
                        !completed,
                        true,
                        activeTransfer,
                        FindMedicalTransferTeamJourneyId(
                            activeTransfer, member.PersonId));
                }

                if (evacuation.Status ==
                        MilitaryMedicalEvacuationStatus.InTransit ||
                    deceasedInOriginalTransit)
                {
                    if (evacuation.ArrivedDay != -1 ||
                        !string.IsNullOrEmpty(evacuation.ReceivingPersonId) ||
                        evacuation.ReceivedDay != -1 ||
                        evacuation.ReceivingMedicalSkillBasisPoints != 0)
                    {
                        throw new InvalidOperationException(
                            $"In-transit evacuation {evacuation.Id} has reception facts.");
                    }
                }
                else
                {
                    if (evacuation.ArrivedDay < evacuation.CreatedDay ||
                        evacuation.ArrivedDay > AbsoluteDay ||
                        FindJourneyById(evacuation.PatientJourneyId) != null)
                    {
                        throw new InvalidOperationException(
                            $"Arrived evacuation {evacuation.Id} has invalid arrival facts.");
                    }
                    for (var memberIndex = 0;
                         memberIndex < evacuation.TeamMembers.Count;
                         memberIndex++)
                    {
                        if (FindJourneyById(
                                evacuation.TeamMembers[memberIndex].JourneyId) != null)
                        {
                            throw new InvalidOperationException(
                                $"Arrived evacuation {evacuation.Id} still has a journey.");
                        }
                    }
                }

                if (evacuation.Status ==
                        MilitaryMedicalEvacuationStatus.AwaitingReception ||
                    originalEvacuationDeath)
                {
                    if (!string.IsNullOrEmpty(evacuation.ReceivingPersonId) ||
                        evacuation.ReceivedDay != -1 ||
                        evacuation.ReceivingMedicalSkillBasisPoints != 0)
                    {
                        throw new InvalidOperationException(
                            $"Awaiting evacuation {evacuation.Id} has receipt facts.");
                    }
                }
                else if (evacuation.Status ==
                            MilitaryMedicalEvacuationStatus.Received ||
                         evacuation.Status ==
                            MilitaryMedicalEvacuationStatus.Admitted ||
                         evacuation.Status ==
                            MilitaryMedicalEvacuationStatus.ReadyForReturn ||
                         evacuation.Status ==
                            MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus
                                 .PatientDeceasedReturningToArmy ||
                         evacuation.Status ==
                             MilitaryMedicalEvacuationStatus
                                 .PatientDeceasedAwaitingTeamRejoin ||
                         evacuation.Status ==
                            MilitaryMedicalEvacuationStatus.Completed)
                {
                    if (evacuation.ReceivingPersonId !=
                            evacuation.DesignatedReceivingPersonId ||
                        evacuation.ReceivedDay < evacuation.ArrivedDay ||
                        evacuation.ReceivedDay > AbsoluteDay ||
                        evacuation.ReceivingMedicalSkillBasisPoints <
                            MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints ||
                        !people.TryGetValue(
                            evacuation.ReceivingPersonId, out var receiver) ||
                        receiver.LocationId != evacuation.DestinationLocationId)
                    {
                        throw new InvalidOperationException(
                            $"Received evacuation {evacuation.Id} has invalid receipt facts.");
                    }
                }

                var rearStarted = !originalEvacuationDeath &&
                    (evacuation.Status ==
                         MilitaryMedicalEvacuationStatus.Admitted ||
                     evacuation.Status ==
                         MilitaryMedicalEvacuationStatus.ReadyForReturn ||
                     evacuation.Status ==
                         MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                     evacuation.Status ==
                         MilitaryMedicalEvacuationStatus
                             .PatientDeceasedReturningToArmy ||
                     evacuation.Status ==
                         MilitaryMedicalEvacuationStatus
                             .PatientDeceasedAwaitingTeamRejoin ||
                     evacuation.Status ==
                         MilitaryMedicalEvacuationStatus.Completed);
                if (rearStarted !=
                        !string.IsNullOrEmpty(evacuation.RearMedicalSiteId) ||
                    rearStarted !=
                        !string.IsNullOrEmpty(evacuation.RearMedicalAdmissionId))
                {
                    throw new InvalidOperationException(
                        $"Evacuation {evacuation.Id} has invalid rear-care references.");
                }
                var returnStarted = evacuation.Status ==
                        MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                    evacuation.Status == MilitaryMedicalEvacuationStatus
                        .PatientDeceasedReturningToArmy ||
                    evacuation.Status == MilitaryMedicalEvacuationStatus
                        .PatientDeceasedAwaitingTeamRejoin ||
                    evacuation.Status ==
                        MilitaryMedicalEvacuationStatus.Completed;
                if (returnStarted !=
                        !string.IsNullOrEmpty(evacuation.ReturnRouteId) ||
                    returnStarted != !string.IsNullOrEmpty(
                        evacuation.ReturnDestinationLocationId) ||
                    returnStarted && patientOrCorpseReturns !=
                        !string.IsNullOrEmpty(
                        evacuation.PatientReturnJourneyId) ||
                    !patientOrCorpseReturns && !string.IsNullOrEmpty(
                        evacuation.PatientReturnJourneyId) ||
                    returnStarted != (evacuation.ReturnStartedDay >= 0) ||
                    returnStarted &&
                        (evacuation.ReturnStartedDay <
                             (originalEvacuationDeath
                                 ? evacuation.ArrivedDay
                                 : evacuation.ReceivedDay) ||
                         evacuation.ReturnStartedDay > AbsoluteDay))
                {
                    throw new InvalidOperationException(
                        $"Evacuation {evacuation.Id} has invalid return facts.");
                }
                if (returnStarted)
                {
                    var returnRoute = FindRouteById(evacuation.ReturnRouteId);
                    var validReturnRoute = returnRoute != null &&
                        (returnRoute.FromLocationId ==
                             evacuation.CurrentCareLocationId &&
                         returnRoute.ToLocationId ==
                             evacuation.ReturnDestinationLocationId ||
                         returnRoute.Bidirectional &&
                         returnRoute.ToLocationId ==
                             evacuation.CurrentCareLocationId &&
                         returnRoute.FromLocationId ==
                             evacuation.ReturnDestinationLocationId);
                    if (!validReturnRoute ||
                        army.LocationId !=
                            evacuation.ReturnDestinationLocationId)
                    {
                        throw new InvalidOperationException(
                            $"Evacuation {evacuation.Id} has an invalid rejoin route.");
                    }
                }
                if (completed != (evacuation.CompletedDay >= 0) ||
                    completed &&
                        (evacuation.CompletedDay < evacuation.ReturnStartedDay ||
                         evacuation.CompletedDay > AbsoluteDay))
                {
                    throw new InvalidOperationException(
                        $"Evacuation {evacuation.Id} has invalid completion facts.");
                }
            }

            for (var i = 0; i < MilitaryServices.Count; i++)
            {
                if (MilitaryServices[i].Status ==
                        MilitaryServiceStatus.MedicalEvacuationDuty &&
                    !evacuationServices.Contains(MilitaryServices[i].Id))
                {
                    throw new InvalidOperationException(
                        $"Medical evacuation duty {MilitaryServices[i].Id} lacks an evacuation.");
                }
            }
        }

        private void ValidateEvacuationTraveler(
            MilitaryMedicalEvacuationState evacuation,
            PersonState person,
            string journeyId,
            string returnJourneyId,
            string serviceId,
            HashSet<string> evacuationServices,
            bool addService,
            bool returnsToArmy,
            MilitaryMedicalTransferState activeTransfer,
            string transferJourneyId)
        {
            _ = new StableId(journeyId);
            if (!string.IsNullOrEmpty(returnJourneyId))
            {
                _ = new StableId(returnJourneyId);
            }
            var permittedWoundDeath =
                person.Id == evacuation.PatientPersonId &&
                HasMilitaryWoundDeath(
                    evacuation.RearMedicalAdmissionId,
                    evacuation.PatientPersonId);
            var permittedReturnTeamDeath =
                HasMilitaryReturnTeamDeath(evacuation.Id, person.Id);
            if (!person.IsAlive && !permittedWoundDeath &&
                    !permittedReturnTeamDeath ||
                addService && !evacuationServices.Add(serviceId))
            {
                throw new InvalidOperationException(
                    $"Invalid traveler in evacuation {evacuation.Id}.");
            }
            var journey = FindJourneyById(journeyId);
            if (evacuation.Status ==
                    MilitaryMedicalEvacuationStatus.InTransit ||
                evacuation.Status ==
                    MilitaryMedicalEvacuationStatus.DeceasedInTransit)
            {
                if (journey != null)
                {
                    if (journey.PersonId != person.Id ||
                        journey.RouteId != evacuation.RouteId ||
                        journey.OriginLocationId != evacuation.OriginLocationId ||
                        journey.DestinationLocationId !=
                            evacuation.DestinationLocationId ||
                        journey.Mode != TravelMode.Foot ||
                        person.LocationId != evacuation.OriginLocationId)
                    {
                        throw new InvalidOperationException(
                            $"Invalid journey in evacuation {evacuation.Id}.");
                    }
                }
                else if (person.LocationId != evacuation.DestinationLocationId)
                {
                    throw new InvalidOperationException(
                        $"Evacuation traveler {person.Id} is neither traveling nor arrived.");
                }
            }
            else if (journey != null)
            {
                throw new InvalidOperationException(
                    $"Evacuation traveler {person.Id} has invalid arrival state.");
            }
            else if (evacuation.Status <
                     MilitaryMedicalEvacuationStatus.ReturningToArmy)
            {
                if (!string.IsNullOrEmpty(returnJourneyId))
                {
                    throw new InvalidOperationException(
                        $"Evacuation traveler {person.Id} has invalid care-site state.");
                }
                if (activeTransfer != null)
                {
                    _ = new StableId(transferJourneyId);
                    var transferJourney = FindJourneyById(transferJourneyId);
                    if (activeTransfer.Status ==
                            MilitaryMedicalTransferStatus.InTransit ||
                        activeTransfer.Status ==
                            MilitaryMedicalTransferStatus.DeceasedInTransit)
                    {
                        if (transferJourney != null)
                        {
                            if (transferJourney.PersonId != person.Id ||
                                transferJourney.RouteId !=
                                    activeTransfer.RouteId ||
                                transferJourney.OriginLocationId !=
                                    evacuation.CurrentCareLocationId ||
                                transferJourney.Mode != TravelMode.Foot ||
                                person.LocationId !=
                                    evacuation.CurrentCareLocationId)
                            {
                                throw new InvalidOperationException(
                                    $"Evacuation traveler {person.Id} has an invalid transfer journey.");
                            }
                        }
                        else if (person.LocationId ==
                            evacuation.CurrentCareLocationId)
                        {
                            throw new InvalidOperationException(
                                $"Evacuation traveler {person.Id} has not progressed on its transfer.");
                        }
                    }
                    else if (transferJourney != null)
                    {
                        throw new InvalidOperationException(
                            $"Evacuation traveler {person.Id} retained a completed transfer journey.");
                    }
                }
                else if (person.LocationId !=
                    evacuation.CurrentCareLocationId)
                {
                    throw new InvalidOperationException(
                        $"Evacuation traveler {person.Id} has invalid care-site state.");
                }
            }
            else
            {
                if (!returnsToArmy)
                {
                    if (!string.IsNullOrEmpty(returnJourneyId) ||
                        person.LocationId != evacuation.CurrentCareLocationId)
                    {
                        throw new InvalidOperationException(
                            $"Evacuation patient {person.Id} has an invalid " +
                            "medical-retirement location.");
                    }
                    return;
                }
                var returnJourney = FindJourneyById(returnJourneyId);
                if (returnJourney != null)
                {
                    if (evacuation.Status !=
                            MilitaryMedicalEvacuationStatus.ReturningToArmy &&
                        evacuation.Status !=
                            MilitaryMedicalEvacuationStatus
                                .PatientDeceasedReturningToArmy &&
                        evacuation.Status !=
                            MilitaryMedicalEvacuationStatus
                                .PatientDeceasedAwaitingTeamRejoin ||
                        returnJourney.PersonId != person.Id ||
                        returnJourney.RouteId != evacuation.ReturnRouteId ||
                        returnJourney.OriginLocationId !=
                            evacuation.CurrentCareLocationId ||
                        returnJourney.DestinationLocationId !=
                            evacuation.ReturnDestinationLocationId ||
                        returnJourney.Mode != TravelMode.Foot ||
                        person.LocationId != evacuation.CurrentCareLocationId)
                    {
                        throw new InvalidOperationException(
                            $"Evacuation traveler {person.Id} has an invalid return journey.");
                    }
                }
                else if (person.LocationId !=
                         evacuation.ReturnDestinationLocationId)
                {
                    throw new InvalidOperationException(
                        $"Evacuation traveler {person.Id} has not rejoined its army.");
                }
            }
        }

        private bool ValidatePatientReturnTeamJourneySnapshots(
            MilitaryMedicalEvacuationState evacuation,
            MilitaryPatientReturnDeathClosureState closure)
        {
            if (closure.TeamJourneySnapshotsAtDeath == null)
            {
                return false;
            }
            if (!closure.PatientJourneyCompletedBeforeDeath)
            {
                return closure.TeamJourneySnapshotsAtDeath.Count == 0;
            }
            if (evacuation == null || evacuation.TeamMembers == null ||
                closure.TeamJourneySnapshotsAtDeath.Count !=
                    evacuation.TeamMembers.Count)
            {
                return false;
            }

            var seenPeople = new HashSet<string>(StringComparer.Ordinal);
            var seenServices = new HashSet<string>(StringComparer.Ordinal);
            var seenJourneys = new HashSet<string>(StringComparer.Ordinal);
            var anyOutstandingAtDeath = false;
            for (var i = 0;
                 i < closure.TeamJourneySnapshotsAtDeath.Count;
                 i++)
            {
                var snapshot = closure.TeamJourneySnapshotsAtDeath[i];
                if (snapshot == null)
                {
                    return false;
                }
                _ = new StableId(snapshot.PersonId);
                _ = new StableId(snapshot.MilitaryServiceId);
                _ = new StableId(snapshot.ReturnJourneyId);
                if (!seenPeople.Add(snapshot.PersonId) ||
                    !seenServices.Add(snapshot.MilitaryServiceId) ||
                    !seenJourneys.Add(snapshot.ReturnJourneyId) ||
                    snapshot.RemainingKilometersAtDeath < 0)
                {
                    return false;
                }

                var member = evacuation.TeamMembers.Find(item =>
                    item.PersonId == snapshot.PersonId);
                var person = People.Find(item =>
                    item.Id == snapshot.PersonId);
                if (member == null || person == null ||
                    member.MilitaryServiceId != snapshot.MilitaryServiceId ||
                    member.ReturnJourneyId != snapshot.ReturnJourneyId)
                {
                    return false;
                }

                var journey = FindJourneyById(snapshot.ReturnJourneyId);
                if (snapshot.RemainingKilometersAtDeath == 0)
                {
                    if (journey != null || person.LocationId !=
                        evacuation.ReturnDestinationLocationId)
                    {
                        return false;
                    }
                    continue;
                }

                anyOutstandingAtDeath = true;
                if (journey != null)
                {
                    if (journey.PersonId != snapshot.PersonId ||
                        journey.RouteId != evacuation.ReturnRouteId ||
                        journey.OriginLocationId !=
                            evacuation.CurrentCareLocationId ||
                        journey.DestinationLocationId !=
                            evacuation.ReturnDestinationLocationId ||
                        journey.Mode != TravelMode.Foot ||
                        journey.RemainingKilometers <= 0 ||
                        journey.RemainingKilometers >
                            snapshot.RemainingKilometersAtDeath)
                    {
                        return false;
                    }
                }
                else if (person.LocationId !=
                         evacuation.ReturnDestinationLocationId)
                {
                    return false;
                }
            }
            return anyOutstandingAtDeath;
        }

        private bool IsMilitaryMedicalEvacuationService(string serviceId)
        {
            for (var i = 0; i < MilitaryMedicalEvacuations.Count; i++)
            {
                var evacuation = MilitaryMedicalEvacuations[i];
                if (evacuation.Status ==
                    MilitaryMedicalEvacuationStatus.Completed)
                {
                    continue;
                }
                if (evacuation.PatientMilitaryServiceId == serviceId)
                {
                    return true;
                }
                if (evacuation.TeamMembers == null)
                {
                    continue;
                }
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    if (evacuation.TeamMembers[memberIndex] != null &&
                        evacuation.TeamMembers[memberIndex].MilitaryServiceId ==
                            serviceId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private JourneyState FindJourneyById(string journeyId)
        {
            for (var i = 0; i < Journeys.Count; i++)
            {
                if (Journeys[i].Id == journeyId)
                {
                    return Journeys[i];
                }
            }
            return null;
        }

        private RouteState FindRouteById(string routeId)
        {
            for (var i = 0; i < Routes.Count; i++)
            {
                if (Routes[i].Id == routeId)
                {
                    return Routes[i];
                }
            }
            return null;
        }

        private bool ContainsLocation(string locationId)
        {
            for (var i = 0; i < Locations.Count; i++)
            {
                if (Locations[i].Id == locationId)
                {
                    return true;
                }
            }
            return false;
        }

        private bool ContainsMilitaryMedicalTransfer(string transferId)
        {
            for (var i = 0; i < MilitaryMedicalTransfers.Count; i++)
            {
                if (MilitaryMedicalTransfers[i].Id == transferId)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasMilitaryWoundDeath(
            string admissionId,
            string patientPersonId)
        {
            return FindMilitaryWoundDeath(
                admissionId, patientPersonId) != null;
        }

        private MilitaryWoundDeathState FindMilitaryWoundDeath(
            string admissionId,
            string patientPersonId)
        {
            for (var i = 0; i < MilitaryWoundDeaths.Count; i++)
            {
                var death = MilitaryWoundDeaths[i];
                if (death != null &&
                    (death.AdmissionId == admissionId ||
                     string.IsNullOrEmpty(death.AdmissionId) &&
                     string.IsNullOrEmpty(admissionId)) &&
                    death.PatientPersonId == patientPersonId)
                {
                    return death;
                }
            }
            return null;
        }

        private bool HasMilitaryReturnTeamDeath(
            string evacuationId,
            string personId)
        {
            for (var i = 0; i < MilitaryReturnTeamDeaths.Count; i++)
            {
                var death = MilitaryReturnTeamDeaths[i];
                if (death != null &&
                    death.EvacuationId == evacuationId &&
                    death.PersonId == personId)
                {
                    return true;
                }
            }
            return false;
        }

        private static string FindMedicalTransferTeamJourneyId(
            MilitaryMedicalTransferState transfer,
            string personId)
        {
            if (transfer?.TeamMembers == null)
            {
                return string.Empty;
            }
            for (var i = 0; i < transfer.TeamMembers.Count; i++)
            {
                if (transfer.TeamMembers[i]?.PersonId == personId)
                {
                    return transfer.TeamMembers[i].JourneyId;
                }
            }
            return string.Empty;
        }

        private static bool ValidMilitaryMedicalAuthorization(
            string policyId,
            string physicianPersonId,
            string armyId,
            Dictionary<string, MilitaryServiceState> services)
        {
            if (policyId == MilitaryMedicalAuthorizationPolicyIds
                    .CommanderAuthorizedPractitioner)
            {
                return true;
            }
            if (policyId !=
                MilitaryMedicalAuthorizationPolicyIds.InternalMedic)
            {
                return false;
            }
            foreach (var pair in services)
            {
                var service = pair.Value;
                if (service.PersonId == physicianPersonId &&
                    service.ArmyId == armyId &&
                    service.Role == MilitaryServiceRole.Medic)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasCommanderService(
            string personId,
            string armyId,
            Dictionary<string, MilitaryServiceState> services)
        {
            foreach (var pair in services)
            {
                var service = pair.Value;
                if (service.PersonId == personId &&
                    service.ArmyId == armyId &&
                    service.Role == MilitaryServiceRole.Commander)
                {
                    return true;
                }
            }
            return false;
        }

        private void ValidateCivilianMedicalCare()
        {
            var people = new Dictionary<string, PersonState>(
                StringComparer.Ordinal);
            for (var i = 0; i < People.Count; i++)
                people.Add(People[i].Id, People[i]);
            var families = new Dictionary<string, FamilyState>(
                StringComparer.Ordinal);
            for (var i = 0; i < Families.Count; i++)
                families.Add(Families[i].Id, Families[i]);
            var villages = new Dictionary<string, VillageState>(
                StringComparer.Ordinal);
            for (var i = 0; i < Villages.Count; i++)
                villages.Add(Villages[i].Id, Villages[i]);
            var facilities = new Dictionary<string, VillageFacilityState>(
                StringComparer.Ordinal);
            for (var i = 0; i < VillageFacilities.Count; i++)
                facilities.Add(VillageFacilities[i].Id, VillageFacilities[i]);
            var episodes = new Dictionary<string, NutritionConditionEpisodeState>(
                StringComparer.Ordinal);
            for (var i = 0; i < NutritionConditionEpisodes.Count; i++)
                episodes.Add(
                    NutritionConditionEpisodes[i].Id,
                    NutritionConditionEpisodes[i]);
            var batches = new Dictionary<string, ProductBatchState>(
                StringComparer.Ordinal);
            for (var i = 0; i < ProductBatches.Count; i++)
                batches.Add(ProductBatches[i].Id, ProductBatches[i]);
            var transactions = new Dictionary<string, InventoryTransactionState>(
                StringComparer.Ordinal);
            for (var i = 0; i < InventoryTransactions.Count; i++)
                transactions.Add(
                    InventoryTransactions[i].Id,
                    InventoryTransactions[i]);

            var cases = new Dictionary<string, CivilianMedicalCaseState>(
                StringComparer.Ordinal);
            var caseByEpisode = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < CivilianMedicalCases.Count; i++)
            {
                var medicalCase = CivilianMedicalCases[i] ??
                    throw new InvalidOperationException(
                        "A civilian medical case cannot be null.");
                _ = new StableId(medicalCase.Id);
                var hasEpisode = episodes.TryGetValue(
                    medicalCase.NutritionConditionEpisodeId,
                    out var episode);
                var hasPatient = people.TryGetValue(
                    medicalCase.PatientPersonId,
                    out var patient);
                var hasPhysician = people.ContainsKey(
                    medicalCase.DiagnosingPhysicianPersonId);
                var hasAuthorizer = people.TryGetValue(
                    medicalCase.AuthorizingPersonId,
                    out var authorizer);
                var selfAuthorized = medicalCase.AuthorizationPolicyId ==
                    CivilianMedicalAuthorizationPolicyIds.PatientSelf &&
                    medicalCase.AuthorizingPersonId ==
                        medicalCase.PatientPersonId;
                var caregiverAuthorized = medicalCase.AuthorizationPolicyId ==
                    CivilianMedicalAuthorizationPolicyIds
                        .HouseholdAdultCaregiver &&
                    medicalCase.AuthorizingPersonId !=
                        medicalCase.PatientPersonId &&
                    medicalCase.PatientFamilyIdAtDiagnosis ==
                        medicalCase.AuthorizingFamilyIdAtDiagnosis;
                var authorizerAge = hasAuthorizer
                    ? (medicalCase.DiagnosedDay - authorizer.BirthDay) / 360
                    : -1;
                if (!hasEpisode || !hasPatient || !hasPhysician ||
                    !hasAuthorizer || episode.PersonId != patient.Id ||
                    medicalCase.DiagnosisId !=
                        CivilianMedicalDiagnosisIds.MalnutritionIllness ||
                    medicalCase.TreatmentProtocolId !=
                        CivilianMedicalTreatmentProtocolIds.SupportiveHerbalCare ||
                    medicalCase.DiagnosedDay < episode.StartDay ||
                    medicalCase.DiagnosedDay > AbsoluteDay ||
                    medicalCase.PhysicianMedicalSkillBasisPointsAtDiagnosis <
                        CivilianMedicalRules.MinimumPhysicianSkillBasisPoints ||
                    medicalCase.PhysicianMedicalSkillBasisPointsAtDiagnosis >
                        10_000 ||
                    !families.ContainsKey(
                        medicalCase.PatientFamilyIdAtDiagnosis) ||
                    !families.ContainsKey(
                        medicalCase.AuthorizingFamilyIdAtDiagnosis) ||
                    (!selfAuthorized && !caregiverAuthorized) ||
                    authorizerAge < CivilianMedicalRules.AdultAgeYears ||
                    medicalCase.LastTreatmentDay < -1 ||
                    medicalCase.LastTreatmentDay > AbsoluteDay ||
                    medicalCase.LastTreatmentDay != -1 &&
                        medicalCase.LastTreatmentDay < medicalCase.DiagnosedDay ||
                    medicalCase.TotalMedicineUnitsConsumed < 0 ||
                    medicalCase.TotalRecoveredHealthBasisPoints < 0 ||
                    !Enum.IsDefined(
                        typeof(CivilianMedicalCaseStatus), medicalCase.Status) ||
                    medicalCase.Status == CivilianMedicalCaseStatus.Active &&
                        (medicalCase.ClosedDay != -1 ||
                         !string.IsNullOrEmpty(medicalCase.ClosureReasonId)) ||
                    medicalCase.Status == CivilianMedicalCaseStatus.Closed &&
                        (medicalCase.ClosedDay < medicalCase.DiagnosedDay ||
                         medicalCase.ClosedDay > AbsoluteDay ||
                         !IsCivilianMedicalClosureReason(
                             medicalCase.ClosureReasonId)) ||
                    !caseByEpisode.Add(medicalCase.NutritionConditionEpisodeId))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian medical case {medicalCase.Id}.");
                }
                cases.Add(medicalCase.Id, medicalCase);
            }

            if (CivilianMedicalServiceContractActivationDay < 0 ||
                CivilianMedicalServiceContractActivationDay >
                    AbsoluteDay + 1)
            {
                throw new InvalidOperationException(
                    "Civilian medical service activation day is invalid.");
            }

            var prescriptions = new Dictionary<
                string, CivilianMedicalPrescriptionState>(
                    StringComparer.Ordinal);
            var prescriptionByCase = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < CivilianMedicalPrescriptions.Count; i++)
            {
                var prescription = CivilianMedicalPrescriptions[i] ??
                    throw new InvalidOperationException(
                        "A civilian medical prescription cannot be null.");
                _ = new StableId(prescription.Id);
                var hasCase = cases.TryGetValue(
                    prescription.MedicalCaseId, out var medicalCase);
                if (!hasCase ||
                    medicalCase.PrescriptionId != prescription.Id ||
                    prescription.PatientPersonId != medicalCase.PatientPersonId ||
                    prescription.PrescribingPhysicianPersonId !=
                        medicalCase.DiagnosingPhysicianPersonId ||
                    prescription.IssuedDay < medicalCase.DiagnosedDay ||
                    prescription.IssuedDay > AbsoluteDay ||
                    prescription.PrescriptionProtocolId !=
                        CivilianMedicalPrescriptionProtocolIds
                            .SupportiveHerbalMaterial ||
                    prescription.Items == null ||
                    prescription.Items.Count == 0 ||
                    prescription.IsActive !=
                        (medicalCase.Status == CivilianMedicalCaseStatus.Active) ||
                    prescription.IsActive && prescription.ClosedDay != -1 ||
                    !prescription.IsActive &&
                        prescription.ClosedDay != medicalCase.ClosedDay ||
                    !prescriptionByCase.Add(prescription.MedicalCaseId))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian medical prescription {prescription.Id}.");
                }

                var hasHerbalLine = false;
                var prescribedProducts = new HashSet<string>(
                    StringComparer.Ordinal);
                for (var itemIndex = 0;
                     itemIndex < prescription.Items.Count;
                     itemIndex++)
                {
                    var item = prescription.Items[itemIndex] ??
                        throw new InvalidOperationException(
                            $"Prescription {prescription.Id} has a null item.");
                    _ = new StableId(item.ProductDefinitionId);
                    _ = new StableId(item.AdministrationRouteId);
                    if (item.UnitsPerTreatment <= 0 ||
                        !prescribedProducts.Add(item.ProductDefinitionId))
                    {
                        throw new InvalidOperationException(
                            $"Invalid prescription item in {prescription.Id}.");
                    }
                    if (item.ProductDefinitionId ==
                            CoreProductionContent
                                .HerbalMedicineMaterialProductId &&
                        item.UnitsPerTreatment ==
                            CivilianMedicalRules.MedicineUnitsPerTreatment &&
                        item.AdministrationRouteId ==
                            CivilianMedicalAdministrationRouteIds
                                .OralPreparedHerbalMedicine)
                    {
                        hasHerbalLine = true;
                    }
                }
                if (!hasHerbalLine)
                {
                    throw new InvalidOperationException(
                        $"Prescription {prescription.Id} lacks its herbal line.");
                }
                prescriptions.Add(prescription.Id, prescription);
            }

            foreach (var pair in cases)
            {
                if (!string.IsNullOrEmpty(pair.Value.PrescriptionId) &&
                    !prescriptions.ContainsKey(pair.Value.PrescriptionId))
                {
                    throw new InvalidOperationException(
                        $"Medical case {pair.Key} references a missing prescription.");
                }
            }

            var services = new Dictionary<string, CivilianMedicalServiceState>(
                StringComparer.Ordinal);
            var serviceByTreatment = new HashSet<string>(
                StringComparer.Ordinal);
            var physicianWorkByDay = new Dictionary<string, int>(
                StringComparer.Ordinal);
            for (var i = 0; i < CivilianMedicalServices.Count; i++)
            {
                var service = CivilianMedicalServices[i] ??
                    throw new InvalidOperationException(
                        "A civilian medical service cannot be null.");
                _ = new StableId(service.Id);
                var hasCase = cases.TryGetValue(
                    service.MedicalCaseId, out var medicalCase);
                var hasPrescription = prescriptions.TryGetValue(
                    service.PrescriptionId, out var prescription);
                var isClinic = service.VenuePolicyId ==
                    CivilianMedicalVenuePolicyIds.VillageClinic;
                var isHomeVisit = service.VenuePolicyId ==
                    CivilianMedicalVenuePolicyIds.HomeVisit;
                var validClinic = isClinic &&
                    !string.IsNullOrEmpty(service.ClinicFacilityId) &&
                    facilities.TryGetValue(
                        service.ClinicFacilityId, out var clinic) &&
                    clinic.Kind == VillageFacilityKind.Clinic &&
                    villages.ContainsKey(clinic.VillageId);
                var sameFamily = service.PayerFamilyId == service.PayeeFamilyId;
                var expectedTotalFee = sameFamily
                    ? 0
                    : CivilianMedicalRules.RecommendedTreatmentFee(
                        service.PhysicianMedicalSkillBeforeBasisPoints);
                if (!hasCase || !hasPrescription ||
                    prescription.MedicalCaseId != medicalCase.Id ||
                    service.Day < CivilianMedicalServiceContractActivationDay ||
                    service.Day < medicalCase.DiagnosedDay ||
                    service.Day > AbsoluteDay ||
                    service.PatientPersonId != medicalCase.PatientPersonId ||
                    service.PhysicianPersonId !=
                        medicalCase.DiagnosingPhysicianPersonId ||
                    service.AuthorizingPersonId !=
                        medicalCase.AuthorizingPersonId ||
                    !people.ContainsKey(service.PatientPersonId) ||
                    !people.ContainsKey(service.PhysicianPersonId) ||
                    !families.ContainsKey(service.PayerFamilyId) ||
                    !families.ContainsKey(service.PayeeFamilyId) ||
                    (!validClinic &&
                     !(isHomeVisit &&
                       string.IsNullOrEmpty(service.ClinicFacilityId))) ||
                    service.WorkMinutes !=
                        CivilianMedicalRules.TreatmentWorkMinutes ||
                    service.TotalFee != expectedTotalFee ||
                    service.TotalFee != checked(
                        service.ConsultationFee + service.MedicineFee) ||
                    sameFamily &&
                        (service.PaymentPolicyId !=
                            CivilianMedicalPaymentPolicyIds.SameHouseholdCare ||
                         service.ConsultationFee != 0 ||
                         service.MedicineFee != 0 ||
                         service.PayerFamilyWealthBefore !=
                            service.PayerFamilyWealthAfter ||
                         service.PayeeFamilyWealthBefore !=
                            service.PayeeFamilyWealthAfter) ||
                    !sameFamily &&
                        (service.PaymentPolicyId !=
                            CivilianMedicalPaymentPolicyIds.HouseholdDirect ||
                         service.ConsultationFee !=
                            CivilianMedicalRules.BaseConsultationFee +
                            service.PhysicianMedicalSkillBeforeBasisPoints / 500L ||
                         service.MedicineFee !=
                            CivilianMedicalRules.MedicineServiceFee ||
                         service.PayerFamilyWealthAfter != checked(
                            service.PayerFamilyWealthBefore -
                            service.TotalFee) ||
                         service.PayeeFamilyWealthAfter != checked(
                            service.PayeeFamilyWealthBefore +
                            service.TotalFee)) ||
                    service.PhysicianMedicalSkillBeforeBasisPoints <
                        CivilianMedicalRules.MinimumPhysicianSkillBasisPoints ||
                    service.PhysicianMedicalSkillAfterBasisPoints <
                        service.PhysicianMedicalSkillBeforeBasisPoints ||
                    service.PhysicianMedicalSkillAfterBasisPoints > 10_000 ||
                    service.PhysicianMedicalSkillGainBasisPoints < 0 ||
                    service.PhysicianMedicalSkillAfterBasisPoints != checked(
                        service.PhysicianMedicalSkillBeforeBasisPoints +
                        service.PhysicianMedicalSkillGainBasisPoints) ||
                    string.IsNullOrEmpty(service.TreatmentId) ||
                    !serviceByTreatment.Add(service.TreatmentId))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian medical service {service.Id}.");
                }

                var workKey = service.PhysicianPersonId + "|" + service.Day;
                physicianWorkByDay.TryGetValue(workKey, out var workMinutes);
                workMinutes = checked(workMinutes + service.WorkMinutes);
                if (workMinutes >
                    CivilianMedicalRules.MaximumDailyPhysicianWorkMinutes)
                {
                    throw new InvalidOperationException(
                        $"Physician work exceeds the daily limit for {workKey}.");
                }
                physicianWorkByDay[workKey] = workMinutes;
                services.Add(service.Id, service);
            }

            var medicineByCase = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var recoveryByCase = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var lastTreatmentByCase = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var treatmentIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < CivilianMedicalTreatments.Count; i++)
            {
                var treatment = CivilianMedicalTreatments[i] ??
                    throw new InvalidOperationException(
                        "A civilian medical treatment cannot be null.");
                _ = new StableId(treatment.Id);
                var hasCase = cases.TryGetValue(
                    treatment.MedicalCaseId,
                    out var medicalCase);
                var hasBatch = batches.TryGetValue(
                    treatment.SourceMedicineBatchId,
                    out var batch);
                var hasTransaction = transactions.TryGetValue(
                    treatment.InventoryTransactionId,
                    out var transaction);
                var requiresFormalService = treatment.Day >=
                    CivilianMedicalServiceContractActivationDay;
                CivilianMedicalServiceState service = null;
                var hasService = !string.IsNullOrEmpty(
                        treatment.MedicalServiceId) &&
                    services.TryGetValue(
                        treatment.MedicalServiceId, out service);
                if (!treatmentIds.Add(treatment.Id) ||
                    !hasCase || !hasBatch || !hasTransaction ||
                    treatment.PatientPersonId != medicalCase.PatientPersonId ||
                    treatment.PhysicianPersonId !=
                        medicalCase.DiagnosingPhysicianPersonId ||
                    treatment.AuthorizingPersonId !=
                        medicalCase.AuthorizingPersonId ||
                    treatment.AuthorizationPolicyId !=
                        medicalCase.AuthorizationPolicyId ||
                    treatment.Day < medicalCase.DiagnosedDay ||
                    treatment.Day > AbsoluteDay ||
                    treatment.PhysicianMedicalSkillBasisPoints <
                        CivilianMedicalRules.MinimumPhysicianSkillBasisPoints ||
                    treatment.PhysicianMedicalSkillBasisPoints > 10_000 ||
                    treatment.MedicineProductDefinitionId !=
                        CoreProductionContent.HerbalMedicineMaterialProductId ||
                    batch.ProductDefinitionId !=
                        treatment.MedicineProductDefinitionId ||
                    treatment.MedicineUnitsConsumed !=
                        CivilianMedicalRules.MedicineUnitsPerTreatment ||
                    treatment.OpeningHealthBasisPoints < 0 ||
                    treatment.OpeningHealthBasisPoints > 10_000 ||
                    treatment.ClosingHealthBasisPoints < 0 ||
                    treatment.ClosingHealthBasisPoints > 10_000 ||
                    treatment.RecoveredHealthBasisPoints <= 0 ||
                    treatment.ClosingHealthBasisPoints != checked(
                        treatment.OpeningHealthBasisPoints +
                        treatment.RecoveredHealthBasisPoints) ||
                    treatment.OpeningNutritionDebtBasisUnits < 0 ||
                    treatment.OpeningNutritionDebtBasisUnits !=
                        treatment.ClosingNutritionDebtBasisUnits ||
                    treatment.OpeningDiseaseRiskBasisPoints < 0 ||
                    treatment.OpeningDiseaseRiskBasisPoints > 10_000 ||
                    treatment.OpeningDiseaseRiskBasisPoints !=
                        treatment.ClosingDiseaseRiskBasisPoints ||
                    transaction.Type !=
                        InventoryTransactionType.MedicalTreatmentConsumed ||
                    transaction.SourceCivilianMedicalTreatmentId !=
                        treatment.Id ||
                    transaction.ActorPersonId != treatment.PhysicianPersonId ||
                    transaction.Lines.Count != 1 ||
                    transaction.Lines[0].BatchId != batch.Id ||
                    transaction.Lines[0].ProductDefinitionId !=
                        treatment.MedicineProductDefinitionId ||
                    transaction.Lines[0].QuantityDelta !=
                        -treatment.MedicineUnitsConsumed ||
                    transaction.Lines[0].ReservedQuantityDelta != 0 ||
                    requiresFormalService != hasService ||
                    requiresFormalService &&
                        (string.IsNullOrEmpty(treatment.PrescriptionId) ||
                         !prescriptions.TryGetValue(
                            treatment.PrescriptionId,
                            out var treatmentPrescription) ||
                         treatmentPrescription.MedicalCaseId != medicalCase.Id ||
                         service.TreatmentId != treatment.Id ||
                         service.MedicalCaseId != medicalCase.Id ||
                         service.PrescriptionId != treatment.PrescriptionId ||
                         service.PatientPersonId != treatment.PatientPersonId ||
                         service.PhysicianPersonId !=
                            treatment.PhysicianPersonId ||
                         service.AuthorizingPersonId !=
                            treatment.AuthorizingPersonId))
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian medical treatment {treatment.Id}.");
                }

                medicineByCase.TryGetValue(medicalCase.Id, out var medicine);
                recoveryByCase.TryGetValue(medicalCase.Id, out var recovery);
                medicineByCase[medicalCase.Id] = checked(
                    medicine + treatment.MedicineUnitsConsumed);
                recoveryByCase[medicalCase.Id] = checked(
                    recovery + treatment.RecoveredHealthBasisPoints);
                if (!lastTreatmentByCase.TryGetValue(
                        medicalCase.Id, out var lastDay) ||
                    treatment.Day > lastDay)
                {
                    lastTreatmentByCase[medicalCase.Id] = treatment.Day;
                }
            }

            foreach (var pair in services)
            {
                if (!treatmentIds.Contains(pair.Value.TreatmentId))
                {
                    throw new InvalidOperationException(
                        $"Medical service {pair.Key} references a missing treatment.");
                }
            }

            for (var i = 0; i < InventoryTransactions.Count; i++)
            {
                var transaction = InventoryTransactions[i];
                var isMedicalConsumption = transaction.Type ==
                    InventoryTransactionType.MedicalTreatmentConsumed;
                var hasMedicalTreatmentSource = !string.IsNullOrWhiteSpace(
                    transaction.SourceCivilianMedicalTreatmentId);
                if (isMedicalConsumption != hasMedicalTreatmentSource ||
                    hasMedicalTreatmentSource && !treatmentIds.Contains(
                        transaction.SourceCivilianMedicalTreatmentId))
                {
                    throw new InvalidOperationException(
                        $"Inventory transaction {transaction.Id} has an invalid " +
                        "civilian medical treatment source.");
                }
            }

            foreach (var pair in cases)
            {
                medicineByCase.TryGetValue(pair.Key, out var medicine);
                recoveryByCase.TryGetValue(pair.Key, out var recovery);
                lastTreatmentByCase.TryGetValue(pair.Key, out var lastDay);
                var expectedLastDay = medicine == 0 ? -1 : lastDay;
                var episode = episodes[pair.Value.NutritionConditionEpisodeId];
                var closureMatches = pair.Value.Status ==
                    CivilianMedicalCaseStatus.Active ||
                    pair.Value.ClosureReasonId ==
                        CivilianMedicalCaseClosureReasonIds.InjuryRecovered &&
                        episode.AppliedHealthDamageBasisPoints -
                            episode.RecoveredHealthBasisPoints - recovery <= 0 ||
                    pair.Value.ClosureReasonId ==
                        CivilianMedicalCaseClosureReasonIds
                            .NutritionConditionResolved &&
                        episode.EndDay != -1 &&
                        pair.Value.ClosedDay >= episode.EndDay ||
                    pair.Value.ClosureReasonId ==
                        CivilianMedicalCaseClosureReasonIds.PatientDied &&
                        !people[pair.Value.PatientPersonId].IsAlive;
                if (pair.Value.TotalMedicineUnitsConsumed != medicine ||
                    pair.Value.TotalRecoveredHealthBasisPoints != recovery ||
                    pair.Value.LastTreatmentDay != expectedLastDay ||
                    recovery > episode.AppliedHealthDamageBasisPoints -
                        episode.RecoveredHealthBasisPoints ||
                    !closureMatches)
                {
                    throw new InvalidOperationException(
                        $"Civilian medical case {pair.Key} does not close.");
                }
            }
        }

        private static bool IsCivilianMedicalClosureReason(string reasonId)
        {
            return reasonId ==
                    CivilianMedicalCaseClosureReasonIds.InjuryRecovered ||
                reasonId == CivilianMedicalCaseClosureReasonIds
                    .NutritionConditionResolved ||
                reasonId == CivilianMedicalCaseClosureReasonIds.PatientDied;
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
            HashSet<string> familyIds,
            HashSet<string> organizationIds,
            IDictionary<string, VillageFacilityState> facilities,
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
                var organizationOrder =
                    !string.IsNullOrEmpty(order.OwnerOrganizationId) &&
                    string.IsNullOrEmpty(order.OwnerFamilyId) &&
                    !string.IsNullOrEmpty(order.ProductionSiteId) &&
                    !string.IsNullOrEmpty(order.InventoryContainerId) &&
                    string.IsNullOrEmpty(order.StorageFacilityId);
                var familyOrder =
                    string.IsNullOrEmpty(order.OwnerOrganizationId) &&
                    !string.IsNullOrEmpty(order.OwnerFamilyId) &&
                    string.IsNullOrEmpty(order.ProductionSiteId) &&
                    string.IsNullOrEmpty(order.InventoryContainerId) &&
                    !string.IsNullOrEmpty(order.StorageFacilityId);
                var validOrganizationOrder = organizationOrder &&
                    productionSites.TryGetValue(
                        order.ProductionSiteId, out var site) &&
                    containers.TryGetValue(
                        order.InventoryContainerId, out var container) &&
                    organizationIds.Contains(order.OwnerOrganizationId) &&
                    site.OwnerOrganizationId == order.OwnerOrganizationId &&
                    site.InventoryContainerId == container.Id &&
                    container.OwnerOrganizationId == order.OwnerOrganizationId &&
                    site.LocationId == resource?.LocationId &&
                    site.FacilityTags.Contains(
                        resource?.RequiredFacilityTag ?? string.Empty) &&
                    site.ManagerPersonId == order.ManagerPersonId &&
                    HasOrganizationMembership(
                        order.ManagerPersonId, order.OwnerOrganizationId);
                var validFamilyOrder = familyOrder &&
                    facilities.TryGetValue(
                        order.StorageFacilityId, out var storage) &&
                    familyIds.Contains(order.OwnerFamilyId) &&
                    storage.OwnerFamilyId == order.OwnerFamilyId &&
                    FindVillageLocationId(storage.VillageId) ==
                        resource?.LocationId &&
                    storage.CapabilityTags.Contains(
                        resource?.RequiredFacilityTag ?? string.Empty) &&
                    FamilyContainsPerson(
                        Families, order.OwnerFamilyId, order.ManagerPersonId);
                if (!validResource ||
                    (!validOrganizationOrder && !validFamilyOrder) ||
                    !personIds.Contains(order.ManagerPersonId) ||
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
                        organizationOrder && !HasOrganizationMembership(
                            workerId, order.OwnerOrganizationId) ||
                        familyOrder && !FamilyContainsPerson(
                            Families, order.OwnerFamilyId, workerId) ||
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
                        organizationOrder &&
                            (output.OwnerOrganizationId !=
                                order.OwnerOrganizationId ||
                             output.InventoryContainerId !=
                                order.InventoryContainerId) ||
                        familyOrder &&
                            (output.OwnerFamilyId != order.OwnerFamilyId ||
                             output.StorageFacilityId !=
                                order.StorageFacilityId) ||
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

        private string FindVillageLocationId(string villageId)
        {
            for (var i = 0; i < Villages.Count; i++)
            {
                if (Villages[i].Id == villageId)
                {
                    return Villages[i].LocationId;
                }
            }

            return string.Empty;
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

        private static void AddInt(
            IDictionary<string, int> values,
            string key,
            int delta)
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
