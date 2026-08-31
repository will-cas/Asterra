using Asterra.Core;
using Asterra.Gameplay;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay.Content
{
    public enum SkirmishMapId : byte
    {
        MundorCapital = 0,
        OutcastCamp = 1,
        RiverCrossing = 2,
        FrozenWastes = 3,
        LushForest = 4,
        TwinCities = 5,
        AncientRelic = 6,
    }

    /// <summary>
    /// Phase-1 skirmish layout helpers. Unit/building defs live in <see cref="FactionDefaultContent"/>.
    /// </summary>
    public static class SkirmishDefaultContent
    {
        public const byte PlayerFaction = 0;
        public const byte EnemyFaction = 1;

        public static DefinitionRegistry CreateRegistry()
        {
            var registry = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(registry);
            return registry;
        }

        public static void PopulateInitialWorld(
            SkirmishWorldSim world,
            IIdFactory ids,
            FactionRoster playerFaction = null,
            FactionRoster enemyFaction = null)
        {
            playerFaction ??= FactionDefaultContent.VeiledInheritance;
            enemyFaction ??= FactionDefaultContent.MundorCrown;
            SkirmishMapLoader.Apply(
                world,
                ids,
                new PlayerId(0),
                playerFaction,
                new PlayerId(1),
                enemyFaction,
                BuiltinMaps.Definition(SkirmishMapId.LushForest));
        }

        /// <summary>
        /// Deterministic layout from lobby seats (sorted by player id). All peers must call this
        /// with the same <paramref name="slots"/> to avoid lockstep desync.
        /// </summary>
        public static void PopulateFromSlots(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerSlotState[] slots,
            SkirmishMapId map = SkirmishMapId.LushForest)
        {
            PopulateFromSlots(world, ids, slots, MapCatalog.BuiltinChoice(map).Id);
        }

        /// <summary>Load built-in or custom map by catalog id (e.g. lush_forest, my_custom_map).</summary>
        public static void PopulateFromSlots(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerSlotState[] slots,
            string mapKey)
        {
            if (slots == null || slots.Length < 2)
                throw new System.ArgumentException("Need at least two player slots.", nameof(slots));

            if (MapCatalog.TryLoad(mapKey, out var custom))
            {
                SkirmishMapLoader.Apply(world, ids, slots, custom);
                return;
            }

            if (!MapCatalog.TryParseBuiltin(mapKey, out var builtin))
                builtin = SkirmishMapId.LushForest;

            SkirmishMapLoader.Apply(world, ids, slots, BuiltinMaps.Definition(builtin));
        }

        public static void ApplyMapEnvironmentOnly(SkirmishWorldSim world, string mapKey)
        {
            if (world?.Environment == null)
                return;

            if (MapCatalog.TryLoad(mapKey, out var custom))
            {
                SkirmishMapLoader.ApplyTerrain(world.Environment, custom);
                SkirmishMapLoader.ApplyTraversal(world.Environment, custom);
                return;
            }

            if (!MapCatalog.TryParseBuiltin(mapKey, out var builtin))
                builtin = SkirmishMapId.LushForest;

            var def = BuiltinMaps.Definition(builtin);
            SkirmishMapLoader.ApplyTerrain(world.Environment, def);
            SkirmishMapLoader.ApplyTraversal(world.Environment, def);
        }

        public static string GetMapDisplayName(SkirmishMapId map)
        {
            return BuiltinMaps.Definition(map).displayName;
        }

        public static SkirmishMapId NextMap(SkirmishMapId map)
        {
            int n = 7;
            return (SkirmishMapId)(((int)map + 1) % n);
        }
    }
}
