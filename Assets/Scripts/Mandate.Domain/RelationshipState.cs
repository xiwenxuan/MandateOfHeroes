using System;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class RelationshipState
    {
        public string Id;
        public string FromPersonId;
        public string ToPersonId;
        public int Affection;
        public int Trust;
        public int Respect;
        public int Obligation;
        public long LastInteractionDay = -1;
    }
}
