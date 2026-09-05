using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum AdministrativeMapViewMode : byte
    {
        WorldReading = 0,
        CountyPlanning = 1
    }

    public enum AdministrativeMapLabelLevel : byte
    {
        Province = 1,
        CommanderyEquivalent = 2,
        County = 3,
        CurrentCountyAndNeighbors = 4
    }

    /// <summary>
    /// Rebuildable map state. It selects and projects the one formal world;
    /// it is deliberately not persisted as WorldState.
    /// </summary>
    public sealed class AdministrativeMapViewState
    {
        public AdministrativeMapViewMode ViewMode { get; private set; } =
            AdministrativeMapViewMode.WorldReading;
        public string SelectedAdministrativeRegionId { get; private set; } =
            string.Empty;
        public AdministrativeRegionLevel? SelectedLevel { get; private set; }
        public string PlanningCountyId { get; private set; } = string.Empty;
        public AdministrativeMapLabelLevel LabelLevel { get; private set; } =
            AdministrativeMapLabelLevel.Province;
        public int CameraCenterRow { get; private set; } = -1;
        public int CameraCenterColumn { get; private set; } = -1;
        public int CameraSpanRows { get; private set; }
        public int CameraSpanColumns { get; private set; }

        public void Select(AdministrativeRegionDefinition region)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            SelectedAdministrativeRegionId = region.Id;
            SelectedLevel = region.Level;
        }

        public void ClearSelection()
        {
            SelectedAdministrativeRegionId = string.Empty;
            SelectedLevel = null;
        }

        public void SetWorldLabelLevel(AdministrativeMapLabelLevel level)
        {
            if (ViewMode == AdministrativeMapViewMode.CountyPlanning)
            {
                LabelLevel = AdministrativeMapLabelLevel.
                    CurrentCountyAndNeighbors;
                return;
            }
            if (level == AdministrativeMapLabelLevel.
                CurrentCountyAndNeighbors)
                throw new ArgumentOutOfRangeException(nameof(level));
            LabelLevel = level;
        }

        public void EnterCountyPlanning(
            AdministrativeRegionSpatialSummary county,
            int contextMarginCells = 8)
        {
            if (county == null) throw new ArgumentNullException(nameof(county));
            if (county.Region.Level != AdministrativeRegionLevel.County)
                throw new ArgumentException(
                    "County planning requires a County region.", nameof(county));
            if (county.CellCount <= 0)
                throw new ArgumentException(
                    "County planning requires mapped Cells.", nameof(county));
            if (contextMarginCells < 1)
                throw new ArgumentOutOfRangeException(nameof(contextMarginCells));

            ViewMode = AdministrativeMapViewMode.CountyPlanning;
            PlanningCountyId = county.Region.Id;
            Select(county.Region);
            LabelLevel = AdministrativeMapLabelLevel.
                CurrentCountyAndNeighbors;
            CameraCenterRow = (county.MinRow + county.MaxRow) / 2;
            CameraCenterColumn = (county.MinColumn + county.MaxColumn) / 2;
            CameraSpanRows = checked(county.MaxRow - county.MinRow + 1 +
                contextMarginCells * 2);
            CameraSpanColumns = checked(county.MaxColumn - county.MinColumn +
                1 + contextMarginCells * 2);
        }

        public void ExitCountyPlanning()
        {
            ViewMode = AdministrativeMapViewMode.WorldReading;
            PlanningCountyId = string.Empty;
            LabelLevel = AdministrativeMapLabelLevel.Province;
            CameraCenterRow = -1;
            CameraCenterColumn = -1;
            CameraSpanRows = 0;
            CameraSpanColumns = 0;
        }
    }
}
