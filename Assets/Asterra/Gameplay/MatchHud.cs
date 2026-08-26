using Asterra.Core;
using Asterra.Gameplay.Audio;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Player;
using Asterra.Gameplay.Presentation;
using Asterra.Gameplay.Sim;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Built-in OnGUI HUD. Debug chrome is off unless showDebugHud is enabled.</summary>
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private LocalOrderController orders;
        [Tooltip("Shows tick/weather/territory diagnostics. Off by default.")]
        [SerializeField] private bool showDebugHud;
        private bool _endSfxPlayed;
        private bool _statsRecorded;
        private string _hoverTip = string.Empty;
        private AsterraMenuPanels.Overlay _overlay = AsterraMenuPanels.Overlay.None;
        private AsterraMenuPanels.Overlay _overlayReturn = AsterraMenuPanels.Overlay.None;

        private void Awake()
        {
            if (match == null)
                match = GetComponent<MatchBootstrap>();
            if (orders == null)
                orders = GetComponent<LocalOrderController>();
        }

        private void Update()
        {
            if (match == null || !match.IsMatchRunning || match.Result.IsOver)
            {
                if (match != null)
                    match.IsMenuOverlayOpen = false;
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
                match.SaveOfflineQuick();
            if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
                match.LoadOfflineQuick();

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                bool armed = orders != null && orders.HasArmedMode;
                if (!armed)
                {
                    if (_overlay == AsterraMenuPanels.Overlay.None)
                        _overlay = AsterraMenuPanels.Overlay.Pause;
                    else if (_overlay == AsterraMenuPanels.Overlay.Pause)
                        _overlay = AsterraMenuPanels.Overlay.None;
                    else
                        _overlay = AsterraMenuPanels.Overlay.Pause; // Esc from Options → pause
                    match.IsMenuOverlayOpen = _overlay != AsterraMenuPanels.Overlay.None;
                    AsterraAudio.PlayUiClick();
                }
            }

            // Never keep Profile open during a match (lobby-only).
            if (_overlay == AsterraMenuPanels.Overlay.Profile)
                _overlay = AsterraMenuPanels.Overlay.Pause;

            match.IsMenuOverlayOpen = _overlay != AsterraMenuPanels.Overlay.None;
        }

        private void OnGUI()
        {
            if (match == null || match.Session == null)
                return;
            if (orders == null)
                orders = FindFirstObjectByType<LocalOrderController>();

            HudClickBlocker.BeginFrame();
            _hoverTip = string.Empty;

            var player = match.Session.LocalPlayer;
            HudStyle.Ensure();

            if (match.Result.IsOver)
            {
                RecordStatsOnce(player);
                DrawEndScreen(player);
                HudClickBlocker.PublishFrame();
                return;
            }

            if (!match.IsMatchRunning)
            {
                HudClickBlocker.PublishFrame();
                return;
            }

            DrawResources(player);
            DrawSaveLoadChrome();
            DrawSelectionStrip(player);
            DrawPowerButton();
            DrawFeedbackToast();

            if (showDebugHud)
                DrawDebugStatus(player);

            DrawCommandBar(player);
            DrawHoverTip();

            if (_overlay != AsterraMenuPanels.Overlay.None)
            {
                var prior = _overlay;
                AsterraMenuPanels.Draw(_overlay, out bool quitMenu, out var next);
                if (next == AsterraMenuPanels.Overlay.Profile)
                    next = AsterraMenuPanels.Overlay.Pause; // Profile is lobby-only
                if (prior == AsterraMenuPanels.Overlay.Pause && next == AsterraMenuPanels.Overlay.Options)
                    _overlayReturn = AsterraMenuPanels.Overlay.Pause;
                if (prior == AsterraMenuPanels.Overlay.Options
                    && next == AsterraMenuPanels.Overlay.None
                    && _overlayReturn != AsterraMenuPanels.Overlay.None)
                {
                    next = _overlayReturn;
                    _overlayReturn = AsterraMenuPanels.Overlay.None;
                }

                if (next == AsterraMenuPanels.Overlay.None || next == AsterraMenuPanels.Overlay.Pause)
                    _overlayReturn = AsterraMenuPanels.Overlay.None;

                _overlay = next;
                match.IsMenuOverlayOpen = _overlay != AsterraMenuPanels.Overlay.None;
                if (quitMenu)
                {
                    _overlay = AsterraMenuPanels.Overlay.None;
                    _overlayReturn = AsterraMenuPanels.Overlay.None;
                    match.IsMenuOverlayOpen = false;
                    match.ReturnToMainMenu();
                }
            }

            HudClickBlocker.PublishFrame();
        }

        private void RecordStatsOnce(PlayerId player)
        {
            if (_statsRecorded || match == null)
                return;
            _statsRecorded = true;
            bool won = match.Result.Winner == player;
            int faction = match.PlayerRoster != null ? match.PlayerRoster.Id.Value : match.PlayerFactionIndex;
            AsterraLocalProfile.RecordMatchEnd(won, faction);
        }

        private void DrawResources(PlayerId player)
        {
            float hold = match.Victory != null ? match.Victory.GetHoldProgress(player) : 0f;
            bool showHold = hold > 0.001f;
            float stripH = showDebugHud ? HudStyle.S(28f) : (showHold ? HudStyle.S(72f) : HudStyle.S(52f));
            var strip = new Rect(HudStyle.S(8f), HudStyle.S(8f), HudStyle.S(420f), stripH);
            HudClickBlocker.Block(strip);
            HudStyle.DrawPanel(strip, new Color(0.05f, 0.07f, 0.08f, 0.82f));

            string resources = string.Empty;
            if (match.Wallet != null)
            {
                int income = EstimateGoldIncomePerSecond(player);
                resources =
                    $"Gold {match.Wallet.Get(player, ResourceType.Gold)}" +
                    (income > 0 ? $" (+{income}/s)" : string.Empty) +
                    $"    Timber {match.Wallet.Get(player, ResourceType.Timber)}";
            }

            GUI.Label(
                new Rect(HudStyle.S(18f), HudStyle.S(12f), HudStyle.S(400f), HudStyle.S(22f)),
                resources,
                HudStyle.Label);
            if (!showDebugHud)
            {
                GUI.Label(
                    new Rect(HudStyle.S(18f), HudStyle.S(32f), HudStyle.S(400f), HudStyle.S(18f)),
                    BuildContextLine(),
                    HudStyle.Label);
                if (showHold)
                {
                    float barW = HudStyle.S(200f);
                    HudStyle.DrawPanel(
                        new Rect(HudStyle.S(18f), HudStyle.S(52f), barW, HudStyle.S(10f)),
                        new Color(0.12f, 0.12f, 0.14f, 0.95f));
                    HudStyle.DrawPanel(
                        new Rect(HudStyle.S(18f), HudStyle.S(52f), barW * Mathf.Clamp01(hold), HudStyle.S(10f)),
                        new Color(0.95f, 0.75f, 0.25f, 0.95f));
                    GUI.Label(
                        new Rect(HudStyle.S(18f) + barW + HudStyle.S(8f), HudStyle.S(48f), HudStyle.S(160f), HudStyle.S(18f)),
                        $"Hold {(int)(hold * 100f)}%",
                        HudStyle.Caption);
                }
            }
        }

        private void DrawSaveLoadChrome()
        {
            if (match == null || !match.IsMatchRunning || match.Result.IsOver)
                return;

            // Sit under the resource strip (left) so commander powers own the top-right.
            float hold = match.Victory != null ? match.Victory.GetHoldProgress(match.Session.LocalPlayer) : 0f;
            bool showHold = hold > 0.001f;
            float stripH = showDebugHud ? HudStyle.S(28f) : (showHold ? HudStyle.S(72f) : HudStyle.S(52f));
            float x = HudStyle.S(8f);
            float y = HudStyle.S(8f) + stripH + HudStyle.S(6f);
            float bw = HudStyle.S(80f);
            float bh = HudStyle.S(28f);
            if (HudButton(new Rect(x, y, bw, bh), "Save", "Save skirmish (F5)"))
                match.SaveOfflineQuick();
            if (HudButton(new Rect(x + bw + HudStyle.S(8f), y, bw, bh), "Load", "Load quicksave (F9)"))
                match.LoadOfflineQuick();
        }

        private void DrawSelectionStrip(PlayerId player)
        {
            if (orders == null || orders.Selection == null || match.World == null)
                return;
            var selected = orders.Selection.Selected;
            if (selected.Count == 0 && !orders.SelectedBuilding.HasValue)
                return;

            float y = Screen.height - HudStyle.S(278f);
            BuildingSnapshot selectedBuilding = default;
            bool buildingSelected = orders.SelectedBuilding.HasValue
                && TryGetSelectedBuilding(out selectedBuilding);
            float panelW = HudStyle.S(56f) + selected.Count * HudStyle.S(48f) + (buildingSelected ? HudStyle.S(56f) : 0f);
            if (buildingSelected && selectedBuilding.State == BuildingState.Constructing)
                panelW = Mathf.Max(panelW, HudStyle.S(280f));
            float maxW = Mathf.Min(HudStyle.S(520f), HudStyle.ContentRight - HudStyle.S(8f));
            var panel = new Rect(HudStyle.S(8f), y, Mathf.Min(maxW, panelW), HudStyle.S(56f));
            HudClickBlocker.Block(panel);
            HudStyle.DrawPanel(panel, new Color(0.05f, 0.07f, 0.09f, 0.88f));

            float x = 14f;
            byte faction = match.PlayerRoster != null ? match.PlayerRoster.Id.Value : (byte)0;
            Color fac = AsterraMeshLibrary.FactionColor(faction);

            if (buildingSelected)
            {
                var tex = HudStyle.Portrait(selectedBuilding.DefinitionId, fac);
                GUI.DrawTexture(new Rect(x, y + 8f, 40f, 40f), tex);
                float hpRatio = selectedBuilding.MaxHealth > 0.01f
                    ? Mathf.Clamp01(selectedBuilding.Health / selectedBuilding.MaxHealth)
                    : 1f;
                HudStyle.DrawPanel(new Rect(x, y + 46f, 40f * hpRatio, 4f), new Color(0.25f, 0.85f, 0.35f, 0.95f));

                if (selectedBuilding.State == BuildingState.Constructing)
                {
                    float pct = Mathf.Clamp01(selectedBuilding.BuildProgress);
                    HudStyle.DrawPanel(new Rect(x + 48f, y + 14f, 160f, 10f), new Color(0.12f, 0.12f, 0.14f, 0.95f));
                    HudStyle.DrawPanel(
                        new Rect(x + 48f, y + 14f, 160f * pct, 10f),
                        new Color(0.95f, 0.72f, 0.22f, 0.95f));
                    GUI.Label(
                        new Rect(x + 48f, y + 28f, 200f, 20f),
                        $"Building  {(int)(pct * 100f)}%",
                        HudStyle.Label);
                    x += 220f;
                }
                else
                {
                    x += 48f;
                }
            }

            int shown = 0;
            for (int i = 0; i < selected.Count && shown < 8; i++)
            {
                string defId = null;
                float hp = 1f, max = 1f;
                UnitStance stance = UnitStance.Aggressive;
                for (int u = 0; u < match.World.Units.Count; u++)
                {
                    var unit = match.World.Units[u];
                    if (unit.Id.Value != selected[i].Value)
                        continue;
                    defId = unit.DefinitionId;
                    hp = unit.Health;
                    max = unit.MaxHealth;
                    stance = unit.Stance;
                    break;
                }
                if (defId == null)
                    continue;
                var tex = HudStyle.Portrait(defId, fac);
                GUI.DrawTexture(new Rect(x, y + 8f, 40f, 40f), tex);
                float ratio = max > 0.01f ? Mathf.Clamp01(hp / max) : 1f;
                HudStyle.DrawPanel(new Rect(x, y + 46f, 40f * ratio, 4f), new Color(0.25f, 0.85f, 0.35f, 0.95f));
                string stanceLetter = stance == UnitStance.Defensive ? "D"
                    : stance == UnitStance.Hold ? "H"
                    : stance == UnitStance.Passive ? "P"
                    : "A";
                Color stanceColor = stance == UnitStance.Hold ? new Color(0.95f, 0.55f, 0.2f)
                    : stance == UnitStance.Defensive ? new Color(0.35f, 0.65f, 1f)
                    : new Color(0.9f, 0.35f, 0.3f);
                HudStyle.DrawPanel(new Rect(x + 28f, y + 8f, 12f, 14f), new Color(0.05f, 0.06f, 0.07f, 0.9f));
                var prev = GUI.color;
                GUI.color = stanceColor;
                GUI.Label(new Rect(x + 28f, y + 6f, 14f, 16f), stanceLetter, HudStyle.Caption);
                GUI.color = prev;
                x += 48f;
                shown++;
            }

            if (selected.Count > 8)
                GUI.Label(new Rect(x, y + 18f, 80f, 20f), $"+{selected.Count - 8}", HudStyle.Label);
        }

        private void DrawDebugStatus(PlayerId player)
        {
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

            GUI.Label(new Rect(12f, 32f, 1100f, 20f),
                $"Units {(match.World != null ? match.World.Units.Count : 0)}  Territory {territory}  Hold {hold:0}%  Tick {match.Clock?.CurrentTick.Value}{envLine}");
            GUI.Label(new Rect(12f, 52f, 1100f, 20f), BuildContextLine());
            GUI.Label(new Rect(12f, 72f, 1200f, 20f), BuildHotkeyHint());
        }

        private void DrawFeedbackToast()
        {
            if (MatchFeedback.Instance == null || !MatchFeedback.Instance.HasActiveMessage)
                return;
            float w = Mathf.Min(520f, Screen.width * 0.55f);
            var rect = new Rect((Screen.width - w) * 0.5f, 64f, w, 34f);
            HudClickBlocker.Block(rect);
            HudStyle.DrawPanel(rect, new Color(0.08f, 0.1f, 0.07f, 0.92f));
            var style = new GUIStyle(HudStyle.Label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 15,
            };
            GUI.Label(rect, MatchFeedback.Instance.CurrentMessage, style);
        }

        private void DrawPowerButton()
        {
            if (orders == null || match.PlayerRoster == null || match.World == null)
                return;

            var roster = match.PlayerRoster;
            var powerIds = roster.PowerIds;
            if (powerIds == null || powerIds.Length == 0)
                return;

            float y = HudStyle.S(10f);
            for (int i = 0; i < powerIds.Length; i++)
            {
                string powerId = powerIds[i];
                if (match.Definitions == null || !match.Definitions.TryGetPower(powerId, out var powerDef))
                    continue;

                match.World.TryGetCommanderAbilityStatus(match.Session.LocalPlayer, powerId, out float cd, out float buff);
                bool unlocked = match.World.HasPower(match.Session.LocalPlayer, powerId);

                string label;
                bool clickUnlock = false;
                bool clickUse = false;
                bool faded = false;
                if (!unlocked)
                {
                    label = $"Unlock {powerDef.DisplayName} ({powerDef.UnlockGoldCost}g)";
                    clickUnlock = true;
                }
                else if (powerDef.IsPassive)
                {
                    label = $"{powerDef.DisplayName} (passive)";
                    faded = true;
                }
                else if (buff > 0.05f)
                {
                    label = $"{powerDef.DisplayName} ACTIVE {buff:0}s";
                    faded = true;
                }
                else if (cd > 0.05f)
                {
                    label = $"{powerDef.DisplayName} {cd:0}s";
                    faded = true;
                }
                else
                {
                    bool isHotkey = IsPrimaryActivePower(roster.PowerIds, i, powerId);
                    label = isHotkey ? $"{powerDef.DisplayName} (Q)" : powerDef.DisplayName;
                    clickUse = true;
                }

                var rect = new Rect(Screen.width - HudStyle.S(240f), y, HudStyle.S(228f), HudStyle.S(30f));
                string tip = DescribePower(powerDef, unlocked, buff, cd);
                if (faded)
                {
                    HudFadedLabel(rect, label, tip);
                }
                else if (HudButton(rect, label, tip))
                {
                    if (clickUnlock)
                        orders.UnlockPower(powerId);
                    else if (clickUse)
                        orders.ActivateCommanderAbility(powerId);
                }

                y += HudStyle.S(34f);
            }
        }

        private void DrawCommandBar(PlayerId player)
        {
            float panelY = Screen.height - HudStyle.S(210f);
            float x = HudStyle.S(12f);
            float btnW = HudStyle.S(118f);
            float btnH = HudStyle.S(30f);
            float gap = HudStyle.S(6f);
            float maxX = HudStyle.ContentRight - HudStyle.S(4f);

            // Only block real controls — do not blanket the bottom third of the screen
            // (that swallowed world selection).
            if (orders != null)
            {
                float idleW = HudStyle.S(100f);
                if (HudButton(new Rect(x, panelY, idleW, btnH), $"Idle ({orders.IdleWorkerCount})"))
                    orders.SelectIdleWorker();
                x += idleW + gap;
            }

            bool hasCombat = orders != null && orders.HasCombatUnitSelected;
            if (hasCombat)
            {
                x = DrawCmdChip(x, panelY, HudStyle.S(64f), btnH, gap, maxX, "Stop", () => orders.StopSelected());
                x = DrawCmdChip(x, panelY, HudStyle.S(64f), btnH, gap, maxX, "Aggro",
                    () => orders.SetSelectedStance(UnitStance.Aggressive));
                x = DrawCmdChip(x, panelY, HudStyle.S(72f), btnH, gap, maxX, "Defend",
                    () => orders.SetSelectedStance(UnitStance.Defensive));
                x = DrawCmdChip(x, panelY, HudStyle.S(64f), btnH, gap, maxX, "Hold",
                    () => orders.SetSelectedStance(UnitStance.Hold));

                if (match.World != null && match.PlayerRoster != null)
                {
                    var equip = match.PlayerRoster.EquipmentUpgradeIds;
                    if (equip != null)
                    {
                        for (int i = 0; i < equip.Length; i++)
                        {
                            string upId = equip[i];
                            if (!match.World.HasUpgrade(player, upId))
                                continue;
                            if (match.Definitions == null || !match.Definitions.TryGetUpgrade(upId, out var up))
                                continue;
                            int equipCost = up.ResolvedEquipGoldCost;
                            string tip = DescribeUpgrade(up)
                                + $" — equip selected units for {equipCost}g each (research unlocks only)";
                            float ew = HudStyle.S(160f);
                            if (x + ew > maxX)
                            {
                                panelY += btnH + gap;
                                x = HudStyle.S(12f);
                            }

                            if (SelectionAllHaveEquipment(upId))
                            {
                                HudFadedLabel(new Rect(x, panelY, ew, btnH),
                                    $"Equip {ShortName(up.DisplayName)} ✓",
                                    tip + " — already equipped");
                            }
                            else if (HudButton(new Rect(x, panelY, ew, btnH),
                                         $"Equip {ShortName(up.DisplayName)} ({equipCost}g)", tip))
                            {
                                orders.ApplyUpgradeToSelected(upId);
                            }

                            x += ew + gap;
                        }
                    }
                }
            }

            float buildY = panelY + HudStyle.S(36f);
            float bx = HudStyle.S(12f);
            if (orders != null && orders.IsPlaceMode)
            {
                if (HudButton(new Rect(bx, buildY, HudStyle.S(160f), btnH), "Cancel Build (Esc)"))
                    orders.CancelPlaceMode();
            }
            else if (orders != null && orders.HasBuilderSelected && match.PlayerRoster != null)
            {
                var roster = match.PlayerRoster;
                bx = DrawPricedBuildingButton(bx, buildY, btnH, gap, "Barracks (B)", roster.ProducerBuildingId);
                bx = DrawPricedBuildingButton(bx, buildY, btnH, gap, "Tower (N)", roster.TowerBuildingId);
                bx = DrawPricedBuildingButton(bx, buildY, btnH, gap, "Wall (M)", roster.WallBuildingId);
                bx = DrawPricedBuildingButton(bx, buildY, btnH, gap, "Gold Mine (O)", roster.OutpostBuildingId);
                bx = DrawPricedBuildingButton(bx, buildY, btnH, gap, "Bridge (V)", FactionDefaultContent.BridgeId);
                DrawPricedBuildingButton(bx, buildY, btnH, gap, "Trench (J)", FactionDefaultContent.TrenchWorksId);
            }

            if (orders == null || !orders.SelectedBuilding.HasValue || match.World == null || match.PlayerRoster == null)
                return;

            if (!TryGetSelectedBuilding(out var b))
                return;

            if (b.State == BuildingState.Constructing)
            {
                DrawConstructionStatus(b, panelY - HudStyle.S(88f));
                return;
            }

            bool producing = !string.IsNullOrEmpty(b.ProductionUnitDefId) || b.QueueCount > 0;
            if (producing)
                DrawProductionQueue(b, panelY - HudStyle.S(72f));

            if (b.AllowsGarrison && b.GarrisonCount > 0
                && HudButton(new Rect(HudStyle.S(12f), panelY - HudStyle.S(104f), HudStyle.S(160f), HudStyle.S(28f)),
                    $"Unload ({b.GarrisonCount})"))
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

            if (b.Owner == player
                && b.State != BuildingState.Destroyed
                && !FactionDefaultContent.IsKeepBuildingId(b.DefinitionId)
                && HudButton(new Rect(HudStyle.S(180f), panelY - HudStyle.S(104f), HudStyle.S(120f), HudStyle.S(28f)),
                    "Demolish",
                    "Tear down this building (half refund if complete). Bridges also collapse their crossing."))
            {
                orders.DemolishSelectedBuilding();
            }

            float trainX = HudStyle.S(12f);
            float trainY = buildY;
            bool isKeep = FactionDefaultContent.IsKeepBuildingId(b.DefinitionId);
            bool canTrain = isKeep || b.CanProduce
                            || !string.IsNullOrEmpty(b.ProductionUnitDefId)
                            || b.QueueCount > 0;
            var r = match.PlayerRoster;

            if (!canTrain)
                return;

            if (isKeep)
            {
                trainX = DrawPricedTrainButton(trainX, trainY, btnW, btnH, gap, "Builder", r.BuilderUnitId);
                trainX = DrawPricedTrainButton(trainX, trainY, btnW + HudStyle.S(20f), btnH, gap, "Leader", r.LeaderUnitId);

                // Keep research (keep techs + equipment unlocks)
                float researchY = trainY + btnH + HudStyle.S(6f);
                float rx = HudStyle.S(12f);
                var keepUps = r.KeepUpgradeIds != null && r.KeepUpgradeIds.Length > 0
                    ? r.KeepUpgradeIds
                    : r.UpgradeIds;
                if (keepUps != null)
                {
                    for (int i = 0; i < keepUps.Length; i++)
                    {
                        string upId = keepUps[i];
                        if (match.Definitions == null || !match.Definitions.TryGetUpgrade(upId, out var up))
                            continue;
                        if (up.Kind == UpgradeKind.Equipment)
                            continue;
                        rx = DrawResearchButton(rx, researchY, HudStyle.S(150f), btnH, player, b, upId, up);
                    }
                }

                if (r.EquipmentUpgradeIds != null)
                {
                    for (int i = 0; i < r.EquipmentUpgradeIds.Length; i++)
                    {
                        string upId = r.EquipmentUpgradeIds[i];
                        if (match.Definitions == null || !match.Definitions.TryGetUpgrade(upId, out var up))
                            continue;
                        rx = DrawResearchButton(rx, researchY, HudStyle.S(150f), btnH, player, b, upId, up);
                    }
                }

                // Attachment slots
                if (b.AttachmentSlotCount > 0 && match.Definitions != null)
                {
                    float attachY = researchY + btnH + HudStyle.S(6f);
                    float ax = HudStyle.S(12f);
                    string attachDef = FactionDefaultContent.KeepTurretId;
                    if (!match.Definitions.TryGetBuilding(attachDef, out _)
                        && match.Definitions.TryGetBuilding(r.TowerBuildingId, out _))
                        attachDef = r.TowerBuildingId;

                    for (byte slot = 0; slot < b.AttachmentSlotCount; slot++)
                    {
                        bool occupied = (b.AttachmentOccupiedMask & (1 << slot)) != 0;
                        string slotName = slot == 0 ? "N" : slot == 1 ? "E" : slot == 2 ? "S" : "W";
                        if (occupied)
                        {
                            HudFadedLabel(new Rect(ax, attachY, HudStyle.S(100f), btnH), $"Turret {slotName} ✓",
                                "Attachment slot occupied");
                            ax += HudStyle.S(106f);
                            continue;
                        }

                        string cost = string.Empty;
                        if (match.Definitions.TryGetBuilding(attachDef, out var aDef))
                            cost = $" {aDef.GoldCost}g/{aDef.TimberCost}t";
                        if (HudButton(new Rect(ax, attachY, HudStyle.S(130f), btnH), $"Turret {slotName}{cost}",
                                "Mount a keep turret on this pad (attach-only)"))
                            orders.AttachToKeep(slot, attachDef);
                        ax += HudStyle.S(136f);
                    }
                }
            }
            else if (b.CanProduce || producing)
            {
                trainX = DrawPricedTrainButton(trainX, trainY, btnW, btnH, gap, "Infantry", r.BasicUnitId);
                trainX = DrawPricedTrainButton(trainX, trainY, btnW, btnH, gap, "Ranged", r.RangedUnitId);
                trainX = DrawPricedTrainButton(trainX, trainY, btnW, btnH, gap, "Elite", r.CavalryUnitId);
                trainX = DrawPricedTrainButton(trainX, trainY, btnW, btnH, gap, "Siege", r.SiegeUnitId);

                // Equipment research at barracks / unit building
                if (!FactionDefaultContent.IsKeepBuildingId(b.DefinitionId)
                    && r.EquipmentUpgradeIds != null
                    && r.EquipmentUpgradeIds.Length > 0)
                {
                    float researchY = trainY + btnH + HudStyle.S(6f);
                    float rx = HudStyle.S(12f);
                    for (int i = 0; i < r.EquipmentUpgradeIds.Length; i++)
                    {
                        string upId = r.EquipmentUpgradeIds[i];
                        if (match.Definitions == null || !match.Definitions.TryGetUpgrade(upId, out var up))
                            continue;
                        rx = DrawResearchButton(rx, researchY, HudStyle.S(150f), btnH, player, b, upId, up);
                    }
                }
            }

            if (producing && HudButton(new Rect(trainX, trainY, HudStyle.S(110f), btnH), "Cancel (X)"))
                orders.CancelProduction();
        }

        private float DrawCmdChip(
            float x,
            float y,
            float w,
            float h,
            float gap,
            float maxX,
            string label,
            System.Action onClick)
        {
            if (x + w > maxX)
            {
                // Caller keeps y; wrapping handled by returning reset x via sentinel is awkward —
                // combat chips are short enough for typical widths; clamp instead.
                w = Mathf.Max(HudStyle.S(48f), maxX - x);
                if (w < HudStyle.S(40f))
                    return x;
            }

            if (HudButton(new Rect(x, y, w, h), label))
                onClick?.Invoke();
            return x + w + gap;
        }

        private float DrawPricedTrainButton(float x, float y, float w, float h, float gap, string title, string unitId)
        {
            string label = title;
            string tip = title;
            bool leaderBlocked = false;
            if (match.Definitions != null && match.Definitions.TryGetUnit(unitId, out var def))
            {
                label = $"{title} ({def.GoldCost}g)";
                tip = DescribeUnit(def);
                if (def.IsLeader && match.World != null && match.Session != null
                    && PlayerOwnsLeader(match.Session.LocalPlayer))
                {
                    leaderBlocked = true;
                    label = $"{title} (fielded)";
                    tip = "Only one leader may be fielded at a time";
                }
            }

            var rect = new Rect(x, y, w, h);
            if (leaderBlocked)
                HudFadedLabel(rect, label, tip);
            else if (HudButton(rect, label, tip))
                orders.TrainUnit(unitId);
            return x + w + gap;
        }

        private float DrawPricedBuildingButton(float x, float y, float h, float gap, string title, string buildingId)
        {
            string label = title;
            float w = HudStyle.S(128f);
            string tip = title;
            if (match.Definitions != null && match.Definitions.TryGetBuilding(buildingId, out var def))
            {
                label = $"{title} {def.GoldCost}g/{def.TimberCost}t";
                w = HudStyle.S(150f);
                tip = DescribeBuilding(def);
            }

            float maxX = HudStyle.ContentRight - HudStyle.S(4f);
            if (x + w > maxX)
                w = Mathf.Max(HudStyle.S(80f), maxX - x);

            if (HudButton(new Rect(x, y, w, h), label, tip))
                orders.EnterPlaceMode(buildingId);
            return x + w + gap;
        }

        private bool PlayerOwnsLeader(PlayerId player)
        {
            if (match.World == null || match.Definitions == null)
                return false;
            for (int i = 0; i < match.World.Units.Count; i++)
            {
                var u = match.World.Units[i];
                if (u.Owner != player || !u.IsAlive)
                    continue;
                if (match.Definitions.TryGetUnit(u.DefinitionId, out var def) && def.IsLeader)
                    return true;
            }

            for (int i = 0; i < match.World.Buildings.Count; i++)
            {
                var b = match.World.Buildings[i];
                if (b.Owner != player)
                    continue;
                if (!string.IsNullOrEmpty(b.ProductionUnitDefId)
                    && match.Definitions.TryGetUnit(b.ProductionUnitDefId, out var producing)
                    && producing.IsLeader)
                    return true;
            }

            return false;
        }

        private static bool HudButton(Rect rect, string label) => HudButton(rect, label, null);

        private static bool HudButton(Rect rect, string label, string tip)
        {
            bool clicked = HudStyle.PanelButton(rect, label, new Color(0.09f, 0.11f, 0.13f, 0.94f));
            if (clicked)
                AsterraAudio.PlayUiClick();
            if (!string.IsNullOrEmpty(tip) && rect.Contains(Event.current.mousePosition))
                CurrentHoverTip = tip;
            return clicked;
        }

        private void HudFadedLabel(Rect rect, string label, string tip)
        {
            HudClickBlocker.Block(rect);
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.38f);
            HudStyle.DrawPanel(rect, new Color(0.07f, 0.08f, 0.09f, 0.85f));
            GUI.Label(rect, label, HudStyle.Button);
            GUI.color = prev;
            if (!string.IsNullOrEmpty(tip) && rect.Contains(Event.current.mousePosition))
                _hoverTip = tip;
        }

        /// <summary>Set by static HudButton; copied into instance tip each frame.</summary>
        private static string CurrentHoverTip;

        private void DrawHoverTip()
        {
            if (!string.IsNullOrEmpty(CurrentHoverTip))
                _hoverTip = CurrentHoverTip;
            CurrentHoverTip = null;
            if (string.IsNullOrEmpty(_hoverTip))
                return;

            var tipRect = new Rect(
                HudStyle.S(12f),
                Screen.height - HudStyle.S(248f),
                Mathf.Min(HudStyle.S(520f), HudStyle.ContentRight - HudStyle.S(16f)),
                HudStyle.S(34f));
            HudClickBlocker.Block(tipRect);
            HudStyle.DrawPanel(tipRect, new Color(0.04f, 0.05f, 0.07f, 0.92f));
            GUI.Label(
                new Rect(tipRect.x + HudStyle.S(10f), tipRect.y + HudStyle.S(6f), tipRect.width - HudStyle.S(20f), HudStyle.S(22f)),
                _hoverTip,
                HudStyle.Label);
        }

        private void DrawEndScreen(PlayerId player)
        {
            bool won = match.Result.Winner == player;
            if (!_endSfxPlayed)
            {
                _endSfxPlayed = true;
                AsterraAudio.Play(won ? AsterraSfx.Victory : AsterraSfx.Defeat);
                AsterraAudio.Instance.SetMusicMuted(true);
            }

            string title = won ? "VICTORY" : "DEFEAT";
            string reason = match.Result.Reason == MatchEndReason.KeepDestroyed
                ? "Enemy keep destroyed"
                : "Territory held long enough";
            string story = BuildStoryBeat(won);
            float boxH = string.IsNullOrEmpty(story) ? 160f : 210f;
            var endRect = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.28f, 440f, boxH);
            HudClickBlocker.Block(endRect);
            HudStyle.DrawPanel(endRect, new Color(0.04f, 0.05f, 0.06f, 0.94f));
            GUI.Label(new Rect(endRect.x, endRect.y + 16f, endRect.width, 36f), title, HudStyle.Title);
            GUI.Label(new Rect(endRect.x + 20f, endRect.y + 58f, endRect.width - 40f, 28f), reason, HudStyle.Label);
            if (!string.IsNullOrEmpty(story))
                GUI.Label(new Rect(endRect.x + 20f, endRect.y + 90f, endRect.width - 40f, 60f), story, HudStyle.Label);

            float by = endRect.yMax - 48f;
            if (HudButton(new Rect(endRect.x + 40f, by, 160f, 34f), "Main Menu"))
            {
                _endSfxPlayed = false;
                _statsRecorded = false;
                _overlay = AsterraMenuPanels.Overlay.None;
                match.ReturnToMainMenu();
            }
            if (HudButton(new Rect(endRect.xMax - 200f, by, 160f, 34f), "Rematch"))
            {
                _endSfxPlayed = false;
                _statsRecorded = false;
                _overlay = AsterraMenuPanels.Overlay.None;
                match.RematchOffline();
            }
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

        private void DrawConstructionStatus(BuildingSnapshot b, float y)
        {
            float pct = Mathf.Clamp01(b.BuildProgress);
            bool builderOnSite = BuilderOnSite(b);
            var rect = new Rect(8f, y, 360f, 72f);
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(
                rect,
                new Color(0.06f, 0.07f, 0.08f, 0.94f),
                new Color(0.85f, 0.65f, 0.25f, 0.7f),
                1.5f);

            string name = ShortName(b.DefinitionId);
            if (match.Definitions != null && match.Definitions.TryGetBuilding(b.DefinitionId, out var def)
                && !string.IsNullOrEmpty(def.DisplayName))
                name = def.DisplayName;

            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, 240f, 20f), $"Constructing — {name}", HudStyle.Label);
            GUI.Label(
                new Rect(rect.xMax - 72f, rect.y + 8f, 60f, 20f),
                $"{(int)(pct * 100f)}%",
                HudStyle.Label);

            HudStyle.DrawPanel(new Rect(rect.x + 12f, rect.y + 34f, rect.width - 24f, 12f), new Color(0.12f, 0.12f, 0.14f, 0.95f));
            HudStyle.DrawPanel(
                new Rect(rect.x + 12f, rect.y + 34f, (rect.width - 24f) * pct, 12f),
                new Color(0.95f, 0.72f, 0.22f, 0.95f));

            GUI.color = builderOnSite
                ? new Color(0.55f, 0.9f, 0.55f, 0.95f)
                : new Color(0.95f, 0.7f, 0.4f, 0.95f);
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 50f, rect.width - 24f, 18f),
                builderOnSite ? "Builder on site — construction advancing" : "Needs a builder on site to start",
                HudStyle.Caption);
            GUI.color = Color.white;
        }

        private bool BuilderOnSite(BuildingSnapshot building)
        {
            if (match.World == null || match.Definitions == null)
                return false;
            float radius = SkirmishWorldSim.ConstructionWorkRadius;
            float r2 = radius * radius;
            for (int i = 0; i < match.World.Units.Count; i++)
            {
                var u = match.World.Units[i];
                if (!u.IsAlive || u.Owner != building.Owner || u.IsGarrisoned)
                    continue;
                if (!match.Definitions.TryGetUnit(u.DefinitionId, out var def) || !def.IsBuilder)
                    continue;
                float dx = u.X - building.X;
                float dz = u.Z - building.Z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }

        private void DrawProductionQueue(BuildingSnapshot b, float y)
        {
            byte faction = match.PlayerRoster != null ? match.PlayerRoster.Id.Value : (byte)0;
            Color fac = AsterraMeshLibrary.FactionColor(faction);

            GUI.Label(new Rect(12f, y - 20f, 420f, 18f), "Production", HudStyle.Label);
            float qx = 12f;
            const float qw = 78f;
            const float qh = 52f;
            const float gap = 6f;

            if (!string.IsNullOrEmpty(b.ProductionUnitDefId))
            {
                DrawQueuePortrait(qx, y, qw, qh, b.ProductionUnitDefId, fac, b.ProductionProgress, true, b.Id);
                qx += qw + gap;
            }

            DrawQueuePortraitSlot(b.QueuedUnitDefId, ref qx, y, qw, qh, gap, fac, b.Id);
            DrawQueuePortraitSlot(b.Queue1DefId, ref qx, y, qw, qh, gap, fac, b.Id);
            DrawQueuePortraitSlot(b.Queue2DefId, ref qx, y, qw, qh, gap, fac, b.Id);
            DrawQueuePortraitSlot(b.Queue3DefId, ref qx, y, qw, qh, gap, fac, b.Id);

            if (!string.IsNullOrEmpty(b.ResearchUpgradeDefId))
            {
                float rx = qx + 12f;
                string name = ShortName(b.ResearchUpgradeDefId);
                if (match.Definitions != null && match.Definitions.TryGetUpgrade(b.ResearchUpgradeDefId, out var up))
                    name = ShortName(up.DisplayName);
                var rect = new Rect(rx, y, 150f, qh);
                HudClickBlocker.Block(rect);
                HudStyle.DrawPanel(rect, new Color(0.08f, 0.12f, 0.18f, 0.95f));
                GUI.Label(new Rect(rx + 8f, y + 6f, 134f, 20f), $"Research {name}", HudStyle.Label);
                float pct = Mathf.Clamp01(b.ResearchProgress);
                HudStyle.DrawPanel(new Rect(rx + 8f, y + qh - 14f, 134f * pct, 6f), new Color(0.35f, 0.7f, 1f, 0.95f));
                GUI.Label(new Rect(rx + 8f, y + 26f, 134f, 18f), $"{(int)(pct * 100f)}%", HudStyle.Label);
            }
        }

        private void DrawQueuePortraitSlot(string defId, ref float qx, float y, float qw, float qh, float gap, Color fac, SimEntityId buildingId)
        {
            if (string.IsNullOrEmpty(defId))
                return;
            DrawQueuePortrait(qx, y, qw, qh, defId, fac, 0f, false, buildingId);
            qx += qw + gap;
        }

        private void DrawQueuePortrait(float x, float y, float w, float h, string defId, Color fac, float progress, bool active, SimEntityId buildingId)
        {
            var rect = new Rect(x, y, w, h);
            HudClickBlocker.Block(rect);
            HudStyle.DrawPanel(rect, active
                ? new Color(0.1f, 0.14f, 0.1f, 0.95f)
                : new Color(0.08f, 0.09f, 0.1f, 0.92f));
            var tex = HudStyle.Portrait(defId, fac);
            GUI.DrawTexture(new Rect(x + 6f, y + 4f, 28f, 28f), tex);
            GUI.Label(new Rect(x + 36f, y + 6f, w - 40f, 24f), ShortName(defId), HudStyle.Label);
            if (active)
            {
                float pct = Mathf.Clamp01(progress);
                HudStyle.DrawPanel(new Rect(x + 6f, y + h - 12f, (w - 12f) * pct, 6f), new Color(0.3f, 0.85f, 0.45f, 0.95f));
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                orders.JumpToBuilding(buildingId);
        }

        private bool SelectionAllHaveEquipment(string upgradeId)
        {
            if (orders?.Selection == null || match.World == null || string.IsNullOrEmpty(upgradeId))
                return false;
            var selected = orders.Selection.Selected;
            if (selected.Count == 0)
                return false;

            UpgradeDefData def = null;
            if (match.Definitions != null)
                match.Definitions.TryGetUpgrade(upgradeId, out def);

            int matched = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                uint id = selected[i].Value;
                bool found = false;
                for (int u = 0; u < match.World.Units.Count; u++)
                {
                    var snap = match.World.Units[u];
                    if (snap.Id.Value != id)
                        continue;
                    found = true;
                    if (FactionDefaultContent.IsBuilderUnitId(snap.DefinitionId))
                        break;
                    if (def != null
                        && match.Definitions != null
                        && match.Definitions.TryGetUnit(snap.DefinitionId, out var unitDef)
                        && !def.FitsUnitRole(unitDef.Role))
                        break;
                    if (!snap.HasAppliedEquipment(upgradeId))
                        return false;
                    matched++;
                    break;
                }

                if (!found)
                    return false;
            }

            return matched > 0;
        }

        private static string DescribeCompatibleRoles(UpgradeDefData up)
        {
            if (up == null || up.CompatibleRoleMask == 0)
                return string.Empty;
            var names = new System.Collections.Generic.List<string>(3);
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Infantry)) != 0)
                names.Add("infantry");
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Ranged)) != 0)
                names.Add("ranged");
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Cavalry)) != 0)
                names.Add("cavalry");
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Siege)) != 0)
                names.Add("siege");
            return names.Count > 0 ? " [" + string.Join("/", names) + " only]" : string.Empty;
        }

        private bool TryGetSelectedBuilding(out BuildingSnapshot building)
        {
            building = default;
            if (orders == null || !orders.SelectedBuilding.HasValue || match.World == null)
                return false;
            for (int i = 0; i < match.World.Buildings.Count; i++)
            {
                if (match.World.Buildings[i].Id != orders.SelectedBuilding.Value)
                    continue;
                building = match.World.Buildings[i];
                return true;
            }

            return false;
        }

        private float DrawResearchButton(float x, float y, float w, float h, PlayerId player, BuildingSnapshot b, string upId, UpgradeDefData up)
        {
            bool researched = match.World.HasUpgrade(player, upId);
            bool researchingThis = b.ResearchUpgradeDefId == upId;
            bool busy = !string.IsNullOrEmpty(b.ResearchUpgradeDefId);
            string tip = DescribeUpgrade(up);

            if (researched)
            {
                HudFadedLabel(new Rect(x, y, w, h), $"{ShortName(up.DisplayName)} ✓", tip + " — researched");
                return x + w + 6f;
            }

            if (researchingThis)
            {
                var rect = new Rect(x, y, w, h);
                HudClickBlocker.Block(rect);
                HudStyle.DrawPanel(rect, new Color(0.08f, 0.12f, 0.18f, 0.95f));
                float pct = Mathf.Clamp01(b.ResearchProgress);
                GUI.Label(new Rect(x + 6f, y + 4f, w - 12f, 16f), $"{ShortName(up.DisplayName)}", HudStyle.Label);
                HudStyle.DrawPanel(new Rect(x + 6f, y + h - 10f, (w - 12f) * pct, 5f), new Color(0.35f, 0.7f, 1f, 0.95f));
                GUI.Label(new Rect(x + 6f, y + 16f, w - 12f, 14f), $"{(int)(pct * 100f)}%", HudStyle.Label);
                if (rect.Contains(Event.current.mousePosition))
                    _hoverTip = tip + " — researching";
                return x + w + 6f;
            }

            string label = busy
                ? $"{ShortName(up.DisplayName)}…"
                : $"{ShortName(up.DisplayName)} ({up.GoldCost}g)";
            if (!busy && HudButton(new Rect(x, y, w, h), label, tip))
                orders.ResearchUpgrade(upId);
            else if (busy)
                HudFadedLabel(new Rect(x, y, w, h), label, tip + " — wait for current research");
            return x + w + 6f;
        }

        private bool IsPrimaryActivePower(string[] powerIds, int index, string powerId)
        {
            if (match == null || match.Definitions == null || powerIds == null)
                return index == 0;
            for (int i = 0; i < powerIds.Length; i++)
            {
                if (!match.Definitions.TryGetPower(powerIds[i], out var def) || def.IsPassive)
                    continue;
                return powerIds[i] == powerId;
            }

            return false;
        }

        private static string DescribePower(PowerDefData power, bool unlocked, float buff, float cd)
        {
            string effect = power.Effect == PowerEffectKind.ArmorAura
                ? $"grants +{power.EffectMagnitude:0.#} armor"
                : power.Effect == PowerEffectKind.MoveSpeedAura
                    ? $"grants +{power.EffectMagnitude:0.#} move speed"
                    : $"grants +{power.EffectMagnitude:0} damage";
            if (power.IsPassive)
            {
                if (!unlocked)
                    return $"{power.DisplayName} (passive): unlock for {power.UnlockGoldCost}g — permanent {effect}";
                return $"{power.DisplayName} (passive) active — permanent {effect}";
            }

            if (!unlocked)
                return $"{power.DisplayName}: unlock for {power.UnlockGoldCost}g — {effect} for {power.DurationSeconds:0}s";
            if (buff > 0.05f)
                return $"{power.DisplayName} active — {effect} ({buff:0}s left)";
            if (cd > 0.05f)
                return $"{power.DisplayName} cooling down ({cd:0}s)";
            return $"{power.DisplayName}: {effect} for {power.DurationSeconds:0}s (CD {power.CooldownSeconds:0}s)";
        }

        private static string DescribeUpgrade(UpgradeDefData up)
        {
            if (up.Kind == UpgradeKind.Equipment)
            {
                var parts = new System.Collections.Generic.List<string>(3);
                if (up.ArmorBonus > 0f)
                    parts.Add($"+{up.ArmorBonus:0} armor");
                if (up.AttackDamageBonus > 0f)
                    parts.Add($"+{up.AttackDamageBonus:0} damage");
                if (System.Math.Abs(up.UnitDamageMultiplier - 1f) > 0.01f)
                    parts.Add($"×{up.UnitDamageMultiplier:0.##} damage");
                string effect = parts.Count > 0 ? string.Join(", ", parts) : "equipment boost";
                string roles = DescribeCompatibleRoles(up);
                return $"{up.DisplayName}: research ({up.GoldCost}g) then Equip selected ({up.ResolvedEquipGoldCost}g each){roles} — {effect}";
            }

            if (up.KeepHealthBonus > 0f || up.KeepSightBonus > 0f)
                return $"{up.DisplayName}: keep +{up.KeepHealthBonus:0} HP, +{up.KeepSightBonus:0} sight";
            if (System.Math.Abs(up.TrainTimeMultiplier - 1f) > 0.01f)
                return $"{up.DisplayName}: train time ×{up.TrainTimeMultiplier:0.##}";
            return $"{up.DisplayName}: faction upgrade ({up.GoldCost}g)";
        }

        private static string DescribeUnit(UnitDefData def)
        {
            if (def.IsLeader)
                return $"{def.DisplayName}: unique commander unit (one at a time)";
            if (def.IsBuilder)
                return $"{def.DisplayName}: gathers and constructs buildings";
            return $"{def.DisplayName}: {def.AttackDamage:0} dmg · range {def.AttackRange:0.#} · {def.MaxHealth:0} HP";
        }

        private static string DescribeBuilding(BuildingDefData def)
        {
            if (def.GoldPerSecond > 0)
                return $"{def.DisplayName}: +{def.GoldPerSecond} gold/sec when complete";
            if (def.Kind == BuildingKind.Wall)
                return $"{def.DisplayName}: fortification — rotate with Q/E while placing";
            if (def.Kind == BuildingKind.Tower)
                return $"{def.DisplayName}: defensive tower";
            if (def.Kind == BuildingKind.Producer)
                return $"{def.DisplayName}: trains combat units";
            return $"{def.DisplayName}: {def.GoldCost}g / {def.TimberCost} timber";
        }

        private int EstimateGoldIncomePerSecond(PlayerId player)
        {
            if (match.World == null)
                return 0;

            int sum = 0;
            for (int i = 0; i < match.World.Territories.Count; i++)
            {
                var t = match.World.Territories[i];
                if (t.HasController && t.Controller == player && t.State == TerritoryState.Controlled)
                    sum += 8;
            }

            for (int i = 0; i < match.World.Buildings.Count; i++)
            {
                var b = match.World.Buildings[i];
                if (b.Owner != player || b.State != BuildingState.Active)
                    continue;
                if (match.Definitions != null
                    && match.Definitions.TryGetBuilding(b.DefinitionId, out var def)
                    && def.GoldPerSecond > 0)
                    sum += def.GoldPerSecond;
            }

            return sum;
        }

        private string BuildContextLine()
        {
            if (orders == null || match.World == null)
                return "Select units or a building";

            if (orders.IsPlaceMode)
                return "Place mode — LMB place · Esc cancel";
            if (orders.IsAttackMoveArmed)
                return "Attack-move — click ground";
            if (orders.IsPatrolArmed)
                return "Patrol — click ground";

            if (orders.SelectedBuilding.HasValue && TryGetSelectedBuilding(out var b))
            {
                if (b.State == BuildingState.Constructing)
                {
                    int pct = Mathf.RoundToInt(Mathf.Clamp01(b.BuildProgress) * 100f);
                    return BuilderOnSite(b)
                        ? $"Constructing {ShortName(b.DefinitionId)} — {pct}% (builder on site)"
                        : $"Constructing {ShortName(b.DefinitionId)} — {pct}% (needs builder on site)";
                }

                if (FactionDefaultContent.IsKeepBuildingId(b.DefinitionId))
                    return "Keep — train · research · attach turrets · unlock powers";
                if (b.CanProduce || !string.IsNullOrEmpty(b.ProductionUnitDefId) || b.QueueCount > 0)
                    return "Producer — train combat units";
                return $"Building — {ShortName(b.DefinitionId)}";
            }

            if (orders.Selection == null || orders.Selection.Selected.Count == 0)
                return "No selection";

            int n = orders.Selection.Selected.Count;
            if (orders.HasBuilderSelected)
                return n == 1 ? "Builder selected — place buildings" : $"{n} builders selected";
            return n == 1 ? "Unit selected" : $"{n} units selected";
        }

        private static string BuildHotkeyHint()
        {
            return "T train · U research · ⇧U apply · Q power · B/N/M/O build · A attack-move · S stop";
        }

        private static string ShortName(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return "?";
            int cut = definitionId.LastIndexOf('_');
            return cut >= 0 && cut + 1 < definitionId.Length
                ? definitionId.Substring(cut + 1)
                : definitionId;
        }
    }
}
