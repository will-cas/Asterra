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

            float iconSize = Mathf.Min(S(26f), rect.height * 0.38f);
            float iconX = rect.x + (rect.width - iconSize) * 0.5f;
            float iconY = rect.y + S(6f);
            Color iconColor = enabled ? accent : Color.Lerp(accent, Color.black, 0.55f);
            GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), GetIcon(iconKey, iconColor));

            var prev = GUI.color;
            GUI.color = enabled ? Text : TextDim;
            GUI.Label(
                new Rect(rect.x + S(2f), rect.yMax - S(32f), rect.width - S(4f), S(30f)),
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
            tex = LoadAndColorizeIcon(key, accent);
            Icons[cacheKey] = tex;
            return tex;
        }

        public static Texture2D Portrait(string definitionId, Color faction)
        {
            string iconKey = PortraitIconKey(definitionId);
            string cacheKey = "port_" + iconKey + "#" + ColorUtility.ToHtmlStringRGBA(faction);
            if (Icons.TryGetValue(cacheKey, out var tex) && tex != null)
                return tex;
            tex = LoadAndColorizeIcon(iconKey, faction);
            Icons[cacheKey] = tex;
            return tex;
        }

        private static string PortraitIconKey(string definitionId)
        {
            string id = definitionId ?? string.Empty;
            if (id.Contains("builder") || id.Contains("pathfinder"))
                return "worker";
            if (id.Contains("archer") || id.Contains("ranger") || id.Contains("acolyte"))
                return "bow";
            if (id.Contains("ashen_knight") || id.Contains("mage"))
                return "research";
            if (id.Contains("knight") || id.Contains("cavalry") || id.Contains("rider"))
                return "horse";
            if (id.Contains("catapult") || id.Contains("siege") || id.Contains("ballista") || id.Contains("guardian"))
                return "siege";
            if (id.Contains("sapper"))
                return "sapper";
            if (id.Contains("lucien") || id.Contains("captain") || id.Contains("hierophant") || id.Contains("leader")
                || id.Contains("vale") || id.Contains("flame"))
                return "leader";
            if (id.Contains("scout"))
                return "scout";
            if (id.Contains("dryad"))
                return "timber";
            if (id.Contains("ember"))
                return "sapper";
            return "sword";
        }

        private static Texture2D LoadAndColorizeIcon(string key, Color accent)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[Asterra] Missing icon key (empty)");
                return MakeClearIcon();
            }

            var src = Resources.Load<Texture2D>("Asterra/Icons/" + key);
            if (src == null)
            {
                Debug.LogError($"[Asterra] Missing icon Resources/Asterra/Icons/{key}");
                return MakeClearIcon();
            }

            var readable = MakeReadableCopy(src);
            var pixels = readable.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a < 8)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                    continue;
                }

                pixels[i] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.r / 255f * accent.r * 255f), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.g / 255f * accent.g * 255f), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(p.b / 255f * accent.b * 255f), 0, 255),
                    p.a);
            }

            readable.SetPixels32(pixels);
            readable.Apply(false, false);
            readable.name = "icon_" + key;
            return readable;
        }

        private static Texture2D MakeClearIcon()
        {
            var empty = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            empty.SetPixel(0, 0, Color.clear);
            empty.Apply();
            return empty;
        }

        private static Texture2D MakeReadableCopy(Texture2D src)
        {
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false)
            {
                filterMode = src.filterMode,
                wrapMode = TextureWrapMode.Clamp,
            };
            copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            copy.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }
    }
}
