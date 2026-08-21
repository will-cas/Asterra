using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Phase 8–9 regression: wall snap/links and capture events.</summary>
    public static class BuildingSystemsSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            float x = 17f;
            float z = -9f;
            WallPlacement.Snap(ref x, ref z, 14f);
            Expect(ref fails, sb, "wall snap x", Abs(x - 14f) < 0.01f);
            Expect(ref fails, sb, "wall snap z", Abs(z) < 0.01f || Abs(z + 14f) < 0.01f);

            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            wallet.Seed(new PlayerId(0), ResourceType.Gold, 5000);
            wallet.Seed(new PlayerId(0), ResourceType.Timber, 5000);

            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var a = sim.SpawnBuilding(
                ids.Next(), new PlayerId(0), new FactionId(0), FactionDefaultContent.PalisadeId, 0f, 0f, startActive: true);
            var b = sim.SpawnBuilding(
                ids.Next(), new PlayerId(0), new FactionId(0), FactionDefaultContent.PalisadeId, 14f, 0f, startActive: true);
            sim.RefreshWallConnectionsAround(a);

            Expect(ref fails, sb, "wall category", a.Category == BuildingCategory.Wall);
            Expect(ref fails, sb, "wall snap flag", a.SnapToWallGrid);
            Expect(ref fails, sb, "east link on A", (a.WallLinks & 2) != 0);
            Expect(ref fails, sb, "west link on B", (b.WallLinks & 8) != 0);

            var keep = sim.SpawnBuilding(
                ids.Next(), new PlayerId(0), new FactionId(0), FactionDefaultContent.IronKeepId, -80f, -80f, startActive: true);
            Expect(ref fails, sb, "keep garrison", keep.AllowsGarrison && keep.GarrisonCapacity >= 8);
            Expect(ref fails, sb, "keep category castle", keep.Category == BuildingCategory.Castle);

            sim.AddTerritory(ids.Next(), 100f, 100f, 40f, goldPerSecond: 1);
            sim.SpawnUnit(ids.Next(), new PlayerId(0), new FactionId(0), FactionDefaultContent.MilitiaId, 100f, 100f);
            bool sawStart = false;
            bool sawComplete = false;
            for (int i = 0; i < 200; i++)
            {
                sim.Tick(0.25f);
                for (int e = 0; e < sim.CombatEvents.Count; e++)
                {
                    if (sim.CombatEvents[e].Kind == CombatEventKind.CaptureStarted)
                        sawStart = true;
                    if (sim.CombatEvents[e].Kind == CombatEventKind.CaptureCompleted)
                        sawComplete = true;
                }
            }

            Expect(ref fails, sb, "capture started", sawStart);
            Expect(ref fails, sb, "capture completed", sawComplete);

            sb.Append(fails == 0 ? "BuildingSystemsSelfTest: OK" : $"BuildingSystemsSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static float Abs(float v) => v < 0f ? -v : v;

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
