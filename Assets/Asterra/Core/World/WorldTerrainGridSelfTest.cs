using System.Collections.Generic;
using System.Text;
using Asterra.Core.World;

namespace Asterra.Core.World
{
    /// <summary>Headless regression for terrain grid / capability rules (runs via Asterra.Smoke).</summary>
    public static class WorldTerrainGridSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            var grass = TerrainDefData.CreateDefaultGrassShort();
            var swamp = new TerrainDefData
            {
                Id = "terrain_swamp",
                DisplayName = "Swamp",
                Category = TerrainCategory.Swamp,
                MovementSpeedModifier = 0.45f,
                PathfindingCost = 3f,
                RequiredCapabilities = TraversalCapability.Land,
            };
            var water = new TerrainDefData
            {
                Id = "terrain_water_lake",
                DisplayName = "Lake",
                Category = TerrainCategory.WaterLake,
                MovementSpeedModifier = 1f,
                PathfindingCost = 1f,
                RequiredCapabilities = TraversalCapability.Water,
                AllowsBuilding = false,
            };
            var mountain = new TerrainDefData
            {
                Id = "terrain_mountain",
                DisplayName = "Mountain",
                Category = TerrainCategory.Mountain,
                MovementSpeedModifier = 0.5f,
                PathfindingCost = 5f,
                RequiredCapabilities = TraversalCapability.Mountain,
                AllowsBuilding = false,
            };
            var noEntry = TerrainDefData.CreateNoEntry();

            var defs = new[] { grass, swamp, water, mountain, noEntry };
            var grid = new WorldTerrainGrid(20, 20, 10f, -100f, -100f, defs);

            // Center cell (0,0) world → cell (10,10)
            Expect(ref fails, sb, "default traversable land", grid.IsTraversable(0f, 0f, TraversalCapability.Land));
            Expect(ref fails, sb, "default move mod 1", Near(grid.GetMovementModifier(0f, 0f, TraversalCapability.Land), 1f));

            grid.SetCellDef(10, 10, 1); // swamp
            Expect(ref fails, sb, "swamp land ok", grid.IsTraversable(0f, 0f, TraversalCapability.Land));
            Expect(ref fails, sb, "swamp slow", Near(grid.GetMovementModifier(0f, 0f, TraversalCapability.Land), 0.45f));
            Expect(ref fails, sb, "swamp cost 3", Near(grid.GetPathCost(0f, 0f, TraversalCapability.Land), 3f));

            grid.SetCellDef(11, 10, 2); // water at +10x
            Expect(ref fails, sb, "land cannot enter water", !grid.IsTraversable(10f, 0f, TraversalCapability.Land));
            Expect(ref fails, sb, "boat can enter water", grid.IsTraversable(10f, 0f, TraversalCapability.Water));
            Expect(ref fails, sb, "amphibious can enter water", grid.IsTraversable(10f, 0f, TraversalCapability.Amphibious));
            Expect(ref fails, sb, "flying can enter water", grid.IsTraversable(10f, 0f, TraversalCapability.Flying));

            grid.SetCellDef(12, 10, 3); // mountain
            Expect(ref fails, sb, "land blocked on mountain", !grid.IsTraversable(20f, 0f, TraversalCapability.Land));
            Expect(ref fails, sb, "mountain unit ok", grid.IsTraversable(20f, 0f, TraversalCapability.Mountain));

            grid.SetBlockedRect(-5f, -5f, 5f, 5f, true);
            Expect(ref fails, sb, "no-entry rect blocks", !grid.IsTraversable(0f, 0f, TraversalCapability.Land));
            Expect(ref fails, sb, "no-entry blocks flying too", !grid.IsTraversable(0f, 0f, TraversalCapability.Flying));
            grid.SetBlockedRect(-5f, -5f, 5f, 5f, false);

            grid.SetCellDef(10, 10, 4); // no-entry terrain def
            Expect(ref fails, sb, "no-entry def blocks", !grid.IsTraversable(0f, 0f, TraversalCapability.Land));
            Expect(ref fails, sb, "building denied on water", !grid.AllowsBuilding(10f, 0f));

            var path = new DirectSteerPathfindingService(grid);
            var pts = new List<(float x, float z)>();
            Expect(ref fails, sb, "path to grass", path.TryGetPath(-50f, 0f, 50f, 0f, TraversalCapability.Land, pts) && pts.Count == 1);
            Expect(ref fails, sb, "path rejects water for land", !path.TryGetPath(0f, 0f, 10f, 0f, TraversalCapability.Land, pts));

            var playable = WorldTerrainGrid.CreatePlayableDefault(450f, 10f);
            Expect(ref fails, sb, "playable size", playable.Width == 90 && playable.Height == 90);
            Expect(ref fails, sb, "outside playable blocked", playable.IsBlocked(500f, 0f));

            sb.Append(fails == 0 ? "WorldTerrainGridSelfTest: OK" : $"WorldTerrainGridSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }

        private static bool Near(float a, float b) => System.Math.Abs(a - b) < 0.0001f;
    }
}
