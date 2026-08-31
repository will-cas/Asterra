using Asterra.AI;
using Asterra.Gameplay.Audio;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Pre-match: hub (campaign / skirmish), then faction cards, map picker, AI difficulty.</summary>
    public sealed class OfflineMatchMenu : MonoBehaviour
    {
        private enum View
        {
            Hub = 0,
            Skirmish = 1,
            Campaign = 2,
        }

        [SerializeField] private MatchBootstrap bootstrap;

        private View _view = View.Hub;
        private int _playerFaction;
        private int _enemyFaction = 1;
        private MapCatalog.Choice _map = MapCatalog.BuiltinChoice(SkirmishMapId.LushForest);
        private AiDifficulty _difficulty = AiDifficulty.Normal;
        private int _spawnSeat;
        private Texture2D _mapPreview;
        private string _previewMapId;
        private AsterraMenuPanels.Overlay _overlay = AsterraMenuPanels.Overlay.None;

        private GUIStyle _brandStyle;
        private GUIStyle _modeStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _startStyle;

        private void Awake()
        {
            if (bootstrap == null)
                bootstrap = GetComponent<MatchBootstrap>();
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<MatchBootstrap>();

            if (bootstrap != null)
            {
                _playerFaction = bootstrap.PlayerFactionIndex;
                _enemyFaction = bootstrap.EnemyFactionIndex;
                _map = MapCatalog.FromId(bootstrap.MapKey);
                _difficulty = bootstrap.AiDifficulty;
                _spawnSeat = bootstrap.LocalSpawnSeat;
            }

            _ = AsterraAudio.Instance;
            RebuildPreviewIfNeeded();
        }

        public void ShowHub()
        {
            _view = View.Hub;
            _overlay = AsterraMenuPanels.Overlay.None;
        }

        public void ShowCampaign()
        {
            _view = View.Campaign;
            _overlay = AsterraMenuPanels.Overlay.None;
            _playerFaction = CampaignCatalog.PlayerFactionIndex;
            if (CampaignProgress.HasSave)
                _difficulty = CampaignProgress.Difficulty;
        }

        public void ShowSkirmish()
        {
            _view = View.Skirmish;
            _overlay = AsterraMenuPanels.Overlay.None;
        }

        private void OnDisable()
        {
            DestroyPreview();
        }

        private void DestroyPreview()
        {
            if (_mapPreview != null)
            {
                Destroy(_mapPreview);
                _mapPreview = null;
                _previewMapId = null;
            }
        }

        private void RebuildPreviewIfNeeded()
        {
            if (_mapPreview != null && _previewMapId == _map.Id)
                return;
            DestroyPreview();
            _mapPreview = MapPreviewBuilder.Build(_map.Id);
            _previewMapId = _map.Id;
        }

        private void OnGUI()
        {
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<MatchBootstrap>();
            if (bootstrap == null || bootstrap.IsMatchRunning || bootstrap.Result.IsOver)
                return;

            HudStyle.Ensure();
            EnsureLocalStyles();

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && _overlay != AsterraMenuPanels.Overlay.None)
                _overlay = AsterraMenuPanels.Overlay.None;

            // Opaque full-screen cover — never show the skirmish world behind the lobby.
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            HudClickBlocker.Block(screen);
            HudStyle.DrawPanel(screen, new Color(0.03f, 0.04f, 0.05f, 1f));

            float pad = 20f;
            float w = Screen.width - pad * 2f;
            float h = Screen.height - pad * 2f;
            float x = pad;
            float y = pad;
            var panel = new Rect(x, y, w, h);

            HudStyle.DrawFrame(
                panel,
                new Color(0.045f, 0.055f, 0.065f, 1f),
                new Color(0.55f, 0.48f, 0.28f, 0.55f),
                2f);
            HudStyle.DrawAccentBar(new Rect(x, y, w, 3f), new Color(0.78f, 0.66f, 0.32f, 0.9f));

            GUI.Label(new Rect(x, y + 14f, w, 36f), "ASTERRA", _brandStyle);
            if (_view == View.Hub)
                GUI.Label(new Rect(x, y + 48f, w, 20f), "MAIN MENU", _modeStyle);
            else if (_view == View.Campaign)
                GUI.Label(new Rect(x, y + 48f, w, 20f), "CAMPAIGN", _modeStyle);
            else
                GUI.Label(new Rect(x, y + 48f, w, 20f), "OFFLINE SKIRMISH", _modeStyle);
            HudStyle.DrawAccentBar(
                new Rect(x + w * 0.5f - 48f, y + 72f, 96f, 2f),
                new Color(0.78f, 0.66f, 0.32f, 0.65f));

            // Top-right lobby chrome.
            float chipY = y + 18f;
            if (LobbyChip(new Rect(x + w - 248f, chipY, 100f, 28f), "Profile"))
            {
                AsterraAudio.PlayUiClick();
                _overlay = AsterraMenuPanels.Overlay.Profile;
            }

            if (LobbyChip(new Rect(x + w - 136f, chipY, 100f, 28f), "Options"))
            {
                AsterraAudio.PlayUiClick();
                _overlay = AsterraMenuPanels.Overlay.Options;
            }

            float contentX = x + 28f;
            float contentW = w - 56f;

            if (_view == View.Hub)
            {
                DrawHub(new Rect(contentX, y + 88f, contentW, h - 120f));
            }
            else if (_view == View.Campaign)
            {
                DrawCampaign(new Rect(contentX, y + 88f, contentW, h - 120f));
            }
            else
            {
                DrawSkirmishLobby(x, y, w, h, contentX, contentW);
            }

            if (_overlay != AsterraMenuPanels.Overlay.None)
            {
                AsterraMenuPanels.Draw(_overlay, out _, out var next);
                _overlay = next;
            }
        }

        private void DrawHub(Rect rect)
        {
            GUI.color = new Color(0.82f, 0.84f, 0.78f, 0.95f);
            GUI.Label(
                new Rect(rect.x, rect.y, rect.width, 48f),
                "Choose how you go to war.",
                HudStyle.Body);
            GUI.color = Color.white;

            float cardW = (rect.width - 18f) * 0.5f;
            float cardH = Mathf.Clamp(rect.height * 0.55f, 220f, 340f);
            float cardY = rect.y + 56f;

            if (DrawModeCard(
                    new Rect(rect.x, cardY, cardW, cardH),
                    "CAMPAIGN",
                    "Mundor Crown — first of six faction stories.\nLinear. Difficulty is the AI.\nBetween fights: story. Secrets: optional aims, a hidden map, a secret ending."))
            {
                ShowCampaign();
            }

            if (DrawModeCard(
                    new Rect(rect.x + cardW + 18f, cardY, cardW, cardH),
                    "SKIRMISH",
                    "Pick factions, map, spawn, and AI difficulty.\nNo story. No campaign save. Just the field."))
            {
                ShowSkirmish();
            }

            GUI.color = new Color(0.7f, 0.72f, 0.68f, 0.75f);
            GUI.Label(
                new Rect(rect.x, rect.yMax - 36f, rect.width, 32f),
                CampaignCatalog.WhatItIsnt,
                HudStyle.Caption);
            GUI.color = Color.white;
        }

        private bool DrawModeCard(Rect rect, string title, string body)
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(
                rect,
                new Color(0.07f, 0.09f, 0.1f, 0.95f),
                new Color(0.78f, 0.66f, 0.32f, 0.55f),
                1.5f);
            GUI.Label(new Rect(rect.x, rect.y + 24f, rect.width, 36f), title, _brandStyle);
            GUI.color = new Color(0.82f, 0.84f, 0.78f, 0.95f);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 80f, rect.width - 48f, 120f), body, HudStyle.Body);
            GUI.color = Color.white;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            if (clicked)
                AsterraAudio.PlayUiClick();
            return clicked;
        }

        private void DrawCampaign(Rect rect)
        {
            if (LobbyChip(new Rect(rect.x, rect.y, 88f, 28f), "Back"))
            {
                AsterraAudio.PlayUiClick();
                ShowHub();
                return;
            }

            int missionIndex = CampaignProgress.HasSave && !CampaignProgress.IsComplete
                ? CampaignProgress.NextMissionIndex
                : 0;
            if (CampaignProgress.IsComplete && CampaignProgress.HiddenMissionUnlocked && !CampaignProgress.SecretEnding)
                missionIndex = CampaignCatalog.SecretMissionIndex;
            else if (missionIndex >= CampaignCatalog.MissionCount)
                missionIndex = CampaignCatalog.MissionCount - 1;
            var mission = CampaignCatalog.Get(missionIndex);
            _playerFaction = CampaignCatalog.PlayerFactionIndex;
            var roster = FactionDefaultContent.All[CampaignCatalog.PlayerFactionIndex];
            int rival = CampaignCatalog.RivalFactionIndex(_playerFaction);
            var rivalRoster = FactionDefaultContent.All[rival];

            GUI.Label(
                new Rect(rect.x + 100f, rect.y, rect.width - 100f, 28f),
                mission.Chapter,
                HudStyle.Subtitle);

            var brief = new Rect(rect.x, rect.y + 40f, rect.width, Mathf.Min(200f, rect.height * 0.38f));
            HudStyle.DrawFrame(brief, new Color(0.06f, 0.08f, 0.09f, 0.95f), new Color(0.55f, 0.48f, 0.28f, 0.45f), 1.5f);
            GUI.Label(new Rect(brief.x + 16f, brief.y + 10f, brief.width - 32f, 28f), mission.DisplayName, _cardTitleStyle);
            GUI.color = new Color(0.82f, 0.84f, 0.78f, 0.95f);
            GUI.Label(
                new Rect(brief.x + 16f, brief.y + 40f, brief.width - 32f, brief.height - 52f),
                "LOOK  " + mission.Look + "\nAIM  " + mission.Aim + "\nSECRET  " + mission.SecretTease +
                "\n\n" + mission.StoryBetween,
                HudStyle.Caption);
            GUI.color = Color.white;

            float rowY = brief.yMax + 12f;
            var youRect = new Rect(rect.x, rowY, Mathf.Min(420f, rect.width * 0.48f), 110f);
            HudStyle.DrawFrame(youRect, new Color(0.07f, 0.09f, 0.1f, 0.95f), new Color(0.78f, 0.66f, 0.32f, 0.5f), 1.5f);
            GUI.Label(new Rect(youRect.x + 16f, youRect.y + 12f, youRect.width - 32f, 20f), "YOU PLAY", HudStyle.Subtitle);
            GUI.Label(new Rect(youRect.x + 16f, youRect.y + 36f, youRect.width - 32f, 28f), roster.DisplayName, _cardTitleStyle);
            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
            GUI.Label(
                new Rect(youRect.x + 16f, youRect.y + 68f, youRect.width - 32f, 36f),
                "Locked for this campaign. Five other faction stories come later.",
                HudStyle.Caption);
            GUI.color = Color.white;

            var rivalRect = new Rect(rect.x + Mathf.Min(436f, rect.width * 0.5f), rowY, Mathf.Min(360f, rect.width * 0.42f), 110f);
            HudStyle.DrawFrame(rivalRect, new Color(0.07f, 0.08f, 0.09f, 0.95f), new Color(0.45f, 0.35f, 0.32f, 0.5f), 1.5f);
            GUI.Label(new Rect(rivalRect.x + 16f, rivalRect.y + 12f, rivalRect.width - 32f, 20f), "RIVAL", HudStyle.Subtitle);
            GUI.Label(new Rect(rivalRect.x + 16f, rivalRect.y + 36f, rivalRect.width - 32f, 28f), rivalRoster.DisplayName, _cardTitleStyle);
            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
            GUI.Label(
                new Rect(rivalRect.x + 16f, rivalRect.y + 68f, rivalRect.width - 32f, 36f),
                "The Outcast Host — the rising you were sent to end.",
                HudStyle.Caption);
            GUI.color = Color.white;

            float diffY = rowY + 122f;
            DrawDifficultyStrip(new Rect(rect.x, diffY, rect.width, 80f));

            string status;
            if (CampaignProgress.SecretEnding)
                status = "Secret ending reached. The Crown kept its name.";
            else if (CampaignProgress.IsComplete && CampaignProgress.HiddenMissionUnlocked)
                status = "Story complete. The Quiet Capital is open.";
            else if (CampaignProgress.IsComplete)
                status = "Story complete. Win Burn the Camp by territory next time to open the hidden map.";
            else if (CampaignProgress.HasSave)
                status = "Continue. Between missions you return here for the next chapter of story.";
            else
                status = "New Crown campaign. Story between fights. Secrets are optional — they do not block the ending.";
            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
            GUI.Label(new Rect(rect.x, diffY + 86f, rect.width, 40f), status, HudStyle.Caption);
            GUI.color = Color.white;

            float btnY = Mathf.Min(diffY + 128f, rect.yMax - 52f);
            float btnW = Mathf.Min(280f, (rect.width - 12f) * 0.48f);
            bool secretReady = CampaignProgress.IsComplete
                               && CampaignProgress.HiddenMissionUnlocked
                               && !CampaignProgress.SecretEnding;
            bool canContinue = (CampaignProgress.HasSave && !CampaignProgress.IsComplete) || secretReady;
            if (canContinue)
            {
                string contLabel = secretReady ? "THE QUIET ROAD" : "CONTINUE CAMPAIGN";
                int startIndex = secretReady
                    ? CampaignCatalog.SecretMissionIndex
                    : CampaignProgress.NextMissionIndex;
                if (DrawSecondaryButton(new Rect(rect.x, btnY, btnW, 40f), contLabel))
                {
                    CampaignProgress.SetLobbyPicks(CampaignCatalog.PlayerFactionIndex, _difficulty);
                    AsterraAudio.Play(AsterraSfx.OrderTrain, 0.8f);
                    bootstrap.ConfigureAndStartCampaign(
                        CampaignCatalog.PlayerFactionIndex, _difficulty, startIndex);
                    enabled = false;
                }
            }

            string newLabel = CampaignProgress.HasSave ? "NEW CAMPAIGN" : "START CAMPAIGN";
            float newX = canContinue ? rect.x + btnW + 12f : rect.x;
            if (DrawStartButton(new Rect(newX, btnY, btnW, 40f), newLabel))
            {
                CampaignProgress.StartNew(CampaignCatalog.PlayerFactionIndex, _difficulty);
                AsterraAudio.Play(AsterraSfx.OrderTrain, 0.8f);
                bootstrap.ConfigureAndStartCampaign(CampaignCatalog.PlayerFactionIndex, _difficulty, 0);
                enabled = false;
            }
        }

        private void DrawSkirmishLobby(float x, float y, float w, float h, float contentX, float contentW)
        {
            if (LobbyChip(new Rect(contentX, y + 88f, 88f, 28f), "Back"))
            {
                AsterraAudio.PlayUiClick();
                ShowHub();
                return;
            }

            float cardY = y + 124f;
            float cardH = Mathf.Clamp(h * 0.2f, 130f, 170f);
            float gap = 14f;
            float cardW = (contentW - gap * 2f - 52f) * 0.5f;

            var playerRoster = FactionDefaultContent.All[_playerFaction % FactionDefaultContent.All.Length];
            var enemyRoster = FactionDefaultContent.All[_enemyFaction % FactionDefaultContent.All.Length];
            Color playerColor = AsterraMeshLibrary.FactionColor((byte)_playerFaction);
            Color enemyColor = AsterraMeshLibrary.FactionColor((byte)_enemyFaction);

            DrawFactionCard(
                new Rect(contentX, cardY, cardW, cardH),
                "YOUR FORCE",
                playerRoster,
                playerColor,
                ref _playerFaction);
            DrawVsBadge(new Rect(contentX + cardW + gap, cardY + cardH * 0.5f - 20f, 52f, 40f));
            DrawFactionCard(
                new Rect(contentX + cardW + gap + 52f + gap, cardY, cardW, cardH),
                "ENEMY FORCE",
                enemyRoster,
                enemyColor,
                ref _enemyFaction);

            float stripY = cardY + cardH + 12f;
            DrawMapStrip(new Rect(contentX, stripY, contentW, 72f));
            float previewY = stripY + 84f;

            const float footH = 18f;
            const float startH = 44f;
            const float contH = 28f;
            const float actionGap = 8f;
            float chromeH = footH + 8f + startH + actionGap + contH + 16f;
            float previewAvailH = Mathf.Max(140f, y + h - previewY - chromeH);
            float previewSize = Mathf.Min(previewAvailH, contentW * 0.42f, 320f);
            DrawMapPreview(new Rect(contentX, previewY, previewSize, previewSize));
            DrawSpawnAndDifficulty(
                new Rect(contentX + previewSize + 18f, previewY, contentW - previewSize - 18f, previewSize));

            float startW = Mathf.Min(360f, contentW);
            float startX = x + (w - startW) * 0.5f;
            float actionsBottom = y + h - footH - 8f;
            float actionsY = previewY + previewSize + 12f;
            float actionsNeeded = contH + actionGap + startH;
            if (actionsY + actionsNeeded > actionsBottom)
                actionsY = actionsBottom - actionsNeeded;

            bool hasSave = Asterra.Gameplay.Save.OfflineMatchSaveService.HasQuickSave;
            var loadRect = new Rect(startX, actionsY, startW, contH);
            if (hasSave)
            {
                if (DrawSecondaryButton(loadRect, "CONTINUE SAVED GAME"))
                {
                    AsterraAudio.Play(AsterraSfx.OrderTrain, 0.8f);
                    if (bootstrap.LoadOfflineQuick())
                        enabled = false;
                }
            }
            else
            {
                HudClickBlocker.Block(loadRect);
                HudStyle.DrawFrame(
                    loadRect,
                    new Color(0.08f, 0.09f, 0.1f, 0.7f),
                    new Color(0.35f, 0.35f, 0.32f, 0.35f),
                    1f);
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(loadRect, "NO SAVE FOUND", HudStyle.Button);
                GUI.color = prev;
            }

            float startY = actionsY + contH + actionGap;
            if (DrawStartButton(new Rect(startX, startY, startW, startH)))
            {
                AsterraAudio.Play(AsterraSfx.OrderTrain, 0.8f);
                bootstrap.ConfigureAndStartOffline(
                    _playerFaction, _enemyFaction, _map.Id, _difficulty, _spawnSeat);
                enabled = false;
            }

            GUI.color = new Color(0.7f, 0.72f, 0.68f, 0.75f);
            GUI.Label(
                new Rect(x + 28f, y + h - footH - 2f, w - 56f, footH),
                "Click keep markers to pick spawn  ·  ‹ › cycle maps  ·  F5/F9 in match",
                HudStyle.Caption);
            GUI.color = Color.white;
        }

        private static bool LobbyChip(Rect rect, string label)
        {
            return HudStyle.FrameButton(
                rect,
                label,
                new Color(0.12f, 0.13f, 0.14f, 0.98f),
                new Color(0.65f, 0.55f, 0.32f, 0.55f),
                1f);
        }

        private bool DrawSecondaryButton(Rect rect, string label)
        {
            bool clicked = HudStyle.FrameButton(
                rect,
                label,
                new Color(0.1f, 0.12f, 0.13f, 0.95f),
                new Color(0.55f, 0.5f, 0.35f, 0.45f),
                1f);
            if (clicked)
                AsterraAudio.PlayUiClick();
            return clicked;
        }

        private void DrawFactionCard(
            Rect rect,
            string role,
            FactionRoster roster,
            Color accent,
            ref int factionIndex)
        {
            HudClickBlocker.Block(rect);
            Color fill = Color.Lerp(new Color(0.07f, 0.09f, 0.1f, 0.95f), accent, 0.12f);
            Color border = Color.Lerp(accent, Color.white, 0.35f);
            HudStyle.DrawFrame(rect, fill, new Color(border.r, border.g, border.b, 0.55f), 1.5f);
            HudStyle.DrawAccentBar(new Rect(rect.x, rect.y, 4f, rect.height), accent);

            GUI.color = new Color(accent.r, accent.g, accent.b, 0.9f);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 10f, rect.width - 32f, 18f), role, HudStyle.Subtitle);
            GUI.color = Color.white;

            var portrait = HudStyle.Portrait(roster.LeaderUnitId ?? roster.BasicUnitId, accent);
            GUI.DrawTexture(new Rect(rect.x + 18f, rect.y + 36f, 56f, 56f), portrait, ScaleMode.ScaleToFit);

            GUI.Label(
                new Rect(rect.x + 88f, rect.y + 36f, rect.width - 104f, 26f),
                roster.DisplayName,
                _cardTitleStyle);

            GUI.color = new Color(0.82f, 0.84f, 0.78f, 0.95f);
            GUI.Label(
                new Rect(rect.x + 88f, rect.y + 64f, rect.width - 104f, 42f),
                roster.LoreBlurb,
                HudStyle.Body);
            GUI.color = Color.white;

            string power = string.IsNullOrEmpty(roster.PowerDisplayName)
                ? "Commander ready"
                : "Power · " + roster.PowerDisplayName;
            GUI.color = new Color(0.9f, 0.82f, 0.45f, 0.9f);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 108f, rect.width - 36f, 20f), power, HudStyle.Caption);
            GUI.color = Color.white;

            var prev = new Rect(rect.x + 18f, rect.yMax - 38f, 44f, 26f);
            var next = new Rect(rect.xMax - 62f, rect.yMax - 38f, 44f, 26f);
            var mid = new Rect(rect.x + 70f, rect.yMax - 38f, rect.width - 140f, 26f);
            HudStyle.DrawPanel(mid, new Color(0.1f, 0.12f, 0.13f, 0.9f));
            GUI.Label(mid, "Change", HudStyle.Subtitle);

            if (CycleChip(prev, "‹"))
                CycleFaction(ref factionIndex, -1);
            if (CycleChip(next, "›"))
                CycleFaction(ref factionIndex, +1);
        }

        private void DrawVsBadge(Rect rect)
        {
            HudStyle.DrawFrame(
                rect,
                new Color(0.12f, 0.1f, 0.08f, 0.95f),
                new Color(0.78f, 0.66f, 0.32f, 0.7f),
                1.5f);
            GUI.Label(rect, "VS", _modeStyle);
        }

        private void DrawMapStrip(Rect rect)
        {
            HudClickBlocker.Block(rect);
            bool custom = !_map.IsBuiltin;
            Color border = custom
                ? new Color(0.45f, 0.72f, 0.55f, 0.7f)
                : new Color(0.45f, 0.5f, 0.55f, 0.55f);
            HudStyle.DrawFrame(rect, new Color(0.06f, 0.08f, 0.09f, 0.95f), border, 1.5f);

            GUI.color = new Color(0.75f, 0.78f, 0.72f, 0.9f);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 8f, 100f, 18f), "BATTLEFIELD", HudStyle.Subtitle);
            GUI.color = Color.white;

            string badge = custom ? "DESIGNER" : "BUILT-IN";
            var badgeRect = new Rect(rect.x + 128f, rect.y + 8f, 88f, 18f);
            HudStyle.DrawPanel(
                badgeRect,
                custom ? new Color(0.18f, 0.35f, 0.24f, 0.95f) : new Color(0.18f, 0.2f, 0.22f, 0.95f));
            GUI.Label(badgeRect, badge, HudStyle.Subtitle);

            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 30f, rect.width - 160f, 24f),
                StripStar(_map.DisplayName),
                _cardTitleStyle);

            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 52f, rect.width - 160f, 18f),
                MapBlurb(_map),
                HudStyle.Caption);
            GUI.color = Color.white;

            if (CycleChip(new Rect(rect.xMax - 120f, rect.y + 18f, 44f, 36f), "‹"))
            {
                _map = PreviousMap(_map);
                _spawnSeat = Mathf.Clamp(_spawnSeat, 0, SeatCount(_map) - 1);
                RebuildPreviewIfNeeded();
            }

            if (CycleChip(new Rect(rect.xMax - 68f, rect.y + 18f, 44f, 36f), "›"))
            {
                _map = MapCatalog.Next(_map);
                _spawnSeat = Mathf.Clamp(_spawnSeat, 0, SeatCount(_map) - 1);
                RebuildPreviewIfNeeded();
            }
        }

        private void DrawMapPreview(Rect rect)
        {
            RebuildPreviewIfNeeded();
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(rect, new Color(0.05f, 0.07f, 0.08f, 0.98f), new Color(0.5f, 0.48f, 0.32f, 0.6f), 1.5f);

            var texRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f);
            if (_mapPreview != null)
                GUI.DrawTexture(texRect, _mapPreview, ScaleMode.StretchToFill);

            var keeps = MapPreviewBuilder.GetKeepMarkers(_map.Id);
            Color youCol = AsterraMeshLibrary.FactionColor((byte)_playerFaction);
            Color aiCol = AsterraMeshLibrary.FactionColor((byte)_enemyFaction);
            for (int i = 0; i < keeps.Count; i++)
            {
                var k = keeps[i];
                MapPreviewBuilder.WorldToPreviewGui(texRect, k.X, k.Z, out float gx, out float gy);
                bool yours = k.SeatIndex == _spawnSeat;
                Color c = yours ? youCol : aiCol;
                float size = yours ? 16f : 12f;
                EditorGuiDot(gx, gy, size, c);
                // Ring for selected
                if (yours)
                    EditorGuiRing(gx, gy, size + 6f, new Color(0.95f, 0.85f, 0.4f, 0.95f));
            }

            // Click to claim a keep seat.
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && texRect.Contains(e.mousePosition))
            {
                if (MapPreviewBuilder.TryHitSeat(texRect, e.mousePosition, _map.Id, 22f, out int seat))
                {
                    _spawnSeat = seat;
                    AsterraAudio.PlayUiClick();
                    e.Use();
                }
            }
        }

        private void DrawSpawnAndDifficulty(Rect rect)
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(rect, new Color(0.06f, 0.08f, 0.09f, 0.95f), new Color(0.45f, 0.5f, 0.55f, 0.45f), 1.5f);

            GUI.color = new Color(0.75f, 0.78f, 0.72f, 0.9f);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 18f), "SPAWN SEATS", HudStyle.Subtitle);
            GUI.color = Color.white;

            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 32f, rect.width - 28f, 44f),
                SpawnSeatCaption(_map, _spawnSeat),
                HudStyle.Caption);

            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.85f);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 82f, rect.width - 28f, 36f),
                SeatCount(_map) > 2
                    ? "Click a keep. Remaining keeps fill with AI."
                    : "Click a keep marker on the map to choose your spawn. The AI takes the other seat.",
                HudStyle.Caption);
            GUI.color = Color.white;

            int seats = SeatCount(_map);
            float by = rect.y + 124f;
            float bw = seats >= 4 ? 72f : 110f;
            float gap = 8f;
            for (int i = 0; i < seats; i++)
            {
                var r = new Rect(rect.x + 14f + i * (bw + gap), by, bw, 28f);
                if (GUI.Button(r, SeatButtonLabel(_map, i)))
                {
                    _spawnSeat = i;
                    AsterraAudio.PlayUiClick();
                }
            }

            float diffY = rect.y + 168f;
            DrawDifficultyStrip(new Rect(rect.x + 8f, diffY, rect.width - 16f, Mathf.Max(72f, rect.yMax - diffY - 8f)));
        }

        private static void EditorGuiDot(float gx, float gy, float size, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(gx - size * 0.5f, gy - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static void EditorGuiRing(float gx, float gy, float size, Color c)
        {
            // Simple 4-edge frame as a selection ring.
            float t = 2f;
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(gx - size * 0.5f, gy - size * 0.5f, size, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gx - size * 0.5f, gy + size * 0.5f - t, size, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gx - size * 0.5f, gy - size * 0.5f, t, size), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gx + size * 0.5f - t, gy - size * 0.5f, t, size), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void DrawDifficultyStrip(Rect rect)
        {
            HudClickBlocker.Block(rect);
            Color accent = DifficultyAccent(_difficulty);
            HudStyle.DrawFrame(rect, new Color(0.06f, 0.08f, 0.09f, 0.95f), new Color(accent.r, accent.g, accent.b, 0.55f), 1.5f);

            GUI.color = new Color(0.75f, 0.78f, 0.72f, 0.9f);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 8f, 120f, 18f), "AI DIFFICULTY", HudStyle.Subtitle);
            GUI.color = Color.white;

            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 28f, rect.width - 160f, 24f),
                AiDifficultyTuning.DisplayName(_difficulty).ToUpperInvariant(),
                _cardTitleStyle);

            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 50f, rect.width - 160f, 18f),
                AiDifficultyTuning.Blurb(_difficulty),
                HudStyle.Caption);
            GUI.color = Color.white;

            if (CycleChip(new Rect(rect.xMax - 120f, rect.y + 20f, 44f, 36f), "‹"))
                _difficulty = AiDifficultyTuning.Cycle(_difficulty, -1);
            if (CycleChip(new Rect(rect.xMax - 68f, rect.y + 20f, 44f, 36f), "›"))
                _difficulty = AiDifficultyTuning.Cycle(_difficulty, +1);
        }

        private static Color DifficultyAccent(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy: return new Color(0.45f, 0.7f, 0.45f, 1f);
                case AiDifficulty.Hard: return new Color(0.85f, 0.55f, 0.3f, 1f);
                case AiDifficulty.Insane: return new Color(0.85f, 0.3f, 0.32f, 1f);
                default: return new Color(0.78f, 0.66f, 0.32f, 1f);
            }
        }

        private bool DrawStartButton(Rect rect, string label = "START SKIRMISH")
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(
                rect,
                new Color(0.22f, 0.18f, 0.08f, 0.98f),
                new Color(0.9f, 0.75f, 0.35f, 0.85f),
                2f);
            HudStyle.DrawAccentBar(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.95f, 0.8f, 0.35f, 1f));
            bool clicked = GUI.Button(rect, label, _startStyle);
            if (clicked)
                AsterraAudio.PlayUiClick();
            return clicked;
        }

        private static bool CycleChip(Rect rect, string label)
        {
            bool clicked = HudStyle.FrameButton(
                rect,
                label,
                new Color(0.12f, 0.14f, 0.15f, 0.95f),
                new Color(0.55f, 0.5f, 0.35f, 0.5f),
                1f);
            if (clicked)
                AsterraAudio.PlayUiClick();
            return clicked;
        }

        private void EnsureLocalStyles()
        {
            if (_brandStyle != null)
                return;
            _brandStyle = new GUIStyle(HudStyle.Title)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _modeStyle = new GUIStyle(HudStyle.Label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _cardTitleStyle = new GUIStyle(HudStyle.Label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
            };
            _startStyle = new GUIStyle(HudStyle.Button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
        }

        private static void CycleFaction(ref int index, int delta)
        {
            int n = FactionDefaultContent.All.Length;
            index = (index + delta) % n;
            if (index < 0)
                index += n;
        }

        private static MapCatalog.Choice PreviousMap(MapCatalog.Choice current)
        {
            var all = MapCatalog.ListChoices();
            if (all.Count == 0)
                return current;
            int idx = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == current.Id)
                {
                    idx = i;
                    break;
                }
            }

            return all[(idx - 1 + all.Count) % all.Count];
        }

        private static string StripStar(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            return name.Replace(" ★", string.Empty).Trim();
        }

        private static string MapBlurb(MapCatalog.Choice map)
        {
            if (!map.IsBuiltin)
                return "Custom layout from the Map Creator — synced from Shared/Maps.";

            switch (map.BuiltinId)
            {
                case SkirmishMapId.MundorCapital:
                    return "Island citadel between two rivers. Defend the island against west and east, or siege from a bank.";
                case SkirmishMapId.OutcastCamp:
                    return "Host camp in the south-west corner. Four corner seats; 1v1 is camp vs north-east.";
                case SkirmishMapId.RiverCrossing:
                    return "North–south river, timber span, fords, boats at the mouths. West vs east.";
                case SkirmishMapId.FrozenWastes:
                    return "Snow and ice. Keeps on opposite corners. Thin ice across the middle.";
                case SkirmishMapId.LushForest:
                    return "Greenveil woods. Tight tree stands, swamp pockets, a centre road.";
                case SkirmishMapId.TwinCities:
                    return "Two cities across a canal. Four bridges. Towers on both banks.";
                case SkirmishMapId.AncientRelic:
                    return "The Reliquary bowl. Cliffs east and west. Jump down into the relic ring.";
                default:
                    return "Skirmish battlefield.";
            }
        }

        private static int SeatCount(MapCatalog.Choice map) => MapCatalog.KeepCount(map.Id);

        private static string SpawnSeatCaption(MapCatalog.Choice map, int seat)
        {
            int n = SeatCount(map);
            seat = Mathf.Clamp(seat, 0, Mathf.Max(0, n - 1));
            if (n > 2)
                return "You: " + SeatName(map, seat) + "\nAI: every other keep";
            return "You: " + SeatName(map, seat)
                   + "\nAI: " + SeatName(map, seat == 0 ? 1 : 0);
        }

        private static string SeatButtonLabel(MapCatalog.Choice map, int seat)
        {
            if (!map.IsBuiltin)
                return seat == 0 ? "WEST" : seat == 1 ? "EAST" : "SEAT " + (seat + 1);
            switch (map.BuiltinId)
            {
                case SkirmishMapId.MundorCapital:
                    if (seat == 0) return "ISLAND";
                    if (seat == 1) return "WEST";
                    return "EAST";
                case SkirmishMapId.OutcastCamp:
                    if (seat == 0) return "CAMP";
                    if (seat == 1) return "N-EAST";
                    if (seat == 2) return "N-WEST";
                    return "S-EAST";
                case SkirmishMapId.FrozenWastes:
                    return seat == 0 ? "N-WEST" : "S-EAST";
                case SkirmishMapId.AncientRelic:
                    return seat == 0 ? "SOUTH" : "NORTH";
                default:
                    return seat == 0 ? "WEST" : "EAST";
            }
        }

        private static string SeatName(MapCatalog.Choice map, int seat)
        {
            switch (SeatButtonLabel(map, seat))
            {
                case "ISLAND": return "island citadel";
                case "NORTH": return "north keep";
                case "SOUTH": return "south keep";
                case "CAMP": return "camp (south-west)";
                case "N-EAST": return "north-east approach";
                case "N-WEST": return "north-west keep";
                case "S-EAST": return "south-east keep";
                case "WEST": return "west keep";
                case "EAST": return "east keep";
                default: return "keep " + (seat + 1);
            }
        }
    }
}
