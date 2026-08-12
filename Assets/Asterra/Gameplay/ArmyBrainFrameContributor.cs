using System.Collections.Generic;
using Asterra.AI;
using Asterra.Core;

namespace Asterra.Gameplay
{
    /// <summary>Adapts <see cref="IArmyBrain"/> into lockstep frames for offline AI seats.</summary>
    public sealed class ArmyBrainFrameContributor : IFrameContributor
    {
        private readonly IArmyBrain _brain;
        private readonly IWorldQuery _world;
        private readonly IResourceWallet _wallet;

        public ArmyBrainFrameContributor(IArmyBrain brain, IWorldQuery world, IResourceWallet wallet)
        {
            _brain = brain;
            _world = world;
            _wallet = wallet;
        }

        public PlayerId Player => _brain.Player;

        public CommandFrame BuildFrame(Tick targetTick, IWorldQuery world, IResourceWallet wallet)
        {
            var query = world ?? _world;
            var walletRef = wallet ?? _wallet;
            var thoughts = _brain.Think(new ArmyBrainContext(targetTick, query, walletRef));
            var commands = thoughts as GameCommand[] ?? ToArray(thoughts);
            for (int i = 0; i < commands.Length; i++)
                commands[i].IssueTick = targetTick;

            return new CommandFrame
            {
                TargetTick = targetTick,
                Player = _brain.Player,
                Commands = commands,
            };
        }

        private static GameCommand[] ToArray(IReadOnlyList<GameCommand> list)
        {
            if (list == null || list.Count == 0)
                return System.Array.Empty<GameCommand>();
            var arr = new GameCommand[list.Count];
            for (int i = 0; i < list.Count; i++)
                arr[i] = list[i];
            return arr;
        }
    }
}
