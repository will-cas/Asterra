using System;
using System.IO;
using Asterra.Core;
using Asterra.Gameplay.Sim;
using UnityEngine;

namespace Asterra.Gameplay.Save
{
    /// <summary>Offline skirmish save/load under persistentDataPath/Asterra/Saves.</summary>
    public static class OfflineMatchSaveService
    {
        public const string DefaultSlotFile = "skirmish_quicksave.json";
        public const int CurrentFormatVersion = 2;

        public static string SavesDirectory =>
            Path.Combine(Application.persistentDataPath, "Asterra", "Saves");

        public static string DefaultSlotPath => Path.Combine(SavesDirectory, DefaultSlotFile);

        public static bool HasQuickSave => File.Exists(DefaultSlotPath);

        public static MatchSaveData Capture(
            MatchBootstrap match,
            SkirmishWorldSim sim,
            IResourceWallet wallet)
        {
            if (match == null || sim == null)
                throw new ArgumentNullException(match == null ? nameof(match) : nameof(sim));

            var data = new MatchSaveData
            {
                formatVersion = CurrentFormatVersion,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                matchSeed = match.MatchSeed,
                mapKey = match.MapKey,
                playerFaction = match.PlayerFactionIndex,
                enemyFaction = match.EnemyFactionIndex,
                aiDifficulty = (int)match.AiDifficulty,
                tick = match.Clock != null ? match.Clock.CurrentTick.Value : 0u,
                nextEntityId = match.Ids != null ? match.Ids.PeekNext : 1u,
                holdSecondsP0 = match.Victory != null ? match.Victory.GetHoldSeconds(new PlayerId(0)) : 0f,
                holdSecondsP1 = match.Victory != null ? match.Victory.GetHoldSeconds(new PlayerId(1)) : 0f,
            };

            if (wallet != null)
            {
                data.wallets = new[]
                {
                    new WalletSave
                    {
                        player = 0,
                        gold = wallet.Get(new PlayerId(0), ResourceType.Gold),
                        timber = wallet.Get(new PlayerId(0), ResourceType.Timber),
                    },
                    new WalletSave
                    {
                        player = 1,
                        gold = wallet.Get(new PlayerId(1), ResourceType.Gold),
                        timber = wallet.Get(new PlayerId(1), ResourceType.Timber),
                    },
                };
            }

            sim.CaptureInto(data);
            return data;
        }

        public static string SaveQuick(MatchBootstrap match, SkirmishWorldSim sim, IResourceWallet wallet)
        {
            var data = Capture(match, sim, wallet);
            return Write(DefaultSlotPath, data);
        }

        public static string Write(string path, MatchSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            return path;
        }

        public static bool TryLoadQuick(out MatchSaveData data)
        {
            return TryRead(DefaultSlotPath, out data);
        }

        public static bool TryRead(string path, out MatchSaveData data)
        {
            data = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;
            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<MatchSaveData>(json);
                if (data == null || data.formatVersion < 1)
                    return false;
                data.units ??= Array.Empty<UnitSave>();
                data.buildings ??= Array.Empty<BuildingSave>();
                data.territories ??= Array.Empty<TerritorySave>();
                data.resources ??= Array.Empty<ResourceSave>();
                data.destructibles ??= Array.Empty<DestructibleSave>();
                data.wallets ??= Array.Empty<WalletSave>();
                data.unlockedUpgrades ??= Array.Empty<string>();
                data.unlockedPowers ??= Array.Empty<string>();
                data.abilities ??= Array.Empty<AbilitySave>();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Asterra] Failed to read save: " + e.Message);
                data = null;
                return false;
            }
        }
    }
}
