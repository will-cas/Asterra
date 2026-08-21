using UnityEngine;
using Asterra.Core.World;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Client lighting + fog from time of day and weather. Owns RenderSettings so weather
    /// cannot stack exponential fog into a gray washout.
    /// </summary>
    public sealed class DayNightLightingPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Light sunLight;
        [SerializeField] private bool driveRenderSettings = true;
        [SerializeField] private Color dayAmbient = new Color(0.55f, 0.6f, 0.65f);
        [SerializeField] private Color nightAmbient = new Color(0.12f, 0.14f, 0.2f);
        [SerializeField] private Color dawnAmbient = new Color(0.5f, 0.4f, 0.42f);
        [SerializeField] private Color duskAmbient = new Color(0.45f, 0.34f, 0.3f);
        [SerializeField] private Color fogColorDay = new Color(0.55f, 0.62f, 0.7f);
        [SerializeField] private Color fogColorNight = new Color(0.08f, 0.1f, 0.14f);
        [SerializeField] private Color fogColorRain = new Color(0.42f, 0.48f, 0.55f);
        [SerializeField] private float fogStart = 120f;
        [SerializeField] private float fogEndClear = 700f;
        [SerializeField] private float fogEndWet = 420f;
        [SerializeField] private float fogEndNight = 380f;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
            if (sunLight == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional && lights[i].name != "LightningFlash")
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
            var sim = match.World as global::Asterra.Gameplay.SkirmishWorldSim;
            if (sim == null)
                return;

            var tod = sim.Environment.TimeOfDaySim;
            var weather = sim.Environment.WeatherSim.Current;

            float sunMul = 1f;
            switch (weather.Kind)
            {
                case WeatherKind.Cloudy: sunMul = 0.85f; break;
                case WeatherKind.Rain: sunMul = 0.7f; break;
                case WeatherKind.Storm: sunMul = 0.45f; break;
                case WeatherKind.Fog: sunMul = 0.55f; break;
                case WeatherKind.Snow: sunMul = 0.75f; break;
            }

            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Max(0.25f, tod.SunIntensity * sunMul);
                sunLight.shadowStrength = tod.ShadowStrength;
                // Keep evening readable — never let the key light die completely.
                if (tod.Phase == TimeOfDayPhase.Evening)
                    sunLight.intensity = Mathf.Max(sunLight.intensity, 0.45f);
                if (tod.Phase == TimeOfDayPhase.Dusk)
                    sunLight.intensity = Mathf.Max(sunLight.intensity, 0.35f);

                var dir = new Vector3(tod.SunDirX, tod.SunDirY, tod.SunDirZ);
                if (dir.sqrMagnitude > 0.0001f)
                    sunLight.transform.rotation = Quaternion.LookRotation(-dir.normalized);

                Color sunColor = Color.white;
                if (tod.Phase == TimeOfDayPhase.Dawn || tod.Phase == TimeOfDayPhase.Dusk || tod.Phase == TimeOfDayPhase.Evening)
                    sunColor = new Color(1f, 0.82f, 0.65f);
                if (weather.Kind == WeatherKind.Rain || weather.Kind == WeatherKind.Storm)
                    sunColor = Color.Lerp(sunColor, new Color(0.75f, 0.8f, 0.9f), weather.Intensity * 0.5f);
                sunLight.color = sunColor;
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

            // Mild weather tint — keep luminance up so Lit terrain does not go flat gray.
            Color weatherTint = amb;
            switch (weather.Kind)
            {
                case WeatherKind.Rain:
                    weatherTint = Color.Lerp(amb, new Color(0.5f, 0.55f, 0.62f), weather.Intensity * 0.35f);
                    break;
                case WeatherKind.Storm:
                case WeatherKind.Fog:
                    weatherTint = Color.Lerp(amb, new Color(0.4f, 0.43f, 0.5f), weather.Intensity * 0.4f);
                    break;
                case WeatherKind.Snow:
                    weatherTint = Color.Lerp(amb, new Color(0.7f, 0.75f, 0.82f), weather.Intensity * 0.3f);
                    break;
                case WeatherKind.Cloudy:
                    weatherTint = Color.Lerp(amb, new Color(0.48f, 0.5f, 0.55f), 0.25f);
                    break;
            }

            float ambStrength = Mathf.Clamp(tod.AmbientIntensity / 0.42f, 0.35f, 1f);
            Color finalAmb = Color.Lerp(nightAmbient, weatherTint, ambStrength);
            // Floor ambient so URP Lit materials stay readable under rain/evening.
            finalAmb = new Color(
                Mathf.Max(finalAmb.r, 0.18f),
                Mathf.Max(finalAmb.g, 0.2f),
                Mathf.Max(finalAmb.b, 0.22f));

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = finalAmb;

            ApplyLinearFog(tod, weather);
        }

        private void ApplyLinearFog(TimeOfDaySystem tod, WeatherState weather)
        {
            bool wet = weather.Kind == WeatherKind.Rain
                       || weather.Kind == WeatherKind.Storm
                       || weather.Kind == WeatherKind.Fog
                       || weather.Kind == WeatherKind.Snow;
            bool dimPhase = tod.IsNight
                            || tod.Phase == TimeOfDayPhase.Dusk
                            || tod.Phase == TimeOfDayPhase.Dawn
                            || tod.Phase == TimeOfDayPhase.Evening;

            RenderSettings.fog = wet || dimPhase;
            if (!RenderSettings.fog)
                return;

            RenderSettings.fogMode = FogMode.Linear;

            float end = fogEndClear;
            if (wet)
                end = Mathf.Lerp(fogEndClear, fogEndWet, Mathf.Clamp01(weather.Intensity));
            if (tod.IsNight || tod.Phase == TimeOfDayPhase.Dusk)
                end = Mathf.Min(end, fogEndNight);
            else if (tod.Phase == TimeOfDayPhase.Evening)
                end = Mathf.Min(end, Mathf.Lerp(fogEndClear, fogEndNight, 0.45f));

            // Hard floor so the playable basin never disappears into fog soup.
            end = Mathf.Max(end, 280f);
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = end;

            Color fog = fogColorDay;
            if (tod.IsNight || tod.Phase == TimeOfDayPhase.Dusk)
                fog = fogColorNight;
            else if (tod.Phase == TimeOfDayPhase.Evening)
                fog = Color.Lerp(fogColorDay, fogColorNight, 0.4f);
            if (wet)
                fog = Color.Lerp(fog, fogColorRain, weather.Intensity * 0.65f);
            RenderSettings.fogColor = fog;
        }
    }
}
