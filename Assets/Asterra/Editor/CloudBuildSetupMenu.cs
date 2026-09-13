using UnityEditor;
using UnityEngine;

namespace Asterra.Editor
{
    /// <summary>Points engineers at the M1 Cloud Build stub docs.</summary>
    public static class CloudBuildSetupMenu
    {
        [MenuItem("Asterra/Cloud Build/Open Setup Doc")]
        public static void OpenDoc()
        {
            var path = "Docs/CLOUD_BUILD.md";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                return;
            }

            // Markdown may not import as TextAsset; open via Application.
            var full = System.IO.Path.GetFullPath(path);
            if (System.IO.File.Exists(full))
            {
                EditorUtility.RevealInFinder(full);
                Application.OpenURL("file://" + full);
            }
            else
                Debug.LogWarning("[Asterra] Docs/CLOUD_BUILD.md not found.");
        }

        [MenuItem("Asterra/Cloud Build/Log Local Build Info")]
        public static void LogInfo()
        {
            Asterra.Gameplay.Analytics.CloudBuildInfo.LogBootstrap();
        }
    }
}
