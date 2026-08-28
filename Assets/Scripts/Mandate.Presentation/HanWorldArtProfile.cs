using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mandate.Presentation
{
    public enum HanWorldArtStyle
    {
        RealisticNatural,
        ChineseSemiRealistic,
        StrategicSandbox,
        ZhonghuaSanguozhiFusion
    }

    /// <summary>
    /// Presentation-only parameters for the national natural map. Profiles never own or mutate
    /// geography, terrain-source heights, rivers, forests, Global Cells, people, facilities or saves.
    /// </summary>
    [Serializable]
    public sealed class HanWorldArtProfile
    {
        public string ProfileId;
        public string ProfileName;
        public HanWorldArtStyle Style;
        public Color TerrainTint;
        public Color RiverTint;
        public Color ForestTint;
        public Color ShoreTint;
        public Color DeepWaterTint;
        public Color FogColor;
        public Color BackgroundColor;
        public Color SunColor;
        public Color AmbientColor;
        public float SunIntensity;
        public float WorldVerticalExaggeration;
        public float RegionVerticalExaggeration;
        public float Saturation;
        public float SlopeStrength;
        public float CurvatureStrength;
        public float RidgeStrength;
        public float ValleyStrength;
        public float MacroNoiseScale;
        public float MacroNoiseStrength;
        public float TerrainNoiseStrength;
        public float RiverNoiseStrength;
        public float ForestNoiseStrength;
        public float WorldFogStart;
        public float WorldFogEnd;
        public float RegionFogStart;
        public float RegionFogEnd;
        public float WorldCameraSizeScale;
        public float RegionCameraSizeScale;
        public float RiverWidthScale;
        public float ForestCanopyScale;
        public float FusionStrength;
        public Color FusionMountainTint;
        public Color FusionForestTint;
        public Color FusionRiverValleyTint;
        public Color FusionPlainTint;
        public string VisualIntent;
    }

    public static class HanWorldArtProfileCatalog
    {
        public const string StyleAId = "art.han-world.realistic-natural.v1";
        public const string StyleBId = "art.han-world.chinese-semi-realistic.v1";
        public const string StyleCId = "art.han-world.strategic-sandbox.v1";
        public const string StyleDId = "art.han-world.zhonghua-sanguozhi-fusion.v2";

        private static readonly IReadOnlyDictionary<HanWorldArtStyle, HanWorldArtProfile> Profiles =
            new Dictionary<HanWorldArtStyle, HanWorldArtProfile>
            {
                [HanWorldArtStyle.RealisticNatural] = new HanWorldArtProfile
                {
                    ProfileId = StyleAId,
                    ProfileName = "STYLE A | Realistic Natural",
                    Style = HanWorldArtStyle.RealisticNatural,
                    TerrainTint = C(0.91f, 0.96f, 0.88f), RiverTint = C(0.72f, 0.91f, 1.00f),
                    ForestTint = C(0.83f, 0.96f, 0.79f), ShoreTint = C(0.90f, 0.86f, 0.70f),
                    DeepWaterTint = C(0.26f, 0.45f, 0.55f), FogColor = C(0.57f, 0.64f, 0.65f),
                    BackgroundColor = C(0.32f, 0.51f, 0.60f), SunColor = C(1.00f, 0.95f, 0.84f),
                    AmbientColor = C(0.39f, 0.44f, 0.42f), SunIntensity = 1.02f,
                    WorldVerticalExaggeration = 1.75f, RegionVerticalExaggeration = 1.28f,
                    Saturation = 0.92f, SlopeStrength = 0.34f, CurvatureStrength = 0.12f,
                    RidgeStrength = 0.12f, ValleyStrength = 0.10f, MacroNoiseScale = 5.2f,
                    MacroNoiseStrength = 0.065f, TerrainNoiseStrength = 0.055f,
                    RiverNoiseStrength = 0.015f, ForestNoiseStrength = 0.045f,
                    WorldFogStart = 2350f, WorldFogEnd = 5450f, RegionFogStart = 98f, RegionFogEnd = 285f,
                    WorldCameraSizeScale = 1f, RegionCameraSizeScale = 1f,
                    RiverWidthScale = 0.94f, ForestCanopyScale = 0.92f,
                    VisualIntent = "Natural semi-realism and restrained relief; the geographic baseline candidate."
                },
                [HanWorldArtStyle.ChineseSemiRealistic] = new HanWorldArtProfile
                {
                    ProfileId = StyleBId,
                    ProfileName = "STYLE B | Chinese Semi-realistic",
                    Style = HanWorldArtStyle.ChineseSemiRealistic,
                    TerrainTint = C(0.86f, 0.90f, 0.76f), RiverTint = C(0.61f, 0.82f, 0.88f),
                    ForestTint = C(0.66f, 0.79f, 0.61f), ShoreTint = C(0.82f, 0.74f, 0.56f),
                    DeepWaterTint = C(0.22f, 0.36f, 0.42f), FogColor = C(0.50f, 0.58f, 0.57f),
                    BackgroundColor = C(0.28f, 0.43f, 0.47f), SunColor = C(1.00f, 0.86f, 0.66f),
                    AmbientColor = C(0.32f, 0.37f, 0.36f), SunIntensity = 1.10f,
                    WorldVerticalExaggeration = 2.18f, RegionVerticalExaggeration = 1.58f,
                    Saturation = 0.74f, SlopeStrength = 0.48f, CurvatureStrength = 0.23f,
                    RidgeStrength = 0.26f, ValleyStrength = 0.22f, MacroNoiseScale = 4.4f,
                    MacroNoiseStrength = 0.085f, TerrainNoiseStrength = 0.075f,
                    RiverNoiseStrength = 0.020f, ForestNoiseStrength = 0.060f,
                    WorldFogStart = 2050f, WorldFogEnd = 5050f, RegionFogStart = 82f, RegionFogEnd = 250f,
                    WorldCameraSizeScale = 1f, RegionCameraSizeScale = 1f,
                    RiverWidthScale = 1.00f, ForestCanopyScale = 1.00f,
                    VisualIntent = "Low-saturation Chinese historical landscape atmosphere on a readable 3D strategic terrain."
                },
                [HanWorldArtStyle.StrategicSandbox] = new HanWorldArtProfile
                {
                    ProfileId = StyleCId,
                    ProfileName = "STYLE C | Strategic Sandbox",
                    Style = HanWorldArtStyle.StrategicSandbox,
                    TerrainTint = C(0.96f, 0.98f, 0.79f), RiverTint = C(0.64f, 0.91f, 1.00f),
                    ForestTint = C(0.70f, 0.92f, 0.61f), ShoreTint = C(0.94f, 0.82f, 0.55f),
                    DeepWaterTint = C(0.18f, 0.43f, 0.59f), FogColor = C(0.58f, 0.65f, 0.62f),
                    BackgroundColor = C(0.31f, 0.50f, 0.58f), SunColor = C(1.00f, 0.91f, 0.74f),
                    AmbientColor = C(0.39f, 0.44f, 0.38f), SunIntensity = 1.16f,
                    WorldVerticalExaggeration = 2.65f, RegionVerticalExaggeration = 1.92f,
                    Saturation = 1.05f, SlopeStrength = 0.62f, CurvatureStrength = 0.18f,
                    RidgeStrength = 0.34f, ValleyStrength = 0.25f, MacroNoiseScale = 6.0f,
                    MacroNoiseStrength = 0.055f, TerrainNoiseStrength = 0.045f,
                    RiverNoiseStrength = 0.012f, ForestNoiseStrength = 0.035f,
                    WorldFogStart = 2500f, WorldFogEnd = 5650f, RegionFogStart = 108f, RegionFogEnd = 310f,
                    WorldCameraSizeScale = 1f, RegionCameraSizeScale = 1f,
                    RiverWidthScale = 1.16f, ForestCanopyScale = 1.08f,
                    VisualIntent = "Stronger landform, river and forest separation for warfare and route planning without Cell-board visuals."
                },
                [HanWorldArtStyle.ZhonghuaSanguozhiFusion] = new HanWorldArtProfile
                {
                    ProfileId = StyleDId,
                    ProfileName = "STYLE D V2 | Strategic Landscape",
                    Style = HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                    TerrainTint = C(0.92f, 0.88f, 0.70f), RiverTint = C(0.54f, 0.77f, 0.82f),
                    ForestTint = C(0.48f, 0.65f, 0.43f), ShoreTint = C(0.82f, 0.70f, 0.49f),
                    DeepWaterTint = C(0.19f, 0.35f, 0.40f), FogColor = C(0.55f, 0.57f, 0.49f),
                    BackgroundColor = C(0.31f, 0.40f, 0.39f), SunColor = C(1.00f, 0.82f, 0.57f),
                    AmbientColor = C(0.37f, 0.36f, 0.30f), SunIntensity = 1.18f,
                    WorldVerticalExaggeration = 2.82f, RegionVerticalExaggeration = 2.08f,
                    Saturation = 0.80f, SlopeStrength = 0.70f, CurvatureStrength = 0.26f,
                    RidgeStrength = 0.58f, ValleyStrength = 0.42f, MacroNoiseScale = 3.4f,
                    MacroNoiseStrength = 0.105f, TerrainNoiseStrength = 0.060f,
                    RiverNoiseStrength = 0.014f, ForestNoiseStrength = 0.025f,
                    WorldFogStart = 2180f, WorldFogEnd = 5200f, RegionFogStart = 84f, RegionFogEnd = 268f,
                    WorldCameraSizeScale = 1f, RegionCameraSizeScale = 1f,
                    RiverWidthScale = 1.16f, ForestCanopyScale = 1.05f,
                    FusionStrength = 0.91f,
                    FusionMountainTint = C(0.48f, 0.43f, 0.31f),
                    FusionForestTint = C(0.29f, 0.43f, 0.27f),
                    FusionRiverValleyTint = C(0.43f, 0.64f, 0.65f),
                    FusionPlainTint = C(0.72f, 0.69f, 0.49f),
                    VisualIntent = "Clean-room Style D V2: readable mountain systems, terrain-conforming rivers, multi-LOD forests and presentation-only terrain detail derived from the authoritative world."
                }
            };

        public static HanWorldArtProfile Get(HanWorldArtStyle style) => Profiles[style];

        public static IEnumerable<HanWorldArtProfile> All
        {
            get
            {
                yield return Profiles[HanWorldArtStyle.RealisticNatural];
                yield return Profiles[HanWorldArtStyle.ChineseSemiRealistic];
                yield return Profiles[HanWorldArtStyle.StrategicSandbox];
                yield return Profiles[HanWorldArtStyle.ZhonghuaSanguozhiFusion];
            }
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);
    }

    public static class ZhonghuaFusionCameraRig
    {
        public const string World = "CAM_STYLE_D_WORLD";
        public const string Region = "CAM_STYLE_D_REGION";
        public const string CityDistance = "CAM_STYLE_D_CITY_DISTANCE";
        public const string Mountain = "CAM_STYLE_D_MOUNTAIN";
        public const string RiverStraight = "CAM_STYLE_D_RIVER_STRAIGHT";
        public const string RiverGentle = "CAM_STYLE_D_RIVER_GENTLE";
        public const string RiverSharpBend = "CAM_STYLE_D_RIVER_SHARP_BEND";
        public const string RiverConfluence = "CAM_STYLE_D_RIVER_CONFLUENCE";
        public const string RiverBankClose = "CAM_STYLE_D_RIVER_BANK_CLOSE";
        public const string ForestWorld = "CAM_STYLE_D_FOREST_WORLD";
        public const string ForestRegion = "CAM_STYLE_D_FOREST_REGION";
        public const string ForestCity = "CAM_STYLE_D_FOREST_CITY";
        public const string ForestEdge = "CAM_STYLE_D_FOREST_EDGE";
        public const string ForestClearing = "CAM_STYLE_D_FOREST_CLEARING";
        public const string Plain = "CAM_STYLE_D_PLAIN";
        public const string TerrainDetail = "CAM_STYLE_D_TERRAIN_DETAIL";
        public const string WorldToCityMid = "CAM_STYLE_D_WORLD_TO_CITY_MID";
        public const string GridOff = "CAM_STYLE_D_BACKGROUND_GRID_OFF";
        public const string River = RiverGentle;
        public const string Forest = ForestRegion;
        public const string WorldToRegionMid = WorldToCityMid;
        public const string CityDistancePreview = CityDistance;

        public static NaturalMapCameraPreset Get(string id)
        {
            switch (id)
            {
                case World: return new NaturalMapCameraPreset(id, 1088, 1657, 1160f, 68f, 0f);
                case Region: return new NaturalMapCameraPreset(id, 1247, 1992, 34f, 58f, -12f);
                case CityDistance: return new NaturalMapCameraPreset(id, 1241, 2043, 14f, 55f, -16f);
                case Mountain: return new NaturalMapCameraPreset(id, 1390, 1710, 29f, 56f, -18f);
                case RiverStraight: return new NaturalMapCameraPreset(id, 1205, 2156, 10f, 55f, -10f);
                case RiverGentle: return new NaturalMapCameraPreset(id, 1209, 2148, 24f, 58f, -10f);
                case RiverSharpBend: return new NaturalMapCameraPreset(id, 1209, 2148, 12f, 56f, -12f);
                case RiverConfluence: return new NaturalMapCameraPreset(id, 1247, 1992, 18f, 59f, -8f);
                case RiverBankClose: return new NaturalMapCameraPreset(id, 1209, 2148, 7f, 53f, -12f);
                case ForestWorld: return new NaturalMapCameraPreset(id, 1460, 1970, 330f, 63f, 8f);
                case ForestRegion: return new NaturalMapCameraPreset(id, 1460, 1970, 26f, 55f, 12f);
                case ForestCity: return new NaturalMapCameraPreset(id, 1460, 1970, 9f, 52f, 14f);
                case ForestEdge: return new NaturalMapCameraPreset(id, 1452, 1962, 15f, 54f, 10f);
                case ForestClearing: return new NaturalMapCameraPreset(id, 1465, 1978, 12f, 52f, 14f);
                case Plain: return new NaturalMapCameraPreset(id, 1110, 2090, 30f, 60f, -7f);
                case TerrainDetail: return new NaturalMapCameraPreset(id, 1241, 2043, 7f, 52f, -16f);
                case WorldToCityMid: return new NaturalMapCameraPreset(id, 1247, 1992, 92f, 61f, -10f);
                case GridOff: return new NaturalMapCameraPreset(id, 1241, 2043, 11f, 54f, -16f);
                default: throw new ArgumentException("Unknown Style D camera preset: " + id, nameof(id));
            }
        }

        public static bool IsWorldView(string id) => id == World || id == ForestWorld || id == WorldToCityMid;

        public static VisualTerrainDetailLevel DetailLevelFor(string id)
        {
            if (IsWorldView(id)) return VisualTerrainDetailLevel.World;
            if (id == CityDistance || id == ForestCity || id == RiverBankClose || id == GridOff)
                return VisualTerrainDetailLevel.City;
            if (id == TerrainDetail) return VisualTerrainDetailLevel.ClosePreview;
            return VisualTerrainDetailLevel.Region;
        }
    }

    public enum ArtDirectionSample
    {
        CentralPlain,
        MountainRiver,
        ForestHills,
        HenanYin
    }

    public static class HanWorldArtDirectionCameraRig
    {
        public static NaturalMapCameraPreset Get(ArtDirectionSample sample, HanNaturalMapView view)
        {
            switch (sample)
            {
                case ArtDirectionSample.CentralPlain:
                    return view == HanNaturalMapView.World
                        ? new NaturalMapCameraPreset("CAM_ART_SAMPLE_A_WORLD", 1209, 2148, 410f, 66f, -5f)
                        : new NaturalMapCameraPreset("CAM_ART_SAMPLE_A_REGION", 1209, 2148, 31f, 58f, -10f);
                case ArtDirectionSample.MountainRiver:
                    return view == HanNaturalMapView.World
                        ? new NaturalMapCameraPreset("CAM_ART_SAMPLE_B_WORLD", 1110, 2090, 340f, 64f, -10f)
                        : new NaturalMapCameraPreset("CAM_ART_SAMPLE_B_REGION", 1110, 2090, 25f, 55f, -16f);
                case ArtDirectionSample.ForestHills:
                    return view == HanNaturalMapView.World
                        ? new NaturalMapCameraPreset("CAM_ART_SAMPLE_C_WORLD", 1460, 1970, 330f, 63f, 8f)
                        : new NaturalMapCameraPreset("CAM_ART_SAMPLE_C_REGION", 1460, 1970, 24f, 54f, 12f);
                case ArtDirectionSample.HenanYin:
                    return view == HanNaturalMapView.World
                        ? new NaturalMapCameraPreset("CAM_ART_HENAN_YIN_WORLD", 1247, 1992, 300f, 64f, -8f)
                        : new NaturalMapCameraPreset("CAM_ART_HENAN_YIN_REGION", 1247, 1992, 30f, 57f, -12f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(sample), sample, null);
            }
        }
    }
}
