using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum HistoricalAnchorStatus : byte
    {
        Dormant,
        Eligible,
        Resolved,
        Prevented,
        Transformed
    }

    public enum HistoricalEffectType : byte
    {
        AdjustPublicOrder,
        AdjustGrainPrice,
        SetWarPressure,
        AdjustRouteSecurity,
        SetTaskAvailability,
        SetArmyMobilized
    }

    [Serializable]
    public sealed class HistoricalEffectState
    {
        public HistoricalEffectType Type;
        public string TargetId;
        public int Value;
    }

    [Serializable]
    public sealed class HistoricalEventDefinitionState
    {
        public string Id;
        public string DisplayName;
        public long EarliestDay;
        public long LatestDay;
        public string PrerequisiteEventId;
        public string CanonicalOutcome;
        public List<HistoricalEffectState> Effects = new List<HistoricalEffectState>();
    }

    [Serializable]
    public sealed class HistoricalAnchorRuntimeState
    {
        public string Id;
        public string DefinitionId;
        public HistoricalAnchorStatus Status;
        public long ResolvedDay = -1;
        public string ActualOutcome;
        public List<string> CausalEventIds = new List<string>();
    }
}
