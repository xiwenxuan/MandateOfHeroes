using System;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class ConstructionProjectState
    {
        public string Id;
        public string DisplayName;
        public string LocationId;
        public LocationFeature TargetFeature;
        public string SponsorPersonId;
        public long StartedDay;
        public int RequiredProgress;
        public int Progress;
        public long MoneyInvested;
        public bool IsCompleted;
        public long CompletedDay = -1;
    }
}
