using System.Threading.Tasks;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Net
{
    /// <summary>
    /// Session placeholder for Auth/Lobby/Relay. Kept package-free so the offline 1v1 demo
    /// compiles even when UGS Lobby assemblies are missing or mismatched on Unity 6.3.
    /// Reintroduce live UGS wiring after Package Manager shows Lobby + Relay as installed.
    /// </summary>
    public sealed class UnityGamingServicesSession : MonoBehaviour, IMultiplayerSession
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
                LobbyCode = "LOCAL",
                RelayJoinCode = "LOCAL",
            };
            IsConnected = false;
            Debug.LogWarning(
                $"[Asterra] Host stub only ({lobbyName}, max {maxPlayers}, seed {matchSeed}). " +
                "Install/enable Unity Lobby + Relay packages, then restore UGS session wiring.");
            return Task.CompletedTask;
        }

        public Task JoinAsync(string lobbyCode)
        {
            Info = new MatchLobbyInfo
            {
                Role = SessionRole.Client,
                MaxPlayers = defaultMaxPlayers,
                LobbyCode = lobbyCode,
            };
            IsConnected = false;
            Debug.LogWarning($"[Asterra] Join stub only ({lobbyCode}). UGS Lobby not wired in this build.");
            return Task.CompletedTask;
        }

        public Task LeaveAsync()
        {
            IsConnected = false;
            Info = new MatchLobbyInfo { Role = SessionRole.Offline, MaxPlayers = defaultMaxPlayers };
            return Task.CompletedTask;
        }
    }
}
