using System;
using System.Threading.Tasks;

namespace Asterra.Core
{
    public enum SessionRole : byte
    {
        Offline = 0,
        Host = 1,
        Client = 2,
    }

    public sealed class MatchLobbyInfo
    {
        public string LobbyId;
        public string LobbyCode;
        public string RelayJoinCode;
        public int MaxPlayers = 8;
        public int CurrentPlayers;
        public uint MatchSeed;
        public SessionRole Role;
    }

    public interface IMultiplayerSession
    {
        MatchLobbyInfo Info { get; }
        bool IsConnected { get; }
        Task HostAsync(string lobbyName, int maxPlayers, uint matchSeed);
        Task JoinAsync(string lobbyCode);
        Task LeaveAsync();
    }
}
