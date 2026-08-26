using System.Collections.Generic;

namespace Asterra.Core.World
{
    /// <summary>
    /// Read-only terrain query surface for movement, placement, vision, and combat modifiers.
    /// Implementations must be deterministic for lockstep (no UnityEngine.Random / wall-clock).
    /// </summary>
    public interface ITerrainMap
    {
        float CellSize { get; }
        int Width { get; }
        int Height { get; }
        float OriginX { get; }
        float OriginZ { get; }

        bool TryGetCell(float worldX, float worldZ, out TerrainCell cell);
        bool TryGetDef(string terrainDefId, out TerrainDefData def);
        float GetMovementModifier(float worldX, float worldZ, TraversalCapability capabilities);
        float GetPathCost(float worldX, float worldZ, TraversalCapability capabilities);
        bool IsTraversable(float worldX, float worldZ, TraversalCapability capabilities);
        bool AllowsBuilding(float worldX, float worldZ);
    }

    /// <summary>Packed per-cell runtime state. Keep blittable-friendly for future Burst/DOTS.</summary>
    public struct TerrainCell
    {
        public ushort TerrainDefIndex;
        public IceState Ice;
        public byte Waterlog01; // 0..255 ≈ 0..1
        public byte SnowDepth01;
        public byte Flags; // reserved: burned, muddy, trench occupied, etc.

        public const byte FlagMuddy = 1 << 0;
        public const byte FlagOnFire = 1 << 1;
        public const byte FlagTrench = 1 << 2;
        public const byte FlagSpikes = 1 << 3;
        public const byte FlagBurningRuin = 1 << 4;
    }

    public interface ITraversalGraph
    {
        IReadOnlyList<TraversalLink> Links { get; }
        bool TryGetLinksFrom(int cellX, int cellZ, List<TraversalLink> results);
        void SetLinkEnabled(int linkId, bool enabled);
        bool TryGetLink(int linkId, out TraversalLink link);
        /// <summary>
        /// Pick a useful link for a unit moving toward a destination (bidirectional).
        /// Returns false if none apply.
        /// </summary>
        bool TryFindLinkForMove(
            float unitX,
            float unitZ,
            float destX,
            float destZ,
            TraversalCapability capabilities,
            float approachRadius,
            out TraversalLink link,
            out bool forward);
    }

    public readonly struct TraversalLink
    {
        public readonly int Id;
        public readonly float StartX;
        public readonly float StartZ;
        public readonly float EndX;
        public readonly float EndZ;
        public readonly TraversalLinkType Type;
        public readonly TraversalCapability AllowedCapabilities;
        public readonly float DurationSeconds;
        public readonly bool AllowsCombat;
        public readonly bool Enabled;
        public readonly bool IsDestructible;
        public readonly bool CanBeBlocked;
        public readonly bool RequiresAnimation;
        public readonly float ApproachRadius;

        public TraversalLink(
            int id,
            float startX,
            float startZ,
            float endX,
            float endZ,
            TraversalLinkType type,
            TraversalCapability allowedCapabilities,
            float durationSeconds,
            bool allowsCombat,
            bool enabled,
            bool isDestructible = true,
            bool canBeBlocked = true,
            bool requiresAnimation = false,
            float approachRadius = 8f)
        {
            Id = id;
            StartX = startX;
            StartZ = startZ;
            EndX = endX;
            EndZ = endZ;
            Type = type;
            AllowedCapabilities = allowedCapabilities;
            DurationSeconds = durationSeconds;
            AllowsCombat = allowsCombat;
            Enabled = enabled;
            IsDestructible = isDestructible;
            CanBeBlocked = canBeBlocked;
            RequiresAnimation = requiresAnimation;
            ApproachRadius = approachRadius > 0f ? approachRadius : 8f;
        }

        public bool Allows(TraversalCapability unitCapabilities)
        {
            if (!Enabled)
                return false;
            if ((unitCapabilities & TraversalCapability.Flying) != 0)
                return true;
            if (AllowedCapabilities == TraversalCapability.None)
                return false;
            return (unitCapabilities & AllowedCapabilities) == AllowedCapabilities;
        }
    }

    public interface INoEntryMap
    {
        bool IsBlocked(float worldX, float worldZ);
        /// <summary>Mark/unmark a rectangular region. Used for map bounds, hazards, magical zones.</summary>
        void SetBlockedRect(float minX, float minZ, float maxX, float maxZ, bool blocked);
    }

    public readonly struct WeatherState
    {
        public readonly WeatherKind Kind;
        public readonly string DefId;
        public readonly float Intensity;
        public readonly float DurationSeconds;
        public readonly float TransitionSeconds;
        public readonly float RemainingSeconds;
        public readonly float VisibilityModifier;
        public readonly float MovementModifier;
        public readonly float SoundModifier;

        public WeatherState(
            WeatherKind kind,
            string defId,
            float intensity,
            float durationSeconds,
            float transitionSeconds,
            float remainingSeconds,
            float visibilityModifier,
            float movementModifier,
            float soundModifier)
        {
            Kind = kind;
            DefId = defId;
            Intensity = intensity;
            DurationSeconds = durationSeconds;
            TransitionSeconds = transitionSeconds;
            RemainingSeconds = remainingSeconds;
            VisibilityModifier = visibilityModifier;
            MovementModifier = movementModifier;
            SoundModifier = soundModifier;
        }
    }

    public interface IWeatherSystem
    {
        WeatherState Current { get; }
        WeatherState? TransitionTarget { get; }
        float WindDirX { get; }
        float WindDirZ { get; }
        float WindIntensity { get; }
        void Tick(float deltaSeconds);
    }

    public interface ITimeOfDaySystem
    {
        float DayLengthSeconds { get; }
        /// <summary>Normalized time in [0,1). 0 = start of dawn.</summary>
        float Time01 { get; }
        TimeOfDayPhase Phase { get; }
        bool IsDay { get; }
        bool IsNight { get; }
        float SunIntensity { get; }
        float AmbientIntensity { get; }
        float ShadowStrength { get; }
        float VisibilityModifier { get; }
        float TemperatureBias { get; }
        float SunDirX { get; }
        float SunDirY { get; }
        float SunDirZ { get; }
        void Tick(float deltaSeconds);
    }

    public enum TimeOfDayEventKind : byte
    {
        PhaseChanged = 1,
        Dawn = 2,
        Day = 3,
        Dusk = 4,
        Night = 5,
    }

    public readonly struct TimeOfDayEvent
    {
        public readonly TimeOfDayEventKind Kind;
        public readonly TimeOfDayPhase Phase;
        public readonly float Time01;

        public TimeOfDayEvent(TimeOfDayEventKind kind, TimeOfDayPhase phase, float time01)
        {
            Kind = kind;
            Phase = phase;
            Time01 = time01;
        }
    }

    /// <summary>
    /// Aggregate environment tick surface. Wired by MatchBootstrap / SkirmishWorldSim —
    /// not a MonoBehaviour singleton. Presentation listens via events on the Gameplay implementation.
    /// </summary>
    public interface IWorldEnvironment
    {
        ITerrainMap Terrain { get; }
        ITraversalGraph Traversal { get; }
        INoEntryMap NoEntry { get; }
        IWeatherSystem Weather { get; }
        ITimeOfDaySystem TimeOfDay { get; }
        void Tick(float deltaSeconds);
    }
}
