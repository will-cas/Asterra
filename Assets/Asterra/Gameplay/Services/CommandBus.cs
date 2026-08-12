using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class CommandBus : ICommandBus
    {
        private readonly List<GameCommand> _localPending = new();
        private readonly Dictionary<uint, List<GameCommand>> _byTick = new();

        public void SubmitLocal(GameCommand command)
        {
            if (command == null)
                return;
            _localPending.Add(command);
        }

        public void EnqueueRemote(CommandFrame frame)
        {
            if (frame?.Commands == null)
                return;
            if (!_byTick.TryGetValue(frame.TargetTick.Value, out var list))
            {
                list = new List<GameCommand>();
                _byTick[frame.TargetTick.Value] = list;
            }

            list.AddRange(frame.Commands);
        }

        /// <summary>Moves local submits into a target tick bucket (call from lockstep scheduler).</summary>
        public void ScheduleLocal(Tick targetTick)
        {
            if (_localPending.Count == 0)
                return;
            if (!_byTick.TryGetValue(targetTick.Value, out var list))
            {
                list = new List<GameCommand>();
                _byTick[targetTick.Value] = list;
            }

            foreach (var command in _localPending)
            {
                command.IssueTick = targetTick;
                list.Add(command);
            }

            _localPending.Clear();
        }

        public IReadOnlyList<GameCommand> DrainForTick(Tick tick)
        {
            if (_byTick.TryGetValue(tick.Value, out var list))
            {
                _byTick.Remove(tick.Value);
                return list;
            }

            return System.Array.Empty<GameCommand>();
        }
    }
}
