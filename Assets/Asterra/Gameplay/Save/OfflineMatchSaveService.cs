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

            var data = CaptureSim(
                sim,
                wallet,
                matchSeed: match.MatchSeed,
                mapKey: match.MapKey,
                playerFaction: match.PlayerFactionIndex,
                enemyFaction: match.EnemyFactionIndex,
                aiDifficulty: (int)match.AiDifficulty,
                tick: match.Clock != null ? match.Clock.CurrentTick.Value : 0u,
                nextEntityId: match.Ids != null ? match.Ids.PeekNext : 1u,
                holdSecondsP0: match.Victory != null ? match.Victory.GetHoldSeconds(new PlayerId(0)) : 0f,
                holdSecondsP1: match.Victory != null ? match.Victory.GetHoldSeconds(new PlayerId(1)) : 0f);
            return data;
        }

        /// <summary>Capture sim + wallet without a live MatchBootstrap (tests / tools).</summary>
        public static MatchSaveData CaptureSim(
            SkirmishWorldSim sim,
            IResourceWallet wallet,
            uint matchSeed = 0,
            string mapKey = null,
            int playerFaction = 0,
            int enemyFaction = 1,
            int aiDifficulty = 1,
            uint tick = 0,
            uint nextEntityId = 1,
            float holdSecondsP0 = 0f,
            float holdSecondsP1 = 0f)
        {
            if (sim == null)
                throw new ArgumentNullException(nameof(sim));

            var data = new MatchSaveData
            {
                formatVersion = CurrentFormatVersion,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                matchSeed = matchSeed,
                mapKey = string.IsNullOrEmpty(mapKey) ? "blackridge_pass" : mapKey,
                playerFaction = playerFaction,
                enemyFaction = enemyFaction,
                aiDifficulty = aiDifficulty,
                tick = tick,
                nextEntityId = nextEntityId,
                holdSecondsP0 = holdSecondsP0,
                holdSecondsP1 = holdSecondsP1,
            };

            ApplyWallets(data, wallet);
            sim.CaptureInto(data);
            return data;
        }

        public static void ApplyWallets(MatchSaveData data, IResourceWallet wallet)
        {
            if (data == null || wallet == null)
                return;
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

        /// <summary>Restore wallets from save into a wallet (MatchBootstrap load path).</summary>
        public static void RestoreWallets(MatchSaveData data, IResourceWallet wallet)
        {
            if (data?.wallets == null || wallet == null)
                return;
            for (int i = 0; i < data.wallets.Length; i++)
            {
                var w = data.wallets[i];
                var p = new PlayerId(w.player);
                wallet.Seed(p, ResourceType.Gold, w.gold);
                wallet.Seed(p, ResourceType.Timber, w.timber);
            }
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
