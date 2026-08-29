using System.Collections.Generic;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
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
        [SerializeField] private Transform destructibleRoot;
        [SerializeField] private float yPosition;
        [SerializeField] private bool createGround = true;

        private readonly Dictionary<uint, EntityView> _unitViews = new();
        private readonly Dictionary<uint, EntityView> _buildingViews = new();
        private readonly Dictionary<uint, ResourceNodeView> _resourceViews = new();
        private readonly Dictionary<uint, DestructibleView> _destructibleViews = new();
        private System.Func<IReadOnlyList<SimEntityId>> _getSelected;
        private System.Func<SimEntityId?> _getSelectedBuilding;
        private TerrainGridPresenter _terrain;

        public void BindSelection(System.Func<IReadOnlyList<SimEntityId>> getSelected)
        {
            _getSelected = getSelected;
        }

        public void BindSelectedBuilding(System.Func<SimEntityId?> getSelectedBuilding)
        {
            _getSelectedBuilding = getSelectedBuilding;
        }

        /// <summary>Destroy all mirrored entity views (used by soft rematch / main menu).</summary>
        public void ClearAllViews()
        {
            DestroyViewMap(_unitViews);
            DestroyViewMap(_buildingViews);
            foreach (var pair in _resourceViews)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            _resourceViews.Clear();
            foreach (var pair in _destructibleViews)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            _destructibleViews.Clear();
        }

        private static void DestroyViewMap(Dictionary<uint, EntityView> views)
        {
            foreach (var pair in views)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            views.Clear();
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

            if (destructibleRoot == null)
            {
                var go = new GameObject("Destructibles");
                go.transform.SetParent(transform, false);
                destructibleRoot = go.transform;
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
            SyncDestructibles(match.World.Destructibles);
            RefreshSelectionHighlights();
        }

        private void SyncUnits(IReadOnlyList<UnitSnapshot> units)
        {
            var alive = new HashSet<uint>();
            for (int i = 0; i < units.Count; i++)
            {
                var snap = units[i];
                if (!snap.IsAlive || snap.IsGarrisoned)
                    continue;
                alive.Add(snap.Id.Value);
                if (!_unitViews.TryGetValue(snap.Id.Value, out var view))
                {
                    int squad = ResolveSquadSize(snap.DefinitionId);
                    view = SpawnEntity(
                        unitRoot,
                        $"Unit_{snap.Id.Value}",
                        snap.Id,
                        isUnit: true,
                        snap.Owner,
                        snap.DefinitionId,
                        snap.Faction.Value,
                        squad);
                    _unitViews[snap.Id.Value] = view;
                }

                view.SyncPresentation(new Vector3(snap.X, SampleY(snap.X, snap.Z), snap.Z));
                view.SetHealth(snap.Health, snap.MaxHealth);
                view.SetEquipmentVisuals(snap.EquipmentVisualFlags);
                view.SetWorking(IsBuilderWorking(snap));
            }

            RemoveMissing(_unitViews, alive);
        }

        private bool IsBuilderWorking(UnitSnapshot snap)
        {
            if (match?.World == null || match.Definitions == null)
                return false;
            if (!match.Definitions.TryGetUnit(snap.DefinitionId, out var def) || !def.IsBuilder)
                return false;
            if (snap.HasAttackTarget)
                return false;

            float workR = SkirmishWorldSim.ConstructionWorkRadius;
            float r2 = workR * workR;
            var buildings = match.World.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                if (b.Owner != snap.Owner || b.State != BuildingState.Constructing)
                    continue;
                float dx = b.X - snap.X;
                float dz = b.Z - snap.Z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
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

                if (view.IsCollapsing)
                    continue;

                view.SyncPresentation(new Vector3(snap.X, SampleY(snap.X, snap.Z), snap.Z));
                view.SetHealth(snap.Health, snap.MaxHealth);
                if (snap.Kind == BuildingKind.Wall || snap.Kind == BuildingKind.Gate)
                    view.ApplyWallLinks(snap.WallLinks, snap.YawDegrees);
                else if (Mathf.Abs(snap.YawDegrees) > 0.01f)
                    view.transform.rotation = Quaternion.Euler(0f, snap.YawDegrees, 0f);
                view.SetBuildingVisual(snap.State, snap.BuildProgress);
            }

            RemoveMissingBuildings(alive);
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

                view.transform.position = new Vector3(snap.X, SampleY(snap.X, snap.Z) + 2f, snap.Z);
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
            var go = new GameObject($"Resource_{snap.Id.Value}");
            go.transform.SetParent(resourceRoot, false);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = AsterraMeshLibrary.GetResourceMesh(snap.Type);
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = CreateColorMaterial(AsterraMeshLibrary.ResourceColor(snap.Type));

            // Gold nugget vs timber log: different world scales for silhouette.
            // Gold mesh ~2.3 tall; timber log ~0.25 tall — timber needs a larger multiplier for a readable pile.
            bool gold = snap.Type == ResourceType.Gold;
            go.transform.localScale = gold
                ? new Vector3(12f, 12f, 12f)
                : new Vector3(28f, 28f, 28f);

            var sphere = go.AddComponent<SphereCollider>();
            sphere.center = new Vector3(0f, gold ? 0.55f : 0.35f, 0f);
            sphere.radius = gold ? 1.0f : 0.9f;

            var view = go.AddComponent<ResourceNodeView>();
            view.Initialize(snap.Id, snap.Type);
            return view;
        }

        private void SyncDestructibles(IReadOnlyList<DestructibleSnapshot> destructibles)
        {
            if (destructibles == null)
                return;

            var alive = new HashSet<uint>();
            for (int i = 0; i < destructibles.Count; i++)
            {
                var snap = destructibles[i];
                if (snap.State == DestructibleState.Destroyed)
                    continue;
                alive.Add(snap.Id.Value);
                if (!_destructibleViews.TryGetValue(snap.Id.Value, out var view) || view == null)
                {
                    view = SpawnDestructible(snap);
                    _destructibleViews[snap.Id.Value] = view;
                }

                view.transform.position = new Vector3(snap.X, SampleY(snap.X, snap.Z), snap.Z);
                view.SetDamaged(snap.State == DestructibleState.Damaged);
            }

            var stale = new List<uint>();
            foreach (var pair in _destructibleViews)
            {
                if (!alive.Contains(pair.Key))
                    stale.Add(pair.Key);
            }

            for (int i = 0; i < stale.Count; i++)
            {
                var id = stale[i];
                if (_destructibleViews.TryGetValue(id, out var view) && view != null)
                    Destroy(view.gameObject);
                _destructibleViews.Remove(id);
            }
        }

        private DestructibleView SpawnDestructible(DestructibleSnapshot snap)
        {
            var go = new GameObject($"Destructible_{snap.Id.Value}");
            go.transform.SetParent(destructibleRoot, false);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = AsterraMeshLibrary.GetDestructibleMesh(snap.DefinitionId);
            var color = AsterraMeshLibrary.DestructibleColor(snap.DefinitionId);
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = CreateColorMaterial(color);

            // prop_tree ~1.5 tall, prop_rock ~0.5, prop_bridge ~1.2 — bump into RTS silhouette space.
            float scale = snap.DefinitionId != null && snap.DefinitionId.Contains("bridge") ? 8f : 12f;
            go.transform.localScale = new Vector3(scale, scale, scale);

            var sphere = go.AddComponent<SphereCollider>();
            sphere.center = new Vector3(0f, snap.DefinitionId != null && snap.DefinitionId.Contains("bridge") ? 0.55f : 0.7f, 0f);
            sphere.radius = snap.FootprintRadius > 0.5f ? snap.FootprintRadius * 0.35f : 0.55f;

            var view = go.AddComponent<DestructibleView>();
            view.Initialize(snap.Id, snap.DefinitionId, color);
            return view;
        }

        private void RefreshSelectionHighlights()
        {
            var selectedSet = new HashSet<uint>();
            if (_getSelected != null)
            {
                var selected = _getSelected();
                if (selected != null)
                {
                    for (int i = 0; i < selected.Count; i++)
                        selectedSet.Add(selected[i].Value);
                }
            }

            uint selectedBuilding = 0;
            bool hasBuilding = false;
            if (_getSelectedBuilding != null)
            {
                var bid = _getSelectedBuilding();
                if (bid.HasValue)
                {
                    selectedBuilding = bid.Value.Value;
                    hasBuilding = true;
                }
            }

            foreach (var pair in _unitViews)
            {
                // Building selection is exclusive — hide unit rings while a building is selected.
                bool unitSelected = !hasBuilding && selectedSet.Contains(pair.Key);
                pair.Value.SetSelected(unitSelected);
            }
            foreach (var pair in _buildingViews)
            {
                bool selected = (hasBuilding && pair.Key == selectedBuilding)
                                || (!hasBuilding && selectedSet.Contains(pair.Key));
                pair.Value.SetSelected(selected);
            }
        }

        private int ResolveSquadSize(string definitionId)
        {
            if (match != null && match.Definitions != null
                && match.Definitions.TryGetUnit(definitionId, out var def))
                return UnitSquadVisual.ResolveSquadSize(def);
            return UnitSquadVisual.ResolveSquadSize(definitionId);
        }

        private static EntityView SpawnEntity(
            Transform parent,
            string name,
            SimEntityId id,
            bool isUnit,
            PlayerId owner,
            string definitionId,
            byte factionIndex,
            int squadSize = 1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<EntityView>();
            view.Initialize(id, isUnit, owner, definitionId, factionIndex, squadSize);
            return view;
        }

        private void RemoveMissingBuildings(HashSet<uint> alive)
        {
            var stale = new List<uint>();
            foreach (var pair in _buildingViews)
            {
                if (!alive.Contains(pair.Key))
                    stale.Add(pair.Key);
            }

            for (int i = 0; i < stale.Count; i++)
            {
                var id = stale[i];
                if (!_buildingViews.TryGetValue(id, out var view) || view == null)
                {
                    _buildingViews.Remove(id);
                    continue;
                }

                if (!view.IsCollapsing)
                {
                    // Earthwork scaffolds vanish on complete — no death collapse.
                    if (FactionDefaultContent.IsEarthworkBuildingId(view.DefinitionId))
                    {
                        Destroy(view.gameObject);
                        _buildingViews.Remove(id);
                        continue;
                    }

                    view.BeginCollapse();
                }

                if (view.CollapseFinished)
                {
                    Destroy(view.gameObject);
                    _buildingViews.Remove(id);
                }
            }
        }

        private float SampleY(float x, float z)
        {
            if (_terrain == null)
                _terrain = FindFirstObjectByType<TerrainGridPresenter>();
            if (_terrain != null)
                return _terrain.SampleHeight(x, z);
            return yPosition;
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
