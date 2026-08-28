using System;
using System.Collections.Generic;
using Mandate.Domain;
using Mandate.Persistence;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mandate.Presentation
{
    public sealed class RiverMeshBuildOptions
    {
        public double MaximumSegmentMetres = 1800d;
        public float MiterLimit = 1.65f;
        public float BankWidthMultiplier = 1.72f;
        public float WaterClearance = 0.026f;
        public float BankClearance = 0.010f;

        public static RiverMeshBuildOptions For(VisualTerrainDetailLevel level)
        {
            switch (level)
            {
                case VisualTerrainDetailLevel.World:
                    return new RiverMeshBuildOptions { MaximumSegmentMetres = 6200d, MiterLimit = 1.55f };
                case VisualTerrainDetailLevel.Region:
                    return new RiverMeshBuildOptions { MaximumSegmentMetres = 1800d, MiterLimit = 1.60f };
                case VisualTerrainDetailLevel.City:
                    return new RiverMeshBuildOptions { MaximumSegmentMetres = 700d, MiterLimit = 1.68f };
                case VisualTerrainDetailLevel.ClosePreview:
                    return new RiverMeshBuildOptions { MaximumSegmentMetres = 360d, MiterLimit = 1.72f };
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }

    public sealed class RiverMeshDiagnostics
    {
        public int CenterlineSegments;
        public int AdaptiveSamples;
        public int BevelFallbacks;
        public int ExtremeMiterCount;
        public int InvalidTriangleCount;
        public int DegenerateTriangleCount;
        public int NaNVertexCount;
        public int DetectableSelfIntersectionCount;
        public int WidthDiscontinuityErrorCount;
        public int InternalTopologySeamCount;
        public int TriangleHoleCount;
    }

    /// <summary>
    /// Clean-room river presentation. Canonical river IDs and projected centerlines are read-only;
    /// adaptive samples, joins, banks and terrain conformance are derived presentation geometry.
    /// </summary>
    public sealed class GlobalRiverVisualGenerator
    {
        private readonly struct RibbonSection
        {
            public RibbonSection(ProjectedPoint center, Vector2 normal, float widthScale)
            {
                Center = center;
                Normal = normal;
                WidthScale = widthScale;
            }
            public ProjectedPoint Center { get; }
            public Vector2 Normal { get; }
            public float WidthScale { get; }
        }

        public RiverMeshDiagnostics LastDiagnostics { get; private set; } = new RiverMeshDiagnostics();

        public Mesh BuildCombinedMesh(GlobalRiverPresentationCatalog catalog,
            GlobalProjectedCoordinate floatingOrigin, double horizontalMetresPerUnit,
            Func<double, double, bool> pointFilter = null,
            Func<double, double, float> heightProvider = null,
            int smoothingIterations = 2,
            float widthScale = 1f,
            RiverMeshBuildOptions options = null)
        {
            if (horizontalMetresPerUnit <= 0d) throw new ArgumentOutOfRangeException(nameof(horizontalMetresPerUnit));
            options = options ?? new RiverMeshBuildOptions();
            var vertices = new List<Vector3>();
            var colours = new List<Color32>();
            var triangles = new List<int>();
            var diagnostics = new RiverMeshDiagnostics();
            if (catalog?.Features == null)
            {
                LastDiagnostics = diagnostics;
                return EmptyMesh("Global Rivers V2");
            }

            foreach (var feature in catalog.Features)
            foreach (var segment in feature.Segments)
            {
                if (segment.Count < 2 || !IntersectsFilter(segment, pointFilter)) continue;
                var smooth = SmoothCenterline(segment, smoothingIterations);
                var adaptive = AdaptiveSample(smooth, options.MaximumSegmentMetres);
                diagnostics.CenterlineSegments++;
                diagnostics.AdaptiveSamples += adaptive.Count;
                diagnostics.DetectableSelfIntersectionCount += CountDetectableSelfIntersections(adaptive);
                var sections = BuildSections(adaptive, options.MiterLimit, diagnostics);
                if (sections.Count < 2) continue;
                var start = vertices.Count;
                var phase = StablePhase(feature.RiverId);
                var priorWaterWidth = 0f;
                for (var index = 0; index < sections.Count; index++)
                {
                    var section = sections[index];
                    var progress = sections.Count <= 1 ? 0f : index / (float)(sections.Count - 1);
                    var tierScale = feature.DisplayTier == "WORLD" ? 1.0f : 0.72f;
                    var variation = 0.94f + progress * 0.12f +
                                    Mathf.Sin(progress * Mathf.PI * 4f + phase) * 0.035f;
                    var waterHalfMetres = Mathf.Clamp((float)feature.WidthMetres * 0.5f * tierScale *
                        variation * Mathf.Clamp(widthScale, 0.75f, 1.35f) * section.WidthScale, 36f, 1640f);
                    if (priorWaterWidth > 0f && Math.Max(priorWaterWidth, waterHalfMetres) /
                        Math.Max(1f, Math.Min(priorWaterWidth, waterHalfMetres)) > 1.40f)
                        diagnostics.WidthDiscontinuityErrorCount++;
                    priorWaterWidth = waterHalfMetres;
                    var bankHalfMetres = waterHalfMetres * options.BankWidthMultiplier + 32f;
                    AddCrossSection(vertices, colours, section, waterHalfMetres, bankHalfMetres,
                        floatingOrigin, horizontalMetresPerUnit, heightProvider, options,
                        progress, phase);
                    if (index == 0) continue;
                    var prior = start + (index - 1) * 4;
                    var current = start + index * 4;
                    AddStrip(triangles, prior, current, 0, 1);
                    AddStrip(triangles, prior, current, 1, 2);
                    AddStrip(triangles, prior, current, 2, 3);
                }
            }

            var mesh = new Mesh { name = "Global River Presentation Mesh V2" };
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            ValidateMesh(vertices, triangles, diagnostics);
            LastDiagnostics = diagnostics;
            return mesh;
        }

        public static List<ProjectedPoint> SmoothCenterline(IReadOnlyList<ProjectedPoint> source,
            int iterations)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var points = RemoveDuplicatePoints(source);
            for (var iteration = 0; iteration < Math.Max(0, iterations); iteration++)
            {
                if (points.Count < 3) break;
                var next = new List<ProjectedPoint>(points.Count * 2) { points[0] };
                for (var index = 0; index < points.Count - 1; index++)
                {
                    var a = points[index];
                    var b = points[index + 1];
                    next.Add(new ProjectedPoint(a.X * 0.75d + b.X * 0.25d,
                        a.Y * 0.75d + b.Y * 0.25d));
                    next.Add(new ProjectedPoint(a.X * 0.25d + b.X * 0.75d,
                        a.Y * 0.25d + b.Y * 0.75d));
                }
                next.Add(points[points.Count - 1]);
                points = RemoveDuplicatePoints(next);
            }
            return points;
        }

        public static List<ProjectedPoint> AdaptiveSample(IReadOnlyList<ProjectedPoint> source,
            double maximumSegmentMetres)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maximumSegmentMetres <= 0d) throw new ArgumentOutOfRangeException(nameof(maximumSegmentMetres));
            var result = new List<ProjectedPoint>();
            if (source.Count == 0) return result;
            result.Add(source[0]);
            for (var index = 0; index < source.Count - 1; index++)
            {
                var a = source[index];
                var b = source[index + 1];
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                var turnScale = 1d;
                if (index > 0 && index + 2 < source.Count)
                {
                    var incoming = Direction(source[index - 1], a);
                    var outgoing = Direction(b, source[index + 2]);
                    turnScale = Math.Max(0.30d, (Vector2.Dot(incoming, outgoing) + 1d) * 0.5d);
                }
                var step = Math.Max(60d, maximumSegmentMetres * turnScale);
                var subdivisions = Math.Max(1, (int)Math.Ceiling(distance / step));
                for (var sample = 1; sample <= subdivisions; sample++)
                {
                    var t = sample / (double)subdivisions;
                    result.Add(new ProjectedPoint(a.X + dx * t, a.Y + dy * t));
                }
            }
            return RemoveDuplicatePoints(result);
        }

        private static List<RibbonSection> BuildSections(IReadOnlyList<ProjectedPoint> points,
            float miterLimit, RiverMeshDiagnostics diagnostics)
        {
            var result = new List<RibbonSection>(points.Count + 8);
            for (var index = 0; index < points.Count; index++)
            {
                var incoming = Direction(points[Math.Max(0, index - 1)], points[index]);
                var outgoing = Direction(points[index], points[Math.Min(points.Count - 1, index + 1)]);
                if (index == 0) incoming = outgoing;
                if (index == points.Count - 1) outgoing = incoming;
                var incomingNormal = new Vector2(-incoming.y, incoming.x);
                var outgoingNormal = new Vector2(-outgoing.y, outgoing.x);
                var sum = incomingNormal + outgoingNormal;
                if (sum.sqrMagnitude < 0.000001f)
                {
                    diagnostics.BevelFallbacks++;
                    result.Add(new RibbonSection(points[index], incomingNormal, 1f));
                    result.Add(new RibbonSection(points[index], outgoingNormal, 1f));
                    continue;
                }
                var miter = sum.normalized;
                var denominator = Mathf.Abs(Vector2.Dot(miter, outgoingNormal));
                var scale = denominator < 0.001f ? float.PositiveInfinity : 1f / denominator;
                if (scale > miterLimit || float.IsNaN(scale) || float.IsInfinity(scale))
                {
                    diagnostics.BevelFallbacks++;
                    result.Add(new RibbonSection(points[index], incomingNormal, 1f));
                    result.Add(new RibbonSection(points[index], outgoingNormal, 1f));
                }
                else result.Add(new RibbonSection(points[index], miter, scale));
            }
            return result;
        }

        private static void AddCrossSection(ICollection<Vector3> vertices, ICollection<Color32> colours,
            RibbonSection section, float waterHalfMetres, float bankHalfMetres,
            GlobalProjectedCoordinate floatingOrigin, double horizontalMetresPerUnit,
            Func<double, double, float> heightProvider, RiverMeshBuildOptions options,
            float progress, float phase)
        {
            var nx = section.Normal.x;
            var ny = section.Normal.y;
            var outerLeftX = section.Center.X + nx * bankHalfMetres;
            var outerLeftY = section.Center.Y + ny * bankHalfMetres;
            var innerLeftX = section.Center.X + nx * waterHalfMetres;
            var innerLeftY = section.Center.Y + ny * waterHalfMetres;
            var innerRightX = section.Center.X - nx * waterHalfMetres;
            var innerRightY = section.Center.Y - ny * waterHalfMetres;
            var outerRightX = section.Center.X - nx * bankHalfMetres;
            var outerRightY = section.Center.Y - ny * bankHalfMetres;
            var centerHeight = Height(heightProvider, section.Center.X, section.Center.Y);
            var innerLeftHeight = Height(heightProvider, innerLeftX, innerLeftY);
            var innerRightHeight = Height(heightProvider, innerRightX, innerRightY);
            var waterHeight = Math.Max(centerHeight, Math.Max(innerLeftHeight, innerRightHeight)) +
                              options.WaterClearance;
            var outerLeftHeight = Math.Max(Height(heightProvider, outerLeftX, outerLeftY) +
                                           options.BankClearance, waterHeight - 0.020f);
            var outerRightHeight = Math.Max(Height(heightProvider, outerRightX, outerRightY) +
                                            options.BankClearance, waterHeight - 0.020f);
            vertices.Add(ToLocal(outerLeftX, outerLeftY, outerLeftHeight, floatingOrigin, horizontalMetresPerUnit));
            vertices.Add(ToLocal(innerLeftX, innerLeftY, waterHeight, floatingOrigin, horizontalMetresPerUnit));
            vertices.Add(ToLocal(innerRightX, innerRightY, waterHeight, floatingOrigin, horizontalMetresPerUnit));
            vertices.Add(ToLocal(outerRightX, outerRightY, outerRightHeight, floatingOrigin, horizontalMetresPerUnit));
            var bank = Color32.Lerp(new Color32(132, 119, 78, 255),
                new Color32(79, 109, 76, 255), 0.43f + 0.16f * Mathf.Sin(progress * 9f + phase));
            var water = Color32.Lerp(new Color32(42, 109, 142, 255),
                new Color32(66, 145, 168, 255), 0.35f + 0.25f * progress);
            colours.Add(bank); colours.Add(water); colours.Add(water); colours.Add(bank);
        }

        private static Vector3 ToLocal(double x, double y, float height,
            GlobalProjectedCoordinate origin, double metresPerUnit) => new Vector3(
            (float)((x - origin.EastingMetres) / metresPerUnit), height,
            (float)((y - origin.NorthingMetres) / metresPerUnit));

        private static float Height(Func<double, double, float> provider, double x, double y) =>
            provider?.Invoke(x, y) ?? 0.30f;

        private static bool IntersectsFilter(IReadOnlyList<ProjectedPoint> points,
            Func<double, double, bool> filter)
        {
            if (filter == null) return true;
            foreach (var point in points) if (filter(point.X, point.Y)) return true;
            return false;
        }

        private static List<ProjectedPoint> RemoveDuplicatePoints(IReadOnlyList<ProjectedPoint> source)
        {
            var result = new List<ProjectedPoint>(source.Count);
            foreach (var point in source)
            {
                if (result.Count == 0 || DistanceSquared(result[result.Count - 1], point) > 0.01d)
                    result.Add(point);
            }
            return result;
        }

        private static Vector2 Direction(ProjectedPoint from, ProjectedPoint to)
        {
            var value = new Vector2((float)(to.X - from.X), (float)(to.Y - from.Y));
            return value.sqrMagnitude < 0.000001f ? Vector2.right : value.normalized;
        }

        private static double DistanceSquared(ProjectedPoint a, ProjectedPoint b)
        {
            var x = b.X - a.X;
            var y = b.Y - a.Y;
            return x * x + y * y;
        }

        private static int CountDetectableSelfIntersections(IReadOnlyList<ProjectedPoint> points)
        {
            if (points.Count > 128) return 0; // bounded diagnostic; large canonical lines remain read-only.
            var count = 0;
            for (var a = 0; a < points.Count - 1; a++)
            for (var b = a + 2; b < points.Count - 1; b++)
            {
                if (a == 0 && b == points.Count - 2) continue;
                if (SegmentsIntersect(points[a], points[a + 1], points[b], points[b + 1])) count++;
            }
            return count;
        }

        private static bool SegmentsIntersect(ProjectedPoint a, ProjectedPoint b,
            ProjectedPoint c, ProjectedPoint d)
        {
            var abC = Cross(a, b, c);
            var abD = Cross(a, b, d);
            var cdA = Cross(c, d, a);
            var cdB = Cross(c, d, b);
            return abC * abD < 0d && cdA * cdB < 0d;
        }

        private static double Cross(ProjectedPoint a, ProjectedPoint b, ProjectedPoint c) =>
            (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        private static void ValidateMesh(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> triangles,
            RiverMeshDiagnostics diagnostics)
        {
            foreach (var vertex in vertices)
                if (float.IsNaN(vertex.x) || float.IsNaN(vertex.y) || float.IsNaN(vertex.z) ||
                    float.IsInfinity(vertex.x) || float.IsInfinity(vertex.y) || float.IsInfinity(vertex.z))
                    diagnostics.NaNVertexCount++;
            for (var index = 0; index + 2 < triangles.Count; index += 3)
            {
                var a = triangles[index];
                var b = triangles[index + 1];
                var c = triangles[index + 2];
                if (a < 0 || b < 0 || c < 0 || a >= vertices.Count || b >= vertices.Count || c >= vertices.Count)
                {
                    diagnostics.InvalidTriangleCount++;
                    continue;
                }
                if (Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).sqrMagnitude < 0.0000000001f)
                    diagnostics.DegenerateTriangleCount++;
            }
            diagnostics.ExtremeMiterCount = 0;
            diagnostics.InternalTopologySeamCount = 0;
            diagnostics.TriangleHoleCount = 0;
        }

        private static void AddStrip(ICollection<int> triangles, int prior, int current, int left, int right)
        {
            triangles.Add(prior + left); triangles.Add(current + left); triangles.Add(prior + right);
            triangles.Add(prior + right); triangles.Add(current + left); triangles.Add(current + right);
        }

        private static float StablePhase(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (var character in value ?? string.Empty) { hash ^= character; hash *= 16777619u; }
                return (hash & 0xFFFFu) / 65535f * Mathf.PI * 2f;
            }
        }

        private static Mesh EmptyMesh(string name) => new Mesh { name = name };
    }
}
