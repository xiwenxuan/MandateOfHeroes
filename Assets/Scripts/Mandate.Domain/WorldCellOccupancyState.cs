using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class WorldCellOccupancyState
    {
        public WorldCellOccupancyState(WorldMapCellAddress address)
        {
            Address = address;
        }

        public WorldMapCellAddress Address { get; }
        public string OwnerId { get; private set; }
        public string FacilityId { get; private set; }
        public string ForceId { get; private set; }

        public void TransferOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner is required.", nameof(ownerId));
            OwnerId = ownerId;
        }

        public void BuildFacility(string actingOwnerId, string facilityId)
        {
            if (!string.Equals(OwnerId, actingOwnerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Only the current Cell owner may build a Facility.");
            if (string.IsNullOrWhiteSpace(facilityId)) throw new ArgumentException("Facility is required.", nameof(facilityId));
            if (!string.IsNullOrEmpty(FacilityId)) throw new InvalidOperationException("A Cell may contain only one base Facility.");
            FacilityId = facilityId;
        }

        public void PlaceForce(string forceId)
        {
            if (string.IsNullOrWhiteSpace(forceId)) throw new ArgumentException("Force is required.", nameof(forceId));
            if (!string.IsNullOrEmpty(ForceId)) throw new InvalidOperationException("A Cell may contain only one independent Force.");
            ForceId = forceId;
        }

        public void RemoveForce(string forceId)
        {
            if (!string.Equals(ForceId, forceId, StringComparison.Ordinal))
                throw new InvalidOperationException("The requested Force does not occupy this Cell.");
            ForceId = null;
        }
    }

    [Serializable]
    public sealed class CityFootprintState
    {
        private readonly HashSet<WorldMapCellAddress> _cells = new HashSet<WorldMapCellAddress>();

        public CityFootprintState(string cityId, WorldMapCellAddress anchor)
        {
            CityId = string.IsNullOrWhiteSpace(cityId) ? throw new ArgumentException("City ID is required.", nameof(cityId)) : cityId;
            Anchor = anchor;
        }

        public string CityId { get; }
        public WorldMapCellAddress Anchor { get; }
        public IReadOnlyCollection<WorldMapCellAddress> Cells => _cells;
        public bool AddFacilityCell(WorldMapCellAddress address) => _cells.Add(address);
        public bool Contains(WorldMapCellAddress address) => _cells.Contains(address);
    }
}
