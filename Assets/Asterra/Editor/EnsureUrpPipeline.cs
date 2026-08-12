#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Asterra.Editor
{
    /// <summary>
    /// Pink Game view = URP package present but no pipeline asset assigned.
    /// Creates and wires a default URP asset automatically.
    /// </summary>
    [InitializeOnLoad]
    public static class EnsureUrpPipeline
    {
        private const string SettingsFolder = "Assets/Asterra/Shared/Settings";
        private const string RendererPath = SettingsFolder + "/AsterraUniversalRenderer.asset";
        private const string PipelinePath = SettingsFolder + "/AsterraURP.asset";

        static EnsureUrpPipeline()
        {
            EditorApplication.delayCall += Ensure;
        }

        [MenuItem("Asterra/Fix Rendering (Assign URP)")]
        private static void MenuFix()
        {
            Ensure(force: true);
        }

        private static void Ensure() => Ensure(force: false);

        private static void Ensure(bool force)
        {
            if (!force && GraphicsSettings.defaultRenderPipeline != null)
                return;

            EnsureFolder("Assets/Asterra");
            EnsureFolder("Assets/Asterra/Shared");
            EnsureFolder(SettingsFolder);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            else if (pipeline.rendererDataList == null || pipeline.rendererDataList.Length == 0 || pipeline.rendererDataList[0] == null)
            {
                // Recreate if corrupted/empty.
                Object.DestroyImmediate(pipeline, true);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            // Apply to every quality level.
            int count = QualitySettings.names.Length;
            for (int i = 0; i < count; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }

            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            Debug.Log("[Asterra] URP pipeline assigned. Pink Game view should clear — press Play again.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
