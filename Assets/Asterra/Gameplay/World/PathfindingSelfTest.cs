using System.Collections.Generic;
using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    public static class PathfindingSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;
            var env = new WorldEnvironmentSim(weatherSeed: 7u);
            var grid = env.Grid;

            // Paint a blocked strip and ensure A* routes around it.
            grid.FillWorldRect(-20f, -5f, 20f, 5f, DefaultTerrainCatalog.NoEntry);
            env.RebuildFeatureIndex();

            var path = new List<(float x, float z)>();
            bool ok = env.Pathfinding.TryGetPath(-40f, 0f, 40f, 0f, TraversalCapability.Land, path);
            Expect(ref fails, sb, "path found around no-entry", ok && path.Count > 0);
            if (ok)
            {
                bool crossedBlocked = false;
                for (int i = 0; i < path.Count; i++)
                {
                    if (grid.GetPathCost(path[i].x, path[i].z, TraversalCapability.Land) >= TerrainDefData.PathCostBlocked)
                        crossedBlocked = true;
                }

                Expect(ref fails, sb, "path avoids blocked cells", !crossedBlocked);
                Expect(ref fails, sb, "path has intermediate points", path.Count >= 2);
            }

            // Open ground stays short.
            path.Clear();
            ok = env.Pathfinding.TryGetPath(100f, 100f, 120f, 100f, TraversalCapability.Land, path);
            Expect(ref fails, sb, "open path ok", ok && path.Count >= 1);

            sb.Append(fails == 0 ? "PathfindingSelfTest: OK" : $"PathfindingSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
