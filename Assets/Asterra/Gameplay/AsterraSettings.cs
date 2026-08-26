using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Persisted audio / display options via PlayerPrefs.</summary>
    public static class AsterraSettings
    {
        private const string MasterKey = "asterra.vol.master";
        private const string MusicKey = "asterra.vol.music";
        private const string SfxKey = "asterra.vol.sfx";
        private const string AmbienceKey = "asterra.vol.ambience";
        private const string MusicMuteKey = "asterra.music.mute";
        private const string UiScaleKey = "asterra.ui.scale";

        public const float UiScaleMin = 0.85f;
        public const float UiScaleMax = 1.25f;

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 0.85f);
            set
            {
                PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                ApplyAudio();
            }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.35f);
            set
            {
                PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                ApplyAudio();
            }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 0.9f);
            set
            {
                PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                ApplyAudio();
            }
        }

        public static float AmbienceVolume
        {
            get => PlayerPrefs.GetFloat(AmbienceKey, 0.28f);
            set
            {
                PlayerPrefs.SetFloat(AmbienceKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                ApplyAudio();
            }
        }

        public static bool MusicMuted
        {
            get => PlayerPrefs.GetInt(MusicMuteKey, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(MusicMuteKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyAudio();
            }
        }

        /// <summary>IMGUI chrome scale (0.85–1.25). Applied via <see cref="HudStyle"/>.</summary>
        public static float UiScale
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(UiScaleKey, 1f), UiScaleMin, UiScaleMax);
            set
            {
                PlayerPrefs.SetFloat(UiScaleKey, Mathf.Clamp(value, UiScaleMin, UiScaleMax));
                PlayerPrefs.Save();
                HudStyle.InvalidateScale();
            }
        }

        public static void ApplyAudio()
        {
            var audio = Audio.AsterraAudio.Instance;
            if (audio == null)
                return;
            audio.ApplyVolumes(MasterVolume, MusicVolume, SfxVolume, AmbienceVolume, MusicMuted);
        }
    }
}
