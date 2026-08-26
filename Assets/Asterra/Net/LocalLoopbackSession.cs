using System.Threading.Tasks;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Net
{
    /// <summary>
    /// Editor/local lockstep host that marks the session connected without UGS.
    /// Use for dual-client soak and offline lobby drills until Lobby+Relay packages are restored.
    /// </summary>
    public sealed class LocalLoopbackSession : MonoBehaviour, IMultiplayerSession
    {
        [SerializeField] private int defaultMaxPlayers = 8;

        public MatchLobbyInfo Info { get; private set; } = new MatchLobbyInfo
        {
            Role = SessionRole.Offline,
            MaxPlayers = 8,
        };

        public bool IsConnected { get; private set; }

        public Task HostAsync(string lobbyName, int maxPlayers, uint matchSeed)
        {
            maxPlayers = Mathf.Clamp(maxPlayers <= 0 ? defaultMaxPlayers : maxPlayers, 2, 8);
            Info = new MatchLobbyInfo
            {
                Role = SessionRole.Host,
                MaxPlayers = maxPlayers,
                MatchSeed = matchSeed,
                CurrentPlayers = 1,
                LobbyCode = "LOOP",
                RelayJoinCode = "LOOP",
            };
            IsConnected = true;
            Debug.Log($"[Asterra] Local loopback host ready ({lobbyName}, max {maxPlayers}, seed {matchSeed}).");
            return Task.CompletedTask;
        }

        public Task JoinAsync(string lobbyCode)
        {
            Info = new MatchLobbyInfo
            {
                Role = SessionRole.Client,
                MaxPlayers = defaultMaxPlayers,
                LobbyCode = string.IsNullOrEmpty(lobbyCode) ? "LOOP" : lobbyCode,
                RelayJoinCode = "LOOP",
                CurrentPlayers = 2,
            };
            IsConnected = true;
            Debug.Log($"[Asterra] Local loopback client joined ({Info.LobbyCode}).");
            return Task.CompletedTask;
        }

        public Task LeaveAsync()
        {
            IsConnected = false;
            Info = new MatchLobbyInfo { Role = SessionRole.Offline, MaxPlayers = defaultMaxPlayers };
            return Task.CompletedTask;
        }

        /// <summary>Simulate a second player arriving (for soak scripts).</summary>
        public void AddLocalPeer()
        {
            if (!IsConnected)
                return;
            var info = Info;
            info.CurrentPlayers = Mathf.Min(info.MaxPlayers, info.CurrentPlayers + 1);
            Info = info;
        }
    }
}
