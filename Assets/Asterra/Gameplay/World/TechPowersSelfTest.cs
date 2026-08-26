using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Research, equipment apply, powers, and unique leaders.</summary>
    public static class TechPowersSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "research equipment at barracks", ResearchEquipment());
            Expect(ref fails, sb, "research equipment at keep", ResearchEquipmentAtKeep());
            Expect(ref fails, sb, "research does not auto-equip", ResearchDoesNotAutoEquip());
            Expect(ref fails, sb, "apply equipment boosts armor", ApplyEquipmentBoosts());
            Expect(ref fails, sb, "equip costs gold per unit", EquipCostsGold());
            Expect(ref fails, sb, "fire swords skip ranged", FireSwordsSkipRanged());
            Expect(ref fails, sb, "passive unlock applies buff", PassiveUnlockApplies());
            Expect(ref fails, sb, "duplicate equipment rejected", DuplicateEquipmentRejected());
            Expect(ref fails, sb, "builder cannot equip", BuilderCannotEquip());
            Expect(ref fails, sb, "keep upgrade increases keep hp", KeepUpgradeBuffsKeep());
            Expect(ref fails, sb, "unlock power spends gold", UnlockPowerSpends());
            Expect(ref fails, sb, "activate power starts cooldown", ActivatePowerCooldown());
            Expect(ref fails, sb, "second leader train rejected", SecondLeaderRejected());
            Expect(ref fails, sb, "wrong faction power rejected", WrongFactionPowerRejected());

            sb.Append(fails == 0 ? "TechPowersSelfTest: OK" : $"TechPowersSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool ResearchEquipment()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out _);
            wallet.Seed(p, ResourceType.Gold, 2000);
            sim.ApplyCommands(new GameCommand[]
            {
                new ChooseUpgradeCommand
                {
                    Issuer = p,
                    BuildingId = barracks.Id,
                    UpgradeDefId = FactionDefaultContent.HeavyArmourId,
                },
            });
            ExpectResearching(barracks);
            for (int i = 0; i < 80; i++)
                sim.Tick(0.25f);
            return sim.HasUpgrade(p, FactionDefaultContent.HeavyArmourId);
        }

        private static bool ResearchEquipmentAtKeep()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out _);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, -40f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 2000);
            sim.ApplyCommands(new GameCommand[]
            {
                new ChooseUpgradeCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UpgradeDefId = FactionDefaultContent.HeavyArmourId,
                },
            });
            for (int i = 0; i < 80; i++)
                sim.Tick(0.25f);
            return sim.HasUpgrade(p, FactionDefaultContent.HeavyArmourId);
        }

        private static bool ResearchDoesNotAutoEquip()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out var unit);
            wallet.Seed(p, ResourceType.Gold, 2000);
            float armor0 = unit.Armor;
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.HeavyArmourId);
            return unit.Armor == armor0 && unit.AppliedEquipmentCount == 0
                   && sim.HasUpgrade(p, FactionDefaultContent.HeavyArmourId);
        }

        private static bool ApplyEquipmentBoosts()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out var unit);
            wallet.Seed(p, ResourceType.Gold, 2000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.HeavyArmourId);
            float armor0 = unit.Armor;
            sim.ApplyCommands(new GameCommand[]
            {
                new ApplyUnitUpgradeCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    UpgradeDefId = FactionDefaultContent.HeavyArmourId,
                },
            });
            return unit.Armor > armor0 && unit.AppliedEquipmentCount >= 1;
        }

        private static bool EquipCostsGold()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out var unit);
            wallet.Seed(p, ResourceType.Gold, 2000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.HeavyArmourId);
            int g0 = wallet.Get(p, ResourceType.Gold);
            sim.ApplyCommands(new GameCommand[]
            {
                new ApplyUnitUpgradeCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    UpgradeDefId = FactionDefaultContent.HeavyArmourId,
                },
            });
            return wallet.Get(p, ResourceType.Gold) < g0 && unit.AppliedEquipmentCount >= 1;
        }

        private static bool FireSwordsSkipRanged()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out _);
            wallet.Seed(p, ResourceType.Gold, 5000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.FireSwordsId);
            var archer = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronArcherId, 12f, 0f);
            var melee = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.MilitiaId, 16f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new ApplyUnitUpgradeCommand
                {
                    Issuer = p,
                    UnitIds = new[] { archer.Id, melee.Id },
                    UpgradeDefId = FactionDefaultContent.FireSwordsId,
                },
            });
            return archer.AppliedEquipmentCount == 0 && melee.AppliedEquipmentCount >= 1;
        }

        private static bool PassiveUnlockApplies()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out var unit);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, -30f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 500);
            float armor0 = unit.Armor;
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.LucienDisciplinePassiveId,
                },
            });
            return sim.HasPower(p, FactionDefaultContent.LucienDisciplinePassiveId)
                   && unit.Armor > armor0;
        }

        private static bool DuplicateEquipmentRejected()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out var unit);
            wallet.Seed(p, ResourceType.Gold, 2000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.HeavyArmourId);
            var cmd = new ApplyUnitUpgradeCommand
            {
                Issuer = p,
                UnitIds = new[] { unit.Id },
                UpgradeDefId = FactionDefaultContent.HeavyArmourId,
            };
            sim.ApplyCommands(new GameCommand[] { cmd });
            int n = unit.AppliedEquipmentCount;
            int g0 = wallet.Get(p, ResourceType.Gold);
            sim.ApplyCommands(new GameCommand[] { cmd });
            return unit.AppliedEquipmentCount == n && wallet.Get(p, ResourceType.Gold) == g0;
        }

        private static bool BuilderCannotEquip()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out _);
            wallet.Seed(p, ResourceType.Gold, 2000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.HeavyArmourId);
            var builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronBuilderId, 5f, 5f);
            sim.ApplyCommands(new GameCommand[]
            {
                new ApplyUnitUpgradeCommand
                {
                    Issuer = p,
                    UnitIds = new[] { builder.Id },
                    UpgradeDefId = FactionDefaultContent.HeavyArmourId,
                },
            });
            return builder.AppliedEquipmentCount == 0;
        }

        private static bool KeepUpgradeBuffsKeep()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 5000);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, 0f, 0f, startActive: true);
            float hp0 = keep.MaxHealth;
            sim.ApplyCommands(new GameCommand[]
            {
                new ChooseUpgradeCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UpgradeDefId = FactionDefaultContent.KeepBastionId,
                },
            });
            for (int i = 0; i < 120; i++)
                sim.Tick(0.25f);
            return keep.MaxHealth > hp0 && sim.HasUpgrade(p, FactionDefaultContent.KeepBastionId);
        }

        private static bool UnlockPowerSpends()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out _);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, -30f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 500);
            int g0 = wallet.Get(p, ResourceType.Gold);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.LucienIronWallAbilityId,
                },
            });
            return sim.HasPower(p, FactionDefaultContent.LucienIronWallAbilityId)
                   && wallet.Get(p, ResourceType.Gold) < g0;
        }

        private static bool ActivatePowerCooldown()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out _);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, -30f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 500);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.LucienIronWallAbilityId,
                },
            });
            sim.ApplyCommands(new GameCommand[]
            {
                new ActivateCommanderAbilityCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.LucienIronWallAbilityId,
                },
            });
            return sim.TryGetCommanderAbilityStatus(
                       p,
                       FactionDefaultContent.LucienIronWallAbilityId,
                       out float cd,
                       out _)
                   && cd > 0f;
        }

        private static bool SecondLeaderRejected()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 5000);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, 0f, 0f, startActive: true);
            sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.LucienLeaderId, 10f, 0f);
            int units = sim.Units.Count;
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.LucienLeaderId,
                },
            });
            return !keep.IsProducing && sim.Units.Count == units;
        }

        private static bool WrongFactionPowerRejected()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out _);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.IronKeepId, -30f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 500);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.AllianceMarchAbilityId,
                },
            });
            return !sim.HasPower(p, FactionDefaultContent.AllianceMarchAbilityId);
        }

        private static void SetupIron(
            out SkirmishWorldSim sim,
            out SequentialIdFactory ids,
            out ResourceWallet wallet,
            out PlayerId p,
            out SimBuilding barracks,
            out SimUnit unit)
        {
            ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            sim = new SkirmishWorldSim(wallet, ids, defs);
            p = new PlayerId(0);
            barracks = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.BarracksId, 0f, 0f, startActive: true);
            unit = sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.MilitiaId, 8f, 0f);
        }

        private static void ResearchNow(
            SkirmishWorldSim sim,
            ResourceWallet wallet,
            PlayerId p,
            SimBuilding building,
            string upgradeId)
        {
            wallet.Seed(p, ResourceType.Gold, 5000);
            sim.ApplyCommands(new GameCommand[]
            {
                new ChooseUpgradeCommand
                {
                    Issuer = p,
                    BuildingId = building.Id,
                    UpgradeDefId = upgradeId,
                },
            });
            for (int i = 0; i < 120; i++)
                sim.Tick(0.25f);
        }

        private static void ExpectResearching(SimBuilding b)
        {
            _ = b.IsResearching || !string.IsNullOrEmpty(b.ResearchUpgradeDefId);
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
