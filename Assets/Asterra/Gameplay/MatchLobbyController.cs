using Asterra.Core;
using Asterra.Gameplay;
using Asterra.Gameplay.Content;
using Asterra.Net;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Local lobby controls: claim seat, pick faction, ready, start.</summary>
    public sealed class MatchLobbyController : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private MatchLobbyNetworkBridge lobbyBridge;
        [SerializeField] private byte localPlayerIndex;
        [SerializeField] private string displayName = "Commander";

        public MatchLobbyState Lobby => lobbyBridge != null ? lobbyBridge.Lobby : match != null ? match.Lobby : null;
        public string Status { get; private set; } = "Lobby idle";

        private void Awake()
        {
            if (match == null)
                match = GetComponent<MatchBootstrap>();
            if (lobbyBridge == null)
                lobbyBridge = GetComponent<MatchLobbyNetworkBridge>();

            if (match != null)
            {
                if (lobbyBridge != null)
                    lobbyBridge.Bind(match.Lobby);
                else if (match.Lobby != null)
                {
                    // Offline lobby uses MatchBootstrap.Lobby directly.
                }
            }

            if (lobbyBridge != null)
            {
                lobbyBridge.LobbyChanged += () => Status = DescribeLobby();
                lobbyBridge.MatchStarting += OnMatchStarting;
            }
        }

        public void ClaimLocalSlot()
        {
            var player = new PlayerId(localPlayerIndex);
            if (lobbyBridge != null)
                lobbyBridge.BroadcastClaimSlot(player, displayName);
            else
                match?.Lobby.ClaimSlot(player, displayName);
            Status = DescribeLobby();
        }

        public void SetLocalFaction(int factionIndex)
        {
            var player = new PlayerId(localPlayerIndex);
            byte faction = (byte)Mathf.Clamp(factionIndex, 0, FactionDefaultContent.All.Length - 1);
            if (lobbyBridge != null)
                lobbyBridge.BroadcastSetFaction(player, faction);
            else
                match?.Lobby.SetFaction(player, faction);
            Status = DescribeLobby();
        }

        public void SetLocalTeamColor(int colorIndex)
        {
            var player = new PlayerId(localPlayerIndex);
            byte color = (byte)Mathf.Clamp(colorIndex, 0, 7);
            if (lobbyBridge != null)
                lobbyBridge.BroadcastSetTeamColor(player, color);
            else
                match?.Lobby.SetTeamColor(player, color);
            match?.SetTeamColorIndex(player, color);
            Status = DescribeLobby();
        }

        public void SetLocalReady(bool ready)
        {
            var player = new PlayerId(localPlayerIndex);
            if (lobbyBridge != null)
                lobbyBridge.BroadcastSetReady(player, ready);
            else
                match?.Lobby.SetReady(player, ready);
            Status = DescribeLobby();
        }

        public void HostStart()
        {
            if (lobbyBridge != null)
            {
                if (!lobbyBridge.TryHostStartMatch())
                    Status = "Cannot start (not all ready?)";
                return;
            }

            if (match?.Lobby != null && match.Lobby.TryStart(out var info))
                OnMatchStarting(info);
            else
                Status = "Cannot start (not all ready?)";
        }

        private void OnMatchStarting(MatchStartInfo info)
        {
            Status = $"Starting seed={info.Seed} players={info.Players.Length}";
            if (match == null)
                return;
            match.StartOnlineFromLobby(new PlayerId(localPlayerIndex), info);
        }

        private string DescribeLobby()
        {
            var lobby = Lobby;
            if (lobby == null)
                return "No lobby";
            return $"players={lobby.PlayerCount} ready={lobby.ReadyCount}/{lobby.PlayerCount} canStart={lobby.CanStart}";
        }
    }
}
