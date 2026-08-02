using System;

namespace Mandate.Domain
{
    public enum BattleResultType : byte
    {
        AttackerVictory,
        DefenderVictory,
        Stalemate
    }

    public enum MilitarySupplyType : byte
    {
        TaskDelivery,
        MerchantSale,
        LocalMarketPurchase
    }

    [Serializable]
    public sealed class ArmyState
    {
        public string Id;
        public string DisplayName;
        public string OrganizationId;
        public string CommanderPersonId;
        public string LocationId;
        public int Troops;
        public int WoundedTroops;
        public int MaximumTroops;
        public int MoraleBasisPoints = 5_000;
        public int TrainingBasisPoints = 5_000;
        public int Provisions;
        public bool IsMobilized = true;
    }

    [Serializable]
    public sealed class ArmyMarchState
    {
        public string Id;
        public string ArmyId;
        public string RouteId;
        public string OriginLocationId;
        public string DestinationLocationId;
        public int RemainingKilometers;
        public long StartedDay;
    }

    [Serializable]
    public sealed class BattleRecordState
    {
        public string Id;
        public long Day;
        public string LocationId;
        public string AttackerArmyId;
        public string DefenderArmyId;
        public int AttackerInitialTroops;
        public int DefenderInitialTroops;
        public int AttackerCasualties;
        public int DefenderCasualties;
        public int AttackerWounded;
        public int DefenderWounded;
        public int AttackerEquipmentReadinessBasisPoints;
        public int DefenderEquipmentReadinessBasisPoints;
        public BattleResultType Result;
        public string WinnerArmyId;
        public string Summary;
    }

    [Serializable]
    public sealed class MilitarySupplyRecordState
    {
        public string Id;
        public long Day;
        public MilitarySupplyType Type;
        public string ArmyId;
        public string SupplierPersonId;
        public string SourceTaskInstanceId;
        public int GrainUnits;
        public int ProvisionsAdded;
        public int UnitPrice;
        public long TotalPaid;
        public string Summary;
    }
}
