using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Player;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Built-in OnGUI HUD so the demo needs no UI canvas setup.</summary>
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private LocalOrderController orders;

        private void Awake()
        {
            if (match == null)
                match = GetComponent<MatchBootstrap>();
            if (orders == null)
                orders = GetComponent<LocalOrderController>();
        }

        private void OnGUI()
        {
            if (match == null || match.Session == null)
                return;
            if (orders == null)
                orders = FindFirstObjectByType<LocalOrderController>();

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
                new Rect(12f, 56f, 1200f, 22f),
                "Click keep → Train Builder  |  select builder → B place barracks  |  drag-select  |  RMB move/attack");

            float panelY = Screen.height - 78f;
            if (orders != null && orders.SelectedBuilding.HasValue)
            {
                string label = "Train";
                if (match.World != null && match.PlayerRoster != null)
                {
                    for (int i = 0; i < match.World.Buildings.Count; i++)
                    {
                        var b = match.World.Buildings[i];
                        if (b.Id != orders.SelectedBuilding.Value)
                            continue;
                        label = FactionDefaultContent.IsKeepBuildingId(b.DefinitionId)
                            ? "Train Builder (T)"
                            : "Train Soldier (T)";
                        break;
                    }
                }

                if (GUI.Button(new Rect(12f, panelY, 180f, 36f), label))
                    orders.TrainFromSelectedBuilding();
            }

            if (orders != null && !orders.IsPlaceMode)
            {
                if (GUI.Button(new Rect(202f, panelY, 200f, 36f), "Build Barracks (B)"))
                    orders.EnterPlaceMode();
            }
            else if (orders != null && orders.IsPlaceMode)
            {
                if (GUI.Button(new Rect(202f, panelY, 200f, 36f), "Cancel Build (Esc)"))
                    orders.CancelPlaceMode();
            }

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
