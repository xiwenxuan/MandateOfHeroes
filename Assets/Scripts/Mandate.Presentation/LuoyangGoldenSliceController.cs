using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;

namespace Mandate.Presentation
{
    public sealed partial class LuoyangWorldValidationController
    {
        private bool _usePlayablePresentation = true;
        private bool _buildMode;
        private bool _debugGrid;
        private MapVisualLod _visualLod = MapVisualLod.City;
        private LuoyangVisualPresentationSystem _visualSystem;
        private LuoyangGoldenSliceProjection _goldenSlice;
        private Texture2D _goldenSliceBackground;
        private string _selectedVisualFacilityId;
        private string _selectedShipmentId;
        private uint? _selectedActorOrdinal;
        private string _selectedBlueprintId =
            "blueprint.han.residence.general.v1";
        private ulong _selectedBuildCellId;
        private string _playableMessage =
            "选择人物、设施或商队；建设模式会显示真实可开发 Cell。";

        public bool UsesPlayablePresentation => _usePlayablePresentation;
        public bool BuildModeEnabled => _buildMode;
        public bool NormalModeHidesCellGrid => !_debugGrid && !_buildMode;
        public MapVisualLod VisualLod => _visualLod;
        public LuoyangGoldenSliceProjection GoldenSlice => _goldenSlice;
        public string PlayableMessage => _playableMessage;

        private void InitializePlayablePresentation()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(Path.Combine(
                Application.streamingAssetsPath, "WorldMap"));
            _visualSystem = new LuoyangVisualPresentationSystem(coverage.Bindings,
                coverage.CombinedCatalog);
            var capable = _livingRuntime.Households
                .Where(item => item.Wealth >= 100)
                .OrderByDescending(item => item.Wealth)
                .ThenBy(item => item.HouseholdOrdinal).FirstOrDefault();
            if (capable != null) SelectLivingPerson(capable.HeadPersonOrdinal);
            RefreshGoldenSlice();
            _goldenSliceBackground = Resources.Load<Texture2D>(
                "Art/Han/Luoyang/luoyang-golden-slice-v1");
        }

        private void DisposePlayablePresentation()
        {
            // Resource lifetime is owned by Unity's Resources system.
            _goldenSliceBackground = null;
        }

        public void RefreshGoldenSlice()
        {
            if (_visualSystem == null || _livingRuntime == null) return;
            var actorBudget = _visualLod == MapVisualLod.Close ? 96 :
                _visualLod == MapVisualLod.City ? 48 : 16;
            _goldenSlice = _visualSystem.BuildProjection(_livingRuntime,
                actorBudget, _visualLod == MapVisualLod.Close ? 96 : 72);
        }

        public void SetPlayablePresentation(bool enabled)
        {
            _usePlayablePresentation = enabled;
            if (enabled) RefreshGoldenSlice();
        }

        public void SetBuildMode(bool enabled)
        {
            _buildMode = enabled;
            _debugGrid = false;
            _playableMessage = enabled
                ? "建设模式：Cell 网格已显示。先购买空地，再选择蓝图确认建设。"
                : "正常游戏模式：Cell 网格隐藏。";
        }

        public void SetVisualLod(MapVisualLod lod)
        {
            _visualLod = lod;
            RefreshGoldenSlice();
        }

        public bool SelectVisualFacility(string facilityId)
        {
            if (_livingRuntime == null || !_livingRuntime.Facilities.Exists(item =>
                    item.FacilityId == facilityId)) return false;
            _selectedVisualFacilityId = facilityId;
            _selectedShipmentId = null;
            _selectedActorOrdinal = null;
            return true;
        }

        public bool SelectVisualActor(uint ordinal)
        {
            if (_livingRuntime == null || ordinal >= _livingRuntime.Workforce.Count)
                return false;
            _selectedActorOrdinal = ordinal;
            _selectedVisualFacilityId = null;
            _selectedShipmentId = null;
            SelectLivingPerson(ordinal);
            return true;
        }

        public bool SelectVisualShipment(string shipmentId)
        {
            if (_livingRuntime == null || !_livingRuntime.Shipments.Exists(item =>
                    item.Id == shipmentId)) return false;
            _selectedShipmentId = shipmentId;
            _selectedVisualFacilityId = null;
            _selectedActorOrdinal = null;
            return true;
        }

        public bool SelectBuildBlueprint(string blueprintId)
        {
            if (_visualSystem?.GetBlueprint(blueprintId) == null) return false;
            _selectedBlueprintId = blueprintId;
            return true;
        }

        public bool PrepareOwnedBuildCell()
        {
            if (_livingRuntime == null) return false;
            var person = _livingRuntime.Workforce[(int)_selectedLivingPersonOrdinal];
            var household = _livingRuntime.Households[(int)person.HouseholdOrdinal];
            var property = _livingRuntime.CellProperties.Where(item =>
                    item.OwnerId == household.HouseholdId &&
                    string.IsNullOrEmpty(item.FacilityId))
                .OrderBy(item => item.CellId64).FirstOrDefault();
            if (property == null)
            {
                if (!ExecutePlayerCommand(LuoyangPlayerCommandTypeIds.BuyProperty))
                {
                    _playableMessage = "购买建设用地失败：" + _playerCommandMessage;
                    return false;
                }
                property = _livingRuntime.CellProperties.Where(item =>
                        item.OwnerId == household.HouseholdId &&
                        string.IsNullOrEmpty(item.FacilityId))
                    .OrderBy(item => item.CellId64).FirstOrDefault();
            }
            if (property == null) return false;
            _selectedBuildCellId = property.CellId64;
            _playableMessage = "已选真实 Cell " + property.CellId64 +
                               "；绿色 Ghost 表示建设预览。";
            return true;
        }

        public bool ConfirmBlueprintConstruction(string requesterPrefix = "player.")
        {
            if (_selectedBuildCellId == 0 && !PrepareOwnedBuildCell()) return false;
            var person = _livingRuntime.Workforce[(int)_selectedLivingPersonOrdinal];
            var household = _livingRuntime.Households[(int)person.HouseholdOrdinal];
            try
            {
                var requesterId = requesterPrefix + "person." +
                    _selectedLivingPersonOrdinal;
                var arrivalDay = _visualSystem.OrderMissingConstructionMaterials(
                    _livingRuntime, _selectedBlueprintId, household.HouseholdId,
                    requesterId);
                if (arrivalDay > _livingRuntime.AbsoluteDay)
                    _livingSystem.AdvanceTo(_livingRuntime, arrivalDay);
                var project = _visualSystem.StartFromBlueprint(_livingRuntime,
                    _selectedBlueprintId, _selectedBuildCellId,
                    household.HouseholdId, requesterId);
                _playableMessage = "建设项目已创建：" + project.Id +
                    "；材料、4名永久人物劳工、资金和工期已进入同一世界账。";
                _selectedBuildCellId = 0;
                RefreshGoldenSlice();
                return true;
            }
            catch (InvalidOperationException exception)
            {
                _playableMessage = "无法建设：" + exception.Message;
                return false;
            }
        }

        public void AdvancePlayableDays(int days)
        {
            if (_livingRuntime == null || days <= 0) return;
            _livingSystem.AdvanceTo(_livingRuntime,
                checked(_livingRuntime.AbsoluteDay + days));
            RefreshGoldenSlice();
            _playableMessage = "世界推进 " + days + " 日；地图已从 Runtime 重建。";
        }

        public void CaptureCleanPlayableEvidence(string path)
        {
            if (_goldenSliceBackground == null || _goldenSlice == null)
                throw new InvalidOperationException(
                    "Playable presentation is not ready for evidence export.");
            const int width = 1024;
            const int height = 640;
            var target = RenderTexture.GetTemporary(width, height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                Graphics.Blit(_goldenSliceBackground, target);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                DrawEvidenceProjection(texture);
                texture.Apply(false, false);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                DestroyImmediate(texture);
            }
        }

        private void DrawEvidenceProjection(Texture2D texture)
        {
            foreach (var river in _goldenSlice.RiverSplines)
                DrawEvidenceSpline(texture, river, new Color(.18f, .55f, .72f));
            foreach (var road in _goldenSlice.RoadSplines)
                DrawEvidenceSpline(texture, road, new Color(.68f, .45f, .20f));
            foreach (var anchor in _goldenSlice.FacilityAnchors)
            {
                var facility = _livingRuntime.Facilities.Find(item =>
                    item.FacilityId == anchor.FacilityId);
                var state = LuoyangVisualPresentationRules.ResolveFacilityState(
                    facility, _livingRuntime.ConstructionProjects.FirstOrDefault(item =>
                        item.TargetFacilityId == anchor.FacilityId && !item.Completed));
                var color = StateColor(state);
                var size = anchor.FacilityId == _selectedVisualFacilityId ? 17 : 11;
                DrawEvidenceRect(texture, EvidenceX(texture, anchor.LocalX),
                    EvidenceY(texture, anchor.LocalY), size, color);
            }
            foreach (var actor in _goldenSlice.Actors)
                DrawEvidenceRect(texture, EvidenceX(texture, actor.LocalX),
                    EvidenceY(texture, actor.LocalY), 4,
                    actor.Priority >= 80 ? new Color(.98f, .67f, .18f) :
                    new Color(.90f, .85f, .68f));
            foreach (var shipment in _goldenSlice.Shipments)
                DrawEvidenceRect(texture,
                    EvidenceX(texture, Mathf.Lerp(.05f, .58f,
                        shipment.Progress01)),
                    EvidenceY(texture, Mathf.Lerp(.28f, .46f,
                        shipment.Progress01)), 8, new Color(.62f, .25f, .08f));
            for (var index = 0; index < _goldenSlice.Crops.Count; index++)
            {
                var stage = LuoyangVisualPresentationRules.ResolveCropStage(
                    _goldenSlice.Crops[index]);
                DrawEvidenceRect(texture,
                    EvidenceX(texture, .78f + index % 4 * .045f),
                    EvidenceY(texture, .87f - index / 4 * .055f), 9,
                    CropColor(stage));
            }
            foreach (var project in _livingRuntime.ConstructionProjects.Where(item =>
                         !item.Completed && !item.Cancelled))
            {
                var x = .68f + (project.CellId64 % 17) / 100f;
                var y = .22f + (project.CellId64 % 13) / 100f;
                DrawEvidenceRect(texture, EvidenceX(texture, x),
                    EvidenceY(texture, y), 13, new Color(.20f, .82f, .92f));
            }
            if (_buildMode && _selectedBuildCellId != 0)
                DrawEvidenceRect(texture, EvidenceX(texture, .76f),
                    EvidenceY(texture, .32f), 20, new Color(.22f, .95f, .38f));
        }

        private static void DrawEvidenceSpline(Texture2D texture,
            RuntimeVisualSpline spline, Color color)
        {
            for (var index = 1; index < spline.Points.Count; index++)
                DrawEvidenceLine(texture,
                    EvidenceX(texture, spline.Points[index - 1].X),
                    EvidenceY(texture, spline.Points[index - 1].Y),
                    EvidenceX(texture, spline.Points[index].X),
                    EvidenceY(texture, spline.Points[index].Y), color);
        }

        private static void DrawEvidenceLine(Texture2D texture, int x0, int y0,
            int x1, int y1, Color color)
        {
            var dx = Math.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Math.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;
            while (true)
            {
                DrawEvidenceRect(texture, x0, y0, 3, color);
                if (x0 == x1 && y0 == y1) break;
                var twice = 2 * error;
                if (twice >= dy) { error += dy; x0 += sx; }
                if (twice <= dx) { error += dx; y0 += sy; }
            }
        }

        private static void DrawEvidenceRect(Texture2D texture, int centerX,
            int centerY, int size, Color color)
        {
            var half = Math.Max(1, size / 2);
            for (var y = Math.Max(0, centerY - half);
                 y < Math.Min(texture.height, centerY + half); y++)
                for (var x = Math.Max(0, centerX - half);
                     x < Math.Min(texture.width, centerX + half); x++)
                    texture.SetPixel(x, y, color);
        }

        private static int EvidenceX(Texture2D texture, float local) =>
            Mathf.Clamp(Mathf.RoundToInt(local * (texture.width - 1)),
                0, texture.width - 1);

        private static int EvidenceY(Texture2D texture, float local) =>
            Mathf.Clamp(Mathf.RoundToInt(local * (texture.height - 1)),
                0, texture.height - 1);

        private void DrawPlayablePresentation()
        {
            var screen = new Rect(0, 0, Screen.width, Screen.height);
            GUI.color = new Color(.10f, .075f, .045f, 1f);
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var header = new Rect(18, 14, Screen.width - 36, 56);
            DrawPanel(header, new Color(.15f, .09f, .045f, .93f));
            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            title.normal.textColor = new Color(.96f, .85f, .58f);
            GUI.Label(new Rect(32, 17, 560, 30),
                "群雄志：仕途 · 184 洛阳 Golden Slice", title);
            GUI.Label(new Rect(32, 45, 750, 20),
                "同一世界：400,000人物 · 80,899家户 · 2,084开局设施 · Runtime Day " +
                _livingRuntime.AbsoluteDay);
            DrawHeaderButtons();

            var map = new Rect(18, 80, Screen.width * .73f,
                Screen.height - 150);
            DrawGoldenSliceMap(map);
            var panel = new Rect(map.xMax + 12, 80,
                Screen.width - map.xMax - 30, map.height);
            DrawInspectorPanel(panel);
            DrawBottomBar(new Rect(18, map.yMax + 10,
                Screen.width - 36, 48));
        }

        private void DrawHeaderButtons()
        {
            var x = Screen.width - 660f;
            if (GUI.Button(new Rect(x, 27, 100, 28),
                    _buildMode ? "退出建设" : "建设模式"))
                SetBuildMode(!_buildMode);
            if (GUI.Button(new Rect(x + 106, 27, 92, 28), "世界视角"))
                SetVisualLod(MapVisualLod.World);
            if (GUI.Button(new Rect(x + 204, 27, 92, 28), "区域视角"))
                SetVisualLod(MapVisualLod.Region);
            if (GUI.Button(new Rect(x + 302, 27, 92, 28), "城市视角"))
                SetVisualLod(MapVisualLod.City);
            if (GUI.Button(new Rect(x + 400, 27, 92, 28), "街区近景"))
                SetVisualLod(MapVisualLod.Close);
            if (GUI.Button(new Rect(x + 498, 27, 136, 28), "数据验证视图"))
                SetPlayablePresentation(false);
        }

        private void DrawGoldenSliceMap(Rect map)
        {
            GUI.BeginGroup(map);
            var local = new Rect(0, 0, map.width, map.height);
            if (_goldenSliceBackground != null)
                GUI.DrawTexture(local, _goldenSliceBackground,
                    ScaleMode.ScaleAndCrop, true);
            else
            {
                GUI.color = new Color(.58f, .49f, .31f);
                GUI.DrawTexture(local, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.color = new Color(.13f, .08f, .04f, .18f);
            GUI.DrawTexture(local, Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (_buildMode || _debugGrid) DrawBuildGrid(local);
            DrawRuntimeSplines(local);
            DrawFacilityMarkers(local);
            DrawCropMarkers(local);
            DrawShipmentMarkers(local);
            DrawActorMarkers(local);
            GUI.EndGroup();
        }

        private void DrawRuntimeSplines(Rect map)
        {
            if (_goldenSlice == null) return;
            foreach (var river in _goldenSlice.RiverSplines)
                DrawSpline(map, river, new Color(.27f, .53f, .65f, .58f));
            foreach (var road in _goldenSlice.RoadSplines)
                DrawSpline(map, road, new Color(.68f, .49f, .25f, .62f));
        }

        private static void DrawSpline(Rect map, RuntimeVisualSpline spline,
            Color color)
        {
            if (spline?.Points == null || spline.Points.Count < 2) return;
            var previous = GUI.color;
            GUI.color = color;
            for (var index = 1; index < spline.Points.Count; index++)
            {
                var a = spline.Points[index - 1];
                var b = spline.Points[index];
                var x1 = a.X * map.width;
                var y1 = (1f - a.Y) * map.height;
                var x2 = b.X * map.width;
                var y2 = (1f - b.Y) * map.height;
                var length = Vector2.Distance(new Vector2(x1, y1),
                    new Vector2(x2, y2));
                var angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;
                var matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
                GUI.DrawTexture(new Rect(x1, y1 - spline.Width * map.height / 2f,
                    length, spline.Width * map.height), Texture2D.whiteTexture);
                GUI.matrix = matrix;
            }
            GUI.color = previous;
        }

        private void DrawBuildGrid(Rect map)
        {
            var previous = GUI.color;
            GUI.color = new Color(.85f, .73f, .32f, .34f);
            for (var x = 0; x < 12; x++)
                GUI.DrawTexture(new Rect(x * map.width / 12f, 0, 1, map.height),
                    Texture2D.whiteTexture);
            for (var y = 0; y < 8; y++)
                GUI.DrawTexture(new Rect(0, y * map.height / 8f, map.width, 1),
                    Texture2D.whiteTexture);
            if (_selectedBuildCellId != 0)
            {
                GUI.color = new Color(.25f, .95f, .38f, .46f);
                GUI.DrawTexture(new Rect(map.width * .73f, map.height * .64f,
                    map.width / 12f, map.height / 8f), Texture2D.whiteTexture);
                GUI.Label(new Rect(map.width * .73f, map.height * .64f,
                    140, 24), "BUILD GHOST");
            }
            GUI.color = previous;
        }

        private void DrawFacilityMarkers(Rect map)
        {
            if (_goldenSlice == null) return;
            foreach (var anchor in _goldenSlice.FacilityAnchors)
            {
                var profile = _visualSystem.Profiles.First(item =>
                    item.VisualProfileId == anchor.VisualProfileId);
                var size = profile.Importance == FacilityVisualImportance.A
                    ? 38f : profile.Importance == FacilityVisualImportance.B ? 30f : 24f;
                var rect = new Rect(anchor.LocalX * map.width - size * .5f,
                    (1f - anchor.LocalY) * map.height - size * .5f, size, size);
                var facility = _livingRuntime.Facilities.Find(item =>
                    item.FacilityId == anchor.FacilityId);
                var state = LuoyangVisualPresentationRules.ResolveFacilityState(
                    facility, _livingRuntime.ConstructionProjects.FirstOrDefault(item =>
                        item.TargetFacilityId == anchor.FacilityId && !item.Completed));
                GUI.backgroundColor = StateColor(state);
                var label = profile.MainAssetId.Replace("HAN_", string.Empty)
                    .Replace("_A", string.Empty).Substring(0,
                        Math.Min(3, profile.MainAssetId.Replace("HAN_", string.Empty)
                            .Replace("_A", string.Empty).Length));
                if (GUI.Button(rect, label)) SelectVisualFacility(anchor.FacilityId);
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawActorMarkers(Rect map)
        {
            if (_goldenSlice == null || _visualLod < MapVisualLod.City) return;
            foreach (var actor in _goldenSlice.Actors)
            {
                var rect = new Rect(actor.LocalX * map.width - 5,
                    (1f - actor.LocalY) * map.height - 5, 11, 11);
                GUI.backgroundColor = actor.Priority >= 80
                    ? new Color(.95f, .65f, .18f) : new Color(.84f, .80f, .64f);
                if (GUI.Button(rect, string.Empty)) SelectVisualActor(actor.PersonOrdinal);
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawShipmentMarkers(Rect map)
        {
            if (_goldenSlice == null) return;
            foreach (var shipment in _goldenSlice.Shipments)
            {
                var x = Mathf.Lerp(.05f, .58f, shipment.Progress01) * map.width;
                var y = Mathf.Lerp(.72f, .54f, shipment.Progress01) * map.height;
                GUI.backgroundColor = new Color(.68f, .34f, .12f);
                if (GUI.Button(new Rect(x, y, 42, 21), "车队"))
                    SelectVisualShipment(shipment.ShipmentId);
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawCropMarkers(Rect map)
        {
            if (_goldenSlice == null || _visualLod < MapVisualLod.City) return;
            for (var i = 0; i < _goldenSlice.Crops.Count; i++)
            {
                var crop = _goldenSlice.Crops[i];
                var stage = LuoyangVisualPresentationRules.ResolveCropStage(crop);
                GUI.backgroundColor = CropColor(stage);
                var x = map.width * (.78f + (i % 4) * .045f);
                var y = map.height * (.13f + (i / 4) * .055f);
                GUI.Button(new Rect(x, y, 25, 18), stage ==
                    CropVisualStage.Harvestable80 ? "80%" : "田");
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawInspectorPanel(Rect panel)
        {
            DrawPanel(panel, new Color(.13f, .09f, .055f, .94f));
            GUILayout.BeginArea(new Rect(panel.x + 12, panel.y + 10,
                panel.width - 24, panel.height - 20));
            var heading = new GUIStyle(GUI.skin.label)
                { fontSize = 18, fontStyle = FontStyle.Bold };
            heading.normal.textColor = new Color(.96f, .80f, .48f);
            GUILayout.Label(_buildMode ? "建设与产业" : "世界实体", heading);
            GUILayout.Label(_playableMessage, GUILayout.Height(42));
            if (_buildMode) DrawBuildInspector();
            else DrawEntityInspector();
            GUILayout.EndArea();
        }

        private void DrawBuildInspector()
        {
            GUILayout.Label("正式 BuildBlueprint（历史/AI/玩家复用）");
            foreach (var blueprint in _visualSystem.Blueprints.Where(item =>
                         (item.Availability & BuildAvailability.Player) != 0))
            {
                GUI.backgroundColor = blueprint.BlueprintId == _selectedBlueprintId
                    ? new Color(.75f, .55f, .20f) : Color.white;
                if (GUILayout.Button(blueprint.FacilityDefinitionId,
                        GUILayout.Height(27)))
                    SelectBuildBlueprint(blueprint.BlueprintId);
            }
            GUI.backgroundColor = Color.white;
            var selected = _visualSystem.GetBlueprint(_selectedBlueprintId);
            if (selected != null)
                GUILayout.Label("资金 " + selected.RequiredMoney + " · 工人 " +
                    selected.RequiredWorkers + " · 工期 " +
                    selected.ConstructionDays + "日\n材料：" +
                    string.Join("、", selected.RequiredMaterials.Select(item =>
                        item.ProductId + " " + item.QuantityMilliunits)));
            if (GUILayout.Button("1. 购买/选择合法 Cell", GUILayout.Height(32)))
                PrepareOwnedBuildCell();
            if (GUILayout.Button("2. 确认建设", GUILayout.Height(34)))
                ConfirmBlueprintConstruction();
            GUILayout.Space(8);
            GUILayout.Label("进行中工程");
            foreach (var project in _livingRuntime.ConstructionProjects.Where(item =>
                         !item.Completed && !item.Cancelled).Take(6))
                GUILayout.Label(project.FacilityDefinitionId + " · " +
                    LuoyangVisualPresentationRules.ResolveConstructionStage(
                        project, _livingRuntime.AbsoluteDay) + " · Day " +
                    project.CompletionDay);
        }

        private void DrawEntityInspector()
        {
            if (!string.IsNullOrEmpty(_selectedVisualFacilityId))
            {
                var facility = _livingRuntime.Facilities.Find(item =>
                    item.FacilityId == _selectedVisualFacilityId);
                var profile = _visualSystem.ResolveProfile(facility.DefinitionId);
                var inventories = _livingRuntime.Inventories.Where(item =>
                    item.FacilityId == facility.FacilityId).ToArray();
                GUILayout.Label("设施：" + facility.FacilityId);
                GUILayout.Label("类型：" + facility.DefinitionId +
                    "\nCell：" + facility.CellId64 + "\n所有者：" +
                    facility.OwnerId + "\n工人：" + facility.AssignedWorkers +
                    "/" + facility.OptimalWorkers + "\n生产：" +
                    facility.Status + "\n耐久：" + facility.ConditionBasisPoints +
                    "\n库存：" + inventories.Sum(item => item.QuantityMilliunits) +
                    "\nVisualProfile：" + profile.VisualProfileId);
            }
            else if (_selectedActorOrdinal.HasValue)
                GUILayout.Label(_livingDebugText);
            else if (!string.IsNullOrEmpty(_selectedShipmentId))
            {
                var shipment = _livingRuntime.Shipments.Find(item =>
                    item.Id == _selectedShipmentId);
                GUILayout.Label("真实 Shipment：" + shipment.Id +
                    "\nRoute：" + shipment.RouteId + "\n货物：" +
                    shipment.ProductId + "\n发运量：" +
                    shipment.ShippedQuantityMilliunits + "\n自然损耗：" +
                    shipment.NaturalLossMilliunits + "\n风险损耗：" +
                    shipment.RiskLossMilliunits + "\n到达日：" +
                    shipment.ArrivalDay);
            }
            else
                GUILayout.Label("点击地图上的设施缩写、人物圆点、车队或农田。\n\n" +
                    "普通人物和设施来自40万永久人口及2,084项开局Facility；" +
                    "图标销毁不会删除世界人物。" +
                    "\n\n正常视图不显示 Cell 网格。切换建设模式才显示。" );
        }

        private void DrawBottomBar(Rect rect)
        {
            DrawPanel(rect, new Color(.15f, .09f, .045f, .94f));
            if (GUI.Button(new Rect(rect.x + 12, rect.y + 10, 90, 28), "+1日"))
                AdvancePlayableDays(1);
            if (GUI.Button(new Rect(rect.x + 108, rect.y + 10, 90, 28), "+10日"))
                AdvancePlayableDays(10);
            if (GUI.Button(new Rect(rect.x + 204, rect.y + 10, 100, 28), "工作"))
                ExecutePlayerCommand(LuoyangPlayerCommandTypeIds.SeekWork);
            if (GUI.Button(new Rect(rect.x + 310, rect.y + 10, 100, 28), "市场交易"))
                ExecutePlayerCommand(LuoyangPlayerCommandTypeIds.Trade);
            GUI.Label(new Rect(rect.x + 430, rect.y + 13, rect.width - 445, 24),
                "视角 " + _visualLod + " · Actor " +
                (_goldenSlice?.Actors.Count ?? 0) + " · Shipment " +
                (_goldenSlice?.Shipments.Count ?? 0) + " · 正常网格 " +
                (NormalModeHidesCellGrid ? "隐藏" : "显示"));
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Color StateColor(FacilityRuntimeVisualState state)
        {
            switch (state)
            {
                case FacilityRuntimeVisualState.Working: return new Color(.30f, .72f, .34f);
                case FacilityRuntimeVisualState.WaitingInput: return new Color(.88f, .63f, .18f);
                case FacilityRuntimeVisualState.Damaged: return new Color(.78f, .30f, .18f);
                case FacilityRuntimeVisualState.Destroyed:
                case FacilityRuntimeVisualState.Abandoned: return new Color(.28f, .25f, .23f);
                case FacilityRuntimeVisualState.UnderConstruction: return new Color(.22f, .78f, .88f);
                default: return new Color(.78f, .62f, .36f);
            }
        }

        private static Color CropColor(CropVisualStage stage)
        {
            switch (stage)
            {
                case CropVisualStage.Seedling: return new Color(.43f, .72f, .28f);
                case CropVisualStage.Growing: return new Color(.34f, .62f, .20f);
                case CropVisualStage.Harvestable80: return new Color(.90f, .73f, .24f);
                case CropVisualStage.Mature: return new Color(.82f, .59f, .14f);
                case CropVisualStage.Harvested: return new Color(.48f, .37f, .22f);
                case CropVisualStage.Fallow: return new Color(.34f, .27f, .21f);
                default: return new Color(.58f, .74f, .32f);
            }
        }
    }
}
