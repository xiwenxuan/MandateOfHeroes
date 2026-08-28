using System.Collections;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mandate.Tests.PlayMode
{
    public sealed class LuoyangHumanScaleLocalMapV1PlayModeTests
    {
        [UnityTest]
        public IEnumerator StreamingAcrossFiveCells_ReturnsWithoutRegeneration()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(Path.Combine(
                Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
                "WorldMap"));
            var row = LuoyangHumanScaleLocalMapIds.MapMinRow + 2;
            var cells = source.Plan.LocalSpaces.Where(item =>
                    item.GridRow == row && item.GridColumn >=
                    LuoyangHumanScaleLocalMapIds.MapMinColumn + 2)
                .OrderBy(item => item.GridColumn).Take(5).ToArray();
            Assert.That(cells.Length, Is.EqualTo(5));
            var origin = new GlobalProjectedCoordinate(
                cells[0].OriginEastingMetres + 1_000d,
                cells[0].OriginNorthingMetres + 1_000d);
            Vector3 Resolve(double east, double north)
            {
                var value = source.Plan.WorldScale.WorldToUnity(
                    new GlobalProjectedCoordinate(east, north), 0d, origin);
                return new Vector3((float)value.XMetres, 0f,
                    (float)value.ZMetres);
            }
            var runtime = LuoyangHumanScaleStreamingRuntime.Build(source.Plan,
                Resolve, cells[0].ParentCellId64);
            var initialHash = runtime.MapAssetHash;
            try
            {
                yield return null;
                foreach (var cell in cells.Skip(1))
                {
                    runtime.MoveWindow(cell.ParentCellId64);
                    yield return null;
                    Assert.That(runtime.ResidentCellCount, Is.EqualTo(9));
                }
                runtime.MoveWindow(cells[0].ParentCellId64);
                yield return null;
                Assert.That(runtime.MapAssetHash, Is.EqualTo(initialHash));
                Assert.That(runtime.ResidentCellCount, Is.EqualTo(9));
                Assert.That(runtime.ResidentGameObjectCount,
                    Is.GreaterThan(9));
            }
            finally
            {
                runtime.Dispose();
            }
            yield return null;
            Assert.That(GameObject.Find(
                LuoyangHumanScaleStreamingRuntime.RootName), Is.Null);
        }
    }
}
