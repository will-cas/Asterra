using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Ensures a playable offline skirmish exists even when the open scene is empty.
    /// </summary>
    public static class DemoAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePlayableDemo()
        {
            if (!Application.isPlaying)
                return;

            EnsureLighting();
            _ = Asterra.Gameplay.Audio.AsterraAudio.Instance;
            if (Object.FindFirstObjectByType<Asterra.Gameplay.Presentation.AsterraAmbiencePresenter>() == null)
            {
                var amb = new GameObject("AsterraAmbience");
                amb.AddComponent<Asterra.Gameplay.Presentation.AsterraAmbiencePresenter>();
            }

            if (Object.FindFirstObjectByType<MatchBootstrap>() != null)
                return;

            Debug.Log("[Asterra] Empty scene detected — spawning offline 1v1 demo root.");
            EnsureLighting();
            var root = new GameObject("AsterraDemo");
            // autoStartOffline defaults to false → OfflineMatchMenu appears before skirmish.
            root.AddComponent<MatchBootstrap>();
        }

        private static void EnsureLighting()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDirectional = false;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    hasDirectional = true;
                    break;
                }
            }

            if (!hasDirectional)
            {
                var lightGo = new GameObject("Asterra Sun");
                var light = lightGo.AddComponent<Light>();
                Asterra.Gameplay.Presentation.AsterraLightingLook.ConfigureSun(light);
                light.intensity = Asterra.Gameplay.Presentation.AsterraLightingLook.NoonSunIntensity;
                light.colorTemperature = 5600f;
                lightGo.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.55f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.76f, 0.78f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.24f, 0.16f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0009f;
            RenderSettings.fogColor = new Color(0.62f, 0.72f, 0.82f);
            if (Camera.main != null)
                Asterra.Gameplay.Presentation.AsterraLightingLook.ConfigureCamera(Camera.main);
        }
    }
}
