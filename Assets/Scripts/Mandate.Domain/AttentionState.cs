using System;

namespace Mandate.Domain
{
    public enum AttentionTargetKind : byte
    {
        Person,
        Family,
        Village,
        Facility,
        Organization
    }

    public enum AttentionLevel : byte
    {
        None,
        Normal,
        Deep
    }

    public enum AttentionLedgerChangeKind : byte
    {
        Added,
        Updated,
        Removed
    }

    [Serializable]
    public sealed class AttentionFocusState
    {
        public string Id;
        public string ObserverPersonId;
        public AttentionTargetKind TargetKind;
        public string TargetId;
        public AttentionLevel Level;
        public string ReasonId;
        public long CreatedDay;
        public long LastChangedDay;
    }

    [Serializable]
    public sealed class AttentionLedgerEntryState
    {
        public string Id;
        public long Day;
        public string ObserverPersonId;
        public AttentionTargetKind TargetKind;
        public string TargetId;
        public string ReasonId;
        public AttentionLedgerChangeKind ChangeKind;
        public AttentionLevel PreviousLevel;
        public AttentionLevel NewLevel;
    }
}
