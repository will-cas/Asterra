using System.Collections.Generic;

namespace Asterra.Core
{
    /// <summary>Records command frames for replay / desync debugging.</summary>
    public sealed class ReplayBuffer
    {
        private readonly List<byte[]> _frames = new();

        public int Count => _frames.Count;

        public void Record(CommandFrame frame)
        {
            if (frame == null)
                return;
            _frames.Add(CommandCodec.SerializeFrame(frame));
        }

        public void RecordPayload(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return;
            var copy = new byte[payload.Length];
            System.Buffer.BlockCopy(payload, 0, copy, 0, payload.Length);
            _frames.Add(copy);
        }

        public CommandFrame GetFrame(int index)
        {
            return CommandCodec.DeserializeFrame(_frames[index]);
        }

        public IReadOnlyList<byte[]> RawFrames => _frames;

        public void Clear() => _frames.Clear();
    }

    /// <summary>Compares world hashes across peers for lockstep desync detection.</summary>
    public sealed class DesyncDetector
    {
        private readonly Dictionary<uint, Dictionary<byte, ulong>> _hashes = new();

        public void Report(uint tick, byte player, ulong hash)
        {
            if (!_hashes.TryGetValue(tick, out var byPlayer))
            {
                byPlayer = new Dictionary<byte, ulong>();
                _hashes[tick] = byPlayer;
            }

            byPlayer[player] = hash;
        }

        /// <returns>True if at least two peers disagree on the hash for this tick.</returns>
        public bool TryGetDesync(uint tick, out ulong expected, out ulong actual)
        {
            expected = 0;
            actual = 0;
            if (!_hashes.TryGetValue(tick, out var byPlayer) || byPlayer.Count < 2)
                return false;

            bool first = true;
            foreach (var pair in byPlayer)
            {
                if (first)
                {
                    expected = pair.Value;
                    first = false;
                    continue;
                }

                if (pair.Value != expected)
                {
                    actual = pair.Value;
                    return true;
                }
            }

            return false;
        }

        public void ForgetBefore(uint tick)
        {
            var stale = new List<uint>();
            foreach (var key in _hashes.Keys)
            {
                if (key < tick)
                    stale.Add(key);
            }

            for (int i = 0; i < stale.Count; i++)
                _hashes.Remove(stale[i]);
        }
    }
}
