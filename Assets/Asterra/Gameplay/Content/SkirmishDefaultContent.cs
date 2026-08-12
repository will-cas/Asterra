using Asterra.Core;
using Asterra.Gameplay;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay.Content
{
    /// <summary>
    /// Hardcoded Phase-1 content so the sim runs without ScriptableObject assets in the Editor.
    /// </summary>
    public static class SkirmishDefaultContent
    {
        public const string MilitiaId = "unit_militia";
        public const string BarracksId = "building_barracks";
        public const string KeepId = "building_keep";
        public const string MilitiaTrainingId = "upgrade_militia_training";

        public const byte PlayerFaction = 0;
        public const byte EnemyFaction = 1;

        public static DefinitionRegistry CreateRegistry()
        {
            var registry = new DefinitionRegistry();
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
                TrainableUnitIds = new[] { MilitiaId },
            });
            registry.Register(new BuildingDefData
            {
                Id = KeepId,
                DisplayName = "Keep",
                MaxHealth = 1200f,
                GoldCost = 0,
                TimberCost = 0,
                BuildSeconds = 0f,
                CanProduce = true,
                TrainableUnitIds = new[] { MilitiaId },
            });
            registry.Register(new UpgradeDefData
            {
                Id = MilitiaTrainingId,
                DisplayName = "Militia Training",
                GoldCost = 150,
                TrainTimeMultiplier = 0.75f,
                UnitDamageMultiplier = 1.25f,
            });
            return registry;
        }

        /// <summary>~1 km play space: player west, contested center, enemy east.</summary>
        public static void PopulateInitialWorld(SkirmishWorldSim world, IIdFactory ids)
        {
            var player = new PlayerId(0);
            var enemy = new PlayerId(1);
            var pFaction = new FactionId(PlayerFaction);
            var eFaction = new FactionId(EnemyFaction);

            world.SpawnBuilding(ids.Next(), player, pFaction, KeepId, -350f, 0f, startActive: true);
            world.SpawnBuilding(ids.Next(), enemy, eFaction, KeepId, 350f, 0f, startActive: true);

            world.SpawnUnit(ids.Next(), player, pFaction, MilitiaId, -320f, -20f);
            world.SpawnUnit(ids.Next(), player, pFaction, MilitiaId, -320f, 0f);
            world.SpawnUnit(ids.Next(), player, pFaction, MilitiaId, -320f, 20f);

            world.SpawnUnit(ids.Next(), enemy, eFaction, MilitiaId, 320f, -15f);
            world.SpawnUnit(ids.Next(), enemy, eFaction, MilitiaId, 320f, 15f);

            world.AddTerritory(ids.Next(), 0f, 0f, radius: 40f, goldPerSecond: 8);
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2000, -80f, 60f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1500, -100f, -70f);
        }
    }
}
