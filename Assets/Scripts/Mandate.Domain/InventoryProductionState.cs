using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum InventoryTransactionType : byte
    {
        LegacyBalanceConverted,
        Reserved,
        ReservationReleased,
        RecipeSettled,
        OpeningBalance,
        MilitaryProcurementDispatched,
        EquipmentRepairReserved,
        EquipmentRepairSettled,
        ResourceExtractionSettled
    }

    [Serializable]
    public sealed class InventoryContainerState
    {
        public string Id;
        public string KindId;
        public string OwnerFamilyId;
        public string OwnerOrganizationId;
        public string CarrierPersonId;
        public string LocationId;
        public long CapacityWeight;
    }

    [Serializable]
    public sealed class ProductionSiteState
    {
        public string Id;
        public string KindId;
        public string OwnerOrganizationId;
        public string LocationId;
        public string ManagerPersonId;
        public string InventoryContainerId;
        public int ConcurrentOrderCapacity = 1;
        public int ConditionBasisPoints = 10_000;
        public List<string> FacilityTags = new List<string>();
    }

    [Serializable]
    public sealed class ProductBatchState
    {
        public string Id;
        public string ProductDefinitionId;
        public string OwnerFamilyId;
        public string OwnerOrganizationId;
        public string StorageFacilityId;
        public string InventoryContainerId;
        public string OriginLocationId;
        public string SourceWorkOrderId;
        public string SourceTransactionId;
        public string CropVarietyDefinitionId;
        public string UnitId;
        public int UnitWeight = 1;
        public long ProducedDay;
        public long Quantity;
        public long ReservedQuantity;
        public int QualityBasisPoints = 10_000;
        public int FreshnessBasisPoints = 10_000;
        public int SeedVigorBasisPoints;
        public int SeedPurityBasisPoints;
    }

    [Serializable]
    public sealed class InventoryTransactionLineState
    {
        public string BatchId;
        public string ProductDefinitionId;
        public string OwnerFamilyId;
        public string OwnerOrganizationId;
        public string StorageFacilityId;
        public string InventoryContainerId;
        public string UnitId;
        public long QuantityDelta;
        public long ReservedQuantityDelta;
    }

    [Serializable]
    public sealed class InventoryTransactionState
    {
        public string Id;
        public long Day;
        public InventoryTransactionType Type;
        public string ActorPersonId;
        public string SourceWorkOrderId;
        public string SourceMilitaryProcurementId;
        public string SourceEquipmentRepairOrderId;
        public string SourceResourceExtractionOrderId;
        public long LegacyFamilyGrainDelta;
        public long LegacyFamilySeedGrainDelta;
        public long FacilityInventoryDelta;
        public string Summary;
        public List<InventoryTransactionLineState> Lines =
            new List<InventoryTransactionLineState>();
    }

    [Serializable]
    public sealed class BatchReservationState
    {
        public string BatchId;
        public long Quantity;
    }

    [Serializable]
    public sealed class ProcessingWorkOrderState
    {
        public string Id;
        public string RecipeDefinitionId;
        public string MethodDefinitionId;
        public string OwnerFamilyId;
        public string StorageFacilityId;
        public string OwnerOrganizationId;
        public string ProductionSiteId;
        public string InventoryContainerId;
        public string ManagerPersonId;
        public ProductionControlMode ControlMode;
        public ProductionOrderStatus Status;
        public long CreatedDay;
        public long FinishDay;
        public long SettledDay = -1;
        public int RunCount;
        public List<BatchReservationState> InputReservations =
            new List<BatchReservationState>();
        public List<string> OutputBatchIds = new List<string>();
    }

    public enum ResourceExtractionLedgerEntryType : byte
    {
        Reserved,
        Settled
    }

    [Serializable]
    public sealed class ResourceBodyState
    {
        public string Id;
        public string ResourceKindId;
        public string OutputProductDefinitionId;
        public string LocationId;
        public string Provenance;
        public string GenerationRuleVersion;
        public string RequiredFacilityTag;
        public long InitialQuantity;
        public long RemainingQuantity;
        public long ReservedQuantity;
        public int QualityBasisPoints = 10_000;
        public int ExtractionDifficultyBasisPoints = 10_000;
    }

    [Serializable]
    public sealed class ResourceExtractionOrderState
    {
        public string Id;
        public string ResourceBodyId;
        public string OwnerOrganizationId;
        public string ProductionSiteId;
        public string InventoryContainerId;
        public string ManagerPersonId;
        public List<string> WorkerPersonIds = new List<string>();
        public ProductionControlMode ControlMode;
        public ProductionOrderStatus Status;
        public long CreatedDay;
        public long FinishDay;
        public long SettledDay = -1;
        public long RequestedQuantity;
        public long ExtractedQuantity;
        public string OutputBatchId;
    }

    [Serializable]
    public sealed class ResourceExtractionLedgerEntryState
    {
        public string Id;
        public long Day;
        public ResourceExtractionLedgerEntryType Type;
        public string ResourceBodyId;
        public string ResourceExtractionOrderId;
        public string ActorPersonId;
        public long RemainingQuantityDelta;
        public long ReservedQuantityDelta;
        public string OutputBatchId;
        public long OutputQuantity;
        public string Summary;
    }
}
