using System;
using UnityEngine;

namespace Mandate.Presentation
{
    internal sealed class ProceduralSilkMapArt : IDisposable
    {
        public static readonly Color SilkLight =
            new Color(0.89f, 0.82f, 0.63f, 1f);
        public static readonly Color SilkDark =
            new Color(0.64f, 0.51f, 0.31f, 1f);
        public static readonly Color Ink =
            new Color(0.18f, 0.14f, 0.10f, 1f);
        public static readonly Color Ochre =
            new Color(0.57f, 0.38f, 0.18f, 1f);
        public static readonly Color MineralGreen =
            new Color(0.28f, 0.42f, 0.25f, 1f);
        public static readonly Color MineralBlue =
            new Color(0.20f, 0.43f, 0.55f, 1f);
        public static readonly Color Cinnabar =
            new Color(0.62f, 0.18f, 0.12f, 1f);

        public Texture2D SilkTexture { get; }
        public Texture2D BrushStampTexture { get; }
        public Texture2D MountainStampTexture { get; }
        public Texture2D SealTexture { get; }

        public ProceduralSilkMapArt()
        {
            SilkTexture = CreateSilkTexture(128);
            BrushStampTexture = CreateBrushStampTexture(96);
            MountainStampTexture = CreateMountainStampTexture(128, 72);
            SealTexture = CreateSealTexture(64);
        }

        public void Dispose()
        {
            DestroyTexture(SilkTexture);
            DestroyTexture(BrushStampTexture);
            DestroyTexture(MountainStampTexture);
            DestroyTexture(SealTexture);
        }

        private static Texture2D CreateSilkTexture(int size)
        {
            var texture = CreateTexture(size, size, TextureWrapMode.Repeat);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var noise = Hash01(x, y);
                    var weave = ((x + y) % 7 == 0 ? 7 : 0) +
                                (x % 11 == 0 ? 5 : 0);
                    var variation = (int)(noise * 12f) - 6 + weave;
                    pixels[y * size + x] = new Color32(
                        ClampByte(211 + variation),
                        ClampByte(191 + variation),
                        ClampByte(142 + variation / 2),
                        255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateBrushStampTexture(int size)
        {
            var texture = CreateTexture(size, size, TextureWrapMode.Clamp);
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = (x - center) / center;
                    var ny = (y - center) / center;
                    var radius = Mathf.Sqrt(nx * nx + ny * ny * 1.45f);
                    var bristle = Hash01(x / 3, y / 3) * 0.18f;
                    var alpha = Mathf.Clamp01((1.04f - radius - bristle) * 5f);
                    pixels[y * size + x] =
                        new Color32(255, 255, 255, (byte)(alpha * 210f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateMountainStampTexture(int width, int height)
        {
            var texture = CreateTexture(width, height, TextureWrapMode.Clamp);
            var pixels = new Color32[width * height];
            var left = new Vector2(5f, 61f);
            var firstPeak = new Vector2(42f, 9f);
            var valley = new Vector2(66f, 47f);
            var secondPeak = new Vector2(88f, 20f);
            var right = new Vector2(123f, 62f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = new Vector2(x, y);
                    var distance = Mathf.Min(
                        DistanceToSegment(point, left, firstPeak),
                        Mathf.Min(
                            DistanceToSegment(point, firstPeak, valley),
                            Mathf.Min(
                                DistanceToSegment(point, valley, secondPeak),
                                DistanceToSegment(point, secondPeak, right))));
                    var ridgeAlpha = Mathf.Clamp01((3.2f - distance) / 2.2f);
                    var fillLine =
                        y > Mathf.Lerp(61f, 9f, Mathf.Clamp01((x - 5f) / 37f)) &&
                        x < 66f;
                    var fill = fillLine
                        ? Mathf.Clamp01((y - 20f) / 80f) * 0.18f
                        : 0f;
                    var alpha = Mathf.Max(ridgeAlpha, fill);
                    pixels[y * width + x] =
                        new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateSealTexture(int size)
        {
            var texture = CreateTexture(size, size, TextureWrapMode.Clamp);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var edge = Mathf.Min(
                        Mathf.Min(x, size - 1 - x),
                        Mathf.Min(y, size - 1 - y));
                    var roughness = Hash01(x / 2, y / 2) * 2.2f;
                    var border = edge > 3f + roughness && edge < 10f + roughness;
                    var cornerCut =
                        (x + y < 8) ||
                        (x + (size - 1 - y) < 8) ||
                        ((size - 1 - x) + y < 8) ||
                        ((size - 1 - x) + (size - 1 - y) < 8);
                    var alpha = border && !cornerCut ? 255 : 0;
                    pixels[y * size + x] =
                        new Color32(255, 255, 255, (byte)alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateTexture(
            int width,
            int height,
            TextureWrapMode wrapMode)
        {
            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = wrapMode,
                hideFlags = HideFlags.HideAndDontSave
            };
            return texture;
        }

        private static float DistanceToSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.001f)
            {
                return Vector2.Distance(point, start);
            }

            var progress = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * progress);
        }

        private static float Hash01(int x, int y)
        {
            unchecked
            {
                var hash = x * 374761393 + y * 668265263;
                hash = (hash ^ (hash >> 13)) * 1274126177;
                return ((hash ^ (hash >> 16)) & 0x7fffffff) /
                       (float)int.MaxValue;
            }
        }

        private static byte ClampByte(int value)
        {
            return (byte)Mathf.Clamp(value, 0, 255);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
