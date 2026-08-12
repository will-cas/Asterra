using System;
using System.Threading.Tasks;
using Asterra.Core;
using Unity.Netcode;
using UnityEngine;

#if ASTERRA_UGS
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
#endif

namespace Asterra.Net
{
    /// <summary>
    /// Hosts or joins a skirmish via UGS Auth + Lobby + Relay, then starts NGO.
    /// Compiles as a stub when UGS packages are not yet resolved (`ASTERRA_UGS`).
    /// </summary>
    public sealed class UnityGamingServicesSession : MonoBehaviour, IMultiplayerSession
    {
        [SerializeField] private int defaultMaxPlayers = 8;
        [SerializeField] private string relayConnectionType = "dtls";

        public MatchLobbyInfo Info { get; private set; } = new MatchLobbyInfo
        {
            Role = SessionRole.Offline,
            MaxPlayers = 8,
        };

        public bool IsConnected { get; private set; }

        public async Task HostAsync(string lobbyName, int maxPlayers, uint matchSeed)
        {
            maxPlayers = Mathf.Clamp(maxPlayers <= 0 ? defaultMaxPlayers : maxPlayers, 2, 8);
            Info = new MatchLobbyInfo
            {
                Role = SessionRole.Host,
                MaxPlayers = maxPlayers,
                MatchSeed = matchSeed,
                CurrentPlayers = 1,
            };

#if ASTERRA_UGS
            await EnsureServicesAsync();
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Info.RelayJoinCode = joinCode;

            var lobby = await LobbyService.Instance.CreateLobbyAsync(
                string.IsNullOrWhiteSpace(lobbyName) ? "Asterra Skirmish" : lobbyName,
                maxPlayers,
                new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new System.Collections.Generic.Dictionary<string, DataObject>
                    {
                        {
                            "relay",
                            new DataObject(DataObject.VisibilityOptions.Member, joinCode)
                        },
                        {
                            "seed",
                            new DataObject(DataObject.VisibilityOptions.Member, matchSeed.ToString())
                        },
                    },
                });

            Info.LobbyId = lobby.Id;
            Info.LobbyCode = lobby.LobbyCode;
            ConfigureTransportAsHost(allocation);
            if (!NetworkManager.Singleton.StartHost())
                throw new InvalidOperationException("NGO StartHost failed.");
            IsConnected = true;
            Debug.Log($"[Asterra] Hosted lobby code={lobby.LobbyCode} relay={joinCode} seed={matchSeed}");
#else
            await Task.CompletedTask;
            Info.LobbyCode = "LOCAL";
            Info.RelayJoinCode = "LOCAL";
            Debug.LogWarning("[Asterra] UGS packages not active (ASTERRA_UGS). Host is offline stub only.");
#endif
        }

        public async Task JoinAsync(string lobbyCode)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode))
                throw new ArgumentException("Lobby code is required.", nameof(lobbyCode));

            Info = new MatchLobbyInfo
            {
                Role = SessionRole.Client,
                MaxPlayers = defaultMaxPlayers,
            };

#if ASTERRA_UGS
            await EnsureServicesAsync();
            var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.Trim());
            Info.LobbyId = lobby.Id;
            Info.LobbyCode = lobby.LobbyCode;
            Info.CurrentPlayers = lobby.Players.Count;
            Info.MaxPlayers = lobby.MaxPlayers;

            if (!lobby.Data.TryGetValue("relay", out var relayData) || string.IsNullOrEmpty(relayData.Value))
                throw new InvalidOperationException("Lobby is missing relay join code.");
            Info.RelayJoinCode = relayData.Value;
            if (lobby.Data.TryGetValue("seed", out var seedData) && uint.TryParse(seedData.Value, out uint seed))
                Info.MatchSeed = seed;

            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(Info.RelayJoinCode);
            ConfigureTransportAsClient(joinAllocation);
            if (!NetworkManager.Singleton.StartClient())
                throw new InvalidOperationException("NGO StartClient failed.");
            IsConnected = true;
            Debug.Log($"[Asterra] Joined lobby code={lobby.LobbyCode} relay={Info.RelayJoinCode}");
#else
            await Task.CompletedTask;
            Info.LobbyCode = lobbyCode;
            Debug.LogWarning("[Asterra] UGS packages not active (ASTERRA_UGS). Join is offline stub only.");
#endif
        }

        public async Task LeaveAsync()
        {
#if ASTERRA_UGS
            try
            {
                if (!string.IsNullOrEmpty(Info.LobbyId) && Info.Role == SessionRole.Host)
                    await LobbyService.Instance.DeleteLobbyAsync(Info.LobbyId);
                else if (!string.IsNullOrEmpty(Info.LobbyId))
                    await LobbyService.Instance.RemovePlayerAsync(Info.LobbyId, AuthenticationService.Instance.PlayerId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Asterra] Lobby leave warning: {ex.Message}");
            }
#endif
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            IsConnected = false;
            Info = new MatchLobbyInfo { Role = SessionRole.Offline, MaxPlayers = defaultMaxPlayers };
            await Task.CompletedTask;
        }

#if ASTERRA_UGS
        private static async Task EnsureServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private void ConfigureTransportAsHost(Allocation allocation)
        {
            var transport = GetUnityTransport();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, relayConnectionType));
        }

        private void ConfigureTransportAsClient(JoinAllocation allocation)
        {
            var transport = GetUnityTransport();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, relayConnectionType));
        }

        private static UnityTransport GetUnityTransport()
        {
            if (NetworkManager.Singleton == null)
                throw new InvalidOperationException("NetworkManager.Singleton is missing from the scene.");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
                throw new InvalidOperationException("UnityTransport is required on NetworkManager.");
            return transport;
        }
#endif
    }
}
