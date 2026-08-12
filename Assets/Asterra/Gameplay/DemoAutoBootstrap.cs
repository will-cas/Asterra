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

            if (Object.FindFirstObjectByType<MatchBootstrap>() != null)
            {
                EnsureLighting();
                return;
            }

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
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.intensity = 1.35f;
                light.shadows = LightShadows.Soft;
                lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.42f);
        }
    }
}
