using System;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class CommodityState
    {
        public string Id;
        public string DisplayName;
        public string ProductDefinitionId;
        public int BasePrice;
        public int UnitWeight = 1;
    }

    [Serializable]
    public sealed class MarketListingState
    {
        public string Id;
        public string LocationId;
        public string CommodityId;
        public int Price;
        public int EquilibriumPrice;
        public int Stock;
        public int TargetStock;
    }

    [Serializable]
    public sealed class InventoryStackState
    {
        public string Id;
        public string OwnerPersonId;
        public string CommodityId;
        public int Quantity;
        public int AverageUnitCost;
    }

    [Serializable]
    public sealed class TradeRecordState
    {
        public string Id;
        public long Day;
        public string PersonId;
        public string LocationId;
        public string CommodityId;
        public int Quantity;
        public int UnitPrice;
        public bool IsPurchase;
        public long MoneyChange;
    }
}
