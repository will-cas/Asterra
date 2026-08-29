using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Asterra.Core
{
    public enum LobbyMessageType : byte
    {
        ClaimSlot = 1,
        SetFaction = 2,
        SetReady = 3,
        StartMatch = 4,
        SyncLobby = 5,
    }

    public sealed class PlayerSlotState
    {
        public PlayerId Player;
        public byte FactionIndex;
        public bool IsReady;
        public string DisplayName = string.Empty;
    }

    public sealed class MatchStartInfo
    {
        public uint Seed;
        public PlayerSlotState[] Players = Array.Empty<PlayerSlotState>();
    }

    /// <summary>Tracks pre-match ready-up and faction picks for up to 8 players.</summary>
    public sealed class MatchLobbyState
    {
        private readonly Dictionary<byte, PlayerSlotState> _slots = new();

        public uint MatchSeed { get; set; } = 42;
        public int MaxPlayers { get; set; } = 8;
        public bool HasStarted { get; private set; }
        public MatchStartInfo StartInfo { get; private set; }

        public IReadOnlyDictionary<byte, PlayerSlotState> Slots => _slots;

        public int ReadyCount
        {
            get
            {
                int count = 0;
                foreach (var pair in _slots)
                {
                    if (pair.Value.IsReady)
                        count++;
                }

                return count;
            }
        }

        public int PlayerCount => _slots.Count;

        public bool CanStart
        {
            get
            {
                if (HasStarted || _slots.Count < 1)
                    return false;
                foreach (var pair in _slots)
                {
                    if (!pair.Value.IsReady)
                        return false;
                    if (pair.Value.FactionIndex > 5)
                        return false;
                }

                return true;
            }
        }

        public PlayerSlotState ClaimSlot(PlayerId player, string displayName = null)
        {
            if (HasStarted)
                throw new InvalidOperationException("Match already started.");
            if (!_slots.TryGetValue(player.Value, out var slot))
            {
                if (_slots.Count >= MaxPlayers)
                    throw new InvalidOperationException("Lobby is full.");
                slot = new PlayerSlotState
                {
                    Player = player,
                    FactionIndex = (byte)(player.Value % 6),
                    DisplayName = string.IsNullOrEmpty(displayName) ? $"Player {player.Value}" : displayName,
                };
                _slots[player.Value] = slot;
            }
            else if (!string.IsNullOrEmpty(displayName))
            {
                slot.DisplayName = displayName;
            }

            return slot;
        }

        public void SetFaction(PlayerId player, byte factionIndex)
        {
            if (HasStarted)
                return;
            if (!_slots.TryGetValue(player.Value, out var slot))
                slot = ClaimSlot(player);
            slot.FactionIndex = (byte)Math.Min(factionIndex, (byte)5);
            slot.IsReady = false;
        }

        public void SetReady(PlayerId player, bool ready)
        {
            if (HasStarted)
                return;
            if (!_slots.TryGetValue(player.Value, out var slot))
                slot = ClaimSlot(player);
            slot.IsReady = ready;
        }

        public bool TryStart(out MatchStartInfo info)
        {
            info = null;
            if (!CanStart)
                return false;

            var players = new PlayerSlotState[_slots.Count];
            int i = 0;
            foreach (var pair in _slots)
            {
                players[i++] = new PlayerSlotState
                {
                    Player = pair.Value.Player,
                    FactionIndex = pair.Value.FactionIndex,
                    IsReady = pair.Value.IsReady,
                    DisplayName = pair.Value.DisplayName,
                };
            }

            Array.Sort(players, (a, b) => a.Player.Value.CompareTo(b.Player.Value));
            info = new MatchStartInfo
            {
                Seed = MatchSeed,
                Players = players,
            };
            StartInfo = info;
            HasStarted = true;
            return true;
        }

        public static byte[] SerializeSnapshot(MatchLobbyState lobby)
        {
            using var ms = new MemoryStream(64);
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
            writer.Write((byte)LobbyMessageType.SyncLobby);
            writer.Write(lobby.MatchSeed);
            writer.Write(lobby.MaxPlayers);
            writer.Write(lobby.HasStarted);
            writer.Write(lobby._slots.Count);
            foreach (var pair in lobby._slots)
            {
                writer.Write(pair.Value.Player.Value);
                writer.Write(pair.Value.FactionIndex);
                writer.Write(pair.Value.IsReady);
                writer.Write(pair.Value.DisplayName ?? string.Empty);
            }

            writer.Flush();
            return ms.ToArray();
        }

        public static void ApplySnapshot(MatchLobbyState lobby, byte[] payload)
        {
            using var ms = new MemoryStream(payload, writable: false);
            using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            var type = (LobbyMessageType)reader.ReadByte();
            if (type != LobbyMessageType.SyncLobby)
                throw new InvalidDataException("Expected SyncLobby payload.");
            lobby._slots.Clear();
            lobby.MatchSeed = reader.ReadUInt32();
            lobby.MaxPlayers = reader.ReadInt32();
            lobby.HasStarted = reader.ReadBoolean();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var slot = new PlayerSlotState
                {
                    Player = new PlayerId(reader.ReadByte()),
                    FactionIndex = reader.ReadByte(),
                    IsReady = reader.ReadBoolean(),
                    DisplayName = reader.ReadString(),
                };
                lobby._slots[slot.Player.Value] = slot;
            }
        }

        public static byte[] SerializeSetFaction(PlayerId player, byte factionIndex)
        {
            using var ms = new MemoryStream(8);
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
            writer.Write((byte)LobbyMessageType.SetFaction);
            writer.Write(player.Value);
            writer.Write(factionIndex);
            writer.Flush();
            return ms.ToArray();
        }

        public static byte[] SerializeSetReady(PlayerId player, bool ready)
        {
            using var ms = new MemoryStream(8);
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
            writer.Write((byte)LobbyMessageType.SetReady);
            writer.Write(player.Value);
            writer.Write(ready);
            writer.Flush();
            return ms.ToArray();
        }

        public static byte[] SerializeStartMatch(uint seed)
        {
            using var ms = new MemoryStream(8);
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
            writer.Write((byte)LobbyMessageType.StartMatch);
            writer.Write(seed);
            writer.Flush();
            return ms.ToArray();
        }

        public static LobbyMessageType PeekType(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                throw new ArgumentException("Empty lobby payload.");
            return (LobbyMessageType)payload[0];
        }

        public void ApplyMessage(byte[] payload)
        {
            using var ms = new MemoryStream(payload, writable: false);
            using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            var type = (LobbyMessageType)reader.ReadByte();
            switch (type)
            {
                case LobbyMessageType.SetFaction:
                    SetFaction(new PlayerId(reader.ReadByte()), reader.ReadByte());
                    break;
                case LobbyMessageType.SetReady:
                    SetReady(new PlayerId(reader.ReadByte()), reader.ReadBoolean());
                    break;
                case LobbyMessageType.StartMatch:
                    MatchSeed = reader.ReadUInt32();
                    if (!TryStart(out _))
                        throw new InvalidOperationException("StartMatch received but lobby cannot start.");
                    break;
                case LobbyMessageType.SyncLobby:
                    ApplySnapshot(this, payload);
                    break;
                case LobbyMessageType.ClaimSlot:
                    ClaimSlot(new PlayerId(reader.ReadByte()), reader.ReadString());
                    break;
                default:
                    throw new InvalidDataException($"Unknown lobby message {(byte)type}");
            }
        }
    }
}
