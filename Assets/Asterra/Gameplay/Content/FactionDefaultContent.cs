using Asterra.Core;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay.Content
{
    /// <summary>Plain-data roster for one of the three launch factions.</summary>
    public sealed class FactionRoster
    {
        public FactionId Id;
        public string DefinitionId;
        public string DisplayName;
        public string KeepBuildingId;
        public string ProducerBuildingId;
        public string BasicUnitId;
        public string BuilderUnitId;
        public string RangedUnitId;
        public string CavalryUnitId;
        public string SiegeUnitId;
        public string TowerBuildingId;
        public string WallBuildingId;
        public string OutpostBuildingId;
        public string BasicUpgradeId;
        public string LoreBlurb;
    }

    /// <summary>
    /// Three asymmetric factions as data only (Iron Covenant, Verdant Court, Ashen Legion).
    /// </summary>
    public static class FactionDefaultContent
    {
        public const string IronCovenantId = "faction_iron_covenant";
        public const string VerdantCourtId = "faction_verdant_court";
        public const string AshenLegionId = "faction_ashen_legion";

        // Iron Covenant
        public const string MilitiaId = "unit_militia";
        public const string IronBuilderId = "unit_iron_builder";
        public const string IronArcherId = "unit_iron_archer";
        public const string IronKnightId = "unit_iron_knight";
        public const string IronCatapultId = "unit_iron_catapult";
        public const string BarracksId = "building_barracks";
        public const string IronKeepId = "building_iron_keep";
        public const string MilitiaTrainingId = "upgrade_militia_training";

        // Shared fortifications
        public const string WatchtowerId = "building_watchtower";
        public const string PalisadeId = "building_palisade";
        public const string OutpostId = "building_outpost";

        // Verdant Court
        public const string DryadId = "unit_dryad";
        public const string VerdantBuilderId = "unit_verdant_builder";
        public const string VerdantArcherId = "unit_verdant_archer";
        public const string VerdantKnightId = "unit_verdant_knight";
        public const string VerdantCatapultId = "unit_verdant_catapult";
        public const string GroveId = "building_grove";
        public const string HeartwoodId = "building_heartwood";
        public const string WildGrowthId = "upgrade_wild_growth";

        // Ashen Legion
        public const string EmberRaiderId = "unit_ember_raider";
        public const string AshenBuilderId = "unit_ashen_builder";
        public const string AshenArcherId = "unit_ashen_archer";
        public const string AshenKnightId = "unit_ashen_knight";
        public const string AshenCatapultId = "unit_ashen_catapult";
        public const string ForgeId = "building_forge";
        public const string AshCitadelId = "building_ash_citadel";
        public const string EmberRitesId = "upgrade_ember_rites";

        public static readonly FactionRoster IronCovenant = new FactionRoster
        {
            Id = new FactionId(0),
            DefinitionId = IronCovenantId,
            DisplayName = "Iron Covenant",
            KeepBuildingId = IronKeepId,
            ProducerBuildingId = BarracksId,
            BasicUnitId = MilitiaId,
            BuilderUnitId = IronBuilderId,
            RangedUnitId = IronArcherId,
            CavalryUnitId = IronKnightId,
            SiegeUnitId = IronCatapultId,
            TowerBuildingId = WatchtowerId,
            WallBuildingId = PalisadeId,
            OutpostBuildingId = OutpostId,
            BasicUpgradeId = MilitiaTrainingId,
            LoreBlurb = "Disciplined steel and drilled infantry.",
        };

        public static readonly FactionRoster VerdantCourt = new FactionRoster
        {
            Id = new FactionId(1),
            DefinitionId = VerdantCourtId,
            DisplayName = "Verdant Court",
            KeepBuildingId = HeartwoodId,
            ProducerBuildingId = GroveId,
            BasicUnitId = DryadId,
            BuilderUnitId = VerdantBuilderId,
            RangedUnitId = VerdantArcherId,
            CavalryUnitId = VerdantKnightId,
            SiegeUnitId = VerdantCatapultId,
            TowerBuildingId = WatchtowerId,
            WallBuildingId = PalisadeId,
            OutpostBuildingId = OutpostId,
            BasicUpgradeId = WildGrowthId,
            LoreBlurb = "Living wood and swift skirmishers.",
        };

        public static readonly FactionRoster AshenLegion = new FactionRoster
        {
            Id = new FactionId(2),
            DefinitionId = AshenLegionId,
            DisplayName = "Ashen Legion",
            KeepBuildingId = AshCitadelId,
            ProducerBuildingId = ForgeId,
            BasicUnitId = EmberRaiderId,
            BuilderUnitId = AshenBuilderId,
            RangedUnitId = AshenArcherId,
            CavalryUnitId = AshenKnightId,
            SiegeUnitId = AshenCatapultId,
            TowerBuildingId = WatchtowerId,
            WallBuildingId = PalisadeId,
            OutpostBuildingId = OutpostId,
            BasicUpgradeId = EmberRitesId,
            LoreBlurb = "Raid doctrine and scorched-earth forges.",
        };

        public static FactionRoster[] All { get; } =
        {
            IronCovenant,
            VerdantCourt,
            AshenLegion,
        };

        public static FactionRoster Get(FactionId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                    return All[i];
            }

            return IronCovenant;
        }

        public static bool IsBuilderUnitId(string definitionId)
        {
            return definitionId == IronBuilderId
                   || definitionId == VerdantBuilderId
                   || definitionId == AshenBuilderId
                   || (definitionId != null && definitionId.Contains("builder"));
        }

        public static bool IsKeepBuildingId(string definitionId)
        {
            return definitionId == IronKeepId
                   || definitionId == HeartwoodId
                   || definitionId == AshCitadelId;
        }

        public static void RegisterAll(DefinitionRegistry registry)
        {
            // Iron
            registry.Register(new UnitDefData
            {
                Id = MilitiaId,
                DisplayName = "Militia",
                MaxHealth = 100f,
                MoveSpeed = 5f,
                AttackDamage = 12f,
                AttackRange = 1.75f,
                AttackCooldown = 1f,
                GoldCost = 50,
                TrainSeconds = 4f,
                Role = UnitRole.Infantry,
                Armor = 1f,
            });
            registry.Register(new UnitDefData
            {
                Id = IronBuilderId,
                DisplayName = "Sapper",
                MaxHealth = 60f,
                MoveSpeed = 4.5f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 30,
                TrainSeconds = 2.8f,
                IsBuilder = true,
                CanGather = true,
                CarryCapacity = 15,
                GatherRate = 6f,
                Role = UnitRole.Builder,
            });
            registry.Register(new UnitDefData
            {
                Id = IronArcherId,
                DisplayName = "Longbow",
                MaxHealth = 65f,
                MoveSpeed = 4.8f,
                AttackDamage = 14f,
                AttackRange = 14f,
                AttackCooldown = 1.2f,
                GoldCost = 60,
                TrainSeconds = 4.5f,
                Role = UnitRole.Ranged,
                ProjectileSpeed = 48f,
            });
            registry.Register(new UnitDefData
            {
                Id = IronKnightId,
                DisplayName = "Knight",
                MaxHealth = 120f,
                MoveSpeed = 8f,
                AttackDamage = 16f,
                AttackRange = 1.6f,
                AttackCooldown = 1.05f,
                GoldCost = 105,
                TrainSeconds = 6f,
                Role = UnitRole.Cavalry,
                Armor = 2f,
            });
            registry.Register(new UnitDefData
            {
                Id = IronCatapultId,
                DisplayName = "Catapult",
                MaxHealth = 90f,
                MoveSpeed = 2.8f,
                AttackDamage = 22f,
                AttackRange = 10f,
                AttackCooldown = 2.2f,
                GoldCost = 140,
                TrainSeconds = 9.5f,
                Role = UnitRole.Siege,
                BuildingDamageMultiplier = 3.5f,
                Armor = 1f,
                ProjectileSpeed = 32f,
            });
            registry.Register(new BuildingDefData
            {
                Id = BarracksId,
                DisplayName = "Barracks",
                MaxHealth = 600f,
                GoldCost = 120,
                TimberCost = 80,
                BuildSeconds = 6f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                SightRadius = 85f,
                TrainableUnitIds = new[] { MilitiaId, IronArcherId, IronKnightId, IronCatapultId },
            });
            registry.Register(new BuildingDefData
            {
                Id = IronKeepId,
                DisplayName = "Iron Keep",
                MaxHealth = 1200f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                SightRadius = 160f,
                TrainableUnitIds = new[] { IronBuilderId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = MilitiaTrainingId,
                DisplayName = "Militia Training",
                GoldCost = 150,
                TrainTimeMultiplier = 0.75f,
                UnitDamageMultiplier = 1.25f,
            });

            // Verdant
            registry.Register(new UnitDefData
            {
                Id = DryadId,
                DisplayName = "Dryad",
                MaxHealth = 80f,
                MoveSpeed = 6.5f,
                AttackDamage = 10f,
                AttackRange = 2.5f,
                AttackCooldown = 0.9f,
                GoldCost = 55,
                TrainSeconds = 3.5f,
                Role = UnitRole.Infantry,
                Armor = 1f,
            });
            registry.Register(new UnitDefData
            {
                Id = VerdantBuilderId,
                DisplayName = "Grovewright",
                MaxHealth = 55f,
                MoveSpeed = 5f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 28,
                TrainSeconds = 2.5f,
                IsBuilder = true,
                CanGather = true,
                CarryCapacity = 15,
                GatherRate = 6f,
                Role = UnitRole.Builder,
            });
            registry.Register(new UnitDefData
            {
                Id = VerdantArcherId,
                DisplayName = "Thornbow",
                MaxHealth = 55f,
                MoveSpeed = 5.5f,
                AttackDamage = 12f,
                AttackRange = 15f,
                AttackCooldown = 1.1f,
                GoldCost = 55,
                TrainSeconds = 4f,
                Role = UnitRole.Ranged,
                ProjectileSpeed = 52f,
            });
            registry.Register(new UnitDefData
            {
                Id = VerdantKnightId,
                DisplayName = "Stag Rider",
                MaxHealth = 100f,
                MoveSpeed = 8.5f,
                AttackDamage = 14f,
                AttackRange = 1.8f,
                AttackCooldown = 0.95f,
                GoldCost = 100,
                TrainSeconds = 5.5f,
                Role = UnitRole.Cavalry,
                Armor = 2f,
            });
            registry.Register(new UnitDefData
            {
                Id = VerdantCatapultId,
                DisplayName = "Bramble Engine",
                MaxHealth = 80f,
                MoveSpeed = 3f,
                AttackDamage = 20f,
                AttackRange = 10f,
                AttackCooldown = 2f,
                GoldCost = 130,
                TrainSeconds = 9f,
                Role = UnitRole.Siege,
                BuildingDamageMultiplier = 3.2f,
                Armor = 1f,
                ProjectileSpeed = 30f,
            });
            registry.Register(new BuildingDefData
            {
                Id = GroveId,
                DisplayName = "Grove",
                MaxHealth = 500f,
                GoldCost = 100,
                TimberCost = 120,
                BuildSeconds = 5f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                SightRadius = 85f,
                TrainableUnitIds = new[] { DryadId, VerdantArcherId, VerdantKnightId, VerdantCatapultId },
            });
            registry.Register(new BuildingDefData
            {
                Id = HeartwoodId,
                DisplayName = "Heartwood",
                MaxHealth = 1100f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                SightRadius = 160f,
                TrainableUnitIds = new[] { VerdantBuilderId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = WildGrowthId,
                DisplayName = "Wild Growth",
                GoldCost = 140,
                TrainTimeMultiplier = 0.7f,
                UnitDamageMultiplier = 1.15f,
            });

            // Ashen
            registry.Register(new UnitDefData
            {
                Id = EmberRaiderId,
                DisplayName = "Ember Raider",
                MaxHealth = 90f,
                MoveSpeed = 5.5f,
                AttackDamage = 15f,
                AttackRange = 1.5f,
                AttackCooldown = 1.1f,
                GoldCost = 60,
                TrainSeconds = 4.5f,
                Role = UnitRole.Infantry,
                Armor = 1f,
            });
            registry.Register(new UnitDefData
            {
                Id = AshenBuilderId,
                DisplayName = "Ashwright",
                MaxHealth = 65f,
                MoveSpeed = 4.8f,
                AttackDamage = 0f,
                AttackRange = 0f,
                AttackCooldown = 1f,
                GoldCost = 32,
                TrainSeconds = 2.9f,
                IsBuilder = true,
                CanGather = true,
                CarryCapacity = 15,
                GatherRate = 6f,
                Role = UnitRole.Builder,
            });
            registry.Register(new UnitDefData
            {
                Id = AshenArcherId,
                DisplayName = "Cinder Bow",
                MaxHealth = 60f,
                MoveSpeed = 5f,
                AttackDamage = 15f,
                AttackRange = 13f,
                AttackCooldown = 1.15f,
                GoldCost = 65,
                TrainSeconds = 4.8f,
                Role = UnitRole.Ranged,
                ProjectileSpeed = 50f,
            });
            registry.Register(new UnitDefData
            {
                Id = AshenKnightId,
                DisplayName = "Ash Rider",
                MaxHealth = 110f,
                MoveSpeed = 8.2f,
                AttackDamage = 17f,
                AttackRange = 1.5f,
                AttackCooldown = 1f,
                GoldCost = 110,
                TrainSeconds = 6.2f,
                Role = UnitRole.Cavalry,
                Armor = 2f,
            });
            registry.Register(new UnitDefData
            {
                Id = AshenCatapultId,
                DisplayName = "Ember Mortar",
                MaxHealth = 95f,
                MoveSpeed = 2.6f,
                AttackDamage = 24f,
                AttackRange = 10.5f,
                AttackCooldown = 2.4f,
                GoldCost = 150,
                TrainSeconds = 10f,
                Role = UnitRole.Siege,
                BuildingDamageMultiplier = 3.8f,
                Armor = 1f,
                ProjectileSpeed = 28f,
            });
            registry.Register(new BuildingDefData
            {
                Id = ForgeId,
                DisplayName = "Forge",
                MaxHealth = 650f,
                GoldCost = 140,
                TimberCost = 60,
                BuildSeconds = 7f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Producer,
                SightRadius = 85f,
                TrainableUnitIds = new[] { EmberRaiderId, AshenArcherId, AshenKnightId, AshenCatapultId },
            });
            registry.Register(new BuildingDefData
            {
                Id = AshCitadelId,
                DisplayName = "Ash Citadel",
                MaxHealth = 1250f,
                CanProduce = true,
                QueueCapacity = 3,
                Kind = BuildingKind.Keep,
                SightRadius = 160f,
                TrainableUnitIds = new[] { AshenBuilderId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = EmberRitesId,
                DisplayName = "Ember Rites",
                GoldCost = 160,
                TrainTimeMultiplier = 0.85f,
                UnitDamageMultiplier = 1.35f,
            });

            // Shared fortifications / economy
            registry.Register(new BuildingDefData
            {
                Id = WatchtowerId,
                DisplayName = "Watchtower",
                MaxHealth = 450f,
                GoldCost = 90,
                TimberCost = 70,
                BuildSeconds = 5f,
                FootprintX = 5f,
                FootprintZ = 5f,
                Kind = BuildingKind.Tower,
                AttackDamage = 14f,
                AttackRange = 22f,
                AttackCooldown = 1.4f,
                SightRadius = 150f,
            });
            registry.Register(new BuildingDefData
            {
                Id = PalisadeId,
                DisplayName = "Palisade",
                MaxHealth = 700f,
                GoldCost = 40,
                TimberCost = 90,
                BuildSeconds = 4f,
                FootprintX = 14f,
                FootprintZ = 4f,
                Kind = BuildingKind.Wall,
                SightRadius = 40f,
            });
            registry.Register(new BuildingDefData
            {
                Id = OutpostId,
                DisplayName = "Outpost",
                MaxHealth = 380f,
                GoldCost = 110,
                TimberCost = 50,
                BuildSeconds = 5.5f,
                FootprintX = 6f,
                FootprintZ = 6f,
                Kind = BuildingKind.Outpost,
                SightRadius = 130f,
                GoldPerSecond = 4,
            });
        }
    }
}
