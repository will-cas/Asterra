using Asterra.Core;
using Asterra.Gameplay;
using UnityEngine;

namespace Asterra.UI
{
    /// <summary>Runtime HUD (OnGUI) for resources, hold progress, and match result.</summary>
    public sealed class SkirmishHud : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private string lastResourceLine;
        [SerializeField] private string lastStatusLine;

        public string LastResourceLine => lastResourceLine;
        public string LastStatusLine => lastStatusLine;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
        }

        private void Update()
        {
            if (match == null || match.Wallet == null || match.Session == null)
                return;

            var player = match.Session.LocalPlayer;
            int gold = match.Wallet.Get(player, ResourceType.Gold);
            int timber = match.Wallet.Get(player, ResourceType.Timber);
            int mana = match.Wallet.Get(player, ResourceType.Mana);
            lastResourceLine = $"Gold {gold}  Timber {timber}  Mana {mana}";

            int units = match.World != null ? match.World.Units.Count : 0;
            string territory = "n/a";
            if (match.World != null && match.World.Territories.Count > 0)
            {
                var t = match.World.Territories[0];
                territory = t.HasController ? t.Controller.ToString() : t.State.ToString();
            }

            float hold = match.Victory != null ? match.Victory.GetHoldProgress(player) : 0f;
            lastStatusLine =
                $"Units {units}  Territory {territory}  Hold {(hold * 100f):0}%  Tick {match.Clock?.CurrentTick.Value}";
        }

        private void OnGUI()
        {
            // MatchHud owns in-match chrome; keep this component for status strings only.
            if (FindFirstObjectByType<MatchHud>() != null)
                return;

            if (match == null)
                return;

            const float pad = 12f;
            GUI.Label(new Rect(pad, pad, 800f, 24f), lastResourceLine ?? string.Empty);
            GUI.Label(new Rect(pad, pad + 22f, 900f, 24f), lastStatusLine ?? string.Empty);
            GUI.Label(
                new Rect(pad, pad + 44f, 900f, 24f),
                "LMB select  |  RMB move/attack  |  B build  |  T train  |  C capture  |  U upgrade");

            if (match.Result.IsOver)
            {
                bool won = match.Session != null && match.Result.Winner == match.Session.LocalPlayer;
                string title = won ? "VICTORY" : "DEFEAT";
                string reason = match.Result.Reason == MatchEndReason.KeepDestroyed
                    ? "Keep destroyed"
                    : "Territory held";
                var area = new Rect(Screen.width * 0.5f - 160f, Screen.height * 0.4f, 320f, 80f);
                GUI.Box(area, $"{title}\n{reason}");
            }
        }

        public void SetResources(int gold, int timber, int mana)
        {
            lastResourceLine = $"Gold {gold}  Timber {timber}  Mana {mana}";
        }
    }
}
