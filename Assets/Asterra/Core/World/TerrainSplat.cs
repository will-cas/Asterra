using UnityEngine;

namespace Asterra.Core.World
{
    /// <summary>
    /// Presentation splat weights (R grass, G dirt, B rock, A sand). Not part of lockstep.
    /// </summary>
    public static class TerrainSplat
    {
        public const string Grass = "grass";
        public const string Dirt = "dirt";
        public const string Rock = "rock";
        public const string Sand = "sand";

        public static ushort DefIndexForLayer(string layer)
        {
            if (layer == Dirt)
                return DefaultTerrainCatalog.Mud;
            if (layer == Rock)
                return DefaultTerrainCatalog.Rubble;
            if (layer == Sand)
                return DefaultTerrainCatalog.Beach;
            return DefaultTerrainCatalog.GrassShort;
        }

        public static Color LayerWeights(string layer)
        {
            if (layer == Dirt)
                return new Color(0f, 1f, 0f, 0f);
            if (layer == Rock)
                return new Color(0f, 0f, 1f, 0f);
            if (layer == Sand)
                return new Color(0f, 0f, 0f, 1f);
            return new Color(1f, 0f, 0f, 0f);
        }

        public static Color32 PreviewTint(string layer)
        {
            if (layer == Dirt)
                return new Color32(140, 95, 45, 255);
            if (layer == Rock)
                return new Color32(130, 128, 122, 255);
            if (layer == Sand)
                return new Color32(210, 190, 130, 255);
            return new Color32(70, 130, 55, 255);
        }

        public static Color WeightsFor(TerrainCategory category, ushort defIndex)
        {
            if (defIndex == DefaultTerrainCatalog.Road)
                return Normalize(new Color(0.25f, 0.7f, 0.05f, 0f));
            if (defIndex == DefaultTerrainCatalog.Mud)
                return Normalize(new Color(0.1f, 0.85f, 0.05f, 0f));
            if (defIndex == DefaultTerrainCatalog.Rubble)
                return Normalize(new Color(0.05f, 0.15f, 0.8f, 0f));
            if (defIndex == DefaultTerrainCatalog.Snow)
                return Normalize(new Color(0.05f, 0.05f, 0.15f, 0.75f));
            if (defIndex == DefaultTerrainCatalog.Scorched)
                return Normalize(new Color(0.15f, 0.75f, 0.1f, 0f));
            if (defIndex == DefaultTerrainCatalog.Berm)
                return Normalize(new Color(0.35f, 0.55f, 0.1f, 0f));
            if (defIndex == DefaultTerrainCatalog.Debris)
                return Normalize(new Color(0.05f, 0.25f, 0.7f, 0f));

            switch (category)
            {
                case TerrainCategory.GrassBare:
                    return Normalize(new Color(0.35f, 0.6f, 0.05f, 0f));
                case TerrainCategory.GrassShort:
                    return Normalize(new Color(0.82f, 0.18f, 0f, 0f));
                case TerrainCategory.GrassLong:
                    return Normalize(new Color(0.95f, 0.05f, 0f, 0f));
                case TerrainCategory.Rock:
                    return Normalize(new Color(0.05f, 0.15f, 0.8f, 0f));
                case TerrainCategory.Swamp:
                    return Normalize(new Color(0.35f, 0.6f, 0.05f, 0f));
                case TerrainCategory.Forest:
                case TerrainCategory.Tree:
                    return Normalize(new Color(0.7f, 0.25f, 0.05f, 0f));
                case TerrainCategory.Beach:
                    return Normalize(new Color(0.05f, 0.1f, 0.05f, 0.8f));
                case TerrainCategory.Mountain:
                    return Normalize(new Color(0.05f, 0.1f, 0.85f, 0f));
                case TerrainCategory.Hill:
                    return Normalize(new Color(0.5f, 0.2f, 0.3f, 0f));
                case TerrainCategory.Trench:
                    return Normalize(new Color(0.1f, 0.8f, 0.1f, 0f));
                case TerrainCategory.Gap:
                case TerrainCategory.NoEntry:
                    return Normalize(new Color(0.05f, 0.2f, 0.75f, 0f));
                case TerrainCategory.Ice:
                    return Normalize(new Color(0.05f, 0.05f, 0.2f, 0.7f));
                default:
                    if (category == TerrainCategory.WaterRiver
                        || category == TerrainCategory.WaterLake
                        || category == TerrainCategory.WaterOcean
                        || category == TerrainCategory.WaterWaterfall)
                        return Normalize(new Color(0.05f, 0.35f, 0.1f, 0.5f));
                    return Normalize(new Color(0.75f, 0.2f, 0.05f, 0f));
            }
        }

        public static Color Overlay(Color current, string layer, float strength)
        {
            strength = Mathf.Clamp01(strength);
            return Normalize(Color.Lerp(current, LayerWeights(layer), strength));
        }

        public static void ApplyStrokes(
            Color[] cells,
            int width,
            int height,
            float originX,
            float originZ,
            float cellSize,
            MapTexturePaint[] strokes)
        {
            if (cells == null || strokes == null || cellSize <= 0.01f)
                return;
            for (int i = 0; i < strokes.Length; i++)
                Stamp(cells, width, height, originX, originZ, cellSize, strokes[i]);
        }

        public static void Stamp(
            Color[] cells,
            int width,
            int height,
            float originX,
            float originZ,
            float cellSize,
            MapTexturePaint paint)
        {
            if (paint == null || cells == null)
                return;

            string layer = string.IsNullOrEmpty(paint.layer) ? Grass : paint.layer.ToLowerInvariant();
            float strength = paint.strength > 0.01f ? Mathf.Clamp01(paint.strength) : 0.85f;
            string shape = string.IsNullOrEmpty(paint.shape) ? "disk" : paint.shape.ToLowerInvariant();

            float minX;
            float minZ;
            float maxX;
            float maxZ;
            bool disk = shape != "rect";
            float cx = paint.x;
            float cz = paint.z;
            float radius = paint.radius > 0.5f ? paint.radius : 16f;
            if (disk)
            {
                minX = cx - radius;
                minZ = cz - radius;
                maxX = cx + radius;
                maxZ = cz + radius;
            }
            else
            {
                minX = paint.minX;
                minZ = paint.minZ;
                maxX = paint.maxX;
                maxZ = paint.maxZ;
            }

            int x0 = Mathf.Clamp(Mathf.FloorToInt((minX - originX) / cellSize), 0, width - 1);
            int x1 = Mathf.Clamp(Mathf.FloorToInt((maxX - originX) / cellSize), 0, width - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((minZ - originZ) / cellSize), 0, height - 1);
            int z1 = Mathf.Clamp(Mathf.FloorToInt((maxZ - originZ) / cellSize), 0, height - 1);
            if (x0 > x1)
                (x0, x1) = (x1, x0);
            if (z0 > z1)
                (z0, z1) = (z1, z0);

            float r2 = radius * radius;
            for (int gz = z0; gz <= z1; gz++)
            {
                for (int gx = x0; gx <= x1; gx++)
                {
                    float wx = originX + (gx + 0.5f) * cellSize;
                    float wz = originZ + (gz + 0.5f) * cellSize;
                    if (disk)
                    {
                        float dx = wx - cx;
                        float dz = wz - cz;
                        if (dx * dx + dz * dz > r2)
                            continue;
                    }

                    int idx = gz * width + gx;
                    cells[idx] = Overlay(cells[idx], layer, strength);
                }
            }
        }

        public static int HashStrokes(MapTexturePaint[] strokes)
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
                    hash = (hash * 16777619) ^ s.layer?.GetHashCode() ?? 0;
                    hash = (hash * 16777619) ^ s.x.GetHashCode();
                    hash = (hash * 16777619) ^ s.z.GetHashCode();
                    hash = (hash * 16777619) ^ s.radius.GetHashCode();
                    hash = (hash * 16777619) ^ s.minX.GetHashCode();
                    hash = (hash * 16777619) ^ s.maxZ.GetHashCode();
                }

                return hash;
            }
        }

        public static Color Normalize(Color w)
        {
            float s = w.r + w.g + w.b + w.a;
            if (s < 0.0001f)
                return new Color(1f, 0f, 0f, 0f);
            float inv = 1f / s;
            return new Color(w.r * inv, w.g * inv, w.b * inv, w.a * inv);
        }
    }
}
