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
        private int _cmdCardIndex;
        private int _cmdMaxRows = 2;
        private float _cmdGridX;
        private float _cmdGridY;
        private float _cmdCardW;
        private float _cmdCardH;
        private float _cmdGap;

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
            DrawPowerButton();
            DrawFeedbackToast();

            if (showDebugHud)
                DrawDebugStatus(player);

            DrawCommandDock(player);
            DrawHoverTip();

            if (_overlay != AsterraMenuPanels.Overlay.None)
            {
                var prior = _overlay;
                AsterraMenuPanels.Draw(_overlay, out bool quitMenu, out var next);
                if (next == AsterraMenuPanels.Overlay.Profile)
                    next = AsterraMenuPanels.Overlay.Pause; // Profile is lobby-only
                if (prior == AsterraMenuPanels.Overlay.Pause && next == AsterraMenuPanels.Overlay.Options)
                    _overlayReturn = AsterraMenuPanels.Overlay.Pause;
                if (prior == AsterraMenuPanels.Overlay.Pause && next == AsterraMenuPanels.Overlay.Controls)
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
            string forecast = string.Empty;
            if (match.World != null
                && match.World.HasPower(player, FactionDefaultContent.ForecastAbilityId)
                && match.World is SkirmishWorldSim worldForWeather
                && worldForWeather.Environment.WeatherSim.TryGetForecast(out var nextKind, out float until))
            {
                forecast = until < 1f
                    ? $"Next {nextKind}"
                    : $"Next {nextKind} in {until:0}s";
            }

            bool showForecast = !string.IsNullOrEmpty(forecast);
            float stripH = showHold || showForecast ? HudStyle.S(56f) : HudStyle.S(40f);
            if (showHold && showForecast)
                stripH = HudStyle.S(72f);
            var strip = new Rect(HudStyle.S(10f), HudStyle.S(10f), HudStyle.S(showForecast ? 420f : 340f), stripH);
            HudClickBlocker.Block(strip);
            HudStyle.DrawFrame(strip, HudStyle.PanelFillDeep, HudStyle.PanelBorder, 1f);
            HudStyle.DrawAccentBar(
                new Rect(strip.x + 1f, strip.y + 1f, strip.width - 2f, HudStyle.S(2f)),
                HudStyle.AccentSoft);

            string gold = "0";
            string timber = "0";
            if (match.Wallet != null)
            {
                int income = EstimateGoldIncomePerSecond(player);
                gold = match.Wallet.Get(player, ResourceType.Gold).ToString();
                if (income > 0)
                    gold += $"  +{income}/s";
                timber = match.Wallet.Get(player, ResourceType.Timber).ToString();
            }

            float pillH = HudStyle.S(28f);
            float pillY = strip.y + HudStyle.S(8f);
            HudStyle.ResourcePill(
                new Rect(strip.x + HudStyle.S(8f), pillY, HudStyle.S(150f), pillH),
                "gold",
                gold,
                HudStyle.Gold);
            HudStyle.ResourcePill(
                new Rect(strip.x + HudStyle.S(166f), pillY, HudStyle.S(120f), pillH),
                "timber",
                timber,
                HudStyle.Timber);

            if (showForecast)
            {
                GUI.Label(
                    new Rect(strip.x + HudStyle.S(8f), pillY + pillH + HudStyle.S(2f), strip.width - HudStyle.S(16f), HudStyle.S(16f)),
                    forecast,
                    HudStyle.Caption);
            }

            if (showHold)
            {
                float barW = HudStyle.S(200f);
                float barY = strip.yMax - HudStyle.S(14f);
                HudStyle.DrawPanel(
                    new Rect(strip.x + HudStyle.S(10f), barY, barW, HudStyle.S(6f)),
                    new Color(0.12f, 0.12f, 0.14f, 0.95f));
                HudStyle.DrawPanel(
                    new Rect(strip.x + HudStyle.S(10f), barY, barW * Mathf.Clamp01(hold), HudStyle.S(6f)),
                    HudStyle.Accent);
                GUI.Label(
                    new Rect(strip.x + HudStyle.S(10f) + barW + HudStyle.S(8f), barY - HudStyle.S(6f), HudStyle.S(100f), HudStyle.S(16f)),
                    $"Hold {(int)(hold * 100f)}%",
                    HudStyle.Caption);
            }
        }

        private void DrawSaveLoadChrome()
        {
            // Save/Load live in the pause menu (F5/F9 still work).
        }

        private void DrawSelectionStrip(PlayerId player)
        {
            // Selection portraits are drawn inside DrawCommandDock.
        }

        private void DrawPowerButton()
        {
            if (orders == null || match.PlayerRoster == null || match.World == null)
                return;

            var roster = match.PlayerRoster;
            var powerIds = roster.PowerIds;
            if (powerIds == null || powerIds.Length == 0)
                return;

            float size = HudStyle.S(44f);
            float gap = HudStyle.S(6f);
            float cardW = HudStyle.S(64f);
            float cardH = HudStyle.S(54f);
            float x = Screen.width - HudStyle.S(10f) - cardW;
            float y = HudStyle.S(12f);
            for (int i = 0; i < powerIds.Length; i++)
            {
                string powerId = powerIds[i];
                if (match.Definitions == null || !match.Definitions.TryGetPower(powerId, out var powerDef))
                    continue;

                match.World.TryGetCommanderAbilityStatus(match.Session.LocalPlayer, powerId, out float cd, out float buff);
                bool unlocked = match.World.HasPower(match.Session.LocalPlayer, powerId);
                bool faded = unlocked && (powerDef.IsPassive || buff > 0.05f || cd > 0.05f);
                bool canClick = !unlocked || (!powerDef.IsPassive && buff <= 0.05f && cd <= 0.05f);
                string tip = DescribePower(powerDef, unlocked, buff, cd);
                string shortName = ShortPowerName(powerDef.DisplayName);
                string status = !unlocked ? $"{powerDef.UnlockGoldCost}g"
                    : powerDef.IsPassive ? "On"
                    : buff > 0.05f ? $"{buff:0}s"
                    : cd > 0.05f ? $"{cd:0}s"
                    : IsPrimaryActivePower(roster.PowerIds, i, powerId) ? "Ready"
                    : "Use";
                string label = $"{shortName}\n{status}";

                var rect = new Rect(x, y, cardW, cardH);
                if (HudStyle.CommandCard(rect, "power", label, HudStyle.Accent, out bool hovered, canClick && !faded, selected: buff > 0.05f))
                {
                    AsterraAudio.PlayUiClick();
                    if (!unlocked)
                        orders.UnlockPower(powerId);
                    else if (canClick)
                        orders.ActivateCommanderAbility(powerId);
                }

                if (hovered)
                    _hoverTip = tip;
                y += cardH + gap;
            }
        }

        private static string ShortPowerName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return "Power";
            // Prefer a readable two-word clip for the small card.
            string[] parts = displayName.Split(' ');
            if (parts.Length >= 2 && (parts[0].Length + parts[1].Length) <= 12)
                return parts[0] + " " + parts[1];
            if (displayName.Length <= 11)
                return displayName;
            return displayName.Substring(0, 10) + "…";
        }

        private void DrawCommandDock(PlayerId player)
        {
            if (orders == null)
                return;

            float dockH = HudStyle.CommandDockHeight;
            float dockY = Screen.height - dockH - HudStyle.S(8f);
            float dockX = HudStyle.S(10f);
            float dockW = HudStyle.ContentRight - dockX - HudStyle.S(6f);
            var dock = new Rect(dockX, dockY, dockW, dockH);
            HudClickBlocker.Block(dock);
            HudStyle.DrawFrame(dock, HudStyle.PanelFillDeep, HudStyle.PanelBorder, 1.5f);
            HudStyle.DrawAccentBar(
                new Rect(dock.x + 1f, dock.y + 1f, dock.width - 2f, HudStyle.S(3f)),
                HudStyle.AccentSoft);

            float selectW = HudStyle.S(220f);
            DrawSelectionInto(player, new Rect(dock.x + HudStyle.S(8f), dock.y + HudStyle.S(10f), selectW, dock.height - HudStyle.S(18f)));

            _cmdCardW = HudStyle.S(64f);
            _cmdCardH = HudStyle.S(62f);
            _cmdGap = HudStyle.S(4f);
            _cmdMaxRows = Mathf.Max(2, Mathf.FloorToInt((dockH - HudStyle.S(24f)) / (_cmdCardH + _cmdGap)));
            _cmdGridX = dock.x + selectW + HudStyle.S(12f);
            _cmdGridY = dock.y + HudStyle.S(12f);
            _cmdCardIndex = 0;

            if (orders.SelectedBuilding.HasValue && TryGetSelectedBuilding(out var building)
                && building.State == BuildingState.Constructing)
            {
                DrawConstructionStatus(building, dock.y - HudStyle.S(78f));
                PushCard("idle", $"Idle {orders.IdleWorkerCount}", "Select idle workers", HudStyle.Timber,
                    () => orders.SelectIdleWorker());
                return;
            }

            if (orders.SelectedBuilding.HasValue && TryGetSelectedBuilding(out var b))
            {
                DrawBuildingCommands(player, b);
                return;
            }

            PushCard("idle", $"Idle {orders.IdleWorkerCount}", "Select idle workers", HudStyle.Timber,
                () => orders.SelectIdleWorker());

            if (orders.IsPlaceMode)
            {
                PushCard("cancel", "Cancel", "Cancel place mode", HudStyle.Danger,
                    () => orders.CancelPlaceMode());
                return;
            }

            if (orders.HasTerrainWorkArmed)
            {
                PushCard("cancel", "Cancel", "Cancel terrain work", HudStyle.Danger,
                    () => orders.CancelTerrainWorkMode());
                return;
            }

            if (orders.HasBuilderSelected)
                DrawBuilderCommands();
            else if (orders.HasCombatUnitSelected)
                DrawCombatCommands(player);
        }

        private void DrawSelectionInto(PlayerId player, Rect area)
        {
            if (orders.Selection == null || match.World == null)
                return;
            var selected = orders.Selection.Selected;
            if (selected.Count == 0 && !orders.SelectedBuilding.HasValue)
            {
                GUI.Label(area, "No selection", HudStyle.Caption);
                return;
            }

            byte faction = match.PlayerRoster != null ? match.PlayerRoster.Id.Value : (byte)0;
            Color fac = AsterraMeshLibrary.FactionColor(faction);
            float x = area.x;
            float y = area.y;
            const float portrait = 44f;

            if (orders.SelectedBuilding.HasValue && TryGetSelectedBuilding(out var selectedBuilding))
            {
                var tex = HudStyle.Portrait(selectedBuilding.DefinitionId, fac);
                GUI.DrawTexture(new Rect(x, y, portrait, portrait), tex);
                float hpRatio = selectedBuilding.MaxHealth > 0.01f
                    ? Mathf.Clamp01(selectedBuilding.Health / selectedBuilding.MaxHealth)
                    : 1f;
                HudStyle.DrawPanel(new Rect(x, y + portrait + 2f, portrait * hpRatio, 4f), HudStyle.Hp);
                string name = ShortName(selectedBuilding.DefinitionId);
                if (match.Definitions != null && match.Definitions.TryGetBuilding(selectedBuilding.DefinitionId, out var def)
                    && !string.IsNullOrEmpty(def.DisplayName))
                    name = def.DisplayName;
                GUI.Label(new Rect(x + portrait + 8f, y + 4f, area.width - portrait - 8f, 20f), name, HudStyle.Label);
                GUI.Label(
                    new Rect(x + portrait + 8f, y + 24f, area.width - portrait - 8f, 18f),
                    $"{selectedBuilding.Health:0}/{selectedBuilding.MaxHealth:0}",
                    HudStyle.Caption);
                return;
            }

            int shown = 0;
            for (int i = 0; i < selected.Count && shown < 5; i++)
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

                float px = x + shown * (portrait + 6f);
                var tex = HudStyle.Portrait(defId, fac);
                GUI.DrawTexture(new Rect(px, y, portrait, portrait), tex);
                float ratio = max > 0.01f ? Mathf.Clamp01(hp / max) : 1f;
                HudStyle.DrawPanel(new Rect(px, y + portrait + 2f, portrait * ratio, 4f), HudStyle.Hp);
                string stanceLetter = stance == UnitStance.Defensive ? "D"
                    : stance == UnitStance.Hold ? "H"
                    : stance == UnitStance.Passive ? "P"
                    : "A";
                HudStyle.DrawPanel(new Rect(px + portrait - 14f, y, 14f, 14f), HudStyle.PanelFillDeep);
                GUI.Label(new Rect(px + portrait - 14f, y - 2f, 16f, 16f), stanceLetter, HudStyle.Caption);
                shown++;
            }

            if (selected.Count > 5)
                GUI.Label(new Rect(x, y + portrait + 10f, area.width, 18f), $"+{selected.Count - 5} more", HudStyle.Caption);
            else if (selected.Count > 0)
                GUI.Label(new Rect(x, y + portrait + 10f, area.width, 18f), $"{selected.Count} selected", HudStyle.Caption);
        }

        private void DrawCombatCommands(PlayerId player)
        {
            PushCard("stop", "Stop", "Stop", HudStyle.Danger, () => orders.StopSelected());
            PushCard("stance", "Aggro", "Aggressive stance", HudStyle.Danger,
                () => orders.SetSelectedStance(UnitStance.Aggressive));
            PushCard("shield", "Defend", "Defensive stance", new Color(0.35f, 0.65f, 1f),
                () => orders.SetSelectedStance(UnitStance.Defensive));
            PushCard("hold", "Hold", "Hold position", HudStyle.Accent,
                () => orders.SetSelectedStance(UnitStance.Hold));
            DrawCombatGearCards(player);
        }

        private void DrawCombatGearCards(PlayerId player)
        {
            var equip = match.PlayerRoster?.EquipmentUpgradeIds;
            if (equip == null || match.World == null)
                return;

            for (int i = 0; i < equip.Length; i++)
            {
                string upId = equip[i];
                if (!match.World.HasUpgrade(player, upId))
                    continue;
                if (match.Definitions == null || !match.Definitions.TryGetUpgrade(upId, out var up))
                    continue;
                int equipCost = up.ResolvedEquipGoldCost;
                string tip = DescribeUpgrade(up);
                bool done = SelectionAllHaveEquipment(upId);
                PushCard(
                    "gear",
                    done ? "Done" : $"{equipCost}g",
                    tip,
                    HudStyle.Accent,
                    done ? null : () => orders.ApplyUpgradeToSelected(upId),
                    enabled: !done);
            }
        }

        private void DrawBuilderCommands()
        {
            var roster = match.PlayerRoster;
            if (roster == null)
                return;

            string producerLabel = "Barracks";
            if (match.Definitions != null
                && roster.ProducerBuildingId != null
                && match.Definitions.TryGetBuilding(roster.ProducerBuildingId, out var producer))
                producerLabel = producer.DisplayName;
            PushBuildCard("hammer", producerLabel, roster.ProducerBuildingId);
            if (roster.ExtraBuildingIds != null)
            {
                for (int i = 0; i < roster.ExtraBuildingIds.Length; i++)
                {
                    string extraId = roster.ExtraBuildingIds[i];
                    string extraLabel = extraId;
                    string extraIcon = "hammer";
                    if (match.Definitions != null && match.Definitions.TryGetBuilding(extraId, out var extra))
                    {
                        extraLabel = extra.DisplayName;
                        extraIcon = extra.GoldPerSecond > 0
                            ? "outpost"
                            : extra.Kind == BuildingKind.Special ? "power" : "hammer";
                    }

                    PushBuildCard(extraIcon, extraLabel, extraId);
                }
            }
            PushBuildCard("tower", BuildingLabel(roster.TowerBuildingId, "Tower"), roster.TowerBuildingId);
            PushBuildCard("wall", BuildingLabel(roster.WallBuildingId, "Wall"), roster.WallBuildingId);
            PushBuildCard("trench", "Trench", FactionDefaultContent.TrenchWorksId);
            bool showBridge = roster.DefinitionId != FactionDefaultContent.UniversityId
                || (match.World != null && match.World.HasUpgrade(player, FactionDefaultContent.AdvancedConstructionUpgradeId));
            if (showBridge)
                PushBuildCard("bridge", "Bridge", FactionDefaultContent.BridgeId);
            PushBuildCard("barricade", "Barrier", FactionDefaultContent.BarricadeId);
            PushBuildCard("ferry", "Ferry", FactionDefaultContent.FerryDockId);
            PushBuildCard("outpost", BuildingLabel(roster.OutpostBuildingId, "Mine"), roster.OutpostBuildingId);
            PushBuildCard("earth", "Berm", FactionDefaultContent.BermWorksId);
            PushBuildCard("trench", "Fill", FactionDefaultContent.FillWorksId);
            PushBuildCard("ferry", "Moat", FactionDefaultContent.MoatWorksId);
            PushBuildCard("timber", "Clear", FactionDefaultContent.ClearWorksId);
            PushBuildCard("power", "Burn", FactionDefaultContent.BurnWorksId);
            PushBuildCard("stone", "Quarry", FactionDefaultContent.QuarryWorksId);
            PushBuildCard("sapper", "Spikes", FactionDefaultContent.SpikesWorksId);
            PushBuildCard("hammer", "Debris", FactionDefaultContent.DebrisWorksId);
            PushCard("repair", "Repair", "Repair bridge", HudStyle.Timber,
                () => orders.RepairBridgeAtCursor());
        }

        private string BuildingLabel(string buildingId, string fallback)
        {
            if (match.Definitions != null
                && !string.IsNullOrEmpty(buildingId)
                && match.Definitions.TryGetBuilding(buildingId, out var def)
                && !string.IsNullOrEmpty(def.DisplayName))
                return def.DisplayName;
            return fallback;
        }

        private void DrawBuildingCommands(PlayerId player, BuildingSnapshot b)
        {
            bool producing = !string.IsNullOrEmpty(b.ProductionUnitDefId) || b.QueueCount > 0;
            if (producing)
                DrawProductionQueue(b, _cmdGridY - HudStyle.S(58f));

            var r = match.PlayerRoster;
            if (r == null)
                return;

            bool isKeep = FactionDefaultContent.IsKeepBuildingId(b.DefinitionId);
            if (isKeep)
            {
                if (match.Definitions != null && match.Definitions.TryGetBuilding(b.DefinitionId, out var keepDef)
                    && keepDef.TrainableUnitIds != null)
                {
                    for (int i = 0; i < keepDef.TrainableUnitIds.Length; i++)
                    {
                        string uid = keepDef.TrainableUnitIds[i];
                        if (match.Definitions.TryGetUnit(uid, out var udef) && udef.IsBuilder)
                            PushTrainCard("worker", udef.DisplayName, uid);
                    }

                    for (int i = 0; i < keepDef.TrainableUnitIds.Length; i++)
                    {
                        string uid = keepDef.TrainableUnitIds[i];
                        if (match.Definitions.TryGetUnit(uid, out var udef) && udef.IsLeader)
                            PushTrainCard("leader", udef.DisplayName, uid);
                    }
                }
                else
                {
                    PushTrainCard("worker", "Builder", r.BuilderUnitId);
                    PushTrainCard("leader", "Leader", r.LeaderUnitId);
                }

                DrawResearchCards(player, b, keepTechs: true);
            }
            else if (b.CanProduce || producing)
            {
                if (match.Definitions != null && match.Definitions.TryGetBuilding(b.DefinitionId, out var prodDef)
                    && prodDef.TrainableUnitIds != null && prodDef.TrainableUnitIds.Length > 0)
                {
                    for (int i = 0; i < prodDef.TrainableUnitIds.Length; i++)
                    {
                        string uid = prodDef.TrainableUnitIds[i];
                        string icon = "sword";
                        string label = uid;
                        if (match.Definitions.TryGetUnit(uid, out var udef))
                        {
                            label = udef.DisplayName;
                            icon = udef.Role == UnitRole.Ranged ? "bow"
                                : udef.Role == UnitRole.Cavalry ? "horse"
                                : udef.Role == UnitRole.Siege ? "siege"
                                : udef.BuildingDamageMultiplier > 2f ? "sapper"
                                : udef.SightRadius > 150f ? "scout"
                                : "sword";
                        }

                        PushTrainCard(icon, label, uid);
                    }
                }
                else
                {
                    PushTrainCard("sword", "Infantry", r.BasicUnitId);
                    PushTrainCard("bow", "Ranged", r.RangedUnitId);
                    PushTrainCard("horse", "Cavalry", r.CavalryUnitId);
                    if (!string.IsNullOrEmpty(r.EliteUnitId))
                        PushTrainCard("elite", "Elite", r.EliteUnitId);
                    PushTrainCard("siege", "Siege", r.SiegeUnitId);
                    PushTrainCard("scout", "Scout", r.ScoutUnitId);
                    PushTrainCard("sapper", "Sapper", r.SapperUnitId);
                }

                DrawResearchCards(player, b, keepTechs: false);
            }

            if (producing)
                PushCard("cancel", "Cancel", "Cancel production", HudStyle.Danger,
                    () => orders.CancelProduction());

            DrawBuildingAdminCards(player, b);
        }

        private void DrawBuildingAdminCards(PlayerId player, BuildingSnapshot b)
        {
            if (b.AllowsGarrison && b.GarrisonCount > 0)
            {
                PushCard("unload", "Unload", $"Unload garrison ({b.GarrisonCount})", HudStyle.Timber, () =>
                {
                    if (match.Commands != null)
                    {
                        match.Commands.SubmitLocal(new ExitGarrisonCommand
                        {
                            Issuer = player,
                            BuildingId = b.Id,
                        });
                    }
                });
            }

            if (b.Owner == player && !FactionDefaultContent.IsKeepBuildingId(b.DefinitionId))
                PushCard("demolish", "Raze", "Demolish (half refund)", HudStyle.Danger,
                    () => orders.DemolishSelectedBuilding());

            if (b.Owner == player && (b.Kind == BuildingKind.Wall || b.Kind == BuildingKind.Gate))
                PushCard("timber", "Salvage", "Raze wall for timber", HudStyle.Timber,
                    () => orders.RazeSelectedWall());

            if (b.Owner == player
                && (b.DefinitionId == FactionDefaultContent.PalisadeId
                    || b.DefinitionId == FactionDefaultContent.BarricadeId)
                && match.World != null
                && match.World.HasUpgrade(player, FactionDefaultContent.StoneWallsUpgradeId))
            {
                PushCard("stone", "Stone", "Upgrade wall to stone", HudStyle.Accent,
                    () => orders.UpgradeSelectedWallToStone());
            }

            if (FactionDefaultContent.IsKeepBuildingId(b.DefinitionId) && b.AttachmentSlotCount > 0)
            {
                string attachDef = FactionDefaultContent.KeepTurretId;
                for (byte slot = 0; slot < b.AttachmentSlotCount; slot++)
                {
                    bool occupied = (b.AttachmentOccupiedMask & (1 << slot)) != 0;
                    string slotName = slot == 0 ? "N" : slot == 1 ? "E" : slot == 2 ? "S" : "W";
                    byte captured = slot;
                    PushCard(
                        "turret",
                        occupied ? $"{slotName}✓" : slotName,
                        occupied ? "Turret pad occupied" : $"Mount keep turret ({slotName})",
                        HudStyle.Accent,
                        occupied ? null : () => orders.AttachToKeep(captured, attachDef),
                        enabled: !occupied);
                }
            }
        }

        private void DrawResearchCards(PlayerId player, BuildingSnapshot b, bool keepTechs)
        {
            var r = match.PlayerRoster;
            if (r == null)
                return;

            if (keepTechs)
            {
                var keepUps = r.KeepUpgradeIds != null && r.KeepUpgradeIds.Length > 0 ? r.KeepUpgradeIds : r.UpgradeIds;
                if (keepUps != null)
                {
                    for (int i = 0; i < keepUps.Length; i++)
                        PushResearchCard(player, b, keepUps[i]);
                }
            }

            if (r.EquipmentUpgradeIds != null)
            {
                for (int i = 0; i < r.EquipmentUpgradeIds.Length; i++)
                    PushResearchCard(player, b, r.EquipmentUpgradeIds[i]);
            }
        }

        private void PushResearchCard(PlayerId player, BuildingSnapshot b, string upId)
        {
            if (match.Definitions == null || !match.Definitions.TryGetUpgrade(upId, out var up))
                return;
            if (up.Kind == UpgradeKind.Equipment && FactionDefaultContent.IsKeepBuildingId(b.DefinitionId))
            {
                // Equipment research also offered at keep for convenience.
            }

            bool researched = match.World != null && match.World.HasUpgrade(player, upId);
            bool researchingThis = b.ResearchUpgradeDefId == upId;
            bool busy = !string.IsNullOrEmpty(b.ResearchUpgradeDefId);
            string tip = DescribeUpgrade(up);
            if (researched)
            {
                PushCard("research", "Done", tip + " — researched", HudStyle.Timber, null, enabled: false);
                return;
            }

            if (researchingThis)
            {
                PushCard("research", $"{(int)(b.ResearchProgress * 100f)}%", tip, new Color(0.35f, 0.7f, 1f), null, enabled: false);
                return;
            }

            PushCard(
                "research",
                $"{up.GoldCost}g",
                tip,
                new Color(0.35f, 0.7f, 1f),
                busy ? null : () => orders.ResearchUpgrade(upId),
                enabled: !busy);
        }

        private void PushBuildCard(string icon, string label, string buildingId)
        {
            string tip = label;
            string cardLabel = label;
            if (match.Definitions != null && match.Definitions.TryGetBuilding(buildingId, out var def))
            {
                tip = DescribeBuilding(def);
                if (def.GoldCost > 0 || def.TimberCost > 0)
                {
                    cardLabel = def.TimberCost > 0
                        ? $"{label}\n{def.GoldCost}g/{def.TimberCost}t"
                        : $"{label}\n{def.GoldCost}g";
                }
            }

            PushCard(icon, cardLabel, tip, HudStyle.Accent, () => orders.EnterPlaceMode(buildingId));
        }

        private void PushWorkCard(string icon, string label, TerrainWorkKind kind)
        {
            string buildingId = FactionDefaultContent.EarthworkBuildingId(kind);
            if (!string.IsNullOrEmpty(buildingId))
            {
                PushBuildCard(icon, label, buildingId);
                return;
            }

            PushCard(icon, label, $"{kind}", HudStyle.Timber,
                () => orders.EnterTerrainWorkMode(kind));
        }

        private void PushTrainCard(string icon, string label, string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return;
            string tip = label;
            if (match.Definitions != null && match.Definitions.TryGetUnit(unitId, out var def))
                tip = DescribeUnit(def) + $" ({def.GoldCost}g)";

            PushCard(icon, label, tip, HudStyle.Accent, () => orders.TrainUnit(unitId));
        }

        private void PushCard(string icon, string label, string tip, Color accent, System.Action onClick, bool enabled = true)
        {
            float maxX = HudStyle.ContentRight - HudStyle.S(8f);
            float cell = _cmdCardW + _cmdGap;
            int maxCols = Mathf.Max(1, Mathf.FloorToInt((maxX - _cmdGridX + _cmdGap) / cell));

            int col = _cmdCardIndex % maxCols;
            int row = _cmdCardIndex / maxCols;
            if (row >= _cmdMaxRows)
                return;

            var rect = new Rect(
                _cmdGridX + col * cell,
                _cmdGridY + row * (_cmdCardH + _cmdGap),
                _cmdCardW,
                _cmdCardH);

            if (HudStyle.CommandCard(rect, icon, label, accent, out bool hovered, enabled))
            {
                AsterraAudio.PlayUiClick();
                onClick?.Invoke();
            }

            if (hovered && !string.IsNullOrEmpty(tip))
                _hoverTip = tip;
            _cmdCardIndex++;
        }

        private void DrawCommandBar(PlayerId player) => DrawCommandDock(player);

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
        }

        private void DrawFeedbackToast()
        {
            if (MatchFeedback.Instance == null || !MatchFeedback.Instance.HasActiveMessage)
                return;
            float w = Mathf.Min(HudStyle.S(480f), Screen.width * 0.5f);
            var rect = new Rect((Screen.width - w) * 0.5f, HudStyle.S(58f), w, HudStyle.S(36f));
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(rect, HudStyle.PanelFillDeep, HudStyle.AccentSoft, 1f);
            var style = new GUIStyle(HudStyle.Label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(14f * HudStyle.Scale),
            };
            GUI.Label(rect, MatchFeedback.Instance.CurrentMessage, style);
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

            // Floating tip above the command dock — not a permanent chrome strip.
            float w = Mathf.Min(HudStyle.S(420f), HudStyle.ContentRight - HudStyle.S(24f));
            var tipRect = new Rect(
                HudStyle.S(12f),
                Screen.height - HudStyle.CommandDockHeight - HudStyle.S(36f),
                w,
                HudStyle.S(24f));
            HudClickBlocker.Block(tipRect);
            HudStyle.DrawFrame(tipRect, HudStyle.PanelFillDeep, HudStyle.PanelBorder, 1f);
            GUI.Label(
                new Rect(tipRect.x + HudStyle.S(8f), tipRect.y + HudStyle.S(2f), tipRect.width - HudStyle.S(16f), HudStyle.S(20f)),
                _hoverTip,
                HudStyle.Caption);
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
                            && match.PlayerRoster.DefinitionId == FactionDefaultContent.VeiledInheritanceId;
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
                    if (def != null
                        && match.Definitions != null
                        && match.Definitions.TryGetUnit(snap.DefinitionId, out var unitDef)
                        && !def.FitsUnit(unitDef.Id, unitDef.Role))
                        break;
                    if (def == null && FactionDefaultContent.IsBuilderUnitId(snap.DefinitionId))
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
            if (up == null)
                return string.Empty;
            if (up.CompatibleUnitIds != null && up.CompatibleUnitIds.Length > 0)
                return " [listed units only]";
            if (up.CompatibleRoleMask == 0)
                return string.Empty;
            var names = new System.Collections.Generic.List<string>(4);
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Infantry)) != 0)
                names.Add("infantry");
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Ranged)) != 0)
                names.Add("ranged");
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Cavalry)) != 0)
                names.Add("cavalry");
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Siege)) != 0)
                names.Add("siege");
            if ((up.CompatibleRoleMask & (1 << (int)UnitRole.Builder)) != 0)
                names.Add("builders");
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
            string effect = power.Effect switch
            {
                PowerEffectKind.ArmorAura => $"grants +{power.EffectMagnitude:0.#} armor",
                PowerEffectKind.MoveSpeedAura => $"grants +{power.EffectMagnitude:0.#} move speed",
                PowerEffectKind.DamageAura => $"grants +{power.EffectMagnitude:0} damage",
                PowerEffectKind.ForceWeather => "summons a harsh weather front",
                PowerEffectKind.SpawnSwarm => "opens a gate that spawns weak shades until destroyed",
                PowerEffectKind.PlaceGate => "place a source gate, then a destination, even through fog",
                PowerEffectKind.EconomyBoost => $"+{(int)power.EffectMagnitude} gold/sec",
                PowerEffectKind.SpawnScouts => "spawns scouts",
                PowerEffectKind.MindControl => "steal one enemy troop for 30s",
                PowerEffectKind.SpawnRandomBeasts => power.Id == FactionDefaultContent.MercenariesAbilityId
                    ? "spawns hired troops that fight until slain"
                    : "spawns wild creatures that fight until slain",
                PowerEffectKind.FloodArea => "floods the ground around the target",
                PowerEffectKind.EyesInSky => "spawns birds you can move for a short time",
                PowerEffectKind.ExplosiveStrip => "sends explosive carts along a strip",
                PowerEffectKind.Forecast => "shows the next weather on the resource bar",
                PowerEffectKind.RelocateSight => "plants lasting sight on one place; recast moves it",
                PowerEffectKind.SunRay => "burns a point; extra damage to non-humans",
                PowerEffectKind.DayOfTheSun => $"clears weather to sun and +{(int)(power.EffectMagnitude * 100f)}% troops",
                PowerEffectKind.BlindRadius => "stuns enemies in a radius",
                _ => "commander power",
            };
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
                if (up.SightBonus > 0f)
                    parts.Add($"+{up.SightBonus:0} sight");
                string effect = parts.Count > 0 ? string.Join(", ", parts) : "equipment boost";
                string roles = DescribeCompatibleRoles(up);
                return $"{up.DisplayName}: research ({up.GoldCost}g) then Equip selected ({up.ResolvedEquipGoldCost}g each){roles} — {effect}";
            }

            if (up.KeepHealthBonus > 0f || up.KeepSightBonus > 0f)
                return $"{up.DisplayName}: keep +{up.KeepHealthBonus:0} HP, +{up.KeepSightBonus:0} sight";
            if (System.Math.Abs(up.TrainTimeMultiplier - 1f) > 0.01f)
                return $"{up.DisplayName}: train time ×{up.TrainTimeMultiplier:0.##}";
            if (up.Id == FactionDefaultContent.DesertStormUpgradeId
                || up.Id == FactionDefaultContent.RainfallUpgradeId
                || up.Id == FactionDefaultContent.FogOfWarUpgradeId
                || up.Id == FactionDefaultContent.IceFormationUpgradeId)
                return $"{up.DisplayName}: priests radiate this weather nearby ({up.GoldCost}g)";
            return $"{up.DisplayName}: faction upgrade ({up.GoldCost}g)";
        }

        private static string DescribeUnit(UnitDefData def)
        {
            if (def.IsLeader)
                return $"{def.DisplayName}: commander hero";
            if (def.IsBuilder)
                return $"{def.DisplayName}: gathers and constructs buildings";
            return $"{def.DisplayName}: {def.AttackDamage:0} dmg · range {def.AttackRange:0.#} · {def.MaxHealth:0} HP";
        }

        private static string DescribeBuilding(BuildingDefData def)
        {
            string cost = def.TimberCost > 0
                ? $"{def.GoldCost}g / {def.TimberCost} timber"
                : $"{def.GoldCost}g";
            string time = def.BuildSeconds > 0.1f ? $" · {def.BuildSeconds:0.#}s build" : string.Empty;
            if (def.GoldPerSecond > 0)
                return $"{def.DisplayName}: +{def.GoldPerSecond} gold/sec when complete ({cost})";
            if (FactionDefaultContent.IsEarthworkBuildingId(def.Id))
                return $"{def.DisplayName}: {cost}{time}";
            if (def.Kind == BuildingKind.Wall)
                return $"{def.DisplayName}: fortification ({cost}){time}";
            if (def.Kind == BuildingKind.Tower)
                return $"{def.DisplayName}: defensive tower ({cost}){time}";
            if (def.Kind == BuildingKind.Producer)
                return $"{def.DisplayName}: trains combat units ({cost}){time}";
            return $"{def.DisplayName}: {cost}{time}";
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
                return "Placing building";
            if (orders.IsAttackMoveArmed)
                return "Attack-move armed";
            if (orders.IsPatrolArmed)
                return "Patrol armed";

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
