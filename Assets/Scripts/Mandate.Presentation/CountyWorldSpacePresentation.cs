using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Presentation
{
    public enum CountySurfaceVisualClass : byte
    {
        Plains,
        Hill,
        Forest,
        Farmland,
        BuiltUp,
        Waterside,
        Water
    }

    public enum CountyFacilityVisualKind : byte
    {
        SpecializedInfrastructure,
        ModelBatch,
        DetailedModel,
        Aggregate
    }

    public enum CountyFarAggregateKind : byte
    {
        Residential,
        Commercial,
        Workshop,
        Storage,
        Civic,
        Military,
        Mixed
    }

    /// <summary>
    /// A deterministic, presentation-only neighbourhood summary.  It is
    /// deliberately not a replacement for the facilities listed in the
    /// county layout: FacilityIds is the complete audit trail for the
    /// ordinary facilities suppressed at Far LOD.
    /// </summary>
    public sealed class CountyFarUrbanAggregate
    {
        public int BucketRow { get; set; }
        public int BucketColumn { get; set; }
        public float CenterRow { get; set; }
        public float CenterColumn { get; set; }
        public int FacilityCount { get; set; }
        public int MaximumHeightCentimetres { get; set; }
        public int RotationQuarterTurns { get; set; }
        public byte Density { get; set; }
        public bool IsInsideUrbanCandidate { get; set; }
        public CountyFarAggregateKind Kind { get; set; }
        public IReadOnlyList<string> FacilityIds { get; set; }
        public ulong StableSignature { get; set; }
    }

    public sealed class CountyWorldSpacePresentationSummary
    {
        public string PresentationVersion { get; set; }
        public string CacheKey { get; set; }
        public int TerrainChunkCount { get; set; }
        public int TerrainVertexCount { get; set; }
        public int WaterCellCount { get; set; }
        public int RoadSegmentCount { get; set; }
        public int RoadJunctionCount { get; set; }
        public int CanalSegmentCount { get; set; }
        public int FortificationSegmentCount { get; set; }
        public int GateCount { get; set; }
        public int FacilityCount { get; set; }
        public int ModelResolvedFacilityCount { get; set; }
        public int SpecializedInfrastructureCount { get; set; }
        public int FarAggregateCount { get; set; }
        public int FarLandmarkCount { get; set; }
        public int FarSuppressedOrdinaryFacilityCount { get; set; }
        public int AgriculturePatchCount { get; set; }
        public int VegetationInstanceCount { get; set; }
        public int PlanningCellGameObjectCount { get; set; }
        public int MaximumLocalPlanningGridCellCount { get; set; }
        public bool UrbanCandidateHullVisibleByDefault { get; set; }
        public bool IsDerivedPresentationOnly { get; set; }
        public ulong DeterministicSignature { get; set; }
    }

    /// <summary>
    /// Read-only planning and sampling contract for the world-space county
    /// renderer. It deliberately owns no WorldState and can be rebuilt from
    /// the formal 50 m layout package at any time.
    /// </summary>
    public sealed class CountyWorldSpacePresentationPlan
    {
        public const string Version =
            "mandate.luoyang.county-worldspace-presentation.v2";
        public const int TerrainChunkCells = 64;
        public const int TerrainSampleStepCells = 4;
        public const int FarAggregateBucketCells = 8;
        public const int PlanningGridRadiusCells = 12;
        public const int MaximumNearDetailedFacilities = 96;

        private readonly Luoyang50mCountyLayoutPackage _layout;
        private readonly CountySpatialPartition _partition;
        private readonly CountyMapPresentationStack _stack;
        private readonly IReadOnlyList<CountyFarUrbanAggregate> _farAggregates;
        private readonly IReadOnlyList<Luoyang50mLayoutFacility> _farLandmarks;

        public CountyWorldSpacePresentationPlan(
            Luoyang50mCountyLayoutPackage layout,
            CountySpatialPartition partition,
            CountyMapPresentationStack stack)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _partition = partition ?? throw new ArgumentNullException(
                nameof(partition));
            _stack = stack ?? throw new ArgumentNullException(nameof(stack));
            if (_layout.Rows != _partition.Rows ||
                _layout.Columns != _partition.Columns ||
                !string.Equals(_layout.CountyId, _partition.CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "County world-space inputs must describe one county.");
            _farLandmarks = _stack.FarFacilities
                .Where(item => item.Kind == CountyFacilityPresentationKind.Major)
                .Select(item => item.Facility)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .ToArray();
            _farAggregates = BuildFarAggregates();
        }

        public string CacheKey => _layout.DeclaredLayoutFingerprint + ":" +
                                  Version;
        public IReadOnlyList<CountyFarUrbanAggregate> FarAggregates =>
            _farAggregates;
        public IReadOnlyList<Luoyang50mLayoutFacility> FarLandmarks =>
            _farLandmarks;

        public static void AppendUpwardTerrainQuadTriangles(
            ICollection<int> triangles, int first, int columns)
        {
            if (triangles == null) throw new ArgumentNullException(
                nameof(triangles));
            if (first < 0 || columns < 1) throw new ArgumentOutOfRangeException(
                nameof(first));
            // County rows run towards -Z. This order therefore points both
            // triangles towards +Y and keeps the terrain front-facing.
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + columns + 1);
            triangles.Add(first + 1);
            triangles.Add(first + columns + 2);
            triangles.Add(first + columns + 1);
        }

        public float SurfaceHeight(float localRow, float localColumn)
        {
            var row = Math.Max(0f, Math.Min(_partition.Rows - 1f, localRow));
            var column = Math.Max(0f,
                Math.Min(_partition.Columns - 1f, localColumn));
            var r0 = (int)Math.Floor(row);
            var c0 = (int)Math.Floor(column);
            var r1 = Math.Min(_partition.Rows - 1, r0 + 1);
            var c1 = Math.Min(_partition.Columns - 1, c0 + 1);
            var tr = row - r0;
            var tc = column - c0;
            var northWest = _partition.GroundElevationDecimetres(r0, c0) /
                            10f;
            var northEast = _partition.GroundElevationDecimetres(r0, c1) /
                            10f;
            var southWest = _partition.GroundElevationDecimetres(r1, c0) /
                            10f;
            var southEast = _partition.GroundElevationDecimetres(r1, c1) /
                            10f;
            var north = northWest + (northEast - northWest) * tc;
            var south = southWest + (southEast - southWest) * tc;
            return north + (south - north) * tr;
        }

        public CountySurfaceVisualClass SurfaceClass(int row, int column)
        {
            if (_partition.WaterState(row, column) > 0)
                return CountySurfaceVisualClass.Water;
            var use = _partition.LandUse(row, column);
            if (use == PlanningLandUseClass.Agriculture)
                return CountySurfaceVisualClass.Farmland;
            if (use == PlanningLandUseClass.Residential ||
                use == PlanningLandUseClass.Industry ||
                use == PlanningLandUseClass.Government ||
                use == PlanningLandUseClass.Military)
                return CountySurfaceVisualClass.BuiltUp;
            if (HasAdjacentWater(row, column))
                return CountySurfaceVisualClass.Waterside;
            switch (_partition.Terrain(row, column))
            {
                case PlanningTerrainClass.Hill:
                    return CountySurfaceVisualClass.Hill;
                case PlanningTerrainClass.Forest:
                    return CountySurfaceVisualClass.Forest;
                case PlanningTerrainClass.Water:
                    return CountySurfaceVisualClass.Water;
                default:
                    return CountySurfaceVisualClass.Plains;
            }
        }

        public CountyFacilityVisualKind FacilityVisualKind(
            Luoyang50mLayoutFacility facility, CountyMapPresentationLod lod)
        {
            if (facility == null)
                throw new ArgumentNullException(nameof(facility));
            if (IsSpecializedInfrastructure(facility.DefinitionId))
                return CountyFacilityVisualKind.SpecializedInfrastructure;
            if (lod == CountyMapPresentationLod.Far)
                return CountyFacilityVisualKind.Aggregate;
            return lod == CountyMapPresentationLod.Near
                ? CountyFacilityVisualKind.DetailedModel
                : CountyFacilityVisualKind.ModelBatch;
        }

        public IReadOnlyList<PlanningCellCoord> LocalPlanningGrid(
            int centerRow, int centerColumn)
        {
            var result = new List<PlanningCellCoord>();
            var minimumRow = Math.Max(0, centerRow - PlanningGridRadiusCells);
            var maximumRow = Math.Min(_partition.Rows - 1,
                centerRow + PlanningGridRadiusCells);
            var minimumColumn = Math.Max(0,
                centerColumn - PlanningGridRadiusCells);
            var maximumColumn = Math.Min(_partition.Columns - 1,
                centerColumn + PlanningGridRadiusCells);
            for (var row = minimumRow; row <= maximumRow; row++)
            for (var column = minimumColumn; column <= maximumColumn; column++)
                result.Add(new PlanningCellCoord(row, column));
            return result;
        }

        public CountyWorldSpacePresentationSummary CreateSummary(
            Func<Luoyang50mLayoutFacility, bool> modelResolver = null)
        {
            var chunkRows = (_partition.Rows + TerrainChunkCells - 1) /
                            TerrainChunkCells;
            var chunkColumns = (_partition.Columns + TerrainChunkCells - 1) /
                               TerrainChunkCells;
            var terrainVertices = 0;
            for (var chunkRow = 0; chunkRow < chunkRows; chunkRow++)
            for (var chunkColumn = 0; chunkColumn < chunkColumns; chunkColumn++)
            {
                var rows = Math.Min(TerrainChunkCells,
                    _partition.Rows - chunkRow * TerrainChunkCells);
                var columns = Math.Min(TerrainChunkCells,
                    _partition.Columns - chunkColumn * TerrainChunkCells);
                terrainVertices += (rows / TerrainSampleStepCells + 1) *
                                   (columns / TerrainSampleStepCells + 1);
            }

            var roadDegree = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var edge in _layout.RoadEdges)
            {
                roadDegree[edge.FromNodeId] = roadDegree.TryGetValue(
                    edge.FromNodeId, out var first) ? first + 1 : 1;
                roadDegree[edge.ToNodeId] = roadDegree.TryGetValue(
                    edge.ToNodeId, out var second) ? second + 1 : 1;
            }

            var waterCells = 0;
            var vegetation = 0;
            for (var row = 0; row < _partition.Rows; row++)
            for (var column = 0; column < _partition.Columns; column++)
            {
                if (_partition.WaterState(row, column) > 0) waterCells++;
                if ((row % 6 == 0 && column % 6 == 0) &&
                    (SurfaceClass(row, column) ==
                         CountySurfaceVisualClass.Forest ||
                     HasAdjacentWater(row, column)) &&
                    StableModulo(row, column, 3) != 0)
                    vegetation++;
            }

            var specialized = _layout.Facilities.Count(item =>
                IsSpecializedInfrastructure(item.DefinitionId));
            var resolved = modelResolver == null
                ? _layout.Facilities.Count - specialized
                : _layout.Facilities.Count(item =>
                    IsSpecializedInfrastructure(item.DefinitionId) ||
                    modelResolver(item));
            var summary = new CountyWorldSpacePresentationSummary
            {
                PresentationVersion = Version,
                CacheKey = CacheKey,
                TerrainChunkCount = chunkRows * chunkColumns,
                TerrainVertexCount = terrainVertices,
                WaterCellCount = waterCells,
                RoadSegmentCount = _layout.RoadEdges.Count,
                RoadJunctionCount = roadDegree.Count(item => item.Value >= 3),
                CanalSegmentCount = _layout.CanalEdges.Count,
                FortificationSegmentCount = _layout.Fortifications.Count,
                GateCount = _layout.Fortifications.Count(item => item.IsGate),
                FacilityCount = _layout.Facilities.Count,
                ModelResolvedFacilityCount = resolved,
                SpecializedInfrastructureCount = specialized,
                FarAggregateCount = _farAggregates.Count,
                FarLandmarkCount = _farLandmarks.Count,
                FarSuppressedOrdinaryFacilityCount = _farAggregates.Sum(
                    item => item.FacilityCount),
                AgriculturePatchCount = _layout.Facilities.Count(item =>
                    string.Equals(item.CategoryId, "agriculture",
                        StringComparison.Ordinal) ||
                    item.DefinitionId.IndexOf("agriculture",
                        StringComparison.Ordinal) >= 0),
                VegetationInstanceCount = vegetation,
                PlanningCellGameObjectCount = 0,
                MaximumLocalPlanningGridCellCount =
                    (PlanningGridRadiusCells * 2 + 1) *
                    (PlanningGridRadiusCells * 2 + 1),
                UrbanCandidateHullVisibleByDefault = false,
                IsDerivedPresentationOnly = true
            };
            summary.DeterministicSignature = SummarySignature(summary);
            return summary;
        }

        public static bool IsSpecializedInfrastructure(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId)) return false;
            return definitionId.StartsWith("facility.public.road",
                       StringComparison.Ordinal) ||
                   definitionId.IndexOf("waterway", StringComparison.Ordinal) >= 0 ||
                   definitionId.IndexOf("canal", StringComparison.Ordinal) >= 0 ||
                   definitionId.StartsWith("facility.fortification.",
                       StringComparison.Ordinal);
        }

        public static bool IsAgriculturalFacility(
            Luoyang50mLayoutFacility facility) => facility != null &&
            (string.Equals(facility.CategoryId, "agriculture",
                 StringComparison.Ordinal) ||
             string.Equals(facility.CategoryId, "resource_agriculture",
                 StringComparison.Ordinal) ||
             facility.DefinitionId.StartsWith("facility.agriculture.",
                 StringComparison.Ordinal));

        public static int StableModulo(int row, int column, int divisor)
        {
            if (divisor <= 0) throw new ArgumentOutOfRangeException(
                nameof(divisor));
            unchecked
            {
                var value = (uint)(row * 73856093) ^
                            (uint)(column * 19349663) ^ 0x9E3779B9u;
                return (int)(value % (uint)divisor);
            }
        }

        private bool HasAdjacentWater(int row, int column)
        {
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                var candidateRow = row + dr;
                var candidateColumn = column + dc;
                if (candidateRow < 0 || candidateRow >= _partition.Rows ||
                    candidateColumn < 0 ||
                    candidateColumn >= _partition.Columns) continue;
                if (_partition.WaterState(candidateRow, candidateColumn) > 0)
                    return true;
            }
            return false;
        }

        private IReadOnlyList<CountyFarUrbanAggregate> BuildFarAggregates()
        {
            var landmarks = new HashSet<string>(_farLandmarks.Select(
                item => item.FacilityId), StringComparer.Ordinal);
            return _layout.Facilities
                .Where(item => !IsSpecializedInfrastructure(
                                   item.DefinitionId) &&
                               !IsAgriculturalFacility(item) &&
                               !landmarks.Contains(item.FacilityId))
                .GroupBy(item => Tuple.Create(
                    item.LocalRow / FarAggregateBucketCells,
                    item.LocalColumn / FarAggregateBucketCells))
                .OrderBy(group => group.Key.Item1)
                .ThenBy(group => group.Key.Item2)
                .Select(BuildFarAggregate)
                .ToArray();
        }

        private CountyFarUrbanAggregate BuildFarAggregate(
            IGrouping<Tuple<int, int>, Luoyang50mLayoutFacility> group)
        {
            var facilities = group.OrderBy(item => item.FacilityId,
                StringComparer.Ordinal).ToArray();
            var ids = facilities.Select(item => item.FacilityId).ToArray();
            var centerRow = facilities.Average(item => item.LocalRow);
            var centerColumn = facilities.Average(item => item.LocalColumn);
            var category = facilities.GroupBy(item => AggregateKind(
                    item.CategoryId))
                .OrderByDescending(item => item.Count())
                .ThenBy(item => item.Key)
                .Select(item => item.Key).FirstOrDefault();
            var rotation = facilities.GroupBy(item =>
                    item.RotationQuarterTurns)
                .OrderByDescending(item => item.Count())
                .ThenBy(item => item.Key)
                .Select(item => item.Key).FirstOrDefault();
            var row = Math.Max(0, Math.Min(_partition.Rows - 1,
                (int)Math.Round(centerRow)));
            var column = Math.Max(0, Math.Min(_partition.Columns - 1,
                (int)Math.Round(centerColumn)));
            var area = _layout.UrbanAreaCandidate;
            var aggregate = new CountyFarUrbanAggregate
            {
                BucketRow = group.Key.Item1,
                BucketColumn = group.Key.Item2,
                CenterRow = (float)centerRow,
                CenterColumn = (float)centerColumn,
                FacilityCount = facilities.Length,
                MaximumHeightCentimetres = facilities.Max(item =>
                    item.HeightCentimetres),
                RotationQuarterTurns = rotation,
                Density = _stack.UrbanDensityAt(row, column),
                IsInsideUrbanCandidate = row >= area.MinimumRow &&
                                         row <= area.MaximumRow &&
                                         column >= area.MinimumColumn &&
                                         column <= area.MaximumColumn,
                Kind = category,
                FacilityIds = ids
            };
            aggregate.StableSignature = StableTextHash(string.Join("|",
                ids));
            return aggregate;
        }

        private static CountyFarAggregateKind AggregateKind(string category)
        {
            switch (category)
            {
                case "residential": return CountyFarAggregateKind.Residential;
                case "commercial": return CountyFarAggregateKind.Commercial;
                case "industry":
                case "resource": return CountyFarAggregateKind.Workshop;
                case "storage": return CountyFarAggregateKind.Storage;
                case "government":
                case "ritual":
                case "education":
                case "public": return CountyFarAggregateKind.Civic;
                case "military": return CountyFarAggregateKind.Military;
                default: return CountyFarAggregateKind.Mixed;
            }
        }

        private static ulong StableTextHash(string value)
        {
            unchecked
            {
                var hash = 14695981039346656037UL;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }

        private static ulong SummarySignature(
            CountyWorldSpacePresentationSummary summary)
        {
            var text = string.Join("|", new[]
            {
                summary.PresentationVersion, summary.CacheKey,
                summary.TerrainChunkCount.ToString(),
                summary.TerrainVertexCount.ToString(),
                summary.WaterCellCount.ToString(),
                summary.RoadSegmentCount.ToString(),
                summary.RoadJunctionCount.ToString(),
                summary.CanalSegmentCount.ToString(),
                summary.FortificationSegmentCount.ToString(),
                summary.GateCount.ToString(), summary.FacilityCount.ToString(),
                summary.ModelResolvedFacilityCount.ToString(),
                summary.SpecializedInfrastructureCount.ToString(),
                summary.FarAggregateCount.ToString(),
                summary.FarLandmarkCount.ToString(),
                summary.FarSuppressedOrdinaryFacilityCount.ToString(),
                summary.AgriculturePatchCount.ToString(),
                summary.VegetationInstanceCount.ToString(),
                summary.MaximumLocalPlanningGridCellCount.ToString()
            });
            unchecked
            {
                var value = 14695981039346656037UL;
                foreach (var character in text)
                {
                    value ^= character;
                    value *= 1099511628211UL;
                }
                return value;
            }
        }
    }
}
