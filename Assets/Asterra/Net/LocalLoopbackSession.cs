using System.Threading.Tasks;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Net
{
    /// <summary>
    /// MonoBehaviour wrapper around <see cref="LoopbackSession"/> for scene wiring.
    /// Use for dual-client soak and offline lobby drills until Lobby+Relay packages are restored.
    /// </summary>
    public sealed class LocalLoopbackSession : MonoBehaviour, IMultiplayerSession
    {
        [SerializeField] private int defaultMaxPlayers = 8;

        private LoopbackSession _inner;

        private void Awake() => EnsureInner();

        private void EnsureInner()
        {
            if (_inner == null)
                _inner = new LoopbackSession(defaultMaxPlayers);
        }

        public MatchLobbyInfo Info
        {
            get
            {
                EnsureInner();
                return _inner.Info;
            }
        }

        public bool IsConnected
        {
            get
            {
                EnsureInner();
                return _inner.IsConnected;
            }
        }

        public Task HostAsync(string lobbyName, int maxPlayers, uint matchSeed)
        {
            EnsureInner();
            Debug.Log($"[Asterra] Local loopback host ready ({lobbyName}, max {maxPlayers}, seed {matchSeed}).");
            return _inner.HostAsync(lobbyName, maxPlayers, matchSeed);
        }

        public Task JoinAsync(string lobbyCode)
        {
            EnsureInner();
            Debug.Log($"[Asterra] Local loopback client joined ({lobbyCode}).");
            return _inner.JoinAsync(lobbyCode);
        }

        public Task LeaveAsync()
        {
            EnsureInner();
            return _inner.LeaveAsync();
        }

        /// <summary>Simulate a second player arriving (for soak scripts).</summary>
        public void AddLocalPeer()
        {
            EnsureInner();
            _inner.AddLocalPeer();
        }
    }
}
