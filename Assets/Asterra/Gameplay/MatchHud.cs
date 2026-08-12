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
                "Builder RMB resource = gather  |  B place producer  |  building RMB = rally  |  minimap click to pan");

            float panelY = Screen.height - 120f;
            float x = 12f;
            const float btnW = 118f;
            const float btnH = 32f;
            const float gap = 6f;

            if (orders != null && orders.SelectedBuilding.HasValue && match.World != null && match.PlayerRoster != null)
            {
                bool foundBuilding = false;
                BuildingSnapshot b = default;
                for (int i = 0; i < match.World.Buildings.Count; i++)
                {
                    if (match.World.Buildings[i].Id != orders.SelectedBuilding.Value)
                        continue;
                    b = match.World.Buildings[i];
                    foundBuilding = true;
                    break;
                }

                if (foundBuilding)
                {
                    float progressPct = b.ProductionProgress * 100f;
                    string prod = string.IsNullOrEmpty(b.ProductionUnitDefId)
                        ? "Idle"
                        : $"{ShortName(b.ProductionUnitDefId)} {progressPct:0}%  queue {b.QueueCount}";
                    GUI.Label(new Rect(12f, panelY - 24f, 700f, 22f), $"Production: {prod}   (RMB set rally)");

                    bool isKeep = FactionDefaultContent.IsKeepBuildingId(b.DefinitionId);
                    var roster = match.PlayerRoster;

                    if (isKeep)
                    {
                        if (GUI.Button(new Rect(x, panelY, btnW, btnH), "Train Builder"))
                            orders.TrainUnit(roster.BuilderUnitId);
                        x += btnW + gap;
                    }
                    else
                    {
                        if (GUI.Button(new Rect(x, panelY, btnW, btnH), "Train Soldier"))
                            orders.TrainUnit(roster.BasicUnitId);
                        x += btnW + gap;
                        if (GUI.Button(new Rect(x, panelY, btnW, btnH), "Train Archer"))
                            orders.TrainUnit(roster.RangedUnitId);
                        x += btnW + gap;
                        if (GUI.Button(new Rect(x, panelY, btnW, btnH), "Train Cavalry"))
                            orders.TrainUnit(roster.CavalryUnitId);
                        x += btnW + gap;
                        if (GUI.Button(new Rect(x, panelY, btnW, btnH), "Train Siege"))
                            orders.TrainUnit(roster.SiegeUnitId);
                        x += btnW + gap;
                    }

                    if (GUI.Button(new Rect(x, panelY, btnW + 20f, btnH), "Cancel Prod (X)"))
                        orders.CancelProduction();
                    x += btnW + 20f + gap;
                }
            }

            if (orders != null && !orders.IsPlaceMode)
            {
                if (GUI.Button(new Rect(x, panelY, 160f, btnH), "Build Barracks (B)"))
                    orders.EnterPlaceMode();
            }
            else if (orders != null && orders.IsPlaceMode)
            {
                if (GUI.Button(new Rect(x, panelY, 160f, btnH), "Cancel Build (Esc)"))
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

        private static string ShortName(string defId)
        {
            if (string.IsNullOrEmpty(defId))
                return "?";
            int idx = defId.LastIndexOf('_');
            return idx >= 0 && idx + 1 < defId.Length ? defId.Substring(idx + 1) : defId;
        }
    }
}
