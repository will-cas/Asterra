using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Built-in OnGUI HUD so the demo needs no UI canvas setup.</summary>
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;

        private void Awake()
        {
            if (match == null)
                match = GetComponent<MatchBootstrap>();
        }

        private void OnGUI()
        {
            if (match == null || match.Session == null)
                return;

            var player = match.Session.LocalPlayer;
            string resources = string.Empty;
            if (match.Wallet != null)
            {
                resources =
                    $"Gold {match.Wallet.Get(player, ResourceType.Gold)}  " +
                    $"Timber {match.Wallet.Get(player, ResourceType.Timber)}";
            }

            string territory = "n/a";
            if (match.World != null && match.World.Territories.Count > 0)
            {
                var t = match.World.Territories[0];
                territory = t.HasController ? t.Controller.ToString() : t.State.ToString();
            }

            float hold = match.Victory != null ? match.Victory.GetHoldProgress(player) * 100f : 0f;
            string status =
                $"Units {(match.World != null ? match.World.Units.Count : 0)}  " +
                $"Territory {territory}  Hold {hold:0}%  Tick {match.Clock?.CurrentTick.Value}";

            GUI.Label(new Rect(12f, 12f, 900f, 22f), resources);
            GUI.Label(new Rect(12f, 34f, 1000f, 22f), status);
            GUI.Label(
                new Rect(12f, 56f, 1100f, 22f),
                "LMB select  |  RMB move/attack  |  R select all  |  B build  |  T train  |  C capture  |  fog hides unseen enemies");

            if (!match.Result.IsOver)
                return;

            bool won = match.Result.Winner == player;
            string title = won ? "VICTORY" : "DEFEAT";
            string reason = match.Result.Reason == MatchEndReason.KeepDestroyed
                ? "Enemy keep destroyed"
                : "Territory held long enough";
            GUI.Box(new Rect(Screen.width * 0.5f - 160f, Screen.height * 0.38f, 320f, 90f), $"{title}\n{reason}");
        }
    }
}
