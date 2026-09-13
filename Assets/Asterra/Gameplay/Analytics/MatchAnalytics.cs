using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Analytics
{
    /// <summary>
    /// M1 Analytics stubs for match start / win / lose.
    /// Logs locally always; optionally forwards to Unity Gaming Services Analytics when
    /// <see cref="UseLiveAnalytics"/> is enabled and the Analytics package is initialized.
    /// Does not block match flow if UGS is unavailable.
    /// </summary>
    public static class MatchAnalytics
    {
        /// <summary>When false (default), events stay as Debug stubs only.</summary>
        public static bool UseLiveAnalytics = false;

        public static void RecordMatchStart(string mapKey, MatchPlayMode mode, int playerCount, uint seed)
        {
            var payload = new Dictionary<string, object>
            {
                { "map", mapKey ?? string.Empty },
                { "mode", mode.ToString() },
                { "players", playerCount },
                { "seed", seed },
            };
            Emit("match_start", payload);
        }

        public static void RecordMatchEnd(MatchResult result, PlayerId localPlayer, string mapKey)
        {
            bool won = result.IsOver && result.Winner.Value == localPlayer.Value;
            var payload = new Dictionary<string, object>
            {
                { "map", mapKey ?? string.Empty },
                { "winner", result.Winner.Value },
                { "local_player", localPlayer.Value },
                { "won", won },
                { "reason", result.Reason.ToString() },
            };
            Emit(won ? "match_win" : "match_lose", payload);
        }

        private static void Emit(string eventName, Dictionary<string, object> parameters)
        {
            Debug.Log($"[Asterra Analytics stub] {eventName} {Format(parameters)}");
            if (!UseLiveAnalytics)
                return;

#if ASTERRA_UGS_ANALYTICS
            try
            {
                Unity.Services.Analytics.AnalyticsService.Instance.CustomData(eventName, parameters);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Asterra Analytics] live emit failed for {eventName}: {ex.Message}");
            }
#else
            Debug.LogWarning("[Asterra Analytics] UseLiveAnalytics is on but ASTERRA_UGS_ANALYTICS define is not set.");
#endif
        }

        private static string Format(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "{}";
            var parts = new List<string>(parameters.Count);
            foreach (var kv in parameters)
                parts.Add($"{kv.Key}={kv.Value}");
            return string.Join(" ", parts);
        }
    }
}
