using System;
using System.Collections.Generic;
using Asterra.Core.World;
using UnityEngine;

namespace Asterra.Gameplay.Content
{
    /// <summary>
    /// Runtime lobby/minimap-style preview of a skirmish map (terrain stamp + keep seats).
    /// </summary>
    public static class MapPreviewBuilder
    {
        public const int Resolution = 90;
        public const float Half = 450f;
        public const float Cell = 10f;

        public readonly struct KeepMarker
        {
            public readonly int SeatIndex;
            public readonly float X;
            public readonly float Z;

            public KeepMarker(int seatIndex, float x, float z)
            {
                SeatIndex = seatIndex;
                X = x;
                Z = z;
            }
        }

        public static Texture2D Build(string mapKey)
        {
            var def = ResolveDefinition(mapKey);
            return Build(def);
        }

        public static Texture2D Build(MapDefinition map)
        {
            if (map == null)
                map = BuiltinPreview(SkirmishMapId.BlackridgePass);
            map.EnsureArrays();

            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "AsterraMapPreview",
            };

            var cells = new ushort[Resolution * Resolution];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = map.defaultTerrain;

            for (int i = 0; i < map.terrain.Length; i++)
                StampPaint(cells, map.terrain[i]);

            var pixels = new Color32[Resolution * Resolution];
            for (int i = 0; i < cells.Length; i++)
                pixels[i] = TerrainColor(cells[i]);

            if (map.texturePaint != null)
            {
                for (int i = 0; i < map.texturePaint.Length; i++)
                    StampTextureOverlay(pixels, map.texturePaint[i]);
            }

            for (int i = 0; i < map.blocked.Length; i++)
            {
                var b = map.blocked[i];
                ForCellsInRect(b.minX, b.minZ, b.maxX, b.maxZ, (cx, cz) =>
                {
                    int idx = cz * Resolution + cx;
                    var c = pixels[idx];
                    pixels[idx] = new Color32(
                        (byte)(c.r * 0.4f), (byte)(c.g * 0.4f), (byte)(c.b * 0.4f), 255);
                });
            }

            tex.SetPixels32(pixels);
            tex.Apply(false);
            return tex;
        }

        public static IReadOnlyList<KeepMarker> GetKeepMarkers(string mapKey)
        {
            var def = ResolveDefinition(mapKey);
            def.EnsureArrays();
            if (def.keeps != null && def.keeps.Length > 0)
            {
                var list = new List<KeepMarker>(def.keeps.Length);
                for (int i = 0; i < def.keeps.Length; i++)
                {
                    var k = def.keeps[i];
                    list.Add(new KeepMarker(k.seatIndex, k.x, k.z));
                }

                list.Sort((a, b) => a.SeatIndex.CompareTo(b.SeatIndex));
                return list;
            }

            return Array.Empty<KeepMarker>();
        }

        public static void WorldToPreviewGui(
            Rect previewRect,
            float worldX,
            float worldZ,
            out float guiX,
            out float guiY)
        {
            float u = (worldX + Half) / (Half * 2f);
            float v = (worldZ + Half) / (Half * 2f);
            guiX = previewRect.x + u * previewRect.width;
            // Texture v=0 is bottom; GUI y grows downward — flip.
            guiY = previewRect.yMax - v * previewRect.height;
        }

        public static bool TryHitSeat(
            Rect previewRect,
            Vector2 mouseGui,
            string mapKey,
            float hitRadiusPx,
            out int seatIndex)
        {
            seatIndex = 0;
            var keeps = GetKeepMarkers(mapKey);
            float best = hitRadiusPx * hitRadiusPx;
            bool hit = false;
            for (int i = 0; i < keeps.Count; i++)
            {
                WorldToPreviewGui(previewRect, keeps[i].X, keeps[i].Z, out float gx, out float gy);
                float dx = mouseGui.x - gx;
                float dy = mouseGui.y - gy;
                float d2 = dx * dx + dy * dy;
                if (d2 <= best)
                {
                    best = d2;
                    seatIndex = keeps[i].SeatIndex;
                    hit = true;
                }
            }

            return hit;
        }

        public static MapDefinition ResolveDefinition(string mapKey)
        {
            if (MapCatalog.TryLoad(mapKey, out var custom) && custom != null)
                return custom;
            if (MapCatalog.TryParseBuiltin(mapKey, out var builtin))
                return BuiltinPreview(builtin);
            return BuiltinPreview(SkirmishMapId.BlackridgePass);
        }

        public static MapDefinition BuiltinPreview(SkirmishMapId id)
        {
            switch (id)
            {
                case SkirmishMapId.TwinKeeps:
                    return TwinKeepsDef();
                case SkirmishMapId.RiverCrossing:
                    return RiverCrossingDef();
                default:
                    return BlackridgePassDef();
            }
        }

        private static MapDefinition TwinKeepsDef()
        {
            var def = BaseDef("twin_keeps", "Twin Keeps", -320f, 0f);
            def.keeps = new[]
            {
                new MapKeepSpawn { seatIndex = 0, x = -350f, z = 0f },
                new MapKeepSpawn { seatIndex = 1, x = 350f, z = 0f },
            };
            def.terrain = new[]
            {
                Rect(-380, -40, -320, 40, DefaultTerrainCatalog.GrassBare),
                Rect(320, -40, 380, 40, DefaultTerrainCatalog.GrassBare),
                Rect(-200, 80, 200, 160, DefaultTerrainCatalog.GrassLong),
                Rect(-200, -160, 200, -80, DefaultTerrainCatalog.GrassLong),
                Rect(-140, -110, -60, -40, DefaultTerrainCatalog.Forest),
                Rect(60, 40, 140, 110, DefaultTerrainCatalog.Forest),
                Rect(-60, 40, -30, 70, DefaultTerrainCatalog.Rock),
                Rect(30, -70, 60, -40, DefaultTerrainCatalog.Rock),
                Rect(-30, -50, 30, -20, DefaultTerrainCatalog.Swamp),
                Rect(-220, -30, -180, 30, DefaultTerrainCatalog.Hill),
                Rect(180, -30, 220, 30, DefaultTerrainCatalog.Hill),
                Rect(-50, -50, 50, 50, DefaultTerrainCatalog.GrassShort),
            };
            return def;
        }

        private static MapDefinition RiverCrossingDef()
        {
            var def = BaseDef("river_crossing", "River Crossing", -280f, -200f);
            def.keeps = new[]
            {
                new MapKeepSpawn { seatIndex = 0, x = -300f, z = -220f },
                new MapKeepSpawn { seatIndex = 1, x = 300f, z = 220f },
            };
            def.terrain = new[]
            {
                Rect(-450, -28, 450, 28, DefaultTerrainCatalog.WaterDeep),
                Rect(-35, -28, 35, 28, DefaultTerrainCatalog.WaterShallow),
                Rect(-150, -28, -110, 28, DefaultTerrainCatalog.WaterShallow),
                Rect(110, -28, 150, 28, DefaultTerrainCatalog.WaterShallow),
                Rect(-380, -48, 380, -28, DefaultTerrainCatalog.Beach),
                Rect(-380, 28, 380, 48, DefaultTerrainCatalog.Beach),
                Rect(-200, -160, -120, -70, DefaultTerrainCatalog.Forest),
                Rect(120, 70, 200, 160, DefaultTerrainCatalog.Forest),
            };
            return def;
        }

        private static MapDefinition BlackridgePassDef()
        {
            var def = BaseDef("blackridge_pass", "Blackridge Pass", -330f, 0f);
            def.keeps = new[]
            {
                new MapKeepSpawn { seatIndex = 0, x = -360f, z = 0f },
                new MapKeepSpawn { seatIndex = 1, x = 360f, z = 0f },
            };
            def.terrain = new[]
            {
                Rect(-80, -200, 80, 200, DefaultTerrainCatalog.Mountain),
                Rect(-40, -40, 40, 40, DefaultTerrainCatalog.GrassBare),
                Rect(-450, -60, -200, 60, DefaultTerrainCatalog.GrassShort),
                Rect(200, -60, 450, 60, DefaultTerrainCatalog.GrassShort),
                Rect(-120, -80, -60, 80, DefaultTerrainCatalog.Hill),
                Rect(60, -80, 120, 80, DefaultTerrainCatalog.Hill),
                Rect(-200, 100, -80, 180, DefaultTerrainCatalog.Forest),
                Rect(80, -180, 200, -100, DefaultTerrainCatalog.Forest),
            };
            return def;
        }

        private static MapDefinition BaseDef(string id, string name, float camX, float camZ)
        {
            return new MapDefinition
            {
                id = id,
                displayName = name,
                defaultTerrain = DefaultTerrainCatalog.GrassShort,
                cameraFocusX = camX,
                cameraFocusZ = camZ,
            };
        }

        private static MapTerrainPaint Rect(float minX, float minZ, float maxX, float maxZ, ushort terrain)
        {
            return new MapTerrainPaint
            {
                shape = "rect",
                minX = minX,
                minZ = minZ,
                maxX = maxX,
                maxZ = maxZ,
                terrainIndex = terrain,
            };
        }

        private static void StampTextureOverlay(Color32[] pixels, MapTexturePaint paint)
        {
            if (paint == null || pixels == null)
                return;
            var tint = TerrainSplat.PreviewTint(paint.layer);
            float radius = paint.radius > 0.5f ? paint.radius : 16f;
            bool disk = string.IsNullOrEmpty(paint.shape) || paint.shape.ToLowerInvariant() != "rect";
            float minX = disk ? paint.x - radius : paint.minX;
            float minZ = disk ? paint.z - radius : paint.minZ;
            float maxX = disk ? paint.x + radius : paint.maxX;
            float maxZ = disk ? paint.z + radius : paint.maxZ;
            float r2 = radius * radius;
            ForCellsInRect(minX, minZ, maxX, maxZ, (cx, cz) =>
            {
                if (disk)
                {
                    float wx = -Half + (cx + 0.5f) * Cell;
                    float wz = -Half + (cz + 0.5f) * Cell;
                    float dx = wx - paint.x;
                    float dz = wz - paint.z;
                    if (dx * dx + dz * dz > r2)
                        return;
                }

                int idx = cz * Resolution + cx;
                var c = pixels[idx];
                pixels[idx] = new Color32(
                    (byte)((c.r + tint.r) / 2),
                    (byte)((c.g + tint.g) / 2),
                    (byte)((c.b + tint.b) / 2),
                    255);
            });
        }

        private static void StampPaint(ushort[] cells, MapTerrainPaint paint)
        {
            if (paint == null)
                return;
            string shape = string.IsNullOrEmpty(paint.shape) ? "rect" : paint.shape.ToLowerInvariant();
            if (shape == "disk")
            {
                float r = paint.radius > 0.5f ? paint.radius : 10f;
                StampRect(cells, paint.x - r, paint.z - r, paint.x + r, paint.z + r, paint.terrainIndex);
                return;
            }

            StampRect(cells, paint.minX, paint.minZ, paint.maxX, paint.maxZ, paint.terrainIndex);
        }

        private static void StampRect(ushort[] cells, float minX, float minZ, float maxX, float maxZ, ushort def)
        {
            ForCellsInRect(minX, minZ, maxX, maxZ, (cx, cz) => { cells[cz * Resolution + cx] = def; });
        }

        private static void ForCellsInRect(float minX, float minZ, float maxX, float maxZ, Action<int, int> fn)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt((minX + Half) / Cell), 0, Resolution - 1);
            int x1 = Mathf.Clamp(Mathf.FloorToInt((maxX + Half) / Cell), 0, Resolution - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((minZ + Half) / Cell), 0, Resolution - 1);
            int z1 = Mathf.Clamp(Mathf.FloorToInt((maxZ + Half) / Cell), 0, Resolution - 1);
            if (x0 > x1) (x0, x1) = (x1, x0);
            if (z0 > z1) (z0, z1) = (z1, z0);
            for (int cz = z0; cz <= z1; cz++)
            {
                for (int cx = x0; cx <= x1; cx++)
                    fn(cx, cz);
            }
        }

        private static Color32 TerrainColor(ushort def)
        {
            switch (def)
            {
                case DefaultTerrainCatalog.GrassBare: return new Color32(160, 150, 90, 255);
                case DefaultTerrainCatalog.GrassShort: return new Color32(90, 140, 70, 255);
                case DefaultTerrainCatalog.GrassLong: return new Color32(60, 110, 50, 255);
                case DefaultTerrainCatalog.Rock: return new Color32(120, 120, 120, 255);
                case DefaultTerrainCatalog.Swamp: return new Color32(70, 90, 50, 255);
                case DefaultTerrainCatalog.Forest: return new Color32(30, 80, 40, 255);
                case DefaultTerrainCatalog.Tree: return new Color32(20, 60, 30, 255);
                case DefaultTerrainCatalog.Beach: return new Color32(210, 190, 130, 255);
                case DefaultTerrainCatalog.Mountain: return new Color32(90, 85, 80, 255);
                case DefaultTerrainCatalog.Hill: return new Color32(110, 130, 80, 255);
                case DefaultTerrainCatalog.WaterRiver:
                case DefaultTerrainCatalog.WaterShallow: return new Color32(70, 140, 190, 255);
                case DefaultTerrainCatalog.WaterDeep:
                case DefaultTerrainCatalog.WaterOcean: return new Color32(30, 70, 140, 255);
                case DefaultTerrainCatalog.WaterFast: return new Color32(50, 160, 200, 255);
                case DefaultTerrainCatalog.WaterLake: return new Color32(40, 100, 160, 255);
                case DefaultTerrainCatalog.WaterWaterfall: return new Color32(150, 200, 220, 255);
                case DefaultTerrainCatalog.IceThick:
                case DefaultTerrainCatalog.IceThin: return new Color32(200, 220, 240, 255);
                case DefaultTerrainCatalog.Trench: return new Color32(80, 60, 40, 255);
                case DefaultTerrainCatalog.NoEntry: return new Color32(20, 20, 20, 255);
                default: return new Color32(100, 100, 100, 255);
            }
        }
    }
}
