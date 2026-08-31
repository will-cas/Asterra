using UnityEngine;
using Asterra.Core.World;
using Asterra.Gameplay.Audio;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Client precipitation + lightning only. Fog/ambient owned by DayNightLightingPresenter.
    /// </summary>
    public sealed class WeatherAtmospherePresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;

        private ParticleSystem _rain;
        private ParticleSystem _snow;
        private Light _flashLight;
        private float _flashRemaining;
        private float _rainRate;
        private float _snowRate;
        private float _thunderIn = -1f;
        [SerializeField] private float precipEaseSeconds = 3.5f;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
            _rain = CreateFallingSystem("RainFx", new Color(0.7f, 0.8f, 0.95f, 0.45f), size: 0.06f, speed: 32f, stretch: true);
            _snow = CreateFallingSystem("SnowFx", new Color(0.95f, 0.97f, 1f, 0.8f), size: 0.16f, speed: 5f, stretch: false);
            SetEmission(_rain, 0f);
            SetEmission(_snow, 0f);
        }

        private void LateUpdate()
        {
            if (match == null)
                return;
            var sim = match.World as global::Asterra.Gameplay.SkirmishWorldSim;
            if (sim == null)
                return;

            var weather = sim.Environment.WeatherSim;
            var w = weather.Current;

            float targetRain = Mathf.Max(0f, weather.PrecipitationRate) * 120f;
            float targetSnow = Mathf.Max(0f, weather.SnowfallRate) * 90f;
            // Storms punch rain a bit harder once precip is up.
            if (w.Kind == WeatherKind.Storm)
                targetRain = Mathf.Max(targetRain, (80f + w.Intensity * 140f) * Mathf.Clamp01(weather.PrecipitationRate));

            float ease = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, precipEaseSeconds * 0.4f));
            _rainRate = Mathf.Lerp(_rainRate, targetRain, ease);
            _snowRate = Mathf.Lerp(_snowRate, targetSnow, ease);

            SetEmission(_rain, _rainRate);
            SetEmission(_snow, _snowRate);
            FollowCamera(_rain);
            FollowCamera(_snow);

            for (int i = 0; i < weather.Events.Count; i++)
            {
                if (weather.Events[i].Kind == WeatherEventKind.Lightning)
                    TriggerLightningFlash(weather.Events[i].Intensity, weather.Events[i].X, weather.Events[i].Z);
            }

            if (_thunderIn > 0f)
            {
                _thunderIn -= Time.deltaTime;
                if (_thunderIn <= 0f)
                    AsterraAudio.Play(AsterraSfx.Thunder, 0.85f);
            }

            if (_flashRemaining > 0f && _flashLight != null)
            {
                _flashRemaining -= Time.deltaTime;
                _flashLight.intensity = Mathf.Lerp(0f, 8f, Mathf.Clamp01(_flashRemaining / 0.08f));
                if (_flashRemaining <= 0f)
                    _flashLight.intensity = 0f;
            }
        }

        private void TriggerLightningFlash(float intensity, float x, float z)
        {
            if (_flashLight == null)
            {
                var go = new GameObject("LightningFlash");
                go.transform.SetParent(transform, false);
                _flashLight = go.AddComponent<Light>();
                _flashLight.type = LightType.Directional;
                _flashLight.color = new Color(0.75f, 0.85f, 1f);
                _flashLight.intensity = 0f;
                _flashLight.shadows = LightShadows.None;
            }

            _flashRemaining = 0.05f + intensity * 0.05f;
            _flashLight.intensity = 4.5f + intensity * 6f;
            _flashLight.useColorTemperature = true;
            _flashLight.colorTemperature = 9000f;
            AsterraLightingLook.PulseLightning(intensity);
            float delay = 0.35f + intensity * 0.25f;
            if (Camera.main != null)
            {
                Vector3 cam = Camera.main.transform.position;
                float dist = Vector2.Distance(new Vector2(x, z), new Vector2(cam.x, cam.z));
                delay = Mathf.Clamp(dist / 343f, 0.12f, 4.8f);
            }
            _thunderIn = delay;
            if (Camera.main != null)
                _flashLight.transform.rotation = Quaternion.LookRotation(Vector3.down + Camera.main.transform.forward * 0.2f);
        }

        private static void FollowCamera(ParticleSystem system)
        {
            if (system == null || Camera.main == null)
                return;
            var cam = Camera.main.transform;
            system.transform.position = cam.position + cam.forward * 40f + Vector3.up * 40f;
        }

        private ParticleSystem CreateFallingSystem(string name, Color color, float size, float speed, bool stretch)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 1.1f;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70f, 1f, 70f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(-speed);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (stretch)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 3.2f;
                renderer.velocityScale = 0.06f;
            }

            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                   ?? Shader.Find("Particles/Standard Unlit")
                                   ?? Shader.Find("Sprites/Default"));
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            renderer.sharedMaterial = mat;
            return ps;
        }

        private static void SetEmission(ParticleSystem system, float rate)
        {
            if (system == null)
                return;
            var emission = system.emission;
            emission.rateOverTime = rate;
            if (rate > 0.1f && !system.isPlaying)
                system.Play();
            if (rate <= 0.1f && system.isPlaying)
                system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
