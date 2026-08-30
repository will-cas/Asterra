using Asterra.Core.World;

namespace Asterra.Core.World
{
    /// <summary>
    /// Built-in terrain defs for skirmish. Designers can register additional defs later via SO → DefData.
    /// Indices are stable for save/replay; append-only when extending.
    /// </summary>
    public static class DefaultTerrainCatalog
    {
        public const ushort GrassBare = 0;
        public const ushort GrassShort = 1;
        public const ushort GrassLong = 2;
        public const ushort Rock = 3;
        public const ushort Swamp = 4;
        public const ushort Forest = 5;
        public const ushort Tree = 6;
        public const ushort Beach = 7;
        public const ushort Mountain = 8;
        public const ushort Hill = 9;
        public const ushort WaterRiver = 10;
        public const ushort WaterLake = 11;
        public const ushort WaterOcean = 12;
        public const ushort WaterWaterfall = 13;
        public const ushort IceThick = 14;
        public const ushort IceThin = 15;
        public const ushort Trench = 16;
        public const ushort NoEntry = 17;
        /// <summary>Fordable shallows — land units can cross slowly.</summary>
        public const ushort WaterShallow = 18;
        /// <summary>Deep channel — boats only; land blocked.</summary>
        public const ushort WaterDeep = 19;
        /// <summary>Fast current — boats only, higher move speed.</summary>
        public const ushort WaterFast = 20;
        /// <summary>Raised earth berm — cover + LOS block.</summary>
        public const ushort Berm = 21;
        /// <summary>Spike / caltrop pit — damages units that stand in it.</summary>
        public const ushort SpikePit = 22;
        /// <summary>Combat debris — blocks movement until cleared.</summary>
        public const ushort Debris = 23;
        /// <summary>Siege crater — trench-like depression.</summary>
        public const ushort Crater = 24;
        /// <summary>Burned brush / scorched earth.</summary>
        public const ushort Scorched = 25;
        /// <summary>Map gap / chasm — bridge span target.</summary>
        public const ushort Gap = 26;
        /// <summary>Packed road — fast, no cover.</summary>
        public const ushort Road = 27;
        /// <summary>Soft mud — slow, little cover.</summary>
        public const ushort Mud = 28;
        /// <summary>Broken stone / rubble — slow, heavy cover.</summary>
        public const ushort Rubble = 29;
        /// <summary>Snow field — slow, exposed.</summary>
        public const ushort Snow = 30;

        public static TerrainDefData[] CreateDefs()
        {
            return new[]
            {
                Def("terrain_grass_bare", "Bare Grass", TerrainCategory.GrassBare, 1.08f, 0.95f, TraversalCapability.Land, visibility: 1.05f, drainage: 1.2f, waterlog: 0.7f),
                Def("terrain_grass_short", "Short Grass", TerrainCategory.GrassShort, 1f, 1f, TraversalCapability.Land),
                Def("terrain_grass_long", "Long Grass", TerrainCategory.GrassLong, 0.84f, 1.25f, TraversalCapability.Land, visibility: 0.8f, sound: 0.9f, cover: 0.22f, los: 0.2f, waterlog: 1.1f),
                Def("terrain_rock", "Rocks", TerrainCategory.Rock, 0.7f, 2.1f, TraversalCapability.Land, allowsBuild: false, gatherMod: 0.5f, destructible: true, cover: 0.28f, combat: 0.95f),
                Def("terrain_swamp", "Swamp", TerrainCategory.Swamp, 0.45f, 3.5f, TraversalCapability.Land, visibility: 0.9f, combat: 0.9f, allowsBuild: false, drainage: 0.35f, waterlog: 1.8f, cover: 0.08f),
                Def("terrain_forest", "Forest", TerrainCategory.Forest, 0.82f, 1.5f, TraversalCapability.Land, visibility: 0.65f, sound: 0.85f, cover: 0.38f, los: 0.55f),
                Def("terrain_tree", "Tree Stand", TerrainCategory.Tree, 0f, TerrainDefData.PathCostBlocked, TraversalCapability.Land, visibility: 0.6f, allowsBuild: false, gatherMod: 1.2f, destructible: true, los: 0.8f, cover: 0.3f),
                Def("terrain_beach", "Beach", TerrainCategory.Beach, 0.88f, 1.2f, TraversalCapability.Land, drainage: 1.4f, waterlog: 0.5f, combat: 0.96f),
                Def("terrain_mountain", "Mountain", TerrainCategory.Mountain, 0.55f, 5f, TraversalCapability.Mountain, allowsBuild: false, allowsGather: false, cover: 0.12f),
                Def("terrain_hill", "Hills", TerrainCategory.Hill, 0.86f, 1.3f, TraversalCapability.Land, combat: 1.12f, cover: 0.1f),
                Def("terrain_water_river", "River", TerrainCategory.WaterRiver, 1f, 1.2f, TraversalCapability.Water, allowsBuild: false, allowsGather: false),
                Def("terrain_water_lake", "Lake", TerrainCategory.WaterLake, 1f, 1f, TraversalCapability.Water, allowsBuild: false, allowsGather: false),
                Def("terrain_water_ocean", "Ocean", TerrainCategory.WaterOcean, 1f, 1f, TraversalCapability.Water, allowsBuild: false, allowsGather: false),
                Def("terrain_waterfall", "Waterfall", TerrainCategory.WaterWaterfall, 0f, TerrainDefData.PathCostBlocked, TraversalCapability.None, allowsBuild: false, allowsGather: false, change: false),
                Def("terrain_ice_thick", "Thick Ice", TerrainCategory.Ice, 0.95f, 1.1f, TraversalCapability.Land, sound: 1.15f, destructible: true, drainage: 0.2f),
                Def("terrain_ice_thin", "Thin Ice", TerrainCategory.Ice, 0.8f, 1.35f, TraversalCapability.Land, sound: 1.25f, combat: 0.9f, destructible: true, drainage: 0.15f),
                Def("terrain_trench", "Trench", TerrainCategory.Trench, 0.68f, 1.35f, TraversalCapability.Land, visibility: 0.75f, cover: 0.52f, los: 0.35f, combat: 1.08f, allowsBuild: false),
                TerrainDefData.CreateNoEntry("terrain_no_entry"),
                Def("terrain_water_shallow", "Shallow River", TerrainCategory.WaterRiver, 0.48f, 2.2f, TraversalCapability.Land, allowsBuild: false, allowsGather: false, drainage: 0.6f, waterlog: 1.6f),
                Def("terrain_water_deep", "Deep River", TerrainCategory.WaterRiver, 0.9f, 1.4f, TraversalCapability.Water, allowsBuild: false, allowsGather: false),
                Def("terrain_water_fast", "Fast River", TerrainCategory.WaterRiver, 1.35f, 1.1f, TraversalCapability.Water, allowsBuild: false, allowsGather: false),
                Def("terrain_berm", "Earth Berm", TerrainCategory.Hill, 0.78f, 1.55f, TraversalCapability.Land, visibility: 0.85f, cover: 0.5f, los: 0.55f, combat: 1.08f, allowsBuild: false),
                Def("terrain_spike_pit", "Spike Pit", TerrainCategory.Trench, 0.55f, 2.2f, TraversalCapability.Land, visibility: 0.8f, cover: 0.1f, combat: 0.85f, allowsBuild: false),
                Def("terrain_debris", "Debris", TerrainCategory.Rock, 0f, TerrainDefData.PathCostBlocked, TraversalCapability.Land, visibility: 0.9f, allowsBuild: false, allowsGather: false, los: 0.25f),
                Def("terrain_crater", "Crater", TerrainCategory.Trench, 0.65f, 1.6f, TraversalCapability.Land, visibility: 0.8f, cover: 0.3f, los: 0.2f, combat: 1.05f, allowsBuild: false),
                Def("terrain_scorched", "Scorched Earth", TerrainCategory.GrassBare, 1.02f, 1.05f, TraversalCapability.Land, visibility: 1.1f, cover: 0f, los: 0f, drainage: 1.3f),
                Def("terrain_gap", "Gap", TerrainCategory.Gap, 0f, TerrainDefData.PathCostBlocked, TraversalCapability.None, allowsBuild: false, allowsGather: false, change: false),
                Def("terrain_road", "Road", TerrainCategory.GrassBare, 1.28f, 0.65f, TraversalCapability.Land, visibility: 1.08f, combat: 0.94f, drainage: 1.3f, waterlog: 0.6f),
                Def("terrain_mud", "Mud", TerrainCategory.GrassBare, 0.58f, 2.1f, TraversalCapability.Land, visibility: 0.95f, combat: 0.92f, drainage: 0.45f, waterlog: 1.7f, cover: 0.06f),
                Def("terrain_rubble", "Rubble", TerrainCategory.Rock, 0.66f, 2.3f, TraversalCapability.Land, allowsBuild: false, combat: 0.97f, cover: 0.36f, los: 0.18f),
                Def("terrain_snow", "Snow", TerrainCategory.Ice, 0.7f, 1.7f, TraversalCapability.Land, sound: 1.1f, combat: 0.93f, drainage: 0.25f),
            };
        }

        /// <summary>Playable grid filled with short grass; outside cells are treated as blocked by the grid.</summary>
        public static WorldTerrainGrid CreatePlayableGrid(float playableHalfExtent = 450f, float cellSize = 10f)
        {
            var defs = CreateDefs();
            int cells = (int)System.Math.Ceiling((playableHalfExtent * 2f) / cellSize);
            if (cells < 1)
                cells = 1;
            float origin = -playableHalfExtent;
            return new WorldTerrainGrid(cells, cells, cellSize, origin, origin, defs, defaultDefIndex: GrassShort);
        }

        private static TerrainDefData Def(
            string id,
            string name,
            TerrainCategory category,
            float move,
            float pathCost,
            TraversalCapability required,
            float visibility = 1f,
            float sound = 1f,
            float combat = 1f,
            bool allowsBuild = true,
            bool allowsGather = true,
            float gatherMod = 1f,
            bool destructible = false,
            bool change = true,
            float drainage = 1f,
            float waterlog = 1f,
            float cover = 0f,
            float los = 0f)
        {
            return new TerrainDefData
            {
                Id = id,
                DisplayName = name,
                Category = category,
                MovementSpeedModifier = move,
                PathfindingCost = pathCost,
                RequiredCapabilities = required,
                VisibilityModifier = visibility,
                SoundNoiseModifier = sound,
                CombatModifier = combat,
                AllowsBuilding = allowsBuild,
                AllowsResourceGathering = allowsGather,
                ResourceGatherModifier = gatherMod,
                IsDestructible = destructible,
                CanChangeAtRuntime = change,
                DrainageRate = drainage,
                WaterlogSensitivity = waterlog,
                CoverBonus = cover,
                LosBlockFactor = los,
            };
        }
    }
}
