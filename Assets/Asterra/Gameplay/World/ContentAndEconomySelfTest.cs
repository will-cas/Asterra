using System.Text;
using Asterra.AI;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Faction defs, wallet, combat, and difficulty invariants.</summary>
    public static class ContentAndEconomySelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "three factions registered", ThreeFactions());
            Expect(ref fails, sb, "each faction has keep+producer+builder", FactionCoreIds());
            Expect(ref fails, sb, "each faction has cavalry+scout+sapper", FactionCombatRoster());
            Expect(ref fails, sb, "keeps allow keep turret only", KeepsAllowTurret());
            Expect(ref fails, sb, "wallet spend and afford", WalletBasics());
            Expect(ref fails, sb, "wallet reject overspend", WalletReject());
            Expect(ref fails, sb, "melee damages enemy unit", MeleeDamagesEnemy());
            Expect(ref fails, sb, "tower attacks nearby enemy", TowerAttacksEnemy());
            Expect(ref fails, sb, "difficulty monotonic aggression", DifficultyMonotonic());
            Expect(ref fails, sb, "easy target towers zero", EasyNoTowers());
            Expect(ref fails, sb, "hard wants two producers", HardTwoProducers());
            Expect(ref fails, sb, "difficulty cycle wraps", DifficultyCycle());
            Expect(ref fails, sb, "outpost and wall defs exist", FortificationDefs());
            Expect(ref fails, sb, "train builder from keep", TrainBuilderFromKeep());
            Expect(ref fails, sb, "cancel production half-refund", CancelProductionRefunds());
            Expect(ref fails, sb, "blackridge starts workers only", BlackridgeStartsWorkersOnly());
            Expect(ref fails, sb, "twin keeps starts workers only", TwinKeepsStartsWorkersOnly());

            sb.Append(fails == 0 ? "ContentAndEconomySelfTest: OK" : $"ContentAndEconomySelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool BlackridgeStartsWorkersOnly()
        {
            return StartingCombatCount(MapCatalog.BlackridgePassId, new PlayerId(0)) == 0
                   && StartingBuilderCount(MapCatalog.BlackridgePassId, new PlayerId(0)) >= 1;
        }

        private static bool TwinKeepsStartsWorkersOnly()
        {
            return StartingCombatCount(MapCatalog.TwinKeepsId, new PlayerId(0)) == 0
                   && StartingBuilderCount(MapCatalog.TwinKeepsId, new PlayerId(0)) >= 1;
        }

        private static int StartingCombatCount(string mapKey, PlayerId player)
        {
            BootMap(mapKey, out var sim);
            int n = 0;
            for (int i = 0; i < sim.Units.Count; i++)
            {
                var u = sim.Units[i];
                if (u.Owner != player || !u.IsAlive)
                    continue;
                if (FactionDefaultContent.IsBuilderUnitId(u.DefinitionId))
                    continue;
                if (u.DefinitionId == FactionDefaultContent.RiverBoatId)
                    continue;
                n++;
            }

            return n;
        }

        private static int StartingBuilderCount(string mapKey, PlayerId player)
        {
            BootMap(mapKey, out var sim);
            int n = 0;
            for (int i = 0; i < sim.Units.Count; i++)
            {
                var u = sim.Units[i];
                if (u.Owner == player && u.IsAlive && FactionDefaultContent.IsBuilderUnitId(u.DefinitionId))
                    n++;
            }

            return n;
        }

        private static void BootMap(string mapKey, out SkirmishWorldSim sim)
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();
            sim = new SkirmishWorldSim(wallet, ids, defs);
            var seats = new[]
            {
                new PlayerSlotState
                {
                    Player = new PlayerId(0),
                    FactionIndex = 0,
                    IsReady = true,
                    DisplayName = "P0",
                },
                new PlayerSlotState
                {
                    Player = new PlayerId(1),
                    FactionIndex = 1,
                    IsReady = true,
                    DisplayName = "P1",
                },
            };
            SkirmishDefaultContent.PopulateFromSlots(sim, ids, seats, mapKey);
        }

        private static bool ThreeFactions()
        {
            var all = FactionDefaultContent.All;
            return all != null && all.Length >= 3;
        }

        private static bool FactionCoreIds()
        {
            var all = FactionDefaultContent.All;
            for (int i = 0; i < all.Length; i++)
            {
                var r = all[i];
                if (string.IsNullOrEmpty(r.KeepBuildingId)
                    || string.IsNullOrEmpty(r.ProducerBuildingId)
                    || string.IsNullOrEmpty(r.BuilderUnitId)
                    || string.IsNullOrEmpty(r.BasicUnitId))
                    return false;
            }

            return true;
        }

        private static bool FactionCombatRoster()
        {
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var all = FactionDefaultContent.All;
            for (int i = 0; i < all.Length; i++)
            {
                var r = all[i];
                if (string.IsNullOrEmpty(r.CavalryUnitId)
                    || string.IsNullOrEmpty(r.ScoutUnitId)
                    || string.IsNullOrEmpty(r.SapperUnitId)
                    || string.IsNullOrEmpty(r.SiegeUnitId)
                    || string.IsNullOrEmpty(r.RangedUnitId))
                    return false;
                if (!defs.TryGetUnit(r.CavalryUnitId, out var cav) || cav.Role != UnitRole.Cavalry)
                    return false;
                if (!defs.TryGetUnit(r.ScoutUnitId, out _) || !defs.TryGetUnit(r.SapperUnitId, out var sapper))
                    return false;
                if (sapper.BuildingDamageMultiplier < 2f)
                    return false;
                if (!defs.TryGetBuilding(r.ProducerBuildingId, out var producer)
                    || producer.TrainableUnitIds == null
                    || !Contains(producer.TrainableUnitIds, r.CavalryUnitId)
                    || !Contains(producer.TrainableUnitIds, r.ScoutUnitId)
                    || !Contains(producer.TrainableUnitIds, r.SapperUnitId))
                    return false;
                if (!string.IsNullOrEmpty(r.EliteUnitId)
                    && (!defs.TryGetUnit(r.EliteUnitId, out _)
                        || !Contains(producer.TrainableUnitIds, r.EliteUnitId)))
                    return false;
            }

            return true;
        }

        private static bool Contains(string[] ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id))
                return false;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == id)
                    return true;
            }

            return false;
        }

        private static bool KeepsAllowTurret()
        {
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            string[] keeps =
            {
                FactionDefaultContent.IronKeepId,
                FactionDefaultContent.HeartwoodId,
                FactionDefaultContent.AshCitadelId,
            };
            for (int i = 0; i < keeps.Length; i++)
            {
                if (!defs.TryGetBuilding(keeps[i], out var def))
                    return false;
                if (def.AttachmentSlotCount < 1)
                    return false;
                if (def.AttachmentAllowedBuildingIds == null || def.AttachmentAllowedBuildingIds.Length == 0)
                    return false;
                bool ok = false;
                for (int a = 0; a < def.AttachmentAllowedBuildingIds.Length; a++)
                {
                    if (def.AttachmentAllowedBuildingIds[a] == FactionDefaultContent.KeepTurretId)
                        ok = true;
                    if (def.AttachmentAllowedBuildingIds[a] == FactionDefaultContent.WatchtowerId)
                        return false;
                }

                if (!ok)
                    return false;
            }

            return true;
        }

        private static bool WalletBasics()
        {
            var w = new ResourceWallet();
            var p = new PlayerId(0);
            w.Seed(p, ResourceType.Gold, 100);
            w.Add(p, ResourceType.Gold, 25);
            if (w.Get(p, ResourceType.Gold) != 125)
                return false;
            if (!w.TrySpend(p, ResourceType.Gold, 25))
                return false;
            return w.Get(p, ResourceType.Gold) == 100 && w.CanAfford(p, ResourceType.Gold, 100);
        }

        private static bool WalletReject()
        {
            var w = new ResourceWallet();
            var p = new PlayerId(2);
            w.Seed(p, ResourceType.Timber, 10);
            return !w.TrySpend(p, ResourceType.Timber, 11) && w.Get(p, ResourceType.Timber) == 10;
        }

        private static bool MeleeDamagesEnemy()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p0 = new PlayerId(0);
            var p1 = new PlayerId(1);
            var a = sim.SpawnUnit(ids.Next(), p0, new FactionId(0), FactionDefaultContent.MilitiaId, 0f, 0f);
            var b = sim.SpawnUnit(ids.Next(), p1, new FactionId(1), FactionDefaultContent.DryadId, 3f, 0f);
            float hp = b.Health;
            sim.ApplyCommands(new GameCommand[]
            {
                new AttackCommand { Issuer = p0, UnitIds = new[] { a.Id }, TargetId = b.Id },
            });
            for (int i = 0; i < 40; i++)
                sim.Tick(0.25f);
            return b.Health < hp || !b.IsAlive;
        }

        private static bool TowerAttacksEnemy()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p0 = new PlayerId(0);
            var p1 = new PlayerId(1);
            sim.SpawnBuilding(
                ids.Next(), p0, new FactionId(0), FactionDefaultContent.WatchtowerId, 0f, 0f, startActive: true);
            var enemy = sim.SpawnUnit(
                ids.Next(), p1, new FactionId(1), FactionDefaultContent.DryadId, 12f, 0f);
            float hp = enemy.Health;
            for (int i = 0; i < 60; i++)
                sim.Tick(0.25f);
            return enemy.Health < hp || !enemy.IsAlive;
        }

        private static bool DifficultyMonotonic()
        {
            var easy = AiDifficultyTuning.For(AiDifficulty.Easy);
            var normal = AiDifficultyTuning.For(AiDifficulty.Normal);
            var hard = AiDifficultyTuning.For(AiDifficulty.Hard);
            var insane = AiDifficultyTuning.For(AiDifficulty.Insane);
            return easy.Aggression < normal.Aggression
                   && normal.Aggression < hard.Aggression
                   && hard.Aggression <= insane.Aggression
                   && easy.ReactionDelayTicks >= normal.ReactionDelayTicks
                   && normal.ReactionDelayTicks >= hard.ReactionDelayTicks;
        }

        private static bool EasyNoTowers() =>
            AiDifficultyTuning.For(AiDifficulty.Easy).TargetTowers == 0;

        private static bool HardTwoProducers() =>
            AiDifficultyTuning.For(AiDifficulty.Hard).TargetProducers >= 2;

        private static bool DifficultyCycle()
        {
            return AiDifficultyTuning.Cycle(AiDifficulty.Insane, 1) == AiDifficulty.Easy
                   && AiDifficultyTuning.Cycle(AiDifficulty.Easy, -1) == AiDifficulty.Insane;
        }

        private static bool FortificationDefs()
        {
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            return defs.TryGetBuilding(FactionDefaultContent.OutpostId, out _)
                   && defs.TryGetBuilding(FactionDefaultContent.PalisadeId, out _)
                   && defs.TryGetBuilding(FactionDefaultContent.WatchtowerId, out _)
                   && defs.TryGetBuilding(FactionDefaultContent.KeepTurretId, out _);
        }

        private static bool TrainBuilderFromKeep()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 500);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, 0f, 0f, startActive: true);
            int unitsBefore = CountBuilders(sim, p);
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.IronBuilderId,
                },
            });
            for (int i = 0; i < 80; i++)
                sim.Tick(0.25f);
            return CountBuilders(sim, p) > unitsBefore;
        }

        private static bool CancelProductionRefunds()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 500);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, 20f, 20f, startActive: true);
            if (!defs.TryGetUnit(FactionDefaultContent.IronBuilderId, out var unitDef))
                return false;
            int g0 = wallet.Get(p, ResourceType.Gold);
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.IronBuilderId,
                },
            });
            int afterTrain = wallet.Get(p, ResourceType.Gold);
            if (afterTrain != g0 - unitDef.GoldCost)
                return false;
            sim.ApplyCommands(new GameCommand[]
            {
                new CancelProductionCommand { Issuer = p, BuildingId = keep.Id },
            });
            // Active production refunds half cost.
            return wallet.Get(p, ResourceType.Gold) == afterTrain + unitDef.GoldCost / 2;
        }

        private static int CountBuilders(SkirmishWorldSim sim, PlayerId owner)
        {
            int n = 0;
            for (int i = 0; i < sim.Units.Count; i++)
            {
                var u = sim.Units[i];
                if (u.Owner == owner && u.IsAlive && FactionDefaultContent.IsBuilderUnitId(u.DefinitionId))
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
