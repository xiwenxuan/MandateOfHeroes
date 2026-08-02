using System;

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
        Transfer
    }

    [Serializable]
    public sealed class MilitaryEquipmentDefinitionState
    {
        public string Id;
        public string DisplayName;
        public string CategoryId;
        public string SlotId;
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
        public string Summary;
    }
}
