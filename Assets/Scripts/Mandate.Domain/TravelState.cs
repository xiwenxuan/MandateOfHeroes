using System;

namespace Mandate.Domain
{
    public enum TravelMode : byte
    {
        Foot,
        Mounted,
        Caravan,
        MilitaryUnit
    }

    [Serializable]
    public sealed class RouteState
    {
        public string Id;
        public string FromLocationId;
        public string ToLocationId;
        public int DistanceKilometers;
        public bool Bidirectional = true;
        public int SecurityBasisPoints = 5_000;
    }

    [Serializable]
    public sealed class JourneyState
    {
        public string Id;
        public string PersonId;
        public string RouteId;
        public string OriginLocationId;
        public string DestinationLocationId;
        public TravelMode Mode;
        public int RemainingKilometers;
        public long StartedDay;
        public byte StartedSegment;
    }
}
