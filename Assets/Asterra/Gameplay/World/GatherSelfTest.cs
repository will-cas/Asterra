using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Keep trickle and resource-building gold (harvest nodes are disabled).</summary>
    public static class GatherSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "gather command ignored", GatherIgnored());
            Expect(ref fails, sb, "keep pays gold per second", KeepPaysGold());
            Expect(ref fails, sb, "resource building pays gold", OutpostPaysGold());

            sb.Append(fails == 0 ? "GatherSelfTest: OK" : $"GatherSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool GatherIgnored()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            var builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 0f, 0f);
            var nodeId = ids.Next();
            sim.AddResourceNode(nodeId, ResourceType.Gold, 500, 10f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { builder.Id }, ResourceNodeId = nodeId },
            });
            return !builder.GatherTargetId.HasValue && !builder.CanGather;
        }

        private static bool KeepPaysGold()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 0);
            for (int i = 0; i < 8; i++)
                sim.Tick(0.25f);
            return wallet.Get(p, ResourceType.Gold) >= 2;
        }

        private static bool OutpostPaysGold()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.OutpostId, 40f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 0);
            for (int i = 0; i < 8; i++)
                sim.Tick(0.25f);
            return wallet.Get(p, ResourceType.Gold) >= 8;
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
