using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Soft circular fog of war (no grid cells). Explored memory is a continuous texture;
    /// live vision is evaluated smoothly in the fog shader.
    /// </summary>
    public sealed class FogOfWarPresenter : MonoBehaviour
    {
        private const int MaxVision = 64;
        private const int ExploredResolution = 192;

        [SerializeField] private MatchBootstrap match;
        [SerializeField] private float unitSightRadius = 110f;
        [SerializeField] private float keepSightRadius = 160f;
        [SerializeField] private float buildingSightRadius = 85f;
        [SerializeField] private float mapHalfExtent = MapBounds.PlayableHalfExtent;
        [SerializeField] private float fogHeight = 40f;

        private readonly List<Vector2> _visionCenters = new();
        private readonly List<float> _visionRadii = new();
        private readonly Vector4[] _visionData = new Vector4[MaxVision];
        private readonly Color32[] _exploredPixels = new Color32[ExploredResolution * ExploredResolution];

        private Texture2D _exploredTex;
        private Material _fogMat;
        private Transform _fogPlane;
        private bool _built;
        private float _mapSize;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
        }

        private void OnDestroy()
        {
            if (_exploredTex != null)
                Destroy(_exploredTex);
            if (_fogMat != null)
                Destroy(_fogMat);
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null || match.Session == null)
                return;

            if (!_built)
                BuildFogPlane();

            CollectVision(match.Session.LocalPlayer);
            StampExplored();
            PushVisionToMaterial();
            ApplyEntityVisibility(match.Session.LocalPlayer);
        }

        private float VisionScale()
        {
            var sim = match.World as global::Asterra.Gameplay.SkirmishWorldSim;
            if (sim == null)
                return 1f;
            return Mathf.Clamp(sim.Environment.CombinedVisibility(), 0.45f, 1.15f);
        }

        public bool IsWorldVisible(float x, float z)
        {
            var sim = match != null ? match.World as global::Asterra.Gameplay.SkirmishWorldSim : null;
            if (sim != null && match.Session != null)
                return sim.IsVisibleTo(match.Session.LocalPlayer, x, z);

            for (int i = 0; i < _visionCenters.Count; i++)
            {
                float dx = x - _visionCenters[i].x;
                float dz = z - _visionCenters[i].y;
                float r = _visionRadii[i];
                if (dx * dx + dz * dz <= r * r)
                    return true;
            }

            return false;
        }

        private void CollectVision(PlayerId local)
        {
            _visionCenters.Clear();
            _visionRadii.Clear();

            var units = match.World.Units;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (!u.IsAlive || u.Owner != local || u.IsGarrisoned)
                    continue;
                float sight = u.SightRadius > 1f ? u.SightRadius : unitSightRadius;
                _visionCenters.Add(new Vector2(u.X, u.Z));
                _visionRadii.Add(sight * VisionScale());
            }

            var buildings = match.World.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                if (b.Owner != local || b.State == BuildingState.Destroyed)
                    continue;
                float radius = buildingSightRadius;
                if (b.SightRadius > 1f)
                    radius = b.SightRadius;
                else if (AsterraMeshLibrary.IsKeep(b.DefinitionId))
                    radius = keepSightRadius;
                _visionCenters.Add(new Vector2(b.X, b.Z));
                _visionRadii.Add(radius * VisionScale());
            }
        }

        private void StampExplored()
        {
            if (_exploredTex == null)
                return;

            float origin = -mapHalfExtent;
            bool dirty = false;
            for (int i = 0; i < _visionCenters.Count; i++)
            {
                float cx = _visionCenters[i].x;
                float cz = _visionCenters[i].y;
                float radius = _visionRadii[i] * 0.92f;
                int minX = Mathf.Clamp(Mathf.FloorToInt(((cx - radius) - origin) / _mapSize * ExploredResolution), 0, ExploredResolution - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(((cx + radius) - origin) / _mapSize * ExploredResolution), 0, ExploredResolution - 1);
                int minZ = Mathf.Clamp(Mathf.FloorToInt(((cz - radius) - origin) / _mapSize * ExploredResolution), 0, ExploredResolution - 1);
                int maxZ = Mathf.Clamp(Mathf.CeilToInt(((cz + radius) - origin) / _mapSize * ExploredResolution), 0, ExploredResolution - 1);

                for (int pz = minZ; pz <= maxZ; pz++)
                {
                    for (int px = minX; px <= maxX; px++)
                    {
                        float wx = origin + (px + 0.5f) / ExploredResolution * _mapSize;
                        float wz = origin + (pz + 0.5f) / ExploredResolution * _mapSize;
                        float dx = wx - cx;
                        float dz = wz - cz;
                        float dist = Mathf.Sqrt(dx * dx + dz * dz);
                        float falloff = 1f - Mathf.SmoothStep(radius * 0.55f, radius, dist);
                        if (falloff <= 0.01f)
                            continue;

                        int idx = pz * ExploredResolution + px;
                        byte next = (byte)Mathf.Min(255, _exploredPixels[idx].r + falloff * 90f);
                        if (next > _exploredPixels[idx].r)
                        {
                            _exploredPixels[idx] = new Color32(next, next, next, 255);
                            dirty = true;
                        }
                    }
                }
            }

            if (!dirty)
                return;

            _exploredTex.SetPixels32(_exploredPixels);
            _exploredTex.Apply(updateMipmaps: false);
        }

        private void PushVisionToMaterial()
        {
            if (_fogMat == null)
                return;

            int count = Mathf.Min(_visionCenters.Count, MaxVision);
            for (int i = 0; i < count; i++)
                _visionData[i] = new Vector4(_visionCenters[i].x, _visionCenters[i].y, _visionRadii[i], 0f);
            for (int i = count; i < MaxVision; i++)
                _visionData[i] = Vector4.zero;

            _fogMat.SetInt("_VisionCount", count);
            _fogMat.SetVectorArray("_VisionData", _visionData);
        }

        private void ApplyEntityVisibility(PlayerId local)
        {
            var views = FindObjectsByType<EntityView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null)
                    continue;

                if (view.Owner == local)
                {
                    view.SetRevealed(true);
                    continue;
                }

                var pos = view.transform.position;
                view.SetRevealed(IsWorldVisible(pos.x, pos.z));
            }
        }

        private void BuildFogPlane()
        {
            _built = true;
            _mapSize = mapHalfExtent * 2f;

            var stale = GameObject.Find("SoftFogOfWar");
            if (stale != null)
                Destroy(stale);

            _exploredTex = new Texture2D(ExploredResolution, ExploredResolution, TextureFormat.RGB24, mipChain: false, linear: true);
            _exploredTex.name = "FoWExplored";
            _exploredTex.wrapMode = TextureWrapMode.Clamp;
            _exploredTex.filterMode = FilterMode.Bilinear;
            for (int i = 0; i < _exploredPixels.Length; i++)
                _exploredPixels[i] = new Color32(0, 0, 0, 255);
            _exploredTex.SetPixels32(_exploredPixels);
            _exploredTex.Apply();

            var shader = Shader.Find("Asterra/SoftFogOfWar");
            if (shader == null)
            {
                Debug.LogWarning("[Asterra] SoftFogOfWar shader missing — fog disabled.");
                return;
            }

            _fogMat = new Material(shader);
            _fogMat.SetTexture("_ExploredTex", _exploredTex);
            // Slate mist — terrain/water stay visible underneath.
            _fogMat.SetColor("_FogColor", new Color(0.2f, 0.26f, 0.34f, 1f));
            _fogMat.SetFloat("_UnexploredAlpha", 0.5f);
            _fogMat.SetFloat("_ExploredAlpha", 0.2f);
            _fogMat.SetVector("_MapOrigin", new Vector4(-mapHalfExtent, -mapHalfExtent, 0f, 0f));
            _fogMat.SetFloat("_MapSize", _mapSize);
            _fogMat.SetFloat("_EdgeSoftness", 0.42f);
            _fogMat.renderQueue = 3200;

            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(plane.GetComponent<Collider>());
            plane.name = "SoftFogOfWar";
            plane.transform.SetParent(transform, false);
            plane.transform.position = new Vector3(0f, fogHeight, 0f);
            plane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            plane.transform.localScale = new Vector3(_mapSize, _mapSize, 1f);
            var rend = plane.GetComponent<Renderer>();
            rend.sharedMaterial = _fogMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            _fogPlane = plane.transform;
        }
    }
}
