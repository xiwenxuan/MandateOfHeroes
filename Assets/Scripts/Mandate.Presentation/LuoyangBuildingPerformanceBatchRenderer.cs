using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mandate.Presentation
{
    [Serializable]
    public sealed class LuoyangBuildingBatchMetrics
    {
        public string BudgetId;
        public int FullCityFacilityCount;
        public int FullCitySpatialBatchCount;
        public int ResidentFacilityCount;
        public int ResidentSpatialBatchCount;
        public int SourceModuleRendererCount;
        public int BuildingRendererBatchCount;
        public int CombinedMeshCount;
        public int UniqueSourceMeshCount;
        public int CombinedVertexCount;
        public double BatchBuildMilliseconds;
        public double RendererReductionRatio;
        public bool AllowsSpatialOcclusion;
        public bool WithinBudget;
    }

    public sealed class LuoyangBuildingPerformanceBatchRenderer : IDisposable
    {
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private bool _disposed;

        public LuoyangBuildingBatchMetrics Build(Transform parent,
            LuoyangBuildingPerformancePlan plan,
            LuoyangBuildingResidentWindow window,
            HanBuildableFacilityModelFactory factory,
            Func<LuoyangBuildingPerformanceFacility, Vector3> positionResolver,
            Func<LuoyangBuildingPerformanceFacility, float> rotationResolver,
            Func<LuoyangBuildingPerformanceFacility, Vector3> scaleResolver = null,
            bool enforceFrozenResidentBudget = true)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
            if (_ownedMeshes.Count != 0)
                throw new InvalidOperationException(
                    "Luoyang building batch renderer can only build once.");
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (positionResolver == null)
                throw new ArgumentNullException(nameof(positionResolver));
            if (rotationResolver == null)
                throw new ArgumentNullException(nameof(rotationResolver));

            var stopwatch = Stopwatch.StartNew();
            var groups = new Dictionary<BatchMaterialKey, BatchMaterialGroup>();
            var sourceMeshes = new HashSet<Mesh>();
            var sourceModuleCount = 0;
            foreach (var facility in window.Facilities)
            {
                var modules = factory.GetWorldBatchModules(facility.ModelId,
                    facility.FacilityId);
                var facilityMatrix = Matrix4x4.TRS(positionResolver(facility),
                    Quaternion.Euler(0f, rotationResolver(facility), 0f),
                    scaleResolver?.Invoke(facility) ?? Vector3.one);
                var batchRow = facility.GridRow /
                               plan.Budget.SpatialBatchEdgeCells;
                var batchColumn = facility.GridColumn /
                                  plan.Budget.SpatialBatchEdgeCells;
                foreach (var module in modules)
                {
                    var key = new BatchMaterialKey(batchRow, batchColumn,
                        module.MaterialId);
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = new BatchMaterialGroup(module.Material);
                        groups.Add(key, group);
                    }
                    else if (group.Material != module.Material)
                        throw new InvalidOperationException(
                            "Luoyang building batch material identity mismatch.");
                    group.Instances.Add(new CombineInstance
                    {
                        mesh = module.Mesh,
                        subMeshIndex = 0,
                        transform = facilityMatrix * module.LocalMatrix
                    });
                    sourceMeshes.Add(module.Mesh);
                    sourceModuleCount++;
                }
            }

            var combinedVertexCount = 0;
            foreach (var pair in groups.OrderBy(item => item.Key.BatchRow)
                         .ThenBy(item => item.Key.BatchColumn)
                         .ThenBy(item => item.Key.MaterialId,
                             StringComparer.Ordinal))
            {
                var mesh = new Mesh
                {
                    name = "HAN_LUOYANG_BATCH_" + pair.Key.BatchRow + "_" +
                           pair.Key.BatchColumn + "_" + pair.Key.MaterialId,
                    indexFormat = IndexFormat.UInt32
                };
                mesh.CombineMeshes(pair.Value.Instances.ToArray(), true, true,
                    false);
                mesh.RecalculateBounds();
                combinedVertexCount += mesh.vertexCount;
                _ownedMeshes.Add(mesh);

                var value = new GameObject(mesh.name);
                value.transform.SetParent(parent, false);
                value.isStatic = true;
                var filter = value.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = value.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Value.Material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.allowOcclusionWhenDynamic = true;
            }
            stopwatch.Stop();

            var rendererReduction = sourceModuleCount == 0 ? 0d :
                1d - groups.Count / (double)sourceModuleCount;
            var metrics = new LuoyangBuildingBatchMetrics
            {
                BudgetId = plan.Budget.BudgetId,
                FullCityFacilityCount = plan.Facilities.Count,
                FullCitySpatialBatchCount = plan.SpatialBatches.Count,
                ResidentFacilityCount = window.Facilities.Count,
                ResidentSpatialBatchCount = window.SpatialBatches.Count,
                SourceModuleRendererCount = sourceModuleCount,
                BuildingRendererBatchCount = groups.Count,
                CombinedMeshCount = _ownedMeshes.Count,
                UniqueSourceMeshCount = sourceMeshes.Count,
                CombinedVertexCount = combinedVertexCount,
                BatchBuildMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                RendererReductionRatio = rendererReduction,
                AllowsSpatialOcclusion = parent
                    .GetComponentsInChildren<MeshRenderer>()
                    .All(item => item.allowOcclusionWhenDynamic)
            };
            metrics.WithinBudget = enforceFrozenResidentBudget
                ? MeetsBudget(metrics, plan.Budget)
                : MeetsWholeCityBaseline(metrics, plan.Budget);
            if (!metrics.WithinBudget)
                throw new InvalidOperationException(
                    enforceFrozenResidentBudget
                        ? "Luoyang building batching exceeds the frozen performance budget."
                        : "Luoyang whole-city batching is incomplete.");
            return metrics;
        }

        public LuoyangBuildingBatchMetrics BuildWholeCity(Transform parent,
            LuoyangBuildingPerformancePlan plan,
            HanBuildableFacilityModelFactory factory,
            Func<LuoyangBuildingPerformanceFacility, Vector3> positionResolver,
            Func<LuoyangBuildingPerformanceFacility, float> rotationResolver,
            Func<LuoyangBuildingPerformanceFacility, Vector3> scaleResolver =
                null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var window = new LuoyangBuildingResidentWindow(
                LuoyangBuildingPerformanceBudgetIds.MinGridRow,
                LuoyangBuildingPerformanceBudgetIds.MinGridColumn,
                Math.Max(
                    LuoyangBuildingPerformanceBudgetIds.MaxGridRow -
                    LuoyangBuildingPerformanceBudgetIds.MinGridRow + 1,
                    LuoyangBuildingPerformanceBudgetIds.MaxGridColumn -
                    LuoyangBuildingPerformanceBudgetIds.MinGridColumn + 1),
                plan.Facilities, plan.SpatialBatches);
            return Build(parent, plan, window, factory, positionResolver,
                rotationResolver, scaleResolver, false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (var mesh in _ownedMeshes)
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            _ownedMeshes.Clear();
            _disposed = true;
        }

        public static bool MeetsBudget(LuoyangBuildingBatchMetrics metrics,
            LuoyangBuildingPerformanceBudgetCatalog budget)
        {
            if (metrics == null || budget == null) return false;
            return metrics.FullCityFacilityCount == budget.FacilityCount &&
                   metrics.FullCitySpatialBatchCount ==
                   budget.FullCitySpatialBatchCount &&
                   metrics.ResidentFacilityCount <=
                   budget.MaxResidentFacilityCount &&
                   metrics.ResidentSpatialBatchCount <=
                   budget.MaxResidentSpatialBatchCount &&
                   metrics.SourceModuleRendererCount >
                   metrics.BuildingRendererBatchCount &&
                   metrics.BuildingRendererBatchCount <=
                   budget.MaxBuildingRendererBatchCount &&
                   metrics.CombinedMeshCount ==
                   metrics.BuildingRendererBatchCount &&
                   metrics.CombinedVertexCount <= budget.MaxCombinedVertexCount &&
                   metrics.BatchBuildMilliseconds <=
                   budget.MaxBatchBuildMilliseconds &&
                   metrics.RendererReductionRatio + 0.000001d >=
                   budget.MinRendererReductionRatio &&
                   metrics.AllowsSpatialOcclusion;
        }

        public static bool MeetsWholeCityBaseline(
            LuoyangBuildingBatchMetrics metrics,
            LuoyangBuildingPerformanceBudgetCatalog budget)
        {
            if (metrics == null || budget == null) return false;
            return metrics.FullCityFacilityCount == budget.FacilityCount &&
                   metrics.ResidentFacilityCount == budget.FacilityCount &&
                   metrics.FullCitySpatialBatchCount ==
                   budget.FullCitySpatialBatchCount &&
                   metrics.ResidentSpatialBatchCount ==
                   budget.FullCitySpatialBatchCount &&
                   metrics.SourceModuleRendererCount >
                   metrics.BuildingRendererBatchCount &&
                   metrics.CombinedMeshCount ==
                   metrics.BuildingRendererBatchCount &&
                   metrics.RendererReductionRatio > 0d &&
                   metrics.AllowsSpatialOcclusion;
        }

        private readonly struct BatchMaterialKey : IEquatable<BatchMaterialKey>
        {
            public BatchMaterialKey(int batchRow, int batchColumn,
                string materialId)
            {
                BatchRow = batchRow;
                BatchColumn = batchColumn;
                MaterialId = materialId;
            }

            public int BatchRow { get; }
            public int BatchColumn { get; }
            public string MaterialId { get; }

            public bool Equals(BatchMaterialKey other) =>
                BatchRow == other.BatchRow &&
                BatchColumn == other.BatchColumn &&
                string.Equals(MaterialId, other.MaterialId,
                    StringComparison.Ordinal);

            public override bool Equals(object value) =>
                value is BatchMaterialKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = BatchRow;
                    hash = hash * 397 ^ BatchColumn;
                    hash = hash * 397 ^
                           StringComparer.Ordinal.GetHashCode(MaterialId ?? "");
                    return hash;
                }
            }
        }

        private sealed class BatchMaterialGroup
        {
            public BatchMaterialGroup(Material material)
            {
                Material = material ?? throw new ArgumentNullException(
                    nameof(material));
            }

            public Material Material { get; }
            public List<CombineInstance> Instances { get; } =
                new List<CombineInstance>();
        }
    }
}
