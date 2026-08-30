using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Builder placement, attract, on-site progress, and finish.</summary>
    public static class ConstructionSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "place spends gold", PlaceSpendsCosts());
            Expect(ref fails, sb, "gold fail rejects place", GoldFailRejectsPlace());
            Expect(ref fails, sb, "place attracts distant builder", PlaceAttractsBuilder());
            Expect(ref fails, sb, "construction finishes with builder", ConstructionFinishes());
            Expect(ref fails, sb, "no progress outside work radius", NoProgressFarAway());
            Expect(ref fails, sb, "attach spends keep turret cost", AttachSpendsTurretCost());
            Expect(ref fails, sb, "second attach slot works", SecondAttachSlot());
            Expect(ref fails, sb, "occupied slot rejects attach", OccupiedSlotRejects());

            sb.Append(fails == 0 ? "ConstructionSelfTest: OK" : $"ConstructionSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool PlaceSpendsCosts()
        {
            Setup(out var sim, out var wallet, out var ids, out var player);
            wallet.Seed(player, ResourceType.Gold, 500);
            wallet.Seed(player, ResourceType.Timber, 500);
            int g0 = wallet.Get(player, ResourceType.Gold);
            if (!defs.TryGetBuilding(FactionDefaultContent.ArcaneAcademyId, out var def))
                return false;
            sim.ApplyCommands(new GameCommand[]
            {
                new PlaceBuildingCommand
                {
                    Issuer = player,
                    BuildingDefId = FactionDefaultContent.ArcaneAcademyId,
                    X = 40f,
                    Z = 40f,
                },
            });
            return wallet.Get(player, ResourceType.Gold) == g0 - def.GoldCost
                   && CountConstructing(sim, player, FactionDefaultContent.ArcaneAcademyId) == 1;
        }

        private static DefinitionRegistry defs;

        private static bool GoldFailRejectsPlace()
        {
            Setup(out var sim, out var wallet, out _, out var player);
            wallet.Seed(player, ResourceType.Gold, 0);
            int g0 = wallet.Get(player, ResourceType.Gold);
            int before = sim.Buildings.Count;
            sim.ApplyCommands(new GameCommand[]
            {
                new PlaceBuildingCommand
                {
                    Issuer = player,
                    BuildingDefId = FactionDefaultContent.ArcaneAcademyId,
                    X = 50f,
                    Z = -50f,
                },
            });
            return sim.Buildings.Count == before && wallet.Get(player, ResourceType.Gold) == g0;
        }

        private static bool PlaceAttractsBuilder()
        {
            Setup(out var sim, out var wallet, out var ids, out var player);
            wallet.Seed(player, ResourceType.Gold, 500);
            wallet.Seed(player, ResourceType.Timber, 500);
            var builder = sim.SpawnUnit(
                ids.Next(), player, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 0f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new PlaceBuildingCommand
                {
                    Issuer = player,
                    BuildingDefId = FactionDefaultContent.ArcaneAcademyId,
                    X = 80f,
                    Z = 0f,
                },
            });
            return builder.PathCount > 0;
        }

        private static bool ConstructionFinishes()
        {
            Setup(out var sim, out var wallet, out var ids, out var player);
            wallet.Seed(player, ResourceType.Gold, 500);
            wallet.Seed(player, ResourceType.Timber, 500);
            var site = sim.SpawnBuilding(
                ids.Next(),
                player,
                new FactionId(0),
                FactionDefaultContent.ArcaneAcademyId,
                30f,
                -30f,
                startActive: false);
            sim.SpawnUnit(
                ids.Next(), player, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 31f, -29f);
            for (int i = 0; i < 80; i++)
                sim.Tick(0.25f);
            return site.State == BuildingState.Active;
        }

        private static bool NoProgressFarAway()
        {
            Setup(out var sim, out var wallet, out var ids, out var player);
            var site = sim.SpawnBuilding(
                ids.Next(),
                player,
                new FactionId(0),
                FactionDefaultContent.ArcaneAcademyId,
                0f,
                0f,
                startActive: false);
            sim.SpawnUnit(
                ids.Next(),
                player,
                new FactionId(0),
                FactionDefaultContent.VeiledBuilderId,
                80f,
                80f);
            float before = site.BuildSecondsRemaining;
            for (int i = 0; i < 20; i++)
                sim.Tick(0.25f);
            return Abs(site.BuildSecondsRemaining - before) < 0.001f;
        }

        private static bool AttachSpendsTurretCost()
        {
            Setup(out var sim, out var wallet, out var ids, out var player);
            wallet.Seed(player, ResourceType.Gold, 500);
            wallet.Seed(player, ResourceType.Timber, 500);
            var keep = sim.SpawnBuilding(
                ids.Next(),
                player,
                new FactionId(0),
                FactionDefaultContent.ArcaneumId,
                -60f,
                -60f,
                startActive: true);
            if (!defs.TryGetBuilding(FactionDefaultContent.KeepTurretId, out var def))
                return false;
            int g0 = wallet.Get(player, ResourceType.Gold);
            sim.ApplyCommands(new GameCommand[]
            {
                new AttachBuildingCommand
                {
                    Issuer = player,
                    ParentBuildingId = keep.Id,
                    SlotIndex = 0,
                    BuildingDefId = FactionDefaultContent.KeepTurretId,
                },
            });
            return keep.AttachmentOccupantIds[0] != 0
                   && wallet.Get(player, ResourceType.Gold) == g0 - def.GoldCost;
        }

        private static bool SecondAttachSlot()
        {
            Setup(out var sim, out var wallet, out var ids, out var player);
            wallet.Seed(player, ResourceType.Gold, 500);
            wallet.Seed(player, ResourceType.Timber, 500);
            var keep = sim.SpawnBuilding(
                ids.Next(),
                player,
                new FactionId(0),
                FactionDefaultContent.ArcaneumId,
                90f,
                -90f,
                startActive: true);
            for (byte slot = 0; slot < 2; slot++)
            {
                sim.ApplyCommands(new GameCommand[]
                {
                    new AttachBuildingCommand
                    {
                        Issuer = player,
                        ParentBuildingId = keep.Id,
                        SlotIndex = slot,
                        BuildingDefId = FactionDefaultContent.KeepTurretId,
                    },
                });
            }

            return keep.AttachmentOccupantIds[0] != 0 && keep.AttachmentOccupantIds[1] != 0;
        }

        private static bool OccupiedSlotRejects()
        {
            Setup(out var sim, out var wallet, out var ids, out var player);
            wallet.Seed(player, ResourceType.Gold, 500);
            wallet.Seed(player, ResourceType.Timber, 500);
            var keep = sim.SpawnBuilding(
                ids.Next(),
                player,
                new FactionId(0),
                FactionDefaultContent.ArcaneumId,
                -120f,
                40f,
                startActive: true);
            sim.ApplyCommands(new GameCommand[]
            {
                new AttachBuildingCommand
                {
                    Issuer = player,
                    ParentBuildingId = keep.Id,
                    SlotIndex = 0,
                    BuildingDefId = FactionDefaultContent.KeepTurretId,
                },
            });
            uint first = keep.AttachmentOccupantIds[0];
            int count = sim.Buildings.Count;
            sim.ApplyCommands(new GameCommand[]
            {
                new AttachBuildingCommand
                {
                    Issuer = player,
                    ParentBuildingId = keep.Id,
                    SlotIndex = 0,
                    BuildingDefId = FactionDefaultContent.KeepTurretId,
                },
            });
            return keep.AttachmentOccupantIds[0] == first && sim.Buildings.Count == count;
        }

        private static void Setup(
            out SkirmishWorldSim sim,
            out ResourceWallet wallet,
            out SequentialIdFactory ids,
            out PlayerId player)
        {
            ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            player = new PlayerId(0);
            sim = new SkirmishWorldSim(wallet, ids, defs);
        }

        private static int CountConstructing(SkirmishWorldSim sim, PlayerId owner, string defId)
        {
            int n = 0;
            for (int i = 0; i < sim.Buildings.Count; i++)
            {
                var b = sim.Buildings[i];
                if (b.Owner == owner && b.DefinitionId == defId && b.State == BuildingState.Constructing)
                    n++;
            }

            return n;
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
