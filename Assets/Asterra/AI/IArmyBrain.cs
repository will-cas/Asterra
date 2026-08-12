using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.AI
{
    public readonly struct ArmyBrainContext
    {
        public readonly Tick Tick;
        public readonly IWorldQuery World;
        public readonly IResourceWallet Wallet;

        public ArmyBrainContext(Tick tick, IWorldQuery world, IResourceWallet wallet)
        {
            Tick = tick;
            World = world;
            Wallet = wallet;
        }
    }

    /// <summary>
    /// Produces GameCommands for a non-human player. Strategy fills this in Phase 4.
    /// </summary>
    public interface IArmyBrain
    {
        PlayerId Player { get; }
        IReadOnlyList<GameCommand> Think(in ArmyBrainContext context);
    }

    public sealed class IdleArmyBrain : IArmyBrain
    {
        public IdleArmyBrain(PlayerId player) => Player = player;

        public PlayerId Player { get; }

        public IReadOnlyList<GameCommand> Think(in ArmyBrainContext context)
        {
            return System.Array.Empty<GameCommand>();
        }
    }
}
