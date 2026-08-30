using System.Collections.Generic;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Shared OnGUI chrome: panels, icon command cards, selection portraits, layout scale.</summary>
    public static class HudStyle
    {
        public static readonly Color PanelFill = new Color(0.06f, 0.075f, 0.09f, 0.94f);
        public static readonly Color PanelFillDeep = new Color(0.035f, 0.045f, 0.055f, 0.96f);
        public static readonly Color PanelBorder = new Color(0.42f, 0.38f, 0.28f, 0.55f);
        public static readonly Color Accent = new Color(0.86f, 0.72f, 0.38f, 1f);
        public static readonly Color AccentSoft = new Color(0.86f, 0.72f, 0.38f, 0.35f);
        public static readonly Color Gold = new Color(0.95f, 0.82f, 0.35f, 1f);
        public static readonly Color Timber = new Color(0.55f, 0.78f, 0.42f, 1f);
        public static readonly Color Hp = new Color(0.32f, 0.82f, 0.42f, 0.95f);
        public static readonly Color Danger = new Color(0.85f, 0.32f, 0.28f, 0.95f);
        public static readonly Color Text = new Color(0.9f, 0.91f, 0.88f, 1f);
        public static readonly Color TextDim = new Color(0.7f, 0.72f, 0.68f, 0.85f);

        private static GUIStyle _panel;
        private static GUIStyle _title;
        private static GUIStyle _label;
        private static GUIStyle _button;
        private static GUIStyle _flatButton;
        private static GUIStyle _toast;
        private static GUIStyle _subtitle;
        private static GUIStyle _caption;
        private static GUIStyle _body;
        private static GUIStyle _cardLabel;
        private static GUIStyle _resourceLabel;
        private static Texture2D _white;
        private static readonly Dictionary<string, Texture2D> Icons = new();
        private static float _appliedScale = -1f;

        public static float Scale => AsterraSettings.UiScale;
        public static float S(float pixels) => pixels * Scale;
        public static float MinimapSize => S(196f);
        public static float MinimapMargin => S(14f);

        public static Rect MinimapRect =>
            new Rect(
                Screen.width - MinimapSize - MinimapMargin,
                Screen.height - MinimapSize - MinimapMargin,
                MinimapSize,
                MinimapSize);

        public static float ContentRight => Screen.width - MinimapSize - MinimapMargin * 2f;
        public static float CommandDockHeight => S(196f);

        public static void InvalidateScale() => _appliedScale = -1f;

        public static void Ensure()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }

            float scale = Scale;
            bool needStyles = _panel == null || !Mathf.Approximately(scale, _appliedScale);
            if (!needStyles)
                return;

            _appliedScale = scale;
            int fs = Mathf.RoundToInt(13f * scale);
            int fsTitle = Mathf.RoundToInt(24f * scale);
            int fsCaption = Mathf.RoundToInt(11f * scale);
            int fsButton = Mathf.RoundToInt(12f * scale);
            int fsToast = Mathf.RoundToInt(14f * scale);
            int fsCard = Mathf.RoundToInt(11f * scale);
            int fsRes = Mathf.RoundToInt(15f * scale);

            _panel = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = fs,
                padding = new RectOffset(10, 10, 8, 8),
            };

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = fsTitle,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _title.normal.textColor = Accent;

            _label = new GUIStyle(GUI.skin.label) { fontSize = fs };
            _label.normal.textColor = Text;

            _subtitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(12f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _subtitle.normal.textColor = Text;

            _caption = new GUIStyle(GUI.skin.label)
            {
                fontSize = fsCaption,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
            };
            _caption.normal.textColor = TextDim;

            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = fs,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
            };
            _body.normal.textColor = Text;

            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = fsButton,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 4, 4),
            };

            _flatButton = new GUIStyle(GUIStyle.none)
            {
                fontSize = fsButton,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 4, 4),
                clipping = TextClipping.Clip,
            };
            _flatButton.normal.textColor = Text;
            _flatButton.hover.textColor = Color.white;
            _flatButton.active.textColor = Accent;
            _flatButton.focused.textColor = Text;

            _cardLabel = new GUIStyle(GUIStyle.none)
            {
                fontSize = fsCard,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = true,
            };
            _cardLabel.normal.textColor = Text;
            _cardLabel.hover.textColor = Color.white;
            _cardLabel.active.textColor = Accent;

            _resourceLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = fsRes,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            _resourceLabel.normal.textColor = Text;

            _toast = new GUIStyle(GUI.skin.box)
            {
                fontSize = fsToast,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
        }

        public static GUIStyle Panel { get { Ensure(); return _panel; } }
        public static GUIStyle Title { get { Ensure(); return _title; } }
        public static GUIStyle Label { get { Ensure(); return _label; } }
        public static GUIStyle Subtitle { get { Ensure(); return _subtitle; } }
        public static GUIStyle Caption { get { Ensure(); return _caption; } }
        public static GUIStyle Body { get { Ensure(); return _body; } }
        public static GUIStyle Button { get { Ensure(); return _button; } }
        public static GUIStyle FlatButton { get { Ensure(); return _flatButton; } }
        public static GUIStyle CardLabel { get { Ensure(); return _cardLabel; } }
        public static GUIStyle ResourceLabel { get { Ensure(); return _resourceLabel; } }
        public static GUIStyle Toast { get { Ensure(); return _toast; } }

        public static bool PanelButton(Rect rect, string label, Color fill)
        {
            Ensure();
            HudClickBlocker.Block(rect);
            bool hover = Event.current != null && rect.Contains(Event.current.mousePosition);
            DrawPanel(rect, hover ? Color.Lerp(fill, Color.white, 0.08f) : fill);
            return GUI.Button(rect, label, FlatButton);
        }

        public static bool FrameButton(Rect rect, string label, Color fill, Color border, float borderWidth = 1f)
        {
            Ensure();
            HudClickBlocker.Block(rect);
            bool hover = Event.current != null && rect.Contains(Event.current.mousePosition);
            DrawFrame(
                rect,
                hover ? Color.Lerp(fill, Color.white, 0.1f) : fill,
                border,
                borderWidth);
            return GUI.Button(rect, label, FlatButton);
        }

        public static bool CommandCard(
            Rect rect,
            string iconKey,
            string label,
            Color accent,
            out bool hovered,
            bool enabled = true,
            bool selected = false)
        {
            Ensure();
            HudClickBlocker.Block(rect);
            hovered = Event.current != null && rect.Contains(Event.current.mousePosition);
            Color fill = selected
                ? Color.Lerp(PanelFill, accent, 0.28f)
                : hovered && enabled
                    ? Color.Lerp(PanelFill, Color.white, 0.1f)
                    : PanelFill;
            Color border = selected || (hovered && enabled)
                ? Color.Lerp(accent, Color.white, 0.25f)
                : PanelBorder;
            DrawFrame(rect, fill, border, selected ? 2f : 1f);
            DrawAccentBar(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, S(2f)), AccentSoft);

            float iconSize = Mathf.Min(S(28f), rect.height * 0.42f);
            float iconX = rect.x + (rect.width - iconSize) * 0.5f;
            float iconY = rect.y + S(8f);
            Color iconColor = enabled ? accent : Color.Lerp(accent, Color.black, 0.55f);
            GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), GetIcon(iconKey, iconColor));

            var prev = GUI.color;
            GUI.color = enabled ? Text : TextDim;
            GUI.Label(
                new Rect(rect.x + S(3f), rect.yMax - S(28f), rect.width - S(6f), S(26f)),
                label,
                CardLabel);
            GUI.color = prev;

            // FlatButton (not GUIStyle.none) — none often fails to receive clicks in IMGUI.
            bool clicked = false;
            if (enabled)
            {
                var prevCol = GUI.color;
                GUI.color = Color.clear;
                clicked = GUI.Button(rect, GUIContent.none, FlatButton);
                GUI.color = prevCol;
            }

            return clicked;
        }

        public static void ResourcePill(Rect rect, string iconKey, string value, Color accent)
        {
            Ensure();
            DrawFrame(rect, PanelFillDeep, PanelBorder, 1f);
            float icon = Mathf.Min(S(18f), rect.height - S(8f));
            GUI.DrawTexture(
                new Rect(rect.x + S(8f), rect.y + (rect.height - icon) * 0.5f, icon, icon),
                GetIcon(iconKey, accent));
            GUI.Label(
                new Rect(rect.x + S(32f), rect.y, rect.width - S(36f), rect.height),
                value,
                ResourceLabel);
        }

        public static void DrawPanel(Rect rect, Color fill)
        {
            Ensure();
            var old = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(rect, _white);
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), _white);
            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), _white);
            GUI.color = old;
        }

        public static void DrawFrame(Rect rect, Color fill, Color border, float borderWidth = 1f)
        {
            Ensure();
            var old = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(rect, _white);
            GUI.color = border;
            float b = Mathf.Max(1f, borderWidth);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, b), _white);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - b, rect.width, b), _white);
            GUI.DrawTexture(new Rect(rect.x, rect.y, b, rect.height), _white);
            GUI.DrawTexture(new Rect(rect.xMax - b, rect.y, b, rect.height), _white);
            GUI.color = old;
        }

        public static void DrawAccentBar(Rect rect, Color accent)
        {
            Ensure();
            var old = GUI.color;
            GUI.color = accent;
            GUI.DrawTexture(rect, _white);
            GUI.color = old;
        }

        public static bool IconButton(Rect rect, string iconKey, string label, Color accent)
        {
            Ensure();
            HudClickBlocker.Block(rect);
            bool hover = Event.current != null && rect.Contains(Event.current.mousePosition);
            DrawPanel(rect, hover
                ? new Color(0.12f, 0.14f, 0.16f, 0.95f)
                : new Color(0.08f, 0.1f, 0.12f, 0.92f));
            var icon = GetIcon(iconKey, accent);
            float iconSize = Mathf.Min(22f, rect.height - 8f);
            GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize), icon);
            var old = GUI.color;
            GUI.color = Text;
            bool clicked = GUI.Button(rect, "    " + label, FlatButton);
            GUI.color = old;
            return clicked;
        }

        public static Texture2D GetIcon(string key, Color accent)
        {
            Ensure();
            string cacheKey = key + "#" + ColorUtility.ToHtmlStringRGBA(accent);
            if (Icons.TryGetValue(cacheKey, out var tex) && tex != null)
                return tex;
            tex = BuildIcon(key, accent);
            Icons[cacheKey] = tex;
            return tex;
        }

        public static Texture2D Portrait(string definitionId, Color faction)
        {
            string key = "port_" + (definitionId ?? "unit");
            if (Icons.TryGetValue(key, out var tex) && tex != null)
                return tex;
            tex = BuildPortrait(definitionId, faction);
            Icons[key] = tex;
            return tex;
        }

        private static Texture2D BuildIcon(string key, Color accent)
        {
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, clear);

            void Pix(int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= s || y >= s)
                    return;
                tex.SetPixel(x, y, c);
            }

            void Fill(int x0, int y0, int x1, int y1, Color c)
            {
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Pix(x, y, c);
            }

            Color ink = Color.Lerp(accent, Color.white, 0.25f);
            switch (key)
            {
                case "sword":
                    Fill(15, 4, 17, 24, ink);
                    Fill(12, 10, 20, 12, ink);
                    Fill(14, 24, 18, 28, Color.Lerp(accent, Color.black, 0.3f));
                    break;
                case "bow":
                    Fill(8, 6, 12, 26, ink);
                    Fill(12, 8, 24, 10, ink);
                    Fill(12, 22, 24, 24, ink);
                    Fill(22, 10, 24, 22, ink);
                    break;
                case "horse":
                    Fill(6, 16, 26, 24, ink);
                    Fill(18, 8, 26, 16, ink);
                    Fill(22, 4, 26, 8, ink);
                    break;
                case "elite":
                    Fill(12, 8, 20, 26, ink);
                    Fill(10, 4, 22, 10, Color.Lerp(accent, Color.yellow, 0.45f));
                    break;
                case "siege":
                    Fill(6, 18, 26, 26, ink);
                    Fill(10, 8, 22, 18, ink);
                    Fill(20, 4, 28, 10, Color.Lerp(accent, Color.black, 0.2f));
                    break;
                case "scout":
                    Fill(14, 6, 18, 26, ink);
                    Fill(8, 12, 24, 16, ink);
                    Fill(20, 6, 26, 12, Color.Lerp(accent, Color.cyan, 0.35f));
                    break;
                case "sapper":
                    Fill(10, 8, 22, 14, ink);
                    Fill(14, 14, 18, 28, ink);
                    Fill(8, 20, 24, 24, Color.Lerp(accent, Color.yellow, 0.25f));
                    break;
                case "shield":
                    Fill(8, 6, 24, 22, ink);
                    Fill(10, 8, 22, 20, Color.Lerp(accent, Color.black, 0.35f));
                    break;
                case "hammer":
                    Fill(8, 8, 24, 14, ink);
                    Fill(14, 14, 18, 28, ink);
                    break;
                case "tower":
                    Fill(12, 6, 20, 26, ink);
                    Fill(10, 4, 22, 8, ink);
                    break;
                case "wall":
                    Fill(4, 12, 28, 24, ink);
                    Fill(6, 8, 10, 12, ink);
                    Fill(14, 8, 18, 12, ink);
                    Fill(22, 8, 26, 12, ink);
                    break;
                case "outpost":
                    Fill(10, 10, 22, 24, ink);
                    Fill(15, 4, 17, 10, ink);
                    Fill(17, 4, 26, 8, Color.Lerp(accent, Color.yellow, 0.4f));
                    break;
                case "bridge":
                    Fill(4, 14, 28, 18, ink);
                    Fill(6, 10, 10, 22, ink);
                    Fill(22, 10, 26, 22, ink);
                    break;
                case "trench":
                    Fill(4, 18, 28, 26, Color.Lerp(accent, Color.black, 0.35f));
                    Fill(8, 12, 24, 18, ink);
                    break;
                case "barricade":
                    Fill(6, 10, 26, 24, ink);
                    Fill(10, 6, 14, 10, ink);
                    Fill(18, 6, 22, 10, ink);
                    break;
                case "ferry":
                    Fill(6, 16, 26, 24, ink);
                    Fill(10, 10, 22, 16, Color.Lerp(accent, Color.cyan, 0.3f));
                    break;
                case "earth":
                    Fill(6, 18, 26, 26, ink);
                    Fill(10, 10, 22, 18, Color.Lerp(accent, Color.green, 0.2f));
                    break;
                case "more":
                    Fill(8, 14, 12, 18, ink);
                    Fill(14, 14, 18, 18, ink);
                    Fill(20, 14, 24, 18, ink);
                    break;
                case "back":
                    Fill(8, 14, 20, 18, ink);
                    Fill(8, 10, 14, 22, ink);
                    break;
                case "gear":
                    Fill(12, 8, 20, 24, ink);
                    Fill(8, 12, 24, 20, ink);
                    break;
                case "admin":
                    Fill(10, 6, 22, 26, ink);
                    Fill(14, 10, 18, 22, Color.Lerp(accent, Color.black, 0.4f));
                    break;
                case "gold":
                    Fill(8, 8, 24, 24, Color.Lerp(accent, Color.yellow, 0.35f));
                    Fill(12, 12, 20, 20, Color.Lerp(accent, Color.black, 0.25f));
                    break;
                case "timber":
                    Fill(10, 6, 22, 26, ink);
                    Fill(8, 10, 24, 14, Color.Lerp(accent, Color.black, 0.2f));
                    Fill(8, 18, 24, 22, Color.Lerp(accent, Color.black, 0.2f));
                    break;
                case "worker":
                case "idle":
                    Fill(12, 6, 20, 14, ink);
                    Fill(10, 14, 22, 26, ink);
                    break;
                case "leader":
                    Fill(12, 8, 20, 26, ink);
                    Fill(10, 4, 22, 10, Color.Lerp(accent, Color.yellow, 0.5f));
                    break;
                case "research":
                    Fill(10, 8, 22, 24, ink);
                    Fill(14, 4, 18, 10, Color.Lerp(accent, Color.cyan, 0.4f));
                    break;
                case "power":
                    Fill(15, 4, 17, 28, Color.Lerp(accent, Color.yellow, 0.5f));
                    Fill(8, 12, 24, 16, Color.Lerp(accent, Color.yellow, 0.5f));
                    break;
                case "stop":
                    Fill(8, 8, 24, 24, ink);
                    break;
                case "stance":
                    Fill(8, 16, 24, 24, ink);
                    Fill(14, 6, 18, 16, ink);
                    break;
                case "hold":
                    Fill(10, 8, 22, 24, ink);
                    Fill(14, 12, 18, 20, Color.Lerp(accent, Color.black, 0.35f));
                    break;
                case "cancel":
                    Fill(8, 8, 12, 12, ink);
                    Fill(20, 8, 24, 12, ink);
                    Fill(8, 20, 12, 24, ink);
                    Fill(20, 20, 24, 24, ink);
                    Fill(12, 12, 20, 20, ink);
                    break;
                case "unload":
                    Fill(8, 8, 24, 14, ink);
                    Fill(12, 14, 20, 26, ink);
                    break;
                case "demolish":
                    Fill(8, 10, 24, 22, ink);
                    Fill(14, 4, 18, 10, Danger);
                    break;
                case "stone":
                    Fill(8, 12, 24, 24, ink);
                    Fill(10, 8, 22, 12, Color.Lerp(accent, Color.white, 0.2f));
                    break;
                case "turret":
                    Fill(12, 10, 20, 26, ink);
                    Fill(8, 6, 24, 12, ink);
                    break;
                case "repair":
                    Fill(8, 14, 24, 18, ink);
                    Fill(14, 8, 18, 24, ink);
                    break;
                default:
                    Fill(10, 10, 22, 22, ink);
                    break;
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D BuildPortrait(string definitionId, Color faction)
        {
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color deep = new Color(0.04f, 0.05f, 0.06f, 1f);
            Color bg = Color.Lerp(faction, deep, 0.55f);
            Color rim = Color.Lerp(faction, Color.white, 0.45f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool outer = x == 0 || y == 0 || x == s - 1 || y == s - 1;
                bool border = x < 2 || y < 2 || x >= s - 2 || y >= s - 2;
                if (outer)
                    tex.SetPixel(x, y, Color.Lerp(rim, Color.black, 0.35f));
                else if (border)
                    tex.SetPixel(x, y, rim);
                else
                {
                    float t = y / (float)(s - 1);
                    tex.SetPixel(x, y, Color.Lerp(bg, deep, t * 0.35f));
                }
            }

            Color body = Color.Lerp(faction, Color.white, 0.22f);
            string id = definitionId ?? string.Empty;
            if (id.Contains("archer") || id.Contains("ranger") || id.Contains("acolyte") || id.Contains("mage"))
            {
                FillRect(tex, 18, 10, 30, 38, body);
                FillRect(tex, 26, 12, 34, 36, Color.Lerp(body, Color.cyan, 0.35f));
            }
            else if (id.Contains("knight") || id.Contains("cavalry") || id.Contains("rider"))
            {
                FillRect(tex, 8, 22, 40, 34, body);
                FillRect(tex, 16, 10, 30, 24, Color.Lerp(body, Color.white, 0.2f));
            }
            else if (id.Contains("catapult") || id.Contains("siege") || id.Contains("ballista") || id.Contains("guardian"))
            {
                FillRect(tex, 6, 20, 42, 34, body);
                FillRect(tex, 20, 8, 36, 22, Color.Lerp(body, Color.black, 0.2f));
            }
            else if (id.Contains("builder") || id.Contains("sapper") || id.Contains("pathfinder"))
            {
                FillRect(tex, 14, 12, 34, 36, body);
                FillRect(tex, 30, 16, 40, 24, Color.Lerp(body, Color.yellow, 0.3f));
            }
            else if (id.Contains("heir") || id.Contains("captain") || id.Contains("leader")
                     || id.Contains("king") || id.Contains("priest"))
            {
                FillRect(tex, 14, 12, 34, 38, body);
                FillRect(tex, 12, 6, 36, 14, Color.Lerp(faction, Color.yellow, 0.55f));
            }
            else
            {
                FillRect(tex, 14, 10, 34, 38, body);
                FillRect(tex, 8, 18, 16, 30, Color.Lerp(body, Color.white, 0.15f));
            }

            tex.Apply();
            return tex;
        }

        private static void FillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
                    continue;
                tex.SetPixel(x, y, c);
            }
        }
    }
}
