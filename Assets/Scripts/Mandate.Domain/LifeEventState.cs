using System;

namespace Mandate.Domain
{
    public enum PersonGender : byte
    {
        Unknown,
        Male,
        Female
    }

    public enum LifeEventType : byte
    {
        HouseholdDebt,
        Illness,
        Recovery,
        Birth,
        Death,
        Succession
    }

    [Serializable]
    public sealed class LifeEventRecordState
    {
        public string Id;
        public LifeEventType Type;
        public long Day;
        public string PrimaryPersonId;
        public string SecondaryPersonId;
        public string FamilyId;
        public string Summary;
    }
}
