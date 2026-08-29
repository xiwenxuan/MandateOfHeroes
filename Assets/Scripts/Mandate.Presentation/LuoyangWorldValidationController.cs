using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;

namespace Mandate.Presentation
{
    public enum LuoyangMapOverlay
    {
        Terrain,
        PopulationDensity,
        HousingPressure,
        Jobs,
        HistoricalConfidence,
        Fortifications,
        Agriculture,
        Residential,
        Industry,
        Commercial,
        Public,
        Road,
        Military,
        UnusedDevelopable
    }

    public sealed partial class LuoyangWorldValidationController : MonoBehaviour
    {
        private const int TextureWidth = 512;
        private const int TextureHeight = 320;
        private WorldMapDataReader _worldReader;
        private Luoyang184HistoricalPrototypeReader _prototypeReader;
        private LuoyangPopulationStressPrototypeReader _stressReader;
        private Texture2D _mapTexture;
        private Color32[] _pixels;
        private float _centerRow;
        private float _centerColumn;
        private float _cellsPerPixel = 0.20f;
        private Rect _mapRect;
        private Luoyang184CellRecord _selectedCell;
        private WorldState _integratedWorld;
        private string _historicalPersonDebugText = string.Empty;
        private Luoyang184LivingWorldRuntimeState _livingRuntime;
        private Luoyang184LivingWorldSystem _livingSystem;
        private string _livingDebugText = string.Empty;
        private uint _selectedLivingPersonOrdinal;
        private string _playerCommandMessage = string.Empty;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }
        public LuoyangMapOverlay Overlay { get; private set; }
        public Luoyang184CellRecord SelectedCell => _selectedCell;
        public Luoyang184HistoricalPrototype Prototype => _prototypeReader?.World;
        public LuoyangStressProfileSummary StressProfile { get; private set; }
        public bool UsesAdaptiveConstruction { get; private set; } = true;
        public LuoyangStressModeSummary StressMode => StressProfile == null
            ? null
            : UsesAdaptiveConstruction ? StressProfile.AdaptiveMode : StressProfile.FixedMode;
        public int MaximumVisualActorCount => StressProfile?.Lod?.MaximumVisualActorCount ?? 0;
        public float CellsPerPixel => _cellsPerPixel;
        public WorldState IntegratedWorld => _integratedWorld;
        public string HistoricalPersonDebugText => _historicalPersonDebugText;
        public Luoyang184LivingWorldRuntimeState LivingRuntime => _livingRuntime;
        public string LivingDebugText => _livingDebugText;

        private void Start() => TryInitialize();

        public bool TryInitialize(string worldRoot = null, string prototypeRoot = null, string stressRoot = null)
        {
            if (IsReady) return true;
            try
            {
                worldRoot ??= Path.Combine(Application.streamingAssetsPath, "WorldMap", "HanWorldV1");
                prototypeRoot ??= Path.Combine(Application.streamingAssetsPath, "WorldMap", "Luoyang184HistoricalV1");
                stressRoot ??= Path.Combine(Application.streamingAssetsPath, "WorldMap", "LuoyangPopulationStressV1");
                _worldReader = new WorldMapDataReader(worldRoot);
                _prototypeReader = new Luoyang184HistoricalPrototypeReader(prototypeRoot);
                _stressReader = new LuoyangPopulationStressPrototypeReader(stressRoot);
                var metropolitanRoot = Path.Combine(Application.streamingAssetsPath,
                    "WorldMap", "Luoyang184MetropolitanInitializationV1");
                var historicalPersonRoot = Path.Combine(
                    Application.streamingAssetsPath, "HistoricalPersons", "Han135260V1");
                _integratedWorld = WorldState.Create(184);
                new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                    metropolitanRoot, historicalPersonRoot).Integrate(_integratedWorld);
                var remediationRoot = Path.Combine(
                    Application.streamingAssetsPath, "WorldMap",
                    "LuoyangOuterSupplyRemediationV1");
                var remediation =
                    new Luoyang184OuterSupplyRemediationBootstrap(
                        remediationRoot);
                remediation.Integrate(_integratedWorld);
                var livingSource = remediation.Source;
                _livingSystem = new Luoyang184LivingWorldSystem(livingSource);
                _livingRuntime = _livingSystem.CreateRuntime(184UL);
                _livingSystem.AdvanceTo(_livingRuntime, 1);
                _livingSystem.AttachSummary(_integratedWorld, _livingRuntime);
                SelectLivingPerson(0);
                InitializePlayablePresentation();
                SelectHistoricalPerson("P0038");
                if (_worldReader.Manifest.GridSchemaVersion != _prototypeReader.World.GridSchemaVersion)
                    throw new InvalidDataException("HanWorld and Luoyang 184 GridSchemaVersion differ.");
                _mapTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "Luoyang 184 historical unified-world validation"
                };
                _pixels = new Color32[TextureWidth * TextureHeight];
                Overlay = LuoyangMapOverlay.HistoricalConfidence;
                LocateLuoyang();
                SelectFacility("facility.instance.luoyang.184.north_palace");
                if (!SelectStressProfile("Profile_020542_HistoricalBaseline"))
                    throw new InvalidDataException("Luoyang stress package has no protected historical baseline.");
                IsReady = true;
                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                Debug.LogError("Luoyang 184 validation failed to initialize: " + LastError);
                return false;
            }
        }

        public bool SelectStressProfile(string profileId)
        {
            if (_stressReader == null || !_stressReader.TryGetProfile(profileId, out var profile)) return false;
            if (profile.HistoricalScenarioPopulation != Prototype.PopulationProfile.TotalPersons ||
                profile.Lod.PermanentPersonCount != profile.PersonCount ||
                profile.Lod.HighFrequencyActorCount > profile.Lod.MaximumVisualActorCount)
                throw new InvalidDataException("Luoyang stress profile violates permanent Person or visual LOD invariants.");
            StressProfile = profile;
            return true;
        }

        public void SetConstructionMode(bool adaptive)
        {
            UsesAdaptiveConstruction = adaptive;
        }

        public void SetOverlay(LuoyangMapOverlay overlay)
        {
            Overlay = overlay;
            if (_mapTexture != null) RenderMap();
        }

        public void LocateLuoyang()
        {
            if (_prototypeReader == null) return;
            var world = _prototypeReader.World;
            _centerRow = world.CityAnchorCellId64 / (float)world.Columns;
            _centerColumn = world.CityAnchorCellId64 % (ulong)world.Columns;
            _cellsPerPixel = 0.10f;
            RenderMap();
        }

        public void LocateLuoyangHulaoCorridor()
        {
            if (_prototypeReader == null) return;
            var world = _prototypeReader.World;
            var cityRow = world.CityAnchorCellId64 / (ulong)world.Columns;
            var cityColumn = world.CityAnchorCellId64 % (ulong)world.Columns;
            var hulaoRow = world.HulaoCellId64 / (ulong)world.Columns;
            var hulaoColumn = world.HulaoCellId64 % (ulong)world.Columns;
            _centerRow = ((float)cityRow + hulaoRow) * 0.5f;
            _centerColumn = ((float)cityColumn + hulaoColumn) * 0.5f;
            _cellsPerPixel = Mathf.Clamp(Mathf.Max(Mathf.Abs((float)cityRow - hulaoRow) / 180f,
                Mathf.Abs((float)cityColumn - hulaoColumn) / 300f), 0.12f, 2f);
            RenderMap();
        }

        public void Zoom(float factor)
        {
            _cellsPerPixel = Mathf.Clamp(_cellsPerPixel * factor, 0.04f, 8f);
            RenderMap();
        }

        public bool SelectCell(ulong cellId64)
        {
            if (_prototypeReader != null && _prototypeReader.TryGetCell(cellId64, out var cell))
            {
                _selectedCell = cell;
                return true;
            }
            return false;
        }

        public bool SelectFacility(string facilityId)
        {
            if (_prototypeReader != null && _prototypeReader.TryGetFacility(facilityId, out var facility))
                return SelectCell(facility.CellId64);
            return false;
        }

        public bool SelectHistoricalPerson(string personId)
        {
            if (_integratedWorld == null) return false;
            var index = new HistoricalPersonFamilyRuntimeIndex(_integratedWorld);
            if (!index.TryGetIdentity(personId, out var identity)) return false;
            var organization = _integratedWorld.FamilyOrganizationMembers
                .FirstOrDefault(item => item.PersonId == personId)?.OrganizationId ??
                "none";
            var office = _integratedWorld.CivilMilitaryOfficeAssignments
                .FirstOrDefault(item => item.HolderPersonId == personId);
            var center = organization == "none"
                ? null
                : _integratedWorld.FamilyCenters.FirstOrDefault(item =>
                    item.OrganizationId == organization);
            _historicalPersonDebugText =
                $"Historical Person: {identity.CanonicalName} ({identity.PersonId})\n" +
                $"Clan / Branch: {identity.ClanId ?? "none"} / {identity.BranchId ?? "none"}\n" +
                $"Household: {identity.HouseholdId}\nResidence: {identity.ResidenceFacilityId}\n" +
                $"FamilyOrganization: {organization}\n" +
                $"Office / Activity: {office?.OfficeDefinitionId ?? "none"} / " +
                $"{_integratedWorld.PersonPrimaryActivities.First(item => item.PersonId == personId).ActivityId}\n" +
                $"FamilyCenter: {center?.Status.ToString() ?? "none"} / " +
                $"{center?.Designation.ToString() ?? "none"}";
            return true;
        }

        public bool SelectLivingPerson(uint ordinal)
        {
            if (_livingRuntime == null || ordinal >= _livingRuntime.Workforce.Count)
                return false;
            var person = _livingRuntime.Workforce[(int)ordinal];
            _selectedLivingPersonOrdinal = ordinal;
            var household = _livingRuntime.Households[(int)person.HouseholdOrdinal];
            var development = _livingRuntime.PersonDevelopment.FirstOrDefault(item =>
                item.PersonOrdinal == ordinal);
            var office = _livingRuntime.Offices.FirstOrDefault(item =>
                item.HolderPersonOrdinal == ordinal);
            _livingDebugText =
                $"Living Person #{person.PersonOrdinal + 1:N0}\n" +
                $"Household #{person.HouseholdOrdinal + 1:N0}; facility index {person.FacilityIndex}\n" +
                $"Role/activity {person.SocialRoleId}/{person.CurrentActivityId}; work {person.Status}; age {person.Age}\n" +
                $"Residence {development?.ResidenceFacilityId ?? household.ResidenceFacilityIndex.ToString()}; money {household.Wealth:N0}; reserve {household.FoodReserveMilliunits:N0}\n" +
                $"Office {office?.OfficeKindId ?? "none"}; knowledge/skill {development?.KnowledgeBasisPoints ?? 0}/{development?.SkillBasisPoints ?? 0}\n" +
                $"Food demand/consumed {person.CumulativeFoodDemandMilliunits:N0}/{person.CumulativeFoodConsumedMilliunits:N0} milliunits";
            return true;
        }

        public bool ExecutePlayerCommand(string commandTypeId, string targetId = null)
        {
            if (_livingRuntime == null) return false;
            var result = new Luoyang184PlayerCommandSystem().Execute(
                _livingRuntime, _selectedLivingPersonOrdinal, commandTypeId,
                targetId);
            _playerCommandMessage = result.StatusId + ": " + result.ResultId;
            SelectLivingPerson(_selectedLivingPersonOrdinal);
            return result.StatusId == "completed";
        }

        public bool SelectLivingHousehold(uint ordinal)
        {
            if (_livingRuntime == null || ordinal >= _livingRuntime.Households.Count)
                return false;
            var household = _livingRuntime.Households[(int)ordinal];
            _livingDebugText =
                $"Household #{household.HouseholdOrdinal + 1:N0}; members {household.MemberCount}\n" +
                $"Daily demand {household.DailyFoodDemandMilliunits:N0}; consumed {household.CumulativeFoodConsumedMilliunits:N0}; shortage {household.CumulativeFoodShortageMilliunits:N0}\n" +
                $"Acquisition {household.LastAcquisitionSourceId}; AI {household.AiResponseActionId}";
            return true;
        }

        public bool SelectLivingFacility(int index)
        {
            if (_livingRuntime == null || index < 0 || index >= _livingRuntime.Facilities.Count)
                return false;
            var facility = _livingRuntime.Facilities[index];
            _livingDebugText =
                $"Facility {facility.FacilityId}\n" +
                $"Recipe {facility.RecipeId}; status {facility.Status}; stop {facility.StopReasonId}\n" +
                $"Workers {facility.AssignedWorkers}/{facility.MinimumWorkers}/{facility.OptimalWorkers}; progress {facility.ProductionProgressBasisPoints}/10000\n" +
                $"Input {facility.InputProductId} {facility.InputQuantity:N0}; output {facility.OutputProductId} {facility.OutputQuantity:N0}; AI {facility.AiResponseActionId}";
            return true;
        }

        public bool DemonstrateGateState(string facilityId, string gateState)
        {
            if (gateState != "Closed" && gateState != "Open" && gateState != "Destroyed") return false;
            if (!_prototypeReader.TryGetFacility(facilityId, out var facility) ||
                facility.DefinitionId.IndexOf("gate", StringComparison.Ordinal) < 0) return false;
            if (!_prototypeReader.TryGetCell(facility.CellId64, out var cell)) return false;
            cell.GateState = gateState;
            RenderMap();
            return true;
        }

        public bool DemonstrateWallBreach(string facilityId)
        {
            if (!_prototypeReader.TryGetFacility(facilityId, out var facility) ||
                facility.DefinitionId.IndexOf("wall", StringComparison.Ordinal) < 0) return false;
            if (!_prototypeReader.TryGetCell(facility.CellId64, out var cell)) return false;
            cell.WallState = "Breached";
            RenderMap();
            return true;
        }

        private void OnDestroy()
        {
            _worldReader?.Dispose();
            if (_mapTexture != null) Destroy(_mapTexture);
            DisposePlayablePresentation();
        }

        private void OnGUI()
        {
            GUI.color = new Color(0.93f, 0.89f, 0.78f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.18f, 0.10f, 0.05f);
            GUI.Label(new Rect(18, 8, Screen.width - 36, 30), "LUOYANG-184 | Historical world + permanent-population stress prototype", title);
            if (!IsReady)
            {
                GUI.Label(new Rect(18, 45, Screen.width - 36, Screen.height - 60), LastError ?? "Loading...");
                return;
            }

            if (_usePlayablePresentation)
            {
                DrawPlayablePresentation();
                return;
            }

            var profile = Prototype.PopulationProfile;
            var stress = StressMode;
            GUI.Label(new Rect(18, 38, Screen.width - 36, 24),
                $"{Prototype.ScenarioYear} / {Prototype.ScenarioPolityId} | {Prototype.CellSizeMetres}m abstract Cell | " +
                $"Profile Persons {StressProfile.PersonCount:N0} | { (UsesAdaptiveConstruction ? "Adaptive" : "Fixed") } | " +
                $"Facilities {stress.FacilityCount:N0} (+{stress.FacilitiesAdded:N0}) | Housed {stress.HousedPopulation:N0} / Unhoused {stress.UnhousedPopulation:N0} | " +
                $"LOD Actors {StressProfile.Lod.HighFrequencyActorCount:N0}/{StressProfile.Lod.PermanentPersonCount:N0}");

            var mapWidth = Mathf.Min(Screen.width - 390, 1024);
            var mapHeight = Mathf.Min(Screen.height - 165, 700);
            _mapRect = new Rect(18, 68, Mathf.Max(420, mapWidth), Mathf.Max(260, mapHeight));
            GUI.DrawTexture(_mapRect, _mapTexture, ScaleMode.StretchToFill, false);
            HandleMapInput(Event.current);

            var panelX = _mapRect.xMax + 10;
            var panelWidth = Screen.width - panelX - 12;
            DrawOverlayButtons(panelX, 68, panelWidth);
            DrawLandmarkButtons(panelX, 252, panelWidth);
            DrawSelection(panelX, 330, panelWidth);

            var y = _mapRect.yMax + 7;
            if (GUI.Button(new Rect(18, y, 96, 27), "Locate Luoyang")) LocateLuoyang();
            if (GUI.Button(new Rect(120, y, 140, 27), "Luoyang-Hulao")) LocateLuoyangHulaoCorridor();
            if (GUI.Button(new Rect(266, y, 74, 27), "Zoom +")) Zoom(0.75f);
            if (GUI.Button(new Rect(346, y, 74, 27), "Zoom -")) Zoom(1.33f);
            GUI.Label(new Rect(430, y + 3, 440, 24), $"Continuous scale: {_cellsPerPixel:F2} Cell/pixel; wheel/click enabled");
            DrawStressControls(18, y + 31);
            DrawPlayerCommands(18, y + 59);
        }

        private void DrawPlayerCommands(float x, float y)
        {
            GUI.Label(new Rect(x, y, 150, 22),
                $"Player Person #{_selectedLivingPersonOrdinal + 1:N0}");
            var commands = new[]
            {
                new[] { "Find work", LuoyangPlayerCommandTypeIds.SeekWork },
                new[] { "Study", LuoyangPlayerCommandTypeIds.Study },
                new[] { "Market trade", LuoyangPlayerCommandTypeIds.Trade },
                new[] { "Buy Cell", LuoyangPlayerCommandTypeIds.BuyProperty },
                new[] { "Expand", LuoyangPlayerCommandTypeIds.ExpandIndustry },
                new[] { "Build", LuoyangPlayerCommandTypeIds.BuildIndustry },
                new[] { "Accept office", LuoyangPlayerCommandTypeIds.AcceptOffice },
                new[] { "Enlist", LuoyangPlayerCommandTypeIds.Enlist }
            };
            for (var i = 0; i < commands.Length; i++)
                if (GUI.Button(new Rect(x + 150 + i * 91, y, 87, 22), commands[i][0]))
                    ExecutePlayerCommand(commands[i][1]);
            GUI.Label(new Rect(x, y + 24, 900, 22),
                "Command result: " + (_playerCommandMessage ?? string.Empty));
        }

        private void DrawStressControls(float x, float y)
        {
            var profiles = new[]
            {
                new[] { "20K", "Profile_020542_HistoricalBaseline" },
                new[] { "50K", "Profile_050000_Stress" },
                new[] { "100K", "Profile_100000_Stress" },
                new[] { "250K", "Profile_250000_Stress" },
                new[] { "500K", "Profile_500000_Stress" }
            };
            for (var index = 0; index < profiles.Length; index++)
                if (GUI.Button(new Rect(x + index * 62, y, 58, 24), profiles[index][0]))
                    SelectStressProfile(profiles[index][1]);
            if (GUI.Button(new Rect(x + 318, y, 106, 24), UsesAdaptiveConstruction ? "Mode: Adaptive" : "Mode: Fixed"))
                SetConstructionMode(!UsesAdaptiveConstruction);
            if (StressMode != null)
                GUI.Label(new Rect(x + 432, y + 2, 620, 22),
                    $"Cell {StressMode.OccupiedFacilityCells:N0}/{_stressReader.Manifest.DevelopableCells:N0}; " +
                    $"Jobs {StressMode.EmployedWorkers:N0}/{StressMode.TotalJobs:N0}; visual pool <= {MaximumVisualActorCount:N0}");
        }

        private void DrawOverlayButtons(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 22), "Information perspective (known world facts)");
            var values = (LuoyangMapOverlay[])Enum.GetValues(typeof(LuoyangMapOverlay));
            for (var index = 0; index < values.Length; index++)
            {
                var overlay = values[index];
                var buttonWidth = Mathf.Max(105, (width - 6) / 2);
                var bx = x + index % 2 * (buttonWidth + 4);
                var by = y + 24 + index / 2 * 23;
                var previous = GUI.backgroundColor;
                if (overlay == Overlay) GUI.backgroundColor = new Color(0.70f, 0.50f, 0.22f);
                if (GUI.Button(new Rect(bx, by, buttonWidth, 21), overlay.ToString())) SetOverlay(overlay);
                GUI.backgroundColor = previous;
            }
        }

        private void DrawLandmarkButtons(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 20), "Historical labels / fortification demo");
            var landmarks = new[]
            {
                new[] { "North Palace", "facility.instance.luoyang.184.north_palace" },
                new[] { "South Palace", "facility.instance.luoyang.184.south_palace" },
                new[] { "Taixue", "facility.instance.luoyang.184.taixue" },
                new[] { "Pingcheng Gate", "facility.instance.luoyang.184.gate.pingchengmen" },
            };
            var bw = Mathf.Max(82, (width - 9) / 4);
            for (var index = 0; index < landmarks.Length; index++)
                if (GUI.Button(new Rect(x + index * (bw + 3), y + 21, bw, 22), landmarks[index][0]))
                    SelectFacility(landmarks[index][1]);
            if (GUI.Button(new Rect(x, y + 47, bw, 22), "Emperor Liu Hong"))
                SelectHistoricalPerson("P0038");
            if (GUI.Button(new Rect(x + bw + 3, y + 47, bw, 22), "He Jin"))
                SelectHistoricalPerson("P0035");
            if (GUI.Button(new Rect(x + (bw + 3) * 2, y + 47, bw, 22), "Cao Cao"))
                SelectHistoricalPerson("P0108");
            if (_livingRuntime != null)
            {
                if (GUI.Button(new Rect(x, y + 72, bw, 22), "Person #1"))
                    SelectLivingPerson(0);
                if (GUI.Button(new Rect(x + bw + 3, y + 72, bw, 22), "Household #1"))
                    SelectLivingHousehold(0);
                if (GUI.Button(new Rect(x + (bw + 3) * 2, y + 72, bw, 22), "Facility #1"))
                    SelectLivingFacility(0);
            }
        }

        private void DrawSelection(float x, float y, float width)
        {
            if (_selectedCell == null) return;
            var cell = _selectedCell;
            var warning = cell.RequiredWorkers > cell.CurrentWorkers ? "LABOUR SHORTAGE / no normal operation" :
                cell.ResidentCapacityPersons > 0 && cell.Population > cell.ResidentCapacityPersons ? "HOUSING OVERLOAD" : "none";
            GUI.TextArea(new Rect(x, y, width, Mathf.Max(170, Screen.height - y - 14)),
                $"CellId64: {cell.CellId64}\nGridX/Y: {cell.GridX}, {cell.GridY}\n" +
                $"Terrain/Slope/Elevation/Water: {cell.TerrainClass}/{cell.SlopeClass}/{cell.Elevation}m/{cell.WaterClass}\n" +
                $"Owner: {cell.OwnerId ?? "none"}\nFacility: {cell.FacilityName ?? "none"}\nDefinition: {cell.FacilityDefinitionId ?? "none"}\n" +
                $"Historical confidence: {cell.HistoricalConfidence ?? "none"}\nWorkers: {cell.CurrentWorkers} (normal threshold {cell.RequiredWorkers})\n" +
                $"Population / permanent Person capacity: {cell.Population}/{cell.ResidentCapacityPersons}\n" +
                $"Wall/Gate/Moat: {cell.WallState ?? "-"}/{cell.GateState ?? "-"}/{cell.MoatState ?? "-"}\nWarning: {warning}\n\n" +
                _historicalPersonDebugText + "\n\n" + LivingSummaryText() +
                "\n\n" + _livingDebugText);
        }

        private string LivingSummaryText()
        {
            if (_livingRuntime == null) return "Living-world closure: unavailable";
            var summary = _livingSystem.BuildWorldSummary(_livingRuntime);
            var pressure = _livingRuntime.SocialPressureHistory.LastOrDefault();
            var force = _livingRuntime.Forces.FirstOrDefault();
            return $"Living-world day {summary.LastSimulatedDay}: Persons {summary.PermanentPersonCount:N0}; Households {summary.HouseholdCount:N0}; Facilities {summary.FacilityCount:N0}\n" +
                   $"Food stock/consumed/shortage {summary.FoodStockMilliunits:N0}/{summary.FoodConsumptionMilliunits:N0}/{summary.FoodShortageMilliunits:N0}\n" +
                   $"Supply {summary.SupplyStatusId}; Markets {_livingRuntime.Markets.Count}; Orders/Shipments {_livingRuntime.SupplyOrders.Count}/{_livingRuntime.Shipments.Count}\n" +
                   $"Treasury {_livingRuntime.GovernmentEconomy.Treasury:N0}; tax {_livingRuntime.GovernmentEconomy.TaxRevenue:N0}; Offices {_livingRuntime.Offices.Count}\n" +
                   $"Force {force?.PermanentPersonCount ?? 0}; defense {force?.DefenseBasisPoints ?? 0}; pressure {pressure?.CompositeBasisPoints ?? 0}\n" +
                   $"Property/projects {_livingRuntime.CellProperties.Count}/{_livingRuntime.ConstructionProjects.Count}; events " +
                   string.Join(",", _livingRuntime.HistoricalEvents.Select(item =>
                       item.DefinitionId + "=" + item.StatusId));
        }

        private void HandleMapInput(Event current)
        {
            if (!_mapRect.Contains(current.mousePosition)) return;
            if (current.type == EventType.ScrollWheel)
            {
                Zoom(current.delta.y > 0 ? 1.12f : 0.89f);
                current.Use();
                return;
            }
            if (current.type != EventType.MouseDown || current.button != 0) return;
            var x = (current.mousePosition.x - _mapRect.x) / _mapRect.width * TextureWidth;
            var y = (current.mousePosition.y - _mapRect.y) / _mapRect.height * TextureHeight;
            var column = Mathf.FloorToInt(_centerColumn + (x - TextureWidth * 0.5f) * _cellsPerPixel);
            var row = Mathf.FloorToInt(_centerRow + (y - TextureHeight * 0.5f) * _cellsPerPixel);
            if (row >= 0 && column >= 0) SelectCell((ulong)row * (ulong)Prototype.Columns + (ulong)column);
            current.Use();
        }

        private void RenderMap()
        {
            if (_prototypeReader == null || _mapTexture == null) return;
            for (var y = 0; y < TextureHeight; y++)
            {
                var row = Mathf.FloorToInt(_centerRow + (y - TextureHeight * 0.5f) * _cellsPerPixel);
                for (var x = 0; x < TextureWidth; x++)
                {
                    var column = Mathf.FloorToInt(_centerColumn + (x - TextureWidth * 0.5f) * _cellsPerPixel);
                    var target = (TextureHeight - 1 - y) * TextureWidth + x;
                    if (row < 0 || column < 0 || row >= Prototype.Rows || column >= Prototype.Columns ||
                        !_prototypeReader.TryGetCell((ulong)row * (ulong)Prototype.Columns + (ulong)column, out var cell))
                        _pixels[target] = new Color32(35, 31, 27, 255);
                    else
                        _pixels[target] = CellColor(cell, Overlay);
                }
            }
            _mapTexture.SetPixels32(_pixels);
            _mapTexture.Apply(false, false);
        }

        private static Color32 CellColor(Luoyang184CellRecord cell, LuoyangMapOverlay overlay)
        {
            if (overlay == LuoyangMapOverlay.PopulationDensity) return Heat(cell.Population, 180);
            if (overlay == LuoyangMapOverlay.HousingPressure)
                return Heat(Math.Max(0, cell.Population - cell.ResidentCapacityPersons), 30);
            if (overlay == LuoyangMapOverlay.Jobs) return Heat(cell.CurrentWorkers, Math.Max(1, cell.RequiredWorkers));
            if (overlay == LuoyangMapOverlay.HistoricalConfidence && !string.IsNullOrEmpty(cell.HistoricalConfidence))
                return cell.HistoricalConfidence == "HistoricalAnchor" ? new Color32(160, 53, 42, 255) :
                    cell.HistoricalConfidence == "HistoricalReconstruction" ? new Color32(202, 139, 51, 255) : new Color32(102, 111, 122, 255);
            if (overlay == LuoyangMapOverlay.Fortifications && cell.FacilityCategoryId == "fortification")
                return cell.WallState == "Breached" || cell.GateState == "Destroyed" ? new Color32(220, 65, 40, 255) :
                    !string.IsNullOrEmpty(cell.GateState) ? new Color32(235, 175, 58, 255) : new Color32(89, 49, 35, 255);
            if (overlay == LuoyangMapOverlay.UnusedDevelopable)
                return cell.Developable && string.IsNullOrEmpty(cell.FacilityId) ? new Color32(118, 164, 106, 255) : new Color32(62, 58, 50, 255);
            var category = CategoryForOverlay(overlay);
            if (category != null)
                return CategoryMatches(cell.FacilityCategoryId, category) ? new Color32(205, 118, 36, 255) : new Color32(55, 53, 48, 255);
            if (cell.WaterClass != 0) return new Color32(51, 112, 153, 255);
            if (cell.RoadClass != 0) return new Color32(179, 126, 58, 255);
            if (cell.TerrainClass >= 3) return new Color32(96, 88, 70, 255);
            return new Color32(125, 151, 91, 255);
        }

        private static string CategoryForOverlay(LuoyangMapOverlay overlay)
        {
            switch (overlay)
            {
                case LuoyangMapOverlay.Agriculture: return "agriculture";
                case LuoyangMapOverlay.Residential: return "residential";
                case LuoyangMapOverlay.Industry: return "industry";
                case LuoyangMapOverlay.Commercial: return "commercial";
                case LuoyangMapOverlay.Public: return "public";
                case LuoyangMapOverlay.Road: return "road";
                case LuoyangMapOverlay.Military: return "military";
                default: return null;
            }
        }

        private static bool CategoryMatches(string actual, string expected) =>
            string.Equals(actual, expected, StringComparison.Ordinal) ||
            expected == "public" && (actual == "government" || actual == "education" || actual == "ritual" || actual == "storage") ||
            expected == "military" && actual == "fortification";

        private static Color32 Heat(int value, int maximum)
        {
            var t = Mathf.Clamp01(value / (float)Math.Max(1, maximum));
            return Color.Lerp(new Color(0.12f, 0.18f, 0.25f), new Color(0.85f, 0.18f, 0.07f), t);
        }
    }
}
