using System.Collections.Generic;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Shared OnGUI chrome: panels, icon buttons, selection portraits.</summary>
    public static class HudStyle
    {
        private static GUIStyle _panel;
        private static GUIStyle _title;
        private static GUIStyle _label;
        private static GUIStyle _button;
        private static GUIStyle _toast;
        private static GUIStyle _subtitle;
        private static GUIStyle _caption;
        private static GUIStyle _body;
        private static Texture2D _white;
        private static readonly Dictionary<string, Texture2D> Icons = new();

        public static void Ensure()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }

            if (_panel == null)
            {
                _panel = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 13,
                    padding = new RectOffset(10, 10, 8, 8),
                };
            }

            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            if (_label == null)
                _label = new GUIStyle(GUI.skin.label) { fontSize = 13 };

            if (_subtitle == null)
            {
                _subtitle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            if (_caption == null)
            {
                _caption = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                };
            }

            if (_body == null)
            {
                _body = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                };
            }

            if (_button == null)
            {
                _button = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(6, 6, 4, 4),
                };
            }

            if (_toast == null)
            {
                _toast = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
        }

        public static GUIStyle Panel
        {
            get
            {
                Ensure();
                return _panel;
            }
        }
        public static GUIStyle Title { get { Ensure(); return _title; } }
        public static GUIStyle Label { get { Ensure(); return _label; } }
        public static GUIStyle Subtitle { get { Ensure(); return _subtitle; } }
        public static GUIStyle Caption { get { Ensure(); return _caption; } }
        public static GUIStyle Body { get { Ensure(); return _body; } }
        public static GUIStyle Button { get { Ensure(); return _button; } }
        public static GUIStyle Toast { get { Ensure(); return _toast; } }

        public static void DrawPanel(Rect rect, Color fill)
        {
            Ensure();
            var old = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(rect, _white);
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), _white);
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
            DrawPanel(rect, new Color(0.08f, 0.1f, 0.12f, 0.92f));
            var icon = GetIcon(iconKey, accent);
            float iconSize = Mathf.Min(22f, rect.height - 8f);
            GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize), icon);
            var old = GUI.color;
            GUI.color = new Color(0.92f, 0.94f, 0.9f, 1f);
            bool clicked = GUI.Button(
                new Rect(rect.x + iconSize + 8f, rect.y, rect.width - iconSize - 10f, rect.height),
                label,
                Button);
            GUI.color = old;
            return clicked;
        }

        public static Texture2D GetIcon(string key, Color accent)
        {
            Ensure();
            if (Icons.TryGetValue(key, out var tex) && tex != null)
                return tex;
            tex = BuildIcon(key, accent);
            Icons[key] = tex;
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
                case "worker":
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
                default:
                    Fill(10, 10, 22, 22, ink);
                    break;
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D BuildPortrait(string definitionId, Color faction)
        {
            const int s = 40;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color bg = Color.Lerp(faction, new Color(0.05f, 0.07f, 0.08f), 0.45f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool border = x < 2 || y < 2 || x >= s - 2 || y >= s - 2;
                tex.SetPixel(x, y, border ? Color.Lerp(faction, Color.white, 0.35f) : bg);
            }

            Color body = Color.Lerp(faction, Color.white, 0.2f);
            string id = definitionId ?? string.Empty;
            if (id.Contains("archer") || id.Contains("ranger") || id.Contains("acolyte") || id.Contains("mage"))
            {
                FillRect(tex, 16, 8, 24, 32, body);
                FillRect(tex, 22, 10, 28, 30, Color.Lerp(body, Color.cyan, 0.35f));
            }
            else if (id.Contains("knight") || id.Contains("cavalry") || id.Contains("rider"))
            {
                FillRect(tex, 8, 18, 32, 28, body);
                FillRect(tex, 14, 8, 24, 20, Color.Lerp(body, Color.white, 0.2f));
            }
            else if (id.Contains("catapult") || id.Contains("siege") || id.Contains("ballista") || id.Contains("guardian"))
            {
                FillRect(tex, 6, 16, 34, 28, body);
                FillRect(tex, 18, 6, 30, 18, Color.Lerp(body, Color.black, 0.2f));
            }
            else if (id.Contains("builder") || id.Contains("guardian") && id.Contains("forest"))
            {
                FillRect(tex, 12, 10, 28, 30, body);
                FillRect(tex, 26, 14, 34, 20, Color.Lerp(body, Color.yellow, 0.3f));
            }
            else if (id.Contains("lucien") || id.Contains("captain") || id.Contains("hierophant") || id.Contains("leader"))
            {
                FillRect(tex, 12, 10, 28, 32, body);
                FillRect(tex, 10, 4, 30, 12, Color.Lerp(faction, Color.yellow, 0.55f));
            }
            else
            {
                FillRect(tex, 12, 8, 28, 32, body);
                FillRect(tex, 8, 16, 14, 26, Color.Lerp(body, Color.white, 0.15f));
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
