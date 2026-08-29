using UnityEngine;
using Asterra.Core.World;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Client lighting + fog from time of day and weather. Owns RenderSettings so weather
    /// cannot stack exponential fog into a gray washout. Values ease toward sim targets
    /// so phase/weather changes never hard-cut.
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
        [SerializeField] private float lightingEaseSeconds = 4.5f;

        private float _smoothedSunIntensity = 1f;
        private float _smoothedShadowStrength = 0.85f;
        private Color _smoothedSunColor = Color.white;
        private Color _smoothedAmbient = new Color(0.55f, 0.6f, 0.65f);
        private Color _smoothedFogColor = new Color(0.55f, 0.62f, 0.7f);
        private float _smoothedFogEnd = 700f;
        private float _smoothedFogWeight;
        private bool _initialized;

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
            var weatherSys = sim.Environment.WeatherSim;
            var weather = weatherSys.Current;

            float precip = Mathf.Clamp01(weatherSys.PrecipitationRate);
            float fogDen = Mathf.Clamp01(weatherSys.FogDensity);
            float snow = Mathf.Clamp01(weatherSys.SnowfallRate);
            float wetness = Mathf.Clamp01(Mathf.Max(precip * 0.85f, fogDen, snow * 0.55f));

            float sunMul = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01(precip * 0.5f + fogDen * 0.55f + snow * 0.25f));
            if (weather.Kind == WeatherKind.Storm)
                sunMul = Mathf.Min(sunMul, Mathf.Lerp(1f, 0.45f, weather.Intensity));
            else if (weather.Kind == WeatherKind.Cloudy)
                sunMul = Mathf.Min(sunMul, 0.85f);

            float targetSunIntensity = Mathf.Max(0.25f, tod.SunIntensity * sunMul);
            // Keep evening/dusk readable without snapping.
            float time01 = tod.Time01;
            if (time01 > 0.52f && time01 < 0.65f)
                targetSunIntensity = Mathf.Max(targetSunIntensity, 0.45f);
            else if (time01 >= 0.65f && time01 < 0.75f)
                targetSunIntensity = Mathf.Max(targetSunIntensity, 0.35f);

            Color targetSunColor = SampleSunColor(time01);
            targetSunColor = Color.Lerp(targetSunColor, new Color(0.75f, 0.8f, 0.9f), Mathf.Clamp01(precip + fogDen * 0.5f) * 0.5f);

            Color targetAmb = SampleAmbient(time01);
            targetAmb = Color.Lerp(targetAmb, new Color(0.5f, 0.55f, 0.62f), precip * 0.35f);
            targetAmb = Color.Lerp(targetAmb, new Color(0.4f, 0.43f, 0.5f), Mathf.Max(fogDen, weather.Kind == WeatherKind.Storm ? weather.Intensity * 0.4f : 0f));
            targetAmb = Color.Lerp(targetAmb, new Color(0.7f, 0.75f, 0.82f), snow * 0.3f);
            if (weather.Kind == WeatherKind.Cloudy)
                targetAmb = Color.Lerp(targetAmb, new Color(0.48f, 0.5f, 0.55f), 0.25f);

            float ambStrength = Mathf.Clamp(tod.AmbientIntensity / 0.42f, 0.35f, 1f);
            targetAmb = Color.Lerp(nightAmbient, targetAmb, ambStrength);
            targetAmb = new Color(
                Mathf.Max(targetAmb.r, 0.18f),
                Mathf.Max(targetAmb.g, 0.2f),
                Mathf.Max(targetAmb.b, 0.22f));

            float targetFogEnd = fogEndClear;
            targetFogEnd = Mathf.Lerp(targetFogEnd, fogEndWet, wetness);
            float nightDim = NightFactor(time01);
            targetFogEnd = Mathf.Lerp(targetFogEnd, Mathf.Min(targetFogEnd, fogEndNight), nightDim);
            targetFogEnd = Mathf.Max(targetFogEnd, 280f);

            Color targetFog = Color.Lerp(fogColorDay, fogColorNight, nightDim);
            targetFog = Color.Lerp(targetFog, fogColorRain, wetness * 0.65f);

            float targetFogWeight = Mathf.Clamp01(wetness * 0.85f + nightDim * 0.9f + (tod.Phase == TimeOfDayPhase.Evening ? 0.25f : 0f));

            float ease = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, lightingEaseSeconds * 0.35f));
            if (!_initialized)
            {
                ease = 1f;
                _initialized = true;
            }

            _smoothedSunIntensity = Mathf.Lerp(_smoothedSunIntensity, targetSunIntensity, ease);
            _smoothedShadowStrength = Mathf.Lerp(_smoothedShadowStrength, tod.ShadowStrength, ease);
            _smoothedSunColor = Color.Lerp(_smoothedSunColor, targetSunColor, ease);
            _smoothedAmbient = Color.Lerp(_smoothedAmbient, targetAmb, ease);
            _smoothedFogColor = Color.Lerp(_smoothedFogColor, targetFog, ease);
            _smoothedFogEnd = Mathf.Lerp(_smoothedFogEnd, targetFogEnd, ease);
            _smoothedFogWeight = Mathf.Lerp(_smoothedFogWeight, targetFogWeight, ease);

            if (sunLight != null)
            {
                sunLight.intensity = _smoothedSunIntensity;
                sunLight.shadowStrength = _smoothedShadowStrength;
                sunLight.color = _smoothedSunColor;

                var dir = new Vector3(tod.SunDirX, tod.SunDirY, tod.SunDirZ);
                if (dir.sqrMagnitude > 0.0001f)
                    sunLight.transform.rotation = Quaternion.Slerp(
                        sunLight.transform.rotation,
                        Quaternion.LookRotation(-dir.normalized),
                        ease);
            }

            if (!driveRenderSettings)
                return;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = _smoothedAmbient;

            bool fogOn = _smoothedFogWeight > 0.04f;
            RenderSettings.fog = fogOn;
            if (!fogOn)
                return;

            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = _smoothedFogEnd;
            RenderSettings.fogColor = _smoothedFogColor;
        }

        private Color SampleAmbient(float time01)
        {
            // Continuous ambient across the day using the same phase windows as TimeOfDaySystem.
            if (time01 < 0.08f)
                return Color.Lerp(nightAmbient, dawnAmbient, Smooth01(time01 / 0.08f));
            if (time01 < 0.28f)
                return Color.Lerp(dawnAmbient, dayAmbient, Smooth01((time01 - 0.08f) / 0.2f));
            if (time01 < 0.52f)
                return dayAmbient;
            if (time01 < 0.65f)
                return Color.Lerp(dayAmbient, Color.Lerp(dayAmbient, duskAmbient, 0.45f), Smooth01((time01 - 0.52f) / 0.13f));
            if (time01 < 0.75f)
                return Color.Lerp(Color.Lerp(dayAmbient, duskAmbient, 0.45f), duskAmbient, Smooth01((time01 - 0.65f) / 0.1f));
            float nightT = Mathf.Min(1f, (time01 - 0.75f) / 0.25f * 2f);
            return Color.Lerp(duskAmbient, nightAmbient, Smooth01(nightT));
        }

        private static Color SampleSunColor(float time01)
        {
            Color day = Color.white;
            Color warm = new Color(1f, 0.82f, 0.65f);
            if (time01 < 0.08f)
                return Color.Lerp(warm, day, Smooth01(time01 / 0.08f));
            if (time01 < 0.52f)
                return day;
            if (time01 < 0.75f)
                return Color.Lerp(day, warm, Smooth01((time01 - 0.52f) / 0.23f));
            return Color.Lerp(warm, new Color(0.55f, 0.6f, 0.85f), Smooth01(Mathf.Min(1f, (time01 - 0.75f) / 0.12f)));
        }

        private static float NightFactor(float time01)
        {
            if (time01 < 0.65f)
                return 0f;
            if (time01 < 0.75f)
                return Smooth01((time01 - 0.65f) / 0.1f) * 0.65f;
            return Mathf.Lerp(0.65f, 1f, Smooth01(Mathf.Min(1f, (time01 - 0.75f) / 0.12f)));
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
