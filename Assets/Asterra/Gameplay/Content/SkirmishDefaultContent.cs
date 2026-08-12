using Asterra.Core;
using Asterra.Gameplay;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay.Content
{
    public enum SkirmishMapId : byte
    {
        TwinKeeps = 0,
        RiverCrossing = 1,
    }

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

        public static void PopulateInitialWorld(
            SkirmishWorldSim world,
            IIdFactory ids,
            FactionRoster playerFaction = null,
            FactionRoster enemyFaction = null)
        {
            playerFaction ??= FactionDefaultContent.IronCovenant;
            enemyFaction ??= FactionDefaultContent.VerdantCourt;
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
            if (slots == null || slots.Length < 2)
                throw new System.ArgumentException("Need at least two player slots.", nameof(slots));

            var a = slots[0];
            var b = slots[1];
            PopulateTwoPlayer(
                world,
                ids,
                a.Player,
                FactionDefaultContent.Get(new FactionId(a.FactionIndex)),
                b.Player,
                FactionDefaultContent.Get(new FactionId(b.FactionIndex)),
                map);
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
            switch (map)
            {
                case SkirmishMapId.TwinKeeps:
                    PopulateTwinKeeps(world, ids, westPlayer, westFaction, eastPlayer, eastFaction);
                    break;
                case SkirmishMapId.RiverCrossing:
                    PopulateRiverCrossing(world, ids, westPlayer, westFaction, eastPlayer, eastFaction);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(map), map, null);
            }
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

            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BasicUnitId, -320f, -20f);
            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BasicUnitId, -320f, 0f);
            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BasicUnitId, -320f, 20f);
            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BuilderUnitId, -300f, 0f);

            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BasicUnitId, 320f, -15f);
            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BasicUnitId, 320f, 15f);
            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BuilderUnitId, 300f, 0f);

            world.AddTerritory(ids.Next(), 0f, 0f, radius: 40f, goldPerSecond: 8);

            // Center-ish nodes (shared contest)
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2000, -80f, 60f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1500, -100f, -70f);

            // West base (player seat)
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2500, -280f, 70f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 2000, -290f, -80f);
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 1800, -240f, -40f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1600, -250f, 50f);

            // East base (enemy seat)
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2500, 280f, -70f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 2000, 290f, 80f);
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 1800, 240f, 40f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1600, 250f, -50f);
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

            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BasicUnitId, -270f, -200f);
            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BasicUnitId, -280f, -230f);
            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BasicUnitId, -250f, -210f);
            world.SpawnUnit(ids.Next(), westPlayer, westFaction.Id, westFaction.BuilderUnitId, -260f, -190f);

            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BasicUnitId, 270f, 200f);
            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BasicUnitId, 280f, 230f);
            world.SpawnUnit(ids.Next(), eastPlayer, eastFaction.Id, eastFaction.BuilderUnitId, 260f, 190f);

            world.AddTerritory(ids.Next(), 0f, 0f, radius: 40f, goldPerSecond: 8);

            // River band near z = 0
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2200, -120f, 8f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1800, -40f, -12f);
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2000, 50f, 10f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1700, 130f, -8f);

            // Near SW base
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2400, -250f, -160f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1900, -220f, -180f);
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 1700, -180f, -140f);

            // Near NE base
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 2400, 250f, 160f);
            world.AddResourceNode(ids.Next(), ResourceType.Timber, 1900, 220f, 180f);
            world.AddResourceNode(ids.Next(), ResourceType.Gold, 1700, 180f, 140f);
        }
    }
}
