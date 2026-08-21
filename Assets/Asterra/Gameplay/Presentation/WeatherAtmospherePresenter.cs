using UnityEngine;
using Asterra.Core.World;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Lightweight client atmosphere: tint / fog density from weather intensity.
    /// Does not spawn particle systems (keeps demo GC-light).
    /// </summary>
    public sealed class WeatherAtmospherePresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Color rainTint = new Color(0.55f, 0.6f, 0.7f);
        [SerializeField] private Color snowTint = new Color(0.75f, 0.8f, 0.9f);
        [SerializeField] private Color stormTint = new Color(0.35f, 0.38f, 0.45f);

        private Color _baseAmbient;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
            _baseAmbient = RenderSettings.ambientLight;
        }

        private void LateUpdate()
        {
            if (match == null)
                return;
            var sim = match.World as SkirmishWorldSim;
            if (sim == null)
                return;

            var w = sim.Environment.WeatherSim.Current;
            Color tint = _baseAmbient;
            switch (w.Kind)
            {
                case WeatherKind.Rain:
                    tint = Color.Lerp(_baseAmbient, rainTint, w.Intensity * 0.45f);
                    break;
                case WeatherKind.Snow:
                    tint = Color.Lerp(_baseAmbient, snowTint, w.Intensity * 0.4f);
                    break;
                case WeatherKind.Storm:
                case WeatherKind.Fog:
                    tint = Color.Lerp(_baseAmbient, stormTint, w.Intensity * 0.55f);
                    break;
            }

            // DayNightLightingPresenter owns ambient most frames; bias fog only here.
            if (w.Kind == WeatherKind.Rain || w.Kind == WeatherKind.Storm || w.Kind == WeatherKind.Fog || w.Kind == WeatherKind.Snow)
            {
                RenderSettings.fog = true;
                RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity, 0.004f + w.Intensity * 0.01f);
            }

            // Soft ambient nudge without fighting day/night hard.
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, tint, 0.25f);
        }
    }
}
