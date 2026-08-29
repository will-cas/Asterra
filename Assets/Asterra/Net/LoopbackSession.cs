using System.Threading.Tasks;
using Asterra.Core;

namespace Asterra.Net
{
    /// <summary>
    /// Pure lockstep session for Editor soak / offline drills — no UGS or NGO required.
    /// Marks the session connected so lobby UI can proceed while packages are stubbed.
    /// </summary>
    public sealed class LoopbackSession : IMultiplayerSession
    {
        private readonly int _defaultMaxPlayers;

        public LoopbackSession(int defaultMaxPlayers = 8)
        {
            _defaultMaxPlayers = defaultMaxPlayers <= 0 ? 8 : defaultMaxPlayers;
            Info = new MatchLobbyInfo
            {
                Role = SessionRole.Offline,
                MaxPlayers = _defaultMaxPlayers,
            };
        }

        public MatchLobbyInfo Info { get; private set; }

        public bool IsConnected { get; private set; }

        public Task HostAsync(string lobbyName, int maxPlayers, uint matchSeed)
        {
            int clamped = maxPlayers <= 0 ? _defaultMaxPlayers : maxPlayers;
            if (clamped < 2)
                clamped = 2;
            if (clamped > 8)
                clamped = 8;

            Info = new MatchLobbyInfo
            {
                Role = SessionRole.Host,
                MaxPlayers = clamped,
                MatchSeed = matchSeed,
                CurrentPlayers = 1,
                LobbyCode = "LOOP",
                RelayJoinCode = "LOOP",
            };
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task JoinAsync(string lobbyCode)
        {
            Info = new MatchLobbyInfo
            {
                Role = SessionRole.Client,
                MaxPlayers = _defaultMaxPlayers,
                LobbyCode = string.IsNullOrEmpty(lobbyCode) ? "LOOP" : lobbyCode,
                RelayJoinCode = "LOOP",
                CurrentPlayers = 2,
            };
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task LeaveAsync()
        {
            IsConnected = false;
            Info = new MatchLobbyInfo { Role = SessionRole.Offline, MaxPlayers = _defaultMaxPlayers };
            return Task.CompletedTask;
        }

        public void AddLocalPeer()
        {
            if (!IsConnected)
                return;
            var info = Info;
            int next = info.CurrentPlayers + 1;
            if (next > info.MaxPlayers)
                next = info.MaxPlayers;
            info.CurrentPlayers = next;
            Info = info;
        }
    }
}
