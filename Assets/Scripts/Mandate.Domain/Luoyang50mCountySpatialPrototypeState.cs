using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mandate.Domain
{
    public static class Luoyang50mCountySpatialPrototypeIds
    {
        public const string ContractId =
            "mandate.luoyang.county-spatial-50m.prototype.v1";
        public const string CountyId =
            "admin.han140.sili.henan.luoyang";
        public const string PlacementProvenanceId =
            "spatial-provenance.gameplay-reconstruction.provisional.v1";
        public const string FootprintProvenanceId =
            "spatial-footprint.category-default.provisional.v1";
        public const string HistoricalPlacementGateId =
            "historical-placement.pending-authoritative-50m-source.v1";
        public const int CountyAreaSquareKilometres = 512;
        public const int Rows = 320;
        public const int Columns = 640;
        public const int StrategicRows = 8;
        public const int StrategicColumns = 16;
        public const int PlanningCellCount = 204_800;
        public const int ChunkCount = 800;
        public const int FacilityCount = 2_084;
        public const int DistrictCount = 6;
        public const int RoadFacilityCount = 359;
        public const int SourceRows = 65;
        public const int SourceColumns = 92;
        public const int SourceMinRow = 1202;
        public const int SourceMaxRow = 1266;
        public const int SourceMinColumn = 2013;
        public const int SourceMaxColumn = 2104;
    }

    public sealed class Luoyang50mFacilityMigrationCandidate
    {
        public Luoyang50mFacilityMigrationCandidate(
            string facilityId, string definitionId, string modelId,
            string categoryId, string districtId, string sourcePrecisionId,
            ulong sourceCellId64, int sourceRow, int sourceColumn,
            PlanningCellCoord candidateCell, int widthCentimetres,
            int depthCentimetres, bool preservesSourceStrategicTile)
        {
            FacilityId = new StableId(facilityId).Value;
            FacilityDefinitionId = new StableId(definitionId).Value;
            ModelId = new StableId(modelId).Value;
            CategoryId = categoryId ?? string.Empty;
            DistrictId = new StableId(districtId).Value;
            SourceSpatialPrecisionId = sourcePrecisionId ?? string.Empty;
            if (sourceCellId64 == 0 || sourceRow < 0 || sourceColumn < 0 ||
                widthCentimetres <= 0 || depthCentimetres <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCellId64));
            SourceCellId64 = sourceCellId64;
            SourceRow = sourceRow;
            SourceColumn = sourceColumn;
            CandidateCell = candidateCell;
            WidthCentimetres = widthCentimetres;
            DepthCentimetres = depthCentimetres;
            PreservesSourceStrategicTile = preservesSourceStrategicTile;
        }

        public string FacilityId { get; }
        public string FacilityDefinitionId { get; }
        public string ModelId { get; }
        public string CategoryId { get; }
        public string DistrictId { get; }
        public string SourceSpatialPrecisionId { get; }
        public ulong SourceCellId64 { get; }
        public int SourceRow { get; }
        public int SourceColumn { get; }
        public PlanningCellCoord CandidateCell { get; }
        public int WidthCentimetres { get; }
        public int DepthCentimetres { get; }
        public bool PreservesSourceStrategicTile { get; }
        public string PlacementProvenanceId =>
            Luoyang50mCountySpatialPrototypeIds.PlacementProvenanceId;
        public string FootprintProvenanceId =>
            Luoyang50mCountySpatialPrototypeIds.FootprintProvenanceId;
    }

    public sealed class Luoyang50mCountySpatialPrototype
    {
        public Luoyang50mCountySpatialPrototype(
            CountySpatialPartition partition,
            IReadOnlyList<Luoyang50mFacilityMigrationCandidate> facilities,
            IReadOnlyDictionary<string, int> facilityCountByDistrict,
            int sourceRoadStrategicCellCount,
            int sourceWaterStrategicCellCount,
            int facilityDerivedWaterPlanningCellCount,
            int roadFacilityCount, int fortificationFacilityCount,
            int sourceAnchorPreservedCount, double buildMilliseconds,
            long managedAllocationBytes, string declaredLayoutFingerprint,
            string runtimeLayoutHash, int roadNetworkEdgeCount,
            int canalNetworkEdgeCount, int districtAreaCount)
        {
            Partition = partition ?? throw new ArgumentNullException(
                nameof(partition));
            Facilities = facilities ?? throw new ArgumentNullException(
                nameof(facilities));
            FacilityCountByDistrict = facilityCountByDistrict ??
                throw new ArgumentNullException(nameof(facilityCountByDistrict));
            SourceRoadStrategicCellCount = sourceRoadStrategicCellCount;
            SourceWaterStrategicCellCount = sourceWaterStrategicCellCount;
            FacilityDerivedWaterPlanningCellCount =
                facilityDerivedWaterPlanningCellCount;
            RoadFacilityCount = roadFacilityCount;
            FortificationFacilityCount = fortificationFacilityCount;
            SourceAnchorPreservedCount = sourceAnchorPreservedCount;
            BuildMilliseconds = buildMilliseconds;
            ManagedAllocationBytes = managedAllocationBytes;
            DeclaredLayoutFingerprint = declaredLayoutFingerprint ??
                string.Empty;
            RuntimeLayoutHash = runtimeLayoutHash ?? string.Empty;
            RoadNetworkEdgeCount = roadNetworkEdgeCount;
            CanalNetworkEdgeCount = canalNetworkEdgeCount;
            DistrictAreaCount = districtAreaCount;
            Validate();
            DeterministicHash = ComputeHash();
        }

        public CountySpatialPartition Partition { get; }
        public IReadOnlyList<Luoyang50mFacilityMigrationCandidate> Facilities
            { get; }
        public IReadOnlyDictionary<string, int> FacilityCountByDistrict
            { get; }
        public int SourceRoadStrategicCellCount { get; }
        public int SourceWaterStrategicCellCount { get; }
        public int FacilityDerivedWaterPlanningCellCount { get; }
        public int RoadFacilityCount { get; }
        public int FortificationFacilityCount { get; }
        public int SourceAnchorPreservedCount { get; }
        public int ReconstructedPlacementCount =>
            Facilities.Count - SourceAnchorPreservedCount;
        public double BuildMilliseconds { get; }
        public long ManagedAllocationBytes { get; }
        public string LayoutPackageId => Luoyang50mCountyLayoutIds.PackageId;
        public string DeclaredLayoutFingerprint { get; }
        public string RuntimeLayoutHash { get; }
        public int RoadNetworkEdgeCount { get; }
        public int CanalNetworkEdgeCount { get; }
        public int DistrictAreaCount { get; }
        public string HistoricalPlacementGateId =>
            Luoyang50mCountySpatialPrototypeIds.HistoricalPlacementGateId;
        public string DeterministicHash { get; }

        private void Validate()
        {
            if (!string.Equals(Partition.CountyId,
                    Luoyang50mCountySpatialPrototypeIds.CountyId,
                    StringComparison.Ordinal) ||
                Partition.Rows != Luoyang50mCountySpatialPrototypeIds.Rows ||
                Partition.Columns !=
                    Luoyang50mCountySpatialPrototypeIds.Columns ||
                Partition.PlanningCellCount !=
                    Luoyang50mCountySpatialPrototypeIds.PlanningCellCount ||
                Partition.ChunkCount !=
                    Luoyang50mCountySpatialPrototypeIds.ChunkCount ||
                Partition.PackedArrayBytes != 2_457_600)
                throw new InvalidOperationException(
                    "Invalid Luoyang 50m county partition contract.");
            if (Facilities.Count !=
                    Luoyang50mCountySpatialPrototypeIds.FacilityCount ||
                Partition.FacilityPlacements.Count != Facilities.Count ||
                Facilities.Select(item => item.FacilityId).Distinct(
                    StringComparer.Ordinal).Count() != Facilities.Count ||
                Facilities.Select(item => item.SourceCellId64).Distinct()
                    .Count() != Facilities.Count ||
                Facilities.Any(item => !Partition.TryToLocal(
                    item.CandidateCell, out _, out _)))
                throw new InvalidOperationException(
                    "Luoyang Facility migration coverage is incomplete.");
            if (FacilityCountByDistrict.Count !=
                    Luoyang50mCountySpatialPrototypeIds.DistrictCount ||
                FacilityCountByDistrict.Values.Sum() != Facilities.Count ||
                RoadFacilityCount !=
                    Luoyang50mCountySpatialPrototypeIds.RoadFacilityCount ||
                SourceRoadStrategicCellCount < 0 ||
                SourceWaterStrategicCellCount < 0 ||
                FacilityDerivedWaterPlanningCellCount <= 0 ||
                SourceAnchorPreservedCount < 0 ||
                SourceAnchorPreservedCount > Facilities.Count ||
                BuildMilliseconds < 0d || ManagedAllocationBytes < 0 ||
                DeclaredLayoutFingerprint.Length != 64 ||
                RuntimeLayoutHash.Length != 64 ||
                RoadNetworkEdgeCount !=
                    Luoyang50mCountyLayoutIds.RoadEdgeCount ||
                CanalNetworkEdgeCount !=
                    Luoyang50mCountyLayoutIds.CanalEdgeCount ||
                DistrictAreaCount !=
                    Luoyang50mCountyLayoutIds.DistrictAreaCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang migration audit metrics.");
        }

        private string ComputeHash()
        {
            var builder = new StringBuilder(Partition.ComputeSpatialHash());
            foreach (var item in Facilities.OrderBy(value => value.FacilityId,
                         StringComparer.Ordinal))
                builder.Append('|').Append(item.FacilityId).Append(':')
                    .Append(item.SourceCellId64).Append(':')
                    .Append(item.CandidateCell.Row).Append(':')
                    .Append(item.CandidateCell.Column).Append(':')
                    .Append(item.WidthCentimetres).Append(':')
                    .Append(item.DepthCentimetres).Append(':')
                    .Append(item.SourceSpatialPrecisionId);
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(
                        builder.ToString()))
                    .Select(value => value.ToString("x2")));
        }
    }
}
