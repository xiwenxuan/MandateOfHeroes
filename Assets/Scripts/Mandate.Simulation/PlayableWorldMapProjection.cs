using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum PlayableWorldMapRouteStatus : byte
    {
        Unavailable,
        Planned,
        InTransit,
        Waiting,
        Arrived,
        Completed
    }

    public sealed class PlayableWorldMapProjection
    {
        public const string ContractId =
            "presentation.playable-world-map-projection.v1";

        public string PlayerPersonId = string.Empty;
        public string PlayerName = string.Empty;
        public string OriginName = string.Empty;
        public string DestinationName = string.Empty;
        public string AssetRouteId = string.Empty;
        public string FormalWorldRouteId = string.Empty;
        public string PlanVersionId = string.Empty;
        public string AssetHash = string.Empty;
        public string FailureReasonId = string.Empty;
        public ulong OriginCellId64;
        public ulong TargetCellId64;
        public ulong CurrentCellId64;
        public int CurrentCellSequence;
        public long TotalWeightedCentimetres;
        public long RemainingWeightedCentimetres;
        public bool UsesFormalCellRoute;
        public PlayableWorldMapRouteStatus Status;
        public List<ulong> CellIds = new List<ulong>();

        public bool HasRoute => UsesFormalCellRoute && CellIds.Count >= 2;
    }

    public static class PlayableWorldMapProjectionSystem
    {
        public static PlayableWorldMapProjection Build(
            WorldState world,
            string personId,
            MerchantHouseholdContentRegistry content,
            IStrategicCellRouteProvider strategicRouteProvider)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (strategicRouteProvider == null)
                throw new ArgumentNullException(nameof(strategicRouteProvider));

            var person = world.People.Find(item => item.Id == personId) ??
                throw new InvalidOperationException(
                    "The playable map player does not exist.");
            var result = new PlayableWorldMapProjection
            {
                PlayerPersonId = person.Id,
                PlayerName = person.DisplayName,
                CurrentCellId64 = person.CurrentCellId64,
                Status = PlayableWorldMapRouteStatus.Unavailable
            };
            var task = world.Tasks.Find(item =>
                item.AssigneePersonId == personId &&
                item.DefinitionId ==
                    MerchantHouseholdGameplayService.PrimaryTaskDefinitionId);
            if (task == null) return result;

            var goal = content.GetGoal(MerchantHouseholdContentIds.FirstGoal);
            result.OriginName = LocationName(world, goal.OriginLocationId);
            result.DestinationName = LocationName(world, goal.TargetLocationId);
            result.AssetRouteId = goal.CellRouteAssetRouteId;
            result.FormalWorldRouteId = goal.RouteId;
            result.OriginCellId64 = goal.CellRouteOriginCellId64;
            result.TargetCellId64 = goal.CellRouteTargetCellId64;

            var freight = FindLatestMerchantFreight(world, personId);
            if (freight != null && freight.UsesCellRoute &&
                freight.CellRouteSegments != null &&
                freight.CellRouteSegments.Count > 0)
            {
                BuildFromFreight(result, freight);
                return result;
            }

            if (!strategicRouteProvider.TryBuildRoute(
                    goal.CellRouteAssetRouteId,
                    goal.RouteId,
                    goal.CellRouteOriginCellId64,
                    goal.CellRouteTargetCellId64,
                    goal.CellRouteMovementCapabilityId,
                    out var plan,
                    out var failureReasonId))
            {
                result.FailureReasonId = failureReasonId ?? string.Empty;
                return result;
            }

            result.PlanVersionId = plan.VersionId;
            result.AssetHash = plan.AssetHash;
            result.TotalWeightedCentimetres =
                plan.Route.WeightedDistanceCentimetres;
            result.RemainingWeightedCentimetres =
                plan.Route.WeightedDistanceCentimetres;
            result.CurrentCellId64 = plan.Route.OriginCellId64;
            result.CurrentCellSequence = 0;
            result.UsesFormalCellRoute = true;
            result.Status = PlayableWorldMapRouteStatus.Planned;
            result.CellIds.Add(plan.Route.OriginCellId64);
            for (var i = 0; i < plan.Route.Segments.Count; i++)
                result.CellIds.Add(plan.Route.Segments[i].ToCellId64);
            Validate(result);
            return result;
        }

        private static void BuildFromFreight(PlayableWorldMapProjection result,
            CivilianFreightState freight)
        {
            result.PlanVersionId = freight.CellRoutePlanVersionId;
            result.AssetHash = freight.CellRouteAssetHash;
            result.AssetRouteId = string.IsNullOrWhiteSpace(
                result.AssetRouteId)
                ? freight.RouteId
                : result.AssetRouteId;
            result.FormalWorldRouteId = freight.RouteId;
            result.OriginCellId64 = freight.CellRouteOriginCellId64;
            result.TargetCellId64 = freight.CellRouteTargetCellId64;
            result.CurrentCellId64 = freight.CellRouteCurrentCellId64;
            result.CurrentCellSequence = Math.Max(0, Math.Min(
                freight.CurrentCellRouteSegmentIndex,
                freight.CellRouteSegments.Count));
            result.RemainingWeightedCentimetres =
                freight.CellRouteRemainingWeightedCentimetres;
            result.UsesFormalCellRoute = true;
            result.CellIds.Add(freight.CellRouteOriginCellId64);
            for (var i = 0; i < freight.CellRouteSegments.Count; i++)
            {
                var segment = freight.CellRouteSegments[i];
                result.CellIds.Add(segment.ToCellId64);
                result.TotalWeightedCentimetres = checked(
                    result.TotalWeightedCentimetres +
                    segment.WeightedDistanceCentimetres);
            }
            result.Status = ResolveStatus(freight);
            Validate(result);
        }

        private static PlayableWorldMapRouteStatus ResolveStatus(
            CivilianFreightState freight)
        {
            if (freight.Status == CivilianFreightStatus.Completed)
                return PlayableWorldMapRouteStatus.Completed;
            if (freight.CellRouteWaiting)
                return PlayableWorldMapRouteStatus.Waiting;
            if (freight.CurrentCellRouteSegmentIndex >=
                freight.CellRouteSegments.Count)
                return PlayableWorldMapRouteStatus.Arrived;
            return PlayableWorldMapRouteStatus.InTransit;
        }

        private static CivilianFreightState FindLatestMerchantFreight(
            WorldState world, string personId)
        {
            CivilianFreightState result = null;
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                var candidate = world.CivilianFreights[i];
                if (candidate.CarrierPersonId != personId ||
                    candidate.PurposeId !=
                        CivilianFreightPurposeIds.MerchantOwnerCarriage)
                    continue;
                if (result == null || candidate.CreatedDay > result.CreatedDay ||
                    candidate.CreatedDay == result.CreatedDay &&
                    string.CompareOrdinal(candidate.Id, result.Id) > 0)
                    result = candidate;
            }
            return result;
        }

        private static string LocationName(WorldState world, string locationId)
        {
            var location = world.Locations.Find(item => item.Id == locationId);
            return location == null ? locationId : location.DisplayName;
        }

        private static void Validate(PlayableWorldMapProjection result)
        {
            if (!result.HasRoute || result.OriginCellId64 == 0 ||
                result.TargetCellId64 == 0 || result.CellIds[0] !=
                result.OriginCellId64 || result.CellIds[
                    result.CellIds.Count - 1] != result.TargetCellId64 ||
                result.CurrentCellSequence < 0 ||
                result.CurrentCellSequence >= result.CellIds.Count ||
                result.CellIds[result.CurrentCellSequence] !=
                    result.CurrentCellId64 ||
                result.RemainingWeightedCentimetres < 0 ||
                result.RemainingWeightedCentimetres >
                    result.TotalWeightedCentimetres)
                throw new InvalidOperationException(
                    "The playable formal map route projection is invalid.");
            for (var i = 0; i < result.CellIds.Count; i++)
                if (result.CellIds[i] == 0)
                    throw new InvalidOperationException(
                        "The playable formal map route contains an empty Cell.");
        }
    }
}
