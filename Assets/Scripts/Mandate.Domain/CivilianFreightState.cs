using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum CivilianFreightStatus : byte
    {
        InTransit,
        AwaitingNextLeg,
        AwaitingReceipt,
        Completed
    }

    public static class CivilianFreightPurposeIds
    {
        public const string FormalMarketDelivery =
            "civilian-freight.purpose.formal-market-delivery.v1";
        public const string PublicReliefProcurement =
            "civilian-freight.purpose.public-relief-procurement.v1";
        public const string MerchantOwnerCarriage =
            "civilian-freight.purpose.merchant-owner-carriage.v1";

        public static readonly IReadOnlyList<string> All = new[]
        {
            FormalMarketDelivery,
            PublicReliefProcurement,
            MerchantOwnerCarriage
        };
    }

    public enum CivilianFreightDemandStatus : byte
    {
        Active,
        Dispatched,
        Cancelled,
        Expired
    }

    public enum CivilianCarrierOfferStatus : byte
    {
        Active,
        Accepted,
        Rejected,
        Withdrawn
    }

    public static class CivilianFreightRoutePolicyIds
    {
        public const string ShortestKnown =
            "mandate.route_policy.shortest_known";
        public const string SafestKnown =
            "mandate.route_policy.safest_known";
    }

    public enum CivilianFreightLedgerType : byte
    {
        Dispatched,
        NaturalLoss,
        Delivered,
        FreightFeePaid
    }

    [Serializable]
    public sealed class CivilianFreightCellRouteSegmentState
    {
        public int Sequence;
        public string Id;
        public string KindId;
        public ulong FromCellId64;
        public ulong ToCellId64;
        public int DistanceCentimetres;
        public int TraversalCostPermille;
        public string TraversalConditionId;
        public string FormalWorldObjectId;

        public long WeightedDistanceCentimetres => Math.Max(
            1L,
            (long)DistanceCentimetres * TraversalCostPermille / 1_000L);
    }

    [Serializable]
    public sealed class CivilianFreightState
    {
        public string Id;
        public string PurposeId = string.Empty;
        public CivilianFreightStatus Status;
        public string BuyOrderId;
        public string SellOrderId;
        public string FormalMarketTradeId;
        public string DemandId;
        public string CarrierOfferId;
        public string OriginCountyGovernanceId;
        public string DestinationCountyGovernanceId;
        public string OriginLocationId;
        public string DestinationLocationId;
        public string BuyerFamilyId;
        public string BuyerOrganizationId;
        public string SellerFamilyId;
        public string BuyerStorageFacilityId;
        public string DestinationInventoryContainerId;
        public string SellerStorageFacilityId;
        public string PublicReliefProcurementTradeId;
        public string SourcePublicReliefEventId;
        public string SourcePublicReliefCommandId;
        public string PublicReliefRecoveryId;
        public bool IsSupplementalPublicReliefFreight;
        public string CarrierPersonId;
        public string CarrierFamilyId;
        public string TransportInventoryContainerId;
        public string RouteId;
        public string JourneyId;
        public List<string> PlannedRouteIds = new List<string>();
        public int CurrentRouteIndex;
        public bool UsesCellRoute;
        public string CellRoutePlanVersionId = string.Empty;
        public string CellRouteAssetHash = string.Empty;
        public string CellRouteMovementCapabilityId = string.Empty;
        public ulong CellRouteOriginCellId64;
        public ulong CellRouteTargetCellId64;
        public ulong CellRouteCurrentCellId64;
        public List<CivilianFreightCellRouteSegmentState> CellRouteSegments =
            new List<CivilianFreightCellRouteSegmentState>();
        public int CurrentCellRouteSegmentIndex;
        public long CurrentCellRouteSegmentRemainingWeightedCentimetres;
        public long CellRouteRemainingWeightedCentimetres;
        public bool CellRouteWaiting;
        public string CellRouteWaitingReasonId = string.Empty;
        public string CellRouteWaitingOnFormalWorldObjectId = string.Empty;
        public int CellRouteRevision;
        public string DispatchInventoryTransactionId;
        public string ProductDefinitionId;
        public long DispatchedQuantity;
        public long RemainingCargoQuantity;
        public long DeliveredQuantity;
        public long NaturalLossQuantity;
        public long GoodsUnitPrice;
        public long GoodsMoneyTransferred;
        public long FreightFee;
        public long FreightFeeEscrow;
        public long FreightFeePaid;
        public int ProductPerishabilityBasisPoints;
        public int FoodSpoilageSensitivityBasisPoints;
        public int CargoUnitWeight;
        public long CreatedDay;
        public long DispatchedDay;
        public long LastLossDay;
        public long ArrivedDay = -1;
        public long CompletedDay = -1;
    }

    [Serializable]
    public sealed class CivilianFreightDemandState
    {
        public string Id;
        public CivilianFreightDemandStatus Status;
        public string BuyOrderId;
        public string SellOrderId;
        public string OriginCountyGovernanceId;
        public string DestinationCountyGovernanceId;
        public string OriginLocationId;
        public string DestinationLocationId;
        public string ProductDefinitionId;
        public long Quantity;
        public long MaximumFreightFee;
        public string RoutePolicyId;
        public long CreatedDay;
        public long ExpiryDay;
        public long ClosedDay = -1;
        public string AcceptedOfferId;
        public string CivilianFreightId;
    }

    [Serializable]
    public sealed class CivilianCarrierRegistrationState
    {
        public string Id;
        public bool Active = true;
        public string CarrierPersonId;
        public string CarrierFamilyId;
        public string TransportInventoryContainerId;
        public long BaseFee;
        public long FeePerKilometer;
        public long FeePerHundredUnits;
        public int MaximumDistanceKilometers;
        public string RoutePolicyId;
        public long RegisteredDay;
        public List<string> KnownRouteIds = new List<string>();
    }

    [Serializable]
    public sealed class CivilianCarrierOfferState
    {
        public string Id;
        public CivilianCarrierOfferStatus Status;
        public string DemandId;
        public string CarrierRegistrationId;
        public string CarrierPersonId;
        public string CarrierFamilyId;
        public string TransportInventoryContainerId;
        public string RoutePolicyId;
        public List<string> PlannedRouteIds = new List<string>();
        public int TotalDistanceKilometers;
        public int MinimumSecurityBasisPoints;
        public long QuotedFreightFee;
        public long CreatedDay;
        public long ClosedDay = -1;
        public string CivilianFreightId;
    }

    [Serializable]
    public sealed class CivilianFreightLedgerEntryState
    {
        public string Id;
        public long Day;
        public CivilianFreightLedgerType Type;
        public string CivilianFreightId;
        public string ActorPersonId;
        public string InventoryTransactionId;
        public long Quantity;
        public long Money;
        public string Summary;
    }
}
