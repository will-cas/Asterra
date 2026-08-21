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
    /// Three asymmetric factions as data only (Notion: Aurelian Dominion, Concord, Eternal Flame).
    /// Internal definition ids stay stable; display names match the wiki.
    /// </summary>
    public static class FactionDefaultContent
    {
        public const string IronCovenantId = "faction_iron_covenant"; // Aurelian Dominion
        public const string VerdantCourtId = "faction_verdant_court"; // Concord of the Free Realms
        public const string AshenLegionId = "faction_ashen_legion"; // Order of the Eternal Flame

        // Aurelian Dominion (ids historically "iron_*")
        public const string MilitiaId = "unit_militia";
        public const string IronBuilderId = "unit_iron_builder";
        public const string IronArcherId = "unit_iron_archer";
        public const string IronKnightId = "unit_iron_knight";
        public const string IronCatapultId = "unit_iron_catapult";
        public const string BarracksId = "building_barracks";
        public const string IronKeepId = "building_iron_keep";
        public const string MilitiaTrainingId = "upgrade_militia_training";

        // Lucien Vale — Iron Wall (Aurelian commander active)
        public const string LucienIronWallAbilityId = "ability_lucien_iron_wall";
        public const float LucienIronWallArmorBonus = 3f;
        public const float LucienIronWallBuildingMitigation = 4f;
        public const float LucienIronWallDurationSeconds = 12f;
        public const float LucienIronWallCooldownSeconds = 45f;

        // Shared vehicles / traversal specialists
        public const string RiverBoatId = "unit_river_boat";
        public const string PathfinderId = "unit_pathfinder";

        // Shared fortifications
        public const string WatchtowerId = "building_watchtower";
        public const string PalisadeId = "building_palisade";
        public const string OutpostId = "building_outpost";

        // Concord of the Free Realms
        public const string DryadId = "unit_dryad";
        public const string VerdantBuilderId = "unit_verdant_builder";
        public const string VerdantArcherId = "unit_verdant_archer";
        public const string VerdantKnightId = "unit_verdant_knight";
        public const string VerdantCatapultId = "unit_verdant_catapult";
        public const string GroveId = "building_grove";
        public const string HeartwoodId = "building_heartwood";
        public const string WildGrowthId = "upgrade_wild_growth";

        // Order of the Eternal Flame
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
            DisplayName = "Aurelian Dominion",
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
            LoreBlurb = "Disciplined soldiers, engineering, and battlefield control.",
        };

        public static readonly FactionRoster VerdantCourt = new FactionRoster
        {
            Id = new FactionId(1),
            DefinitionId = VerdantCourtId,
            DisplayName = "Concord of the Free Realms",
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
            LoreBlurb = "Alliance spearmen, free rangers, and flexible mercenary companies.",
        };

        public static readonly FactionRoster AshenLegion = new FactionRoster
        {
            Id = new FactionId(2),
            DefinitionId = AshenLegionId,
            DisplayName = "Order of the Eternal Flame",
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
            LoreBlurb = "Sacred warriors and ritual fire dominating open battle.",
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
            // Aurelian Dominion
            registry.Register(new UnitDefData
            {
                Id = MilitiaId,
                DisplayName = "Legionnaire",
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
                DisplayName = "Engineer",
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
                DisplayName = "Dominion Archer",
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
                DisplayName = "Iron Guard",
                MaxHealth = 150f,
                MoveSpeed = 3.6f,
                AttackDamage = 15f,
                AttackRange = 1.5f,
                AttackCooldown = 1.15f,
                GoldCost = 120,
                TrainSeconds = 7f,
                Role = UnitRole.Infantry,
                Armor = 4f,
            });
            registry.Register(new UnitDefData
            {
                Id = IronCatapultId,
                DisplayName = "Siege Cannon",
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
                DisplayName = "Fortress",
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
                DisplayName = "Legion Discipline",
                GoldCost = 150,
                TrainTimeMultiplier = 0.75f,
                UnitDamageMultiplier = 1.25f,
            });

            // Concord of the Free Realms
            registry.Register(new UnitDefData
            {
                Id = DryadId,
                DisplayName = "Alliance Spearman",
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
                DisplayName = "Forest Guardian",
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
                DisplayName = "Free Ranger",
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
                DisplayName = "Mercenary Rider",
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
                DisplayName = "Alliance Ballista",
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
                DisplayName = "Alliance Hall",
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
                DisplayName = "Concord Hold",
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
                DisplayName = "Alliance Pact",
                GoldCost = 140,
                TrainTimeMultiplier = 0.7f,
                UnitDamageMultiplier = 1.15f,
            });

            // Order of the Eternal Flame
            registry.Register(new UnitDefData
            {
                Id = EmberRaiderId,
                DisplayName = "Sacred Warrior",
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
                DisplayName = "Ritualist",
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
                DisplayName = "Flame Acolyte",
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
                DisplayName = "Fire Mage",
                MaxHealth = 85f,
                MoveSpeed = 4.6f,
                AttackDamage = 18f,
                AttackRange = 9f,
                AttackCooldown = 1.35f,
                GoldCost = 110,
                TrainSeconds = 6.2f,
                Role = UnitRole.Ranged,
                Armor = 1f,
                ProjectileSpeed = 40f,
            });
            registry.Register(new UnitDefData
            {
                Id = AshenCatapultId,
                DisplayName = "Ancient Guardian",
                MaxHealth = 140f,
                MoveSpeed = 2.4f,
                AttackDamage = 26f,
                AttackRange = 8f,
                AttackCooldown = 2.6f,
                GoldCost = 160,
                TrainSeconds = 11f,
                Role = UnitRole.Siege,
                BuildingDamageMultiplier = 3.8f,
                Armor = 3f,
                ProjectileSpeed = 28f,
            });
            registry.Register(new BuildingDefData
            {
                Id = ForgeId,
                DisplayName = "Sanctum Forge",
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
                DisplayName = "Flame Sanctum",
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
                DisplayName = "Sacred Flame",
                GoldCost = 160,
                TrainTimeMultiplier = 0.85f,
                UnitDamageMultiplier = 1.35f,
            });

            // Shared fortifications / economy
            registry.Register(new UnitDefData
            {
                Id = RiverBoatId,
                DisplayName = "River Boat",
                MaxHealth = 220f,
                MoveSpeed = 6.5f,
                AttackDamage = 8f,
                AttackRange = 10f,
                AttackCooldown = 1.6f,
                GoldCost = 120,
                TrainSeconds = 8f,
                Role = UnitRole.Siege,
                Armor = 2f,
                ProjectileSpeed = 28f,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Water,
            });
            registry.Register(new UnitDefData
            {
                Id = PathfinderId,
                DisplayName = "Pathfinder",
                MaxHealth = 70f,
                MoveSpeed = 5.8f,
                AttackDamage = 9f,
                AttackRange = 2f,
                AttackCooldown = 1f,
                GoldCost = 70,
                TrainSeconds = 5f,
                Role = UnitRole.Infantry,
                TraversalCapabilities = Asterra.Core.World.TraversalCapability.Land
                    | Asterra.Core.World.TraversalCapability.Jump,
            });
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
