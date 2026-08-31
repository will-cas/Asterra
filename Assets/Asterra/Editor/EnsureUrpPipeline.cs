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
            WirePostProcessData(renderer);
            EnsureSsao(renderer);
            if (!force && !graphicsWrong && !rendererMissing)
                return;

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            ApplyOutdoorQuality(pipeline);
            WirePostProcessData(renderer);
            EnsureSsao(renderer);

            int count = QualitySettings.names.Length;
            for (int i = 0; i < count; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }

            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            Debug.Log("[Asterra] URP pipeline assigned with outdoor lighting (cascaded shadows, HDR). Pink Game view should clear — press Play again.");
        }

        private static void ApplyOutdoorQuality(UniversalRenderPipelineAsset pipeline)
        {
            if (pipeline == null)
                return;
            var so = new SerializedObject(pipeline);
            SetFloat(so, "m_ShadowDistance", 480f);
            SetInt(so, "m_ShadowCascadeCount", 4);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_MSAA", 4);
            SetInt(so, "m_RequireDepthTexture", 1);
            SetInt(so, "m_RequireOpaqueTexture", 1);
            SetInt(so, "m_MainLightShadowmapResolution", 4096);
            SetInt(so, "m_ColorGradingMode", 1);
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 6);
            SetFloat(so, "m_ShadowDepthBias", 0.4f);
            SetFloat(so, "m_ShadowNormalBias", 0.35f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            var p = so.FindProperty(name);
            if (p != null)
                p.floatValue = value;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            var p = so.FindProperty(name);
            if (p != null)
                p.boolValue = value;
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            var p = so.FindProperty(name);
            if (p != null)
                p.intValue = value;
        }

        private static void WirePostProcessData(UniversalRendererData renderer)
        {
            if (renderer == null)
                return;
            var pp = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if (pp == null)
            {
                string[] found = AssetDatabase.FindAssets("t:PostProcessData");
                if (found != null && found.Length > 0)
                    pp = AssetDatabase.LoadAssetAtPath<PostProcessData>(AssetDatabase.GUIDToAssetPath(found[0]));
            }

            if (pp == null)
                return;
            var so = new SerializedObject(renderer);
            var prop = so.FindProperty("postProcessData");
            if (prop != null && prop.objectReferenceValue != pp)
            {
                prop.objectReferenceValue = pp;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void EnsureSsao(UniversalRendererData renderer)
        {
            if (renderer == null)
                return;

            ScreenSpaceAmbientOcclusion ssao = null;
            var features = renderer.rendererFeatures;
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i] is ScreenSpaceAmbientOcclusion existing)
                {
                    ssao = existing;
                    break;
                }
            }

            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "ScreenSpaceAmbientOcclusion";
                AssetDatabase.AddObjectToAsset(ssao, renderer);
                features.Add(ssao);

                var so = new SerializedObject(renderer);
                var map = so.FindProperty("m_RendererFeatureMap");
                if (map != null)
                {
                    map.arraySize = features.Count;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long localId))
                        map.GetArrayElementAtIndex(features.Count - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                ssao.Create();
                TuneSsao(ssao);
                renderer.SetDirty();
                EditorUtility.SetDirty(ssao);
                EditorUtility.SetDirty(renderer);
                AssetDatabase.SaveAssets();
            }
        }

        private static void TuneSsao(ScreenSpaceAmbientOcclusion ssao)
        {
            if (ssao == null)
                return;
            ssao.SetActive(true);
            var so = new SerializedObject(ssao);
            var settings = so.FindProperty("m_Settings");
            if (settings == null)
                return;
            SetFloatRel(settings, "Intensity", 1.7f);
            SetFloatRel(settings, "Radius", 0.32f);
            SetFloatRel(settings, "Falloff", 280f);
            SetFloatRel(settings, "DirectLightingStrength", 0.38f);
            var source = settings.FindPropertyRelative("Source");
            if (source != null)
                source.enumValueIndex = 1;
            var downsample = settings.FindPropertyRelative("Downsample");
            if (downsample != null)
                downsample.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloatRel(SerializedProperty parent, string name, float value)
        {
            var p = parent.FindPropertyRelative(name);
            if (p != null)
                p.floatValue = value;
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
