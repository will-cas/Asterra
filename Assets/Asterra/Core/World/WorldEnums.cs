using System;

namespace Asterra.Core.World
{
    /// <summary>
    /// Logical terrain categories. Concrete gameplay properties live on <see cref="TerrainDefData"/>;
    /// designers add new defs without extending this enum when possible (use Id strings).
    /// Categories exist for fast filtering / debug visualisation.
    /// </summary>
    public enum TerrainCategory : byte
    {
        GrassBare = 0,
        GrassShort = 1,
        GrassLong = 2,
        Rock = 3,
        Swamp = 4,
        Forest = 5,
        Tree = 6,
        Beach = 7,
        Mountain = 8,
        Hill = 9,
        WaterRiver = 10,
        WaterLake = 11,
        WaterOcean = 12,
        WaterWaterfall = 13,
        Ice = 14,
        Trench = 15,
        Gap = 16,
        NoEntry = 17,
        Custom = 255,
    }

    [Flags]
    public enum DamageType : byte
    {
        None = 0,
        Slash = 1 << 0,
        Pierce = 1 << 1,
        Blunt = 1 << 2,
        Siege = 1 << 3,
        Fire = 1 << 4,
        Magic = 1 << 5,
        All = 0xFF,
    }

    /// <summary>
    /// Unit movement capabilities. Prefer flags over per-unit-type branching.
    /// Amphibious is Land|Water; Flying ignores most ground blockers via path rules.
    /// </summary>
    [Flags]
    public enum TraversalCapability : ushort
    {
        None = 0,
        Land = 1 << 0,
        Water = 1 << 1,
        Mountain = 1 << 2,
        Flying = 1 << 3,
        Magic = 1 << 4,
        Jump = 1 << 5,
        Amphibious = Land | Water,
    }

    public enum TraversalLinkType : byte
    {
        Bridge = 0,
        MagicCrossing = 1,
        JumpOver = 2,
        JumpDown = 3,
        JumpUp = 4,
        TreeGap = 5,
        ShoreTransition = 6,
        Custom = 255,
    }

    public enum WeatherKind : byte
    {
        Clear = 0,
        Sunny = 1,
        Cloudy = 2,
        Rain = 3,
        Snow = 4,
        Fog = 5,
        Storm = 6,
        Custom = 255,
    }

    public enum TimeOfDayPhase : byte
    {
        Dawn = 0,
        Morning = 1,
        Afternoon = 2,
        Evening = 3,
        Dusk = 4,
        Night = 5,
    }

    public enum IceState : byte
    {
        None = 0,
        Thin = 1,
        Thick = 2,
        Broken = 3,
        FrozenWater = 4,
    }

    public enum DestructibleState : byte
    {
        Intact = 0,
        Damaged = 1,
        Destroyed = 2,
    }
}
