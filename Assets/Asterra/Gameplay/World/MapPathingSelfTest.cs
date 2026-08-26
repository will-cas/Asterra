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

            Expect(ref fails, sb, "blackridge west-east path", BlackridgeCross());
            Expect(ref fails, sb, "blackridge path avoids mountain core", BlackridgeAvoidsMountain());
            Expect(ref fails, sb, "river uses bridge lane", RiverBridgePath());
            Expect(ref fails, sb, "twin keeps open corridor", TwinKeepsCorridor());
            Expect(ref fails, sb, "boat on river ocean", RiverBoatWater());
            Expect(ref fails, sb, "land rejected on deep water", LandRejectedOnOcean());

            sb.Append(fails == 0 ? "MapPathingSelfTest: OK" : $"MapPathingSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool BlackridgeCross()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 11u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.BlackridgePass);
            SkirmishMapTraversal.Apply(env, SkirmishMapId.BlackridgePass);
            var path = new List<(float x, float z)>();
            bool ok = env.Pathfinding.TryGetPath(-300f, 0f, 300f, 0f, TraversalCapability.Land, path);
            return ok && path.Count >= 2;
        }

        private static bool BlackridgeAvoidsMountain()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 11u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.BlackridgePass);
            var path = new List<(float x, float z)>();
            if (!env.Pathfinding.TryGetPath(-200f, 0f, 200f, 0f, TraversalCapability.Land, path))
                return false;
            // Center mountain strip should not be stepped on if blocked.
            for (int i = 0; i < path.Count; i++)
            {
                float x = path[i].x;
                float z = path[i].z;
                if (x > -40f && x < 40f && z > -40f && z < 40f)
                {
                    if (!env.CanUnitEnter(x, z, TraversalCapability.Land))
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
            bool ok = env.Pathfinding.TryGetPath(-65f, -40f, 65f, 40f, TraversalCapability.Land, path);
            return ok && path.Count >= 2;
        }

        private static bool TwinKeepsCorridor()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 5u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.TwinKeeps);
            var path = new List<(float x, float z)>();
            return env.Pathfinding.TryGetPath(-300f, 0f, 300f, 0f, TraversalCapability.Land, path)
                   && path.Count >= 1;
        }

        private static bool RiverBoatWater()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 3u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.RiverCrossing);
            return env.CanUnitEnter(-390f, 0f, TraversalCapability.Water);
        }

        private static bool LandRejectedOnOcean()
        {
            var env = new WorldEnvironmentSim(weatherSeed: 3u);
            SkirmishMapTerrain.Apply(env, SkirmishMapId.RiverCrossing);
            return !env.CanUnitEnter(-390f, 0f, TraversalCapability.Land);
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
