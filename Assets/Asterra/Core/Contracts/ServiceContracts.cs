using System.Collections.Generic;

namespace Asterra.Core
{
    public interface IMatchSession
    {
        bool IsInMatch { get; }
        int PlayerCount { get; }
        PlayerId LocalPlayer { get; }
    }

    public interface ILockstepClock
    {
        Tick CurrentTick { get; }
        int CommandDelayTicks { get; }
        float FixedDeltaSeconds { get; }
        void Advance();
    }

    public interface ICommandBus
    {
        void SubmitLocal(GameCommand command);
        void EnqueueRemote(CommandFrame frame);
        IReadOnlyList<GameCommand> DrainForTick(Tick tick);
    }

    public interface IWorldSim : IWorldQuery
    {
        void ApplyCommands(IReadOnlyList<GameCommand> commands);
        void Tick(float deltaSeconds);
        ulong ComputeWorldHash();
    }

    public interface IResourceWallet
    {
        int Get(PlayerId player, ResourceType type);
        bool CanAfford(PlayerId player, ResourceType type, int amount);
        bool TrySpend(PlayerId player, ResourceType type, int amount);
        void Add(PlayerId player, ResourceType type, int amount);
    }

    public interface ITerritoryMap
    {
        ITerritoryNode Get(EntityId id);
        IReadOnlyList<ITerritoryNode> All { get; }
        void SetController(EntityId id, PlayerId controller);
    }

    public interface IFactionCatalog
    {
        IFaction Get(FactionId id);
        IReadOnlyList<IFaction> All { get; }
    }

    public interface IProductionQueue
    {
        bool TryEnqueue(EntityId buildingId, string unitDefId, PlayerId payer);
        void Tick(float deltaSeconds);
    }

    public interface IPathfindingService
    {
        bool TryGetPath(float fromX, float fromZ, float toX, float toZ, List<(float x, float z)> pathOut);
        void RequestFlowField(float toX, float toZ, int fieldId);
    }

    public interface IIdFactory
    {
        EntityId Next();
    }
}
