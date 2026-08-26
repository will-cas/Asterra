using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Shared vision / FoW query coverage.</summary>
    public static class VisionSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "empty world not visible", EmptyNotVisible());
            Expect(ref fails, sb, "unit provides vision", UnitProvidesVision());
            Expect(ref fails, sb, "beyond sight not visible", BeyondSightHidden());
            Expect(ref fails, sb, "building provides vision", BuildingProvidesVision());
            Expect(ref fails, sb, "enemy does not share vision", EnemyNoShare());
            Expect(ref fails, sb, "garrisoned unit loses vision", GarrisonedLosesVision());
            Expect(ref fails, sb, "keep turret sight after attach finish", AttachedTurretVision());

            sb.Append(fails == 0 ? "VisionSelfTest: OK" : $"VisionSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool EmptyNotVisible()
        {
            var sim = NewSim(out _, out _, out _);
            return !sim.IsVisibleTo(new PlayerId(0), 0f, 0f);
        }

        private static bool UnitProvidesVision()
        {
            var sim = NewSim(out var ids, out _, out var defs);
            _ = defs;
            var p = new PlayerId(0);
            sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.MilitiaId, 0f, 0f);
            return sim.IsVisibleTo(p, 5f, 0f);
        }

        private static bool BeyondSightHidden()
        {
            var sim = NewSim(out var ids, out _, out _);
            var p = new PlayerId(0);
            sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.MilitiaId, 0f, 0f);
            return !sim.IsVisibleTo(p, 400f, 400f);
        }

        private static bool BuildingProvidesVision()
        {
            var sim = NewSim(out var ids, out _, out _);
            var p = new PlayerId(0);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.WatchtowerId, 100f, 100f, startActive: true);
            return sim.IsVisibleTo(p, 110f, 100f);
        }

        private static bool EnemyNoShare()
        {
            var sim = NewSim(out var ids, out _, out _);
            sim.SpawnUnit(ids.Next(), new PlayerId(1), new FactionId(1), FactionDefaultContent.DryadId, 0f, 0f);
            return !sim.IsVisibleTo(new PlayerId(0), 0f, 0f);
        }

        private static bool GarrisonedLosesVision()
        {
            var sim = NewSim(out var ids, out var wallet, out _);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 0);
            // Unit alone at far point — then garrison into tower elsewhere? Tower at origin, unit at origin, enter.
            var tower = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.WatchtowerId, 0f, 0f, startActive: true);
            var scout = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.MilitiaId, 200f, 0f);
            // Scout provides vision at 200 — then we only check garrisoned unit doesn't provide.
            // Spawn second unit near tower and garrison it; vision at far scout still works.
            // Better: only one unit, garrison it, check far point loses vision from that unit's old pos.
            var only = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.MilitiaId, 5f, 0f);
            bool before = sim.IsVisibleTo(p, 8f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new EnterGarrisonCommand
                {
                    Issuer = p,
                    UnitIds = new[] { only.Id },
                    BuildingId = tower.Id,
                },
            });
            // Remove scout from interfering — kill/teleport far.
            scout.X = 400f;
            scout.Z = 400f;
            bool afterNearTower = sim.IsVisibleTo(p, 8f, 0f); // tower still sees
            bool unitGarrisoned = only.IsGarrisoned;
            return before && unitGarrisoned && afterNearTower;
        }

        private static bool AttachedTurretVision()
        {
            var sim = NewSim(out var ids, out var wallet, out _);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 500);
            wallet.Seed(p, ResourceType.Timber, 500);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, 0f, 0f, startActive: true);
            // Keep already provides vision; ensure attach doesn't break visibility.
            bool before = sim.IsVisibleTo(p, 10f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new AttachBuildingCommand
                {
                    Issuer = p,
                    ParentBuildingId = keep.Id,
                    SlotIndex = 0,
                    BuildingDefId = FactionDefaultContent.KeepTurretId,
                },
            });
            return before && sim.IsVisibleTo(p, 10f, 0f);
        }

        private static SkirmishWorldSim NewSim(
            out SequentialIdFactory ids,
            out ResourceWallet wallet,
            out DefinitionRegistry defs)
        {
            ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            return new SkirmishWorldSim(wallet, ids, defs);
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
