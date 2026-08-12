using System;
using System.Collections.Generic;

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

    [Serializable]
    public sealed class Luoyang184LivingFacilitySourceRecord
    {
        public int FacilityIndex;
        public string FacilityId;
        public string DefinitionId;
        public string CategoryId;
        public string OwnerId;
        public string ControllerId;
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
        string GetPersonId(uint ordinal);
        string GetHouseholdId(uint ordinal);
        string GetFacilityId(uint facilityIndex);
        string GetActivityId(ushort activityIndex);
        string GetOccupationId(ushort occupationIndex);
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
    }

    [Serializable]
    public sealed class LuoyangHouseholdConsumptionState
    {
        public uint HouseholdOrdinal;
        public uint HeadPersonOrdinal;
        public uint MemberStartOrdinal;
        public ushort MemberCount;
        public long Wealth;
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
    public sealed class LuoyangInventoryBalanceState
    {
        public string Id;
        public LuoyangInventoryOwnerKind OwnerKind;
        public string OwnerId;
        public string FacilityId;
        public string ProductId;
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
    }

    [Serializable]
    public sealed class Luoyang184LivingWorldRuntimeState
    {
        public const int FormatVersion = 1;

        public int Version = FormatVersion;
        public long AbsoluteDay;
        public ulong MasterSeed;
        public string SourcePackageId;
        public string ProtectedPackageDigest;
        public List<LuoyangWorkforceAssignmentState> Workforce =
            new List<LuoyangWorkforceAssignmentState>();
        public List<LuoyangFacilityProductionRuntimeState> Facilities =
            new List<LuoyangFacilityProductionRuntimeState>();
        public List<LuoyangCropRuntimeState> Crops =
            new List<LuoyangCropRuntimeState>();
        public List<LuoyangHouseholdConsumptionState> Households =
            new List<LuoyangHouseholdConsumptionState>();
        public List<LuoyangInventoryBalanceState> Inventories =
            new List<LuoyangInventoryBalanceState>();
        public List<LuoyangInventoryFlowState> InventoryFlows =
            new List<LuoyangInventoryFlowState>();
        public List<LuoyangMarketRuntimeState> Markets =
            new List<LuoyangMarketRuntimeState>();
        public List<LuoyangShortageResponseState> ShortageResponses =
            new List<LuoyangShortageResponseState>();
        public List<LuoyangLivingWorldDaySnapshotState> DaySnapshots =
            new List<LuoyangLivingWorldDaySnapshotState>();
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
                runtime.Crops == null || runtime.Households == null ||
                runtime.Inventories == null || runtime.InventoryFlows == null ||
                runtime.Markets == null || runtime.ShortageResponses == null ||
                runtime.DaySnapshots == null || runtime.Performance == null)
                throw new InvalidOperationException("Invalid Luoyang living-world runtime collections.");
            if (runtime.Workforce.Count != expectedPersons ||
                runtime.Households.Count != expectedHouseholds ||
                runtime.Facilities.Count != expectedFacilities)
                throw new InvalidOperationException("Luoyang living-world protected counts changed.");

            var activePersons = new HashSet<uint>();
            for (var i = 0; i < runtime.Workforce.Count; i++)
            {
                var assignment = runtime.Workforce[i] ??
                    throw new InvalidOperationException("A workforce assignment cannot be null.");
                if (!activePersons.Add(assignment.PersonOrdinal) ||
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
                    inventory.QuantityMilliunits < 0 ||
                    inventory.CapacityMilliunits < 0 ||
                    inventory.QuantityMilliunits > inventory.CapacityMilliunits)
                    throw new InvalidOperationException("Invalid Luoyang inventory balance.");
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
