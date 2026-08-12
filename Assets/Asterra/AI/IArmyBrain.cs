using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.AI
{
    /// <summary>
    /// Produces GameCommands for a non-human player. Strategy fills this in Phase 4.
    /// </summary>
    public interface IArmyBrain
    {
        PlayerId Player { get; }
        IReadOnlyList<GameCommand> Think(Tick tick);
    }

    public sealed class IdleArmyBrain : IArmyBrain
    {
        public IdleArmyBrain(PlayerId player) => Player = player;

        public PlayerId Player { get; }

        public IReadOnlyList<GameCommand> Think(Tick tick)
        {
            _ = tick;
            return System.Array.Empty<GameCommand>();
        }
    }
}
