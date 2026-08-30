using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Territory income, wall blocking, and leader lifecycle edges.</summary>
    public static class EcoFortLeaderSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "territory income pays controller", TerritoryIncome());
            Expect(ref fails, sb, "vision camp pays less gold", VisionCampPaysLess());
            Expect(ref fails, sb, "signature building near keep", SignatureNearKeep());
            Expect(ref fails, sb, "outpost passive gold", OutpostIncome());
            Expect(ref fails, sb, "capture progresses with unit", CaptureProgresses());
            Expect(ref fails, sb, "wall links and reject stack", WallBlocksPath());
            Expect(ref fails, sb, "palisade snap grids", PalisadeSnap());
            Expect(ref fails, sb, "leader unique while alive", LeaderUniqueAlive());
            Expect(ref fails, sb, "leader trainable after death", LeaderAfterDeath());
            Expect(ref fails, sb, "leader not double-queued", LeaderNotDoubleQueued());

            sb.Append(fails == 0 ? "EcoFortLeaderSelfTest: OK" : $"EcoFortLeaderSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool TerritoryIncome()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 0);
            var tid = ids.Next();
            sim.AddTerritory(tid, 0f, 0f, 40f, goldPerSecond: 5);
            // Force controlled — capture loop would take long; set via damage-free approach:
            // spawn unit and tick capture.
            sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 0f, 0f);
            for (int i = 0; i < 250; i++)
                sim.Tick(0.25f);
            bool controlled = false;
            for (int i = 0; i < sim.Territories.Count; i++)
            {
                if (sim.Territories[i].Id.Value == tid.Value
                    && sim.Territories[i].HasController
                    && sim.Territories[i].Controller.Value == p.Value)
                    controlled = true;
            }

            if (!controlled)
                return false;
            int g0 = wallet.Get(p, ResourceType.Gold);
            for (int i = 0; i < 20; i++)
                sim.Tick(1f);
            return wallet.Get(p, ResourceType.Gold) > g0;
        }

        private static bool VisionCampPaysLess()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 80);
            var tid = ids.Next();
            sim.AddTerritory(tid, 0f, 0f, 40f, goldPerSecond: 5);
            sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 0f, 0f);
            for (int i = 0; i < 250; i++)
                sim.Tick(0.25f);
            bool controlled = false;
            for (int i = 0; i < sim.Territories.Count; i++)
            {
                if (sim.Territories[i].Id.Value == tid.Value
                    && sim.Territories[i].HasController
                    && sim.Territories[i].Controller.Value == p.Value)
                    controlled = true;
            }

            if (!controlled)
                return false;

            sim.ApplyCommands(new GameCommand[]
            {
                new SetTerritoryJobCommand
                {
                    Issuer = p,
                    TerritoryId = tid,
                    Job = TerritoryJob.Vision,
                },
            });
            int g0 = wallet.Get(p, ResourceType.Gold);
            for (int i = 0; i < 5; i++)
                sim.Tick(1f);
            int gained = wallet.Get(p, ResourceType.Gold) - g0;
            return gained >= 8 && gained <= 16;
        }

        private static bool SignatureNearKeep()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 8000);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            bool near = sim.CanPreviewPlaceBuilding(FactionDefaultContent.PortalGateId, 50f, 0f, 0f, p);
            bool far = sim.CanPreviewPlaceBuilding(FactionDefaultContent.PortalGateId, 120f, 0f, 0f, p);
            return near && !far;
        }

        private static bool OutpostIncome()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 0);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.OutpostId, 50f, 50f, startActive: true);
            int g0 = wallet.Get(p, ResourceType.Gold);
            for (int i = 0; i < 12; i++)
                sim.Tick(1f);
            return wallet.Get(p, ResourceType.Gold) > g0;
        }

        private static bool CaptureProgresses()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            var tid = ids.Next();
            sim.AddTerritory(tid, 10f, 10f, 40f, goldPerSecond: 1);
            sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 10f, 10f);
            float prog0 = 0f;
            for (int i = 0; i < sim.Territories.Count; i++)
            {
                if (sim.Territories[i].Id.Value == tid.Value)
                    prog0 = sim.Territories[i].CaptureProgress;
            }

            for (int i = 0; i < 30; i++)
                sim.Tick(0.25f);
            for (int i = 0; i < sim.Territories.Count; i++)
            {
                if (sim.Territories[i].Id.Value == tid.Value)
                    return sim.Territories[i].CaptureProgress > prog0
                           || sim.Territories[i].State == TerritoryState.Controlled;
            }

            return false;
        }

        private static bool WallBlocksPath()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 5000);
            wallet.Seed(p, ResourceType.Timber, 5000);
            var a = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.PalisadeId, 0f, 0f, startActive: true);
            var b = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.PalisadeId, 14f, 0f, startActive: true);
            sim.RefreshWallConnectionsAround(a);
            // Adjacent walls should link; stacking on same cell rejected by place.
            int before = sim.Buildings.Count;
            sim.ApplyCommands(new GameCommand[]
            {
                new PlaceBuildingCommand
                {
                    Issuer = p,
                    BuildingDefId = FactionDefaultContent.PalisadeId,
                    X = 0f,
                    Z = 0f,
                },
            });
            return (a.WallLinks & 2) != 0
                   && (b.WallLinks & 8) != 0
                   && sim.Buildings.Count == before;
        }

        private static bool PalisadeSnap()
        {
            float x = 17f;
            float z = -3f;
            WallPlacement.Snap(ref x, ref z, 14f);
            return Abs(x % 14f) < 0.01f || Abs(Abs(x) - 14f) < 0.01f || Abs(x) < 0.01f;
        }

        private static bool LeaderUniqueAlive()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 5000);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledHeirId, 5f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.VeiledHeirId,
                },
            });
            return !keep.IsProducing;
        }

        private static bool LeaderAfterDeath()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 5000);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            var leader = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledHeirId, 5f, 0f);
            sim.ApplyWorldDamage(leader.Id, 9999f, vsStructure: false);
            sim.Tick(0.05f);
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.VeiledHeirId,
                },
            });
            return keep.IsProducing;
        }

        private static bool LeaderNotDoubleQueued()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 5000);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.VeiledHeirId,
                },
            });
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.VeiledHeirId,
                },
            });
            return keep.IsProducing && keep.QueueCount == 0;
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
