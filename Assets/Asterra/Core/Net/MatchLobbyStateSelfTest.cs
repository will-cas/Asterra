using System.Text;

namespace Asterra.Core
{
    public static class MatchLobbyStateSelfTest
    {
        public static string Run()
        {
            var lobby = new MatchLobbyState { MatchSeed = 99, MaxPlayers = 8 };
            lobby.ClaimSlot(new PlayerId(0), "Host");
            lobby.ClaimSlot(new PlayerId(1), "Client");
            lobby.SetFaction(new PlayerId(0), 0);
            lobby.SetFaction(new PlayerId(1), 2);
            lobby.SetTeamColor(new PlayerId(0), 4);
            lobby.SetTeamColor(new PlayerId(1), 6);
            lobby.SetReady(new PlayerId(0), true);

            if (lobby.CanStart)
                throw new System.InvalidOperationException("Should not start with one player unready.");

            lobby.SetReady(new PlayerId(1), true);
            if (!lobby.TryStart(out var start))
                throw new System.InvalidOperationException("Expected start.");
            if (start.Players.Length != 2 || start.Seed != 99)
                throw new System.InvalidOperationException("Bad start info.");
            if (start.Players[0].TeamColorIndex != 4 || start.Players[1].TeamColorIndex != 6)
                throw new System.InvalidOperationException("Team color not copied into start info.");

            var snapshot = MatchLobbyState.SerializeSnapshot(lobby);
            var copy = new MatchLobbyState();
            MatchLobbyState.ApplySnapshot(copy, snapshot);
            if (copy.PlayerCount != 2 || !copy.HasStarted)
                throw new System.InvalidOperationException("Snapshot roundtrip failed.");
            if (!copy.Slots.TryGetValue(0, out var host) || host.TeamColorIndex != 4)
                throw new System.InvalidOperationException("Snapshot dropped team color.");

            var sb = new StringBuilder();
            sb.AppendLine("[Asterra Lobby]");
            sb.AppendLine("status=ok");
            return sb.ToString();
        }
    }
}
