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
                if (!snap.IsAlive)
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

                if (view.IsDying)
                    continue;

                view.SetGarrisoned(snap.IsGarrisoned);
                if (snap.IsGarrisoned)
                {
                    view.SetHealth(snap.Health, snap.MaxHealth);
                    continue;
                }

                float y = SampleY(snap.X, snap.Z);
                float dx = 0f;
                float dz = 1f;
                if (snap.HasMoveTarget || snap.AttackMoving || snap.Patrolling)
                {
                    dx = snap.MoveTargetX - snap.X;
                    dz = snap.MoveTargetZ - snap.Z;
                    float mag = Mathf.Sqrt(dx * dx + dz * dz);
                    if (mag > 0.01f)
                    {
                        dx /= mag;
                        dz /= mag;
                    }
                }

                float slope = Mathf.Atan2(
                    SampleY(snap.X + dx * 2f, snap.Z + dz * 2f) - SampleY(snap.X - dx * 2f, snap.Z - dz * 2f),
                    4f) * Mathf.Rad2Deg;

                view.SyncPresentation(
                    new Vector3(snap.X, y, snap.Z),
                    locomoting: snap.HasMoveTarget || snap.AttackMoving || snap.Patrolling,
                    attacking: snap.HasAttackTarget,
                    idle: snap.IsIdle,
                    hasCarry: snap.HasCarry,
                    running: snap.AttackMoving || snap.Patrolling,
                    gathering: snap.HasGatherTarget && !snap.HasAttackTarget,
                    stunned: snap.Stunned,
                    airborne: snap.Airborne,
                    boat: snap.WaterCraft,
                    wade: SampleWade(snap.X, snap.Z),
                    carryTimber: snap.HasCarry && snap.CarryType == ResourceType.Timber,
                    stance: snap.Stance,
                    slopeDegrees: slope,
                    facingYaw: Mathf.Atan2(dx, dz) * Mathf.Rad2Deg,
                    hasFacing: snap.HasMoveTarget || snap.AttackMoving || snap.Patrolling);
                view.SetHealth(snap.Health, snap.MaxHealth);
                view.SetEquipmentVisuals(snap.EquipmentVisualFlags);
            }

            RemoveMissingUnits(alive);
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
                view.SetBuildingActivity(
                    snap.QueueCount > 0 || snap.ProductionProgress > 0.01f,
                    snap.ProductionProgress,
                    researching: !string.IsNullOrEmpty(snap.ResearchUpgradeDefId) || snap.ResearchProgress > 0.01f,
                    research01: snap.ResearchProgress,
                    kind: snap.Kind,
                    disabled: snap.State == BuildingState.Disabled);
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

                view.SetWorldPose(new Vector3(snap.X, SampleY(snap.X, snap.Z) + 2f, snap.Z));
                view.SetRemaining(snap.Remaining);
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
            bool gold = snap.Type == ResourceType.Gold;
            rend.sharedMaterial = AsterraPbrLibrary.CreateLit(
                AsterraMeshLibrary.ResourceColor(snap.Type),
                gold ? "crystal" : "bark",
                gold ? 0.55f : 0.04f);

            // Gold nugget vs timber log: different world scales for silhouette.
            go.transform.localScale = gold
                ? new Vector3(7.5f, 7.5f, 7.5f)
                : new Vector3(6.5f, 6.5f, 6.5f);

            var sphere = go.AddComponent<SphereCollider>();
            sphere.center = new Vector3(0f, 0.6f, 0f);
            sphere.radius = gold ? 1.2f : 1.4f;

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

                if (view.IsFalling)
                    continue;
                view.SetWorldPose(
                    new Vector3(snap.X, SampleY(snap.X, snap.Z), snap.Z),
                    snap.YawDegrees);
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
                if (!_destructibleViews.TryGetValue(id, out var view) || view == null)
                {
                    _destructibleViews.Remove(id);
                    continue;
                }

                if (!view.IsFalling)
                    view.BeginFall();
                if (view.FallFinished)
                {
                    Destroy(view.gameObject);
                    _destructibleViews.Remove(id);
                }
            }
        }

        private DestructibleView SpawnDestructible(DestructibleSnapshot snap)
        {
            var go = new GameObject($"Destructible_{snap.Id.Value}");
            go.transform.SetParent(destructibleRoot, false);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = AsterraMeshLibrary.GetDestructibleMesh(snap.DefinitionId);
            var color = AsterraMeshLibrary.DestructibleColor(snap.DefinitionId);
            string texKey = AsterraMeshLibrary.DestructibleTexKey(snap.DefinitionId);
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = AsterraPbrLibrary.CreateLit(
                color,
                AsterraPbrLibrary.PropSetKey(texKey),
                0.04f);

            bool scenery = DefaultDestructibleCatalog.IsScenery(snap.DefinitionId);
            float scale = scenery ? 2.15f : snap.DefinitionId != null && snap.DefinitionId.Contains("bridge") ? 1.15f : 1.35f;
            go.transform.localScale = new Vector3(scale, scale, scale);
            go.transform.rotation = Quaternion.Euler(0f, snap.YawDegrees, 0f);

            if (!scenery)
            {
                var sphere = go.AddComponent<SphereCollider>();
                sphere.center = new Vector3(0f, 1.2f, 0f);
                sphere.radius = snap.FootprintRadius > 0.5f ? snap.FootprintRadius * 0.35f : 1.4f;
            }

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

        private EntityView SpawnEntity(
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
            Color teamColor = match != null
                ? match.TeamTintFor(owner)
                : AsterraMeshLibrary.FactionColor(factionIndex);
            view.Initialize(id, isUnit, owner, definitionId, factionIndex, squadSize, teamColor);
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
                    view.BeginCollapse();

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

        private void RemoveMissingUnits(HashSet<uint> alive)
        {
            var stale = new List<uint>();
            foreach (var pair in _unitViews)
            {
                if (!alive.Contains(pair.Key))
                    stale.Add(pair.Key);
            }

            for (int i = 0; i < stale.Count; i++)
            {
                var id = stale[i];
                if (!_unitViews.TryGetValue(id, out var view) || view == null)
                {
                    _unitViews.Remove(id);
                    continue;
                }

                if (!view.IsDying)
                    view.BeginDeath();
                if (view.DeathFinished)
                {
                    Destroy(view.gameObject);
                    _unitViews.Remove(id);
                }
            }
        }

        private float SampleWade(float x, float z)
        {
            if (_terrain == null)
                _terrain = FindFirstObjectByType<TerrainGridPresenter>();
            return _terrain != null ? _terrain.SampleWade(x, z) : 0f;
        }

        private static Material CreateColorMaterial(Color color)
        {
            return CreateColorMaterial(color, null, 0.18f);
        }

        private static Material CreateColorMaterial(Color color, Texture2D albedo, float uvScale)
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
            if (albedo != null)
            {
                mat.SetTexture("_MainTex", albedo);
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", albedo);
            }

            mat.SetFloat("_UvScale", uvScale);
            mat.SetFloat("_TexBlend", albedo != null ? 0.72f : 0f);
            return mat;
        }
    }
}
