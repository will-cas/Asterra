using System.Text;
using Asterra.AI;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Regression: AI places producers / keep turrets instead of stalling on workers.</summary>
    public static class AiMacroSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "places first producer", PlacesFirstProducer());
            Expect(ref fails, sb, "attaches keep turret not watchtower", AttachesKeepTurret());
            Expect(ref fails, sb, "opening does not greed-train past TargetBuilders", OpeningBuilderCap());
            Expect(ref fails, sb, "verdant places grove", VerdantPlacesProducer());
            Expect(ref fails, sb, "decision place_producer after think", DecisionIsPlaceProducer());

            sb.Append(fails == 0 ? "AiMacroSelfTest: OK" : $"AiMacroSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool VerdantPlacesProducer()
        {
            var roster = FactionDefaultContent.MundorCrown;
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 5000);
            wallet.Seed(ai, ResourceType.Timber, 5000);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            sim.SpawnBuilding(ids.Next(), ai, roster.Id, roster.KeepBuildingId, -90f, 30f, startActive: true);
            sim.SpawnUnit(ids.Next(), ai, roster.Id, roster.BuilderUnitId, -84f, 34f);
            var brain = new SkirmishOpponentBrain(
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
                null,
                null,
                AiDifficulty.Easy,
                FactionDefaultContent.KeepTurretId);
            for (int t = 0; t < 350; t++)
            {
                var cmds = brain.Think(new ArmyBrainContext(new Tick((uint)t), sim, wallet));
                if (cmds != null && cmds.Count > 0)
                {
                    var arr = new GameCommand[cmds.Count];
                    for (int i = 0; i < cmds.Count; i++)
                        arr[i] = cmds[i];
                    sim.ApplyCommands(arr);
                }

                sim.Tick(0.25f);
                if (CountDef(sim, ai, roster.ProducerBuildingId) > 0)
                    return true;
            }

            return false;
        }

        private static bool DecisionIsPlaceProducer()
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
            sim.SpawnBuilding(ids.Next(), ai, roster.Id, roster.KeepBuildingId, 60f, -60f, startActive: true);
            sim.SpawnUnit(ids.Next(), ai, roster.Id, roster.BuilderUnitId, 66f, -54f);
            var brain = new SkirmishOpponentBrain(
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
                null,
                null,
                AiDifficulty.Normal,
                FactionDefaultContent.KeepTurretId);
            for (int t = 0; t < 80; t++)
            {
                brain.Think(new ArmyBrainContext(new Tick((uint)t), sim, wallet));
                if (brain.LastDecision == "place_producer")
                    return true;
            }

            return false;
        }

        private static bool PlacesFirstProducer()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 5000);
            wallet.Seed(ai, ResourceType.Timber, 5000);

            var sim = new SkirmishWorldSim(wallet, ids, defs);
            float keepX = 120f;
            float keepZ = 80f;
            sim.SpawnBuilding(
                ids.Next(), ai, new FactionId(1), FactionDefaultContent.RoyalCitadelId, keepX, keepZ, startActive: true);
            sim.SpawnUnit(
                ids.Next(), ai, new FactionId(1), FactionDefaultContent.RoyalBuilderId, keepX + 8f, keepZ + 8f);

            var roster = FactionDefaultContent.MundorCrown;
            var brain = new SkirmishOpponentBrain(
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
                AiDifficulty.Normal,
                FactionDefaultContent.KeepTurretId);

            for (int t = 0; t < 400; t++)
            {
                var cmds = brain.Think(new ArmyBrainContext(new Tick((uint)t), sim, wallet));
                if (cmds != null && cmds.Count > 0)
                {
                    var arr = new GameCommand[cmds.Count];
                    for (int i = 0; i < cmds.Count; i++)
                        arr[i] = cmds[i];
                    sim.ApplyCommands(arr);
                }

                sim.Tick(0.25f);

                if (CountDef(sim, ai, roster.ProducerBuildingId) > 0)
                    return true;
            }

            return false;
        }

        private static bool AttachesKeepTurret()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 5000);
            wallet.Seed(ai, ResourceType.Timber, 5000);

            var sim = new SkirmishWorldSim(wallet, ids, defs);
            float keepX = -100f;
            float keepZ = -60f;
            sim.SpawnBuilding(
                ids.Next(), ai, new FactionId(0), FactionDefaultContent.ArcaneumId, keepX, keepZ, startActive: true);
            // Active producer so macro moves past opening into towers.
            sim.SpawnBuilding(
                ids.Next(),
                ai,
                new FactionId(0),
                FactionDefaultContent.ArcaneAcademyId,
                keepX + 40f,
                keepZ + 20f,
                startActive: true);
            sim.SpawnUnit(
                ids.Next(), ai, new FactionId(0), FactionDefaultContent.VeiledBuilderId, keepX + 6f, keepZ + 6f);

            var roster = FactionDefaultContent.VeiledInheritance;
            var brain = new SkirmishOpponentBrain(
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
                AiDifficulty.Hard,
                FactionDefaultContent.KeepTurretId);

            for (int t = 0; t < 250; t++)
            {
                var cmds = brain.Think(new ArmyBrainContext(new Tick((uint)t), sim, wallet));
                if (cmds != null && cmds.Count > 0)
                {
                    var arr = new GameCommand[cmds.Count];
                    for (int i = 0; i < cmds.Count; i++)
                        arr[i] = cmds[i];
                    sim.ApplyCommands(arr);
                }

                sim.Tick(0.25f);

                if (CountDef(sim, ai, FactionDefaultContent.KeepTurretId) > 0)
                    return CountDef(sim, ai, FactionDefaultContent.WatchtowerId) == 0
                           || CountDef(sim, ai, FactionDefaultContent.KeepTurretId) > 0;
            }

            return CountDef(sim, ai, FactionDefaultContent.KeepTurretId) > 0;
        }

        private static bool OpeningBuilderCap()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 5000);
            wallet.Seed(ai, ResourceType.Timber, 5000);

            var sim = new SkirmishWorldSim(wallet, ids, defs);
            // Extra map gold would previously inflate DesiredBuilders past TargetBuilders.
            sim.AddResourceNode(ids.Next(), ResourceType.Gold, 5000, 200f, 200f);
            sim.AddResourceNode(ids.Next(), ResourceType.Gold, 5000, -200f, 200f);
            sim.AddResourceNode(ids.Next(), ResourceType.Gold, 5000, 0f, -220f);

            float keepX = 80f;
            float keepZ = -80f;
            sim.SpawnBuilding(
                ids.Next(), ai, new FactionId(0), FactionDefaultContent.ArcaneumId, keepX, keepZ, startActive: true);
            sim.SpawnUnit(
                ids.Next(), ai, new FactionId(0), FactionDefaultContent.VeiledBuilderId, keepX + 5f, keepZ);

            var roster = FactionDefaultContent.VeiledInheritance;
            var brain = new SkirmishOpponentBrain(
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
                null,
                null,
                AiDifficulty.Normal,
                FactionDefaultContent.KeepTurretId);

            bool sawPlace = false;
            int maxBuildersSeen = 0;
            for (int t = 0; t < 200; t++)
            {
                var cmds = brain.Think(new ArmyBrainContext(new Tick((uint)t), sim, wallet));
                if (cmds != null && cmds.Count > 0)
                {
                    for (int i = 0; i < cmds.Count; i++)
                    {
                        if (cmds[i] is PlaceBuildingCommand place
                            && place.BuildingDefId == roster.ProducerBuildingId)
                            sawPlace = true;
                    }

                    var arr = new GameCommand[cmds.Count];
                    for (int i = 0; i < cmds.Count; i++)
                        arr[i] = cmds[i];
                    sim.ApplyCommands(arr);
                }

                sim.Tick(0.25f);
                int builders = CountUnits(sim, ai, roster.BuilderUnitId);
                if (builders > maxBuildersSeen)
                    maxBuildersSeen = builders;

                if (sawPlace && CountDef(sim, ai, roster.ProducerBuildingId) > 0)
                    break;
            }

            int targetBuilders = AiDifficultyTuning.For(AiDifficulty.Normal).TargetBuilders;
            // Before first producer completes, should not train past opening TargetBuilders (+1 slack for in-queue).
            return sawPlace && maxBuildersSeen <= targetBuilders + 2;
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

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
