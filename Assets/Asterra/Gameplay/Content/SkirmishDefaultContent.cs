using Asterra.Core;
using Asterra.Gameplay;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay.Content
{
    public enum SkirmishMapId : byte
    {
        TwinKeeps = 0,
        RiverCrossing = 1,
        /// <summary>M1 vertical-slice map: fortress warfare through a mountain pass.</summary>
        BlackridgePass = 2,
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
            PopulateTwoPlayer(
                world,
                ids,
                new PlayerId(0),
                playerFaction,
                new PlayerId(1),
                enemyFaction,
                SkirmishMapId.TwinKeeps);
        }

        /// <summary>
        /// Deterministic layout from lobby seats (sorted by player id). All peers must call this
        /// with the same <paramref name="slots"/> to avoid lockstep desync.
        /// </summary>
        public static void PopulateFromSlots(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerSlotState[] slots,
            SkirmishMapId map = SkirmishMapId.TwinKeeps)
        {
            PopulateFromSlots(world, ids, slots, MapCatalog.BuiltinChoice(map).Id);
        }

        /// <summary>Load built-in or custom map by catalog id (e.g. twin_keeps, my_custom_map).</summary>
        public static void PopulateFromSlots(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerSlotState[] slots,
            string mapKey)
        {
            if (slots == null || slots.Length < 2)
                throw new System.ArgumentException("Need at least two player slots.", nameof(slots));

            var a = slots[0];
            var b = slots[1];
            var westFaction = FactionDefaultContent.Get(new FactionId(a.FactionIndex));
            var eastFaction = FactionDefaultContent.Get(new FactionId(b.FactionIndex));

            if (MapCatalog.TryLoad(mapKey, out var custom))
            {
                SkirmishMapLoader.Apply(
                    world, ids, a.Player, westFaction, b.Player, eastFaction, custom);
                return;
            }

            if (!MapCatalog.TryParseBuiltin(mapKey, out var builtin))
                builtin = SkirmishMapId.TwinKeeps;

            PopulateTwoPlayer(world, ids, a.Player, westFaction, b.Player, eastFaction, builtin);
        }

        private static void PopulateTwoPlayer(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerId westPlayer,
            FactionRoster westFaction,
            PlayerId eastPlayer,
            FactionRoster eastFaction,
            SkirmishMapId map)
        {
            SkirmishMapTerrain.Apply(world.Environment, map);
            SkirmishMapTraversal.Apply(world.Environment, map);

            switch (map)
            {
                case SkirmishMapId.TwinKeeps:
                    PopulateTwinKeeps(world, ids, westPlayer, westFaction, eastPlayer, eastFaction);
                    break;
                case SkirmishMapId.RiverCrossing:
                    PopulateRiverCrossing(world, ids, westPlayer, westFaction, eastPlayer, eastFaction);
                    break;
                case SkirmishMapId.BlackridgePass:
                    PopulateBlackridgePass(world, ids, westPlayer, westFaction, eastPlayer, eastFaction);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(map), map, null);
            }

            SkirmishMapDestructibles.Apply(world, ids, map);
            EnsureBuiltinArmies(world, ids, westPlayer, westFaction, eastPlayer, eastFaction);
        }

        private static void EnsureBuiltinArmies(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerId westPlayer,
            FactionRoster westFaction,
            PlayerId eastPlayer,
            FactionRoster eastFaction)
        {
            SkirmishMapLoader.EnsureMinimumStartingArmy(world, ids, westPlayer, westFaction);
            SkirmishMapLoader.EnsureMinimumStartingArmy(world, ids, eastPlayer, eastFaction);
        }

        private static void PopulateTwinKeeps(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerId westPlayer,
            FactionRoster westFaction,
            PlayerId eastPlayer,
            FactionRoster eastFaction)
        {
            world.SpawnBuilding(
                ids.Next(), westPlayer, westFaction.Id, westFaction.KeepBuildingId, -350f, 0f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), eastPlayer, eastFaction.Id, eastFaction.KeepBuildingId, 350f, 0f, startActive: true);

            // Workers only — train combat after the match starts.
            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BuilderUnitId, -300f, 0f);
            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BuilderUnitId, 300f, 0f);

            world.AddTerritory(ids.Next(), 0f, 0f, radius: 40f, goldPerSecond: 8);
        }

        private static void PopulateRiverCrossing(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerId westPlayer,
            FactionRoster westFaction,
            PlayerId eastPlayer,
            FactionRoster eastFaction)
        {
            // Diagonal keeps across the river band (z ≈ 0).
            world.SpawnBuilding(
                ids.Next(), westPlayer, westFaction.Id, westFaction.KeepBuildingId, -300f, -220f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), eastPlayer, eastFaction.Id, eastFaction.KeepBuildingId, 300f, 220f, startActive: true);

            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BuilderUnitId, -260f, -190f);
            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BuilderUnitId, 260f, 190f);

            world.AddTerritory(ids.Next(), 0f, 0f, radius: 40f, goldPerSecond: 8);

            world.SpawnUnit(
                ids.Next(), westPlayer, westFaction.Id, FactionDefaultContent.RiverBoatId, -390f, 0f);
            world.SpawnUnit(
                ids.Next(), eastPlayer, eastFaction.Id, FactionDefaultContent.RiverBoatId, 390f, 0f);
        }

        /// <summary>
        /// Blackridge Pass — east/west fortresses, narrow central choke, high-ground flanks.
        /// Notion M1 vertical-slice region.
        /// </summary>
        private static void PopulateBlackridgePass(
            SkirmishWorldSim world,
            IIdFactory ids,
            PlayerId westPlayer,
            FactionRoster westFaction,
            PlayerId eastPlayer,
            FactionRoster eastFaction)
        {
            // Fortresses outside the pass mouth
            world.SpawnBuilding(
                ids.Next(), westPlayer, westFaction.Id, westFaction.KeepBuildingId, -360f, 0f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), eastPlayer, eastFaction.Id, eastFaction.KeepBuildingId, 360f, 0f, startActive: true);

            // Workers only — train combat after the match starts.
            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BuilderUnitId, -300f, 8f);
            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BuilderUnitId, 300f, -8f);

            // West pass mouth fortifications
            world.SpawnBuilding(
                ids.Next(), westPlayer, westFaction.Id, westFaction.TowerBuildingId, -140f, -55f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), westPlayer, westFaction.Id, westFaction.TowerBuildingId, -140f, 55f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), westPlayer, westFaction.Id, westFaction.WallBuildingId, -120f, -70f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), westPlayer, westFaction.Id, westFaction.WallBuildingId, -120f, 70f, startActive: true);

            // East pass mouth fortifications
            world.SpawnBuilding(
                ids.Next(), eastPlayer, eastFaction.Id, eastFaction.TowerBuildingId, 140f, -55f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), eastPlayer, eastFaction.Id, eastFaction.TowerBuildingId, 140f, 55f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), eastPlayer, eastFaction.Id, eastFaction.WallBuildingId, 120f, -70f, startActive: true);
            world.SpawnBuilding(
                ids.Next(), eastPlayer, eastFaction.Id, eastFaction.WallBuildingId, 120f, 70f, startActive: true);

            // Contested pass control + high-ground supply flanks
            world.AddTerritory(ids.Next(), 0f, 0f, radius: 36f, goldPerSecond: 10);
            world.AddTerritory(ids.Next(), 0f, 110f, radius: 28f, goldPerSecond: 6);
            world.AddTerritory(ids.Next(), 0f, -110f, radius: 28f, goldPerSecond: 6);
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
                builtin = SkirmishMapId.TwinKeeps;

            SkirmishMapTerrain.Apply(world.Environment, builtin);
            SkirmishMapTraversal.Apply(world.Environment, builtin);
        }

        public static string GetMapDisplayName(SkirmishMapId map)
        {
            switch (map)
            {
                case SkirmishMapId.TwinKeeps:
                    return "Twin Keeps";
                case SkirmishMapId.RiverCrossing:
                    return "River Crossing";
                case SkirmishMapId.BlackridgePass:
                    return "Blackridge Pass";
                default:
                    return map.ToString();
            }
        }

        public static SkirmishMapId NextMap(SkirmishMapId map)
        {
            switch (map)
            {
                case SkirmishMapId.TwinKeeps:
                    return SkirmishMapId.RiverCrossing;
                case SkirmishMapId.RiverCrossing:
                    return SkirmishMapId.BlackridgePass;
                case SkirmishMapId.BlackridgePass:
                    return SkirmishMapId.TwinKeeps;
                default:
                    return SkirmishMapId.TwinKeeps;
            }
        }
    }
}
