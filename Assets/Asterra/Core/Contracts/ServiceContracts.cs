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
        void Seek(Tick tick);
    }

    public interface ICommandBus
    {
        void SubmitLocal(GameCommand command);
        void EnqueueRemote(CommandFrame frame);
        void ScheduleLocal(Tick targetTick);
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
        void Seed(PlayerId player, ResourceType type, int amount);
    }

    public interface ITerritoryMap
    {
        ITerritoryNode Get(SimEntityId id);
        IReadOnlyList<ITerritoryNode> All { get; }
        void SetController(SimEntityId id, PlayerId controller);
    }

    public interface IFactionCatalog
    {
        IFaction Get(FactionId id);
        IReadOnlyList<IFaction> All { get; }
    }

    public interface IProductionQueue
    {
        bool TryEnqueue(SimEntityId buildingId, string unitDefId, PlayerId payer);
        void Tick(float deltaSeconds);
    }

    /// <summary>
    /// Path API for units. Slice 1 may return direct steers; slice 2 flow fields.
    /// Cost-aware overloads accept traversal capabilities so terrain/water/no-entry can participate
    /// without branching on unit type names. Existing callers keep using the 2D overload.
    /// </summary>
    public interface IPathfindingService
    {
        bool TryGetPath(float fromX, float fromZ, float toX, float toZ, List<(float x, float z)> pathOut);
        void RequestFlowField(float toX, float toZ, int fieldId);

        /// <summary>
        /// Cost-aware path. Default delegates to the capability-agnostic overload until a
        /// terrain-aware implementation is wired.
        /// </summary>
        bool TryGetPath(
            float fromX,
            float fromZ,
            float toX,
            float toZ,
            Asterra.Core.World.TraversalCapability capabilities,
            List<(float x, float z)> pathOut)
            => TryGetPath(fromX, fromZ, toX, toZ, pathOut);
    }

    public interface IIdFactory
    {
        SimEntityId Next();
        uint PeekNext { get; }
        void Seek(uint nextId);
    }
}
