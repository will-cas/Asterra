using Asterra.AI;
using Asterra.Gameplay.Content;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Local campaign save (PlayerPrefs). Mission index is the next mission to play.</summary>
    public static class CampaignProgress
    {
        private const string MissionKey = "asterra.campaign.nextMission";
        private const string FactionKey = "asterra.campaign.faction";
        private const string DifficultyKey = "asterra.campaign.difficulty";
        private const string StartedKey = "asterra.campaign.started";
        private const string CompleteKey = "asterra.campaign.complete";
        private const string MercifulKey = "asterra.campaign.secret.merciful";
        private const string HiddenUnlockedKey = "asterra.campaign.secret.hidden";
        private const string SecretEndingKey = "asterra.campaign.secret.ending";

        public static bool HasSave => PlayerPrefs.GetInt(StartedKey, 0) != 0;

        public static bool IsComplete => PlayerPrefs.GetInt(CompleteKey, 0) != 0;

        public static bool Merciful => PlayerPrefs.GetInt(MercifulKey, 0) != 0;

        public static bool HiddenMissionUnlocked => PlayerPrefs.GetInt(HiddenUnlockedKey, 0) != 0;

        public static bool SecretEnding => PlayerPrefs.GetInt(SecretEndingKey, 0) != 0;

        public static int NextMissionIndex =>
            Mathf.Clamp(PlayerPrefs.GetInt(MissionKey, 0), 0, CampaignCatalog.MissionCount);

        public static int FactionIndex => CampaignCatalog.PlayerFactionIndex;

        public static AiDifficulty Difficulty =>
            CampaignCatalog.ClampDifficulty((AiDifficulty)PlayerPrefs.GetInt(DifficultyKey, (int)AiDifficulty.Normal));

        public static void StartNew(int factionIndex, AiDifficulty difficulty)
        {
            PlayerPrefs.SetInt(StartedKey, 1);
            PlayerPrefs.SetInt(CompleteKey, 0);
            PlayerPrefs.SetInt(MissionKey, 0);
            PlayerPrefs.SetInt(FactionKey, CampaignCatalog.PlayerFactionIndex);
            PlayerPrefs.SetInt(DifficultyKey, (int)CampaignCatalog.ClampDifficulty(difficulty));
            PlayerPrefs.DeleteKey(MercifulKey);
            PlayerPrefs.DeleteKey(HiddenUnlockedKey);
            PlayerPrefs.DeleteKey(SecretEndingKey);
            PlayerPrefs.Save();
        }

        public static void SetLobbyPicks(int factionIndex, AiDifficulty difficulty)
        {
            PlayerPrefs.SetInt(FactionKey, CampaignCatalog.PlayerFactionIndex);
            PlayerPrefs.SetInt(DifficultyKey, (int)CampaignCatalog.ClampDifficulty(difficulty));
            PlayerPrefs.Save();
        }

        public static void MarkMerciful()
        {
            PlayerPrefs.SetInt(MercifulKey, 1);
            PlayerPrefs.SetInt(HiddenUnlockedKey, 1);
            PlayerPrefs.Save();
        }

        public static void MarkSecretEnding()
        {
            PlayerPrefs.SetInt(SecretEndingKey, 1);
            PlayerPrefs.Save();
        }

        public static void OnMissionWon(int playedIndex)
        {
            if (!HasSave)
                StartNew(FactionIndex, Difficulty);

            int next = playedIndex + 1;
            if (playedIndex == CampaignCatalog.SecretMissionIndex)
            {
                MarkSecretEnding();
                PlayerPrefs.SetInt(CompleteKey, 1);
            }
            else if (next >= CampaignCatalog.MissionCount)
            {
                PlayerPrefs.SetInt(MissionKey, CampaignCatalog.MissionCount);
                PlayerPrefs.SetInt(CompleteKey, 1);
            }
            else
            {
                PlayerPrefs.SetInt(MissionKey, next);
                PlayerPrefs.SetInt(CompleteKey, 0);
            }

            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(MissionKey);
            PlayerPrefs.DeleteKey(FactionKey);
            PlayerPrefs.DeleteKey(DifficultyKey);
            PlayerPrefs.DeleteKey(StartedKey);
            PlayerPrefs.DeleteKey(CompleteKey);
            PlayerPrefs.DeleteKey(MercifulKey);
            PlayerPrefs.DeleteKey(HiddenUnlockedKey);
            PlayerPrefs.DeleteKey(SecretEndingKey);
            PlayerPrefs.Save();
        }
    }
}
