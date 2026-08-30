using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public enum LuoyangWorkforceStatus : byte
    {
        NotEligible,
        Unemployed,
        Assigned,
        Official,
        MilitaryDuty,
        Student,
        FamilyManagement
    }

    public enum LuoyangProductionRuntimeStatus : byte
    {
        Idle,
        WaitingInput,
        WaitingWorker,
        Ready,
        InProgress,
        Paused,
        Completed,
        OutputBlocked,
        Maintenance
    }

    public enum LuoyangCropPhase : byte
    {
        Prepare,
        Sowing,
        Growing,
        Harvestable,
        Mature,
        AtRisk,
        Harvested,
        Fallow
    }

    public enum LuoyangInventoryOwnerKind : byte
    {
        Person,
        Household,
        Facility,
        FamilyOrganization,
        Government,
        Market,
        Military
    }

    public enum LuoyangShortageLevel : byte
    {
        Normal,
        Tight,
        Shortage,
        SevereShortage,
        Critical
    }

    public enum LuoyangSupplierMaterializationLevel : byte
    {
        FullPhysical,
        CompactRuntime,
        DeferredExternalTrade
    }

    public enum LuoyangSupplyOrderStatus : byte
    {
        Requested,
        InTransit,
        Delivered,
        Failed
    }

    public enum LuoyangIntelligentAgentRole : byte
    {
        Household,
        FamilyOrganization,
        Merchant,
        SettlementDevelopment,
        Government,
        FacilityManager
    }

    public enum LuoyangCompactConstructionKind : byte
    {
        NewBuild,
        Repair,
        Expansion
    }

    [Serializable]
    public sealed class Luoyang184LivingFacilitySourceRecord
    {
        public int FacilityIndex;
        public string FacilityId;
        public string DefinitionId;
        public string CategoryId;
        public string OwnerId;
        public string ControllerId;
        public string SettlementId;
        public ulong CellId64;
        public int ResidentCapacity;
        public int CurrentResidents;
        public int WorkerCapacity;
        public int MinimumWorkers;
        public int CurrentWorkers;
        public long StorageCapacity;
        public bool Operational = true;
    }

    public interface ILuoyang184LivingWorldSource
    {
        string PackageId { get; }
        string ProtectedPackageDigest { get; }
        int PersonCount { get; }
        int HouseholdCount { get; }
        int FacilityCount { get; }
        IEnumerable<Luoyang184PermanentPersonRecord> ReadPersons(
            int startOrdinal, int count);
        IEnumerable<Luoyang184HouseholdRecord> ReadHouseholds(
            int startOrdinal, int count);
        IReadOnlyList<Luoyang184LivingFacilitySourceRecord> Facilities { get; }
        IReadOnlyList<Luoyang184MetropolitanAgricultureRecord> Agriculture { get; }
        IReadOnlyList<Luoyang184MetropolitanSupplyChainRecord> SupplyChains { get; }
        IReadOnlyList<Luoyang184T4SupplierSourceRecord> ExternalSuppliers { get; }
        IReadOnlyList<Luoyang184FamilyOrganizationSourceRecord>
            FamilyOrganizations { get; }
        IReadOnlyList<ulong> DevelopableCellIds { get; }
        string GetPersonId(uint ordinal);
        string GetHouseholdId(uint ordinal);
        string GetFacilityId(uint facilityIndex);
        string GetActivityId(ushort activityIndex);
        string GetOccupationId(ushort occupationIndex);
    }

    [Serializable]
    public sealed class Luoyang184FamilyOrganizationSourceRecord
    {
        public ushort Index;
        public string Id;
        public string HeadPersonId;
        public long Funds;
        public long AssetValue;
        public List<string> FacilityIds = new List<string>();
    }

    [Serializable]
    public sealed class Luoyang184T4SupplierSourceRecord
    {
        public string SupplierId;
        public LuoyangSupplierMaterializationLevel Level;
        public string CountyId;
        public string SettlementId;
        public string FacilityId;
        public string InventoryId;
        public string OrganizationId;
        public string ManagerPersonId;
        public string ManagerHouseholdId;
        public string ProductId;
        public long OpeningQuantityMilliunits;
        public long StorageCapacityMilliunits;
        public long DailyProductionMilliunits;
        public string RouteId;
        public int DistanceKilometers;
        public int TravelDays;
        public int NaturalLossBasisPoints;
        public int RiskLossBasisPoints;
        public string EvidenceGrade;
        public string SourceReferenceId;
    }

    [Serializable]
    public sealed class LuoyangExternalSupplierRuntimeState
    {
        public string SupplierId;
        public LuoyangSupplierMaterializationLevel Level;
        public string CountyId;
        public string SettlementId;
        public string FacilityId;
        public string InventoryId;
        public string OrganizationId;
        public string ManagerPersonId;
        public string ManagerHouseholdId;
        public string ProductId;
        public long InventoryQuantityMilliunits;
        public long StorageCapacityMilliunits;
        public long DailyProductionMilliunits;
        public string RouteId;
        public int DistanceKilometers;
        public int TravelDays;
        public int NaturalLossBasisPoints;
        public int RiskLossBasisPoints;
        public long CumulativeProducedMilliunits;
        public long CumulativeDispatchedMilliunits;
        public long CashBalance;
        public long CumulativeSalesRevenue;
        public long CumulativeOperatingExpense;
        public string EvidenceGrade;
        public string SourceReferenceId;
    }

    [Serializable]
    public sealed class LuoyangSupplyOrderRuntimeState
    {
        public string Id;
        public long RequestedDay;
        public string ProductId;
        public string SupplierId;
        public string DestinationInventoryId;
        public long RequestedQuantityMilliunits;
        public long DispatchedQuantityMilliunits;
        public long DeliveredQuantityMilliunits;
        public long UnitPrice;
        public long PurchaseCost;
        public LuoyangSupplyOrderStatus Status;
        public string ShipmentId;
        public string RequestedByAgentId;
        public string ReasonId;
    }

    [Serializable]
    public sealed class LuoyangShipmentRuntimeState
    {
        public string Id;
        public string OrderId;
        public string ProductId;
        public string SupplierId;
        public string SourceInventoryId;
        public string DestinationInventoryId;
        public string RouteId;
        public string CarrierPersonId;
        public long DispatchDay;
        public long ArrivalDay;
        public long ShippedQuantityMilliunits;
        public long CarrierConsumptionMilliunits;
        public long NaturalLossMilliunits;
        public long RiskLossMilliunits;
        public long DeliveredQuantityMilliunits;
        // DeliveredQuantityMilliunits is the net quantity placed in the
        // mobile formal container after dispatch loss.  Receipt can be
        // partial when the destination has no free capacity, so the two
        // fields below are the authoritative receipt progress.
        public long ReceivedQuantityMilliunits;
        public long RemainingCargoQuantityMilliunits;
        public long PurchaseCost;
        public bool Delivered;
        public bool AwaitingReceipt;
        public bool RouteWaiting;
        public string WaitingReasonId;
        public string WaitingFormalObjectId;
        public string PhysicalRouteSignature;
        public int RouteRevision;
        public bool PlayerDirected;
        public uint BuyerHouseholdOrdinal = uint.MaxValue;
        public bool PlayerSaleSettled;
        public long PlayerSaleRevenue;
    }

    [Serializable]
    public sealed class LuoyangMerchantCarrierRuntimeState
    {
        public string Id;
        public uint PersonOrdinal;
        public long CapacityMilliunits;
        public string CurrentShipmentId;
        public List<string> KnownRouteIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangWorkforceAssignmentState
    {
        public uint PersonOrdinal;
        public uint HouseholdOrdinal;
        public uint FacilityIndex = uint.MaxValue;
        public ushort OccupationIndex;
        public ushort ActivityIndex;
        public short Age;
        public LuoyangWorkforceStatus Status;
        public int EffectiveLaborBasisPoints;
        public string SocialRoleId;
        public string CurrentActivityId;
        public string CurrentLocationId = "location.capital.luoyang";
        public string TransitDestinationId;
        public long TransitArrivalDay = -1;
        public long CumulativeFoodDemandMilliunits;
        public long CumulativeFoodConsumedMilliunits;
    }

    [Serializable]
    public sealed class LuoyangFacilityProductionRuntimeState
    {
        public int FacilityIndex;
        public string FacilityId;
        public string DefinitionId;
        public string OwnerId;
        public string SettlementId;
        public ulong CellId64;
        public int ResidentCapacity;
        public int CurrentResidents;
        public string RecipeId;
        public string InputProductId;
        public string OutputProductId;
        public string OutputInventoryId;
        public int MinimumWorkers;
        public int OptimalWorkers;
        public int AssignedWorkers;
        public int EffectiveWorkersBasisPoints;
        public int ProductionProgressBasisPoints;
        public long CycleStartedDay = -1;
        public long CycleDueDay = -1;
        public long InputQuantity;
        public long OutputQuantity;
        public LuoyangProductionRuntimeStatus Status;
        public string StopReasonId;
        public string AiResponseActionId;
        public int ConditionBasisPoints = 10_000;
        public int RuntimeExpansionLevel;
    }

    [Serializable]
    public sealed class LuoyangCropRuntimeState
    {
        public string FieldId;
        public int FacilityIndex;
        public string FacilityId;
        public ulong CellId64;
        public string CropProductId;
        public string StorageInventoryId;
        public string SeedInventoryId;
        public long PlantingDay;
        public long FullMaturityDay;
        public long HarvestedDay = -1;
        public long NextPlantingDay = -1;
        public int CycleDurationDays;
        public int CycleNumber = 1;
        public int EarlyHarvestMinimumBasisPoints = 8_000;
        public int MaturityBasisPoints;
        public int HarvestQualityBasisPoints;
        public long FullYieldMilliunits;
        public long ActualYieldMilliunits;
        public long StoredYieldMilliunits;
        public long SeedRecoveredMilliunits;
        public long LostYieldMilliunits;
        public long CumulativeYieldMilliunits;
        public long CumulativeStoredYieldMilliunits;
        public long CumulativeSeedRecoveredMilliunits;
        public long CumulativeLostYieldMilliunits;
        public int AssignedWorkers;
        public LuoyangCropPhase Phase;
        public long NextDueDay = -1;
        public int ScheduleRevision;
    }

    [Serializable]
    public sealed class LuoyangAgricultureDueEntryState
    {
        public long DueDay;
        public int CropIndex;
        public int ScheduleRevision;
    }

    [Serializable]
    public sealed class LuoyangHouseholdConsumptionState
    {
        public string HouseholdId;
        public uint HouseholdOrdinal;
        public uint HeadPersonOrdinal;
        public uint MemberStartOrdinal;
        public ushort MemberCount;
        public ushort FamilyOrganizationIndex = ushort.MaxValue;
        public uint ResidenceFacilityIndex = uint.MaxValue;
        public long Wealth;
        public long FoodReserveMilliunits;
        public long CumulativeMoneySpent;
        public long CumulativeMoneyTaxPaid;
        public long CumulativeReliefReceivedMilliunits;
        public long LastConsumptionSettlementDay;
        public long DailyFoodDemandMilliunits;
        public long CumulativeFoodDemandMilliunits;
        public long CumulativeFoodAcquiredMilliunits;
        public long CumulativeFoodConsumedMilliunits;
        public long CumulativeFoodShortageMilliunits;
        public int FoodSecurityBasisPoints;
        public string LastAcquisitionSourceId;
        public string AiResponseActionId;
    }

    [Serializable]
    public sealed class LuoyangIntelligentAgentRuntimeState
    {
        public string Id;
        public string SubjectId;
        public string RepresentativePersonId;
        public int SubjectIndex = -1;
        public WorldAgentKind AgentKind;
        public LuoyangIntelligentAgentRole Role;
        public string GoalId;
        public int RiskPreferenceBasisPoints;
        public int DiligenceBasisPoints;
        public int AmbitionBasisPoints;
        public int CompassionBasisPoints;
        public long DecisionSequence;
        public long LastDecisionDay = -1;
        public long NextDecisionDay;
        public string LastActionTypeId;
        public long ExecutedActionCount;
        public long RejectedActionCount;
    }

    [Serializable]
    public sealed class LuoyangDecisionAuditState
    {
        public string Id;
        public long Day;
        public string AgentId;
        public LuoyangIntelligentAgentRole Role;
        public string SignalDigest;
        public string CandidateDigest;
        public string SelectedActionTypeId;
        public string ValidationReasonId;
        public bool Executed;
        public string ResultEntityId;
    }

    [Serializable]
    public sealed class LuoyangDecisionScheduleBucketState
    {
        public int BucketIndex;
        public List<int> AgentIndexes = new List<int>();
    }

    [Serializable]
    public sealed class LuoyangFamilyOrganizationRuntimeState
    {
        public ushort Index;
        public string Id;
        public string HeadPersonId;
        public long Funds;
        public long AssetValue;
        public int HouseholdCount;
        public string FamilyCenterFacilityId;
        public long MemberSupportPaid;
        public long InvestmentPaid;
        public string LastStrategyId;
    }

    [Serializable]
    public sealed class LuoyangCompactConstructionProjectState
    {
        public string Id;
        public LuoyangCompactConstructionKind Kind;
        public ulong CellId64;
        public string TargetFacilityId;
        public string ResultFacilityId;
        public string FacilityDefinitionId;
        public string OwnerId;
        public string MaterialInventoryId;
        public string MaterialProductId;
        public long MaterialQuantityMilliunits;
        public List<LuoyangCompactConstructionMaterialState> Materials =
            new List<LuoyangCompactConstructionMaterialState>();
        public long StartedDay;
        public long CompletionDay;
        public int RequiredLaborers;
        public List<uint> LaborerPersonOrdinals = new List<uint>();
        public long MoneyCost;
        public bool Completed;
        public bool Cancelled;
        public bool LegacyImported;
        public string MigrationNote;
        public string RequestedByAgentId;
    }

    [Serializable]
    public sealed class LuoyangCompactConstructionMaterialState
    {
        public string InventoryId;
        public string ProductId;
        public long ConsumedMilliunits;
    }

    [Serializable]
    public sealed class LuoyangCellPropertyRuntimeState
    {
        public ulong CellId64;
        public string OwnerId;
        public string AdministrativeControllerId;
        public string BuildingRightHolderId;
        public string FacilityId;
        public long LastTransferDay;
        public long LastTransferPrice;
        public int Revision;
    }

    [Serializable]
    public sealed class LuoyangCellPropertyTransferRuntimeState
    {
        public string Id;
        public ulong CellId64;
        public string FromOwnerId;
        public string ToOwnerId;
        public long Price;
        public long Day;
        public string AuthorizingPersonId;
    }

    [Serializable]
    public sealed class LuoyangGovernmentEconomyRuntimeState
    {
        public string OrganizationId = "organization.government.han.luoyang";
        public string CurrentLocationId = "location.capital.luoyang";
        public string GranaryInventoryId;
        public long Treasury;
        public long TaxRevenue;
        public long PurchaseExpense;
        public long ReliefExpense;
        public long ConstructionExpense;
        public string CurrentFoodPolicyId;
        public string CurrentDevelopmentPolicyId;
    }

    [Serializable]
    public sealed class LuoyangMarketTradeRuntimeState
    {
        public string Id;
        public long Day;
        public string ProductId;
        public string BuyerId;
        public string SellerId;
        public string SourceInventoryId;
        public long QuantityMilliunits;
        public long UnitPrice;
        public long MoneyTransferred;
        public string TradeOrderId;
    }

    [Serializable]
    public sealed class LuoyangInventoryBalanceState
    {
        public string Id;
        public LuoyangInventoryOwnerKind OwnerKind;
        public string OwnerId;
        public string FacilityId;
        public string ProductId;
        public string CurrentLocationId = "location.capital.luoyang";
        public string TransitDestinationId;
        public long TransitArrivalDay = -1;
        public long CapacityMilliunits;
        public long QuantityMilliunits;
        public int QualityBasisPoints = 10_000;
        public bool IsTransitionalReferenceSupply;
    }

    [Serializable]
    public sealed class LuoyangInventoryFlowState
    {
        public string Id;
        public long Day;
        public string OperationId;
        public string ProductId;
        public string SourceInventoryId;
        public string DestinationInventoryId;
        public long QuantityMilliunits;
        public long LossMilliunits;
        public string PersonId;
        public string HouseholdId;
        public string FacilityId;
    }

    [Serializable]
    public sealed class LuoyangMarketRuntimeState
    {
        public string ProductId;
        public long SupplyMilliunits;
        public long DemandMilliunits;
        public long TransferredMilliunits;
        public long FailedDemandMilliunits;
        public int BasePrice;
        public int CurrentPriceBasisPoints = 10_000;
        public long CashBalance = 100_000_000;
        public long RecentTradeQuantityMilliunits;
        public long RecentTradeValue;
        public int TransportCostBasisPoints;
        public int RiskBasisPoints;
        public int SeasonBasisPoints = 10_000;
        public int ShortageBasisPoints;
    }

    [Serializable]
    public sealed class LuoyangShortageResponseState
    {
        public string Id;
        public string SubjectKindId;
        public string SubjectId;
        public string ResourceId;
        public LuoyangShortageLevel Level;
        public string ResponseActionId;
        public long DetectedDay;
        public long DeficitMilliunits;
    }

    [Serializable]
    public sealed class LuoyangLivingWorldDaySnapshotState
    {
        public long Day;
        public long FoodStockMilliunits;
        public long FoodDemandMilliunits;
        public long FoodProducedMilliunits;
        public long FoodImportedMilliunits;
        public long FoodConsumedMilliunits;
        public long FoodLostMilliunits;
        public long FoodShortageMilliunits;
        public int ActiveProductionFacilities;
        public int IdleDueWorker;
        public int IdleDueInput;
        public int OutputBlocked;
        public int HouseholdShortageCount;
        public int HarvestableCrops;
        public int MatureCrops;
    }

    [Serializable]
    public sealed class LuoyangLivingWorldPerformanceState
    {
        public long InitializationMilliseconds;
        public long OneDayMilliseconds;
        public long SevenDayMilliseconds;
        public long ThirtyDayMilliseconds;
        public long ThreeHundredSixtyFiveDayMilliseconds;
        public long PeakManagedMemoryBytes;
        public long ConsumptionMilliseconds;
        public long ProductionMilliseconds;
        public long MarketMilliseconds;
        public long DecisionMilliseconds;
        public long DecisionIndexMilliseconds;
        public long HouseholdDecisionMilliseconds;
        public long FacilityDecisionMilliseconds;
        public long OrganizationDecisionMilliseconds;
        public long SupplyMilliseconds;
        public long ShortageMilliseconds;
    }

    [Serializable]
    public sealed class Luoyang184LivingWorldRuntimeState
    {
        public const int FormatVersion = 8;

        public int Version = FormatVersion;
        public bool RequiresSourceRehydration;
        public List<string> MigrationWarnings = new List<string>();
        public long AbsoluteDay;
        public ulong MasterSeed;
        public string SourcePackageId;
        public string ProtectedPackageDigest;
        public long DailyFoodDemandMilliunits;
        public int CurrentUnemployedCount;
        public int CurrentLocalPopulation;
        public int UnemployedSearchCursor;
        public int FacilityVacancySearchCursor;
        public List<LuoyangWorkforceAssignmentState> Workforce =
            new List<LuoyangWorkforceAssignmentState>();
        public List<LuoyangFacilityProductionRuntimeState> Facilities =
            new List<LuoyangFacilityProductionRuntimeState>();
        public List<LuoyangCropRuntimeState> Crops =
            new List<LuoyangCropRuntimeState>();
        public List<LuoyangAgricultureDueEntryState> AgricultureDueEntries =
            new List<LuoyangAgricultureDueEntryState>();
        public long AgricultureScheduleDispatchCount;
        public List<LuoyangHouseholdConsumptionState> Households =
            new List<LuoyangHouseholdConsumptionState>();
        public List<LuoyangInventoryBalanceState> Inventories =
            new List<LuoyangInventoryBalanceState>();
        public List<LuoyangInventoryFlowState> InventoryFlows =
            new List<LuoyangInventoryFlowState>();
        public List<LuoyangExternalSupplierRuntimeState> ExternalSuppliers =
            new List<LuoyangExternalSupplierRuntimeState>();
        public List<LuoyangSupplyOrderRuntimeState> SupplyOrders =
            new List<LuoyangSupplyOrderRuntimeState>();
        public List<LuoyangShipmentRuntimeState> Shipments =
            new List<LuoyangShipmentRuntimeState>();
        public List<LuoyangMerchantCarrierRuntimeState> MerchantCarriers =
            new List<LuoyangMerchantCarrierRuntimeState>();
        public List<LuoyangMarketRuntimeState> Markets =
            new List<LuoyangMarketRuntimeState>();
        public List<LuoyangMarketTradeRuntimeState> MarketTrades =
            new List<LuoyangMarketTradeRuntimeState>();
        public List<LuoyangIntelligentAgentRuntimeState> IntelligentAgents =
            new List<LuoyangIntelligentAgentRuntimeState>();
        public List<LuoyangDecisionAuditState> DecisionAudits =
            new List<LuoyangDecisionAuditState>();
        public List<LuoyangDecisionScheduleBucketState> DecisionScheduleBuckets =
            new List<LuoyangDecisionScheduleBucketState>();
        public List<LuoyangFamilyOrganizationRuntimeState> FamilyOrganizations =
            new List<LuoyangFamilyOrganizationRuntimeState>();
        public List<LuoyangCompactConstructionProjectState> ConstructionProjects =
            new List<LuoyangCompactConstructionProjectState>();
        public List<LuoyangCellPropertyRuntimeState> CellProperties =
            new List<LuoyangCellPropertyRuntimeState>();
        public List<LuoyangCellPropertyTransferRuntimeState> CellPropertyTransfers =
            new List<LuoyangCellPropertyTransferRuntimeState>();
        public List<LuoyangFamilyAssetRuntimeState> FamilyAssets =
            new List<LuoyangFamilyAssetRuntimeState>();
        public List<LuoyangPersonDevelopmentRuntimeState> PersonDevelopment =
            new List<LuoyangPersonDevelopmentRuntimeState>();
        public List<LuoyangOfficeRuntimeState> Offices =
            new List<LuoyangOfficeRuntimeState>();
        public List<LuoyangTaxRuntimeState> Taxes =
            new List<LuoyangTaxRuntimeState>();
        public List<LuoyangMilitaryForceRuntimeState> Forces =
            new List<LuoyangMilitaryForceRuntimeState>();
        public List<LuoyangSocialPressureRuntimeState> SocialPressureHistory =
            new List<LuoyangSocialPressureRuntimeState>();
        public List<LuoyangHistoricalEventRuntimeState> HistoricalEvents =
            new List<LuoyangHistoricalEventRuntimeState>();
        public List<LuoyangPlayerCommandRuntimeState> PlayerCommands =
            new List<LuoyangPlayerCommandRuntimeState>();
        public LuoyangGovernmentEconomyRuntimeState GovernmentEconomy =
            new LuoyangGovernmentEconomyRuntimeState();
        public List<LuoyangShortageResponseState> ShortageResponses =
            new List<LuoyangShortageResponseState>();
        public List<LuoyangLivingWorldDaySnapshotState> DaySnapshots =
            new List<LuoyangLivingWorldDaySnapshotState>();
        public LuoyangFormalEconomyRuntimeState FormalEconomy;
        public LuoyangLivingWorldPerformanceState Performance =
            new LuoyangLivingWorldPerformanceState();
    }

    [Serializable]
    public sealed class Luoyang184LivingWorldState
    {
        public string Id;
        public string ScenarioId;
        public string SourcePackageId;
        public string ProtectedPackageDigest;
        public string CheckpointRelativePath;
        public string CheckpointDigest;
        public long InitializedDay;
        public long LastSimulatedDay;
        public int PermanentPersonCount;
        public int HouseholdCount;
        public int FacilityCount;
        public int LaborEligibleCount;
        public int EmployedCount;
        public int UnemployedCount;
        public int MilitaryCount;
        public int OfficialCount;
        public int StudentCount;
        public int FamilyManagerCount;
        public int FacilitiesWithWorkers;
        public int FacilitiesIdleDueWorker;
        public int FacilitiesIdleDueInput;
        public int FacilitiesOutputBlocked;
        public long DailyFoodDemandMilliunits;
        public long LocalFoodProductionMilliunits;
        public long FoodImportMilliunits;
        public long FoodStockMilliunits;
        public long FoodConsumptionMilliunits;
        public long FoodLossMilliunits;
        public long FoodShortageMilliunits;
        public int HouseholdShortageCount;
        public bool SupplyRegionDependency;
        public string SupplyStatusId;
        public List<LuoyangLivingWorldDaySnapshotState> DaySnapshots =
            new List<LuoyangLivingWorldDaySnapshotState>();
    }

    public static class Luoyang184LivingWorldRules
    {
        public static int CalculateMaturityBasisPoints(
            long currentDay, long plantingDay, long fullMaturityDay)
        {
            if (fullMaturityDay <= plantingDay)
                throw new ArgumentOutOfRangeException(nameof(fullMaturityDay));
            if (currentDay <= plantingDay) return 0;
            var elapsed = currentDay - plantingDay;
            var duration = fullMaturityDay - plantingDay;
            return (int)Math.Max(0, Math.Min(12_000,
                elapsed * 10_000L / duration));
        }

        public static bool CanHarvest(int maturityBasisPoints,
            int earlyHarvestMinimumBasisPoints = 8_000) =>
            maturityBasisPoints >= earlyHarvestMinimumBasisPoints;

        public static long CalculateHarvestYield(
            long fullYieldMilliunits, int maturityBasisPoints)
        {
            if (!CanHarvest(maturityBasisPoints)) return 0;
            var yieldBasisPoints = maturityBasisPoints >= 10_000
                ? 10_000
                : 7_500 + (maturityBasisPoints - 8_000) * 2_500 / 2_000;
            return checked(fullYieldMilliunits * yieldBasisPoints / 10_000);
        }

        public static int CalculateHarvestQuality(int maturityBasisPoints) =>
            maturityBasisPoints >= 10_000
                ? 10_000
                : Math.Max(5_000, maturityBasisPoints - 1_500);

        public static void ValidateRuntime(
            Luoyang184LivingWorldRuntimeState runtime,
            int expectedPersons,
            int expectedHouseholds,
            int expectedFacilities)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (runtime.Version != Luoyang184LivingWorldRuntimeState.FormatVersion ||
                runtime.Workforce == null || runtime.Facilities == null ||
                runtime.Crops == null ||
                runtime.AgricultureDueEntries == null ||
                runtime.Households == null ||
                runtime.Inventories == null || runtime.InventoryFlows == null ||
                runtime.ExternalSuppliers == null ||
                runtime.SupplyOrders == null || runtime.Shipments == null ||
                runtime.MerchantCarriers == null ||
                runtime.Markets == null || runtime.MarketTrades == null ||
                runtime.IntelligentAgents == null ||
                runtime.DecisionAudits == null ||
                runtime.DecisionScheduleBuckets == null ||
                runtime.FamilyOrganizations == null ||
                runtime.ConstructionProjects == null ||
                runtime.CellProperties == null ||
                runtime.CellPropertyTransfers == null ||
                runtime.FamilyAssets == null ||
                runtime.PersonDevelopment == null ||
                runtime.Offices == null || runtime.Taxes == null ||
                runtime.Forces == null || runtime.SocialPressureHistory == null ||
                runtime.HistoricalEvents == null || runtime.PlayerCommands == null ||
                runtime.GovernmentEconomy == null ||
                runtime.ShortageResponses == null ||
                runtime.DaySnapshots == null || runtime.Performance == null ||
                runtime.FormalEconomy == null ||
                !runtime.FormalEconomy.IsPhysicalAuthority ||
                runtime.FormalEconomy.HouseholdFoodClaimsMilliunits == null ||
                runtime.FormalEconomy.HouseholdFoodClaimsMilliunits.Count !=
                    runtime.Households.Count ||
                runtime.FormalEconomy.InventoryContainers == null ||
                runtime.FormalEconomy.InventoryBindings == null ||
                runtime.FormalEconomy.ProductBatches == null ||
                runtime.FormalEconomy.InventoryTransactions == null)
                throw new InvalidOperationException("Invalid Luoyang living-world runtime collections.");
            if (runtime.Workforce.Count != expectedPersons ||
                runtime.Households.Count != expectedHouseholds ||
                runtime.Facilities.Count < expectedFacilities)
                throw new InvalidOperationException("Luoyang living-world protected counts changed.");
            if (runtime.CurrentLocalPopulation < 0 ||
                runtime.CurrentLocalPopulation > runtime.Workforce.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang current-location population cache.");

            var scheduledCrops = new HashSet<int>();
            long previousDueDay = long.MinValue;
            for (var i = 0; i < runtime.AgricultureDueEntries.Count; i++)
            {
                var entry = runtime.AgricultureDueEntries[i];
                if (entry == null || entry.CropIndex < 0 ||
                    entry.CropIndex >= runtime.Crops.Count ||
                    entry.DueDay <= runtime.AbsoluteDay ||
                    entry.DueDay < previousDueDay)
                    throw new InvalidOperationException(
                        "Invalid agriculture due entry.");
                var crop = runtime.Crops[entry.CropIndex];
                if (entry.ScheduleRevision == crop.ScheduleRevision &&
                    entry.DueDay == crop.NextDueDay &&
                    !scheduledCrops.Add(entry.CropIndex))
                    throw new InvalidOperationException(
                        "A crop has more than one active due entry.");
                previousDueDay = entry.DueDay;
            }
            if (scheduledCrops.Count != runtime.Crops.Count ||
                runtime.Crops.Any(item => item == null ||
                    item.NextDueDay <= runtime.AbsoluteDay ||
                    item.ScheduleRevision <= 0))
                throw new InvalidOperationException(
                    "Every Luoyang crop must have one future due schedule.");

            var activePersons = new HashSet<uint>();
            for (var i = 0; i < runtime.Workforce.Count; i++)
            {
                var assignment = runtime.Workforce[i] ??
                    throw new InvalidOperationException("A workforce assignment cannot be null.");
                if (!activePersons.Add(assignment.PersonOrdinal) ||
                    string.IsNullOrWhiteSpace(assignment.CurrentLocationId) ||
                    (assignment.TransitArrivalDay >= 0 &&
                     string.IsNullOrWhiteSpace(
                         assignment.TransitDestinationId)) ||
                    assignment.EffectiveLaborBasisPoints < 0 ||
                    assignment.CumulativeFoodDemandMilliunits < 0 ||
                    assignment.CumulativeFoodConsumedMilliunits < 0 ||
                    assignment.CumulativeFoodConsumedMilliunits >
                    assignment.CumulativeFoodDemandMilliunits)
                    throw new InvalidOperationException("Invalid Person workforce or consumption state.");
            }

            for (var i = 0; i < runtime.Inventories.Count; i++)
            {
                var inventory = runtime.Inventories[i];
                if (inventory == null || string.IsNullOrWhiteSpace(inventory.Id) ||
                    string.IsNullOrWhiteSpace(inventory.CurrentLocationId) ||
                    (inventory.TransitArrivalDay >= 0 &&
                     string.IsNullOrWhiteSpace(inventory.TransitDestinationId)) ||
                    inventory.QuantityMilliunits < 0 ||
                    inventory.CapacityMilliunits < 0 ||
                    inventory.QuantityMilliunits > inventory.CapacityMilliunits)
                    throw new InvalidOperationException("Invalid Luoyang inventory balance.");
            }

            var supplierIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var supplier in runtime.ExternalSuppliers)
            {
                if (supplier == null ||
                    !supplierIds.Add(supplier.SupplierId) ||
                    string.IsNullOrWhiteSpace(supplier.CountyId) ||
                    string.IsNullOrWhiteSpace(supplier.FacilityId) ||
                    string.IsNullOrWhiteSpace(supplier.InventoryId) ||
                    string.IsNullOrWhiteSpace(supplier.ProductId) ||
                    supplier.InventoryQuantityMilliunits < 0 ||
                    supplier.StorageCapacityMilliunits <= 0 ||
                    supplier.InventoryQuantityMilliunits >
                        supplier.StorageCapacityMilliunits ||
                    supplier.DailyProductionMilliunits < 0 ||
                    supplier.CashBalance < 0 ||
                    supplier.CumulativeOperatingExpense < 0 ||
                    supplier.TravelDays <= 0 ||
                    supplier.NaturalLossBasisPoints < 0 ||
                    supplier.RiskLossBasisPoints < 0)
                    throw new InvalidOperationException(
                        "Invalid Luoyang external supplier state.");
            }
            var orderIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var order in runtime.SupplyOrders)
            {
                if (order == null || !orderIds.Add(order.Id) ||
                    !supplierIds.Contains(order.SupplierId) ||
                    order.RequestedDay < 0 ||
                    order.RequestedQuantityMilliunits <= 0 ||
                    order.DispatchedQuantityMilliunits < 0 ||
                    order.DeliveredQuantityMilliunits < 0 ||
                    order.UnitPrice <= 0 || order.PurchaseCost < 0 ||
                    order.DeliveredQuantityMilliunits >
                        order.DispatchedQuantityMilliunits)
                    throw new InvalidOperationException(
                        "Invalid Luoyang supply order state.");
            }
            var shipmentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var shipment in runtime.Shipments)
            {
                if (shipment == null || !shipmentIds.Add(shipment.Id) ||
                    !orderIds.Contains(shipment.OrderId) ||
                    !supplierIds.Contains(shipment.SupplierId) ||
                    shipment.ArrivalDay <= shipment.DispatchDay ||
                    shipment.ShippedQuantityMilliunits <= 0 ||
                    shipment.CarrierConsumptionMilliunits < 0 ||
                    shipment.NaturalLossMilliunits < 0 ||
                    shipment.RiskLossMilliunits < 0 ||
                    shipment.DeliveredQuantityMilliunits < 0 ||
                    shipment.ReceivedQuantityMilliunits < 0 ||
                    shipment.RemainingCargoQuantityMilliunits < 0 ||
                    shipment.PurchaseCost < 0 ||
                    shipment.ReceivedQuantityMilliunits +
                        shipment.RemainingCargoQuantityMilliunits !=
                        shipment.DeliveredQuantityMilliunits ||
                    shipment.Delivered !=
                        (shipment.RemainingCargoQuantityMilliunits == 0) ||
                    shipment.AwaitingReceipt && shipment.Delivered ||
                    shipment.RouteWaiting && shipment.Delivered ||
                    shipment.PlayerSaleRevenue < 0 ||
                    shipment.ShippedQuantityMilliunits != checked(
                        shipment.CarrierConsumptionMilliunits +
                        shipment.NaturalLossMilliunits +
                        shipment.RiskLossMilliunits +
                        shipment.DeliveredQuantityMilliunits))
                    throw new InvalidOperationException(
                        "Invalid or unconserved Luoyang shipment state.");
            }
            var carrierIds = new HashSet<string>(StringComparer.Ordinal);
            var carrierPersons = new HashSet<uint>();
            foreach (var carrier in runtime.MerchantCarriers)
            {
                if (carrier == null || string.IsNullOrWhiteSpace(carrier.Id) ||
                    !carrierIds.Add(carrier.Id) ||
                    !carrierPersons.Add(carrier.PersonOrdinal) ||
                    carrier.PersonOrdinal >= runtime.Workforce.Count ||
                    carrier.CapacityMilliunits <= 0 ||
                    carrier.KnownRouteIds == null ||
                    carrier.KnownRouteIds.Any(string.IsNullOrWhiteSpace) ||
                    carrier.KnownRouteIds.Distinct(StringComparer.Ordinal)
                        .Count() != carrier.KnownRouteIds.Count ||
                    !string.IsNullOrWhiteSpace(carrier.CurrentShipmentId) &&
                    !runtime.Shipments.Exists(item =>
                        item.Id == carrier.CurrentShipmentId &&
                        item.PlayerDirected && !item.Delivered))
                    throw new InvalidOperationException(
                        "Invalid Luoyang player merchant carrier state.");
            }
            var tradeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var trade in runtime.MarketTrades)
            {
                if (trade == null || string.IsNullOrWhiteSpace(trade.Id) ||
                    !tradeIds.Add(trade.Id) || trade.Day < 0 ||
                    string.IsNullOrWhiteSpace(trade.ProductId) ||
                    string.IsNullOrWhiteSpace(trade.BuyerId) ||
                    string.IsNullOrWhiteSpace(trade.SellerId) ||
                    trade.QuantityMilliunits <= 0 || trade.UnitPrice <= 0 ||
                    trade.MoneyTransferred <= 0)
                    throw new InvalidOperationException("Invalid Luoyang market trade.");
            }

            var agentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var agent in runtime.IntelligentAgents)
            {
                if (agent == null || string.IsNullOrWhiteSpace(agent.Id) ||
                    !agentIds.Add(agent.Id) ||
                    !Enum.IsDefined(typeof(LuoyangIntelligentAgentRole), agent.Role) ||
                    agent.RiskPreferenceBasisPoints < 0 ||
                    agent.RiskPreferenceBasisPoints > 10_000 ||
                    agent.DiligenceBasisPoints < 0 || agent.DiligenceBasisPoints > 10_000 ||
                    agent.AmbitionBasisPoints < 0 || agent.AmbitionBasisPoints > 10_000 ||
                    agent.CompassionBasisPoints < 0 || agent.CompassionBasisPoints > 10_000 ||
                    agent.DecisionSequence < 0 || agent.NextDecisionDay < 0)
                    throw new InvalidOperationException("Invalid Luoyang intelligent Agent state.");
            }
            if (runtime.GovernmentEconomy.Treasury < 0 ||
                runtime.Markets.Exists(item => item.CashBalance < 0))
                throw new InvalidOperationException("Invalid Luoyang money balance.");
            if (string.IsNullOrWhiteSpace(
                    runtime.GovernmentEconomy.CurrentLocationId) ||
                string.IsNullOrWhiteSpace(
                    runtime.GovernmentEconomy.GranaryInventoryId) ||
                !runtime.Inventories.Exists(item => item.Id ==
                    runtime.GovernmentEconomy.GranaryInventoryId))
                throw new InvalidOperationException(
                    "Invalid Luoyang government location or granary contract.");
            var propertyCells = new HashSet<ulong>();
            foreach (var property in runtime.CellProperties)
            {
                if (property == null || property.CellId64 == 0 ||
                    !propertyCells.Add(property.CellId64) ||
                    string.IsNullOrWhiteSpace(property.OwnerId) ||
                    string.IsNullOrWhiteSpace(property.AdministrativeControllerId) ||
                    string.IsNullOrWhiteSpace(property.BuildingRightHolderId))
                    throw new InvalidOperationException(
                        "Invalid or duplicate Luoyang Cell property.");
            }
            foreach (var project in runtime.ConstructionProjects)
            {
                if (project == null ||
                    (!runtime.RequiresSourceRehydration && project.CellId64 == 0) ||
                    (!runtime.RequiresSourceRehydration &&
                     string.IsNullOrWhiteSpace(project.OwnerId)) ||
                    project.CompletionDay <= project.StartedDay ||
                    project.RequiredLaborers <= 0 ||
                    project.LaborerPersonOrdinals == null ||
                    !project.LegacyImported &&
                    project.LaborerPersonOrdinals.Count < project.RequiredLaborers ||
                    project.LaborerPersonOrdinals.Any(item =>
                        item >= runtime.Workforce.Count) ||
                    project.Materials == null ||
                    (!project.LegacyImported && project.Materials.Count < 2) ||
                    project.Materials.Any(item => item.ConsumedMilliunits <= 0 ||
                        !runtime.Inventories.Exists(inventory =>
                            inventory.Id == item.InventoryId)) ||
                    project.LegacyImported &&
                    string.IsNullOrWhiteSpace(project.MigrationNote))
                    throw new InvalidOperationException(
                        "Invalid Luoyang construction project backing facts.");
            }
        }

        public static void ValidateWorld(WorldState world)
        {
            if (world.LuoyangLivingWorlds == null)
                throw new InvalidOperationException("Luoyang living-world collection cannot be null.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var state in world.LuoyangLivingWorlds)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.Id) ||
                    !ids.Add(state.Id) || state.PermanentPersonCount < 0 ||
                    state.HouseholdCount < 0 || state.FacilityCount < 0 ||
                    state.EmployedCount < 0 || state.UnemployedCount < 0 ||
                    state.FoodStockMilliunits < 0 ||
                    state.FoodConsumptionMilliunits < 0 ||
                    state.FoodLossMilliunits < 0 ||
                    state.DaySnapshots == null)
                    throw new InvalidOperationException("Invalid Luoyang living-world summary.");
            }
        }
    }
}
