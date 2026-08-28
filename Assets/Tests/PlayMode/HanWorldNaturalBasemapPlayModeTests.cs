using System.Collections;
using System.IO;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class HanWorldNaturalBasemapPlayModeTests
    {
        [UnityTest]
        public IEnumerator NaturalWorldMap_InitializesWithoutLegacyBackgroundAndSupportsCellPicking()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldNaturalBasemap", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            Assert.That(controller.UsesLegacyBackground, Is.False);
            Assert.That(controller.IndexedTerrainTileCount, Is.EqualTo(112880));
            Assert.That(controller.RuntimeTerrainObjectCount, Is.EqualTo(1));
            controller.LocateLuoyang();
            yield return null;
            Assert.That(controller.RuntimeTerrainObjectCount, Is.EqualTo(10),
                "V2 keeps one DEM-derived regional mid-LOD mesh behind nine formal Terrain Tiles.");
            Assert.That(controller.RuntimeVegetationObjectCount, Is.LessThanOrEqualTo(1));
            Assert.That(controller.TryPickGlobalCell(Vector3.zero, out var picked), Is.True);
            Assert.That(picked.Value, Is.EqualTo(4114717UL));
        }

        [UnityTest]
        public IEnumerator NaturalWorldMap_CapturesRequiredBackgroundOffVisualEvidence()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldNaturalBasemap", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            Assert.That(controller.UsesLegacyBackground, Is.False);
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
                "HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1", "Screenshots");
            Directory.CreateDirectory(root);

            controller.SetWorldView();
            yield return Capture(controller, root, "01_WORLD_NATURAL_MAP_CLEAN.png");
            controller.LocateNorthChinaPlain();
            yield return Capture(controller, root, "02_NORTH_CHINA_PLAIN.png");
            controller.LocateMountainRegion();
            yield return Capture(controller, root, "03_MOUNTAIN_REGION.png");
            controller.LocateMajorRiverRegion();
            yield return Capture(controller, root, "04_MAJOR_RIVER_REGION.png");
            controller.LocateForestRegion();
            yield return Capture(controller, root, "05_FOREST_REGION.png");
            controller.LocateHenanYin();
            yield return Capture(controller, root, "06_HENAN_YIN_NATURAL_REGION.png");
            controller.LocateLuoyang();
            yield return Capture(controller, root, "07_LUOYANG_AREA_WITHOUT_CITY_BACKGROUND.png");
            yield return Capture(controller, root, "08_TERRAIN_TILE_SEAM_CLOSEUP.png");
            controller.SetCellOverlayVisible(true);
            yield return Capture(controller, root, "09_CELL_OVERLAY_DEBUG.png");
            controller.SetWorldView();
            yield return Capture(controller, root, "10_BACKGROUND_OFF_WORLD.png");
            Assert.That(controller.UsesLegacyBackground, Is.False);
        }

        private static IEnumerator Capture(HanWorldNaturalMapController controller, string root, string file)
        {
            yield return null;
            var path = Path.Combine(root, file);
            controller.CaptureEvidence(path);
            Assert.That(File.Exists(path), Is.True, file);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(5000), file);
        }
    }
}
