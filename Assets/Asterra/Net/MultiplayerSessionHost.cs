using Asterra.Core;
using UnityEngine;

namespace Asterra.Net
{
    /// <summary>
    /// Inspector-facing session host. Defaults to <see cref="LocalLoopbackSession"/> so Editor
    /// soak works without UGS Lobby/Relay. Flip to UGS stub when packages are restored.
    /// </summary>
    public sealed class MultiplayerSessionHost : MonoBehaviour
    {
        public enum SessionBackend : byte
        {
            LocalLoopback = 0,
            UnityGamingServicesStub = 1,
        }

        [SerializeField] private SessionBackend backend = SessionBackend.LocalLoopback;
        [SerializeField] private LocalLoopbackSession loopbackSession;
        [SerializeField] private UnityGamingServicesSession ugsSession;
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private uint matchSeed = 42;
        [SerializeField] private string defaultLobbyName = "Asterra Skirmish";

        public IMultiplayerSession Session { get; private set; }

        private void Awake() => ResolveSession();

        private void ResolveSession()
        {
            if (backend == SessionBackend.LocalLoopback)
            {
                if (loopbackSession == null)
                    loopbackSession = GetComponent<LocalLoopbackSession>();
                if (loopbackSession == null)
                    loopbackSession = gameObject.AddComponent<LocalLoopbackSession>();
                Session = loopbackSession;
                return;
            }

            if (ugsSession == null)
                ugsSession = GetComponent<UnityGamingServicesSession>();
            if (ugsSession == null)
                ugsSession = gameObject.AddComponent<UnityGamingServicesSession>();
            Session = ugsSession;
        }

        public async void HostSkirmishAsync(string lobbyName = null)
        {
            if (Session == null)
                ResolveSession();
            await Session.HostAsync(
                string.IsNullOrWhiteSpace(lobbyName) ? defaultLobbyName : lobbyName,
                maxPlayers,
                matchSeed);
        }

        public async void JoinSkirmishAsync(string lobbyCode)
        {
            if (Session == null)
                ResolveSession();
            await Session.JoinAsync(lobbyCode);
        }

        public async void LeaveAsync()
        {
            if (Session == null)
                ResolveSession();
            await Session.LeaveAsync();
        }

        /// <summary>Editor soak helper: grow local peer count on the loopback host.</summary>
        public void AddLocalPeer()
        {
            if (Session == null)
                ResolveSession();
            if (loopbackSession != null)
                loopbackSession.AddLocalPeer();
        }
    }
}
