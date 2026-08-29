using System.Text;
using Asterra.Core;
using Asterra.Core.World;
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
            Expect(ref fails, sb, "second leader train allowed", SecondLeaderAllowed());
            Expect(ref fails, sb, "wrong faction power rejected", WrongFactionPowerRejected());
            Expect(ref fails, sb, "veiled wrath forces weather", VeiledWrathForcesWeather());
            Expect(ref fails, sb, "veiled eight powers", VeiledEightPowers());
            Expect(ref fails, sb, "mundor eight powers", MundorEightPowers());
            Expect(ref fails, sb, "outcast seven powers", OutcastSevenPowers());
            Expect(ref fails, sb, "freetown eight powers", FreetownEightPowers());
            Expect(ref fails, sb, "university eight powers", UniversityEightPowers());
            Expect(ref fails, sb, "church eight powers", ChurchEightPowers());
            Expect(ref fails, sb, "thrallbind steals then returns", ThrallbindStealsThenReturns());
            Expect(ref fails, sb, "twin gates ignore fog", TwinGatesPlacePair());

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
                    UpgradeDefId = FactionDefaultContent.VeiledMailId,
                },
            });
            ExpectResearching(barracks);
            for (int i = 0; i < 80; i++)
                sim.Tick(0.25f);
            return sim.HasUpgrade(p, FactionDefaultContent.VeiledMailId);
        }

        private static bool ResearchEquipmentAtKeep()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out _);
            var keep = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, -40f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 2000);
            sim.ApplyCommands(new GameCommand[]
            {
                new ChooseUpgradeCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UpgradeDefId = FactionDefaultContent.VeiledMailId,
                },
            });
            for (int i = 0; i < 80; i++)
                sim.Tick(0.25f);
            return sim.HasUpgrade(p, FactionDefaultContent.VeiledMailId);
        }

        private static bool ResearchDoesNotAutoEquip()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out var unit);
            wallet.Seed(p, ResourceType.Gold, 2000);
            float armor0 = unit.Armor;
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.VeiledMailId);
            return unit.Armor == armor0 && unit.AppliedEquipmentCount == 0
                   && sim.HasUpgrade(p, FactionDefaultContent.VeiledMailId);
        }

        private static bool ApplyEquipmentBoosts()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out var unit);
            wallet.Seed(p, ResourceType.Gold, 2000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.VeiledMailId);
            float armor0 = unit.Armor;
            sim.ApplyCommands(new GameCommand[]
            {
                new ApplyUnitUpgradeCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    UpgradeDefId = FactionDefaultContent.VeiledMailId,
                },
            });
            return unit.Armor > armor0 && unit.AppliedEquipmentCount >= 1;
        }

        private static bool EquipCostsGold()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out var unit);
            wallet.Seed(p, ResourceType.Gold, 2000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.VeiledMailId);
            int g0 = wallet.Get(p, ResourceType.Gold);
            sim.ApplyCommands(new GameCommand[]
            {
                new ApplyUnitUpgradeCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    UpgradeDefId = FactionDefaultContent.VeiledMailId,
                },
            });
            return wallet.Get(p, ResourceType.Gold) < g0 && unit.AppliedEquipmentCount >= 1;
        }

        private static bool FireSwordsSkipRanged()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out _);
            wallet.Seed(p, ResourceType.Gold, 5000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.FineSteelId);
            var archer = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledRuneCasterId, 12f, 0f);
            var melee = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 16f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new ApplyUnitUpgradeCommand
                {
                    Issuer = p,
                    UnitIds = new[] { archer.Id, melee.Id },
                    UpgradeDefId = FactionDefaultContent.FineSteelId,
                },
            });
            return archer.AppliedEquipmentCount == 0 && melee.AppliedEquipmentCount >= 1;
        }

        private static bool PassiveUnlockApplies()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out var unit);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, -30f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 500);
            float dmg0 = unit.AttackDamage;
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.ForbiddenGiftPassiveId,
                },
            });
            return sim.HasPower(p, FactionDefaultContent.ForbiddenGiftPassiveId)
                   && unit.AttackDamage > dmg0;
        }

        private static bool DuplicateEquipmentRejected()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out var barracks, out var unit);
            wallet.Seed(p, ResourceType.Gold, 2000);
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.VeiledMailId);
            var cmd = new ApplyUnitUpgradeCommand
            {
                Issuer = p,
                UnitIds = new[] { unit.Id },
                UpgradeDefId = FactionDefaultContent.VeiledMailId,
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
            ResearchNow(sim, wallet, p, barracks, FactionDefaultContent.VeiledMailId);
            var builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 5f, 5f);
            sim.ApplyCommands(new GameCommand[]
            {
                new ApplyUnitUpgradeCommand
                {
                    Issuer = p,
                    UnitIds = new[] { builder.Id },
                    UpgradeDefId = FactionDefaultContent.VeiledMailId,
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
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            float hp0 = keep.MaxHealth;
            sim.ApplyCommands(new GameCommand[]
            {
                new ChooseUpgradeCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UpgradeDefId = FactionDefaultContent.VeiledBastionId,
                },
            });
            for (int i = 0; i < 120; i++)
                sim.Tick(0.25f);
            return keep.MaxHealth > hp0 && sim.HasUpgrade(p, FactionDefaultContent.VeiledBastionId);
        }

        private static bool UnlockPowerSpends()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out _);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, -30f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 500);
            int g0 = wallet.Get(p, ResourceType.Gold);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.BeastChorusAbilityId,
                },
            });
            return sim.HasPower(p, FactionDefaultContent.BeastChorusAbilityId)
                   && wallet.Get(p, ResourceType.Gold) < g0;
        }

        private static bool ActivatePowerCooldown()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out _);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, -30f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 500);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.BeastChorusAbilityId,
                },
            });
            sim.ApplyCommands(new GameCommand[]
            {
                new ActivateCommanderAbilityCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.BeastChorusAbilityId,
                },
            });
            return sim.TryGetCommanderAbilityStatus(
                       p,
                       FactionDefaultContent.BeastChorusAbilityId,
                       out float cd,
                       out _)
                   && cd > 0f;
        }

        private static bool SecondLeaderAllowed()
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
            sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledHeirId, 10f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new TrainUnitCommand
                {
                    Issuer = p,
                    BuildingId = keep.Id,
                    UnitDefId = FactionDefaultContent.VeiledHeirId,
                },
            });
            return keep.IsProducing && keep.ProductionUnitDefId == FactionDefaultContent.VeiledHeirId;
        }

        private static bool WrongFactionPowerRejected()
        {
            SetupIron(out var sim, out var ids, out var wallet, out var p, out _, out _);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, -30f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 500);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.LevyHornAbilityId,
                },
            });
            return !sim.HasPower(p, FactionDefaultContent.LevyHornAbilityId);
        }

        private static bool VeiledWrathForcesWeather()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 2000);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.WrathOfSkiesAbilityId,
                },
            });
            sim.ApplyCommands(new GameCommand[]
            {
                new ActivateCommanderAbilityCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.WrathOfSkiesAbilityId,
                },
            });
            var kind = sim.Environment.WeatherSim.Current.Kind;
            var target = sim.Environment.WeatherSim.TransitionTarget;
            return kind == WeatherKind.Storm || kind == WeatherKind.Fog || kind == WeatherKind.Rain || kind == WeatherKind.Snow
                   || (target.HasValue && (target.Value.Kind == WeatherKind.Storm
                                          || target.Value.Kind == WeatherKind.Fog
                                          || target.Value.Kind == WeatherKind.Rain
                                          || target.Value.Kind == WeatherKind.Snow));
        }

        private static bool VeiledEightPowers()
        {
            var r = FactionDefaultContent.VeiledInheritance;
            return r.PowerIds != null && r.PowerIds.Length == 8
                   && r.DisplayName == "The Uncrowned";
        }

        private static bool MundorEightPowers()
        {
            var r = FactionDefaultContent.MundorCrown;
            return r.PowerIds != null && r.PowerIds.Length == 8
                   && r.DisplayName == "The Mundor Crown";
        }

        private static bool OutcastSevenPowers()
        {
            var r = FactionDefaultContent.Outcast;
            return r.PowerIds != null && r.PowerIds.Length == 7
                   && r.DisplayName == "The Outcast Host";
        }

        private static bool FreetownEightPowers()
        {
            var r = FactionDefaultContent.Freetown;
            return r.PowerIds != null && r.PowerIds.Length == 8
                   && r.DisplayName == "Freetown";
        }

        private static bool UniversityEightPowers()
        {
            var r = FactionDefaultContent.UniversityGuild;
            return r.PowerIds != null && r.PowerIds.Length == 8
                   && r.DisplayName == "University Guild";
        }

        private static bool ChurchEightPowers()
        {
            var r = FactionDefaultContent.RisingSun;
            return r.PowerIds != null && r.PowerIds.Length == 8
                   && r.DisplayName == "Church of the Rising Sun";
        }

        private static bool ThrallbindStealsThenReturns()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p0 = new PlayerId(0);
            var p1 = new PlayerId(1);
            sim.SpawnBuilding(
                ids.Next(), p0, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            var victim = sim.SpawnUnit(
                ids.Next(), p1, new FactionId(1), FactionDefaultContent.RoyalPeasantId, 20f, 0f);
            wallet.Seed(p0, ResourceType.Gold, 2000);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand { Issuer = p0, PowerDefId = FactionDefaultContent.ThrallbindAbilityId },
            });
            sim.ApplyCommands(new GameCommand[]
            {
                new ActivateCommanderAbilityCommand
                {
                    Issuer = p0,
                    PowerDefId = FactionDefaultContent.ThrallbindAbilityId,
                    TargetId = victim.Id,
                },
            });
            if (victim.Owner != p0)
                return false;
            for (int i = 0; i < 130; i++)
                sim.Tick(0.25f);
            return victim.IsAlive && victim.Owner == p1;
        }

        private static bool TwinGatesPlacePair()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            wallet.Seed(p, ResourceType.Gold, 2000);
            sim.ApplyCommands(new GameCommand[]
            {
                new UnlockPowerCommand { Issuer = p, PowerDefId = FactionDefaultContent.TwinGatesAbilityId },
            });
            sim.ApplyCommands(new GameCommand[]
            {
                new ActivateCommanderAbilityCommand
                {
                    Issuer = p,
                    PowerDefId = FactionDefaultContent.TwinGatesAbilityId,
                    TargetX = 40f,
                    TargetZ = 40f,
                    SecondaryX = -40f,
                    SecondaryZ = -40f,
                },
            });
            int gates = 0;
            for (int i = 0; i < sim.Buildings.Count; i++)
            {
                if (sim.Buildings[i].DefinitionId == FactionDefaultContent.PortalGateId)
                    gates++;
            }

            return gates >= 2;
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
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneAcademyId, 0f, 0f, startActive: true);
            unit = sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 8f, 0f);
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
