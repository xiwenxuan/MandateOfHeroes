using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum PublicReliefRecoveryStatus : byte
    {
        SupplementalInTransit,
        DistributionBlocked,
        Fulfilled,
        Exhausted
    }

    [Serializable]
    public sealed class PublicReliefVillageRecoveryState
    {
        public string VillageId;
        public long RequiredQuantity;
        public long RecoveredQuantity;
        public long RemainingQuantity;
        public List<string> InventoryTransactionIds = new List<string>();
    }

    [Serializable]
    public sealed class PublicReliefFreightRecoveryReportState
    {
        public string Id;
        public string CivilianFreightId;
        public string PublicReliefProcurementTradeId;
        public bool IsSupplemental;
        public long DispatchedQuantity;
        public long NaturalLossQuantity;
        public long DeliveredQuantity;
        public long RecoveryDistributedQuantity;
        public long DispatchedDay;
        public long ArrivedDay;
        public long CompletedDay;
        public long ReconciledDay;
        public long TransitDays;
        public long ReceiptWaitingDays;
        public string ExceptionCode;
    }

    [Serializable]
    public sealed class PublicReliefRecoveryState
    {
        public string Id;
        public PublicReliefRecoveryStatus Status;
        public string CountyGovernanceId;
        public string SourceShortfallEventId;
        public string SourceExternalSourcingEventId;
        public long SourceShortfallDay;
        public long ExternalShortfallQuantity;
        public long TotalDispatchedQuantity;
        public long TotalNaturalLossQuantity;
        public long TotalDeliveredQuantity;
        public long TotalRecoveredQuantity;
        public long RemainingQuantity;
        public int SupplementalAttemptCount;
        public long SupplementalRequestedQuantity;
        public string SupplementalFreightId;
        public long LastRecoveryDay;
        public List<PublicReliefVillageRecoveryState> VillageRecoveries =
            new List<PublicReliefVillageRecoveryState>();
        public List<PublicReliefFreightRecoveryReportState> FreightReports =
            new List<PublicReliefFreightRecoveryReportState>();
    }
}
