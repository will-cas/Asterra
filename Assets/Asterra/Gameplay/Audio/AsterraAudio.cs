using System.Collections.Generic;
using UnityEngine;

namespace Asterra.Gameplay.Audio
{
    public enum AsterraSfx : byte
    {
        UiClick = 0,
        OrderMove = 1,
        OrderAttack = 2,
        OrderBuild = 3,
        OrderGather = 4,
        OrderTrain = 5,
        OrderResearch = 6,
        Hit = 7,
        Death = 8,
        BuildComplete = 9,
        Deposit = 10,
        Capture = 11,
        Victory = 12,
        Defeat = 13,
        Select = 14,
        Invalid = 15,
        Thunder = 16,
    }

    /// <summary>
    /// Procedural placeholder audio (no asset pack required). Replace clips later via Resources/Asterra/Audio.
    /// </summary>
    public sealed class AsterraAudio : MonoBehaviour
    {
        private static AsterraAudio _instance;

        [SerializeField] private float masterVolume = 0.85f;
        [SerializeField] private float sfxVolume = 0.9f;
        [SerializeField] private float musicVolume = 0.35f;
        [SerializeField] private float ambienceVolume = 0.28f;

        private AudioSource _sfx;
        private AudioSource _ui;
        private AudioSource _music;
        private AudioSource _ambience;
        private readonly Dictionary<AsterraSfx, AudioClip> _clips = new();
        private AudioClip _musicBed;
        private AudioClip _ambienceBed;
        private float _lastUiClickAt = -10f;

        public static AsterraAudio Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                _instance = FindFirstObjectByType<AsterraAudio>();
                if (_instance != null)
                    return _instance;
                var go = new GameObject("AsterraAudio");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<AsterraAudio>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            BuildLibrary();
            AsterraSettings.ApplyAudio();
            StartBeds();
            AsterraSettings.ApplyAudio();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public static void Play(AsterraSfx sfx, float volumeScale = 1f) =>
            Instance.PlayInternal(sfx, volumeScale);

        public static void PlayUiClick() => Instance.PlayUiClickInternal();

        public void SetMusicMuted(bool muted)
        {
            if (_music != null)
                _music.mute = muted;
        }

        /// <summary>Apply persisted volumes (and mute preference) without rewriting prefs.</summary>
        public void ApplyVolumes(float master, float music, float sfx, float ambience, bool musicMuted)
        {
            masterVolume = Mathf.Clamp01(master);
            musicVolume = Mathf.Clamp01(music);
            sfxVolume = Mathf.Clamp01(sfx);
            ambienceVolume = Mathf.Clamp01(ambience);
            EnsureSources();
            if (_music != null)
            {
                _music.volume = musicVolume * masterVolume;
                _music.mute = musicMuted;
            }

            if (_ambience != null)
                _ambience.volume = ambienceVolume * masterVolume;
        }

        public void SetAmbienceIntensity(float intensity01)
        {
            if (_ambience == null)
                return;
            _ambience.volume = ambienceVolume * masterVolume * Mathf.Clamp01(intensity01);
        }

        private void EnsureSources()
        {
            if (_sfx == null)
                _sfx = CreateSource("Sfx", spatial: false);
            if (_ui == null)
                _ui = CreateSource("Ui", spatial: false);
            if (_music == null)
            {
                _music = CreateSource("Music", spatial: false);
                _music.loop = true;
            }

            if (_ambience == null)
            {
                _ambience = CreateSource("Ambience", spatial: false);
                _ambience.loop = true;
            }
        }

        private AudioSource CreateSource(string name, bool spatial)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = spatial ? 1f : 0f;
            src.dopplerLevel = 0f;
            return src;
        }

        private void BuildLibrary()
        {
            _clips[AsterraSfx.UiClick] = Tone("ui_click", 880f, 0.04f, 0.22f, soft: false);
            _clips[AsterraSfx.Select] = Tone("select", 620f, 0.05f, 0.18f, soft: false);
            _clips[AsterraSfx.OrderMove] = Sweep("order_move", 320f, 480f, 0.09f, 0.28f);
            _clips[AsterraSfx.OrderAttack] = NoiseBurst("order_attack", 0.08f, 0.35f, bright: true);
            _clips[AsterraSfx.OrderBuild] = Sweep("order_build", 180f, 260f, 0.12f, 0.3f);
            _clips[AsterraSfx.OrderGather] = Tone("order_gather", 240f, 0.08f, 0.25f, soft: true);
            _clips[AsterraSfx.OrderTrain] = Sweep("order_train", 400f, 700f, 0.14f, 0.28f);
            _clips[AsterraSfx.OrderResearch] = Sweep("order_research", 500f, 900f, 0.16f, 0.25f);
            _clips[AsterraSfx.Hit] = NoiseBurst("hit", 0.05f, 0.4f, bright: true);
            _clips[AsterraSfx.Death] = Sweep("death", 220f, 80f, 0.22f, 0.4f);
            _clips[AsterraSfx.BuildComplete] = Chord("build_done", 360f, 540f, 0.18f, 0.32f);
            _clips[AsterraSfx.Deposit] = Tone("deposit", 760f, 0.07f, 0.22f, soft: false);
            _clips[AsterraSfx.Capture] = Chord("capture", 440f, 660f, 0.2f, 0.3f);
            _clips[AsterraSfx.Victory] = Chord("victory", 523f, 784f, 0.55f, 0.4f);
            _clips[AsterraSfx.Defeat] = Sweep("defeat", 300f, 90f, 0.6f, 0.4f);
            _clips[AsterraSfx.Invalid] = Tone("invalid", 140f, 0.08f, 0.3f, soft: true);
            _clips[AsterraSfx.Thunder] = NoiseBurst("thunder", 0.55f, 0.55f, bright: false);

            _musicBed = SoftPad("music_bed", 110f, 165f, 4.5f);
            _ambienceBed = SoftNoise("ambience", 3.2f, 0.35f);
        }

        private void StartBeds()
        {
            if (_musicBed != null)
            {
                _music.clip = _musicBed;
                _music.volume = musicVolume * masterVolume;
                _music.Play();
            }

            if (_ambienceBed != null)
            {
                _ambience.clip = _ambienceBed;
                _ambience.volume = ambienceVolume * masterVolume;
                _ambience.Play();
            }
        }

        private void PlayUiClickInternal()
        {
            if (Time.unscaledTime - _lastUiClickAt < 0.04f)
                return;
            _lastUiClickAt = Time.unscaledTime;
            PlayInternal(AsterraSfx.UiClick, 0.7f);
        }

        private void PlayInternal(AsterraSfx sfx, float volumeScale)
        {
            if (!_clips.TryGetValue(sfx, out var clip) || clip == null)
                return;
            var src = sfx == AsterraSfx.UiClick || sfx == AsterraSfx.Select ? _ui : _sfx;
            src.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * masterVolume * volumeScale));
        }

        private static AudioClip Tone(string name, float hz, float seconds, float amp, bool soft)
        {
            int rate = 22050;
            int samples = Mathf.Max(64, (int)(rate * seconds));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float env = soft
                    ? Mathf.Sin(Mathf.PI * (i / (float)(samples - 1)))
                    : Mathf.Exp(-t * 18f);
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * amp * env;
            }

            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Sweep(string name, float fromHz, float toHz, float seconds, float amp)
        {
            int rate = 22050;
            int samples = Mathf.Max(64, (int)(rate * seconds));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float u = i / (float)(samples - 1);
                float hz = Mathf.Lerp(fromHz, toHz, u);
                float t = i / (float)rate;
                float env = Mathf.Sin(Mathf.PI * u);
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * amp * env;
            }

            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Chord(string name, float aHz, float bHz, float seconds, float amp)
        {
            int rate = 22050;
            int samples = Mathf.Max(64, (int)(rate * seconds));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float u = i / (float)(samples - 1);
                float env = Mathf.Sin(Mathf.PI * u);
                float s = Mathf.Sin(2f * Mathf.PI * aHz * t) + 0.7f * Mathf.Sin(2f * Mathf.PI * bHz * t);
                data[i] = s * 0.5f * amp * env;
            }

            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip NoiseBurst(string name, float seconds, float amp, bool bright)
        {
            int rate = 22050;
            int samples = Mathf.Max(64, (int)(rate * seconds));
            var data = new float[samples];
            float prev = 0f;
            for (int i = 0; i < samples; i++)
            {
                float n = (Random.value * 2f - 1f);
                if (!bright)
                    n = (n + prev) * 0.5f;
                prev = n;
                float env = Mathf.Exp(-(i / (float)rate) * 28f);
                data[i] = n * amp * env;
            }

            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SoftPad(string name, float aHz, float bHz, float seconds)
        {
            int rate = 22050;
            int samples = Mathf.Max(64, (int)(rate * seconds));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float s = Mathf.Sin(2f * Mathf.PI * aHz * t) * 0.35f
                          + Mathf.Sin(2f * Mathf.PI * bHz * t) * 0.25f
                          + Mathf.Sin(2f * Mathf.PI * (aHz * 1.5f) * t) * 0.12f;
                data[i] = s * 0.22f;
            }

            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SoftNoise(string name, float seconds, float amp)
        {
            int rate = 22050;
            int samples = Mathf.Max(64, (int)(rate * seconds));
            var data = new float[samples];
            float state = 0f;
            for (int i = 0; i < samples; i++)
            {
                state = state * 0.97f + (Random.value * 2f - 1f) * 0.03f;
                data[i] = state * amp;
            }

            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
