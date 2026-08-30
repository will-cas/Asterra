using System.Text;
using Asterra.AI;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Extra AI macro regressions beyond the opening place/attach suite.</summary>
    public static class AiMacroExtendedSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "hard places second producer", HardPlacesSecondProducer());
            Expect(ref fails, sb, "easy never attaches towers", EasyNeverAttachesTowers());
            Expect(ref fails, sb, "place decision blocks gather same tick", PlaceBlocksGatherSameTick());
            Expect(ref fails, sb, "assist moves idle builder to site", AssistMovesToSite());
            Expect(ref fails, sb, "normal trains combat after producer", TrainsCombatAfterProducer());
            Expect(ref fails, sb, "hard places free watchtower after turrets", PlacesWatchtowerAfterTurrets());
            Expect(ref fails, sb, "insane opening still places producer", InsanePlacesProducer());
            Expect(ref fails, sb, "ashen camp places forge", AshenPlacesProducer());

            sb.Append(fails == 0 ? "AiMacroExtendedSelfTest: OK" : $"AiMacroExtendedSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool HardPlacesSecondProducer()
        {
            return RunUntil(
                AiDifficulty.Hard,
                FactionDefaultContent.VeiledInheritance,
                seedActiveProducer: true,
                wantDef: FactionDefaultContent.VeiledInheritance.ProducerBuildingId,
                wantCount: 2,
                ticks: 550);
        }

        private static bool EasyNeverAttachesTowers()
        {
            var roster = FactionDefaultContent.VeiledInheritance;
            SetupCamp(
                roster,
                AiDifficulty.Easy,
                seedActiveProducer: true,
                out var sim,
                out var wallet,
                out var brain,
                out var ai);
            for (int t = 0; t < 300; t++)
                Step(brain, sim, wallet, t);
            return CountDef(sim, ai, FactionDefaultContent.KeepTurretId) == 0
                   && CountDef(sim, ai, FactionDefaultContent.WatchtowerId) == 0;
        }

        private static bool PlaceBlocksGatherSameTick()
        {
            var roster = FactionDefaultContent.Outcast;
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 8000);
            wallet.Seed(ai, ResourceType.Timber, 8000);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            float kx = 100f, kz = 20f;
            sim.SpawnBuilding(ids.Next(), ai, roster.Id, roster.KeepBuildingId, kx, kz, startActive: true);
            sim.SpawnUnit(ids.Next(), ai, roster.Id, roster.BuilderUnitId, kx + 6f, kz + 4f);
            sim.SpawnUnit(ids.Next(), ai, roster.Id, roster.BuilderUnitId, kx + 10f, kz + 2f);
            sim.AddResourceNode(ids.Next(), ResourceType.Gold, 4000, kx + 18f, kz + 8f);
            var brain = Brain(roster, ai, AiDifficulty.Normal);

            bool sawPlaceWithoutGather = false;
            for (int t = 0; t < 120; t++)
            {
                var cmds = brain.Think(new ArmyBrainContext(new Tick((uint)t), sim, wallet));
                bool place = false;
                bool gather = false;
                if (cmds != null)
                {
                    for (int i = 0; i < cmds.Count; i++)
                    {
                        if (cmds[i] is PlaceBuildingCommand)
                            place = true;
                        if (cmds[i] is GatherCommand)
                            gather = true;
                    }

                    Apply(sim, cmds);
                }

                if (place && gather)
                    return false;
                if (place && !gather)
                    sawPlaceWithoutGather = true;
                sim.Tick(0.25f);
            }

            return sawPlaceWithoutGather;
        }

        private static bool AssistMovesToSite()
        {
            var roster = FactionDefaultContent.VeiledInheritance;
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 5000);
            wallet.Seed(ai, ResourceType.Timber, 5000);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            float kx = 50f, kz = 50f;
            sim.SpawnBuilding(ids.Next(), ai, roster.Id, roster.KeepBuildingId, kx, kz, startActive: true);
            sim.SpawnBuilding(
                ids.Next(),
                ai,
                roster.Id,
                roster.ProducerBuildingId,
                kx + 40f,
                kz,
                startActive: false);
            var builder = sim.SpawnUnit(ids.Next(), ai, roster.Id, roster.BuilderUnitId, kx - 5f, kz - 5f);
            var brain = Brain(roster, ai, AiDifficulty.Normal);
            for (int t = 0; t < 40; t++)
            {
                Step(brain, sim, wallet, t);
                if (builder.PathCount > 0 || Dist2(builder.X, builder.Z, kx + 40f, kz) < 20f * 20f)
                    return true;
            }

            return false;
        }

        private static bool TrainsCombatAfterProducer()
        {
            var roster = FactionDefaultContent.VeiledInheritance;
            SetupCamp(
                roster,
                AiDifficulty.Normal,
                seedActiveProducer: true,
                out var sim,
                out var wallet,
                out var brain,
                out var ai,
                builders: 3);
            for (int t = 0; t < 400; t++)
            {
                Step(brain, sim, wallet, t);
                if (CountUnits(sim, ai, roster.BasicUnitId) > 0
                    || CountUnits(sim, ai, roster.RangedUnitId) > 0
                    || CountUnits(sim, ai, roster.CavalryUnitId) > 0)
                    return true;
            }

            return false;
        }

        private static bool PlacesWatchtowerAfterTurrets()
        {
            var roster = FactionDefaultContent.VeiledInheritance;
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 8000);
            wallet.Seed(ai, ResourceType.Timber, 8000);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            float kx = -70f, kz = 70f;
            var keep = sim.SpawnBuilding(ids.Next(), ai, roster.Id, roster.KeepBuildingId, kx, kz, startActive: true);
            sim.SpawnBuilding(
                ids.Next(), ai, roster.Id, roster.ProducerBuildingId, kx + 40f, kz - 10f, startActive: true);
            sim.SpawnUnit(ids.Next(), ai, roster.Id, roster.BuilderUnitId, kx + 8f, kz);
            for (byte s = 0; s < keep.AttachmentSlotCount; s++)
            {
                sim.ApplyCommands(new GameCommand[]
                {
                    new AttachBuildingCommand
                    {
                        Issuer = ai,
                        ParentBuildingId = keep.Id,
                        SlotIndex = s,
                        BuildingDefId = FactionDefaultContent.KeepTurretId,
                    },
                });
            }

            var brain = Brain(roster, ai, AiDifficulty.Hard);
            for (int t = 0; t < 350; t++)
            {
                Step(brain, sim, wallet, t);
                if (CountDef(sim, ai, FactionDefaultContent.WatchtowerId) > 0)
                    return true;
            }

            return false;
        }

        private static bool InsanePlacesProducer()
        {
            return RunUntil(
                AiDifficulty.Insane,
                FactionDefaultContent.VeiledInheritance,
                seedActiveProducer: false,
                wantDef: FactionDefaultContent.VeiledInheritance.ProducerBuildingId,
                wantCount: 1,
                ticks: 350);
        }

        private static bool AshenPlacesProducer()
        {
            return RunUntil(
                AiDifficulty.Normal,
                FactionDefaultContent.Freetown,
                seedActiveProducer: false,
                wantDef: FactionDefaultContent.Freetown.ProducerBuildingId,
                wantCount: 1,
                ticks: 400);
        }

        private static bool RunUntil(
            AiDifficulty difficulty,
            FactionRoster roster,
            bool seedActiveProducer,
            string wantDef,
            int wantCount,
            int ticks)
        {
            SetupCamp(roster, difficulty, seedActiveProducer, out var sim, out var wallet, out var brain, out var ai);
            for (int t = 0; t < ticks; t++)
            {
                Step(brain, sim, wallet, t);
                if (CountDef(sim, ai, wantDef) >= wantCount)
                    return true;
            }

            return CountDef(sim, ai, wantDef) >= wantCount;
        }

        private static void SetupCamp(
            FactionRoster roster,
            AiDifficulty difficulty,
            bool seedActiveProducer,
            out SkirmishWorldSim sim,
            out ResourceWallet wallet,
            out SkirmishOpponentBrain brain,
            out PlayerId ai,
            int builders = 1)
        {
            var ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 8000);
            wallet.Seed(ai, ResourceType.Timber, 8000);
            sim = new SkirmishWorldSim(wallet, ids, defs);
            float kx = 110f, kz = -40f;
            sim.SpawnBuilding(ids.Next(), ai, roster.Id, roster.KeepBuildingId, kx, kz, startActive: true);
            if (seedActiveProducer)
            {
                sim.SpawnBuilding(
                    ids.Next(),
                    ai,
                    roster.Id,
                    roster.ProducerBuildingId,
                    kx + 38f,
                    kz + 18f,
                    startActive: true);
            }

            for (int i = 0; i < builders; i++)
                sim.SpawnUnit(ids.Next(), ai, roster.Id, roster.BuilderUnitId, kx + 6f + i * 3f, kz + 4f);
            brain = Brain(roster, ai, difficulty);
        }

        private static SkirmishOpponentBrain Brain(FactionRoster roster, PlayerId ai, AiDifficulty difficulty)
        {
            return new SkirmishOpponentBrain(
                ai,
                roster.KeepBuildingId,
                roster.ProducerBuildingId,
                roster.BuilderUnitId,
                roster.BasicUnitId,
                roster.RangedUnitId,
                roster.CavalryUnitId,
                roster.TowerBuildingId,
                roster.OutpostBuildingId,
                roster.WallBuildingId,
                roster.BasicUpgradeId,
                roster.PowerId,
                difficulty,
                FactionDefaultContent.KeepTurretId);
        }

        private static void Step(SkirmishOpponentBrain brain, SkirmishWorldSim sim, IResourceWallet wallet, int t)
        {
            var cmds = brain.Think(new ArmyBrainContext(new Tick((uint)t), sim, wallet));
            Apply(sim, cmds);
            sim.Tick(0.25f);
        }

        private static void Apply(SkirmishWorldSim sim, System.Collections.Generic.IReadOnlyList<GameCommand> cmds)
        {
            if (cmds == null || cmds.Count == 0)
                return;
            var arr = new GameCommand[cmds.Count];
            for (int i = 0; i < cmds.Count; i++)
                arr[i] = cmds[i];
            sim.ApplyCommands(arr);
        }

        private static int CountDef(SkirmishWorldSim sim, PlayerId owner, string defId)
        {
            int n = 0;
            for (int i = 0; i < sim.Buildings.Count; i++)
            {
                var b = sim.Buildings[i];
                if (b.Owner == owner && b.DefinitionId == defId && b.State != BuildingState.Destroyed)
                    n++;
            }

            return n;
        }

        private static int CountUnits(SkirmishWorldSim sim, PlayerId owner, string defId)
        {
            int n = 0;
            for (int i = 0; i < sim.Units.Count; i++)
            {
                var u = sim.Units[i];
                if (u.Owner == owner && u.IsAlive && u.DefinitionId == defId)
                    n++;
            }

            return n;
        }

        private static float Dist2(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return dx * dx + dz * dz;
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
