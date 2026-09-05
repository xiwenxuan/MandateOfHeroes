using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mandate.Domain
{
    public static class LuoyangCountyPlanningIds
    {
        public const string ContractId =
            "mandate.luoyang.county-planning-tools.v1";
        public const string ProfileSchemaId =
            "mandate.facility-placement-profile-catalog.v1";
        public const string ProfilePackageId =
            "mandate.luoyang.facility-placement-profiles.v1";
        public const string ProfileDirectoryName =
            "LuoyangCountyPlanningToolsV1";
        public const string ProfileFileName =
            "facility_placement_profiles_v1.json";
        public const string DraftProvenanceId =
            "planning-session.draft-only.non-persistent.v1";
        public const string RoadClassPath = "road.han.path";
        public const string RoadClassGeneral = "road.han.general";
    }

    public enum FacilityRoadAccessRequirement : byte
    {
        None = 0,
        Optional = 1,
        Required = 2
    }

    public enum PlacementValidationState : byte
    {
        Valid = 0,
        Conditional = 1,
        Invalid = 2
    }

    public enum FacilityRoadAccessStatus : byte
    {
        NotRequired = 0,
        Connected = 1,
        TooFar = 2,
        Blocked = 3,
        WrongSide = 4,
        NoRoad = 5
    }

    public static class PlacementReasonIds
    {
        public const string OutsideCounty = "placement.outside_county";
        public const string CellNotBuildable = "placement.cell_not_buildable";
        public const string TerrainForbidden = "placement.terrain_forbidden";
        public const string SlopeTooSteep = "placement.slope_too_steep";
        public const string WaterOverlap = "placement.water_overlap";
        public const string RoadOverlap = "placement.road_overlap";
        public const string FortificationOverlap =
            "placement.fortification_overlap";
        public const string PortalCorridorOverlap =
            "placement.portal_corridor_overlap";
        public const string ExistingFacilityCollision =
            "placement.existing_facility_collision";
        public const string DraftCollision = "placement.draft_collision";
        public const string RoadNoRoad = "placement.road.no_road";
        public const string RoadTooFar = "placement.road.too_far";
        public const string RoadBlocked = "placement.road.blocked";
        public const string RoadWrongSide = "placement.road.wrong_side";
        public const string ConstructionPermissionDeferred =
            "placement.permission.construction_deferred";
        public const string MilitaryAuthorityRequired =
            "placement.permission.military_authority_required";
    }

    public sealed class FacilityEntranceOffsetDefinition
    {
        public FacilityEntranceOffsetDefinition(string entranceId,
            int eastOffsetCentimetres, int northOffsetCentimetres,
            PlanningCellDirection outwardDirection, bool primary)
        {
            EntranceId = new StableId(entranceId).Value;
            EastOffsetCentimetres = eastOffsetCentimetres;
            NorthOffsetCentimetres = northOffsetCentimetres;
            OutwardDirection = outwardDirection;
            Primary = primary;
        }

        public string EntranceId { get; }
        public int EastOffsetCentimetres { get; }
        public int NorthOffsetCentimetres { get; }
        public PlanningCellDirection OutwardDirection { get; }
        public bool Primary { get; }
    }

    public sealed class FacilityPlacementProfile
    {
        public FacilityPlacementProfile(string profileId,
            string facilityDefinitionId, string blueprintId, string modelId,
            string displayName, int footprintWidthCentimetres,
            int footprintLengthCentimetres, int heightCentimetres,
            IReadOnlyList<int> allowedRotationQuarterTurns,
            IReadOnlyList<FacilityEntranceOffsetDefinition> entranceOffsets,
            IReadOnlyList<PlanningTerrainClass> allowedTerrain,
            IReadOnlyList<PlanningTerrainClass> forbiddenTerrain,
            byte maximumSlopeBasis,
            FacilityRoadAccessRequirement roadAccessRequirement,
            string minimumRoadClassId,
            int maximumEntranceToRoadDistanceCentimetres,
            bool allowWaterOverlap, bool allowFortificationOverlap,
            bool allowExistingFacilityOverlap,
            int requiredClearanceCentimetres, string placementCategoryId,
            IReadOnlyList<string> availabilityIds, string provenanceId)
        {
            ProfileId = new StableId(profileId).Value;
            FacilityDefinitionId = new StableId(facilityDefinitionId).Value;
            BlueprintId = new StableId(blueprintId).Value;
            ModelId = new StableId(modelId).Value;
            DisplayName = displayName ?? string.Empty;
            FootprintWidthCentimetres = footprintWidthCentimetres;
            FootprintLengthCentimetres = footprintLengthCentimetres;
            HeightCentimetres = heightCentimetres;
            AllowedRotationQuarterTurns = (allowedRotationQuarterTurns ??
                Array.Empty<int>()).OrderBy(value => value).ToArray();
            EntranceOffsets = (entranceOffsets ??
                Array.Empty<FacilityEntranceOffsetDefinition>()).ToArray();
            AllowedTerrain = (allowedTerrain ??
                Array.Empty<PlanningTerrainClass>()).Distinct().OrderBy(
                value => value).ToArray();
            ForbiddenTerrain = (forbiddenTerrain ??
                Array.Empty<PlanningTerrainClass>()).Distinct().OrderBy(
                value => value).ToArray();
            MaximumSlopeBasis = maximumSlopeBasis;
            RoadAccessRequirement = roadAccessRequirement;
            MinimumRoadClassId = string.IsNullOrWhiteSpace(minimumRoadClassId)
                ? LuoyangCountyPlanningIds.RoadClassPath
                : new StableId(minimumRoadClassId).Value;
            MaximumEntranceToRoadDistanceCentimetres =
                maximumEntranceToRoadDistanceCentimetres;
            AllowWaterOverlap = allowWaterOverlap;
            AllowFortificationOverlap = allowFortificationOverlap;
            AllowExistingFacilityOverlap = allowExistingFacilityOverlap;
            RequiredClearanceCentimetres = requiredClearanceCentimetres;
            PlacementCategoryId = new StableId(placementCategoryId).Value;
            AvailabilityIds = (availabilityIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            ProvenanceId = new StableId(provenanceId).Value;
            Validate();
        }

        public string ProfileId { get; }
        public string FacilityDefinitionId { get; }
        public string BlueprintId { get; }
        public string ModelId { get; }
        public string DisplayName { get; }
        public int FootprintWidthCentimetres { get; }
        public int FootprintLengthCentimetres { get; }
        public int HeightCentimetres { get; }
        public IReadOnlyList<int> AllowedRotationQuarterTurns { get; }
        public IReadOnlyList<FacilityEntranceOffsetDefinition>
            EntranceOffsets { get; }
        public IReadOnlyList<PlanningTerrainClass> AllowedTerrain { get; }
        public IReadOnlyList<PlanningTerrainClass> ForbiddenTerrain { get; }
        public byte MaximumSlopeBasis { get; }
        public FacilityRoadAccessRequirement RoadAccessRequirement { get; }
        public string MinimumRoadClassId { get; }
        public int MaximumEntranceToRoadDistanceCentimetres { get; }
        public bool AllowWaterOverlap { get; }
        public bool AllowFortificationOverlap { get; }
        public bool AllowExistingFacilityOverlap { get; }
        public int RequiredClearanceCentimetres { get; }
        public string PlacementCategoryId { get; }
        public IReadOnlyList<string> AvailabilityIds { get; }
        public string ProvenanceId { get; }
        public bool PlayerBuildable => AvailabilityIds.Contains("Player");

        public bool AllowsRotation(int quarterTurns) =>
            AllowedRotationQuarterTurns.Contains(NormalizeRotation(
                quarterTurns));

        public static int NormalizeRotation(int quarterTurns)
        {
            var normalized = quarterTurns % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(DisplayName) ||
                FootprintWidthCentimetres <= 0 ||
                FootprintLengthCentimetres <= 0 || HeightCentimetres <= 0 ||
                AllowedRotationQuarterTurns.Count == 0 ||
                AllowedRotationQuarterTurns.Any(value => value < 0 ||
                    value > 3) ||
                AllowedRotationQuarterTurns.Distinct().Count() !=
                    AllowedRotationQuarterTurns.Count ||
                EntranceOffsets.Count == 0 ||
                EntranceOffsets.Any(value => value == null) ||
                EntranceOffsets.Select(value => value.EntranceId).Distinct(
                    StringComparer.Ordinal).Count() != EntranceOffsets.Count ||
                EntranceOffsets.Count(value => value.Primary) != 1 ||
                AllowedTerrain.Count == 0 || MaximumSlopeBasis > 100 ||
                MaximumEntranceToRoadDistanceCentimetres < 0 ||
                RequiredClearanceCentimetres < 0 ||
                AvailabilityIds.Count == 0)
                throw new InvalidOperationException(
                    "Invalid Facility placement profile: " + ProfileId);
        }
    }

    public sealed class FacilityPlacementProfileCatalog
    {
        public FacilityPlacementProfileCatalog(string schemaId,
            string packageId, string sourceLayoutPackageId, string statusId,
            IReadOnlyList<FacilityPlacementProfile> profiles)
        {
            SchemaId = schemaId ?? string.Empty;
            PackageId = packageId ?? string.Empty;
            SourceLayoutPackageId = sourceLayoutPackageId ?? string.Empty;
            StatusId = statusId ?? string.Empty;
            Profiles = profiles ?? Array.Empty<FacilityPlacementProfile>();
            ProfilesById = Profiles.ToDictionary(value => value.ProfileId,
                StringComparer.Ordinal);
            ProfilesByDefinitionId = Profiles.ToDictionary(
                value => value.FacilityDefinitionId, StringComparer.Ordinal);
            Validate();
        }

        public string SchemaId { get; }
        public string PackageId { get; }
        public string SourceLayoutPackageId { get; }
        public string StatusId { get; }
        public IReadOnlyList<FacilityPlacementProfile> Profiles { get; }
        public IReadOnlyDictionary<string, FacilityPlacementProfile>
            ProfilesById { get; }
        public IReadOnlyDictionary<string, FacilityPlacementProfile>
            ProfilesByDefinitionId { get; }

        private void Validate()
        {
            if (!string.Equals(SchemaId,
                    LuoyangCountyPlanningIds.ProfileSchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(PackageId,
                    LuoyangCountyPlanningIds.ProfilePackageId,
                    StringComparison.Ordinal) ||
                !string.Equals(SourceLayoutPackageId,
                    Luoyang50mCountyLayoutIds.PackageId,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(StatusId) || Profiles.Count < 5 ||
                ProfilesById.Count != Profiles.Count ||
                ProfilesByDefinitionId.Count != Profiles.Count)
                throw new InvalidOperationException(
                    "Invalid Facility placement profile catalog.");
        }
    }

    public readonly struct PlanningMetricBounds
    {
        public PlanningMetricBounds(double minimumEasting,
            double maximumEasting, double minimumNorthing,
            double maximumNorthing)
        {
            if (maximumEasting < minimumEasting ||
                maximumNorthing < minimumNorthing)
                throw new ArgumentException("Invalid planning bounds.");
            MinimumEasting = minimumEasting;
            MaximumEasting = maximumEasting;
            MinimumNorthing = minimumNorthing;
            MaximumNorthing = maximumNorthing;
        }

        public double MinimumEasting { get; }
        public double MaximumEasting { get; }
        public double MinimumNorthing { get; }
        public double MaximumNorthing { get; }

        public PlanningMetricBounds Expand(double metres) =>
            new PlanningMetricBounds(MinimumEasting - metres,
                MaximumEasting + metres, MinimumNorthing - metres,
                MaximumNorthing + metres);

        public bool Intersects(PlanningMetricBounds other) =>
            MinimumEasting < other.MaximumEasting &&
            MaximumEasting > other.MinimumEasting &&
            MinimumNorthing < other.MaximumNorthing &&
            MaximumNorthing > other.MinimumNorthing;

        public bool Contains(GlobalProjectedCoordinate point) =>
            point.EastingMetres >= MinimumEasting &&
            point.EastingMetres <= MaximumEasting &&
            point.NorthingMetres >= MinimumNorthing &&
            point.NorthingMetres <= MaximumNorthing;
    }

    public sealed class PlanningFacilityEntrance
    {
        public PlanningFacilityEntrance(string entranceId,
            GlobalProjectedCoordinate position,
            PlanningCellDirection outwardDirection, bool primary)
        {
            EntranceId = new StableId(entranceId).Value;
            Position = position;
            OutwardDirection = outwardDirection;
            Primary = primary;
        }

        public string EntranceId { get; }
        public GlobalProjectedCoordinate Position { get; }
        public PlanningCellDirection OutwardDirection { get; }
        public bool Primary { get; }
    }

    public sealed class PlanningFacilityFootprint
    {
        public PlanningFacilityFootprint(FacilityPlacementProfile profile,
            GlobalProjectedCoordinate center, int rotationQuarterTurns)
        {
            Profile = profile ?? throw new ArgumentNullException(
                nameof(profile));
            RotationQuarterTurns = FacilityPlacementProfile
                .NormalizeRotation(rotationQuarterTurns);
            if (!profile.AllowsRotation(RotationQuarterTurns))
                throw new InvalidOperationException(
                    "Placement profile does not allow this rotation.");
            Center = center;
            var width = profile.FootprintWidthCentimetres / 100d;
            var length = profile.FootprintLengthCentimetres / 100d;
            if ((RotationQuarterTurns & 1) != 0)
            {
                var swap = width;
                width = length;
                length = swap;
            }
            WidthMetres = width;
            LengthMetres = length;
            Bounds = new PlanningMetricBounds(center.EastingMetres -
                width * 0.5d, center.EastingMetres + width * 0.5d,
                center.NorthingMetres - length * 0.5d,
                center.NorthingMetres + length * 0.5d);
            Entrances = profile.EntranceOffsets.Select(value =>
                RotateEntrance(value)).ToArray();
        }

        public FacilityPlacementProfile Profile { get; }
        public GlobalProjectedCoordinate Center { get; }
        public int RotationQuarterTurns { get; }
        public double WidthMetres { get; }
        public double LengthMetres { get; }
        public PlanningMetricBounds Bounds { get; }
        public IReadOnlyList<PlanningFacilityEntrance> Entrances { get; }

        private PlanningFacilityEntrance RotateEntrance(
            FacilityEntranceOffsetDefinition definition)
        {
            var east = definition.EastOffsetCentimetres / 100d;
            var north = definition.NorthOffsetCentimetres / 100d;
            for (var turn = 0; turn < RotationQuarterTurns; turn++)
            {
                var previousEast = east;
                east = north;
                north = -previousEast;
            }
            var direction = (PlanningCellDirection)(
                ((int)definition.OutwardDirection +
                 RotationQuarterTurns) % 4);
            return new PlanningFacilityEntrance(definition.EntranceId,
                new GlobalProjectedCoordinate(Center.EastingMetres + east,
                    Center.NorthingMetres + north), direction,
                definition.Primary);
        }
    }

    public sealed class PlacementIssue
    {
        public PlacementIssue(string code, string message, int priority,
            IEnumerable<string> relatedIds = null)
        {
            Code = new StableId(code).Value;
            Message = message ?? string.Empty;
            Priority = priority;
            RelatedIds = (relatedIds ?? Array.Empty<string>()).Where(value =>
                    !string.IsNullOrWhiteSpace(value)).Distinct(
                    StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
        }

        public string Code { get; }
        public string Message { get; }
        public int Priority { get; }
        public IReadOnlyList<string> RelatedIds { get; }
    }

    public sealed class FacilityRoadAccessResult
    {
        public FacilityRoadAccessResult(FacilityRoadAccessStatus status,
            string roadEdgeId, string roadClassId, int distanceCentimetres,
            GlobalProjectedCoordinate entrancePosition,
            GlobalProjectedCoordinate connectionPosition,
            IReadOnlyList<PlanningCellCoord> accessCells)
        {
            Status = status;
            RoadEdgeId = roadEdgeId ?? string.Empty;
            RoadClassId = roadClassId ?? string.Empty;
            DistanceCentimetres = distanceCentimetres;
            EntrancePosition = entrancePosition;
            ConnectionPosition = connectionPosition;
            AccessCells = accessCells ?? Array.Empty<PlanningCellCoord>();
        }

        public FacilityRoadAccessStatus Status { get; }
        public string RoadEdgeId { get; }
        public string RoadClassId { get; }
        public int DistanceCentimetres { get; }
        public GlobalProjectedCoordinate EntrancePosition { get; }
        public GlobalProjectedCoordinate ConnectionPosition { get; }
        public IReadOnlyList<PlanningCellCoord> AccessCells { get; }
    }

    public sealed class PlacementValidationResult
    {
        public PlacementValidationResult(PlacementValidationState state,
            IReadOnlyList<PlacementIssue> blockingReasons,
            IReadOnlyList<PlacementIssue> warnings,
            IReadOnlyList<PlanningCellCoord> coveredCells,
            FacilityRoadAccessResult roadAccessResult,
            IReadOnlyList<string> collisionIds,
            ushort minimumElevationDecimetres,
            ushort maximumElevationDecimetres, byte maximumSlopeBasis)
        {
            State = state;
            BlockingReasons = Sort(blockingReasons);
            Warnings = Sort(warnings);
            CoveredCells = (coveredCells ?? Array.Empty<PlanningCellCoord>())
                .Distinct().OrderBy(value => value).ToArray();
            RoadAccessResult = roadAccessResult ?? throw new ArgumentNullException(
                nameof(roadAccessResult));
            CollisionIds = (collisionIds ?? Array.Empty<string>()).Distinct(
                StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            MinimumElevationDecimetres = minimumElevationDecimetres;
            MaximumElevationDecimetres = maximumElevationDecimetres;
            MaximumSlopeBasis = maximumSlopeBasis;
        }

        public PlacementValidationState State { get; }
        public bool IsValid => State != PlacementValidationState.Invalid;
        public IReadOnlyList<PlacementIssue> BlockingReasons { get; }
        public IReadOnlyList<PlacementIssue> Warnings { get; }
        public IReadOnlyList<PlanningCellCoord> CoveredCells { get; }
        public FacilityRoadAccessResult RoadAccessResult { get; }
        public IReadOnlyList<string> CollisionIds { get; }
        public ushort MinimumElevationDecimetres { get; }
        public ushort MaximumElevationDecimetres { get; }
        public byte MaximumSlopeBasis { get; }
        public string PrimaryReason => BlockingReasons.Count == 0
            ? Warnings.Count == 0 ? string.Empty : Warnings[0].Message
            : BlockingReasons[0].Message;

        private static IReadOnlyList<PlacementIssue> Sort(
            IReadOnlyList<PlacementIssue> issues) =>
            (issues ?? Array.Empty<PlacementIssue>()).OrderBy(value =>
                    value.Priority).ThenBy(value => value.Code,
                    StringComparer.Ordinal).ThenBy(value => value.Message,
                    StringComparer.Ordinal).ToArray();
    }

    public sealed class DraftBuildingBlueprint : ICountyPlanningDraft
    {
        public DraftBuildingBlueprint(string draftId, string countyId,
            FacilityPlacementProfile profile,
            PlanningFacilityFootprint footprint,
            PlacementValidationResult validationSnapshot, int createdOrder)
        {
            DraftId = new StableId(draftId).Value;
            CountyId = new StableId(countyId).Value;
            ProfileId = profile.ProfileId;
            FacilityDefinitionId = profile.FacilityDefinitionId;
            BlueprintId = profile.BlueprintId;
            ModelId = profile.ModelId;
            Position = footprint.Center;
            RotationQuarterTurns = footprint.RotationQuarterTurns;
            WidthCentimetres = profile.FootprintWidthCentimetres;
            LengthCentimetres = profile.FootprintLengthCentimetres;
            EntrancePositions = footprint.Entrances.ToArray();
            CoveredPlanningCells = validationSnapshot.CoveredCells.ToArray();
            ValidationSnapshot = validationSnapshot;
            CreatedOrder = createdOrder;
            ProvenanceId = LuoyangCountyPlanningIds.DraftProvenanceId;
        }

        public string DraftId { get; }
        public string CountyId { get; }
        public string ProfileId { get; }
        public string FacilityDefinitionId { get; }
        public string BlueprintId { get; }
        public string ModelId { get; }
        public GlobalProjectedCoordinate Position { get; }
        public int RotationQuarterTurns { get; }
        public int WidthCentimetres { get; }
        public int LengthCentimetres { get; }
        public IReadOnlyList<PlanningFacilityEntrance> EntrancePositions
            { get; }
        public IReadOnlyList<PlanningCellCoord> CoveredPlanningCells { get; }
        public PlacementValidationResult ValidationSnapshot { get; }
        public int CreatedOrder { get; }
        public string ProvenanceId { get; }
        public CountyPlanningDraftKind Kind =>
            CountyPlanningDraftKind.Building;

        public PlanningMetricBounds Bounds => new PlanningMetricBounds(
            Position.EastingMetres - RotatedWidthMetres * 0.5d,
            Position.EastingMetres + RotatedWidthMetres * 0.5d,
            Position.NorthingMetres - RotatedLengthMetres * 0.5d,
            Position.NorthingMetres + RotatedLengthMetres * 0.5d);

        public double RotatedWidthMetres =>
            ((RotationQuarterTurns & 1) == 0 ? WidthCentimetres :
                LengthCentimetres) / 100d;
        public double RotatedLengthMetres =>
            ((RotationQuarterTurns & 1) == 0 ? LengthCentimetres :
                WidthCentimetres) / 100d;
    }

    public sealed class CountyPlanningSession
    {
        private sealed class Snapshot
        {
            public DraftBuildingBlueprint[] Buildings;
            public DraftRoadGeometry[] Roads;
            public DraftFortification[] Fortifications;
            public DraftCanalGeometry[] Canals;
            public DraftPlanningZone[] Zones;
            public int NextOrder;
        }

        private sealed class HistoryEntry
        {
            public Snapshot Before;
            public Snapshot After;
            public ICountyPlanningDraft Affected;
        }

        private readonly List<DraftBuildingBlueprint> _drafts =
            new List<DraftBuildingBlueprint>();
        private readonly List<DraftRoadGeometry> _roadDrafts =
            new List<DraftRoadGeometry>();
        private readonly List<DraftFortification> _fortificationDrafts =
            new List<DraftFortification>();
        private readonly List<DraftCanalGeometry> _canalDrafts =
            new List<DraftCanalGeometry>();
        private readonly List<DraftPlanningZone> _zoneDrafts =
            new List<DraftPlanningZone>();
        private readonly Stack<HistoryEntry> _undo =
            new Stack<HistoryEntry>();
        private readonly Stack<HistoryEntry> _redo =
            new Stack<HistoryEntry>();
        private int _nextOrder = 1;

        public CountyPlanningSession(string countyId)
        {
            CountyId = new StableId(countyId).Value;
        }

        public string CountyId { get; }
        public IReadOnlyList<DraftBuildingBlueprint> Drafts => _drafts;
        public IReadOnlyList<DraftRoadGeometry> RoadDrafts => _roadDrafts;
        public IReadOnlyList<DraftFortification> FortificationDrafts =>
            _fortificationDrafts;
        public IReadOnlyList<DraftCanalGeometry> CanalDrafts => _canalDrafts;
        public IReadOnlyList<DraftPlanningZone> ZoneDrafts => _zoneDrafts;
        public IReadOnlyList<ICountyPlanningDraft> AllDrafts => _drafts
            .Cast<ICountyPlanningDraft>().Concat(_roadDrafts)
            .Concat(_fortificationDrafts).Concat(_canalDrafts)
            .Concat(_zoneDrafts).OrderBy(value => value.CreatedOrder)
            .ToArray();
        public int UndoCount => _undo.Count;
        public int RedoCount => _redo.Count;
        public int Version { get; private set; }

        public DraftBuildingBlueprint CreateDraft(
            FacilityPlacementProfile profile,
            PlanningFacilityFootprint footprint,
            PlacementValidationResult validation)
        {
            if (profile == null || footprint == null || validation == null)
                throw new ArgumentNullException(nameof(profile));
            if (!validation.IsValid)
                throw new InvalidOperationException(
                    "An invalid placement cannot become a Draft Blueprint.");
            var before = Capture();
            var order = NextOrder();
            var draft = new DraftBuildingBlueprint(
                DraftId(order), CountyId, profile,
                footprint, validation, order);
            _drafts.Add(draft);
            Record(before, draft);
            return draft;
        }

        public DraftRoadGeometry CreateRoadDraft(
            IReadOnlyList<PlanningCellCoord> path,
            PlanningDraftValidation validation)
        {
            RequireValid(validation);
            var before = Capture();
            var order = NextOrder();
            var draft = new DraftRoadGeometry(DraftId(order), path, order,
                validation);
            _roadDrafts.Add(draft);
            Record(before, draft);
            return draft;
        }

        public DraftFortification CreateFortificationDraft(
            IReadOnlyList<DraftFortificationSegment> segments,
            PlanningDraftValidation validation)
        {
            RequireValid(validation);
            var before = Capture();
            var order = NextOrder();
            var draft = new DraftFortification(DraftId(order), segments,
                order, validation);
            _fortificationDrafts.Add(draft);
            Record(before, draft);
            return draft;
        }

        public DraftCanalGeometry CreateCanalDraft(
            IReadOnlyList<PlanningCellCoord> path,
            PlanningDraftValidation validation)
        {
            RequireValid(validation);
            var before = Capture();
            var order = NextOrder();
            var draft = new DraftCanalGeometry(DraftId(order), path, order,
                validation);
            _canalDrafts.Add(draft);
            Record(before, draft);
            return draft;
        }

        public DraftPlanningZone CreateZoneDraft(
            CountyPlanningZoneKind zoneKind,
            IReadOnlyList<PlanningCellCoord> cells)
        {
            var before = Capture();
            var order = NextOrder();
            var draft = new DraftPlanningZone(DraftId(order), zoneKind,
                cells, order);
            _zoneDrafts.Add(draft);
            Record(before, draft);
            return draft;
        }

        public DraftBuildingBlueprint MoveBuildingDraft(string draftId,
            FacilityPlacementProfile profile,
            PlanningFacilityFootprint footprint,
            PlacementValidationResult validation)
        {
            if (!validation.IsValid) return null;
            var index = _drafts.FindIndex(value => string.Equals(
                value.DraftId, draftId, StringComparison.Ordinal));
            if (index < 0) return null;
            var before = Capture();
            var old = _drafts[index];
            var moved = new DraftBuildingBlueprint(old.DraftId, CountyId,
                profile, footprint, validation, old.CreatedOrder);
            _drafts[index] = moved;
            Record(before, moved);
            return moved;
        }

        public DraftBuildingBlueprint CopyBuildingDraft(
            FacilityPlacementProfile profile,
            PlanningFacilityFootprint footprint,
            PlacementValidationResult validation)
        {
            return CreateDraft(profile, footprint, validation);
        }

        public bool RemoveDraft(string draftId)
        {
            var draft = FindDraft(draftId);
            if (draft == null) return false;
            var before = Capture();
            RemoveWithoutHistory(draft);
            Record(before, draft);
            return true;
        }

        public int RemoveDrafts(IEnumerable<string> draftIds)
        {
            var ids = new HashSet<string>(draftIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var matches = AllDrafts.Where(value => ids.Contains(
                value.DraftId)).ToArray();
            if (matches.Length == 0) return 0;
            var before = Capture();
            foreach (var draft in matches) RemoveWithoutHistory(draft);
            Record(before, matches[0]);
            return matches.Length;
        }

        public ICountyPlanningDraft FindDraft(string draftId) => AllDrafts
            .FirstOrDefault(value => string.Equals(value.DraftId, draftId,
                StringComparison.Ordinal));

        public ICountyPlanningDraft Undo()
        {
            if (_undo.Count == 0) return null;
            var entry = _undo.Pop();
            Restore(entry.Before);
            _redo.Push(entry);
            Version++;
            return entry.Affected;
        }

        public ICountyPlanningDraft Redo()
        {
            if (_redo.Count == 0) return null;
            var entry = _redo.Pop();
            Restore(entry.After);
            _undo.Push(entry);
            Version++;
            return entry.Affected;
        }

        public string ComputeDeterministicHash()
        {
            var builder = new StringBuilder(CountyId);
            foreach (var draft in _drafts.OrderBy(value => value.CreatedOrder))
                builder.Append('|').Append(draft.DraftId).Append(':')
                    .Append(draft.FacilityDefinitionId).Append(':')
                    .Append(draft.Position.EastingMetres.ToString("R",
                        CultureInfo.InvariantCulture)).Append(':')
                    .Append(draft.Position.NorthingMetres.ToString("R",
                        CultureInfo.InvariantCulture)).Append(':')
                    .Append(draft.RotationQuarterTurns);
            foreach (var draft in _roadDrafts.OrderBy(value =>
                         value.CreatedOrder))
                AppendCells(builder, draft.DraftId, draft.Path);
            foreach (var draft in _fortificationDrafts.OrderBy(value =>
                         value.CreatedOrder))
            {
                builder.Append('|').Append(draft.DraftId);
                foreach (var segment in draft.Segments)
                    builder.Append(':').Append(segment.Cell.Row).Append(',')
                        .Append(segment.Cell.Column).Append(',')
                        .Append((int)segment.EdgeDirection);
            }
            foreach (var draft in _canalDrafts.OrderBy(value =>
                         value.CreatedOrder))
                AppendCells(builder, draft.DraftId, draft.Path);
            foreach (var draft in _zoneDrafts.OrderBy(value =>
                         value.CreatedOrder))
            {
                builder.Append('|').Append(draft.DraftId).Append(':')
                    .Append((int)draft.ZoneKind);
                foreach (var cell in draft.Cells)
                    builder.Append(':').Append(cell.Row).Append(',')
                        .Append(cell.Column);
            }
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(
                        builder.ToString()))
                    .Select(value => value.ToString("x2")));
        }

        private int NextOrder() => _nextOrder++;

        private static string DraftId(int order) =>
            "draft.luoyang.county-planning." + order.ToString("D6",
                CultureInfo.InvariantCulture);

        private static void RequireValid(PlanningDraftValidation validation)
        {
            if (validation == null || !validation.IsValid)
                throw new InvalidOperationException(
                    "An invalid planning geometry cannot become a Draft.");
        }

        private Snapshot Capture() => new Snapshot
        {
            Buildings = _drafts.ToArray(),
            Roads = _roadDrafts.ToArray(),
            Fortifications = _fortificationDrafts.ToArray(),
            Canals = _canalDrafts.ToArray(),
            Zones = _zoneDrafts.ToArray(),
            NextOrder = _nextOrder
        };

        private void Restore(Snapshot snapshot)
        {
            _drafts.Clear();
            _drafts.AddRange(snapshot.Buildings);
            _roadDrafts.Clear();
            _roadDrafts.AddRange(snapshot.Roads);
            _fortificationDrafts.Clear();
            _fortificationDrafts.AddRange(snapshot.Fortifications);
            _canalDrafts.Clear();
            _canalDrafts.AddRange(snapshot.Canals);
            _zoneDrafts.Clear();
            _zoneDrafts.AddRange(snapshot.Zones);
            _nextOrder = snapshot.NextOrder;
        }

        private void Record(Snapshot before, ICountyPlanningDraft affected)
        {
            _undo.Push(new HistoryEntry
            {
                Before = before,
                After = Capture(),
                Affected = affected
            });
            _redo.Clear();
            Version++;
        }

        private void RemoveWithoutHistory(ICountyPlanningDraft draft)
        {
            switch (draft.Kind)
            {
                case CountyPlanningDraftKind.Building:
                    _drafts.Remove((DraftBuildingBlueprint)draft);
                    break;
                case CountyPlanningDraftKind.Road:
                    _roadDrafts.Remove((DraftRoadGeometry)draft);
                    break;
                case CountyPlanningDraftKind.Fortification:
                    _fortificationDrafts.Remove(
                        (DraftFortification)draft);
                    break;
                case CountyPlanningDraftKind.Canal:
                    _canalDrafts.Remove((DraftCanalGeometry)draft);
                    break;
                case CountyPlanningDraftKind.Zone:
                    _zoneDrafts.Remove((DraftPlanningZone)draft);
                    break;
            }
        }

        private static void AppendCells(StringBuilder builder, string id,
            IEnumerable<PlanningCellCoord> cells)
        {
            builder.Append('|').Append(id);
            foreach (var cell in cells)
                builder.Append(':').Append(cell.Row).Append(',')
                    .Append(cell.Column);
        }
    }
}
