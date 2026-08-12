using UnityEngine;
using Asterra.Net;

namespace Asterra.UI
{
    /// <summary>Minimal host/join controls for skirmish lobbies.</summary>
    public sealed class MultiplayerMenu : MonoBehaviour
    {
        [SerializeField] private MultiplayerSessionHost sessionHost;
        [SerializeField] private string lobbyName = "Asterra Skirmish";
        [SerializeField] private string joinCode = "";
        [SerializeField] private string status = "Idle";

        public string Status => status;

        public void Host()
        {
            if (sessionHost == null)
            {
                status = "Missing MultiplayerSessionHost";
                return;
            }

            status = "Hosting...";
            sessionHost.HostSkirmishAsync(lobbyName);
            status = "Host requested";
        }

        public void Join()
        {
            if (sessionHost == null)
            {
                status = "Missing MultiplayerSessionHost";
                return;
            }

            status = $"Joining {joinCode}...";
            sessionHost.JoinSkirmishAsync(joinCode);
            status = "Join requested";
        }

        public void Leave()
        {
            sessionHost?.LeaveAsync();
            status = "Left";
        }

        public void SetJoinCode(string code) => joinCode = code ?? string.Empty;
    }
}
