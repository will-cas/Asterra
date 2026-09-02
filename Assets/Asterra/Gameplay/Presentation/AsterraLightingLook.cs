using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Outdoor look for Asterra: sun disk, trilight bounce, and a procedural sky.
    /// Intensities assume URP with linear light intensity (not physical lux).
    /// </summary>
    public static class AsterraLightingLook
    {
        public const float NoonSunIntensity = 2.15f;
        public const float MoonIntensity = 0.16f;

        public static void ConfigureSun(Light sun)
        {
            if (sun == null)
                return;
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.35f;
            sun.shadowNearPlane = 1.2f;
            sun.useColorTemperature = true;
            sun.color = Color.white;
            var data = sun.GetComponent<UniversalAdditionalLightData>();
            if (data == null)
                data = sun.gameObject.AddComponent<UniversalAdditionalLightData>();
            data.usePipelineSettings = true;
        }

        public static Light EnsureMoon(Transform parent)
        {
            Transform t = parent != null ? parent.Find("Moon") : null;
            Light moon = t != null ? t.GetComponent<Light>() : null;
            if (moon == null)
            {
                var go = new GameObject("Moon");
                if (parent != null)
                    go.transform.SetParent(parent, false);
                moon = go.AddComponent<Light>();
            }

            moon.type = LightType.Directional;
            moon.shadows = LightShadows.None;
            moon.useColorTemperature = true;
            moon.colorTemperature = 7100f;
            moon.color = Color.white;
            moon.intensity = 0f;
            var data = moon.GetComponent<UniversalAdditionalLightData>();
            if (data == null)
                moon.gameObject.AddComponent<UniversalAdditionalLightData>();
            return moon;
        }

        public static Material EnsureProceduralSky(Material existing)
        {
            if (existing != null)
                return existing;
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
                return null;
            var mat = new Material(shader) { name = "AsterraSky" };
            if (mat.HasProperty("_SunDisk"))
                mat.SetFloat("_SunDisk", 2f);
            if (mat.HasProperty("_SunSize"))
                mat.SetFloat("_SunSize", 0.04f);
            if (mat.HasProperty("_SunSizeConvergence"))
                mat.SetFloat("_SunSizeConvergence", 5f);
            return mat;
        }

        public static void ConfigureCamera(Camera cam)
        {
            if (cam == null)
                return;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.clearFlags = CameraClearFlags.Skybox;
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
                data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderShadows = true;
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.Medium;
        }

        /// <summary>Approximate CIE D-series daylight / blackbody to linear RGB.</summary>
        public static Color KelvinRgb(float kelvin)
        {
            float k = Mathf.Clamp(kelvin, 1000f, 12000f) / 100f;
            float r, g, b;
            if (k <= 66f)
                r = 1f;
            else
                r = Mathf.Clamp01(1.2929362f * Mathf.Pow(k - 60f, -0.1332048f));

            if (k <= 66f)
                g = Mathf.Clamp01(0.3900816f * Mathf.Log(k) - 0.6318414f);
            else
                g = Mathf.Clamp01(1.129891f * Mathf.Pow(k - 60f, -0.0755148f));

            if (k >= 66f)
                b = 1f;
            else if (k <= 19f)
                b = 0f;
            else
                b = Mathf.Clamp01(0.5432068f * Mathf.Log(k - 10f) - 1.1962541f);

            return new Color(r, g, b, 1f);
        }

        public static float SunKelvin(float time01)
        {
            time01 = Mathf.Repeat(time01, 1f);
            if (time01 < 0.08f)
                return Mathf.Lerp(2200f, 4300f, Smooth01(time01 / 0.08f));
            if (time01 < 0.28f)
                return Mathf.Lerp(4300f, 5600f, Smooth01((time01 - 0.08f) / 0.2f));
            if (time01 < 0.52f)
                return 5600f;
            if (time01 < 0.65f)
                return Mathf.Lerp(5600f, 3800f, Smooth01((time01 - 0.52f) / 0.13f));
            if (time01 < 0.75f)
                return Mathf.Lerp(3800f, 2400f, Smooth01((time01 - 0.65f) / 0.1f));
            return Mathf.Lerp(2400f, 7500f, Smooth01(Mathf.Min(1f, (time01 - 0.75f) / 0.12f)));
        }

        public static float NightAmount(float time01)
        {
            time01 = Mathf.Repeat(time01, 1f);
            if (time01 < 0.62f)
                return 0f;
            if (time01 < 0.75f)
                return Smooth01((time01 - 0.62f) / 0.13f);
            if (time01 < 0.97f)
                return 1f;
            return Smooth01((1f - time01) / 0.03f);
        }

        public static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float LightningBloom { get; private set; }

        public static void PulseLightning(float intensity)
        {
            LightningBloom = Mathf.Max(LightningBloom, 0.35f + Mathf.Clamp01(intensity) * 1.15f);
        }

        public static void TickLightning(float deltaTime)
        {
            LightningBloom = Mathf.MoveTowards(LightningBloom, 0f, Mathf.Max(0.01f, deltaTime) * 5.2f);
        }

        public static void ApplyCloudGlobals(float strength, Vector2 offset)
        {
            Shader.SetGlobalFloat("_AsterraCloudStrength", Mathf.Clamp01(strength));
            Shader.SetGlobalVector("_AsterraCloudParams", new Vector4(0.0036f, 0.0036f, offset.x, offset.y));
        }

        public static void ApplyWindGlobals(float dirX, float dirZ, float intensity)
        {
            Shader.SetGlobalVector("_AsterraWind", new Vector4(dirX, Mathf.Clamp(intensity, 0.08f, 1.8f), dirZ, Time.time));
        }
    }
}
