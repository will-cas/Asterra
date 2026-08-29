#if UNITY_EDITOR
using Asterra.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Asterra.Editor
{
    /// <summary>
    /// Builds the playable offline skirmish entry scene (MatchBootstrap + camera + light + ground).
    /// Presentation/terrain/HUD attach at match start from MatchBootstrap.
    /// </summary>
    public static class SkirmishSceneBuilder
    {
        public const string ScenePath = "Assets/Asterra/Shared/Scenes/Skirmish.unity";

        [MenuItem("Asterra/Build Skirmish Scene")]
        public static void BuildFromMenu()
        {
            Build(saveAndQuit: false);
        }

        /// <summary>Unity batchmode: -executeMethod Asterra.Editor.SkirmishSceneBuilder.BuildFromCommandLine</summary>
        public static void BuildFromCommandLine()
        {
            Build(saveAndQuit: true);
        }

        public static void Build(bool saveAndQuit)
        {
            EnsureFolder("Assets/Asterra");
            EnsureFolder("Assets/Asterra/Shared");
            EnsureFolder("Assets/Asterra/Shared/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Sun
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Main camera (rig creates one if missing; having it in-scene is clearer for Play)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 3500f;
            cam.transform.position = new Vector3(-320f, 200f, -110f);
            cam.transform.LookAt(new Vector3(-320f, 0f, 0f));
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            // Fallback flat ground (TerrainGridPresenter hides this once fantasy mesh exists)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "AsterraGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(120f, 1f, 120f);
            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                       ?? Shader.Find("Standard"));
                mat.color = new Color(0.28f, 0.36f, 0.24f);
                renderer.sharedMaterial = mat;
            }

            // Match composition root
            var matchGo = new GameObject("Match");
            var coordinator = matchGo.AddComponent<LockstepMatchCoordinator>();
            var bootstrap = matchGo.AddComponent<MatchBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("playMode").enumValueIndex = (int)MatchPlayMode.OfflineVsAi;
            so.FindProperty("playerFactionIndex").intValue = 0;
            so.FindProperty("enemyFactionIndex").intValue = 1;
            so.FindProperty("mapId").enumValueIndex = (int)Asterra.Gameplay.Content.SkirmishMapId.BlackridgePass;
            so.FindProperty("mapKey").stringValue = Asterra.Gameplay.Content.MapCatalog.BlackridgePassId;
            so.FindProperty("tickHz").floatValue = 20f;
            so.FindProperty("commandDelayTicks").intValue = 2;
            so.FindProperty("startingGold").intValue = 500;
            so.FindProperty("startingTimber").intValue = 300;
            so.FindProperty("enemyStartingGold").intValue = 500;
            so.FindProperty("runSmokeOnAwake").boolValue = false;
            so.FindProperty("reportHashEveryTick").boolValue = false;
            so.FindProperty("autoStartOffline").boolValue = false;
            so.FindProperty("attachLocalOrders").boolValue = true;
            so.FindProperty("attachPresentation").boolValue = true;
            so.FindProperty("attachCameraRig").boolValue = true;
            so.FindProperty("territoryHoldSecondsToWin").floatValue = 90f;
            so.FindProperty("matchSeed").uintValue = 42;
            so.FindProperty("coordinator").objectReferenceValue = coordinator;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Audio (DDOL singleton also auto-creates; bake into scene for first Play)
            matchGo.AddComponent<Asterra.Gameplay.Audio.AsterraAudio>();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[Asterra] Failed to save Skirmish.unity");
                if (saveAndQuit)
                    EditorApplication.Exit(1);
                return;
            }

            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Asterra] Skirmish scene ready at {ScenePath}. Open it and press Play → OfflineMatchMenu.");

            if (saveAndQuit)
                EditorApplication.Exit(0);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    if (!scenes[i].enabled)
                    {
                        scenes[i].enabled = true;
                        EditorBuildSettings.scenes = scenes;
                    }

                    return;
                }
            }

            var next = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
                next[i] = scenes[i];
            next[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = next;
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
