using Asterra.Core;
using Unity.Netcode;
using UnityEngine;

namespace Asterra.Net
{
    /// <summary>
    /// NGO transport for lockstep command frames. Does not NetworkObject-spawn individual units.
    /// </summary>
    public sealed class LockstepNetworkBridge : NetworkBehaviour
    {
        private ICommandBus _bus;
        private DesyncDetector _desync;
        private ReplayBuffer _replay;
        private LockstepFrameGate _gate;
        private PlayerId _localPlayer;
        private bool _useGate;

        public DesyncDetector Desync => _desync;
        public ReplayBuffer Replay => _replay;
        public LockstepFrameGate Gate => _gate;

        public void Bind(
            ICommandBus bus,
            PlayerId localPlayer,
            ReplayBuffer replay = null,
            LockstepFrameGate gate = null)
        {
            _bus = bus;
            _localPlayer = localPlayer;
            _replay = replay ?? new ReplayBuffer();
            _desync = new DesyncDetector();
            _gate = gate;
            _useGate = gate != null;
        }

        public void BroadcastFrame(CommandFrame frame)
        {
            if (frame == null)
                return;

            // Local apply once; RPC fans out to everyone else.
            ApplyFrame(frame);
            if (!IsSpawned)
                return;

            byte[] payload = CommandCodec.SerializeFrame(frame);
            SubmitCommandFrameRpc(frame.TargetTick.Value, frame.Player.Value, payload);
        }

        public void BroadcastWorldHash(Tick tick, ulong hash)
        {
            _desync?.Report(tick.Value, _localPlayer.Value, hash);
            if (!IsSpawned)
                return;
            ReportWorldHashRpc(tick.Value, _localPlayer.Value, hash);
        }

        [Rpc(SendTo.NotMe)]
        private void SubmitCommandFrameRpc(uint targetTick, byte player, byte[] payload)
        {
            _ = targetTick;
            _ = player;
            if (payload == null || payload.Length == 0)
                return;
            ApplyFrame(CommandCodec.DeserializeFrame(payload));
        }

        [Rpc(SendTo.NotMe)]
        private void ReportWorldHashRpc(uint tick, byte player, ulong hash)
        {
            _desync?.Report(tick, player, hash);
            if (_desync != null && _desync.TryGetDesync(tick, out ulong expected, out ulong actual))
                Debug.LogError($"[Asterra] DESYNC tick={tick} player={player} expected={expected} actual={actual}");
            _desync?.ForgetBefore(tick > 64 ? tick - 64 : 0);
        }

        private void ApplyFrame(CommandFrame frame)
        {
            if (frame == null)
                return;
            _replay?.Record(frame);
            if (_useGate && _gate != null)
            {
                _gate.Submit(frame);
                return;
            }

            _bus?.EnqueueRemote(frame);
        }
    }
}
