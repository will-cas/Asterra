using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Asterra.Core.World;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Outdoor lighting from time of day and weather: sun, moon, trilight bounce, sky, fog.
    /// </summary>
    public sealed class DayNightLightingPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Light sunLight;
        [SerializeField] private bool driveRenderSettings = true;
        [SerializeField] private float lightingEaseSeconds = 3.2f;

        private Light _moon;
        private Material _sky;
        private Volume _volume;
        private ColorAdjustments _colorAdjust;
        private WhiteBalance _whiteBalance;
        private Bloom _bloom;
        private float _cloudOffsetX;
        private float _cloudOffsetZ;
        private float _smoothedSunIntensity = AsterraLightingLook.NoonSunIntensity;
        private float _smoothedShadowStrength = 0.9f;
        private float _smoothedKelvin = 5600f;
        private Color _smoothedSky = new Color(0.42f, 0.62f, 0.92f);
        private Color _smoothedEquator = new Color(0.72f, 0.78f, 0.82f);
        private Color _smoothedGround = new Color(0.28f, 0.24f, 0.18f);
        private Color _smoothedFog = new Color(0.62f, 0.72f, 0.82f);
        private float _smoothedFogDensity = 0.0011f;
        private float _smoothedExposure = 1.15f;
        private float _smoothedAtmosphere = 1.05f;
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
                    if (lights[i].type == LightType.Directional
                        && lights[i].name != "LightningFlash"
                        && lights[i].name != "Moon")
                    {
                        sunLight = lights[i];
                        break;
                    }
                }
            }

            if (sunLight != null)
                AsterraLightingLook.ConfigureSun(sunLight);
            _moon = AsterraLightingLook.EnsureMoon(transform);
            _sky = AsterraLightingLook.EnsureProceduralSky(_sky);
            EnsureVolume();
            if (Camera.main != null)
                AsterraLightingLook.ConfigureCamera(Camera.main);
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
            float overcast = Mathf.Clamp01(precip * 0.55f + fogDen * 0.7f + snow * 0.35f);
            if (weather.Kind == WeatherKind.Storm)
                overcast = Mathf.Max(overcast, 0.45f + weather.Intensity * 0.45f);
            else if (weather.Kind == WeatherKind.Cloudy)
                overcast = Mathf.Max(overcast, 0.4f);

            float time01 = tod.Time01;
            float night = AsterraLightingLook.NightAmount(time01);
            float day = 1f - night;

            float sunMul = Mathf.Lerp(1f, 0.28f, overcast);
            if (weather.Kind == WeatherKind.Storm)
                sunMul *= Mathf.Lerp(1f, 0.55f, weather.Intensity);

            float targetSun = tod.SunIntensity * AsterraLightingLook.NoonSunIntensity * sunMul;
            targetSun = Mathf.Lerp(targetSun, AsterraLightingLook.MoonIntensity * 0.35f, night);
            targetSun = Mathf.Max(0.02f, targetSun);

            float targetKelvin = AsterraLightingLook.SunKelvin(time01);
            targetKelvin = Mathf.Lerp(targetKelvin, 6800f, overcast * 0.55f);
            targetKelvin = Mathf.Lerp(targetKelvin, 7500f, night * 0.85f);

            float targetShadow = Mathf.Lerp(tod.ShadowStrength, 0.22f, overcast);
            targetShadow = Mathf.Lerp(targetShadow, 0.35f, night);

            Color sunRgb = AsterraLightingLook.KelvinRgb(targetKelvin);
            Color zenithDay = Color.Lerp(new Color(0.23f, 0.48f, 0.88f), new Color(0.55f, 0.62f, 0.7f), overcast);
            Color zenithNight = new Color(0.03f, 0.04f, 0.09f);
            Color targetSky = Color.Lerp(zenithDay, zenithNight, night);
            targetSky = Color.Lerp(targetSky, sunRgb * 0.35f, (1f - night) * (1f - overcast) * 0.12f);

            Color targetEquator = Color.Lerp(
                Color.Lerp(new Color(0.78f, 0.84f, 0.9f), sunRgb, 0.22f),
                new Color(0.08f, 0.1f, 0.16f),
                night);
            targetEquator = Color.Lerp(targetEquator, new Color(0.5f, 0.54f, 0.58f), overcast * day);

            Color targetGround = Color.Lerp(new Color(0.32f, 0.28f, 0.18f), new Color(0.06f, 0.07f, 0.08f), night);
            if (snow > 0.05f)
                targetGround = Color.Lerp(targetGround, new Color(0.72f, 0.78f, 0.84f), snow * day);

            float targetFogDensity = Mathf.Lerp(0.00085f, 0.0026f, overcast);
            targetFogDensity = Mathf.Lerp(targetFogDensity, 0.0022f, night);
            targetFogDensity += fogDen * 0.0018f;

            Color targetFog = Color.Lerp(targetEquator, targetSky, 0.45f);
            targetFog = Color.Lerp(targetFog, new Color(0.45f, 0.5f, 0.55f), precip * 0.5f);

            float targetExposure = Mathf.Lerp(1.18f, 0.72f, night);
            targetExposure = Mathf.Lerp(targetExposure, 0.88f, overcast * day);
            float targetAtmosphere = Mathf.Lerp(1.02f, 1.55f, overcast);
            targetAtmosphere = Mathf.Lerp(targetAtmosphere, 0.78f, night);

            float ease = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, lightingEaseSeconds * 0.32f));
            if (!_initialized)
            {
                ease = 1f;
                _initialized = true;
            }

            _smoothedSunIntensity = Mathf.Lerp(_smoothedSunIntensity, targetSun, ease);
            _smoothedShadowStrength = Mathf.Lerp(_smoothedShadowStrength, targetShadow, ease);
            _smoothedKelvin = Mathf.Lerp(_smoothedKelvin, targetKelvin, ease);
            _smoothedSky = Color.Lerp(_smoothedSky, targetSky, ease);
            _smoothedEquator = Color.Lerp(_smoothedEquator, targetEquator, ease);
            _smoothedGround = Color.Lerp(_smoothedGround, targetGround, ease);
            _smoothedFog = Color.Lerp(_smoothedFog, targetFog, ease);
            _smoothedFogDensity = Mathf.Lerp(_smoothedFogDensity, targetFogDensity, ease);
            _smoothedExposure = Mathf.Lerp(_smoothedExposure, targetExposure, ease);
            _smoothedAtmosphere = Mathf.Lerp(_smoothedAtmosphere, targetAtmosphere, ease);

            if (sunLight != null)
            {
                sunLight.intensity = _smoothedSunIntensity;
                sunLight.shadowStrength = _smoothedShadowStrength;
                sunLight.useColorTemperature = true;
                sunLight.colorTemperature = _smoothedKelvin;
                sunLight.color = Color.white;
                var dir = new Vector3(tod.SunDirX, tod.SunDirY, tod.SunDirZ);
                if (dir.sqrMagnitude > 0.0001f)
                    sunLight.transform.rotation = Quaternion.Slerp(
                        sunLight.transform.rotation,
                        Quaternion.LookRotation(-dir.normalized),
                        ease);
            }

            if (_moon != null)
            {
                _moon.intensity = night * AsterraLightingLook.MoonIntensity * Mathf.Lerp(1f, 0.4f, overcast);
                _moon.colorTemperature = 7200f;
                if (sunLight != null)
                {
                    Vector3 moonDir = -(sunLight.transform.forward);
                    moonDir.y = Mathf.Abs(moonDir.y) * 0.45f + 0.35f;
                    _moon.transform.rotation = Quaternion.LookRotation(-moonDir.normalized);
                }
            }

            float nightDim = AsterraLightingLook.NightAmount(time01);
            _cloudOffsetX += Time.deltaTime * Mathf.Lerp(0.006f, 0.018f, overcast);
            _cloudOffsetZ += Time.deltaTime * 0.004f;
            AsterraLightingLook.ApplyCloudGlobals(
                Mathf.Lerp(0.08f, 0.72f, overcast) * Mathf.Lerp(1f, 0.35f, nightDim),
                new Vector2(_cloudOffsetX, _cloudOffsetZ));
            AsterraLightingLook.TickLightning(Time.deltaTime);

            if (!driveRenderSettings)
                return;

            RenderSettings.sun = sunLight;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _smoothedSky;
            RenderSettings.ambientEquatorColor = _smoothedEquator;
            RenderSettings.ambientGroundColor = _smoothedGround;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.85f, 0.22f, night);

            if (_sky != null)
            {
                if (_sky.HasProperty("_SkyTint"))
                    _sky.SetColor("_SkyTint", Color.Lerp(Color.white, new Color(0.55f, 0.6f, 0.7f), overcast));
                if (_sky.HasProperty("_GroundColor"))
                    _sky.SetColor("_GroundColor", _smoothedGround);
                if (_sky.HasProperty("_AtmosphereThickness"))
                    _sky.SetFloat("_AtmosphereThickness", _smoothedAtmosphere);
                if (_sky.HasProperty("_Exposure"))
                    _sky.SetFloat("_Exposure", _smoothedExposure);
                RenderSettings.skybox = _sky;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = _smoothedFogDensity;
            RenderSettings.fogColor = _smoothedFog;

            if (_colorAdjust != null)
            {
                float pulse = AsterraLightingLook.LightningBloom;
                _colorAdjust.postExposure.value = Mathf.Lerp(0.12f, -0.35f, night) + pulse * 0.85f;
                _colorAdjust.contrast.value = Mathf.Lerp(8f, 4f, overcast);
                _colorAdjust.saturation.value = Mathf.Lerp(6f, -8f, overcast);
            }

            if (_whiteBalance != null)
                _whiteBalance.temperature.value = Mathf.Lerp(8f, -12f, night);

            if (_bloom != null)
                _bloom.intensity.value = 0.08f + AsterraLightingLook.LightningBloom * 1.4f;
        }

        private void EnsureVolume()
        {
            if (_volume != null)
                return;
            try
            {
                var go = new GameObject("AsterraPost");
                go.transform.SetParent(transform, false);
                _volume = go.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 10f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _volume.sharedProfile = profile;

                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.ACES);

                _colorAdjust = profile.Add<ColorAdjustments>(true);
                _colorAdjust.postExposure.Override(0.05f);
                _colorAdjust.contrast.Override(7f);
                _colorAdjust.saturation.Override(3f);

                _whiteBalance = profile.Add<WhiteBalance>(true);
                _whiteBalance.temperature.Override(2f);

                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(0.08f);
                bloom.threshold.Override(1.15f);
                bloom.scatter.Override(0.65f);
                _bloom = bloom;
            }
            catch (System.Exception)
            {
                _volume = null;
                _colorAdjust = null;
                _whiteBalance = null;
                _bloom = null;
            }
        }
    }
}
