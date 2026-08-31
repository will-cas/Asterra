using System.Text;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Regression: each built-in map paints logical terrain correctly.</summary>
    public static class SkirmishMapTerrainSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            ExpectMap(ref fails, sb, SkirmishMapId.LushForest, env =>
            {
                Expect(ref fails, sb, "GV keep pad", env.CanUnitEnter(-340f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "GV long grass", env.Features.HasLongGrassNear(-120f, -120f, 40f));
                Expect(ref fails, sb, "GV forest", env.Features.HasForestNear(0f, 200f, 40f));
                Expect(ref fails, sb, "GV tree blocks", !env.CanUnitEnter(-110f, -80f, TraversalCapability.Land));
                Expect(ref fails, sb, "GV swamp slow", env.MovementModifier(-40f, 110f, TraversalCapability.Land) < 0.6f);
            });

            ExpectMap(ref fails, sb, SkirmishMapId.RiverCrossing, env =>
            {
                Expect(ref fails, sb, "RC river blocks land", !env.CanUnitEnter(0f, 50f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC river allows boat", env.CanUnitEnter(0f, 50f, TraversalCapability.Water));
                Expect(ref fails, sb, "RC center deck", env.CanUnitEnter(0f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC south ford", env.CanUnitEnter(0f, -190f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC ocean blocks land", !env.CanUnitEnter(0f, 410f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC ocean allows boat", env.CanUnitEnter(0f, 410f, TraversalCapability.Water));
                Expect(ref fails, sb, "RC water feature", env.Features.HasWaterNear(0f, 80f, 40f));
                Expect(ref fails, sb, "RC keep pad", env.CanUnitEnter(-320f, 0f, TraversalCapability.Land));
            });

            ExpectMap(ref fails, sb, SkirmishMapId.AncientRelic, env =>
            {
                Expect(ref fails, sb, "AR relic bowl", env.CanUnitEnter(0f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "AR no-entry cliff", env.Grid.IsBlocked(-300f, 0f));
                Expect(ref fails, sb, "AR south keep", env.CanUnitEnter(0f, -340f, TraversalCapability.Land));
            });

            ExpectMap(ref fails, sb, SkirmishMapId.MundorCapital, env =>
            {
                Expect(ref fails, sb, "MC island land", env.CanUnitEnter(0f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "MC west river blocks", !env.CanUnitEnter(-140f, 80f, TraversalCapability.Land));
                Expect(ref fails, sb, "MC west river boat", env.CanUnitEnter(-140f, 80f, TraversalCapability.Water));
                Expect(ref fails, sb, "MC east river blocks", !env.CanUnitEnter(140f, 80f, TraversalCapability.Land));
            });

            sb.Append(fails == 0 ? "SkirmishMapTerrainSelfTest: OK" : $"SkirmishMapTerrainSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static void ExpectMap(
            ref int fails,
            StringBuilder sb,
            SkirmishMapId map,
            System.Action<WorldEnvironmentSim> assert)
        {
            var env = new WorldEnvironmentSim();
            SkirmishMapTerrain.Apply(env, map);
            assert(env);
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
