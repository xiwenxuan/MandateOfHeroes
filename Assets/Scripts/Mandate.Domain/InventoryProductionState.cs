using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum InventoryTransactionType : byte
    {
        LegacyBalanceConverted,
        Reserved,
        ReservationReleased,
        RecipeSettled
    }

    [Serializable]
    public sealed class ProductBatchState
    {
        public string Id;
        public string ProductDefinitionId;
        public string OwnerFamilyId;
        public string StorageFacilityId;
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
        public string StorageFacilityId;
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
}
