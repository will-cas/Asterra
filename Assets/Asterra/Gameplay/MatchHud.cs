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
            string envLine = string.Empty;
            if (match.World is SkirmishWorldSim worldSim)
            {
                var tod = worldSim.Environment.TimeOfDaySim;
                var weather = worldSim.Environment.WeatherSim.Current;
                envLine = $"  {tod.Phase}  {weather.Kind}";
            }

            string status =
                $"Units {(match.World != null ? match.World.Units.Count : 0)}  " +
                $"Territory {territory}  Hold {hold:0}%  Tick {match.Clock?.CurrentTick.Value}{envLine}";

            GUI.Label(new Rect(12f, 12f, 900f, 22f), resources);
            GUI.Label(new Rect(12f, 34f, 1000f, 22f), status);
            GUI.Label(new Rect(12f, 56f, 1200f, 22f), BuildContextLine());
            GUI.Label(new Rect(12f, 78f, 1400f, 22f), BuildHotkeyHint());

            if (orders != null && orders.CanUseCommanderAbility)
                DrawCommanderAbilityButton();

            if (MatchFeedback.Instance != null && MatchFeedback.Instance.HasActiveMessage)
            {
                string toast = MatchFeedback.Instance.CurrentMessage;
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 16,
                };
                GUI.Label(new Rect(0f, 10f, Screen.width, 28f), toast, style);
            }

            float panelY = Screen.height - 156f;
            float x = 12f;
            const float btnW = 110f;
            const float btnH = 28f;
            const float gap = 5f;

            bool hasBuilder = orders != null && (orders.HasBuilderSelected || orders.IsPlaceMode);
            bool hasCombat = orders != null && orders.HasCombatUnitSelected;
            bool showIdle = orders != null;

            if (showIdle)
            {
                if (GUI.Button(new Rect(x, panelY, 100f, btnH), $"Idle ({orders.IdleWorkerCount})"))
                    orders.SelectIdleWorker();
                x += 100f + gap;
            }

            if (hasCombat)
            {
                if (GUI.Button(new Rect(x, panelY, 70f, btnH), "Stop"))
                    orders.StopSelected();
                x += 70f + gap;
                if (GUI.Button(new Rect(x, panelY, 70f, btnH), "Aggro"))
                    orders.SetSelectedStance(UnitStance.Aggressive);
                x += 70f + gap;
                if (GUI.Button(new Rect(x, panelY, 80f, btnH), "Defend"))
                    orders.SetSelectedStance(UnitStance.Defensive);
                x += 80f + gap;
                if (GUI.Button(new Rect(x, panelY, 70f, btnH), "Hold"))
                    orders.SetSelectedStance(UnitStance.Hold);
                x += 70f + gap;
            }

            float buildY = panelY + 34f;
            float bx = 12f;
            if (orders != null && orders.IsPlaceMode)
            {
                if (GUI.Button(new Rect(bx, buildY, 160f, btnH), "Cancel Build (Esc)"))
                    orders.CancelPlaceMode();
                bx += 160f + gap;
            }
            else if (hasBuilder && match.PlayerRoster != null)
            {
                var roster = match.PlayerRoster;
                if (GUI.Button(new Rect(bx, buildY, 120f, btnH), "Barracks (B)"))
                    orders.EnterPlaceMode(roster.ProducerBuildingId);
                bx += 120f + gap;
                if (GUI.Button(new Rect(bx, buildY, 110f, btnH), "Tower (N)"))
                    orders.EnterPlaceMode(roster.TowerBuildingId);
                bx += 110f + gap;
                if (GUI.Button(new Rect(bx, buildY, 100f, btnH), "Wall (M)"))
                    orders.EnterPlaceMode(roster.WallBuildingId);
                bx += 100f + gap;
                if (GUI.Button(new Rect(bx, buildY, 120f, btnH), "Outpost (O)"))
                    orders.EnterPlaceMode(roster.OutpostBuildingId);
                bx += 120f + gap;
            }

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
                    bool producing = !string.IsNullOrEmpty(b.ProductionUnitDefId) || b.QueueCount > 0;
                    if (producing)
                        DrawProductionQueue(b, panelY - 70f);

                    if (b.AllowsGarrison && b.GarrisonCount > 0
                        && GUI.Button(new Rect(12f, panelY - 100f, 160f, 28f), $"Unload ({b.GarrisonCount})"))
                    {
                        if (match.Commands != null)
                        {
                            match.Commands.SubmitLocal(new ExitGarrisonCommand
                            {
                                Issuer = player,
                                BuildingId = b.Id,
                            });
                        }
                    }

                    float trainX = bx;
                    float trainY = buildY;
                    bool isKeep = FactionDefaultContent.IsKeepBuildingId(b.DefinitionId);
                    bool canTrain = isKeep || b.CanProduce
                                    || !string.IsNullOrEmpty(b.ProductionUnitDefId)
                                    || b.QueueCount > 0;
                    var roster = match.PlayerRoster;

                    if (canTrain)
                    {
                        if (isKeep)
                        {
                            if (GUI.Button(new Rect(trainX, trainY, btnW, btnH), "Builder"))
                                orders.TrainUnit(roster.BuilderUnitId);
                            trainX += btnW + gap;
                        }
                        else if (b.CanProduce || !string.IsNullOrEmpty(b.ProductionUnitDefId) || b.QueueCount > 0)
                        {
                            if (GUI.Button(new Rect(trainX, trainY, btnW, btnH), "Infantry"))
                                orders.TrainUnit(roster.BasicUnitId);
                            trainX += btnW + gap;
                            if (GUI.Button(new Rect(trainX, trainY, btnW, btnH), "Ranged"))
                                orders.TrainUnit(roster.RangedUnitId);
                            trainX += btnW + gap;
                            if (GUI.Button(new Rect(trainX, trainY, btnW, btnH), "Elite"))
                                orders.TrainUnit(roster.CavalryUnitId);
                            trainX += btnW + gap;
                            if (GUI.Button(new Rect(trainX, trainY, btnW, btnH), "Siege"))
                                orders.TrainUnit(roster.SiegeUnitId);
                            trainX += btnW + gap;
                        }

                        if (producing)
                        {
                            if (GUI.Button(new Rect(trainX, trainY, 120f, btnH), "Cancel (X)"))
                                orders.CancelProduction();
                            GUI.Label(new Rect(trainX + 128f, trainY + 4f, 220f, 22f), "Shift+train queues");
                        }
                        else if (canTrain)
                        {
                            GUI.Label(new Rect(trainX, trainY + 4f, 220f, 22f), "Shift+train queues");
                        }
                    }
                }
            }

            if (!match.Result.IsOver)
                return;

            bool won = match.Result.Winner == player;
            string title = won ? "VICTORY" : "DEFEAT";
            string reason = match.Result.Reason == MatchEndReason.KeepDestroyed
                ? "Enemy keep destroyed"
                : "Territory held long enough";
            string story = BuildStoryBeat(won);
            float boxH = string.IsNullOrEmpty(story) ? 90f : 150f;
            GUI.Box(
                new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.34f, 400f, boxH),
                string.IsNullOrEmpty(story)
                    ? $"{title}\n{reason}"
                    : $"{title}\n{reason}\n\n{story}");
        }

        private void DrawCommanderAbilityButton()
        {
            float cd = 0f;
            float buff = 0f;
            if (match.World != null)
                match.World.TryGetCommanderAbilityStatus(match.Session.LocalPlayer, out cd, out buff);

            string label;
            if (buff > 0.05f)
                label = $"Iron Wall ACTIVE {buff:0}s";
            else if (cd > 0.05f)
                label = $"Iron Wall {cd:0}s";
            else
                label = "Iron Wall (Q)";

            if (GUI.Button(new Rect(Screen.width - 210f, 12f, 198f, 36f), label) && cd <= 0.05f)
                orders.ActivateCommanderAbility();
        }

        private string BuildStoryBeat(bool won)
        {
            if (match == null)
                return string.Empty;

            bool blackridge = match.MapId == SkirmishMapId.BlackridgePass;
            bool aurelian = match.PlayerRoster != null
                            && match.PlayerRoster.DefinitionId == FactionDefaultContent.IronCovenantId;
            if (!blackridge)
                return string.Empty;

            if (won && aurelian)
                return "Blackridge Pass secured.\nStory unlock: The First Border War continues.";
            if (won)
                return "Blackridge Pass falls under your banner.";
            return "The pass is lost. The Crownlands stand exposed.";
        }

        private void DrawProductionQueue(BuildingSnapshot b, float y)
        {
            GUI.Label(new Rect(12f, y - 22f, 500f, 20f), "Production queue (click to jump):");
            float qx = 12f;
            const float qw = 72f;
            const float qh = 40f;
            const float gap = 4f;

            if (!string.IsNullOrEmpty(b.ProductionUnitDefId))
            {
                string label = $"{ShortName(b.ProductionUnitDefId)}\n{(int)(b.ProductionProgress * 100f)}%";
                if (GUI.Button(new Rect(qx, y, qw, qh), label))
                    orders.JumpToBuilding(b.Id);
                qx += qw + gap;
            }

            DrawQueueSlot(b.QueuedUnitDefId, ref qx, y, qw, qh, gap, b.Id);
            DrawQueueSlot(b.Queue1DefId, ref qx, y, qw, qh, gap, b.Id);
            DrawQueueSlot(b.Queue2DefId, ref qx, y, qw, qh, gap, b.Id);
            DrawQueueSlot(b.Queue3DefId, ref qx, y, qw, qh, gap, b.Id);

            if (string.IsNullOrEmpty(b.ProductionUnitDefId) && b.QueueCount <= 0)
                GUI.Label(new Rect(12f, y + 8f, 200f, 22f), "Idle");
        }

        private void DrawQueueSlot(string defId, ref float qx, float y, float qw, float qh, float gap, SimEntityId buildingId)
        {
            if (string.IsNullOrEmpty(defId))
                return;
            if (GUI.Button(new Rect(qx, y, qw, qh), ShortName(defId)))
                orders.JumpToBuilding(buildingId);
            qx += qw + gap;
        }

        private string BuildContextLine()
        {
            if (orders == null || match.World == null)
                return "No selection";

            if (orders.IsPlaceMode)
                return "Place mode — LMB place, Esc/RMB cancel, Shift keeps placing";

            if (orders.IsAttackMoveArmed)
                return "Attack-move armed — click ground";

            if (orders.IsPatrolArmed)
                return "Patrol armed — click ground";

            if (orders.SelectedBuilding.HasValue)
            {
                for (int i = 0; i < match.World.Buildings.Count; i++)
                {
                    var b = match.World.Buildings[i];
                    if (b.Id != orders.SelectedBuilding.Value)
                        continue;
                    string name = ShortName(b.DefinitionId);
                    if (FactionDefaultContent.IsKeepBuildingId(b.DefinitionId))
                        return $"Keep selected ({name}) — train builders";
                    if (b.CanProduce || !string.IsNullOrEmpty(b.ProductionUnitDefId) || b.QueueCount > 0)
                        return $"Producer selected ({name}) — train combat units";
                    return $"Building selected ({name})";
                }

                return "Building selected";
            }

            if (orders.Selection == null || orders.Selection.Selected.Count == 0)
                return "No selection — click units or buildings";

            int total = orders.Selection.Selected.Count;
            int builders = 0;
            int combat = 0;
            UnitStance? stance = null;
            bool mixedStance = false;
            var local = match.Session.LocalPlayer;
            for (int i = 0; i < orders.Selection.Selected.Count; i++)
            {
                var id = orders.Selection.Selected[i];
                for (int u = 0; u < match.World.Units.Count; u++)
                {
                    var unit = match.World.Units[u];
                    if (unit.Id != id || !unit.IsAlive || unit.Owner != local)
                        continue;
                    if (FactionDefaultContent.IsBuilderUnitId(unit.DefinitionId))
                        builders++;
                    else
                        combat++;
                    if (!stance.HasValue)
                        stance = unit.Stance;
                    else if (stance.Value != unit.Stance)
                        mixedStance = true;
                    break;
                }
            }

            string stanceLabel = mixedStance ? "mixed" : (stance.HasValue ? stance.Value.ToString() : "-");
            if (builders > 0 && combat == 0)
                return builders == 1
                    ? "Builder selected — B/N/M/O build, RMB gather"
                    : $"{builders} builders selected — B/N/M/O build, RMB gather";
            if (combat > 0 && builders == 0)
                return $"{combat} combat unit{(combat == 1 ? "" : "s")} selected — S stop, P patrol, A attack-move  stance {stanceLabel}";
            if (builders > 0 && combat > 0)
                return $"{total} units ({builders} builders, {combat} combat) — build + combat orders  stance {stanceLabel}";
            return $"{total} units selected";
        }

        private string BuildHotkeyHint()
        {
            if (orders == null)
                return string.Empty;
            if (orders.IsPlaceMode)
                return "LMB place  Esc/RMB cancel  Shift keep placing";
            if (orders.HasBuilderSelected)
                return "B barracks  N tower  M wall  O outpost  . idle workers";
            if (orders.CanUseCommanderAbility)
            {
                if (orders.HasCombatUnitSelected)
                    return "S stop  P patrol  A attack-move  Q Iron Wall  F/G/H stance  Ctrl+1-9 groups";
                return "Q Iron Wall (Lucien Vale)  . idle workers  Ctrl+1-9 groups  R reselect all";
            }
            if (orders.HasCombatUnitSelected)
                return "S stop  P patrol  A attack-move  F/G/H stance  Ctrl+1-9 groups";
            if (orders.SelectedBuilding.HasValue)
                return "Train buttons / T  X cancel production  RMB set rally";
            return ". idle workers  Ctrl+1-9 groups  R reselect all";
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
