using System.Collections;
using System.Diagnostics;
using Mandate.Domain;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class Luoyang184PersonWorkProductionConsumptionClosurePlayModeTests
    {
        [UnityTest]
        public IEnumerator LivingWorldDashboardUsesCompactRecordsAndExposesDebugSelectors()
        {
            var before = Object.FindObjectsOfType<GameObject>().Length;
            var memoryBefore = Profiler.GetTotalAllocatedMemoryLong();
            var initialization = Stopwatch.StartNew();
            var load = SceneManager.LoadSceneAsync("LuoyangWorldValidation",
                LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;

            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            initialization.Stop();
            Assert.That(controller.LivingRuntime.Workforce.Count,
                Is.EqualTo(700_000));
            Assert.That(controller.LivingRuntime.Households.Count,
                Is.EqualTo(142_980));
            Assert.That(controller.LivingRuntime.Facilities.Count,
                Is.EqualTo(2_779));
            Assert.That(controller.SelectLivingPerson(699_999), Is.True);
            Assert.That(controller.SelectLivingHousehold(142_979), Is.True);
            Assert.That(controller.SelectLivingFacility(2_778), Is.True);
            Assert.That(controller.LivingDebugText, Is.Not.Empty);
            Assert.That(controller.ExecutePlayerCommand(
                LuoyangPlayerCommandTypeIds.Study), Is.True);
            Assert.That(controller.LivingRuntime.PlayerCommands,
                Has.Some.Matches<LuoyangPlayerCommandRuntimeState>(item =>
                    item.CommandTypeId == LuoyangPlayerCommandTypeIds.Study &&
                    item.StatusId == "completed"));
            var addedGameObjects = Object.FindObjectsOfType<GameObject>().Length - before;
            Assert.That(addedGameObjects,
                Is.LessThan(32), "Permanent Persons must not become GameObjects.");

            const int frameSampleCount = 20;
            var totalFrameMilliseconds = 0f;
            var maximumFrameMilliseconds = 0f;
            for (var index = 0; index < frameSampleCount; index++)
            {
                yield return null;
                var frameMilliseconds = Time.unscaledDeltaTime * 1000f;
                totalFrameMilliseconds += frameMilliseconds;
                maximumFrameMilliseconds = Mathf.Max(maximumFrameMilliseconds,
                    frameMilliseconds);
            }

            var memoryAfter = Profiler.GetTotalAllocatedMemoryLong();
            var memoryDelta = memoryAfter - memoryBefore;
            var averageFrameMilliseconds = totalFrameMilliseconds / frameSampleCount;
            Assert.That(initialization.ElapsedMilliseconds, Is.LessThan(60_000),
                "The expanded compact world must initialize within the V1 validation budget.");
            Assert.That(memoryDelta, Is.LessThan(1_500_000_000L),
                "The expanded compact world must not materialize a full hot-object population.");
            Assert.That(float.IsNaN(averageFrameMilliseconds), Is.False);
            UnityEngine.Debug.Log(string.Format(
                "OUTER_SUPPLY_UNITY_PERFORMANCE initialization_ms={0} " +
                "loaded_gameobjects={1} allocated_memory_before={2} " +
                "allocated_memory_after={3} allocated_memory_delta={4} " +
                "sample_frames={5} average_frame_ms={6:F3} maximum_frame_ms={7:F3}",
                initialization.ElapsedMilliseconds,
                addedGameObjects,
                memoryBefore,
                memoryAfter,
                memoryDelta,
                frameSampleCount,
                averageFrameMilliseconds,
                maximumFrameMilliseconds));
        }
    }
}
