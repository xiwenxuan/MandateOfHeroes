using System;
using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using UnityEngine;

namespace Mandate.Presentation
{
    public sealed class MapValidationController : MonoBehaviour
    {
        private const int TextureWidth = 384;
        private const int TextureHeight = 240;
        private WorldMapDataReader _reader;
        private Texture2D _mapTexture;
        private Color32[] _pixels;
        private int _centerRow;
        private int _centerColumn;
        private int _cellsPerPixel;
        private WorldMapCellRecord _selectedCell;
        private bool _hasSelection;
        private Rect _mapRect;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }
        public WorldMapCellRecord SelectedCell => _selectedCell;
        public int PositionedCityCount { get; private set; }

        private void Start()
        {
            TryInitialize();
        }

        public bool TryInitialize(string packageRoot = null)
        {
            if (IsReady)
            {
                return true;
            }

            try
            {
                packageRoot = packageRoot ?? Path.Combine(
                    Application.streamingAssetsPath, "WorldMap", "HanWorldV0");
                _reader = new WorldMapDataReader(packageRoot);
                PositionedCityCount = 0;
                foreach (var feature in _reader.Cities.Features)
                {
                    if (feature.Properties.CellId.HasValue)
                    {
                        PositionedCityCount++;
                    }
                }
                _centerRow = _reader.Grid.Rows / 2;
                _centerColumn = _reader.Grid.Columns / 2;
                _cellsPerPixel = Math.Max(1, (int)Math.Ceiling(_reader.Grid.Columns / (double)TextureWidth));
                _mapTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "HanWorldV0 Chunk Map"
                };
                _pixels = new Color32[TextureWidth * TextureHeight];
                RenderMap();
                SelectCell(_centerRow, _centerColumn);
                IsReady = true;
                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                Debug.LogError($"MapValidation failed to initialize: {LastError}");
                return false;
            }
        }

        private void OnDestroy()
        {
            _reader?.Dispose();
            if (_mapTexture != null)
            {
                Destroy(_mapTexture);
            }
        }

        private void OnGUI()
        {
            var previous = GUI.color;
            GUI.color = new Color(0.95f, 0.91f, 0.81f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.17f, 0.12f, 0.08f) }
            };
            GUI.Label(new Rect(20, 12, Screen.width - 40, 34), "东汉世界地图 V0 · 真实 Cell / Chunk 验证场景", titleStyle);

            if (!IsReady)
            {
                GUI.Label(new Rect(20, 55, Screen.width - 40, Screen.height - 70),
                    string.IsNullOrEmpty(LastError) ? "正在读取地图数据……" : LastError);
                return;
            }

            GUI.Label(new Rect(20, 48, Screen.width - 40, 25),
                $"{_reader.Manifest.Columns}×{_reader.Manifest.Rows} = {_reader.Manifest.TotalCells:N0} Cells · " +
                $"64×64 Chunk · 当前每像素 {_cellsPerPixel} Cell（滚轮/按钮缩放）");

            var width = Mathf.Min(Screen.width - 40, 1100);
            var height = Mathf.Min(Screen.height - 255, 670);
            _mapRect = new Rect(20, 78, width, height);
            GUI.DrawTexture(_mapRect, _mapTexture, ScaleMode.StretchToFill, false);
            DrawCityMarkers();
            HandleMapClick(Event.current);

            var controlsY = _mapRect.yMax + 8;
            if (GUI.Button(new Rect(20, controlsY, 56, 28), "←")) Pan(0, -1);
            if (GUI.Button(new Rect(80, controlsY, 56, 28), "→")) Pan(0, 1);
            if (GUI.Button(new Rect(140, controlsY, 56, 28), "↑")) Pan(-1, 0);
            if (GUI.Button(new Rect(200, controlsY, 56, 28), "↓")) Pan(1, 0);
            if (GUI.Button(new Rect(266, controlsY, 72, 28), "放大 +")) Zoom(-1);
            if (GUI.Button(new Rect(342, controlsY, 72, 28), "缩小 -")) Zoom(1);
            if (GUI.Button(new Rect(420, controlsY, 90, 28), "全国视图")) ResetView();

            if (_hasSelection)
            {
                var province = _reader.ResolveProvince(_selectedCell.ProvinceCode) ?? "无";
                var commandery = _reader.ResolveCommandery(_selectedCell.CommanderyCode) ?? "无";
                var county = _reader.ResolveCounty(_selectedCell.CountyCode) ?? "无";
                GUI.Label(new Rect(20, controlsY + 34, Screen.width - 40, 54),
                    $"CellId={_selectedCell.Id.Value}  Row={_selectedCell.Row}  Col={_selectedCell.Column}  " +
                    $"中心=({_selectedCell.CenterX:F0}, {_selectedCell.CenterY:F0})m  高程={_selectedCell.Elevation}m\n" +
                    $"地形={_selectedCell.TerrainClass}  坡度={_selectedCell.SlopeClass}  水域={_selectedCell.WaterClass}  道路={_selectedCell.RoadClass}  " +
                    $"可建设={_selectedCell.Buildable}  可通行={_selectedCell.Passable}  行政={province} / {commandery} / {county}");
            }
        }

        private void HandleMapClick(Event current)
        {
            if (current.type != EventType.MouseDown || current.button != 0 || !_mapRect.Contains(current.mousePosition))
            {
                return;
            }

            var normalizedX = (current.mousePosition.x - _mapRect.x) / _mapRect.width;
            var normalizedY = (current.mousePosition.y - _mapRect.y) / _mapRect.height;
            var columnStart = _centerColumn - TextureWidth * _cellsPerPixel / 2;
            var rowStart = _centerRow - TextureHeight * _cellsPerPixel / 2;
            var column = columnStart + Mathf.FloorToInt(normalizedX * TextureWidth) * _cellsPerPixel;
            var row = rowStart + Mathf.FloorToInt(normalizedY * TextureHeight) * _cellsPerPixel;
            SelectCell(row, column);
            current.Use();
        }

        private void SelectCell(int row, int column)
        {
            row = Mathf.Clamp(row, 0, _reader.Grid.Rows - 1);
            column = Mathf.Clamp(column, 0, _reader.Grid.Columns - 1);
            _selectedCell = _reader.ReadCell(row, column);
            _hasSelection = true;
        }

        private void DrawCityMarkers()
        {
            var columnStart = _centerColumn - TextureWidth * _cellsPerPixel / 2;
            var rowStart = _centerRow - TextureHeight * _cellsPerPixel / 2;
            var previous = GUI.color;
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.28f, 0.04f, 0.02f) }
            };
            foreach (var feature in _reader.Cities.Features)
            {
                var city = feature.Properties;
                if (!city.Row.HasValue || !city.Column.HasValue)
                {
                    continue;
                }

                var normalizedX = (city.Column.Value - columnStart) / (float)(TextureWidth * _cellsPerPixel);
                var normalizedY = (city.Row.Value - rowStart) / (float)(TextureHeight * _cellsPerPixel);
                if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
                {
                    continue;
                }

                var x = _mapRect.x + normalizedX * _mapRect.width;
                var y = _mapRect.y + normalizedY * _mapRect.height;
                GUI.color = new Color(0.72f, 0.08f, 0.04f, 1f);
                GUI.DrawTexture(new Rect(x - 3, y - 3, 7, 7), Texture2D.whiteTexture);
                GUI.color = previous;
                if (_cellsPerPixel <= 4)
                {
                    GUI.Label(new Rect(x + 5, y - 8, 90, 18), city.DisplayName ?? city.CityId, labelStyle);
                }
            }
            GUI.color = previous;
        }

        private void Pan(int rowDirection, int columnDirection)
        {
            _centerRow = Mathf.Clamp(_centerRow + rowDirection * 40 * _cellsPerPixel, 0, _reader.Grid.Rows - 1);
            _centerColumn = Mathf.Clamp(_centerColumn + columnDirection * 40 * _cellsPerPixel, 0, _reader.Grid.Columns - 1);
            RenderMap();
        }

        private void Zoom(int direction)
        {
            _cellsPerPixel = direction < 0 ? Math.Max(1, _cellsPerPixel / 2) : Math.Min(64, _cellsPerPixel * 2);
            RenderMap();
        }

        private void ResetView()
        {
            _centerRow = _reader.Grid.Rows / 2;
            _centerColumn = _reader.Grid.Columns / 2;
            _cellsPerPixel = Math.Max(1, (int)Math.Ceiling(_reader.Grid.Columns / (double)TextureWidth));
            RenderMap();
        }

        private void RenderMap()
        {
            var columnStart = _centerColumn - TextureWidth * _cellsPerPixel / 2;
            var rowStart = _centerRow - TextureHeight * _cellsPerPixel / 2;
            for (var y = 0; y < TextureHeight; y++)
            {
                var row = rowStart + y * _cellsPerPixel;
                for (var x = 0; x < TextureWidth; x++)
                {
                    var column = columnStart + x * _cellsPerPixel;
                    _pixels[(TextureHeight - 1 - y) * TextureWidth + x] =
                        _reader.Grid.Contains(row, column) ? CellColor(_reader.ReadCell(row, column)) : new Color32(30, 27, 24, 255);
                }
            }

            _mapTexture.SetPixels32(_pixels);
            _mapTexture.Apply(false, false);
        }

        private static Color32 CellColor(WorldMapCellRecord cell)
        {
            if ((cell.WaterClass & 4) != 0) return new Color32(45, 112, 155, 255);
            if ((cell.WaterClass & 2) != 0) return new Color32(55, 132, 180, 255);
            if ((cell.WaterClass & 1) != 0) return new Color32(37, 73, 104, 255);
            if (cell.RoadClass > 0) return new Color32(174, 112, 51, 255);
            switch (cell.TerrainClass)
            {
                case 1: return new Color32(129, 157, 91, 255);
                case 2: return new Color32(115, 126, 76, 255);
                case 3: return new Color32(104, 92, 71, 255);
                case 4: return new Color32(174, 169, 155, 255);
                default: return new Color32(40, 50, 52, 255);
            }
        }
    }
}
