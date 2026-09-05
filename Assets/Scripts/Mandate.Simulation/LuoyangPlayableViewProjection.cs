using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum LuoyangPlayableViewMode
    {
        Person = 0,
        County = 1,
        World = 2
    }

    public enum CountySubViewMode
    {
        Overview = 0,
        UrbanArea = 1,
        Planning = 2
    }

    public enum LuoyangPlayableViewCommand
    {
        ShowPerson,
        ShowCounty,
        ShowWorld
    }

    /// <summary>
    /// Non-persistent view state. It intentionally owns no WorldState and no
    /// simulation service, so changing a camera view cannot advance or mutate
    /// the authoritative world.
    /// </summary>
    public sealed class LuoyangPlayableViewState
    {
        public LuoyangPlayableViewMode Mode { get; private set; } =
            LuoyangPlayableViewMode.Person;
        public string FocusFacilityId { get; private set; }
        public string ObservedCountyId { get; private set; }
        public CountySubViewMode CountySubView { get; private set; } =
            CountySubViewMode.Overview;
        public bool FollowsPlayer { get; private set; } = true;
        public int Revision { get; private set; }

        public void ShowWorld()
        {
            Mode = LuoyangPlayableViewMode.World;
            FocusFacilityId = null;
            FollowsPlayer = false;
            Revision++;
        }

        public void ShowCounty(string countyId,
            CountySubViewMode subView = CountySubViewMode.Overview,
            string focusFacilityId = null)
        {
            Mode = LuoyangPlayableViewMode.County;
            ObservedCountyId = new StableId(countyId).Value;
            CountySubView = subView;
            FocusFacilityId = NormalizeOptionalId(focusFacilityId);
            FollowsPlayer = false;
            Revision++;
        }

        public void SetCountySubView(CountySubViewMode subView)
        {
            if (Mode != LuoyangPlayableViewMode.County ||
                string.IsNullOrWhiteSpace(ObservedCountyId))
                throw new InvalidOperationException(
                    "A county subview requires an observed CountyId.");
            CountySubView = subView;
            Revision++;
        }

        [Obsolete("City is a County UrbanArea subview. Use ShowCounty.")]
        public void ShowCity(string focusFacilityId = null)
        {
            ShowCounty(Luoyang50mCountySpatialPrototypeIds.CountyId,
                CountySubViewMode.UrbanArea, focusFacilityId);
        }

        public void ShowPlayer()
        {
            Mode = LuoyangPlayableViewMode.Person;
            ObservedCountyId = null;
            CountySubView = CountySubViewMode.Overview;
            FocusFacilityId = null;
            FollowsPlayer = true;
            Revision++;
        }

        public void ObserveFacility(string facilityId)
        {
            Mode = LuoyangPlayableViewMode.Person;
            ObservedCountyId = null;
            CountySubView = CountySubViewMode.Overview;
            FocusFacilityId = new StableId(facilityId).Value;
            FollowsPlayer = false;
            Revision++;
        }

        public void SetFollowPlayer(bool followsPlayer)
        {
            if (Mode != LuoyangPlayableViewMode.Person)
                followsPlayer = false;
            FollowsPlayer = followsPlayer;
            if (followsPlayer) FocusFacilityId = null;
            Revision++;
        }

        private static string NormalizeOptionalId(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : new StableId(value).Value;
    }

    public static class LuoyangPlayableViewCommandBindings
    {
        public static bool TryResolve(char key,
            out LuoyangPlayableViewCommand command)
        {
            switch (char.ToUpperInvariant(key))
            {
                case 'M':
                    command = LuoyangPlayableViewCommand.ShowWorld;
                    return true;
                case 'C':
                    command = LuoyangPlayableViewCommand.ShowCounty;
                    return true;
                case 'F':
                    command = LuoyangPlayableViewCommand.ShowPerson;
                    return true;
                default:
                    command = default;
                    return false;
            }
        }
    }

    public sealed class LuoyangCityFacilityProjection
    {
        public LuoyangCityFacilityProjection(
            LuoyangBuildingPerformanceFacility facility,
            LuoyangWholeCityCompositionAnchor anchor,
            LuoyangFacilitySpatialCapability capability)
        {
            FacilityId = new StableId(facility?.FacilityId).Value;
            FacilityDefinitionId = new StableId(
                facility.FacilityDefinitionId).Value;
            DisplayName = facility.DisplayName ?? FacilityId;
            ModelId = RequirePresentationId(facility.ModelId,
                nameof(facility.ModelId));
            AssetVariantId = RequirePresentationId(anchor?.AssetVariantId,
                nameof(anchor.AssetVariantId));
            DistrictId = RequirePresentationId(anchor.DistrictId,
                nameof(anchor.DistrictId));
            CapabilityId = new StableId(capability?.CapabilityId).Value;
            CellId64 = facility.CellId64;
            GridRow = facility.GridRow;
            GridColumn = facility.GridColumn;
            RotationDegrees = anchor.RotationDegrees;
            Scale = anchor.Scale;
        }

        private static string RequirePresentationId(string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Presentation ID cannot be empty.", parameterName);
            return value.Trim();
        }

        public string FacilityId { get; }
        public string FacilityDefinitionId { get; }
        public string DisplayName { get; }
        public string ModelId { get; }
        public string AssetVariantId { get; }
        public string DistrictId { get; }
        public string CapabilityId { get; }
        public ulong CellId64 { get; }
        public int GridRow { get; }
        public int GridColumn { get; }
        public float RotationDegrees { get; }
        public float Scale { get; }
    }

    /// <summary>
    /// Read-only projection of all formal Luoyang Facilities for the city
    /// camera. It never creates replacement Facilities or stores view facts in
    /// WorldState.
    /// </summary>
    public sealed class LuoyangCityViewProjection
    {
        private readonly IReadOnlyDictionary<string,
            LuoyangCityFacilityProjection> _byFacilityId;

        private LuoyangCityViewProjection(
            IReadOnlyList<LuoyangCityFacilityProjection> facilities)
        {
            Facilities = facilities;
            _byFacilityId = facilities.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);
            StableSummary = BuildStableSummary(facilities);
        }

        public IReadOnlyList<LuoyangCityFacilityProjection> Facilities
        {
            get;
        }
        public int FacilityCount => Facilities.Count;
        public int AssetVariantCount => Facilities.Select(item =>
            item.AssetVariantId).Distinct(StringComparer.Ordinal).Count();
        public int DistrictCount => Facilities.Select(item => item.DistrictId)
            .Distinct(StringComparer.Ordinal).Count();
        public int CityGateCount => Facilities.Count(item => string.Equals(
            item.FacilityDefinitionId, "facility.fortification.city_gate",
            StringComparison.Ordinal));
        public bool HasWallNetwork => Facilities.Any(item => string.Equals(
            item.CapabilityId, FacilitySpatialCapabilityIds.Wall,
            StringComparison.Ordinal));
        public bool HasNorthPalace => Facilities.Any(item => string.Equals(
            item.DisplayName, "北宫", StringComparison.Ordinal));
        public bool HasSouthPalace => Facilities.Any(item => string.Equals(
            item.DisplayName, "南宫", StringComparison.Ordinal));
        public bool HasMarket => Facilities.Any(item => string.Equals(
            item.FacilityDefinitionId, "facility.commercial.market",
            StringComparison.Ordinal));
        public bool HasGovernment => Facilities.Any(item => string.Equals(
            item.FacilityDefinitionId, "facility.government.court_hall",
            StringComparison.Ordinal) || string.Equals(item.FacilityDefinitionId,
            "facility.historical.central_office", StringComparison.Ordinal));
        public bool HasStateStorage => Facilities.Any(item => string.Equals(
            item.DisplayName, "太仓", StringComparison.Ordinal) ||
            string.Equals(item.DisplayName, "武库", StringComparison.Ordinal));
        public bool HasSouthernRitualArea => Facilities.Any(item =>
            string.Equals(item.FacilityDefinitionId,
                "facility.education.academy",
                StringComparison.Ordinal) || string.Equals(
                item.FacilityDefinitionId,
                "facility.public.ritual_hall",
                StringComparison.Ordinal) || string.Equals(
                item.FacilityDefinitionId,
                "facility.public.observatory",
                StringComparison.Ordinal));
        public ulong StableSummary { get; }

        public bool TryGet(string facilityId,
            out LuoyangCityFacilityProjection facility)
        {
            facility = null;
            return !string.IsNullOrWhiteSpace(facilityId) &&
                   _byFacilityId.TryGetValue(facilityId, out facility);
        }

        public static LuoyangCityViewProjection Create(
            LuoyangBuildingPerformancePlan performance,
            LuoyangWholeCityCompositionPlan composition,
            LuoyangHumanScaleLocalMapPlan localMap)
        {
            if (performance == null)
                throw new ArgumentNullException(nameof(performance));
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));
            if (localMap == null)
                throw new ArgumentNullException(nameof(localMap));
            var items = performance.Facilities.OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .Select(item => new LuoyangCityFacilityProjection(item,
                    composition.AnchorsByFacilityId[item.FacilityId],
                    localMap.FacilityCapabilitiesByFacilityId[
                        item.FacilityId])).ToArray();
            var projection = new LuoyangCityViewProjection(items);
            if (projection.FacilityCount !=
                    LuoyangWholeCityCompositionIds.FacilityAnchorCount ||
                projection.AssetVariantCount !=
                    LuoyangWholeCityCompositionIds.AssetVariantCount ||
                projection.DistrictCount !=
                    LuoyangWholeCityCompositionIds.DistrictCount)
                throw new InvalidOperationException(
                    "The Luoyang city projection is incomplete.");
            return projection;
        }

        private static ulong BuildStableSummary(
            IEnumerable<LuoyangCityFacilityProjection> facilities)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var value = offset;
            foreach (var facility in facilities)
            {
                value = Hash(value, facility.FacilityId, prime);
                value = Hash(value, facility.AssetVariantId, prime);
                value = Hash(value, facility.DistrictId, prime);
                value ^= facility.CellId64;
                value *= prime;
            }
            return value;
        }

        private static ulong Hash(ulong value, string text, ulong prime)
        {
            foreach (var character in text ?? string.Empty)
            {
                value ^= character;
                value *= prime;
            }
            return value;
        }
    }

    public sealed class LuoyangNearfieldVisualProfile
    {
        public string FacilityId { get; set; }
        public string CapabilityId { get; set; }
        public string ProfileId { get; set; }
        public string ClusterHookId { get; set; }
        public int StableVariantIndex { get; set; }
        public int HeightCentimetres { get; set; }
        public bool HasStructuralPlaceholder { get; set; }
    }

    public static class LuoyangNearfieldVisualProfileResolver
    {
        public static LuoyangNearfieldVisualProfile Resolve(
            LuoyangHumanScaleLocalMapPlan plan, string facilityId)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            facilityId = new StableId(facilityId).Value;
            if (!plan.FacilityCapabilitiesByFacilityId.TryGetValue(facilityId,
                    out var capability))
                throw new KeyNotFoundException(
                    "Unknown Luoyang Facility: " + facilityId);
            var variant = StableVariant(facilityId, 4);
            var structural = string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Building,
                    StringComparison.Ordinal) || string.Equals(
                    capability.CapabilityId, FacilitySpatialCapabilityIds.Wall,
                    StringComparison.Ordinal) || string.Equals(
                    capability.CapabilityId, FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal) || string.Equals(
                    capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal);
            var baseHeight = string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Wall,
                    StringComparison.Ordinal) ? 450 : string.Equals(
                    capability.CapabilityId, FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal) ? 1_050 : string.Equals(
                    capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal) ? 280 : 620;
            return new LuoyangNearfieldVisualProfile
            {
                FacilityId = facilityId,
                CapabilityId = capability.CapabilityId,
                ProfileId = "visual-profile.luoyang.nearfield." +
                            capability.CapabilityId + ".v1",
                ClusterHookId = "nearfield-cluster-hook." + facilityId,
                StableVariantIndex = variant,
                HeightCentimetres = baseHeight + variant * 70,
                HasStructuralPlaceholder = structural
            };
        }

        private static int StableVariant(string value, int count)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in value)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return (int)(hash % (uint)count);
            }
        }
    }

    public sealed class LuoyangNearfieldContextFacilityProjection
    {
        public LuoyangNearfieldContextFacilityProjection(string facilityId,
            ulong sourceCellId64, bool isFocusFacility,
            double visualEastUnityUnits, double visualNorthUnityUnits,
            int rotationMilliDegrees)
        {
            FacilityId = new StableId(facilityId).Value;
            SourceCellId64 = sourceCellId64;
            IsFocusFacility = isFocusFacility;
            VisualEastUnityUnits = visualEastUnityUnits;
            VisualNorthUnityUnits = visualNorthUnityUnits;
            RotationMilliDegrees = rotationMilliDegrees;
        }

        public string FacilityId { get; }
        public ulong SourceCellId64 { get; }
        public bool IsFocusFacility { get; }
        public double VisualEastUnityUnits { get; }
        public double VisualNorthUnityUnits { get; }
        public int RotationMilliDegrees { get; }
    }

    /// <summary>
    /// Read-only P0-1 visual projection for a compact human-scale street
    /// context. Source Facility IDs and cells remain authoritative; the
    /// offsets are presentation-only and must never be used for movement,
    /// access, ownership, inventory, or simulation decisions.
    /// </summary>
    public sealed class LuoyangNearfieldUrbanContextProjection
    {
        private static readonly (double East, double North)[] VisualSlots =
        {
            (0d, 0d),
            (-8.5d, 0d),
            (8.5d, 0d),
            (0d, 7.5d),
            (0d, -7.5d),
            (-8.5d, 7.5d),
            (8.5d, 7.5d),
            (-8.5d, -7.5d),
            (8.5d, -7.5d)
        };

        private LuoyangNearfieldUrbanContextProjection(string focusFacilityId,
            IReadOnlyList<LuoyangNearfieldContextFacilityProjection>
                facilities)
        {
            FocusFacilityId = focusFacilityId;
            Facilities = facilities;
            StableSummary = BuildStableSummary(facilities);
        }

        public string FocusFacilityId { get; }
        public IReadOnlyList<LuoyangNearfieldContextFacilityProjection>
            Facilities { get; }
        public ulong StableSummary { get; }

        public static LuoyangNearfieldUrbanContextProjection Create(
            LuoyangHumanScaleLocalMapPlan plan, string focusFacilityId,
            int maximumFacilityCount = 9)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            focusFacilityId = new StableId(focusFacilityId).Value;
            if (!plan.FootprintsByFacilityId.TryGetValue(focusFacilityId,
                    out var focus))
                throw new KeyNotFoundException(
                    "Unknown nearfield focus Facility: " + focusFacilityId);
            if (maximumFacilityCount < 3 ||
                maximumFacilityCount > VisualSlots.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(maximumFacilityCount));

            var focusEast = AbsoluteEast(plan, focus);
            var focusNorth = AbsoluteNorth(plan, focus);
            var neighbors = plan.Footprints.Where(item =>
                    !string.Equals(item.FacilityId, focusFacilityId,
                        StringComparison.Ordinal) &&
                    LuoyangNearfieldVisualProfileResolver.Resolve(plan,
                        item.FacilityId).HasStructuralPlaceholder)
                .OrderBy(item => SquaredDistance(
                    AbsoluteEast(plan, item) - focusEast,
                    AbsoluteNorth(plan, item) - focusNorth))
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .Take(maximumFacilityCount - 1)
                .ToArray();
            var source = new[] { focus }.Concat(neighbors).ToArray();
            var projected = new List<
                LuoyangNearfieldContextFacilityProjection>(source.Length);
            for (var index = 0; index < source.Length; index++)
            {
                var item = source[index];
                projected.Add(new LuoyangNearfieldContextFacilityProjection(
                    item.FacilityId, item.CellId64, index == 0,
                    VisualSlots[index].East, VisualSlots[index].North,
                    item.RotationMilliDegrees));
            }
            return new LuoyangNearfieldUrbanContextProjection(
                focusFacilityId, projected);
        }

        private static double AbsoluteEast(LuoyangHumanScaleLocalMapPlan plan,
            LuoyangFacilityLocalFootprint footprint) =>
            plan.LocalSpacesByCellId[footprint.CellId64].OriginEastingMetres +
            footprint.CenterEastMetres;

        private static double AbsoluteNorth(
            LuoyangHumanScaleLocalMapPlan plan,
            LuoyangFacilityLocalFootprint footprint) =>
            plan.LocalSpacesByCellId[footprint.CellId64]
                .OriginNorthingMetres + footprint.CenterNorthMetres;

        private static double SquaredDistance(double east, double north) =>
            east * east + north * north;

        private static ulong BuildStableSummary(IEnumerable<
            LuoyangNearfieldContextFacilityProjection> facilities)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var value = offset;
            foreach (var facility in facilities)
            {
                foreach (var character in facility.FacilityId)
                {
                    value ^= character;
                    value *= prime;
                }
                value ^= facility.SourceCellId64;
                value *= prime;
                value ^= unchecked((ulong)BitConverter.DoubleToInt64Bits(
                    facility.VisualEastUnityUnits));
                value *= prime;
                value ^= unchecked((ulong)BitConverter.DoubleToInt64Bits(
                    facility.VisualNorthUnityUnits));
                value *= prime;
            }
            return value;
        }
    }
}
