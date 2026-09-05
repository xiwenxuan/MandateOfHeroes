using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class Luoyang50mCountySpatialBenchmarkResult
    {
        public Luoyang50mCountySpatialBenchmarkResult(
            int iterations, double hotP50Milliseconds,
            double hotP95Milliseconds, double warmP50Milliseconds,
            double warmP95Milliseconds, double coldP50Milliseconds,
            double coldP95Milliseconds, int hotFacilityIndexCount,
            int hotPlanningCellCount, int hotChunkCount,
            string spatialHashBefore, string spatialHashAfter)
        {
            Iterations = iterations;
            HotP50Milliseconds = hotP50Milliseconds;
            HotP95Milliseconds = hotP95Milliseconds;
            WarmP50Milliseconds = warmP50Milliseconds;
            WarmP95Milliseconds = warmP95Milliseconds;
            ColdP50Milliseconds = coldP50Milliseconds;
            ColdP95Milliseconds = coldP95Milliseconds;
            HotFacilityIndexCount = hotFacilityIndexCount;
            HotPlanningCellCount = hotPlanningCellCount;
            HotChunkCount = hotChunkCount;
            SpatialHashBefore = spatialHashBefore;
            SpatialHashAfter = spatialHashAfter;
        }

        public int Iterations { get; }
        public double HotP50Milliseconds { get; }
        public double HotP95Milliseconds { get; }
        public double WarmP50Milliseconds { get; }
        public double WarmP95Milliseconds { get; }
        public double ColdP50Milliseconds { get; }
        public double ColdP95Milliseconds { get; }
        public int HotFacilityIndexCount { get; }
        public int HotPlanningCellCount { get; }
        public int HotChunkCount { get; }
        public string SpatialHashBefore { get; }
        public string SpatialHashAfter { get; }
        public bool SpatialStateUnchanged => string.Equals(SpatialHashBefore,
            SpatialHashAfter, StringComparison.Ordinal);
    }

    public static class Luoyang50mCountySpatialPrototypeBenchmark
    {
        public static Luoyang50mCountySpatialBenchmarkResult Run(
            Luoyang50mCountySpatialPrototype prototype, int iterations = 9)
        {
            if (prototype == null) throw new ArgumentNullException(
                nameof(prototype));
            if (iterations < 3 || iterations > 100)
                throw new ArgumentOutOfRangeException(nameof(iterations));
            var coordinator = new CountySpatialLoadCoordinator(
                new DualScaleCoordinateProjection());
            var hot = new List<double>(iterations);
            var warm = new List<double>(iterations);
            var cold = new List<double>(iterations);
            var before = prototype.Partition.ComputeSpatialHash();
            CountySpatialCacheHandle hotHandle = null;
            for (var index = 0; index < iterations; index++)
            {
                hot.Add(Measure(() => hotHandle = coordinator.SetLevel(
                    prototype.Partition, CountySpatialLoadLevel.Hot)));
                warm.Add(Measure(() => coordinator.SetLevel(
                    prototype.Partition, CountySpatialLoadLevel.Warm)));
                cold.Add(Measure(() => coordinator.Unload(
                    prototype.Partition)));
            }
            var finalCold = coordinator.Get(prototype.Partition.CountyId);
            if (hotHandle == null || hotHandle.IndexedFacilityCount !=
                    Luoyang50mCountySpatialPrototypeIds.FacilityCount ||
                hotHandle.ResidentPlanningCellCount !=
                    Luoyang50mCountySpatialPrototypeIds.PlanningCellCount ||
                hotHandle.ResidentChunkCount !=
                    Luoyang50mCountySpatialPrototypeIds.ChunkCount ||
                finalCold == null || finalCold.ResidentPlanningCellCount != 0 ||
                finalCold.ResidentChunkCount != 0 ||
                finalCold.IndexedFacilityCount != 0)
                throw new InvalidOperationException(
                    "Luoyang county streaming residency contract failed.");
            var after = prototype.Partition.ComputeSpatialHash();
            if (!string.Equals(before, after, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Streaming changed Luoyang spatial source state.");
            return new Luoyang50mCountySpatialBenchmarkResult(iterations,
                Percentile(hot, 0.50d), Percentile(hot, 0.95d),
                Percentile(warm, 0.50d), Percentile(warm, 0.95d),
                Percentile(cold, 0.50d), Percentile(cold, 0.95d),
                hotHandle.IndexedFacilityCount,
                hotHandle.ResidentPlanningCellCount,
                hotHandle.ResidentChunkCount, before, after);
        }

        private static double Measure(Action action)
        {
            var timer = Stopwatch.StartNew();
            action();
            timer.Stop();
            return timer.Elapsed.TotalMilliseconds;
        }

        private static double Percentile(IEnumerable<double> source,
            double percentile)
        {
            var values = source.OrderBy(value => value).ToArray();
            var index = (int)Math.Ceiling(percentile * values.Length) - 1;
            return values[Math.Max(0, Math.Min(values.Length - 1, index))];
        }
    }
}
