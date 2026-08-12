using System.Collections.Generic;

namespace Asterra.Core
{
    /// <summary>
    /// Collects per-player command frames for a tick and releases them only when all expected
    /// players have reported (empty frames allowed). Classic lockstep readiness gate.
    /// </summary>
    public sealed class LockstepFrameGate
    {
        private readonly HashSet<byte> _expectedPlayers = new();
        private readonly Dictionary<uint, Dictionary<byte, CommandFrame>> _frames = new();

        public void SetExpectedPlayers(IEnumerable<PlayerId> players)
        {
            _expectedPlayers.Clear();
            foreach (var player in players)
                _expectedPlayers.Add(player.Value);
        }

        public void AddExpectedPlayer(PlayerId player) => _expectedPlayers.Add(player.Value);

        public void ClearExpectedPlayers() => _expectedPlayers.Clear();

        public int ExpectedCount => _expectedPlayers.Count;

        public void Submit(CommandFrame frame)
        {
            if (frame == null)
                return;
            if (!_frames.TryGetValue(frame.TargetTick.Value, out var byPlayer))
            {
                byPlayer = new Dictionary<byte, CommandFrame>();
                _frames[frame.TargetTick.Value] = byPlayer;
            }

            byPlayer[frame.Player.Value] = frame;
        }

        public bool TryConsume(Tick tick, List<GameCommand> into)
        {
            if (into == null)
                throw new System.ArgumentNullException(nameof(into));
            if (_expectedPlayers.Count == 0)
                return false;
            if (!_frames.TryGetValue(tick.Value, out var byPlayer))
                return false;

            foreach (var player in _expectedPlayers)
            {
                if (!byPlayer.ContainsKey(player))
                    return false;
            }

            into.Clear();
            foreach (var player in _expectedPlayers)
            {
                var frame = byPlayer[player];
                if (frame.Commands == null)
                    continue;
                for (int i = 0; i < frame.Commands.Length; i++)
                    into.Add(frame.Commands[i]);
            }

            _frames.Remove(tick.Value);
            return true;
        }

        /// <summary>Inserts an empty frame for a player that issued no orders this tick.</summary>
        public void SubmitEmpty(Tick tick, PlayerId player)
        {
            Submit(new CommandFrame
            {
                TargetTick = tick,
                Player = player,
                Commands = System.Array.Empty<GameCommand>(),
            });
        }
    }
}
