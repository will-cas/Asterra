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
        private PlayerId _localPlayer;

        public DesyncDetector Desync => _desync;
        public ReplayBuffer Replay => _replay;

        public void Bind(ICommandBus bus, PlayerId localPlayer, ReplayBuffer replay = null)
        {
            _bus = bus;
            _localPlayer = localPlayer;
            _replay = replay ?? new ReplayBuffer();
            _desync = new DesyncDetector();
        }

        public void BroadcastFrame(CommandFrame frame)
        {
            if (frame == null)
                return;
            byte[] payload = CommandCodec.SerializeFrame(frame);
            _replay?.RecordPayload(payload);
            if (IsSpawned)
                SubmitCommandFrameRpc(frame.TargetTick.Value, frame.Player.Value, payload);
            else
                ApplyPayload(payload);
        }

        public void BroadcastWorldHash(Tick tick, ulong hash)
        {
            if (IsSpawned)
                ReportWorldHashRpc(tick.Value, hash);
            else
                _desync?.Report(tick.Value, _localPlayer.Value, hash);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SubmitCommandFrameRpc(uint targetTick, byte player, byte[] payload)
        {
            _ = targetTick;
            _ = player;
            ApplyPayload(payload);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ReportWorldHashRpc(uint tick, ulong hash)
        {
            // Issuer identity isn't on the RPC yet; host stamps channel peer later in Phase 3.
            byte reporter = IsServer ? (byte)0 : _localPlayer.Value;
            _desync?.Report(tick, reporter, hash);
            if (_desync != null && _desync.TryGetDesync(tick, out ulong expected, out ulong actual))
                Debug.LogError($"[Asterra] DESYNC tick={tick} expected={expected} actual={actual}");
        }

        private void ApplyPayload(byte[] payload)
        {
            if (payload == null || payload.Length == 0 || _bus == null)
                return;
            var frame = CommandCodec.DeserializeFrame(payload);
            _replay?.RecordPayload(payload);
            _bus.EnqueueRemote(frame);
        }
    }

    /// <summary>UGS Auth + Lobby + Relay bootstrap placeholder.</summary>
    public sealed class MultiplayerSessionHost : MonoBehaviour
    {
        [SerializeField] private int maxPlayers = 8;

        public async void HostSkirmishAsync(string lobbyName)
        {
            Debug.Log($"[Asterra] HostSkirmish placeholder ({lobbyName}, max {maxPlayers}).");
            await System.Threading.Tasks.Task.CompletedTask;
        }

        public async void JoinSkirmishAsync(string lobbyCode)
        {
            Debug.Log($"[Asterra] JoinSkirmish placeholder ({lobbyCode}).");
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
