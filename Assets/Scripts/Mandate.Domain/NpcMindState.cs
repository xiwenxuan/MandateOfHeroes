using System;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class PersonalityState
    {
        public int Ambition = 5_000;
        public int FamilyDuty = 5_000;
        public int Sociability = 5_000;
        public int RiskTolerance = 5_000;
        public int Benevolence = 5_000;
    }

    [Serializable]
    public sealed class NeedState
    {
        public int Livelihood = 3_000;
        public int Family = 3_000;
        public int Status = 3_000;
        public int Wealth = 3_000;
        public int Relationships = 3_000;
        public int WarPressure;
    }
}
