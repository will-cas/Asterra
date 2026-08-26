using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Local skirmish career stats (PlayerPrefs — no account backend).</summary>
    public static class AsterraLocalProfile
    {
        private const string PlayedKey = "asterra.stats.played";
        private const string WinsKey = "asterra.stats.wins";
        private const string LossesKey = "asterra.stats.losses";
        private const string LastFactionKey = "asterra.stats.lastFaction";
        private const string DisplayNameKey = "asterra.profile.name";

        public static int MatchesPlayed => PlayerPrefs.GetInt(PlayedKey, 0);
        public static int Wins => PlayerPrefs.GetInt(WinsKey, 0);
        public static int Losses => PlayerPrefs.GetInt(LossesKey, 0);
        public static int LastFactionIndex => PlayerPrefs.GetInt(LastFactionKey, 0);

        public static string DisplayName
        {
            get
            {
                string n = PlayerPrefs.GetString(DisplayNameKey, string.Empty);
                return string.IsNullOrEmpty(n) ? "Commander" : n;
            }
            set
            {
                PlayerPrefs.SetString(DisplayNameKey, string.IsNullOrEmpty(value) ? "Commander" : value.Trim());
                PlayerPrefs.Save();
            }
        }

        public static float WinRate
        {
            get
            {
                int n = MatchesPlayed;
                if (n <= 0)
                    return 0f;
                return Wins / (float)n;
            }
        }

        public static void RecordMatchEnd(bool won, int playerFactionIndex)
        {
            PlayerPrefs.SetInt(PlayedKey, MatchesPlayed + 1);
            if (won)
                PlayerPrefs.SetInt(WinsKey, Wins + 1);
            else
                PlayerPrefs.SetInt(LossesKey, Losses + 1);
            PlayerPrefs.SetInt(LastFactionKey, Mathf.Clamp(playerFactionIndex, 0, 8));
            PlayerPrefs.Save();
        }

        public static void ResetStats()
        {
            PlayerPrefs.DeleteKey(PlayedKey);
            PlayerPrefs.DeleteKey(WinsKey);
            PlayerPrefs.DeleteKey(LossesKey);
            PlayerPrefs.DeleteKey(LastFactionKey);
            PlayerPrefs.Save();
        }
    }
}
