using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Presentation
{
    public enum Luoyang50mPrototypeView : byte
    {
        TerrainWaterRoad,
        FacilityDistricts,
        MigrationPrecision,
        LayoutClosure
    }

    public sealed class Luoyang50mCountySpatialPrototypeController :
        MonoBehaviour
    {
        private Texture2D _mapTexture;
        private Material _mapMaterial;
        private GameObject _mapObject;
        private Camera _camera;
        private Luoyang50mPrototypeView _view;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }
        public Luoyang50mCountySpatialPrototype Prototype { get; private set; }
        public Luoyang50mCountyLayoutPackage LayoutPackage { get; private set; }
        public Luoyang50mCountySpatialBenchmarkResult Benchmark
            { get; private set; }
        public int PlanningCellGameObjectCount => 0;
        public int PlanningCellRenderObjectCount => _mapObject == null ? 0 : 1;
        public Texture2D MapTexture => _mapTexture;
        public Camera PresentationCamera => _camera;
        public Luoyang50mPrototypeView CurrentView => _view;

        private void Start()
        {
            if (!IsReady) TryInitialize();
        }

        private void OnDestroy()
        {
            if (_mapTexture != null)
                DestroyRuntimeObject(_mapTexture);
            if (_mapMaterial != null)
                DestroyRuntimeObject(_mapMaterial);
        }

        public bool TryInitialize()
        {
            if (IsReady) return true;
            try
            {
                var worldMapRoot = Path.Combine(Application.streamingAssetsPath,
                    "WorldMap");
                var source = new Luoyang50mCountySpatialPrototypeSource(
                    worldMapRoot);
                Prototype = source.Prototype;
                LayoutPackage = source.LayoutPackage;
                Benchmark = Luoyang50mCountySpatialPrototypeBenchmark.Run(
                    Prototype, 3);
                EnsureCamera();
                BuildMapObject();
                SetView(Luoyang50mPrototypeView.TerrainWaterRoad);
                IsReady = true;
                LastError = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                Debug.LogException(exception);
                return false;
            }
        }

        public void SetView(Luoyang50mPrototypeView view)
        {
            if (Prototype == null)
                throw new InvalidOperationException(
                    "Initialize the Luoyang 50m prototype first.");
            _view = view;
            var rows = Prototype.Partition.Rows;
            var columns = Prototype.Partition.Columns;
            var pixels = new Color32[rows * columns];
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                var target = (rows - 1 - row) * columns + column;
                pixels[target] = BaseColor(row, column, view);
            }
            if (view == Luoyang50mPrototypeView.LayoutClosure)
                foreach (var area in LayoutPackage.DistrictAreas)
                    PaintHull(pixels, rows, columns, area,
                        DistrictColor(area.DistrictId));
            foreach (var facility in Prototype.Facilities)
            {
                Prototype.Partition.TryToLocal(facility.CandidateCell,
                    out var row, out var column);
                var color = view == Luoyang50mPrototypeView.FacilityDistricts
                    ? DistrictColor(facility.DistrictId)
                    : view == Luoyang50mPrototypeView.MigrationPrecision
                        ? PrecisionColor(facility.SourceSpatialPrecisionId)
                        : new Color32(226, 177, 64, 255);
                Paint(pixels, rows, columns, row, column, color,
                    view == Luoyang50mPrototypeView.TerrainWaterRoad ? 0 : 1);
            }
            if (view == Luoyang50mPrototypeView.LayoutClosure)
            {
                foreach (var edge in LayoutPackage.RoadEdges)
                    PaintLine(pixels, rows, columns, edge.FromLocalRow,
                        edge.FromLocalColumn, edge.ToLocalRow,
                        edge.ToLocalColumn, new Color32(211, 173, 91, 255));
                foreach (var edge in LayoutPackage.CanalEdges)
                    PaintLine(pixels, rows, columns, edge.FromLocalRow,
                        edge.FromLocalColumn, edge.ToLocalRow,
                        edge.ToLocalColumn, new Color32(55, 151, 190, 255));
                foreach (var wall in LayoutPackage.Fortifications)
                    Paint(pixels, rows, columns, wall.LocalRow,
                        wall.LocalColumn, wall.IsGate
                            ? new Color32(246, 207, 91, 255)
                            : new Color32(174, 69, 62, 255), 1);
            }
            foreach (var portal in Prototype.Partition.Portals.Values)
            {
                Prototype.Partition.TryToLocal(portal.Cell, out var row,
                    out var column);
                Paint(pixels, rows, columns, row, column,
                    new Color32(255, 255, 255, 255), 3);
            }
            _mapTexture.SetPixels32(pixels);
            _mapTexture.Apply(false, false);
        }

        public void CaptureEvidence(string absolutePath, int width = 1280,
            int height = 720)
        {
            if (!IsReady || _camera == null)
                throw new InvalidOperationException(
                    "Prototype presentation is not ready.");
            var render = new RenderTexture(width, height, 24,
                RenderTextureFormat.ARGB32);
            var previousTarget = _camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                _camera.targetTexture = render;
                _camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(width, height,
                    TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, image.EncodeToPNG());
                DestroyRuntimeObject(image);
            }
            finally
            {
                _camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                render.Release();
                DestroyRuntimeObject(render);
            }
        }

        private void EnsureCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.AddComponent<Camera>();
            }
            _camera.orthographic = true;
            _camera.orthographicSize = 6.2f;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _camera.transform.rotation = Quaternion.identity;
            _camera.backgroundColor = new Color(0.035f, 0.045f, 0.035f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void BuildMapObject()
        {
            _mapTexture = new Texture2D(
                Luoyang50mCountySpatialPrototypeIds.Columns,
                Luoyang50mCountySpatialPrototypeIds.Rows,
                TextureFormat.RGBA32, false, true)
            {
                name = "Luoyang 50m County Prototype Map",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var shader = Shader.Find("Unlit/Texture") ??
                         Shader.Find("Sprites/Default");
            _mapMaterial = new Material(shader)
            {
                name = "Luoyang 50m County Prototype Material",
                mainTexture = _mapTexture
            };
            _mapObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _mapObject.name = "Luoyang 320x640 Planning Grid (one renderer)";
            _mapObject.transform.SetParent(transform, false);
            _mapObject.transform.localScale = new Vector3(20f, 10f, 1f);
            _mapObject.GetComponent<Renderer>().sharedMaterial = _mapMaterial;
            var collider = _mapObject.GetComponent<Collider>();
            if (collider != null) DestroyRuntimeObject(collider);
        }

        private Color32 BaseColor(int row, int column,
            Luoyang50mPrototypeView view)
        {
            if (view != Luoyang50mPrototypeView.TerrainWaterRoad)
                return new Color32(36, 42, 31, 255);
            var partition = Prototype.Partition;
            if (partition.WaterState(row, column) > 0)
                return new Color32(42, 101, 132, 255);
            if (partition.LandUse(row, column) == PlanningLandUseClass.Road)
                return new Color32(176, 145, 86, 255);
            switch (partition.Terrain(row, column))
            {
                case PlanningTerrainClass.Hill:
                    return new Color32(92, 93, 54, 255);
                case PlanningTerrainClass.Forest:
                    return new Color32(47, 83, 49, 255);
                case PlanningTerrainClass.Marsh:
                    return new Color32(62, 94, 77, 255);
                default:
                    return new Color32(91, 116, 61, 255);
            }
        }

        private static Color32 DistrictColor(string id)
        {
            if (id == LuoyangWholeCityCompositionIds.PalaceCivicDistrictId)
                return new Color32(197, 69, 55, 255);
            if (id == LuoyangWholeCityCompositionIds.ResidentialWardDistrictId)
                return new Color32(220, 174, 90, 255);
            if (id == LuoyangWholeCityCompositionIds.MarketWorkshopDistrictId)
                return new Color32(191, 112, 54, 255);
            if (id == LuoyangWholeCityCompositionIds.DefenseDistrictId)
                return new Color32(126, 91, 119, 255);
            if (id == LuoyangWholeCityCompositionIds.WaterTransportDistrictId)
                return new Color32(55, 139, 164, 255);
            return new Color32(91, 151, 77, 255);
        }

        private static Color32 PrecisionColor(string id)
        {
            if (string.Equals(id, "Cell", StringComparison.OrdinalIgnoreCase))
                return new Color32(87, 178, 112, 255);
            if (string.Equals(id, "Probable",
                    StringComparison.OrdinalIgnoreCase))
                return new Color32(235, 199, 75, 255);
            return new Color32(221, 112, 57, 255);
        }

        private static void Paint(Color32[] pixels, int rows, int columns,
            int row, int column, Color32 color, int radius)
        {
            for (var dr = -radius; dr <= radius; dr++)
            for (var dc = -radius; dc <= radius; dc++)
            {
                var r = row + dr;
                var c = column + dc;
                if (r < 0 || r >= rows || c < 0 || c >= columns) continue;
                pixels[(rows - 1 - r) * columns + c] = color;
            }
        }

        private static void PaintHull(Color32[] pixels, int rows, int columns,
            Luoyang50mLayoutArea area, Color32 color)
        {
            for (var index = 0; index < area.HullCells.Count; index++)
            {
                var first = area.HullCells[index];
                var second = area.HullCells[(index + 1) %
                                            area.HullCells.Count];
                PaintLine(pixels, rows, columns, first.Row, first.Column,
                    second.Row, second.Column, color);
            }
        }

        private static void PaintLine(Color32[] pixels, int rows, int columns,
            int firstRow, int firstColumn, int secondRow, int secondColumn,
            Color32 color)
        {
            var row = firstRow;
            var column = firstColumn;
            var deltaColumn = Math.Abs(secondColumn - firstColumn);
            var columnStep = firstColumn < secondColumn ? 1 : -1;
            var deltaRow = -Math.Abs(secondRow - firstRow);
            var rowStep = firstRow < secondRow ? 1 : -1;
            var error = deltaColumn + deltaRow;
            while (true)
            {
                Paint(pixels, rows, columns, row, column, color, 0);
                if (row == secondRow && column == secondColumn) break;
                var doubled = 2 * error;
                if (doubled >= deltaRow)
                {
                    error += deltaRow;
                    column += columnStep;
                }
                if (doubled <= deltaColumn)
                {
                    error += deltaColumn;
                    row += rowStep;
                }
            }
        }

        private void OnGUI()
        {
            if (!IsReady) return;
            GUI.Box(new Rect(18, 18, 730, 146),
                "洛阳50m县域运行时权威布局 V1（史实定位仍待审）");
            GUI.Label(new Rect(34, 47, 690, 24),
                "512 km²  |  320×640 = 204,800格  |  Chunk 800");
            GUI.Label(new Rect(34, 70, 690, 24),
                "Facility 2,084  |  道路边334  |  水渠边17  |  城防144  |  Portal 4");
            GUI.Label(new Rect(34, 93, 690, 24),
                "JSON是运行时唯一布局输入；玩法重建候选，不是历史精绘且未写入存档");
            if (GUI.Button(new Rect(34, 121, 150, 28), "地形/水系/道路"))
                SetView(Luoyang50mPrototypeView.TerrainWaterRoad);
            if (GUI.Button(new Rect(194, 121, 150, 28), "六类城市分区"))
                SetView(Luoyang50mPrototypeView.FacilityDistricts);
            if (GUI.Button(new Rect(354, 121, 150, 28), "源空间精度"))
                SetView(Luoyang50mPrototypeView.MigrationPrecision);
            if (GUI.Button(new Rect(514, 121, 190, 28), "布局闭环/网络几何"))
                SetView(Luoyang50mPrototypeView.LayoutClosure);
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
