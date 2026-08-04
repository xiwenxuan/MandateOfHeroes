using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class PublicReliefProcurementContractIds
    {
        public const string CommandTypeId =
            "mandate.command.public_relief.procure_shortfall";
        public const string ShortfallEventTypeId =
            "mandate.event.formal_public_food.county_relief_shortfall_detected";
        public const string ExternalSourcingRequiredEventTypeId =
            "mandate.event.public_relief.external_sourcing_required";
        public const string ExternalProcurementCommandTypeId =
            "mandate.command.public_relief.procure_external_shortfall";
        public const string ArrivalRecoveryCommandTypeId =
            "mandate.command.public_relief.recover_arrival";
    }

    public enum FormalMarketOrderSide : byte
    {
        Buy,
        Sell
    }

    public enum FormalMarketOrderStatus : byte
    {
        Active,
        Filled,
        Cancelled,
        Expired
    }

    [Serializable]
    public sealed class FormalMarketBatchReservationState
    {
        public string BatchId;
        public long OriginalQuantity;
        public long RemainingQuantity;
    }

    [Serializable]
    public sealed class FormalMarketOrderState
    {
        public string Id;
        public string CountyGovernanceId;
        public string OwnerFamilyId;
        public string StorageFacilityId;
        public string ProductDefinitionId;
        public FormalMarketOrderSide Side;
        public FormalMarketOrderStatus Status;
        public long CreatedDay;
        public long ExpiryDay;
        public long ClosedDay = -1;
        public long OriginalQuantity;
        public long RemainingQuantity;
        public long UnitPrice;
        public int MinimumQualityBasisPoints;
        public long EscrowMoney;
        public long FilledQuantity;
        public long SettledMoney;
        public string CloseReason;
        public List<FormalMarketBatchReservationState> BatchReservations =
            new List<FormalMarketBatchReservationState>();
    }

    [Serializable]
    public sealed class FormalMarketTradeState
    {
        public string Id;
        public long Day;
        public string CountyGovernanceId;
        public string DestinationCountyGovernanceId;
        public string BuyOrderId;
        public string SellOrderId;
        public string BuyerFamilyId;
        public string SellerFamilyId;
        public string ProductDefinitionId;
        public long Quantity;
        public long UnitPrice;
        public long MoneyTransferred;
        public long SellerProceeds;
        public string InventoryTransactionId;
        public string CivilianFreightId;
    }

    [Serializable]
    public sealed class FormalMarketPriceState
    {
        public string Id;
        public string CountyGovernanceId;
        public string ProductDefinitionId;
        public long EquilibriumUnitPrice;
        public long LastTradeUnitPrice;
        public long LastTradeDay = -1;
        public long CumulativeTradedQuantity;
        public long CumulativeTurnover;
    }

    [Serializable]
    public sealed class PublicReliefProcurementTradeState
    {
        public string Id;
        public long Day;
        public string CountyGovernanceId;
        public string SourceCountyGovernanceId;
        public string BuyerOrganizationId;
        public string DestinationInventoryContainerId;
        public string SourceShortfallEventId;
        public string SourceCommandId;
        public string SellOrderId;
        public string SellerFamilyId;
        public string ProductDefinitionId;
        public long Quantity;
        public long UnitPrice;
        public long MoneyTransferred;
        public string InventoryTransactionId;
        public string CivilianFreightId;
        public long FreightFee;
        public string PublicReliefRecoveryId;
        public bool IsSupplementalPublicReliefProcurement;
    }
}
