using System;

namespace Asterra.Core.World
{
    /// <summary>Applies authored height disks onto a cell heightfield.</summary>
    public static class MapHeightSculpt
    {
        public static int HashStrokes(MapHeightPaint[] strokes)
        {
            if (strokes == null || strokes.Length == 0)
                return 0;
            unchecked
            {
                int hash = strokes.Length * 397;
                for (int i = 0; i < strokes.Length; i++)
                {
                    var s = strokes[i];
                    if (s == null)
                        continue;
                    hash = (hash * 16777619) ^ s.x.GetHashCode();
                    hash = (hash * 16777619) ^ s.z.GetHashCode();
                    hash = (hash * 16777619) ^ s.radius.GetHashCode();
                    hash = (hash * 16777619) ^ s.delta.GetHashCode();
                    hash = (hash * 16777619) ^ s.falloff.GetHashCode();
                }

                return hash;
            }
        }

        public static void Apply(
            float[] heights,
            int width,
            int height,
            float originX,
            float originZ,
            float cellSize,
            MapHeightPaint[] strokes)
        {
            if (heights == null || strokes == null || strokes.Length == 0)
                return;
            float cell = cellSize > 0.01f ? cellSize : 10f;
            for (int i = 0; i < strokes.Length; i++)
            {
                var stroke = strokes[i];
                if (stroke == null)
                    continue;
                Stamp(heights, width, height, originX, originZ, cell, stroke);
            }
        }

        private static void Stamp(
            float[] heights,
            int width,
            int height,
            float originX,
            float originZ,
            float cellSize,
            MapHeightPaint stroke)
        {
            float radius = stroke.radius > 0.5f ? stroke.radius : 16f;
            float r2 = radius * radius;
            float falloff = stroke.falloff < 0f ? 0f : (stroke.falloff > 1f ? 1f : stroke.falloff);
            int x0 = Clamp(FloorToInt((stroke.x - radius - originX) / cellSize), 0, width - 1);
            int x1 = Clamp(FloorToInt((stroke.x + radius - originX) / cellSize), 0, width - 1);
            int z0 = Clamp(FloorToInt((stroke.z - radius - originZ) / cellSize), 0, height - 1);
            int z1 = Clamp(FloorToInt((stroke.z + radius - originZ) / cellSize), 0, height - 1);
            for (int cz = z0; cz <= z1; cz++)
            {
                for (int cx = x0; cx <= x1; cx++)
                {
                    float wx = originX + (cx + 0.5f) * cellSize;
                    float wz = originZ + (cz + 0.5f) * cellSize;
                    float dx = wx - stroke.x;
                    float dz = wz - stroke.z;
                    float d2 = dx * dx + dz * dz;
                    if (d2 > r2)
                        continue;
                    float t = 1f;
                    if (falloff > 0.001f && radius > 0.01f)
                    {
                        float u = (float)Math.Sqrt(d2) / radius;
                        float s = (float)Math.Cos(u * Math.PI * 0.5);
                        t = 1f - falloff + falloff * (s * s);
                    }

                    heights[cz * width + cx] += stroke.delta * t;
                }
            }
        }

        private static int FloorToInt(float v) => (int)Math.Floor(v);

        private static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
