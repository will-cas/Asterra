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
    }

    /// <summary>
    /// Loads clips from <c>Resources/Asterra/Audio</c> (Kenney CC0). No procedural synth fallback.
    /// </summary>
    public sealed class AsterraAudio : MonoBehaviour
    {
        private const string ResourceRoot = "Asterra/Audio/";

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
            _clips[AsterraSfx.UiClick] = LoadClip("ui_click");
            _clips[AsterraSfx.Select] = LoadClip("select");
            _clips[AsterraSfx.OrderMove] = LoadClip("order_move");
            _clips[AsterraSfx.OrderAttack] = LoadClip("order_attack");
            _clips[AsterraSfx.OrderBuild] = LoadClip("order_build");
            _clips[AsterraSfx.OrderGather] = LoadClip("order_gather");
            _clips[AsterraSfx.OrderTrain] = LoadClip("order_train");
            _clips[AsterraSfx.OrderResearch] = LoadClip("order_research");
            _clips[AsterraSfx.Hit] = LoadClip("hit");
            _clips[AsterraSfx.Death] = LoadClip("death");
            _clips[AsterraSfx.BuildComplete] = LoadClip("build_done");
            _clips[AsterraSfx.Deposit] = LoadClip("deposit");
            _clips[AsterraSfx.Capture] = LoadClip("capture");
            _clips[AsterraSfx.Victory] = LoadClip("victory");
            _clips[AsterraSfx.Defeat] = LoadClip("defeat");
            _clips[AsterraSfx.Invalid] = LoadClip("invalid");

            _musicBed = LoadClip("music_bed");
            _ambienceBed = LoadClip("ambience");
        }

        private static AudioClip LoadClip(string resourceName)
        {
            var clip = Resources.Load<AudioClip>(ResourceRoot + resourceName);
            if (clip == null)
                Debug.LogError($"[Asterra] Missing audio clip Resources/{ResourceRoot}{resourceName}");
            return clip;
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
    }
}
