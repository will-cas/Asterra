using System;
using Asterra.Core;
using Unity.Netcode;
using UnityEngine;

namespace Asterra.Net
{
    /// <summary>Replicates lobby ready-up / faction pick messages before lockstep starts.</summary>
    public sealed class MatchLobbyNetworkBridge : NetworkBehaviour
    {
        public MatchLobbyState Lobby { get; private set; } = new MatchLobbyState();

        public event Action LobbyChanged;
        public event Action<MatchStartInfo> MatchStarting;

        public void Bind(MatchLobbyState lobby)
        {
            Lobby = lobby ?? new MatchLobbyState();
        }

        public void BroadcastSetFaction(PlayerId player, byte factionIndex)
        {
            Lobby.SetFaction(player, factionIndex);
            LobbyChanged?.Invoke();
            if (IsSpawned)
                LobbyMessageRpc(MatchLobbyState.SerializeSetFaction(player, factionIndex));
        }

        public void BroadcastSetReady(PlayerId player, bool ready)
        {
            Lobby.SetReady(player, ready);
            LobbyChanged?.Invoke();
            if (IsSpawned)
                LobbyMessageRpc(MatchLobbyState.SerializeSetReady(player, ready));
        }

        public void BroadcastClaimSlot(PlayerId player, string displayName)
        {
            Lobby.ClaimSlot(player, displayName);
            LobbyChanged?.Invoke();
            if (IsSpawned)
                LobbyMessageRpc(SerializeClaim(player, displayName));
        }

        public bool TryHostStartMatch()
        {
            if (!IsSpawned || !IsServer)
            {
                if (Lobby.TryStart(out var localStart))
                {
                    MatchStarting?.Invoke(localStart);
                    return true;
                }

                return false;
            }

            if (!Lobby.CanStart)
                return false;

            byte[] payload = MatchLobbyState.SerializeStartMatch(Lobby.MatchSeed);
            LobbyMessageRpc(payload);
            Lobby.ApplyMessage(payload);
            if (Lobby.StartInfo != null)
                MatchStarting?.Invoke(Lobby.StartInfo);
            return Lobby.HasStarted;
        }

        public void BroadcastFullSync()
        {
            if (!IsSpawned || !IsServer)
                return;
            LobbyMessageRpc(MatchLobbyState.SerializeSnapshot(Lobby));
        }

        [Rpc(SendTo.NotMe)]
        private void LobbyMessageRpc(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return;
            var type = MatchLobbyState.PeekType(payload);
            Lobby.ApplyMessage(payload);
            LobbyChanged?.Invoke();
            if (type == LobbyMessageType.StartMatch && Lobby.StartInfo != null)
                MatchStarting?.Invoke(Lobby.StartInfo);
        }

        private static byte[] SerializeClaim(PlayerId player, string displayName)
        {
            using var ms = new System.IO.MemoryStream(32);
            using var writer = new System.IO.BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true);
            writer.Write((byte)LobbyMessageType.ClaimSlot);
            writer.Write(player.Value);
            writer.Write(displayName ?? string.Empty);
            writer.Flush();
            return ms.ToArray();
        }
    }
}
