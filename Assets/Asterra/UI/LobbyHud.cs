using Asterra.Core;
using Asterra.Gameplay;
using Asterra.Net;
using UnityEngine;

namespace Asterra.UI
{
    /// <summary>Exposes lobby actions for UI buttons / debug inspector.</summary>
    public sealed class LobbyHud : MonoBehaviour
    {
        [SerializeField] private MatchLobbyController lobby;
        [SerializeField] private MultiplayerMenu multiplayerMenu;
        [SerializeField] private int factionIndex;

        public string StatusLine { get; private set; } = "Lobby";

        private void Update()
        {
            if (lobby != null)
                StatusLine = lobby.Status;
        }

        public void OnClaim() => lobby?.ClaimLocalSlot();
        public void OnReady() => lobby?.SetLocalReady(true);
        public void OnUnready() => lobby?.SetLocalReady(false);
        public void OnStart() => lobby?.HostStart();

        public void OnFactionIron() => lobby?.SetLocalFaction(0);
        public void OnFactionVerdant() => lobby?.SetLocalFaction(1);
        public void OnFactionAshen() => lobby?.SetLocalFaction(2);

        public void OnCycleFaction()
        {
            factionIndex = (factionIndex + 1) % 3;
            lobby?.SetLocalFaction(factionIndex);
        }

        public void OnHostSession() => multiplayerMenu?.Host();
        public void OnJoinSession() => multiplayerMenu?.Join();
    }
}
