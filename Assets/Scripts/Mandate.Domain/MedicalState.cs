using System;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class MedicalTreatmentRecordState
    {
        public string Id;
        public long Day;
        public string PhysicianPersonId;
        public string ArmyId;
        public int PatientsTreated;
        public int RecoveredTroops;
        public int HerbsConsumed;
        public string Summary;
    }
}
