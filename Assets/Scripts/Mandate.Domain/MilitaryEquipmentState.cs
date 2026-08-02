using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum MilitaryEquipmentTransactionType : byte
    {
        OpeningStock,
        Issue,
        Return,
        Damage,
        Repair,
        Loss,
        Capture,
        Transfer,
        ProcurementReceipt
    }

    [Serializable]
    public sealed class MilitaryEquipmentDefinitionState
    {
        public string Id;
        public string DisplayName;
        public string CategoryId;
        public string SlotId;
        public string ProductDefinitionId;
        public string RepairMaterialProductDefinitionId;
        public int RepairMaterialQuantityPerUnit;
        public int RepairDurationDays;
        public string RepairFacilityTag;
        public int UnitWeight = 1;
        public int MaximumConditionBasisPoints = 10_000;
        public int MeleePowerBasisPoints;
        public int RangedPowerBasisPoints;
        public int ProtectionBasisPoints;
        public int RequiredStrengthBasisPoints;
        public int RequiredDexterityBasisPoints;
        public string CompatibleEquipmentId;
        public bool IsUnique;
    }

    [Serializable]
    public sealed class MilitaryArmoryStockState
    {
        public string Id;
        public string ArmyId;
        public string EquipmentDefinitionId;
        public int AvailableQuantity;
        public int DamagedQuantity;
        public int ReservedDamagedQuantity;
        public int AverageConditionBasisPoints = 10_000;
        public int OpeningQuantity;
    }

    [Serializable]
    public sealed class MilitaryEquipmentIssueState
    {
        public string Id;
        public string MilitaryServiceId;
        public string PersonId;
        public string ArmyId;
        public string EquipmentDefinitionId;
        public string SlotId;
        public int Quantity;
        public int ConditionBasisPoints;
        public long IssuedDay;
        public long LastChangedDay;
    }

    [Serializable]
    public sealed class MilitaryEquipmentTransactionState
    {
        public string Id;
        public long Day;
        public MilitaryEquipmentTransactionType Type;
        public string EquipmentDefinitionId;
        public int Quantity;
        public string FromArmyId;
        public string ToArmyId;
        public string MilitaryServiceId;
        public string BattleId;
        public string SourceProcurementOrderId;
        public string SourceRepairOrderId;
        public string Summary;
    }

    [Serializable]
    public sealed class MilitaryEquipmentRepairOrderState
    {
        public string Id;
        public string ArmyId;
        public string EquipmentDefinitionId;
        public string ProductionSiteId;
        public string InventoryContainerId;
        public string ManagerPersonId;
        public ProductionControlMode ControlMode;
        public ProductionOrderStatus Status;
        public long CreatedDay;
        public long FinishDay;
        public long SettledDay = -1;
        public int Quantity;
        public List<BatchReservationState> MaterialReservations =
            new List<BatchReservationState>();
    }
}
