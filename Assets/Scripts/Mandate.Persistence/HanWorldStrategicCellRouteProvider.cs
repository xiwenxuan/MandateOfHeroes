using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class HanWorldStrategicCellRouteProvider :
        IStrategicCellRouteProvider
    {
        public const string PlanVersionId =
            "hanworld.strategic-cell-route.v1";

        private readonly string _packageRoot;
        private readonly Dictionary<string, ulong[]> _authoredRoutes;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<ulong>>
            _authoredRouteView;

        public HanWorldStrategicCellRouteProvider(string packageRoot)
        {
            if (string.IsNullOrWhiteSpace(packageRoot))
                throw new ArgumentException(
                    "A Han world package root is required.",
                    nameof(packageRoot));
            _packageRoot = Path.GetFullPath(packageRoot);
            var path = Path.Combine(
                _packageRoot, "locations", "road_edges.json");
            var package = JsonConvert.DeserializeObject<RoadEdgePackage>(
                File.ReadAllText(path)) ?? throw new InvalidDataException(
                "Han world road-edge package is empty.");
            _authoredRoutes = new Dictionary<string, ulong[]>(
                StringComparer.Ordinal);
            if (package.Routes == null) return;
            foreach (var route in package.Routes)
            {
                if (route == null || string.IsNullOrWhiteSpace(route.RouteId) ||
                    route.CellIds == null || route.CellIds.Length < 2 ||
                    !_authoredRoutes.TryAdd(route.RouteId, route.CellIds))
                    throw new InvalidDataException(
                        "Han world road-edge identity is invalid.");
            }
            _authoredRouteView = _authoredRoutes.ToDictionary(
                item => item.Key,
                item => (IReadOnlyList<ulong>)Array.AsReadOnly(item.Value),
                StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, IReadOnlyList<ulong>>
            AuthoredRoutes => _authoredRouteView ??
                new Dictionary<string, IReadOnlyList<ulong>>();

        public bool TryBuildRoute(string assetRouteId,
            string formalWorldRouteId, ulong originCellId64,
            ulong targetCellId64, string movementCapabilityId,
            out StrategicCellRoutePlan plan, out string failureReasonId)
        {
            plan = null;
            if (!_authoredRoutes.TryGetValue(
                    assetRouteId ?? string.Empty, out var authored))
            {
                failureReasonId =
                    "strategic-cell-route.failure.unknown-asset-route.v1";
                return false;
            }
            if (string.IsNullOrWhiteSpace(formalWorldRouteId) ||
                !MovementCapabilityIds.All.Contains(
                    movementCapabilityId, StringComparer.Ordinal))
            {
                failureReasonId =
                    "strategic-cell-route.failure.invalid-request.v1";
                return false;
            }

            var forward = authored[0] == originCellId64 &&
                authored[authored.Length - 1] == targetCellId64;
            var reverse = authored[0] == targetCellId64 &&
                authored[authored.Length - 1] == originCellId64;
            if (!forward && !reverse)
            {
                failureReasonId =
                    "strategic-cell-route.failure.endpoint-mismatch.v1";
                return false;
            }
            var ordered = forward
                ? authored
                : authored.Reverse().ToArray();

            using var reader = new WorldMapDataReader(_packageRoot);
            var segments = new List<CellRouteSegment>();
            for (var i = 1; i < ordered.Length; i++)
            {
                if (!AppendAuthoredStep(reader, ordered[i - 1], ordered[i],
                        assetRouteId, formalWorldRouteId, segments,
                        out failureReasonId))
                    return false;
            }
            var route = new CellRoute(
                originCellId64, targetCellId64, movementCapabilityId,
                segments);
            var hash = ComputeHash(
                assetRouteId, formalWorldRouteId, route);
            plan = new StrategicCellRoutePlan(
                PlanVersionId, hash, assetRouteId, formalWorldRouteId, route);
            failureReasonId = string.Empty;
            return true;
        }

        private static bool AppendAuthoredStep(WorldMapDataReader reader,
            ulong fromCellId64, ulong toCellId64, string assetRouteId,
            string formalWorldRouteId, IList<CellRouteSegment> segments,
            out string failureReasonId)
        {
            if (!reader.Grid.TryDecode(
                    new WorldMapCellId(fromCellId64), out var fromRow,
                    out var fromColumn) ||
                !reader.Grid.TryDecode(
                    new WorldMapCellId(toCellId64), out var toRow,
                    out var toColumn))
            {
                failureReasonId =
                    "strategic-cell-route.failure.outside-grid.v1";
                return false;
            }
            var rowDelta = Math.Abs(toRow - fromRow);
            var columnDelta = Math.Abs(toColumn - fromColumn);
            if (rowDelta + columnDelta == 1)
            {
                AddSegment(segments, assetRouteId, formalWorldRouteId,
                    fromCellId64, toCellId64, 200_000);
                failureReasonId = string.Empty;
                return true;
            }
            if (rowDelta != 1 || columnDelta != 1)
            {
                failureReasonId =
                    "strategic-cell-route.failure.non-adjacent-authored-cell.v1";
                return false;
            }

            var first = reader.Grid.ToCellId(fromRow, toColumn).Value;
            var second = reader.Grid.ToCellId(toRow, fromColumn).Value;
            var firstCell = reader.ReadCell(new WorldMapCellId(first));
            var secondCell = reader.ReadCell(new WorldMapCellId(second));
            if (!firstCell.Passable && !secondCell.Passable)
            {
                failureReasonId =
                    "strategic-cell-route.failure.diagonal-corner-blocked.v1";
                return false;
            }
            var corner = !firstCell.Passable ? second :
                !secondCell.Passable ? first :
                firstCell.RoadClass > secondCell.RoadClass ? first :
                secondCell.RoadClass > firstCell.RoadClass ? second :
                Math.Min(first, second);
            const int halfDiagonalCentimetres = 141_421;
            AddSegment(segments, assetRouteId, formalWorldRouteId,
                fromCellId64, corner, halfDiagonalCentimetres);
            AddSegment(segments, assetRouteId, formalWorldRouteId,
                corner, toCellId64, halfDiagonalCentimetres);
            failureReasonId = string.Empty;
            return true;
        }

        private static void AddSegment(IList<CellRouteSegment> segments,
            string assetRouteId, string formalWorldRouteId,
            ulong fromCellId64, ulong toCellId64, int distanceCentimetres)
        {
            var sequence = segments.Count;
            segments.Add(new CellRouteSegment(
                sequence,
                "cell-route." + assetRouteId.ToLowerInvariant() + "." +
                    sequence.ToString("D4", CultureInfo.InvariantCulture),
                CellTraversalIds.BoundarySegmentKindId,
                fromCellId64,
                toCellId64,
                distanceCentimetres,
                1_000,
                CellTraversalIds.FormalRoadConditionId,
                formalWorldRouteId));
        }

        private static string ComputeHash(string assetRouteId,
            string formalWorldRouteId, CellRoute route)
        {
            var source = new StringBuilder(PlanVersionId)
                .Append('|').Append(assetRouteId)
                .Append('|').Append(formalWorldRouteId)
                .Append('|').Append(route.OriginCellId64)
                .Append('|').Append(route.TargetCellId64)
                .Append('|').Append(route.MovementCapabilityId);
            foreach (var segment in route.Segments)
                source.Append('|').Append(segment.Sequence)
                    .Append(':').Append(segment.FromCellId64)
                    .Append(':').Append(segment.ToCellId64)
                    .Append(':').Append(segment.DistanceCentimetres)
                    .Append(':').Append(segment.TraversalConditionId)
                    .Append(':').Append(segment.FormalWorldObjectId);
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(
                    Encoding.UTF8.GetBytes(source.ToString()))
                .Select(item => item.ToString(
                    "x2", CultureInfo.InvariantCulture)));
        }

        [Serializable]
        private sealed class RoadEdgePackage
        {
            [JsonProperty("routes")]
            public List<RoadEdge> Routes;
        }

        [Serializable]
        private sealed class RoadEdge
        {
            [JsonProperty("route_id")]
            public string RouteId;

            [JsonProperty("cell_ids")]
            public ulong[] CellIds;
        }
    }
}
