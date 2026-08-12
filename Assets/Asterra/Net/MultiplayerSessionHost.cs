using Asterra.Core;
using UnityEngine;

namespace Asterra.Net
{
    /// <summary>Thin inspector-facing wrapper around <see cref="UnityGamingServicesSession"/>.</summary>
    public sealed class MultiplayerSessionHost : MonoBehaviour
    {
        [SerializeField] private UnityGamingServicesSession session;
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private uint matchSeed = 42;
        [SerializeField] private string defaultLobbyName = "Asterra Skirmish";

        public IMultiplayerSession Session => session;

        private void Awake()
        {
            if (session == null)
                session = GetComponent<UnityGamingServicesSession>();
            if (session == null)
                session = gameObject.AddComponent<UnityGamingServicesSession>();
        }

        public async void HostSkirmishAsync(string lobbyName = null)
        {
            await session.HostAsync(
                string.IsNullOrWhiteSpace(lobbyName) ? defaultLobbyName : lobbyName,
                maxPlayers,
                matchSeed);
        }

        public async void JoinSkirmishAsync(string lobbyCode)
        {
            await session.JoinAsync(lobbyCode);
        }

        public async void LeaveAsync()
        {
            await session.LeaveAsync();
        }
    }
}
