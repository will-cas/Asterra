using UnityEngine;
using Asterra.Core.World;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Client-only lighting driven by sim <see cref="TimeOfDaySystem"/>. Does not affect lockstep.
    /// SkirmishWorldSim lives in namespace Asterra.Gameplay (not Asterra.Gameplay.Sim).
    /// </summary>
    public sealed class DayNightLightingPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Light sunLight;
        [SerializeField] private bool driveRenderSettings = true;
        [SerializeField] private Color dayAmbient = new Color(0.55f, 0.6f, 0.65f);
        [SerializeField] private Color nightAmbient = new Color(0.08f, 0.1f, 0.16f);
        [SerializeField] private Color dawnAmbient = new Color(0.45f, 0.35f, 0.4f);
        [SerializeField] private Color duskAmbient = new Color(0.4f, 0.28f, 0.22f);

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
            if (sunLight == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        sunLight = lights[i];
                        break;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (match == null)
                return;
            var sim = match.World as SkirmishWorldSim;
            if (sim == null)
                return;
            var tod = sim.Environment.TimeOfDaySim;

            if (sunLight != null)
            {
                sunLight.intensity = tod.SunIntensity;
                sunLight.shadowStrength = tod.ShadowStrength;
                var dir = new Vector3(tod.SunDirX, tod.SunDirY, tod.SunDirZ);
                if (dir.sqrMagnitude > 0.0001f)
                    sunLight.transform.rotation = Quaternion.LookRotation(-dir.normalized);
            }

            if (!driveRenderSettings)
                return;

            Color amb = dayAmbient;
            switch (tod.Phase)
            {
                case TimeOfDayPhase.Night:
                    amb = nightAmbient;
                    break;
                case TimeOfDayPhase.Dawn:
                    amb = dawnAmbient;
                    break;
                case TimeOfDayPhase.Dusk:
                    amb = duskAmbient;
                    break;
                case TimeOfDayPhase.Evening:
                    amb = Color.Lerp(dayAmbient, duskAmbient, 0.45f);
                    break;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(nightAmbient, amb, Mathf.Clamp01(tod.AmbientIntensity / 0.42f));
            RenderSettings.fog = tod.IsNight || tod.Phase == TimeOfDayPhase.Dusk || tod.Phase == TimeOfDayPhase.Dawn;
            if (!RenderSettings.fog)
                return;

            float fogDensity = tod.IsNight ? 0.012f : 0.006f;
            fogDensity *= 1f + sim.Environment.WeatherSim.FogDensity;
            float vis = sim.Environment.CombinedVisibility();
            fogDensity *= Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(vis));
            RenderSettings.fogDensity = fogDensity;
        }
    }
}
