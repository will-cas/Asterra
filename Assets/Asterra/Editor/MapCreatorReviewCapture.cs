using System.IO;
using Asterra.Core.World;
using UnityEditor;
using UnityEngine;

namespace Asterra.EditorTools
{
    /// <summary>Renders the 3D map studio to PNGs for visual review.</summary>
    public static class MapCreatorReviewCapture
    {
        public const string OutputDir = "Docs/MapCreatorReview";

        [MenuItem("Asterra/Map Creator/Capture Review Shots")]
        public static void CaptureFromMenu()
        {
            Capture(quit: false);
        }

        public static void CaptureFromCommandLine()
        {
            try
            {
                Capture(quit: true);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Asterra] Map creator capture failed: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void Capture(bool quit)
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), OutputDir);
            Directory.CreateDirectory(dir);

            var preview = new MapCreatorWorldPreview();
            try
            {
                var map = ReviewMap();
                preview.EnsureActive();
                preview.Sync(map, rebuildTerrain: true);

                preview.CapturePng(
                    Path.Combine(dir, "01_world_overview.png"),
                    new Vector3(0f, 280f, -420f),
                    new Vector3(0f, 8f, 0f));
                preview.CapturePng(
                    Path.Combine(dir, "02_west_keep.png"),
                    new Vector3(-240f, 90f, -140f),
                    new Vector3(-320f, 8f, 0f));
                preview.CapturePng(
                    Path.Combine(dir, "03_rts_top.png"),
                    new Vector3(0f, 620f, -40f),
                    new Vector3(0f, 0f, 0f));
                preview.CapturePng(
                    Path.Combine(dir, "04_east_hills.png"),
                    new Vector3(280f, 110f, 160f),
                    new Vector3(180f, 12f, 0f));

                Debug.Log($"[Asterra] Map creator review shots written to {dir}");
            }
            finally
            {
                preview.Dispose();
            }

            if (quit)
                EditorApplication.Exit(0);
        }

        private static MapDefinition ReviewMap()
        {
            return new MapDefinition
            {
                id = "review_arena",
                displayName = "Custom Arena",
                defaultTerrain = DefaultTerrainCatalog.GrassShort,
                cameraFocusX = -320f,
                cameraFocusZ = 0f,
                terrain = new[]
                {
                    Disk(-220f, 0f, 48f, DefaultTerrainCatalog.Hill),
                    Disk(200f, 20f, 56f, DefaultTerrainCatalog.Hill),
                    Disk(210f, 30f, 28f, DefaultTerrainCatalog.Mountain),
                    Disk(0f, 80f, 70f, DefaultTerrainCatalog.Forest),
                    Disk(0f, -90f, 36f, DefaultTerrainCatalog.Trench),
                    Disk(-80f, 60f, 18f, DefaultTerrainCatalog.GrassBare),
                },
                texturePaint = new[]
                {
                    new MapTexturePaint { shape = "disk", x = -40f, z = 10f, radius = 40f, layer = "dirt", strength = 0.85f },
                },
                heightPaint = new[]
                {
                    new MapHeightPaint { x = -180f, z = 40f, radius = 70f, delta = 14f, falloff = 0.9f },
                    new MapHeightPaint { x = 190f, z = -20f, radius = 90f, delta = 18f, falloff = 0.88f },
                    new MapHeightPaint { x = 210f, z = 10f, radius = 40f, delta = 10f, falloff = 0.75f },
                    new MapHeightPaint { x = 0f, z = -90f, radius = 36f, delta = -5f, falloff = 0.8f },
                },
                keeps = new[]
                {
                    new MapKeepSpawn { seatIndex = 0, x = -320f, z = 0f, yawDegrees = 90f },
                    new MapKeepSpawn { seatIndex = 1, x = 320f, z = 0f, yawDegrees = -90f },
                },
                buildings = new[]
                {
                    new MapBuildingSpawn { seatIndex = 0, role = "tower", x = -280f, z = 40f, yawDegrees = 45f },
                    new MapBuildingSpawn { seatIndex = 0, role = "wall", x = -300f, z = 28f, yawDegrees = 90f },
                    new MapBuildingSpawn { seatIndex = 1, role = "outpost", x = 280f, z = -36f, yawDegrees = 180f },
                },
                units = new[]
                {
                    new MapUnitSpawn { seatIndex = 0, role = "basic", x = -290f, z = -15f, yawDegrees = 90f },
                    new MapUnitSpawn { seatIndex = 0, role = "builder", x = -270f, z = 0f, yawDegrees = 90f },
                    new MapUnitSpawn { seatIndex = 1, role = "basic", x = 290f, z = 15f, yawDegrees = -90f },
                },
                resources = new[]
                {
                    new MapResourceNode { type = "gold", amount = 2200, x = -80f, z = 60f },
                    new MapResourceNode { type = "timber", amount = 1800, x = 80f, z = -60f },
                },
                territories = new[]
                {
                    new MapTerritory { x = 0f, z = 0f, radius = 40f, goldPerSecond = 8 },
                },
                destructibles = new[]
                {
                    new MapDestructible { catalogId = "tree", x = -110f, z = -40f, yawDegrees = 20f },
                    new MapDestructible { catalogId = "tree", x = -90f, z = -55f, yawDegrees = 140f },
                    new MapDestructible { catalogId = "rock", x = 40f, z = 50f, yawDegrees = 55f },
                    new MapDestructible { catalogId = "bridge", x = 0f, z = -90f, yawDegrees = 90f },
                },
            };
        }

        private static MapTerrainPaint Disk(float x, float z, float radius, ushort terrain) =>
            new MapTerrainPaint
            {
                shape = "disk",
                x = x,
                z = z,
                radius = radius,
                terrainIndex = terrain,
            };
    }
}
