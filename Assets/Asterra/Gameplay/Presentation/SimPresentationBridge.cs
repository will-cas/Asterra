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
        [SerializeField] private float yPosition;
        [SerializeField] private bool createGround = true;
        [SerializeField] private float groundSize = 1200f;

        private readonly Dictionary<uint, EntityView> _unitViews = new();
        private readonly Dictionary<uint, EntityView> _buildingViews = new();
        private System.Func<IReadOnlyList<SimEntityId>> _getSelected;

        public void BindSelection(System.Func<IReadOnlyList<SimEntityId>> getSelected)
        {
            _getSelected = getSelected;
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

            if (createGround)
                EnsureGround();
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null)
                return;

            SyncUnits(match.World.Units);
            SyncBuildings(match.World.Buildings);
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
                        snap.Owner.Value);
                    _buildingViews[snap.Id.Value] = view;
                }

                view.transform.position = new Vector3(snap.X, yPosition, snap.Z);
            }

            RemoveMissing(_buildingViews, alive);
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

        private void EnsureGround()
        {
            if (GameObject.Find("AsterraGround") != null)
                return;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "AsterraGround";
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(groundSize / 10f, 1f, groundSize / 10f);
            ground.transform.position = Vector3.zero;
            var rend = ground.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                var color = new Color(0.28f, 0.36f, 0.22f);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
                rend.sharedMaterial = mat;
            }
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
    }
}
