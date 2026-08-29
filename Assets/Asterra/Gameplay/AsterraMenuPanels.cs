using Asterra.Gameplay.Audio;
using Asterra.Gameplay.Content;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Shared IMGUI overlays for Options, Profile (lobby-only), Controls, and in-match Pause.</summary>
    public static class AsterraMenuPanels
    {
        public enum Overlay
        {
            None = 0,
            Options = 1,
            Profile = 2,
            Pause = 3,
            Controls = 4,
        }

        /// <summary>Profile is lobby / out-of-match only — never from Esc pause.</summary>
        public static bool IsProfileAllowedDuringMatch => false;

        private static Vector2 _controlsScroll;
        private static Overlay _controlsReturn = Overlay.None;

        /// <summary>Call before opening <see cref="Overlay.Controls"/> so Back returns correctly.</summary>
        public static void PrepareControls(Overlay returnTo)
        {
            _controlsReturn = returnTo;
            _controlsScroll = Vector2.zero;
        }

        private static readonly (string key, string action)[] ControlRows =
        {
            ("Camera", ""),
            ("WASD / arrows", "Pan camera"),
            ("Middle-mouse / edge", "Pan camera"),
            ("Minimap click", "Jump camera"),
            ("", ""),
            ("Selection", ""),
            ("LMB click / drag", "Select units"),
            ("Shift / Cmd + LMB", "Add to selection"),
            ("RMB", "Move / attack / gather / chop / rally"),
            ("R", "Reselect all owned units"),
            ("Ctrl/Cmd + 1–9", "Assign control group"),
            ("1–9", "Select group (double-tap centers)"),
            (". / I", "Select idle workers"),
            ("", ""),
            ("Combat orders", ""),
            ("A then click", "Attack-move"),
            ("S", "Stop"),
            ("P then click", "Patrol"),
            ("F / G / H", "Stance: Aggressive / Defensive / Hold"),
            ("C", "Capture nearest territory"),
            ("U", "Buy faction upgrade / equip"),
            ("Q", "Primary commander power (outside place mode)"),
            ("", ""),
            ("Build & earthworks", ""),
            ("B / N / M / O", "Barracks / Tower / Wall / Mine"),
            ("V / J / , / .", "Bridge / Trench / Barricade / Ferry"),
            ("Y / K / L", "Fill / Berm / Moat (ghost + click/drag)"),
            ("; / ' / \\", "Clear forest / Burn / Quarry"),
            ("[ / ]", "Spikes / Clear debris (or rotate while placing)"),
            ("Q / E / scroll", "Rotate place ghost 90°"),
            ("=", "Repair collapsed bridge"),
            ("Shift while placing", "Keep place mode after one stamp"),
            ("Esc", "Cancel armed mode; otherwise pause"),
            ("", ""),
            ("Production", ""),
            ("T", "Train from selected keep/producer"),
            ("X", "Cancel production queue"),
            ("", ""),
            ("System", ""),
            ("F5 / F9", "Quick save / load"),
            ("Esc", "Pause menu (Options, Controls, Main Menu)"),
        };

        /// <summary>
        /// Draws a modal. Returns true if the overlay should close (Close / backdrop not used).
        /// For Pause: outQuitMainMenu when user picks Main Menu.
        /// </summary>
        public static bool Draw(
            Overlay overlay,
            out bool quitMainMenu,
            out Overlay navigateTo)
        {
            quitMainMenu = false;
            navigateTo = overlay;
            if (overlay == Overlay.None)
                return false;

            HudStyle.Ensure();
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            HudClickBlocker.Block(screen);
            HudStyle.DrawPanel(screen, new Color(0.02f, 0.03f, 0.04f, 0.82f));

            float w = Mathf.Min(HudStyle.S(460f), Screen.width - 48f);
            float h = HudStyle.S(240f);
            if (overlay == Overlay.Options)
                h = HudStyle.S(420f);
            else if (overlay == Overlay.Profile)
                h = HudStyle.S(360f);
            else if (overlay == Overlay.Pause)
                h = HudStyle.S(360f);
            else if (overlay == Overlay.Controls)
            {
                w = Mathf.Min(HudStyle.S(560f), Screen.width - 40f);
                h = Mathf.Min(HudStyle.S(520f), Screen.height - 48f);
            }

            var box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            HudClickBlocker.Block(box);
            HudStyle.DrawFrame(
                box,
                new Color(0.05f, 0.06f, 0.08f, 0.98f),
                new Color(0.7f, 0.6f, 0.35f, 0.7f),
                2f);

            switch (overlay)
            {
                case Overlay.Options:
                    return DrawOptions(box, out navigateTo);
                case Overlay.Profile:
                    return DrawProfile(box, out navigateTo);
                case Overlay.Pause:
                    return DrawPause(box, out quitMainMenu, out navigateTo);
                case Overlay.Controls:
                    return DrawControls(box, out navigateTo);
                default:
                    return false;
            }
        }

        private static bool DrawOptions(Rect box, out Overlay navigateTo)
        {
            navigateTo = Overlay.Options;
            GUI.Label(new Rect(box.x, box.y + HudStyle.S(14f), box.width, HudStyle.S(28f)), "OPTIONS", HudStyle.Title);

            float y = box.y + HudStyle.S(52f);
            float labelW = HudStyle.S(110f);
            float pad = HudStyle.S(24f);
            float sliderX = box.x + pad + labelW;
            float sliderW = box.width - pad * 2f - labelW;
            float row = HudStyle.S(34f);

            GUI.Label(new Rect(box.x + pad, y, labelW, HudStyle.S(22f)), "Master", HudStyle.Label);
            float master = GUI.HorizontalSlider(
                new Rect(sliderX, y + HudStyle.S(4f), sliderW, HudStyle.S(18f)),
                AsterraSettings.MasterVolume,
                0f,
                1f);
            if (!Mathf.Approximately(master, AsterraSettings.MasterVolume))
                AsterraSettings.MasterVolume = master;
            y += row;

            GUI.Label(new Rect(box.x + pad, y, labelW, HudStyle.S(22f)), "Music", HudStyle.Label);
            float music = GUI.HorizontalSlider(
                new Rect(sliderX, y + HudStyle.S(4f), sliderW, HudStyle.S(18f)),
                AsterraSettings.MusicVolume,
                0f,
                1f);
            if (!Mathf.Approximately(music, AsterraSettings.MusicVolume))
                AsterraSettings.MusicVolume = music;
            y += row;

            GUI.Label(new Rect(box.x + pad, y, labelW, HudStyle.S(22f)), "SFX", HudStyle.Label);
            float sfx = GUI.HorizontalSlider(
                new Rect(sliderX, y + HudStyle.S(4f), sliderW, HudStyle.S(18f)),
                AsterraSettings.SfxVolume,
                0f,
                1f);
            if (!Mathf.Approximately(sfx, AsterraSettings.SfxVolume))
                AsterraSettings.SfxVolume = sfx;
            y += row;

            GUI.Label(new Rect(box.x + pad, y, labelW, HudStyle.S(22f)), "Ambience", HudStyle.Label);
            float amb = GUI.HorizontalSlider(
                new Rect(sliderX, y + HudStyle.S(4f), sliderW, HudStyle.S(18f)),
                AsterraSettings.AmbienceVolume,
                0f,
                1f);
            if (!Mathf.Approximately(amb, AsterraSettings.AmbienceVolume))
                AsterraSettings.AmbienceVolume = amb;
            y += HudStyle.S(38f);

            bool mute = GUI.Toggle(
                new Rect(box.x + pad, y, box.width - pad * 2f, HudStyle.S(24f)),
                AsterraSettings.MusicMuted,
                " Mute music");
            if (mute != AsterraSettings.MusicMuted)
                AsterraSettings.MusicMuted = mute;
            y += HudStyle.S(32f);

            GUI.Label(new Rect(box.x + pad, y, labelW, HudStyle.S(22f)), "UI scale", HudStyle.Label);
            float scale = GUI.HorizontalSlider(
                new Rect(sliderX, y + HudStyle.S(4f), sliderW - HudStyle.S(48f), HudStyle.S(18f)),
                AsterraSettings.UiScale,
                AsterraSettings.UiScaleMin,
                AsterraSettings.UiScaleMax);
            GUI.Label(
                new Rect(box.xMax - pad - HudStyle.S(44f), y, HudStyle.S(44f), HudStyle.S(22f)),
                $"{AsterraSettings.UiScale:0.00}",
                HudStyle.Caption);
            if (!Mathf.Approximately(scale, AsterraSettings.UiScale))
                AsterraSettings.UiScale = scale;
            y += HudStyle.S(36f);

            bool full = GUI.Toggle(
                new Rect(box.x + pad, y, box.width - pad * 2f, HudStyle.S(24f)),
                Screen.fullScreen,
                " Fullscreen");
            if (full != Screen.fullScreen)
                Screen.fullScreen = full;
            y += HudStyle.S(34f);

            if (ChipButton(new Rect(box.x + pad, y, HudStyle.S(160f), HudStyle.S(32f)), "Controls"))
            {
                AsterraAudio.PlayUiClick();
                PrepareControls(Overlay.Options);
                navigateTo = Overlay.Controls;
                return false;
            }

            if (ChipButton(new Rect(box.x + pad, box.yMax - HudStyle.S(48f), HudStyle.S(120f), HudStyle.S(32f)), "Close"))
            {
                AsterraAudio.PlayUiClick();
                navigateTo = Overlay.None;
                return true;
            }

            return false;
        }

        private static bool DrawProfile(Rect box, out Overlay navigateTo)
        {
            navigateTo = Overlay.Profile;
            GUI.Label(new Rect(box.x, box.y + HudStyle.S(14f), box.width, HudStyle.S(28f)), "COMMANDER", HudStyle.Title);

            float y = box.y + HudStyle.S(56f);
            float pad = HudStyle.S(28f);
            GUI.Label(new Rect(box.x + pad, y, box.width - pad * 2f, HudStyle.S(20f)), "Display name", HudStyle.Caption);
            y += HudStyle.S(22f);
            string name = AsterraLocalProfile.DisplayName;
            string edited = GUI.TextField(
                new Rect(box.x + pad, y, box.width - pad * 2f, HudStyle.S(26f)),
                name,
                24);
            if (edited != name)
                AsterraLocalProfile.DisplayName = edited;
            y += HudStyle.S(36f);

            GUI.Label(
                new Rect(box.x + pad, y, box.width - pad * 2f, HudStyle.S(22f)),
                $"Matches: {AsterraLocalProfile.MatchesPlayed}",
                HudStyle.Label);
            y += HudStyle.S(24f);
            GUI.Label(
                new Rect(box.x + pad, y, box.width - pad * 2f, HudStyle.S(22f)),
                $"Wins: {AsterraLocalProfile.Wins}   Losses: {AsterraLocalProfile.Losses}",
                HudStyle.Label);
            y += HudStyle.S(24f);
            GUI.Label(
                new Rect(box.x + pad, y, box.width - pad * 2f, HudStyle.S(22f)),
                $"Win rate: {(AsterraLocalProfile.WinRate * 100f):0}%",
                HudStyle.Label);
            y += HudStyle.S(28f);

            string factionName = "—";
            var all = FactionDefaultContent.All;
            int fi = AsterraLocalProfile.LastFactionIndex;
            if (all != null && fi >= 0 && fi < all.Length)
                factionName = all[fi].DisplayName;
            GUI.Label(
                new Rect(box.x + pad, y, box.width - pad * 2f, HudStyle.S(22f)),
                $"Last faction: {factionName}",
                HudStyle.Label);

            if (ChipButton(
                    new Rect(box.x + pad, box.yMax - HudStyle.S(48f), HudStyle.S(140f), HudStyle.S(32f)),
                    "Reset stats"))
            {
                AsterraAudio.PlayUiClick();
                AsterraLocalProfile.ResetStats();
            }

            if (ChipButton(
                    new Rect(box.xMax - HudStyle.S(148f), box.yMax - HudStyle.S(48f), HudStyle.S(120f), HudStyle.S(32f)),
                    "Close"))
            {
                AsterraAudio.PlayUiClick();
                navigateTo = Overlay.None;
                return true;
            }

            return false;
        }

        private static bool DrawPause(Rect box, out bool quitMainMenu, out Overlay navigateTo)
        {
            quitMainMenu = false;
            navigateTo = Overlay.Pause;
            GUI.Label(new Rect(box.x, box.y + HudStyle.S(18f), box.width, HudStyle.S(28f)), "PAUSED", HudStyle.Title);
            GUI.Label(
                new Rect(box.x + HudStyle.S(24f), box.y + HudStyle.S(52f), box.width - HudStyle.S(48f), HudStyle.S(36f)),
                "Soft pause keeps the sim ticking (lockstep-safe).",
                HudStyle.Caption);

            float y = box.y + HudStyle.S(96f);
            float bw = box.width - HudStyle.S(80f);
            float bx = box.x + HudStyle.S(40f);
            float bh = HudStyle.S(34f);
            float gap = HudStyle.S(38f);
            if (ChipButton(new Rect(bx, y, bw, bh), "Resume"))
            {
                AsterraAudio.PlayUiClick();
                navigateTo = Overlay.None;
                return true;
            }

            y += gap;
            if (ChipButton(new Rect(bx, y, bw * 0.48f, bh), "Save"))
            {
                AsterraAudio.PlayUiClick();
                var match = Object.FindFirstObjectByType<MatchBootstrap>();
                match?.SaveOfflineQuick();
            }

            if (ChipButton(new Rect(bx + bw * 0.52f, y, bw * 0.48f, bh), "Load"))
            {
                AsterraAudio.PlayUiClick();
                var match = Object.FindFirstObjectByType<MatchBootstrap>();
                match?.LoadOfflineQuick();
            }

            y += gap;
            if (ChipButton(new Rect(bx, y, bw * 0.48f, bh), "Options"))
            {
                AsterraAudio.PlayUiClick();
                navigateTo = Overlay.Options;
                return false;
            }

            if (ChipButton(new Rect(bx + bw * 0.52f, y, bw * 0.48f, bh), "Controls"))
            {
                AsterraAudio.PlayUiClick();
                PrepareControls(Overlay.Pause);
                navigateTo = Overlay.Controls;
                return false;
            }

            y += gap;
            if (ChipButton(new Rect(bx, y, bw, bh), "Main Menu"))
            {
                AsterraAudio.PlayUiClick();
                quitMainMenu = true;
                navigateTo = Overlay.None;
                return true;
            }

            return false;
        }

        private static bool DrawControls(Rect box, out Overlay navigateTo)
        {
            navigateTo = Overlay.Controls;
            GUI.Label(new Rect(box.x, box.y + HudStyle.S(12f), box.width, HudStyle.S(28f)), "CONTROLS", HudStyle.Title);
            GUI.Label(
                new Rect(box.x + HudStyle.S(20f), box.y + HudStyle.S(40f), box.width - HudStyle.S(40f), HudStyle.S(22f)),
                "Keyboard & mouse bindings.",
                HudStyle.Caption);

            float pad = HudStyle.S(18f);
            float footer = HudStyle.S(52f);
            float header = HudStyle.S(68f);
            var view = new Rect(box.x + pad, box.y + header, box.width - pad * 2f, box.height - header - footer);
            float keyW = HudStyle.S(150f);
            float rowH = HudStyle.S(22f);
            float contentH = ControlRows.Length * rowH + HudStyle.S(8f);

            _controlsScroll = GUI.BeginScrollView(view, _controlsScroll, new Rect(0f, 0f, view.width - HudStyle.S(18f), contentH));
            float y = 0f;
            for (int i = 0; i < ControlRows.Length; i++)
            {
                var row = ControlRows[i];
                if (string.IsNullOrEmpty(row.key) && string.IsNullOrEmpty(row.action))
                {
                    y += HudStyle.S(8f);
                    continue;
                }

                if (string.IsNullOrEmpty(row.action))
                {
                    GUI.Label(new Rect(0f, y, view.width - HudStyle.S(20f), rowH), row.key.ToUpperInvariant(), HudStyle.Label);
                    y += rowH;
                    continue;
                }

                GUI.Label(new Rect(0f, y, keyW, rowH), row.key, HudStyle.Caption);
                GUI.Label(new Rect(keyW + HudStyle.S(8f), y, view.width - keyW - HudStyle.S(28f), rowH), row.action, HudStyle.Caption);
                y += rowH;
            }

            GUI.EndScrollView();

            if (ChipButton(
                    new Rect(box.x + pad, box.yMax - HudStyle.S(42f), HudStyle.S(120f), HudStyle.S(32f)),
                    "Back"))
            {
                AsterraAudio.PlayUiClick();
                navigateTo = _controlsReturn != Overlay.None ? _controlsReturn : Overlay.None;
                _controlsReturn = Overlay.None;
                return navigateTo == Overlay.None;
            }

            return false;
        }

        private static bool ChipButton(Rect rect, string label)
        {
            return HudStyle.FrameButton(
                rect,
                label,
                new Color(0.14f, 0.15f, 0.16f, 0.98f),
                new Color(0.65f, 0.55f, 0.32f, 0.65f),
                1f);
        }
    }
}
