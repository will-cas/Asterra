using System.Collections.Generic;
using Asterra.Core;
using Asterra.Gameplay.Presentation;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Mirrors sim snapshots onto low-poly unit/building meshes with clickable colliders.
    /// </summary>
    public sealed class SimPresentationBridge : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Transform unitRoot;
        [SerializeField] private Transform buildingRoot;
        [SerializeField] private Transform resourceRoot;
        [SerializeField] private float yPosition;
        [SerializeField] private bool createGround = true;

        private readonly Dictionary<uint, EntityView> _unitViews = new();
        private readonly Dictionary<uint, EntityView> _buildingViews = new();
        private readonly Dictionary<uint, ResourceNodeView> _resourceViews = new();
        private System.Func<IReadOnlyList<SimEntityId>> _getSelected;

        public void BindSelection(System.Func<IReadOnlyList<SimEntityId>> getSelected)
        {
            _getSelected = getSelected;
        }

        public bool TryGetEntityView(SimEntityId id, out EntityView view)
        {
            if (_unitViews.TryGetValue(id.Value, out view) && view != null)
                return true;
            if (_buildingViews.TryGetValue(id.Value, out view) && view != null)
                return true;
            view = null;
            return false;
        }

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();

            if (unitRoot == null)
            {
                var go = new GameObject("Units");
                go.transform.SetParent(transform, false);
                unitRoot = go.transform;
            }

            if (buildingRoot == null)
            {
                var go = new GameObject("Buildings");
                go.transform.SetParent(transform, false);
                buildingRoot = go.transform;
            }

            if (resourceRoot == null)
            {
                var go = new GameObject("Resources");
                go.transform.SetParent(transform, false);
                resourceRoot = go.transform;
            }

            if (createGround)
                MapBorderVisual.Ensure(transform);
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null)
                return;

            SyncUnits(match.World.Units);
            SyncBuildings(match.World.Buildings);
            SyncResources(match.World.Resources);
            RefreshSelectionHighlights();
        }

        private void SyncUnits(IReadOnlyList<UnitSnapshot> units)
        {
            var alive = new HashSet<uint>();
            for (int i = 0; i < units.Count; i++)
            {
                var snap = units[i];
                if (!snap.IsAlive)
                    continue;
                alive.Add(snap.Id.Value);
                if (!_unitViews.TryGetValue(snap.Id.Value, out var view))
                {
                    view = SpawnEntity(
                        unitRoot,
                        $"Unit_{snap.Id.Value}",
                        snap.Id,
                        isUnit: true,
                        snap.Owner,
                        snap.DefinitionId,
                        snap.Faction.Value);
                    _unitViews[snap.Id.Value] = view;
                }

                view.transform.position = new Vector3(snap.X, yPosition, snap.Z);
                view.SetHealth(snap.Health, snap.MaxHealth);
            }

            RemoveMissing(_unitViews, alive);
        }

        private void SyncBuildings(IReadOnlyList<BuildingSnapshot> buildings)
        {
            var alive = new HashSet<uint>();
            for (int i = 0; i < buildings.Count; i++)
            {
                var snap = buildings[i];
                if (snap.State == BuildingState.Destroyed)
                    continue;
                alive.Add(snap.Id.Value);
                if (!_buildingViews.TryGetValue(snap.Id.Value, out var view))
                {
                    view = SpawnEntity(
                        buildingRoot,
                        $"Building_{snap.Id.Value}",
                        snap.Id,
                        isUnit: false,
                        snap.Owner,
                        snap.DefinitionId,
                        snap.Faction.Value);
                    _buildingViews[snap.Id.Value] = view;
                }

                view.transform.position = new Vector3(snap.X, yPosition, snap.Z);
                view.SetHealth(snap.Health, snap.MaxHealth);
            }

            RemoveMissing(_buildingViews, alive);
        }

        private void SyncResources(IReadOnlyList<ResourceSnapshot> resources)
        {
            if (resources == null)
                return;

            var alive = new HashSet<uint>();
            for (int i = 0; i < resources.Count; i++)
            {
                var snap = resources[i];
                if (snap.Remaining <= 0)
                    continue;
                alive.Add(snap.Id.Value);
                if (!_resourceViews.TryGetValue(snap.Id.Value, out var view) || view == null)
                {
                    view = SpawnResource(snap);
                    _resourceViews[snap.Id.Value] = view;
                }

                view.transform.position = new Vector3(snap.X, yPosition + 2f, snap.Z);
            }

            var stale = new List<uint>();
            foreach (var pair in _resourceViews)
            {
                if (!alive.Contains(pair.Key))
                    stale.Add(pair.Key);
            }

            for (int i = 0; i < stale.Count; i++)
            {
                var id = stale[i];
                if (_resourceViews.TryGetValue(id, out var view) && view != null)
                    Destroy(view.gameObject);
                _resourceViews.Remove(id);
            }
        }

        private ResourceNodeView SpawnResource(ResourceSnapshot snap)
        {
            bool gold = snap.Type == ResourceType.Gold;
            var go = GameObject.CreatePrimitive(gold ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            go.name = $"Resource_{snap.Id.Value}";
            go.transform.SetParent(resourceRoot, false);
            go.transform.localScale = gold ? new Vector3(8f, 8f, 8f) : new Vector3(7f, 5f, 7f);

            var color = gold
                ? new Color(0.95f, 0.82f, 0.2f)
                : new Color(0.45f, 0.28f, 0.14f);
            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = CreateColorMaterial(color);

            var view = go.AddComponent<ResourceNodeView>();
            view.Initialize(snap.Id, snap.Type);
            return view;
        }

        private void RefreshSelectionHighlights()
        {
            if (_getSelected == null)
                return;
            var selected = _getSelected();
            var selectedSet = new HashSet<uint>();
            if (selected != null)
            {
                for (int i = 0; i < selected.Count; i++)
                    selectedSet.Add(selected[i].Value);
            }

            foreach (var pair in _unitViews)
                pair.Value.SetSelected(selectedSet.Contains(pair.Key));
            foreach (var pair in _buildingViews)
                pair.Value.SetSelected(selectedSet.Contains(pair.Key));
        }

        private static EntityView SpawnEntity(
            Transform parent,
            string name,
            SimEntityId id,
            bool isUnit,
            PlayerId owner,
            string definitionId,
            byte factionIndex)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<EntityView>();
            view.Initialize(id, isUnit, owner, definitionId, factionIndex);
            return view;
        }

        private static void RemoveMissing(Dictionary<uint, EntityView> views, HashSet<uint> alive)
        {
            var stale = new List<uint>();
            foreach (var pair in views)
            {
                if (!alive.Contains(pair.Key))
                    stale.Add(pair.Key);
            }

            for (int i = 0; i < stale.Count; i++)
            {
                var id = stale[i];
                if (views.TryGetValue(id, out var view) && view != null)
                    Destroy(view.gameObject);
                views.Remove(id);
            }
        }

        private static Material CreateColorMaterial(Color color)
        {
            var shader = Shader.Find("Asterra/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            return mat;
        }
    }
}
