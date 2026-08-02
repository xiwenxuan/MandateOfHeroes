using System;

namespace Mandate.Domain
{
    public enum MilitaryProcurementStatus : byte
    {
        InTransit,
        AwaitingArmy,
        Delivered
    }

    public enum MilitaryProcurementLedgerType : byte
    {
        DispatchPayment,
        ArmoryReceipt
    }

    [Serializable]
    public sealed class MilitaryProcurementOrderState
    {
        public string Id;
        public long CreatedDay;
        public long DeliveredDay = -1;
        public string BuyerOrganizationId;
        public string SupplierOrganizationId;
        public string IssuerPersonId;
        public string CarrierPersonId;
        public string TargetArmyId;
        public string EquipmentDefinitionId;
        public string ProductDefinitionId;
        public string SourceBatchId;
        public string InventoryContainerId;
        public string RouteId;
        public string JourneyId;
        public string OriginLocationId;
        public string DestinationLocationId;
        public int Quantity;
        public long UnitPrice;
        public long TotalPaid;
        public MilitaryProcurementStatus Status;
    }

    [Serializable]
    public sealed class MilitaryProcurementLedgerEntryState
    {
        public string Id;
        public long Day;
        public MilitaryProcurementLedgerType Type;
        public string ProcurementOrderId;
        public string BuyerOrganizationId;
        public string SupplierOrganizationId;
        public long BuyerMoneyDelta;
        public long SupplierMoneyDelta;
        public int ArmoryQuantityDelta;
        public string Summary;
    }
}
