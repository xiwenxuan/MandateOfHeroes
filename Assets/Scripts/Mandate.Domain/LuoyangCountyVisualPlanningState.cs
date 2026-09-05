using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public enum CountyPlanningPrimaryTool : byte
    {
        None = 0,
        Building = 1,
        Road = 2,
        Wall = 3,
        Canal = 4,
        Zone = 5,
        Select = 6,
        DemolishDraft = 7,
        MoveDraft = 8,
        CopyDraft = 9,
        Eyedropper = 10
    }

    public enum CountyPlanningDraftKind : byte
    {
        Building = 1,
        Road = 2,
        Fortification = 3,
        Canal = 4,
        Zone = 5
    }

    public enum PlanningInputIntent : byte
    {
        None = 0,
        PrimaryToolAction = 1,
        CancelTool = 2,
        PanCamera = 3,
        RotateCamera = 4,
        ZoomCamera = 5,
        RotatePlacement = 6
    }

    public enum CountyPlanningZoneKind : byte
    {
        Residential = 1,
        Production = 2,
        Storage = 3,
        Agriculture = 4
    }

    public interface ICountyPlanningDraft
    {
        string DraftId { get; }
        CountyPlanningDraftKind Kind { get; }
        int CreatedOrder { get; }
    }

    public sealed class PlanningToolState
    {
        public CountyPlanningPrimaryTool PrimaryTool { get; private set; }
        public string SelectedProfileId { get; private set; } = string.Empty;
        public CountyPlanningZoneKind ZoneKind { get; private set; } =
            CountyPlanningZoneKind.Residential;
        public int RotationQuarterTurns { get; private set; }
        public bool IsDragging { get; private set; }
        public PlanningCellCoord DragStart { get; private set; }
        public string EditingDraftId { get; private set; } = string.Empty;

        public void Activate(CountyPlanningPrimaryTool tool,
            string profileId = null)
        {
            PrimaryTool = tool;
            SelectedProfileId = profileId ?? string.Empty;
            IsDragging = false;
            EditingDraftId = string.Empty;
        }

        public void ActivateZone(CountyPlanningZoneKind zoneKind)
        {
            ZoneKind = zoneKind;
            Activate(CountyPlanningPrimaryTool.Zone);
        }

        public void SetRotation(int quarterTurns)
        {
            RotationQuarterTurns = FacilityPlacementProfile
                .NormalizeRotation(quarterTurns);
        }

        public void BeginDrag(PlanningCellCoord start)
        {
            if (PrimaryTool != CountyPlanningPrimaryTool.Road &&
                PrimaryTool != CountyPlanningPrimaryTool.Wall &&
                PrimaryTool != CountyPlanningPrimaryTool.Canal &&
                PrimaryTool != CountyPlanningPrimaryTool.Zone &&
                PrimaryTool != CountyPlanningPrimaryTool.Select)
                throw new InvalidOperationException(
                    "The active planning tool does not support dragging.");
            DragStart = start;
            IsDragging = true;
        }

        public void BeginDraftEdit(CountyPlanningPrimaryTool tool,
            string draftId)
        {
            if (tool != CountyPlanningPrimaryTool.MoveDraft &&
                tool != CountyPlanningPrimaryTool.CopyDraft)
                throw new ArgumentOutOfRangeException(nameof(tool));
            PrimaryTool = tool;
            EditingDraftId = new StableId(draftId).Value;
            IsDragging = false;
        }

        public void EndDrag()
        {
            IsDragging = false;
        }

        public void CancelCurrentAction()
        {
            if (IsDragging)
            {
                IsDragging = false;
                return;
            }
            PrimaryTool = CountyPlanningPrimaryTool.None;
            SelectedProfileId = string.Empty;
            EditingDraftId = string.Empty;
        }
    }

    public static class PlanningInputContract
    {
        public static PlanningInputIntent ResolveMouseIntent(int button,
            bool altHeld, bool dragging, float wheelDelta = 0f)
        {
            if (Math.Abs(wheelDelta) > 0.0001f)
                return PlanningInputIntent.ZoomCamera;
            if (button == 2 && dragging)
                return PlanningInputIntent.PanCamera;
            if (button == 1 && altHeld && dragging)
                return PlanningInputIntent.RotateCamera;
            if (button == 1 && !altHeld && !dragging)
                return PlanningInputIntent.CancelTool;
            if (button == 0)
                return PlanningInputIntent.PrimaryToolAction;
            return PlanningInputIntent.None;
        }
    }

    public sealed class PlanningMapOverlayState
    {
        private bool _administrativeVisible = true;
        private bool _roadsVisible = true;
        private bool _riversVisible = true;
        private bool _gridVisible = true;
        private bool _fortificationsVisible = true;
        private bool _planningVisible = true;
        private bool _terrainAnalysisVisible;

        public bool AdministrativeVisible => _administrativeVisible;
        public bool RoadsVisible => _roadsVisible;
        public bool RiversVisible => _riversVisible;
        public bool GridVisible => _gridVisible;
        public bool FortificationsVisible => _fortificationsVisible;
        public bool PlanningVisible => _planningVisible;
        public bool TerrainAnalysisVisible => _terrainAnalysisVisible;
        public int Version { get; private set; }

        public void SetAdministrativeVisible(bool visible) =>
            Set(ref _administrativeVisible, visible);
        public void SetRoadsVisible(bool visible) =>
            Set(ref _roadsVisible, visible);
        public void SetRiversVisible(bool visible) =>
            Set(ref _riversVisible, visible);
        public void SetGridVisible(bool visible) =>
            Set(ref _gridVisible, visible);
        public void SetFortificationsVisible(bool visible) =>
            Set(ref _fortificationsVisible, visible);
        public void SetPlanningVisible(bool visible) =>
            Set(ref _planningVisible, visible);
        public void SetTerrainAnalysisVisible(bool visible) =>
            Set(ref _terrainAnalysisVisible, visible);

        private void Set(ref bool field, bool visible)
        {
            if (field == visible) return;
            field = visible;
            Version++;
        }
    }

    public sealed class PlanningDraftValidation
    {
        public PlanningDraftValidation(IEnumerable<PlacementIssue> blocking,
            IEnumerable<PlacementIssue> warnings)
        {
            BlockingReasons = Sort(blocking);
            Warnings = Sort(warnings);
        }

        public IReadOnlyList<PlacementIssue> BlockingReasons { get; }
        public IReadOnlyList<PlacementIssue> Warnings { get; }
        public bool IsValid => BlockingReasons.Count == 0;
        public PlacementValidationState State => !IsValid
            ? PlacementValidationState.Invalid
            : Warnings.Count > 0
                ? PlacementValidationState.Conditional
                : PlacementValidationState.Valid;
        public string PrimaryReason => BlockingReasons.Count > 0
            ? BlockingReasons[0].Message
            : Warnings.Count > 0 ? Warnings[0].Message : string.Empty;

        private static IReadOnlyList<PlacementIssue> Sort(
            IEnumerable<PlacementIssue> issues) => (issues ??
                Array.Empty<PlacementIssue>()).OrderBy(value =>
                    value.Priority).ThenBy(value => value.Code,
                    StringComparer.Ordinal).ToArray();
    }

    public abstract class CountyLinearDraft : ICountyPlanningDraft
    {
        protected CountyLinearDraft(string draftId,
            CountyPlanningDraftKind kind, IReadOnlyList<PlanningCellCoord> path,
            int createdOrder, PlanningDraftValidation validation)
        {
            DraftId = new StableId(draftId).Value;
            Kind = kind;
            Path = (path ?? Array.Empty<PlanningCellCoord>()).ToArray();
            CreatedOrder = createdOrder;
            Validation = validation ?? throw new ArgumentNullException(
                nameof(validation));
            if (Path.Count < 2)
                throw new ArgumentException("A linear draft needs a path.");
        }

        public string DraftId { get; }
        public CountyPlanningDraftKind Kind { get; }
        public IReadOnlyList<PlanningCellCoord> Path { get; }
        public int CreatedOrder { get; }
        public PlanningDraftValidation Validation { get; }
    }

    public sealed class DraftRoadGeometry : CountyLinearDraft
    {
        public DraftRoadGeometry(string draftId,
            IReadOnlyList<PlanningCellCoord> path, int createdOrder,
            PlanningDraftValidation validation) : base(draftId,
            CountyPlanningDraftKind.Road, path, createdOrder, validation)
        {
        }
    }

    public sealed class DraftCanalGeometry : CountyLinearDraft
    {
        public DraftCanalGeometry(string draftId,
            IReadOnlyList<PlanningCellCoord> path, int createdOrder,
            PlanningDraftValidation validation) : base(draftId,
            CountyPlanningDraftKind.Canal, path, createdOrder, validation)
        {
        }
    }

    public sealed class DraftFortificationSegment
    {
        public DraftFortificationSegment(PlanningCellCoord cell,
            PlanningCellDirection edgeDirection)
        {
            Cell = cell;
            EdgeDirection = edgeDirection;
        }

        public PlanningCellCoord Cell { get; }
        public PlanningCellDirection EdgeDirection { get; }
    }

    public sealed class DraftFortification : ICountyPlanningDraft
    {
        public DraftFortification(string draftId,
            IReadOnlyList<DraftFortificationSegment> segments,
            int createdOrder, PlanningDraftValidation validation)
        {
            DraftId = new StableId(draftId).Value;
            Segments = (segments ??
                Array.Empty<DraftFortificationSegment>()).ToArray();
            CreatedOrder = createdOrder;
            Validation = validation ?? throw new ArgumentNullException(
                nameof(validation));
            if (Segments.Count == 0)
                throw new ArgumentException("A wall draft needs segments.");
        }

        public string DraftId { get; }
        public CountyPlanningDraftKind Kind =>
            CountyPlanningDraftKind.Fortification;
        public IReadOnlyList<DraftFortificationSegment> Segments { get; }
        public int CreatedOrder { get; }
        public PlanningDraftValidation Validation { get; }
    }

    public sealed class DraftPlanningZone : ICountyPlanningDraft
    {
        public DraftPlanningZone(string draftId, CountyPlanningZoneKind kind,
            IReadOnlyList<PlanningCellCoord> cells, int createdOrder)
        {
            DraftId = new StableId(draftId).Value;
            ZoneKind = kind;
            Cells = (cells ?? Array.Empty<PlanningCellCoord>()).Distinct()
                .OrderBy(value => value).ToArray();
            CreatedOrder = createdOrder;
            if (Cells.Count == 0)
                throw new ArgumentException("A zone draft needs cells.");
        }

        public string DraftId { get; }
        public CountyPlanningDraftKind Kind => CountyPlanningDraftKind.Zone;
        public CountyPlanningZoneKind ZoneKind { get; }
        public IReadOnlyList<PlanningCellCoord> Cells { get; }
        public int CreatedOrder { get; }
    }
}
