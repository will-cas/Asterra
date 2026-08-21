namespace Asterra.Core.World
{
    /// <summary>
    /// Designer-facing terrain properties consumed by sim queries / pathfinding.
    /// Matches the UnitDefData pattern: plain C# now; ScriptableObject wrappers can copy into these later.
    /// </summary>
    public sealed class TerrainDefData
    {
        public string Id = "terrain_grass_short";
        public string DisplayName = "Short Grass";
        public TerrainCategory Category = TerrainCategory.GrassShort;

        /// <summary>Multiplies unit MoveSpeed while on this cell (1 = normal).</summary>
        public float MovementSpeedModifier = 1f;

        /// <summary>Additive A*/flow-field cost. Higher = avoid. Use float.PositiveInfinity via PathCostInfinite for blocked.</summary>
        public float PathfindingCost = 1f;

        /// <summary>Capabilities required to enter. Flying may bypass via path rules.</summary>
        public TraversalCapability RequiredCapabilities = TraversalCapability.Land;

        /// <summary>Multiplies vision / detection radius (1 = normal, &lt;1 reduced).</summary>
        public float VisibilityModifier = 1f;

        /// <summary>Multiplies footstep / movement noise (1 = normal, &lt;1 quieter).</summary>
        public float SoundNoiseModifier = 1f;

        /// <summary>Multiplies outgoing or incoming combat effectiveness while standing here (design-specific).</summary>
        public float CombatModifier = 1f;

        public bool AllowsBuilding = true;
        public bool AllowsResourceGathering = true;
        public float ResourceGatherModifier = 1f;
        public bool IsDestructible;
        public bool CanChangeAtRuntime = true;

        /// <summary>Drainage rate for waterlogging / mud recovery (higher drains faster).</summary>
        public float DrainageRate = 1f;

        /// <summary>How quickly this cell waterlogs under rain.</summary>
        public float WaterlogSensitivity = 1f;

        /// <summary>Cover bonus for units occupying this cell (0 = none).</summary>
        public float CoverBonus;

        /// <summary>Line-of-sight blockage factor (0 = clear, 1 = fully blocks).</summary>
        public float LosBlockFactor;

        public const float PathCostBlocked = 1e9f;

        public bool IsTraversableBy(TraversalCapability unitCapabilities)
        {
            if (PathfindingCost >= PathCostBlocked)
                return false;
            // Flying bypasses ground capability gates; blockers still use PathCostBlocked / no-entry.
            if ((unitCapabilities & TraversalCapability.Flying) != 0)
                return true;
            if (RequiredCapabilities == TraversalCapability.None)
                return false;
            // Unit must possess every required capability bit (Amphibious = Land|Water satisfies Land or Water).
            return (unitCapabilities & RequiredCapabilities) == RequiredCapabilities;
        }

        public static TerrainDefData CreateDefaultGrassShort()
        {
            return new TerrainDefData
            {
                Id = "terrain_grass_short",
                DisplayName = "Short Grass",
                Category = TerrainCategory.GrassShort,
            };
        }

        public static TerrainDefData CreateNoEntry(string id = "terrain_no_entry")
        {
            return new TerrainDefData
            {
                Id = id,
                DisplayName = "No Entry",
                Category = TerrainCategory.NoEntry,
                MovementSpeedModifier = 0f,
                PathfindingCost = PathCostBlocked,
                RequiredCapabilities = TraversalCapability.None,
                AllowsBuilding = false,
                AllowsResourceGathering = false,
                CanChangeAtRuntime = false,
            };
        }
    }

    public sealed class TraversalLinkDefData
    {
        public string Id = "traversal_bridge";
        public TraversalLinkType Type = TraversalLinkType.Bridge;
        public TraversalCapability AllowedCapabilities = TraversalCapability.Land;
        public float DurationSeconds = 1f;
        public bool RequiresAnimation;
        public bool AllowsCombatDuringTraversal;
        public bool IsDestructible = true;
        public bool CanBeBlocked = true;
    }

    public sealed class WeatherDefData
    {
        public string Id = "weather_clear";
        public string DisplayName = "Clear";
        public WeatherKind Kind = WeatherKind.Clear;
        public float DefaultIntensity = 0.5f;
        public float MinDurationSeconds = 30f;
        public float MaxDurationSeconds = 120f;
        public float TransitionSeconds = 8f;
        public float VisibilityModifier = 1f;
        public float MovementModifier = 1f;
        public float SoundModifier = 1f;
        public float TemperatureDelta;
        /// <summary>Contribution to rain accumulation / waterlogging when Kind is Rain/Storm.</summary>
        public float PrecipitationRate;
        /// <summary>Contribution to snow depth when Kind is Snow.</summary>
        public float SnowfallRate;
        public bool EnablesLightning;
        public float LightningChancePerSecond;
    }

    public sealed class MovementCapabilityDefData
    {
        public string Id = "move_land";
        public TraversalCapability Capabilities = TraversalCapability.Land;
        /// <summary>Optional speed multipliers keyed later via terrain category overrides (empty = use terrain def).</summary>
        public float DefaultSpeedMultiplier = 1f;
    }

    /// <summary>Designer data for trees, bridges, rocks, and other world props.</summary>
    public sealed class DestructibleDefData
    {
        public string Id = "destructible_tree";
        public string DisplayName = "Tree";
        public float MaxHealth = 80f;
        public float Armor;
        public DamageType Resistances;
        public float ResistanceFactor = 0.5f; // damage multiplier when resisted
        public bool BlocksMovement = true;
        public bool BlocksLos;
        public ushort ReplaceTerrainDefIndex = DefaultTerrainCatalog.GrassShort;
        public bool ClearsTerrainOnDestroy = true;
        public bool DisableTraversalOnDestroy = true;
        public Asterra.Core.ResourceType? ResourceDropType;
        public int ResourceDropAmount;
        public float FootprintRadius = 4f;
        public bool ProvidesCover;
    }
}
