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
        private int _playerFaction = 1; // Mundor Crown M1 default
        private int _enemyFaction = 1;
        private int _playerTeamColor;
        private int _enemyTeamColor = 1;
        private MapCatalog.Choice _map = MapCatalog.BuiltinChoice(SkirmishMapId.BlackridgePass);
        private AiDifficulty _difficulty = AiDifficulty.Normal;
        private int _spawnSeat;
        private Texture2D _mapPreview;
        private string _previewMapId;
        private AsterraMenuPanels.Overlay _overlay = AsterraMenuPanels.Overlay.None;
        private bool _quitConfirm;

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
                _playerTeamColor = bootstrap.PlayerTeamColorIndex;
                _enemyTeamColor = bootstrap.EnemyTeamColorIndex;
                if (_playerTeamColor == 0 && _enemyTeamColor == 0 && _enemyFaction != 0)
                    _enemyTeamColor = _enemyFaction;
                _map = MapCatalog.FromId(bootstrap.MapKey);
                _difficulty = bootstrap.AiDifficulty;
                _spawnSeat = bootstrap.LocalSpawnSeat;
                // M1: if scene still has pre-slice defaults, snap to Mundor + Blackridge.
                if (_playerFaction == 0 && (_map.Id == MapCatalog.LushForestId || string.IsNullOrEmpty(bootstrap.MapKey)))
                {
                    _playerFaction = 1;
                    _enemyFaction = 0;
                    _map = MapCatalog.BuiltinChoice(SkirmishMapId.BlackridgePass);
                }
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
            _playerFaction = 1; // Mundor
            _enemyFaction = 0; // Uncrowned
            _map = MapCatalog.BuiltinChoice(SkirmishMapId.BlackridgePass);
            RebuildPreviewIfNeeded();
        }

        private void OnDisable()
        {
            DestroyPreview();
        }

        private void DestroyPreview()
        {
            if (_mapPreview != null)
            {
                // Authored menu stills are cached in MenuArt — do not destroy them.
                if (_mapPreview != MenuArt.BlackridgeMapPreview)
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
            if (_map.Id == MapCatalog.BlackridgePassId
                || _map.Id.IndexOf("blackridge", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var art = MenuArt.BlackridgeMapPreview;
                if (art != null)
                {
                    _mapPreview = art;
                    _previewMapId = _map.Id;
                    return;
                }
            }
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

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (_quitConfirm)
                    _quitConfirm = false;
                else if (_overlay != AsterraMenuPanels.Overlay.None)
                    _overlay = AsterraMenuPanels.Overlay.None;
                else if (_view == View.Skirmish || _view == View.Campaign)
                    ShowHub();
                // Esc on Main: do nothing (Quit requires confirm).
            }

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
                GUI.Label(new Rect(x, y + 48f, w, 20f), "THE IRON PATH", _modeStyle);
            else if (_view == View.Campaign)
                GUI.Label(new Rect(x, y + 48f, w, 20f), "CAMPAIGN", _modeStyle);
            else
                GUI.Label(new Rect(x, y + 48f, w, 20f), "SKIRMISH SETUP", _modeStyle);
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


        private void DrawHubDiorama(Rect rect)
        {
            var tex = MenuArt.HubDiorama;
            if (tex == null)
                return;
            float pad = 8f;
            var plate = new Rect(rect.x + rect.width * 0.38f, rect.y + pad, rect.width * 0.62f - pad, rect.height - pad * 2f);
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.92f);
            GUI.DrawTexture(plate, tex, ScaleMode.ScaleAndCrop);
            GUI.color = prev;
            HudStyle.DrawFrame(
                new Rect(rect.x, rect.y, rect.width * 0.42f, rect.height),
                new Color(0.04f, 0.035f, 0.03f, 0.55f),
                new Color(0f, 0f, 0f, 0f),
                0f);
        }

        private void DrawHub(Rect rect)
        {
            // Hub: Skirmish (heavy) + Campaign; Settings/Quit chips. No parchment.
            DrawHubDiorama(rect);
            float stackW = Mathf.Min(360f, rect.width * 0.42f);
            float stackX = rect.x + 24f;
            float y = rect.y + 12f;

            GUI.color = new Color(0.92f, 0.88f, 0.72f, 1f);
            GUI.Label(new Rect(stackX, y, stackW, 44f), "ASTERRA", HudStyle.Title);
            GUI.color = new Color(0.78f, 0.72f, 0.55f, 0.95f);
            GUI.Label(new Rect(stackX, y + 40f, stackW, 24f), "The Iron Path", HudStyle.Body);
            GUI.color = Color.white;
            y += 84f;

            float bh = 56f;
            float gap = 14f;
            if (DrawPrimaryStackButton(new Rect(stackX, y, stackW, bh), "Skirmish"))
            {
                AsterraAudio.PlayUiClick();
                ShowSkirmish();
            }
            y += bh + gap;

            if (DrawPrimaryStackButton(new Rect(stackX, y, stackW, 48f), "Campaign"))
            {
                AsterraAudio.PlayUiClick();
                ShowCampaign();
            }
            y += 48f + gap + 8f;

            float chipW = (stackW - 12f) * 0.5f;
            if (LobbyChip(new Rect(stackX, y, chipW, 32f), "Settings"))
            {
                AsterraAudio.PlayUiClick();
                _overlay = AsterraMenuPanels.Overlay.Options;
            }
            if (LobbyChip(new Rect(stackX + chipW + 12f, y, chipW, 32f), "Quit"))
            {
                AsterraAudio.PlayUiClick();
                _quitConfirm = true;
            }

            if (_quitConfirm)
                DrawQuitConfirm();
        }

        private bool DrawPrimaryStackButton(Rect rect, string label)
        {
            HudClickBlocker.Block(rect);
            bool hover = rect.Contains(Event.current.mousePosition);
            Color fill = hover ? new Color(0.16f, 0.14f, 0.1f, 0.98f) : new Color(0.1f, 0.09f, 0.07f, 0.96f);
            Color border = hover ? new Color(0.85f, 0.72f, 0.38f, 0.95f) : new Color(0.55f, 0.48f, 0.28f, 0.7f);
            HudStyle.DrawFrame(rect, fill, border, 1.5f);
            GUI.Label(rect, label, HudStyle.Button);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void DrawQuitConfirm()
        {
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            HudClickBlocker.Block(screen);
            HudStyle.DrawPanel(screen, new Color(0.02f, 0.03f, 0.04f, 0.72f));
            float w = 360f;
            float h = 160f;
            var box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            HudClickBlocker.Block(box);
            HudStyle.DrawFrame(box, new Color(0.08f, 0.07f, 0.06f, 0.98f), new Color(0.55f, 0.48f, 0.28f, 0.8f), 2f);
            GUI.Label(new Rect(box.x, box.y + 20f, box.width, 28f), "Quit Asterra?", HudStyle.Title);
            GUI.Label(new Rect(box.x + 24f, box.y + 56f, box.width - 48f, 28f), "Leave the desktop client.", HudStyle.Caption);
            if (HudStyle.FrameButton(new Rect(box.x + 24f, box.yMax - 52f, 140f, 34f), "Cancel",
                    new Color(0.18f, 0.19f, 0.2f), new Color(0.45f, 0.48f, 0.5f)))
            {
                AsterraAudio.PlayUiClick();
                _quitConfirm = false;
            }
            if (HudStyle.FrameButton(new Rect(box.xMax - 164f, box.yMax - 52f, 140f, 34f), "Quit",
                    new Color(0.35f, 0.14f, 0.12f), new Color(0.75f, 0.35f, 0.28f)))
            {
                AsterraAudio.PlayUiClick();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
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
            // M1: three columns — Faction | Map | Summary + Start/Back
            float topY = y + 88f;
            float colGap = 14f;
            float colW = (contentW - colGap * 2f) / 3f;
            float colH = Mathf.Max(280f, h - 120f);

            var colFaction = new Rect(contentX, topY, colW, colH);
            var colMap = new Rect(contentX + colW + colGap, topY, colW, colH);
            var colSummary = new Rect(contentX + (colW + colGap) * 2f, topY, colW, colH);

            DrawSkirmishFactionColumn(colFaction);
            DrawSkirmishMapColumn(colMap);
            DrawSkirmishSummaryColumn(colSummary);
        }

        private void DrawSkirmishFactionColumn(Rect rect)
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(rect, new Color(0.06f, 0.07f, 0.08f, 0.94f), new Color(0.55f, 0.48f, 0.28f, 0.5f), 1.5f);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 22f), "FACTION", HudStyle.Subtitle);

            var all = FactionDefaultContent.All;
            // UX: 72px rows, 64×64 crest left, 8px gap, name + Selected/Coming right.
            float tileY = rect.y + 34f;
            const float tileH = 72f;
            const float crestSize = 64f;
            const float crestGap = 8f;
            float gap = 4f; // keep six rows + detail in Column A
            float rowPadX = 8f;
            for (int i = 0; i < all.Length; i++)
            {
                var roster = all[i];
                bool playable = IsSkirmishFactionPlayable(i);
                bool selected = _playerFaction == i;
                var row = new Rect(rect.x + 8f, tileY, rect.width - 16f, tileH);
                HudClickBlocker.Block(row);

                Color facColor = AsterraMeshLibrary.FactionColor((byte)(roster.Id.Value));
                Color fill = selected
                    ? new Color(0.2f, 0.17f, 0.1f, 0.98f)
                    : playable
                        ? new Color(0.1f, 0.11f, 0.12f, 0.92f)
                        : new Color(0.07f, 0.07f, 0.08f, 0.85f);
                Color border = selected
                    ? new Color(0.92f, 0.78f, 0.38f, 0.95f)
                    : playable
                        ? new Color(0.45f, 0.48f, 0.42f, 0.55f)
                        : new Color(0.28f, 0.28f, 0.3f, 0.45f);
                HudStyle.DrawFrame(row, fill, border, selected ? 2f : 1f);
                if (selected)
                {
                    var inset = new Rect(row.x + 3f, row.y + 3f, row.width - 6f, row.height - 6f);
                    HudStyle.DrawPanel(inset, new Color(0.12f, 0.1f, 0.07f, 0.65f));
                }

                float crestX = row.x + rowPadX;
                float crestY = row.y + (tileH - crestSize) * 0.5f;
                var crestRect = new Rect(crestX, crestY, crestSize, crestSize);
                var crest = MenuArt.CrestIcon(roster.DefinitionId, facColor, muted: !playable);
                if (crest != null)
                    GUI.DrawTexture(crestRect, crest, ScaleMode.ScaleToFit);
                else
                    HudStyle.DrawPanel(crestRect, new Color(0.12f, 0.12f, 0.14f, 0.9f));

                if (!playable)
                    DrawCrestLockBadge(crestRect);

                float labelX = crestX + crestSize + crestGap;
                float labelW = row.xMax - labelX - rowPadX;
                var prev = GUI.color;
                GUI.color = playable ? Color.white : new Color(0.65f, 0.65f, 0.68f, 0.9f);
                GUI.Label(new Rect(labelX, row.y + 14f, labelW, 22f), roster.DisplayName, HudStyle.Button);
                GUI.color = playable
                    ? (selected ? new Color(0.92f, 0.82f, 0.45f, 0.95f) : new Color(0.72f, 0.74f, 0.7f, 0.9f))
                    : new Color(0.55f, 0.55f, 0.58f, 0.85f);
                string status = !playable ? "Coming" : selected ? "Selected" : "Playable";
                GUI.Label(new Rect(labelX, row.y + 38f, labelW, 20f), status, HudStyle.Caption);
                GUI.color = prev;

                if (playable && GUI.Button(row, GUIContent.none, GUIStyle.none))
                {
                    _playerFaction = i;
                    if (_enemyFaction == _playerFaction)
                        _enemyFaction = _playerFaction == 1 ? 0 : 1;
                    AsterraAudio.PlayUiClick();
                }
                tileY += tileH + gap;
            }

            // Selected force detail + power one-liner (crests stay on tiles only per UX).
            if (_playerFaction >= 0 && _playerFaction < all.Length)
            {
                var roster = all[_playerFaction];
                float detailY = tileY + 6f;
                GUI.Label(
                    new Rect(rect.x + 12f, detailY, rect.width - 24f, 18f),
                    roster.DisplayName,
                    HudStyle.Subtitle);
                detailY += 20f;
                string power = string.IsNullOrEmpty(roster.PowerDisplayName)
                    ? "Commander ready"
                    : roster.PowerDisplayName;
                GUI.color = new Color(0.9f, 0.82f, 0.45f, 0.95f);
                GUI.Label(new Rect(rect.x + 12f, detailY, rect.width - 24f, 18f), power, HudStyle.Caption);
                GUI.color = Color.white;
                detailY += 20f;
                GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
                GUI.Label(
                    new Rect(rect.x + 12f, detailY, rect.width - 24f, 40f),
                    PowerOneLiner(roster),
                    HudStyle.Caption);
                GUI.color = Color.white;

                detailY += 48f;
                GUI.Label(new Rect(rect.x + 12f, detailY, rect.width - 24f, 18f), "Enemy", HudStyle.Subtitle);
                detailY += 22f;
                // Enemy: playable only
                float ew = (rect.width - 28f - 6f) * 0.5f;
                for (int ei = 0; ei < 2; ei++)
                {
                    var er = new Rect(rect.x + 12f + ei * (ew + 6f), detailY, ew, 28f);
                    bool esel = _enemyFaction == ei;
                    string ename = all[ei].DisplayName;
                    Color fill = esel ? new Color(0.2f, 0.17f, 0.1f, 0.98f) : new Color(0.1f, 0.11f, 0.12f, 0.92f);
                    Color border = esel ? new Color(0.92f, 0.78f, 0.38f, 0.9f) : new Color(0.4f, 0.42f, 0.4f, 0.5f);
                    if (HudStyle.FrameButton(er, ename, fill, border, esel ? 1.5f : 1f))
                    {
                        if (ei != _playerFaction)
                        {
                            _enemyFaction = ei;
                            AsterraAudio.PlayUiClick();
                        }
                    }
                }

                detailY += 36f;
                DrawTeamSwatches(new Rect(rect.x + 12f, detailY, rect.width - 24f, 26f), ref _playerTeamColor);
            }
        }

        private void DrawSkirmishMapColumn(Rect rect)
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(rect, new Color(0.06f, 0.07f, 0.08f, 0.94f), new Color(0.45f, 0.5f, 0.55f, 0.5f), 1.5f);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 22f), "MAP", HudStyle.Subtitle);

            // Honesty: Blackridge playable; other builtins shown grey as Coming.
            var choices = MapCatalog.ListChoices();
            float listY = rect.y + 36f;
            float rowH = 30f;
            float ly = listY;
            float listBottom = rect.y + rect.height * 0.48f;
            for (int i = 0; i < choices.Count; i++)
            {
                if (ly + rowH > listBottom)
                    break;
                var choice = choices[i];
                bool playable = IsSkirmishMapPlayable(choice);
                bool selected = choice.Id == _map.Id;
                var row = new Rect(rect.x + 10f, ly, rect.width - 20f, rowH);
                HudClickBlocker.Block(row);

                Color fill = selected
                    ? new Color(0.2f, 0.17f, 0.1f, 0.98f)
                    : playable
                        ? new Color(0.1f, 0.11f, 0.12f, 0.92f)
                        : new Color(0.07f, 0.07f, 0.08f, 0.85f);
                Color border = selected
                    ? new Color(0.92f, 0.78f, 0.38f, 0.95f)
                    : playable
                        ? new Color(0.45f, 0.48f, 0.42f, 0.55f)
                        : new Color(0.28f, 0.28f, 0.3f, 0.45f);
                HudStyle.DrawFrame(row, fill, border, selected ? 2f : 1f);
                if (selected)
                {
                    var inset = new Rect(row.x + 4f, row.y + 4f, row.width - 8f, row.height - 8f);
                    HudStyle.DrawPanel(inset, new Color(0.12f, 0.1f, 0.07f, 0.65f));
                }

                string label = playable
                    ? StripStar(choice.DisplayName)
                    : StripStar(choice.DisplayName) + "  ·  Coming";
                var prev = GUI.color;
                GUI.color = playable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.85f);
                GUI.Label(new Rect(row.x + 10f, row.y, row.width - 20f, row.height), label, HudStyle.Button);
                GUI.color = prev;

                if (playable && GUI.Button(row, GUIContent.none, GUIStyle.none))
                {
                    _map = choice;
                    _spawnSeat = Mathf.Clamp(_spawnSeat, 0, SeatCount(_map) - 1);
                    RebuildPreviewIfNeeded();
                    AsterraAudio.PlayUiClick();
                }
                ly += rowH + 4f;
            }

            // Ensure selection stays on a playable map
            if (!IsSkirmishMapPlayable(_map))
            {
                _map = MapCatalog.BuiltinChoice(SkirmishMapId.BlackridgePass);
                RebuildPreviewIfNeeded();
            }

            GUI.Label(
                new Rect(rect.x + 12f, ly + 8f, rect.width - 24f, 20f),
                "Selected · " + StripStar(_map.DisplayName),
                HudStyle.Subtitle);
            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
            GUI.Label(
                new Rect(rect.x + 12f, ly + 30f, rect.width - 24f, 36f),
                MapBlurb(_map),
                HudStyle.Caption);
            GUI.color = Color.white;

            float previewY = ly + 72f;
            float previewSize = Mathf.Min(rect.width - 20f, rect.yMax - previewY - 12f, 200f);
            if (previewSize > 80f)
                DrawMapPreview(new Rect(rect.x + 10f, previewY, previewSize, previewSize));
        }

        private void DrawSkirmishSummaryColumn(Rect rect)
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(rect, new Color(0.06f, 0.07f, 0.08f, 0.94f), new Color(0.55f, 0.48f, 0.28f, 0.5f), 1.5f);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 22f), "SUMMARY", HudStyle.Subtitle);

            var playerRoster = FactionDefaultContent.All[_playerFaction % FactionDefaultContent.All.Length];
            var enemyRoster = FactionDefaultContent.All[_enemyFaction % FactionDefaultContent.All.Length];

            float y = rect.y + 40f;
            GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 22f), "You · " + playerRoster.DisplayName, _cardTitleStyle);
            y += 24f;
            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
            GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 18f), "vs " + enemyRoster.DisplayName, HudStyle.Caption);
            GUI.color = Color.white;
            y += 24f;
            GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 22f), StripStar(_map.DisplayName), _cardTitleStyle);
            y += 24f;

            GUI.color = new Color(0.9f, 0.82f, 0.45f, 0.95f);
            GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 18f), playerRoster.PowerDisplayName ?? "Commander", HudStyle.Caption);
            GUI.color = Color.white;
            y += 18f;
            GUI.color = new Color(0.78f, 0.8f, 0.74f, 0.92f);
            GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 36f), PowerOneLiner(playerRoster), HudStyle.Caption);
            GUI.color = Color.white;
            y += 40f;

            GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 18f), "Victory", HudStyle.Subtitle);
            y += 20f;
            GUI.color = new Color(0.82f, 0.84f, 0.78f, 0.95f);
            GUI.Label(
                new Rect(rect.x + 14f, y, rect.width - 28f, 40f),
                "Destroy the enemy keep.\nStandard skirmish · ~15–25 min.",
                HudStyle.Caption);
            GUI.color = Color.white;
            y += 48f;

            DrawDifficultyStrip(new Rect(rect.x + 10f, y, rect.width - 20f, 72f));
            y += 84f;

            int seats = SeatCount(_map);
            GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 18f), "Spawn", HudStyle.Subtitle);
            y += 22f;
            float bw = Mathf.Min(72f, (rect.width - 28f - (seats - 1) * 6f) / Mathf.Max(1, seats));
            for (int i = 0; i < seats; i++)
            {
                var r = new Rect(rect.x + 14f + i * (bw + 6f), y, bw, 26f);
                bool picked = _spawnSeat == i;
                if (picked)
                    HudStyle.DrawFrame(r, new Color(0.22f, 0.18f, 0.1f, 0.95f), new Color(0.9f, 0.75f, 0.35f, 0.8f), 1.5f);
                if (GUI.Button(r, SeatButtonLabel(_map, i)))
                {
                    _spawnSeat = i;
                    AsterraAudio.PlayUiClick();
                }
            }

            float btnW = rect.width - 28f;
            float btnX = rect.x + 14f;
            float actionsBottom = rect.yMax - 14f;
            float startH = 44f;
            float backH = 32f;
            float startY = actionsBottom - startH;
            float backY = startY - 10f - backH;

            if (LobbyChip(new Rect(btnX, backY, btnW * 0.42f, backH), "Back"))
            {
                AsterraAudio.PlayUiClick();
                ShowHub();
                return;
            }

            bool hasSave = Asterra.Gameplay.Save.OfflineMatchSaveService.HasQuickSave;
            if (hasSave && LobbyChip(new Rect(btnX + btnW * 0.48f, backY, btnW * 0.52f, backH), "Continue"))
            {
                AsterraAudio.Play(AsterraSfx.OrderTrain, 0.8f);
                if (bootstrap.LoadOfflineQuick())
                    enabled = false;
                return;
            }

            bool canStart = CanStartSkirmish();
            if (DrawStartButton(new Rect(btnX, startY, btnW, startH), "START", canStart) && canStart)
            {
                AsterraAudio.Play(AsterraSfx.OrderTrain, 0.8f);
                bootstrap.ConfigureAndStartOffline(
                    _playerFaction, _enemyFaction, _map.Id, _difficulty, _spawnSeat,
                    _playerTeamColor, _enemyTeamColor);
                enabled = false;
            }
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
            ref int factionIndex,
            ref int teamColorIndex)
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
            GUI.Label(new Rect(rect.x + 18f, rect.y + 100f, rect.width - 36f, 18f), power, HudStyle.Caption);
            GUI.color = Color.white;

            DrawTeamSwatches(new Rect(rect.x + 14f, rect.y + 120f, rect.width - 28f, 26f), ref teamColorIndex);

            var prev = new Rect(rect.x + 18f, rect.yMax - 34f, 44f, 24f);
            var next = new Rect(rect.xMax - 62f, rect.yMax - 34f, 44f, 24f);
            var mid = new Rect(rect.x + 70f, rect.yMax - 34f, rect.width - 140f, 24f);
            HudStyle.DrawPanel(mid, new Color(0.1f, 0.12f, 0.13f, 0.9f));
            GUI.Label(mid, "Change force", HudStyle.Subtitle);

            if (CycleChip(prev, "‹"))
                CycleFaction(ref factionIndex, -1);
            if (CycleChip(next, "›"))
                CycleFaction(ref factionIndex, +1);
        }

        private void DrawTeamSwatches(Rect row, ref int colorIndex)
        {
            int n = AsterraMeshLibrary.TeamSwatchCount;
            float s = Mathf.Min(22f, (row.width - (n - 1) * 4f) / n);
            float total = n * s + (n - 1) * 4f;
            float x = row.x + (row.width - total) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var r = new Rect(x + i * (s + 4f), row.y + (row.height - s) * 0.5f, s, s);
                HudClickBlocker.Block(r);
                Color c = AsterraMeshLibrary.TeamSwatch(i);
                bool picked = colorIndex == i;
                HudStyle.DrawFrame(
                    r,
                    c,
                    picked ? Color.white : new Color(0.05f, 0.05f, 0.05f, 0.7f),
                    picked ? 2f : 1f);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    colorIndex = i;
                    AsterraAudio.PlayUiClick();
                }
            }
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
            Color youCol = AsterraMeshLibrary.TeamSwatch(_playerTeamColor);
            Color aiCol = AsterraMeshLibrary.TeamSwatch(_enemyTeamColor);
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

        private bool DrawStartButton(Rect rect, string label = "START SKIRMISH", bool enabled = true)
        {
            HudClickBlocker.Block(rect);
            Color fill = enabled
                ? new Color(0.22f, 0.18f, 0.08f, 0.98f)
                : new Color(0.12f, 0.12f, 0.12f, 0.85f);
            Color border = enabled
                ? new Color(0.9f, 0.75f, 0.35f, 0.85f)
                : new Color(0.35f, 0.35f, 0.35f, 0.55f);
            HudStyle.DrawFrame(rect, fill, border, enabled ? 2f : 1f);
            if (enabled)
                HudStyle.DrawAccentBar(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.95f, 0.8f, 0.35f, 1f));
            var prev = GUI.color;
            GUI.color = enabled ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.8f);
            bool clicked = enabled && GUI.Button(rect, label, _startStyle);
            GUI.color = prev;
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


        // M1 honesty: only Mundor (1) + Uncrowned (0) are playable in skirmish.
        private static bool IsSkirmishFactionPlayable(int index)
        {
            int n = FactionDefaultContent.All.Length;
            if (n <= 0) return false;
            int i = ((index % n) + n) % n;
            return i == 0 || i == 1;
        }

        private static bool IsSkirmishMapPlayable(MapCatalog.Choice map)
        {
            return map.IsBuiltin && map.BuiltinId == SkirmishMapId.BlackridgePass;
        }

        private static string PowerOneLiner(FactionRoster roster)
        {
            if (roster == null) return string.Empty;
            if (roster.PowerDisplayName == "Royal Standard")
                return "Plant a banner: nearby allies hold the line.";
            if (roster.PowerDisplayName == "Wrath of Skies")
                return "Call down a strike on a chosen ground point.";
            if (string.IsNullOrEmpty(roster.PowerDisplayName))
                return "Commander power locked for this force.";
            return "Commander power — details in match.";
        }

        private bool CanStartSkirmish()
        {
            return IsSkirmishFactionPlayable(_playerFaction)
                   && IsSkirmishFactionPlayable(_enemyFaction)
                   && IsSkirmishMapPlayable(_map)
                   && _playerFaction != _enemyFaction;
        }

        private static void CycleFaction(ref int index, int delta)
        {
            int n = FactionDefaultContent.All.Length;
            if (n <= 0) return;
            // Skip Coming factions in M1 skirmish.
            for (int step = 0; step < n; step++)
            {
                index = (index + delta) % n;
                if (index < 0) index += n;
                if (IsSkirmishFactionPlayable(index))
                    return;
            }
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
                case SkirmishMapId.BlackridgePass:
                    return "Mountain pass between Crownlands and Iron Frontier. Chokepoint mid-ridge, high ground on the flanks.";
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
                case SkirmishMapId.BlackridgePass:
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
