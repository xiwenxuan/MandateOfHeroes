using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mandate.Domain;
using UnityEngine;

namespace Mandate.Presentation
{
    public sealed partial class LuoyangCountyPlanningPresentationController
    {
        private CountyMapPresentationStack _presentationStack;
        private readonly CountyMapPresentationLodController
            _presentationLodController =
                new CountyMapPresentationLodController();
        private IReadOnlyList<CountyRoadPresentationSegment> _visibleRoads =
            Array.Empty<CountyRoadPresentationSegment>();
        private IReadOnlyList<CountyFacilityPresentationItem>
            _visibleFacilities =
                Array.Empty<CountyFacilityPresentationItem>();
        private IReadOnlyList<Luoyang50mLayoutFortification>
            _visibleFortifications =
                Array.Empty<Luoyang50mLayoutFortification>();
        private IReadOnlyList<Luoyang50mLayoutFortification> _visibleGates =
            Array.Empty<Luoyang50mLayoutFortification>();

        public CountyMapPresentationLod PresentationLod =>
            _presentationLodController.Current;
        public CountyMapPresentationStack PresentationStack =>
            _presentationStack;
        public CountyMapPresentationSnapshot PresentationSnapshot
            { get; private set; }
        public double LastLodTransitionMilliseconds { get; private set; }
        public bool ShouldShowPlanningGrid =>
            _presentationStack != null &&
            _presentationStack.IsLayerVisible(
                CountyMapPresentationLayerId.PlanningGrid,
                PresentationLod, PresentationMode, MapOverlays);

        private CountyMapViewport CurrentPresentationViewport(
            float marginCells = 0f) => new CountyMapViewport(
            _viewMinimumRow, _viewMinimumColumn, _viewRows, _viewColumns,
            marginCells);

        private void InitializeCountyPresentationStack()
        {
            _presentationStack = new CountyMapPresentationStack(
                _layoutPackage, _prototype.Partition);
            _presentationLodController.Reset(_viewRows);
            RefreshCountyPresentation(false);
        }

        private void RefreshCountyPresentation(bool rebuildTexture)
        {
            if (_presentationStack == null) return;
            var watch = Stopwatch.StartNew();
            var viewport = CurrentPresentationViewport(4f);
            _visibleRoads = _presentationStack.VisibleRoads(
                PresentationLod, viewport);
            _visibleFacilities = _presentationStack.VisibleFacilities(
                PresentationLod, viewport);
            _visibleFortifications = _presentationStack
                .VisibleFortifications(PresentationLod, viewport);
            _visibleGates = _presentationStack.VisibleGates(viewport);
            PresentationSnapshot = _presentationStack.Snapshot(
                PresentationLod, viewport, PresentationMode, MapOverlays);
            if (rebuildTexture) RebuildPlanningMapTexture();
            watch.Stop();
            LastLodTransitionMilliseconds = watch.Elapsed.TotalMilliseconds;
        }

        private void ResetCountyPresentationLod()
        {
            if (_presentationStack == null) return;
            _presentationLodController.Reset(_viewRows);
            RefreshCountyPresentation(true);
        }

        private void UpdateCountyPresentationLod()
        {
            if (_presentationStack == null) return;
            var changed = _presentationLodController.Update(_viewRows);
            RefreshCountyPresentation(changed);
        }

        private Color32 ApplyCountySurfaceWash(Color32 source, int row,
            int column)
        {
            if (_presentationStack == null ||
                PresentationLod == CountyMapPresentationLod.Near)
                return source;
            var urban = _presentationStack.UrbanDensityAt(row, column);
            var agriculture = _presentationStack.AgriculturalDensityAt(
                row, column);
            var district = _presentationStack.DistrictWashAt(row, column);
            if (agriculture > 18)
                source = Blend(source, new Color32(129, 126, 66, 255),
                    (byte)Mathf.Clamp(agriculture / 5, 8, 42));
            if (urban > 18)
                source = Blend(source, new Color32(117, 103, 76, 255),
                    (byte)Mathf.Clamp(urban / 4, 10, 58));
            if (district >= 0)
            {
                var tint = (district & 1) == 0
                    ? new Color32(133, 112, 78, 255)
                    : new Color32(106, 111, 76, 255);
                source = Blend(source, tint, 20);
            }
            return source;
        }

        private static Color32 Blend(Color32 source, Color32 tint,
            byte weight)
        {
            var inverse = 255 - weight;
            return new Color32(
                (byte)((source.r * inverse + tint.r * weight) / 255),
                (byte)((source.g * inverse + tint.g * weight) / 255),
                (byte)((source.b * inverse + tint.b * weight) / 255), 255);
        }

        private void DrawCountyPresentationLayers(Rect mapRect)
        {
            if (_presentationStack == null) return;
            if (_presentationStack.IsLayerVisible(
                    CountyMapPresentationLayerId.Road, PresentationLod,
                    PresentationMode, MapOverlays))
                DrawPublishedRoads(mapRect);
            if (_presentationStack.IsLayerVisible(
                    CountyMapPresentationLayerId.Fortification,
                    PresentationLod, PresentationMode, MapOverlays))
                DrawPublishedFortifications(mapRect);
            DrawPublishedFacilities(mapRect);
        }

        private void DrawPublishedRoads(Rect mapRect)
        {
            foreach (var road in _visibleRoads)
            {
                Color color;
                float width;
                switch (road.PresentationClass)
                {
                    case CountyRoadPresentationClass.StrategicR0:
                        color = new Color(0.46f, 0.37f, 0.23f, 0.92f);
                        width = 4.2f;
                        break;
                    case CountyRoadPresentationClass.CountyMainR1:
                        color = new Color(0.52f, 0.43f, 0.28f, 0.80f);
                        width = 3.1f;
                        break;
                    case CountyRoadPresentationClass.UrbanMainR2:
                        color = new Color(0.57f, 0.49f, 0.35f, 0.64f);
                        width = 2.2f;
                        break;
                    default:
                        color = new Color(0.61f, 0.55f, 0.43f, 0.45f);
                        width = 1.2f;
                        break;
                }
                DrawLine(LocalCellCenter(road.Edge.FromLocalRow,
                        road.Edge.FromLocalColumn, mapRect),
                    LocalCellCenter(road.Edge.ToLocalRow,
                        road.Edge.ToLocalColumn, mapRect), color, width);
            }
        }

        private void DrawPublishedFacilities(Rect mapRect)
        {
            if (!_presentationStack.IsLayerVisible(
                    PresentationLod == CountyMapPresentationLod.Near
                        ? CountyMapPresentationLayerId.FacilityDetail
                        : CountyMapPresentationLayerId.FacilityAggregate,
                    PresentationLod, PresentationMode, MapOverlays)) return;
            foreach (var item in _visibleFacilities)
            {
                var facility = item.Facility;
                var center = LocalCellCenter(facility.LocalRow,
                    facility.LocalColumn, mapRect);
                var widthMetres = facility.WidthCentimetres / 100f;
                var depthMetres = facility.DepthCentimetres / 100f;
                if ((facility.RotationQuarterTurns & 1) != 0)
                {
                    var temporary = widthMetres;
                    widthMetres = depthMetres;
                    depthMetres = temporary;
                }
                var width = Mathf.Max(PresentationLod ==
                    CountyMapPresentationLod.Far ? 7f : 3f,
                    widthMetres / (DualScaleCountySpatialContractV1
                        .PlanningCellSizeMetres * _viewColumns) *
                    mapRect.width);
                var height = Mathf.Max(PresentationLod ==
                    CountyMapPresentationLod.Far ? 7f : 3f,
                    depthMetres / (DualScaleCountySpatialContractV1
                        .PlanningCellSizeMetres * _viewRows) *
                    mapRect.height);
                var bounds = new Rect(center.x - width * 0.5f,
                    center.y - height * 0.5f, width, height);
                var major = item.Kind ==
                    CountyFacilityPresentationKind.Major;
                DrawFilled(bounds, major
                    ? new Color(0.72f, 0.55f, 0.25f, 0.92f)
                    : new Color(0.55f, 0.48f, 0.34f, 0.76f));
                if (PresentationLod == CountyMapPresentationLod.Near)
                {
                    DrawOutline(bounds, new Color(0.22f, 0.18f, 0.12f,
                        0.88f), 1f);
                    DrawFacilityEntrance(facility, center, bounds, mapRect);
                }
                if (major && bounds.width > 38f && bounds.height > 14f)
                    GUI.Label(bounds, facility.DisplayName);
            }
        }

        private void DrawFacilityEntrance(Luoyang50mLayoutFacility facility,
            Vector2 center, Rect bounds, Rect mapRect)
        {
            var position = center;
            switch (facility.EntranceDirection)
            {
                case PlanningCellDirection.North:
                    position.y = bounds.y;
                    break;
                case PlanningCellDirection.South:
                    position.y = bounds.yMax;
                    break;
                case PlanningCellDirection.East:
                    position.x = bounds.xMax;
                    break;
                case PlanningCellDirection.West:
                    position.x = bounds.x;
                    break;
            }
            DrawFilled(new Rect(position.x - 2f, position.y - 2f, 4f, 4f),
                new Color(0.94f, 0.83f, 0.48f, 0.95f));
        }

        private void DrawPublishedFortifications(Rect mapRect)
        {
            if (PresentationLod == CountyMapPresentationLod.Far)
            {
                foreach (var hull in _presentationStack
                             .FarFortificationOutlines)
                {
                    for (var index = 0; index < hull.Count; index++)
                        DrawLine(LocalCellCenter(hull[index].Row,
                                hull[index].Column, mapRect),
                            LocalCellCenter(hull[(index + 1) % hull.Count].Row,
                                hull[(index + 1) % hull.Count].Column,
                                mapRect),
                            new Color(0.34f, 0.25f, 0.17f, 0.94f), 3.2f);
                }
            }
            else
            {
                foreach (var wall in _visibleFortifications)
                    DrawFortificationEdge(wall, mapRect,
                        new Color(0.37f, 0.27f, 0.19f, 0.94f),
                        PresentationLod == CountyMapPresentationLod.Mid
                            ? 2.8f : 2f);
            }
            foreach (var gate in _visibleGates)
            {
                var position = LocalCellCenter(gate.LocalRow,
                    gate.LocalColumn, mapRect);
                var size = PresentationLod == CountyMapPresentationLod.Far
                    ? 9f : 7f;
                DrawFilled(new Rect(position.x - size * 0.5f,
                    position.y - size * 0.5f, size, size),
                    new Color(0.82f, 0.61f, 0.22f, 0.98f));
            }
        }

        private void DrawFortificationEdge(
            Luoyang50mLayoutFortification wall, Rect mapRect, Color color,
            float width)
        {
            var center = LocalCellCenter(wall.LocalRow, wall.LocalColumn,
                mapRect);
            var halfWidth = mapRect.width / _viewColumns * 0.5f;
            var halfHeight = mapRect.height / _viewRows * 0.5f;
            Vector2 first;
            Vector2 second;
            if (wall.Direction == PlanningCellDirection.North ||
                wall.Direction == PlanningCellDirection.South)
            {
                var y = center.y + (wall.Direction ==
                    PlanningCellDirection.North ? -halfHeight : halfHeight);
                first = new Vector2(center.x - halfWidth, y);
                second = new Vector2(center.x + halfWidth, y);
            }
            else
            {
                var x = center.x + (wall.Direction ==
                    PlanningCellDirection.East ? halfWidth : -halfWidth);
                first = new Vector2(x, center.y - halfHeight);
                second = new Vector2(x, center.y + halfHeight);
            }
            DrawLine(first, second, color, width);
        }

        private Vector2 LocalCellCenter(int row, int column, Rect mapRect) =>
            new Vector2(
                mapRect.x + (column + 0.5f - _viewMinimumColumn) /
                _viewColumns * mapRect.width,
                mapRect.y + (row + 0.5f - _viewMinimumRow) /
                _viewRows * mapRect.height);
    }
}
