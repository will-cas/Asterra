using Asterra.Core;
using Asterra.Gameplay;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay.Content
{
    /// <summary>
    /// Phase-1 skirmish layout helpers. Unit/building defs live in <see cref="FactionDefaultContent"/>.
    /// </summary>
    public static class SkirmishDefaultContent
    {
        public const string MilitiaId = FactionDefaultContent.MilitiaId;
        public const string BarracksId = FactionDefaultContent.BarracksId;
        public const string KeepId = FactionDefaultContent.IronKeepId;
        public const string MilitiaTrainingId = FactionDefaultContent.MilitiaTrainingId;

        public const byte PlayerFaction = 0;
        public const byte EnemyFaction = 1;

        public static DefinitionRegistry CreateRegistry()
        {
            var registry = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(registry);
            return registry;
        }

        /// <summary>~1 km play space: player west (faction A), enemy east (faction B).</summary>
        public static void PopulateInitialWorld(
            SkirmishWorldSim world,
            IIdFactory ids,
            FactionRoster playerFaction = null,
            FactionRoster enemyFaction = null)
        {
            playerFaction ??= FactionDefaultContent.IronCovenant;
            enemyFaction ??= FactionDefaultContent.VerdantCourt;

            var player = new PlayerId(0);
            var enemy = new PlayerId(1);

            world.SpawnBuilding(
                ids.Next(), player, playerFaction.Id, playerFaction.KeepBuildingId, -350f, 0f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), enemy, enemyFaction.Id, enemyFaction.KeepBuildingId, 350f, 0f, startActive: true);

            world.SpawnUnit(ids.Next(), player, playerFaction.Id, playerFaction.BasicUnitId, -320f, -20f);
            world.SpawnUnit(ids.Next(), player, playerFaction.Id, playerFaction.BasicUnitId, -320f, 0f);
            world.SpawnUnit(ids.Next(), player, playerFaction.Id, playerFaction.BasicUnitId, -320f, 20f);

            world.SpawnUnit(ids.Next(), enemy, enemyFaction.Id, enemyFaction.BasicUnitId, 320f, -15f);
            world.SpawnUnit(ids.Next(), enemy, enemyFaction.Id, enemyFaction.BasicUnitId, 320f, 15f);

            world.AddTerritory(ids.Next(), 0f, 0f, radius: 40f, goldPerSecond: 8);
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2000, -80f, 60f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1500, -100f, -70f);
        }
    }
}
