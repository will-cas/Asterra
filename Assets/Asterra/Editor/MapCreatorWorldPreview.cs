using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using Asterra.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Asterra.EditorTools
{
    /// <summary>
    /// Isolated Scene View world for the map creator: heightfield + authored props.
    /// </summary>
    public sealed class MapCreatorWorldPreview
    {
        private Scene _scene;
        private bool _ownsScene;
        private GameObject _root;
        private Transform _overlay;
        private TerrainGridPresenter _terrain;
        private Light _sun;
        private readonly List<Material> _mats = new();
        public bool IsActive => _ownsScene && _scene.IsValid();
        public Scene Scene => _scene;
        public TerrainGridPresenter Terrain => _terrain;

        private static readonly PropertyInfo SceneViewCustomScene =
            typeof(SceneView).GetProperty("customScene", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public void EnsureActive()
        {
            if (IsActive)
                return;

            _scene = EditorSceneManager.NewPreviewScene();
            _ownsScene = true;

            _root = new GameObject("AsterraMapCreatorWorld");
            SceneManager.MoveGameObjectToScene(_root, _scene);

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(_root.transform, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            _sun = sunGo.AddComponent<Light>();
            AsterraLightingLook.ConfigureSun(_sun);
            _sun.intensity = AsterraLightingLook.NoonSunIntensity;
            _sun.colorTemperature = 5450f;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_root.transform, false);
            fillGo.transform.rotation = Quaternion.Euler(18f, 145f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.useColorTemperature = true;
            fill.colorTemperature = 14000f;
            fill.color = Color.white;
            fill.intensity = 0.22f;
            fill.shadows = LightShadows.None;
            fillGo.AddComponent<UniversalAdditionalLightData>();

            var terrainGo = new GameObject("Terrain");
            terrainGo.transform.SetParent(_root.transform, false);
            _terrain = terrainGo.AddComponent<TerrainGridPresenter>();
            _terrain.HideSkirmishGround = false;

            var overlayGo = new GameObject("AuthoredProps");
            overlayGo.transform.SetParent(_root.transform, false);
            _overlay = overlayGo.transform;

            BindSceneViews();
        }

        public void Dispose()
        {
            UnbindSceneViews();
            for (int i = 0; i < _mats.Count; i++)
            {
                if (_mats[i] != null)
                    Object.DestroyImmediate(_mats[i]);
            }

            _mats.Clear();
            _terrain = null;
            _overlay = null;
            _root = null;
            _sun = null;
            if (_ownsScene && _scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            _ownsScene = false;
            _scene = default;
        }

        public void Sync(MapDefinition map, bool rebuildTerrain)
        {
            EnsureActive();
            if (map == null)
                return;
            map.EnsureArrays();
            if (rebuildTerrain)
                RebuildTerrain(map);
            RebuildOverlays(map);
            SceneView.RepaintAll();
        }

        public void Frame()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return;
            view.LookAt(new Vector3(0f, 8f, 0f), Quaternion.Euler(52f, 18f, 0f), 520f);
            view.Repaint();
        }

        public bool TryPick(Ray ray, out Vector3 hit)
        {
            hit = default;
            if (_terrain != null)
            {
                const float maxDist = 4000f;
                const float step = 8f;
                float prevY = ray.origin.y - _terrain.SampleHeight(ray.origin.x, ray.origin.z);
                for (float t = step; t <= maxDist; t += step)
                {
                    var p = ray.GetPoint(t);
                    float dy = p.y - _terrain.SampleHeight(p.x, p.z);
                    if (prevY > 0f && dy <= 0f)
                    {
                        float u = prevY / (prevY - dy + 0.0001f);
                        hit = ray.GetPoint(t - step + step * u);
                        return true;
                    }

                    prevY = dy;
                }
            }

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float dist))
                return false;
            hit = ray.GetPoint(dist);
            if (_terrain != null)
                hit.y = _terrain.SampleHeight(hit.x, hit.z);
            return true;
        }

        public float GroundY(float x, float z)
        {
            return _terrain != null ? _terrain.SampleHeight(x, z) : 0f;
        }

        public void DrawSceneGizmos(MapDefinition map, float brushRadius, Vector3? hover)
        {
            if (map == null)
                return;
            map.EnsureArrays();

            if (map.traversalLinks != null)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                for (int i = 0; i < map.traversalLinks.Length; i++)
                {
                    var link = map.traversalLinks[i];
                    float y0 = GroundY(link.startX, link.startZ) + 2f;
                    float y1 = GroundY(link.endX, link.endZ) + 2f;
                    Handles.DrawAAPolyLine(
                        4f,
                        new Vector3(link.startX, y0, link.startZ),
                        new Vector3(link.endX, y1, link.endZ));
                }
            }

            if (map.territories != null)
            {
                Handles.color = new Color(0.4f, 0.75f, 1f, 0.35f);
                for (int i = 0; i < map.territories.Length; i++)
                {
                    var t = map.territories[i];
                    Handles.DrawWireDisc(
                        new Vector3(t.x, GroundY(t.x, t.z) + 0.4f, t.z),
                        Vector3.up,
                        t.radius > 1f ? t.radius : 36f);
                }
            }

            if (map.objectives != null)
            {
                Handles.color = new Color(1f, 0.75f, 0.2f, 0.55f);
                for (int i = 0; i < map.objectives.Length; i++)
                {
                    var o = map.objectives[i];
                    float r = o.radius > 1f ? o.radius : 28f;
                    Handles.DrawWireDisc(new Vector3(o.x, GroundY(o.x, o.z) + 0.5f, o.z), Vector3.up, r);
                    Handles.Label(
                        new Vector3(o.x, GroundY(o.x, o.z) + 14f, o.z),
                        (o.required ? "[!] " : "") + (string.IsNullOrEmpty(o.title) ? o.kind : o.title),
                        EditorStyles.whiteLabel);
                }
            }

            if (map.talkTriggers != null)
            {
                Handles.color = new Color(0.7f, 0.85f, 1f, 0.5f);
                for (int i = 0; i < map.talkTriggers.Length; i++)
                {
                    var t = map.talkTriggers[i];
                    Handles.DrawWireDisc(
                        new Vector3(t.x, GroundY(t.x, t.z) + 0.5f, t.z),
                        Vector3.up,
                        t.radius > 1f ? t.radius : 24f);
                    Handles.Label(
                        new Vector3(t.x, GroundY(t.x, t.z) + 12f, t.z),
                        "Talk " + t.conversationId,
                        EditorStyles.whiteLabel);
                }
            }

            if (hover.HasValue)
            {
                Handles.color = new Color(1f, 0.95f, 0.45f, 0.85f);
                Handles.DrawWireDisc(hover.Value, Vector3.up, Mathf.Max(6f, brushRadius));
                Handles.color = new Color(1f, 0.95f, 0.45f, 0.2f);
                Handles.DrawSolidDisc(hover.Value + Vector3.up * 0.15f, Vector3.up, Mathf.Max(6f, brushRadius));
            }

            float half = map.playableHalfExtent > 10f ? map.playableHalfExtent : 450f;
            Handles.color = new Color(1f, 1f, 1f, 0.18f);
            Handles.DrawWireCube(new Vector3(0f, 1f, 0f), new Vector3(half * 2f, 2f, half * 2f));

            if (map.keeps != null)
            {
                for (int i = 0; i < map.keeps.Length; i++)
                {
                    var k = map.keeps[i];
                    float y = GroundY(k.x, k.z) + 18f;
                    Handles.Label(
                        new Vector3(k.x, y, k.z),
                        k.seatIndex == 0 ? "West keep" : "East keep",
                        EditorStyles.whiteLargeLabel);
                }
            }
        }

        public void CapturePng(string path, Vector3 eye, Vector3 lookAt, int width = 1920, int height = 1080)
        {
            EnsureActive();
            var camGo = new GameObject("MapCreatorCaptureCam");
            SceneManager.MoveGameObjectToScene(camGo, _scene);
            camGo.transform.position = eye;
            camGo.transform.LookAt(lookAt);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 4000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.38f, 0.55f, 0.72f);
            cam.allowHDR = false;
            cam.allowMSAA = false;
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderShadows = true;
            urp.renderPostProcessing = false;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply(false);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);
        }

        private void RebuildTerrain(MapDefinition map)
        {
            float half = map.playableHalfExtent > 10f ? map.playableHalfExtent : 450f;
            float cell = map.cellSize > 0.5f ? map.cellSize : 10f;
            var grid = DefaultTerrainCatalog.CreatePlayableGrid(half, cell);
            if (map.defaultTerrain != DefaultTerrainCatalog.GrassShort)
                grid.FillWorldRect(-half, -half, half, half, map.defaultTerrain);

            var env = new WorldEnvironmentSim(grid);
            SkirmishMapLoader.ApplyTerrain(env, map);
            _terrain.SetTextureStrokes(map.texturePaint);
            _terrain.SetHeightStrokes(map.heightPaint);
            _terrain.RebuildFromGrid(grid);
        }

        private void RebuildOverlays(MapDefinition map)
        {
            if (_overlay == null)
                return;
            for (int i = 0; i < _mats.Count; i++)
            {
                if (_mats[i] != null)
                    Object.DestroyImmediate(_mats[i]);
            }

            _mats.Clear();
            for (int i = _overlay.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_overlay.GetChild(i).gameObject);

            var west = FactionDefaultContent.MundorCrown;
            var east = FactionDefaultContent.Outcast;

            if (map.keeps != null)
            {
                for (int i = 0; i < map.keeps.Length; i++)
                {
                    var k = map.keeps[i];
                    var faction = k.seatIndex == 0 ? west : east;
                    SpawnMesh(
                        $"Keep_{k.seatIndex}",
                        AsterraMeshLibrary.GetBuildingMesh(faction.KeepBuildingId, faction.Id.Value),
                        new Vector3(k.x, GroundY(k.x, k.z), k.z),
                        EntityView.BuildingVisualScale
                        * AsterraMeshLibrary.BuildingVisualMultiplier(faction.KeepBuildingId, faction.Id.Value),
                        AsterraMeshLibrary.FactionBodyColor(faction.Id.Value, false, faction.KeepBuildingId),
                        AsterraMeshLibrary.GetBodyAlbedo(false, faction.KeepBuildingId, faction.Id.Value),
                        k.yawDegrees);
                }
            }

            if (map.units != null)
            {
                for (int i = 0; i < map.units.Length; i++)
                {
                    var u = map.units[i];
                    var faction = u.seatIndex == 0 ? west : east;
                    string defId = UnitDef(u.role, faction);
                    SpawnMesh(
                        $"Unit_{i}",
                        AsterraMeshLibrary.GetUnitMesh(defId),
                        new Vector3(u.x, GroundY(u.x, u.z), u.z),
                        EntityView.UnitVisualScale * AsterraMeshLibrary.RoleScaleMultiplier(
                            AsterraMeshLibrary.InferRole(defId)),
                        AsterraMeshLibrary.FactionBodyColor(faction.Id.Value, true, defId),
                        AsterraMeshLibrary.GetBodyAlbedo(true, defId, faction.Id.Value),
                        u.yawDegrees);
                }
            }

            if (map.resources != null)
            {
                for (int i = 0; i < map.resources.Length; i++)
                {
                    var r = map.resources[i];
                    bool gold = string.Equals(r.type, "gold", System.StringComparison.OrdinalIgnoreCase);
                    var type = gold ? ResourceType.Gold : ResourceType.Timber;
                    SpawnMesh(
                        $"Resource_{i}",
                        AsterraMeshLibrary.GetResourceMesh(type),
                        new Vector3(r.x, GroundY(r.x, r.z), r.z),
                        1.4f,
                        AsterraMeshLibrary.ResourceColor(type),
                        AsterraMeshLibrary.GetPropAlbedo(gold ? "gold" : "timber"),
                        r.yawDegrees);
                }
            }

            if (map.destructibles != null)
            {
                for (int i = 0; i < map.destructibles.Length; i++)
                {
                    var d = map.destructibles[i];
                    string id = d.catalogId ?? "tree";
                    bool scenery = DefaultDestructibleCatalog.IsScenery(id);
                    float scale = scenery
                        ? 2.15f
                        : id.IndexOf("bridge", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 1.15f : 1.35f;
                    string tex = AsterraMeshLibrary.DestructibleTexKey(id);
                    SpawnMesh(
                        $"Prop_{id}_{i}",
                        AsterraMeshLibrary.GetDestructibleMesh(id),
                        new Vector3(d.x, GroundY(d.x, d.z), d.z),
                        scale,
                        AsterraMeshLibrary.DestructibleColor(id),
                        AsterraMeshLibrary.GetPropAlbedo(tex),
                        d.yawDegrees);
                }
            }

            if (map.buildings != null)
            {
                for (int i = 0; i < map.buildings.Length; i++)
                {
                    var b = map.buildings[i];
                    var faction = b.seatIndex == 0 ? west : east;
                    string defId = BuildingDef(b.role, faction);
                    if (string.IsNullOrEmpty(defId))
                        continue;
                    SpawnMesh(
                        $"Building_{i}",
                        AsterraMeshLibrary.GetBuildingMesh(defId, faction.Id.Value),
                        new Vector3(b.x, GroundY(b.x, b.z), b.z),
                        EntityView.BuildingVisualScale
                        * AsterraMeshLibrary.BuildingVisualMultiplier(defId, faction.Id.Value),
                        AsterraMeshLibrary.FactionBodyColor(faction.Id.Value, false, defId),
                        AsterraMeshLibrary.GetBodyAlbedo(false, defId, faction.Id.Value),
                        b.yawDegrees);
                }
            }
        }

        private GameObject SpawnMesh(
            string name,
            Mesh mesh,
            Vector3 pos,
            float scale,
            Color color,
            Texture2D albedo,
            float yawDegrees = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_overlay, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            go.transform.localScale = Vector3.one * Mathf.Max(0.2f, scale);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = MakeMat(color, albedo);
            return go;
        }

        private Material MakeMat(Color color, Texture2D albedo)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (albedo != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", albedo);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", albedo);
            }

            _mats.Add(mat);
            return mat;
        }

        private static string UnitDef(string role, FactionRoster faction)
        {
            switch ((role ?? "basic").ToLowerInvariant())
            {
                case "builder": return faction.BuilderUnitId;
                case "ranged": return faction.RangedUnitId;
                case "cavalry": return faction.CavalryUnitId;
                case "siege": return faction.SiegeUnitId;
                case "leader": return faction.LeaderUnitId;
                case "pathfinder":
                case "scout": return faction.ScoutUnitId;
                default: return faction.BasicUnitId;
            }
        }

        private static string BuildingDef(string role, FactionRoster faction)
        {
            switch ((role ?? "tower").ToLowerInvariant())
            {
                case "wall": return faction.WallBuildingId;
                case "producer": return faction.ProducerBuildingId;
                case "outpost": return faction.OutpostBuildingId;
                case "keep": return faction.KeepBuildingId;
                default: return faction.TowerBuildingId;
            }
        }

        private void BindSceneViews()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                return;
            foreach (SceneView view in SceneView.sceneViews)
                ApplyView(view);
        }

        private void UnbindSceneViews()
        {
            foreach (SceneView view in SceneView.sceneViews)
            {
                if (view != null && GetCustomScene(view) == _scene)
                    SetCustomScene(view, default);
            }
        }

        private void ApplyView(SceneView view)
        {
            if (view == null || !_scene.IsValid())
                return;
            SetCustomScene(view, _scene);
            view.sceneLighting = true;
            view.drawGizmos = true;
            if (view.camera != null)
            {
                view.camera.clearFlags = CameraClearFlags.SolidColor;
                view.camera.backgroundColor = new Color(0.38f, 0.55f, 0.72f);
            }
        }

        public bool IsShowingIn(SceneView view)
        {
            return view != null && _scene.IsValid() && GetCustomScene(view) == _scene;
        }

        public void EnsureSceneViewBound()
        {
            if (!IsActive)
                return;
            var view = SceneView.lastActiveSceneView;
            if (view != null && GetCustomScene(view) != _scene)
                ApplyView(view);
        }

        private static Scene GetCustomScene(SceneView view)
        {
            if (view == null || SceneViewCustomScene == null)
                return default;
            object value = SceneViewCustomScene.GetValue(view);
            return value is Scene scene ? scene : default;
        }

        private static void SetCustomScene(SceneView view, Scene scene)
        {
            if (view == null || SceneViewCustomScene == null)
                return;
            SceneViewCustomScene.SetValue(view, scene);
            if (view.camera != null)
                view.camera.scene = scene;
        }
    }
}
