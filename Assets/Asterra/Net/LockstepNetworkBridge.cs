using Unity.Netcode;
using UnityEngine;

namespace Asterra.Net
{
    /// <summary>
    /// NGO transport for lockstep command frames. Does not NetworkObject-spawn individual units.
    /// </summary>
    public sealed class LockstepNetworkBridge : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        /// <summary>Client → all: opaque serialized CommandFrame for a future tick.</summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void SubmitCommandFrameRpc(uint targetTick, byte player, byte[] payload)
        {
            // Deserialize into Asterra.Core.CommandFrame and ICommandBus.EnqueueRemote in Phase 3.
            _ = targetTick;
            _ = player;
            _ = payload;
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ReportWorldHashRpc(uint tick, ulong hash)
        {
            _ = tick;
            _ = hash;
        }
    }

    /// <summary>UGS Auth + Lobby + Relay bootstrap placeholder.</summary>
    public sealed class MultiplayerSessionHost : MonoBehaviour
    {
        [SerializeField] private int maxPlayers = 8;

        public async void HostSkirmishAsync(string lobbyName)
        {
            // Phase 3: AuthenticationService → LobbyService.CreateLobby → Relay allocation → NGO StartHost.
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
