#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Asterra.Editor
{
    /// <summary>
    /// Pink Game view / "Default Renderer is missing" = URP asset assigned but renderer slot broken.
    /// Creates and wires AsterraURP + AsterraUniversalRenderer automatically.
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
            EnsureFolder("Assets/Asterra");
            EnsureFolder("Assets/Asterra/Shared");
            EnsureFolder(SettingsFolder);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                force = true;
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            bool rendererMissing = pipeline == null
                                   || pipeline.rendererDataList == null
                                   || pipeline.rendererDataList.Length == 0
                                   || pipeline.rendererDataList[0] == null;

            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                force = true;
            }
            else if (rendererMissing)
            {
                // Broken GUID / missing default renderer — recreate a clean asset.
                AssetDatabase.DeleteAsset(PipelinePath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                force = true;
            }

            bool graphicsWrong = GraphicsSettings.defaultRenderPipeline != pipeline
                                 || QualitySettings.renderPipeline != pipeline;
            if (!force && !graphicsWrong && !rendererMissing)
                return;

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            int count = QualitySettings.names.Length;
            for (int i = 0; i < count; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }

            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            Debug.Log("[Asterra] URP pipeline assigned with default renderer. Pink Game view should clear — press Play again.");
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
