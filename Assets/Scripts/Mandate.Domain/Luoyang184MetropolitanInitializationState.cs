using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public sealed class Luoyang184MetropolitanInitializationManifest
    {
        public string Schema { get; set; }
        public int FormatVersion { get; set; }
        public string ScenarioId { get; set; }
        public int ScenarioYear { get; set; }
        public string WorldId { get; set; }
        public string CityId { get; set; }
        public string PopulationProfileId { get; set; }
        public string BasePackageRelativePath { get; set; }
        public int BasePersonCount { get; set; }
        public int AddedPersonCount { get; set; }
        public int PersonCount { get; set; }
        public int BaseHouseholdCount { get; set; }
        public int AddedHouseholdCount { get; set; }
        public int HouseholdCount { get; set; }
        public int BaseFacilityCount { get; set; }
        public int AddedFacilityCount { get; set; }
        public int FacilityCount { get; set; }
        public int PersonRecordSize { get; set; }
        public int HouseholdRecordSize { get; set; }
        public int WalledCityPopulation { get; set; }
        public int UrbanAreaPopulation { get; set; }
        public int MetropolitanPopulation { get; set; }
        public int SupplyRegionPlanPopulation { get; set; }
        public int HistoricalPersonCount { get; set; }
        public List<Luoyang184UrbanPackageFile> BasePackageFiles { get; } = new List<Luoyang184UrbanPackageFile>();
        public List<Luoyang184UrbanPackageFile> Files { get; } = new List<Luoyang184UrbanPackageFile>();
    }

    public sealed class Luoyang184MetropolitanFacilityRecord
    {
        public int GlobalFacilityIndex { get; set; }
        public string FacilityId { get; set; }
        public string DefinitionId { get; set; }
        public string CategoryId { get; set; }
        public ulong CellId64 { get; set; }
        public string OwnerId { get; set; }
        public string AdministrativeControllerId { get; set; }
        public string AreaType { get; set; }
        public string SettlementId { get; set; }
        public int ResidentialCapacity { get; set; }
        public int CurrentResidents { get; set; }
        public int WorkerCapacity { get; set; }
        public int CurrentWorkers { get; set; }
        public long StorageCapacity { get; set; }
    }

    public sealed class Luoyang184MetropolitanRouteRecord
    {
        public string RouteId { get; set; }
        public string SettlementId { get; set; }
        public string GateFacilityId { get; set; }
        public List<ulong> CellIds { get; } = new List<ulong>();
        public int DistanceMetres { get; set; }
        public int TravelMinutes { get; set; }
        public bool UsesGateComplexTransition { get; set; }
        public int GateComplexTransitionSpanCells { get; set; }
    }

    public sealed class Luoyang184MetropolitanAgricultureRecord
    {
        public string FieldId { get; set; }
        public string FacilityId { get; set; }
        public ulong CellId64 { get; set; }
        public string ProductDefinitionId { get; set; }
        public int PlantedDay { get; set; }
        public int MaturityDay { get; set; }
        public int EarlyHarvestMinimumBasisPoints { get; set; }
        public long FullYieldUnits { get; set; }
        public List<uint> WorkerPersonOrdinals { get; } = new List<uint>();
        public string InventoryContainerId { get; set; }
    }

    public sealed class Luoyang184MetropolitanSupplyChainRecord
    {
        public string ChainId { get; set; }
        public string ProductDefinitionId { get; set; }
        public string ProducerFacilityId { get; set; }
        public string WarehouseFacilityId { get; set; }
        public uint CarrierPersonOrdinal { get; set; }
        public string GateFacilityId { get; set; }
        public string DestinationFacilityId { get; set; }
        public long ShippedUnits { get; set; }
        public long CarrierConsumptionUnits { get; set; }
        public long NaturalLossUnits { get; set; }
        public long RoadLossUnits { get; set; }
        public long DeliveredUnits { get; set; }
    }

    public sealed class Luoyang184MetropolitanEventImpact
    {
        public string EventId { get; set; }
        public int RecruitmentPersons { get; set; }
        public int TransportCapacityDelta { get; set; }
        public int GrainPriceBasisPoints { get; set; }
        public int MilitarySupplyUnits { get; set; }
        public int RoadCapacityDelta { get; set; }
        public int AgriculturalLaborDelta { get; set; }
        public int RefugeePressure { get; set; }
        public int SecurityPressure { get; set; }
        public int RoadInspectionPressure { get; set; }
    }

    public sealed class Luoyang184MetropolitanRuntimeState
    {
        public int RecruitmentPersons { get; set; }
        public int TransportCapacityDelta { get; set; }
        public int GrainPriceBasisPoints { get; set; } = 10000;
        public int MilitarySupplyUnits { get; set; }
        public int RoadCapacityDelta { get; set; }
        public int AgriculturalLaborDelta { get; set; }
        public int RefugeePressure { get; set; }
        public int SecurityPressure { get; set; }
        public int RoadInspectionPressure { get; set; }
        public Dictionary<string, long> InventoryUnitsByContainer { get; } =
            new Dictionary<string, long>(StringComparer.Ordinal);
        public HashSet<string> AppliedEventIds { get; } = new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class Luoyang184MetropolitanHarvestResult
    {
        public string FieldId { get; set; }
        public string ProductDefinitionId { get; set; }
        public int MaturityBasisPoints { get; set; }
        public long HarvestedUnits { get; set; }
        public bool RejectedAsTooEarly { get; set; }
    }

    public sealed class Luoyang184MetropolitanLogisticsResult
    {
        public int ChainCount { get; set; }
        public long ShippedUnits { get; set; }
        public long LostUnits { get; set; }
        public long DeliveredUnits { get; set; }
        public long CarrierConsumptionUnits { get; set; }
    }

    public sealed class Luoyang184MetropolitanForceJourneyState
    {
        public string ForceId { get; set; }
        public string RouteId { get; set; }
        public int CurrentRouteCellIndex { get; set; }
        public long SupplyUnits { get; set; }
        public bool Arrived { get; set; }
    }

    public sealed class LuoyangOuterSupplyCatchmentManifest
    {
        public string Schema { get; set; }
        public int FormatVersion { get; set; }
        public string CatchmentId { get; set; }
        public string WorldId { get; set; }
        public string CityId { get; set; }
        public string SourcePackageRelativePath { get; set; }
        public string SelectionContract { get; set; }
        public string AdministrativeEffect { get; set; }
        public bool IsProjectionOnly { get; set; }
        public int InclusivePopulationTarget { get; set; }
        public int MaterializedWorldPopulation { get; set; }
        public int MaterializedOuterPopulation { get; set; }
        public int UnmaterializedPopulationGap { get; set; }
        public int MaterializedOuterHouseholds { get; set; }
        public int SelectedFacilityCount { get; set; }
        public int SelectedSettlementCount { get; set; }
        public int SelectedAgricultureUnitCount { get; set; }
        public int SelectedStorageFacilityCount { get; set; }
        public int SelectedRoadFacilityCount { get; set; }
        public List<string> FoodProductDefinitionIds { get; } =
            new List<string>();
        public List<string> WoodProductDefinitionIds { get; } =
            new List<string>();
        public List<LuoyangOuterSupplyContentIdCrosswalk> ContentIdCrosswalks
        { get; } = new List<LuoyangOuterSupplyContentIdCrosswalk>();
        public List<Luoyang184UrbanPackageFile> SourceFiles { get; } =
            new List<Luoyang184UrbanPackageFile>();
    }

    public sealed class LuoyangOuterSupplyContentIdCrosswalk
    {
        public string SourceId { get; set; }
        public string FormalId { get; set; }
        public string MigrationId { get; set; }
    }

    public sealed class LuoyangOuterSupplyCatchmentDefinition
    {
        public string Id { get; set; }
        public List<ulong> CellIds { get; } = new List<ulong>();
        public List<string> FacilityIds { get; } = new List<string>();
        public List<string> SettlementIds { get; } = new List<string>();
        public List<string> FoodProductDefinitionIds { get; } =
            new List<string>();
        public List<string> WoodProductDefinitionIds { get; } =
            new List<string>();
        public List<LuoyangOuterSupplyContentIdCrosswalk> ContentIdCrosswalks
        { get; } = new List<LuoyangOuterSupplyContentIdCrosswalk>();
    }

    public sealed class LuoyangOuterSupplyCatchmentDataAudit
    {
        public int CellCount { get; set; }
        public int FacilityCount { get; set; }
        public int SettlementCount { get; set; }
        public int AgricultureUnitCount { get; set; }
        public int StorageFacilityCount { get; set; }
        public int RoadFacilityCount { get; set; }
        public int MaterializedWorldPopulation { get; set; }
        public int MaterializedOuterPopulation { get; set; }
        public int MaterializedOuterHouseholds { get; set; }
        public int InclusivePopulationTarget { get; set; }
        public int UnmaterializedPopulationGap { get; set; }
        public List<string> CriticalReferenceErrors { get; } =
            new List<string>();
        public List<string> UnresolvedContentDefinitionIds { get; } =
            new List<string>();
        public bool CriticalReferencesPassed =>
            CriticalReferenceErrors.Count == 0;
        public bool FormalContentBridgeComplete =>
            UnresolvedContentDefinitionIds.Count == 0;
        public bool PopulationTargetMaterialized =>
            UnmaterializedPopulationGap == 0 &&
            MaterializedWorldPopulation >= InclusivePopulationTarget;
    }
}
