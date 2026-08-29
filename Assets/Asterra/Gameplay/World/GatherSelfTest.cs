using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Gather, deposit, and resource-node economy.</summary>
    public static class GatherSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "gather assigns target", GatherAssigns());
            Expect(ref fails, sb, "combat unit cannot gather", CombatCannotGather());
            Expect(ref fails, sb, "gather fills carry", GatherFillsCarry());
            Expect(ref fails, sb, "deposit increases wallet", DepositIncreasesWallet());
            Expect(ref fails, sb, "node depletes", NodeDepletes());
            Expect(ref fails, sb, "depleted gather rejected", DepletedGatherRejected());

            sb.Append(fails == 0 ? "GatherSelfTest: OK" : $"GatherSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool GatherAssigns()
        {
            Setup(out var sim, out _, out var wallet, out var p, out var builder, out var nodeId);
            _ = wallet;
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { builder.Id }, ResourceNodeId = nodeId },
            });
            return builder.GatherTargetId.HasValue && builder.GatherTargetId.Value.Value == nodeId.Value;
        }

        private static bool CombatCannotGather()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            var grunt = sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 0f, 0f);
            var nodeId = ids.Next();
            sim.AddResourceNode(nodeId, ResourceType.Gold, 500, 10f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { grunt.Id }, ResourceNodeId = nodeId },
            });
            return !grunt.GatherTargetId.HasValue;
        }

        private static bool GatherFillsCarry()
        {
            Setup(out var sim, out _, out _, out var p, out var builder, out var nodeId);
            PlaceOnNode(sim, builder, nodeId);
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { builder.Id }, ResourceNodeId = nodeId },
            });
            for (int i = 0; i < 80; i++)
                sim.Tick(0.25f);
            return builder.CarryAmount > 0 || builder.ReturningToDeposit;
        }

        private static bool DepositIncreasesWallet()
        {
            Setup(out var sim, out var ids, out var wallet, out var p, out var builder, out var nodeId);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            PlaceOnNode(sim, builder, nodeId);
            wallet.Seed(p, ResourceType.Gold, 0);
            int g0 = wallet.Get(p, ResourceType.Gold);
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { builder.Id }, ResourceNodeId = nodeId },
            });
            for (int i = 0; i < 200; i++)
                sim.Tick(0.25f);
            return wallet.Get(p, ResourceType.Gold) > g0;
        }

        private static bool NodeDepletes()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            var nodeId = ids.Next();
            sim.AddResourceNode(nodeId, ResourceType.Gold, 8, 12f, 0f);
            var builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 12f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { builder.Id }, ResourceNodeId = nodeId },
            });
            for (int i = 0; i < 400; i++)
                sim.Tick(0.25f);
            return !FindNode(sim, nodeId, out int rem) || rem <= 0;
        }

        private static bool DepletedGatherRejected()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            var nodeId = ids.Next();
            sim.AddResourceNode(nodeId, ResourceType.Gold, 1, 20f, 20f);
            var builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 20f, 20f);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { builder.Id }, ResourceNodeId = nodeId },
            });
            for (int i = 0; i < 500; i++)
                sim.Tick(0.25f);

            var other = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 25f, 25f);
            sim.ApplyCommands(new GameCommand[]
            {
                new GatherCommand { Issuer = p, UnitIds = new[] { other.Id }, ResourceNodeId = nodeId },
            });
            return !other.GatherTargetId.HasValue;
        }

        private static void Setup(
            out SkirmishWorldSim sim,
            out SequentialIdFactory ids,
            out ResourceWallet wallet,
            out PlayerId p,
            out SimUnit builder,
            out SimEntityId nodeId)
        {
            ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            sim = new SkirmishWorldSim(wallet, ids, defs);
            p = new PlayerId(0);
            nodeId = ids.Next();
            sim.AddResourceNode(nodeId, ResourceType.Gold, 500, 40f, 0f);
            builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 30f, 0f);
        }

        private static void PlaceOnNode(SkirmishWorldSim sim, SimUnit builder, SimEntityId nodeId)
        {
            for (int i = 0; i < sim.Resources.Count; i++)
            {
                if (sim.Resources[i].Id.Value != nodeId.Value)
                    continue;
                builder.X = sim.Resources[i].X;
                builder.Z = sim.Resources[i].Z;
                return;
            }
        }

        private static bool FindNode(SkirmishWorldSim sim, SimEntityId nodeId, out int remaining)
        {
            remaining = 0;
            for (int i = 0; i < sim.Resources.Count; i++)
            {
                if (sim.Resources[i].Id.Value != nodeId.Value)
                    continue;
                remaining = sim.Resources[i].Remaining;
                return true;
            }

            return false;
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
