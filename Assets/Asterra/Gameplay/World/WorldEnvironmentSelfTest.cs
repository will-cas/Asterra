using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Phase-2 regression: terrain modifiers and no-entry affect the live sim.</summary>
    public static class WorldEnvironmentSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();
            var env = new WorldEnvironmentSim();
            var sim = new SkirmishWorldSim(wallet, ids, defs, env);

            var player = new PlayerId(0);
            var faction = new FactionId(0);
            wallet.Seed(player, ResourceType.Gold, 500);
            wallet.Seed(player, ResourceType.Timber, 300);

            // Baseline: short grass, MoveSpeed from militia def.
            var unit = sim.SpawnUnit(ids.Next(), player, faction, FactionDefaultContent.MilitiaId, 0f, 0f);
            float baseSpeed = unit.MoveSpeed;
            unit.MoveTargetX = 100f;
            unit.MoveTargetZ = 0f;

            sim.Tick(1f);
            float grassX = unit.X;
            Expect(ref fails, sb, "grass moved", grassX > 1f);
            Expect(ref fails, sb, "grass approx base speed", Near(grassX, baseSpeed, 0.15f));

            // Reset and paint swamp under the unit.
            unit.X = 0f;
            unit.Z = 0f;
            unit.MoveTargetX = 100f;
            unit.MoveTargetZ = 0f;
            env.Grid.FillWorldRect(-20f, -20f, 20f, 20f, DefaultTerrainCatalog.Swamp);
            float swampMod = env.MovementModifier(0f, 0f, unit.TraversalCapabilities);
            Expect(ref fails, sb, "swamp mod ~0.45", Near(swampMod, 0.45f, 0.001f));

            sim.Tick(1f);
            Expect(ref fails, sb, "swamp slower than grass", unit.X < grassX - 0.5f);
            Expect(ref fails, sb, "swamp distance matches mod", Near(unit.X, baseSpeed * swampMod, 0.2f));

            // No-entry wall ahead of unit.
            unit.X = 0f;
            unit.Z = 0f;
            unit.MoveTargetX = 80f;
            unit.MoveTargetZ = 0f;
            env.Grid.FillWorldRect(-20f, -20f, 20f, 20f, DefaultTerrainCatalog.GrassShort);
            env.Grid.SetBlockedRect(15f, -40f, 40f, 40f, blocked: true);

            for (int i = 0; i < 30; i++)
                sim.Tick(0.2f);

            Expect(ref fails, sb, "blocked before no-entry", unit.X < 16f);
            Expect(ref fails, sb, "still alive at barrier", unit.IsAlive);

            // Building placement rejected on water.
            env.Grid.FillWorldRect(50f, 50f, 70f, 70f, DefaultTerrainCatalog.WaterLake);
            Expect(ref fails, sb, "cannot build on lake", !env.CanPlaceBuilding(60f, 60f));
            Expect(ref fails, sb, "can build on grass", env.CanPlaceBuilding(0f, 0f));

            // Water not enterable by land unit.
            Expect(ref fails, sb, "land blocked by lake", !env.CanUnitEnter(60f, 60f, TraversalCapability.Land));
            Expect(ref fails, sb, "boat enters lake", env.CanUnitEnter(60f, 60f, TraversalCapability.Water));

            sb.Append(fails == 0 ? "WorldEnvironmentSelfTest: OK" : $"WorldEnvironmentSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }

        private static bool Near(float a, float b, float eps) => System.Math.Abs(a - b) <= eps;
    }
}
