namespace Asterra.Core
{
    public interface IUnit
    {
        SimEntityId Id { get; }
        PlayerId Owner { get; }
        FactionId Faction { get; }
        string DefinitionId { get; }
        float Health { get; }
        float MaxHealth { get; }
        UnitStance Stance { get; }
        bool IsAlive { get; }
    }

    public interface IBuilding
    {
        SimEntityId Id { get; }
        PlayerId Owner { get; }
        FactionId Faction { get; }
        string DefinitionId { get; }
        BuildingState State { get; }
        float Health { get; }
        float MaxHealth { get; }
        bool CanProduce { get; }
    }

    public interface IResourceNode
    {
        SimEntityId Id { get; }
        ResourceType Type { get; }
        int Remaining { get; }
        bool IsDepleted { get; }
    }

    public interface IFaction
    {
        FactionId Id { get; }
        string DisplayName { get; }
        string DefinitionId { get; }
    }

    public interface ICommander
    {
        SimEntityId Id { get; }
        PlayerId Owner { get; }
        FactionId Faction { get; }
        string DefinitionId { get; }
        int Level { get; }
    }

    public interface ITerritoryNode
    {
        SimEntityId Id { get; }
        TerritoryState State { get; }
        PlayerId? Controller { get; }
        float CaptureProgress { get; }
    }
}
