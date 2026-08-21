using System.Text;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Phase-3 regression: each skirmish map paints logical terrain correctly.</summary>
    public static class SkirmishMapTerrainSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            ExpectMap(ref fails, sb, SkirmishMapId.TwinKeeps, env =>
            {
                Expect(ref fails, sb, "TK center land", env.CanUnitEnter(0f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "TK long grass", env.Features.HasLongGrassNear(0f, 120f, 30f));
                Expect(ref fails, sb, "TK forest", env.Features.HasForestNear(-100f, -70f, 40f));
                Expect(ref fails, sb, "TK tree blocks", !env.CanUnitEnter(-110f, -80f, TraversalCapability.Land));
                Expect(ref fails, sb, "TK swamp slow", env.MovementModifier(0f, -35f, TraversalCapability.Land) < 0.6f);
            });

            ExpectMap(ref fails, sb, SkirmishMapId.RiverCrossing, env =>
            {
                Expect(ref fails, sb, "RC river blocks land", !env.CanUnitEnter(80f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC river allows boat", env.CanUnitEnter(80f, 0f, TraversalCapability.Water));
                Expect(ref fails, sb, "RC center ford", env.CanUnitEnter(0f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC west ford", env.CanUnitEnter(-130f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC waterfall blocked", !env.CanUnitEnter(-345f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC waterfall blocks boat", !env.CanUnitEnter(-345f, 0f, TraversalCapability.Water));
                Expect(ref fails, sb, "RC water feature", env.Features.HasWaterNear(80f, 0f, 40f));
                Expect(ref fails, sb, "RC ice land", env.CanUnitEnter(190f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "RC keep pad", env.CanUnitEnter(-300f, -220f, TraversalCapability.Land));
            });

            ExpectMap(ref fails, sb, SkirmishMapId.BlackridgePass, env =>
            {
                Expect(ref fails, sb, "BP trench center", env.CanUnitEnter(0f, 0f, TraversalCapability.Land));
                Expect(ref fails, sb, "BP trench cover", env.Features.SampleCoverBonus(0f, 0f) >= 0.3f);
                Expect(ref fails, sb, "BP mountain blocks", !env.CanUnitEnter(-200f, 200f, TraversalCapability.Land));
                Expect(ref fails, sb, "BP mountain unit ok", env.CanUnitEnter(-200f, 120f, TraversalCapability.Mountain));
                Expect(ref fails, sb, "BP north ramp", env.CanUnitEnter(0f, 110f, TraversalCapability.Land));
                Expect(ref fails, sb, "BP south ramp", env.CanUnitEnter(0f, -110f, TraversalCapability.Land));
                Expect(ref fails, sb, "BP no-entry corner", env.Grid.IsBlocked(-300f, 300f));
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
