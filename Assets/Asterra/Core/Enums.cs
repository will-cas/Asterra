namespace Asterra.Core
{
    public enum ResourceType : byte
    {
        Gold = 0,
        Timber = 1,
        Mana = 2,
    }

    public enum UnitStance : byte
    {
        Aggressive = 0,
        Defensive = 1,
        Hold = 2,
        Passive = 3,
    }

    public enum BuildingState : byte
    {
        Ghost = 0,
        Constructing = 1,
        Active = 2,
        Disabled = 3,
        Destroyed = 4,
    }

    public enum TerritoryState : byte
    {
        Neutral = 0,
        Contested = 1,
        Controlled = 2,
    }
}
