using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Client-only fog of war: vision circles from owned units/buildings,
    /// hide enemies outside sight, and darken unexplored map cells.
    /// </summary>
    public sealed class FogOfWarPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private float unitSightRadius = 110f;
        [SerializeField] private float keepSightRadius = 160f;
        [SerializeField] private float buildingSightRadius = 85f;
        [SerializeField] private float mapHalfExtent = MapBounds.PlayableHalfExtent;
        [SerializeField] private float cellSize = 30f;
        [SerializeField] private float fogHeight = 0.4f;

        private readonly List<Vector2> _visionCenters = new();
        private readonly List<float> _visionRadii = new();
        private readonly List<FogCell> _cells = new();
        private Transform _fogRoot;
        private Material _unexploredMat;
        private Material _exploredMat;
        private bool _built;

        private struct FogCell
        {
            public float X;
            public float Z;
            public Renderer Renderer;
            public bool Explored;
        }

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null || match.Session == null)
                return;

            if (!_built)
                BuildFogGrid();

            CollectVision(match.Session.LocalPlayer);
            UpdateFogCells();
            ApplyEntityVisibility(match.Session.LocalPlayer);
        }

        private float VisionScale()
        {
            // SkirmishWorldSim is in Asterra.Gameplay (parent namespace), not Asterra.Gameplay.Sim.
            var sim = match.World as SkirmishWorldSim;
            if (sim == null)
                return 1f;
            return Mathf.Clamp(sim.Environment.CombinedVisibility(), 0.45f, 1.15f);
        }

        public bool IsWorldVisible(float x, float z)
        {
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
                if (!u.IsAlive || u.Owner != local)
                    continue;
                _visionCenters.Add(new Vector2(u.X, u.Z));
                _visionRadii.Add(unitSightRadius * VisionScale());
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

        private void UpdateFogCells()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                bool visible = IsWorldVisible(cell.X, cell.Z);
                if (visible)
                    cell.Explored = true;

                if (visible)
                {
                    cell.Renderer.enabled = false;
                }
                else if (cell.Explored)
                {
                    cell.Renderer.enabled = true;
                    cell.Renderer.sharedMaterial = _exploredMat;
                }
                else
                {
                    cell.Renderer.enabled = true;
                    cell.Renderer.sharedMaterial = _unexploredMat;
                }

                _cells[i] = cell;
            }
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

        private void BuildFogGrid()
        {
            _built = true;
            EnsureMaterials();

            var rootGo = new GameObject("FogOfWar");
            rootGo.transform.SetParent(transform, false);
            _fogRoot = rootGo.transform;

            int cellsPerSide = Mathf.CeilToInt((mapHalfExtent * 2f) / cellSize);
            float origin = -mapHalfExtent + cellSize * 0.5f;
            for (int iz = 0; iz < cellsPerSide; iz++)
            {
                for (int ix = 0; ix < cellsPerSide; ix++)
                {
                    float x = origin + ix * cellSize;
                    float z = origin + iz * cellSize;
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.Destroy(quad.GetComponent<Collider>());
                    quad.name = $"Fog_{ix}_{iz}";
                    quad.transform.SetParent(_fogRoot, false);
                    quad.transform.position = new Vector3(x, fogHeight, z);
                    quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    quad.transform.localScale = new Vector3(cellSize * 0.98f, cellSize * 0.98f, 1f);
                    var rend = quad.GetComponent<Renderer>();
                    rend.sharedMaterial = _unexploredMat;
                    _cells.Add(new FogCell
                    {
                        X = x,
                        Z = z,
                        Renderer = rend,
                        Explored = false,
                    });
                }
            }
        }

        private void EnsureMaterials()
        {
            if (_unexploredMat != null)
                return;

            var shader = Shader.Find("Asterra/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");

            _unexploredMat = new Material(shader);
            SetMatColor(_unexploredMat, new Color(0.02f, 0.03f, 0.05f, 0.92f));

            _exploredMat = new Material(shader);
            SetMatColor(_exploredMat, new Color(0.05f, 0.06f, 0.08f, 0.55f));
        }

        private static void SetMatColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }
    }
}
