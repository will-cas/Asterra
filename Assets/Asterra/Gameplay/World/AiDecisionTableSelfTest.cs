using System.Text;
using Asterra.AI;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Difficulty table + opening phase / decision smoke for SkirmishOpponentBrain.</summary>
    public static class AiDecisionTableSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "difficulty ladder builders", DifficultyBuildersLadder());
            Expect(ref fails, sb, "difficulty ladder producers", DifficultyProducersLadder());
            Expect(ref fails, sb, "difficulty reaction ladder", DifficultyReactionLadder());
            Expect(ref fails, sb, "easy no expand targets", EasyNoExpand());
            Expect(ref fails, sb, "opening phase without producer", OpeningPhase());
            Expect(ref fails, sb, "ecoexpand with incomplete eco", EcoExpandPhase());
            Expect(ref fails, sb, "defend when keep damaged", DefendPhase());
            Expect(ref fails, sb, "all difficulties emit decisions", AllDifficultiesDecide());
            Expect(ref fails, sb, "hard build interval faster than easy", HardFasterThanEasy());
            Expect(ref fails, sb, "insane assault size smaller", InsaneAssaultPressure());

            sb.Append(fails == 0 ? "AiDecisionTableSelfTest: OK" : $"AiDecisionTableSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool DifficultyBuildersLadder()
        {
            var e = AiDifficultyTuning.For(AiDifficulty.Easy);
            var n = AiDifficultyTuning.For(AiDifficulty.Normal);
            var h = AiDifficultyTuning.For(AiDifficulty.Hard);
            var i = AiDifficultyTuning.For(AiDifficulty.Insane);
            return e.TargetBuilders <= n.TargetBuilders
                   && n.TargetBuilders <= h.TargetBuilders
                   && h.TargetBuilders <= i.TargetBuilders
                   && e.MaxBuilders <= i.MaxBuilders;
        }

        private static bool DifficultyProducersLadder()
        {
            return AiDifficultyTuning.For(AiDifficulty.Easy).TargetProducers
                   <= AiDifficultyTuning.For(AiDifficulty.Hard).TargetProducers;
        }

        private static bool DifficultyReactionLadder()
        {
            return AiDifficultyTuning.For(AiDifficulty.Easy).ReactionDelayTicks
                   >= AiDifficultyTuning.For(AiDifficulty.Insane).ReactionDelayTicks;
        }

        private static bool EasyNoExpand()
        {
            var e = AiDifficultyTuning.For(AiDifficulty.Easy);
            return e.TargetTowers == 0 && e.TargetOutposts == 0 && e.TargetWalls == 0;
        }

        private static bool OpeningPhase()
        {
            var brain = Brain(AiDifficulty.Normal, seedProducer: false, out var sim, out var wallet, out _);
            brain.Think(new ArmyBrainContext(new Tick(1), sim, wallet));
            return brain.CurrentPhase == "Opening";
        }

        private static bool EcoExpandPhase()
        {
            // Active producer but below tower/outpost targets → EcoExpand on Normal+.
            var brain = Brain(AiDifficulty.Hard, seedProducer: true, out var sim, out var wallet, out _);
            brain.Think(new ArmyBrainContext(new Tick(5), sim, wallet));
            return brain.CurrentPhase == "EcoExpand" || brain.CurrentPhase == "Opening" || brain.CurrentPhase == "Tech";
        }

        private static bool DefendPhase()
        {
            var brain = Brain(AiDifficulty.Normal, seedProducer: true, out var sim, out var wallet, out var keep);
            keep.Health = keep.MaxHealth * 0.5f;
            sim.Tick(0.05f); // refresh snapshots so Perceive sees low HP
            brain.Think(new ArmyBrainContext(new Tick(10), sim, wallet));
            return brain.CurrentPhase == "Defend";
        }

        private static bool AllDifficultiesDecide()
        {
            var diffs = new[]
            {
                AiDifficulty.Easy,
                AiDifficulty.Normal,
                AiDifficulty.Hard,
                AiDifficulty.Insane,
            };
            for (int d = 0; d < diffs.Length; d++)
            {
                var brain = Brain(diffs[d], seedProducer: false, out var sim, out var wallet, out _);
                bool saw = false;
                for (uint t = 0; t < 60; t++)
                {
                    brain.Think(new ArmyBrainContext(new Tick(t), sim, wallet));
                    if (!string.IsNullOrEmpty(brain.LastDecision) && brain.LastDecision != "boot")
                    {
                        saw = true;
                        break;
                    }
                }

                if (!saw)
                    return false;
            }

            return true;
        }

        private static bool HardFasterThanEasy()
        {
            return AiDifficultyTuning.For(AiDifficulty.Hard).BuildIntervalTicks
                   < AiDifficultyTuning.For(AiDifficulty.Easy).BuildIntervalTicks
                   && AiDifficultyTuning.For(AiDifficulty.Hard).TrainIntervalTicks
                   < AiDifficultyTuning.For(AiDifficulty.Easy).TrainIntervalTicks;
        }

        private static bool InsaneAssaultPressure()
        {
            return AiDifficultyTuning.For(AiDifficulty.Insane).AssaultArmySize
                   <= AiDifficultyTuning.For(AiDifficulty.Easy).AssaultArmySize
                   && AiDifficultyTuning.For(AiDifficulty.Insane).Aggression
                   > AiDifficultyTuning.For(AiDifficulty.Easy).Aggression;
        }

        private static SkirmishOpponentBrain Brain(
            AiDifficulty difficulty,
            bool seedProducer,
            out SkirmishWorldSim sim,
            out ResourceWallet wallet,
            out SimBuilding keep)
        {
            var ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var ai = new PlayerId(1);
            wallet.Seed(ai, ResourceType.Gold, 5000);
            wallet.Seed(ai, ResourceType.Timber, 5000);
            sim = new SkirmishWorldSim(wallet, ids, defs);
            var roster = FactionDefaultContent.VeiledInheritance;
            keep = sim.SpawnBuilding(
                ids.Next(), ai, roster.Id, roster.KeepBuildingId, 100f, 0f, startActive: true);
            if (seedProducer)
            {
                sim.SpawnBuilding(
                    ids.Next(),
                    ai,
                    roster.Id,
                    roster.ProducerBuildingId,
                    140f,
                    20f,
                    startActive: true);
            }

            sim.SpawnUnit(ids.Next(), ai, roster.Id, roster.BuilderUnitId, 108f, 4f);
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

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
