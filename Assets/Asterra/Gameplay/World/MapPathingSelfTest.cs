using System.Collections.Generic;
using System.Text;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Pathfinding on real skirmish map terrains (chokes / rivers).</summary>
    public static class MapPathingSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "greenveil west-east path", GreenveilCross());
            Expect(ref fails, sb, "relic path avoids east cliff", RelicAvoidsCliff());
            Expect(ref fails, sb, "river uses bridge lane", RiverBridgePath());
            Expect(ref fails, sb, "twin cities open corridor", TwinCitiesCorridor());
            Expect(ref fails, sb, "boat on river ocean", RiverBoatWater());
            Expect(ref fails, sb, "land rejected on ocean", LandRejectedOnOcean());

            sb.Append(fails == 0 ? "MapPathingSelfTest: OK" : $"MapPathingSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool GreenveilCross()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 11u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.LushForest);
            SkirmishMapTraversal.Apply(env, SkirmishMapId.LushForest);
            var path = new List<(float x, float z)>();
            bool ok = env.Pathfinding.TryGetPath(-300f, 0f, 300f, 0f, TraversalCapability.Land, path);
            return ok && path.Count >= 2;
        }

        private static bool RelicAvoidsCliff()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 11u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.AncientRelic);
            var path = new List<(float x, float z)>();
            if (!env.Pathfinding.TryGetPath(0f, -200f, 0f, 200f, TraversalCapability.Land, path))
                return false;
            for (int i = 0; i < path.Count; i++)
            {
                float x = path[i].x;
                float z = path[i].z;
                if (x < -90f || x > 90f)
                {
                    if (z > -25f && z < 25f && !env.CanUnitEnter(x, z, TraversalCapability.Land))
                        return false;
                }
            }

            return true;
        }

        private static bool RiverBridgePath()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 3u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.RiverCrossing);
            SkirmishMapTraversal.Apply(env, SkirmishMapId.RiverCrossing);
            var path = new List<(float x, float z)>();
            bool ok = env.Pathfinding.TryGetPath(-65f, -10f, 65f, 10f, TraversalCapability.Land, path);
            return ok && path.Count >= 2;
        }

        private static bool TwinCitiesCorridor()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 5u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.TwinCities);
            SkirmishMapTraversal.Apply(env, SkirmishMapId.TwinCities);
            var path = new List<(float x, float z)>();
            return env.Pathfinding.TryGetPath(-280f, 0f, 280f, 0f, TraversalCapability.Land, path)
                   && path.Count >= 1;
        }

        private static bool RiverBoatWater()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 3u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.RiverCrossing);
            return env.CanUnitEnter(0f, -400f, TraversalCapability.Water);
        }

        private static bool LandRejectedOnOcean()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 3u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.RiverCrossing);
            return !env.CanUnitEnter(0f, -400f, TraversalCapability.Land);
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
