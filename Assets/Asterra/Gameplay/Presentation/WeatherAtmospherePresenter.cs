using UnityEngine;
using Asterra.Core.World;

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

            float rainRate = 0f;
            float snowRate = 0f;
            if (w.Kind == WeatherKind.Rain)
                rainRate = 40f + w.Intensity * 90f;
            else if (w.Kind == WeatherKind.Storm)
                rainRate = 80f + w.Intensity * 140f;
            else if (w.Kind == WeatherKind.Snow)
                snowRate = 25f + w.Intensity * 70f;

            SetEmission(_rain, rainRate);
            SetEmission(_snow, snowRate);
            FollowCamera(_rain);
            FollowCamera(_snow);

            for (int i = 0; i < weather.Events.Count; i++)
            {
                if (weather.Events[i].Kind == WeatherEventKind.Lightning)
                    TriggerLightningFlash(weather.Events[i].Intensity);
            }

            if (_flashRemaining > 0f && _flashLight != null)
            {
                _flashRemaining -= Time.deltaTime;
                _flashLight.intensity = Mathf.Lerp(0f, 1.6f, Mathf.Clamp01(_flashRemaining / 0.1f));
                if (_flashRemaining <= 0f)
                    _flashLight.intensity = 0f;
            }
        }

        private void TriggerLightningFlash(float intensity)
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

            _flashRemaining = 0.06f + intensity * 0.04f;
            _flashLight.intensity = 0.9f + intensity * 0.7f;
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
