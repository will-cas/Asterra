using Asterra.AI;
using Asterra.Gameplay.Audio;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Pre-match lobby: faction cards, map picker, AI difficulty, start offline skirmish.</summary>
    public sealed class OfflineMatchMenu : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap bootstrap;

        private int _playerFaction;
        private int _enemyFaction = 1;
        private MapCatalog.Choice _map = MapCatalog.BuiltinChoice(SkirmishMapId.BlackridgePass);
        private AiDifficulty _difficulty = AiDifficulty.Normal;

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
            }

            _ = AsterraAudio.Instance;
        }

        private void OnGUI()
        {
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<MatchBootstrap>();
            if (bootstrap == null || bootstrap.IsMatchRunning || bootstrap.Result.IsOver)
                return;

            HudStyle.Ensure();
            EnsureLocalStyles();

            HudStyle.DrawPanel(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.03f, 0.04f, 0.55f));

            float w = Mathf.Min(920f, Screen.width - 48f);
            float h = Mathf.Min(660f, Screen.height - 48f);
            float x = (Screen.width - w) * 0.5f;
            float y = Mathf.Max(20f, (Screen.height - h) * 0.5f - 8f);
            var panel = new Rect(x, y, w, h);
            HudClickBlocker.Block(panel);

            HudStyle.DrawFrame(
                panel,
                new Color(0.04f, 0.055f, 0.065f, 0.96f),
                new Color(0.55f, 0.48f, 0.28f, 0.55f),
                2f);
            HudStyle.DrawAccentBar(new Rect(x, y, w, 3f), new Color(0.78f, 0.66f, 0.32f, 0.9f));

            GUI.Label(new Rect(x, y + 18f, w, 40f), "ASTERRA", _brandStyle);
            GUI.Label(new Rect(x, y + 54f, w, 22f), "OFFLINE SKIRMISH", _modeStyle);
            HudStyle.DrawAccentBar(
                new Rect(x + w * 0.5f - 48f, y + 80f, 96f, 2f),
                new Color(0.78f, 0.66f, 0.32f, 0.65f));

            float contentX = x + 28f;
            float contentW = w - 56f;
            float cardY = y + 100f;
            float cardH = 190f;
            float gap = 16f;
            float cardW = (contentW - gap * 2f - 56f) * 0.5f;

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
            DrawVsBadge(new Rect(contentX + cardW + gap, cardY + cardH * 0.5f - 22f, 56f, 44f));
            DrawFactionCard(
                new Rect(contentX + cardW + gap + 56f + gap, cardY, cardW, cardH),
                "ENEMY FORCE",
                enemyRoster,
                enemyColor,
                ref _enemyFaction);

            float stripY = cardY + cardH + 14f;
            DrawMapStrip(new Rect(contentX, stripY, contentW, 92f));
            DrawDifficultyStrip(new Rect(contentX, stripY + 100f, contentW, 72f));

            float startW = Mathf.Min(320f, contentW);
            float startX = x + (w - startW) * 0.5f;
            float startY = y + h - 72f;
            if (DrawStartButton(new Rect(startX, startY, startW, 48f)))
            {
                AsterraAudio.Play(AsterraSfx.OrderTrain, 0.8f);
                bootstrap.ConfigureAndStartOffline(_playerFaction, _enemyFaction, _map.Id, _difficulty);
                enabled = false;
            }

            bool hasSave = Asterra.Gameplay.Save.OfflineMatchSaveService.HasQuickSave;
            var loadRect = new Rect(startX, startY - 40f, startW, 32f);
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
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                DrawSecondaryButton(loadRect, "NO SAVE FOUND");
                GUI.color = Color.white;
            }

            GUI.color = new Color(0.7f, 0.72f, 0.68f, 0.75f);
            GUI.Label(
                new Rect(x + 28f, y + h - 22f, w - 56f, 18f),
                "Use ‹ › to cycle  ·  F5 save / F9 load in match  ·  Designer maps: DESIGNER",
                HudStyle.Caption);
            GUI.color = Color.white;
        }

        private bool DrawSecondaryButton(Rect rect, string label)
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(
                rect,
                new Color(0.1f, 0.12f, 0.13f, 0.95f),
                new Color(0.55f, 0.5f, 0.35f, 0.45f),
                1f);
            bool clicked = GUI.Button(rect, label, HudStyle.Button);
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
                new Rect(rect.x + 16f, rect.y + 56f, rect.width - 160f, 28f),
                MapBlurb(_map),
                HudStyle.Caption);
            GUI.color = Color.white;

            if (CycleChip(new Rect(rect.xMax - 120f, rect.y + 28f, 44f, 36f), "‹"))
                _map = PreviousMap(_map);
            if (CycleChip(new Rect(rect.xMax - 68f, rect.y + 28f, 44f, 36f), "›"))
                _map = MapCatalog.Next(_map);
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

        private bool DrawStartButton(Rect rect)
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(
                rect,
                new Color(0.22f, 0.18f, 0.08f, 0.98f),
                new Color(0.9f, 0.75f, 0.35f, 0.85f),
                2f);
            HudStyle.DrawAccentBar(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.95f, 0.8f, 0.35f, 1f));
            bool clicked = GUI.Button(rect, "START SKIRMISH", _startStyle);
            if (clicked)
                AsterraAudio.PlayUiClick();
            return clicked;
        }

        private static bool CycleChip(Rect rect, string label)
        {
            HudClickBlocker.Block(rect);
            HudStyle.DrawFrame(
                rect,
                new Color(0.12f, 0.14f, 0.15f, 0.95f),
                new Color(0.55f, 0.5f, 0.35f, 0.5f),
                1f);
            bool clicked = GUI.Button(rect, label, HudStyle.Button);
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
                case SkirmishMapId.TwinKeeps:
                    return "Open basin duel. Contest the center territory and flank through forest.";
                case SkirmishMapId.RiverCrossing:
                    return "East–west river with fords and boats. Control the crossings.";
                case SkirmishMapId.BlackridgePass:
                    return "Mountain choke and fortress mouths. Hold the pass or die trying.";
                default:
                    return "Skirmish battlefield.";
            }
        }
    }
}
