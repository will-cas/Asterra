using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Mirrors sim snapshots onto scene transforms. Safe no-op until prefabs are assigned in Editor.
    /// </summary>
    public sealed class SimPresentationBridge : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Transform unitRoot;
        [SerializeField] private Transform buildingRoot;
        [SerializeField] private GameObject fallbackUnitPrefab;
        [SerializeField] private GameObject fallbackBuildingPrefab;
        [SerializeField] private float yPosition;

        private readonly Dictionary<uint, Transform> _unitViews = new();
        private readonly Dictionary<uint, Transform> _buildingViews = new();

        private void Awake()
        {
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
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null)
                return;

            SyncUnits(match.World.Units);
            SyncBuildings(match.World.Buildings);
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
                    view = SpawnView(fallbackUnitPrefab, unitRoot, $"Unit_{snap.Id.Value}");
                    _unitViews[snap.Id.Value] = view;
                }

                view.position = new Vector3(snap.X, yPosition, snap.Z);
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
                    view = SpawnView(fallbackBuildingPrefab, buildingRoot, $"Building_{snap.Id.Value}");
                    _buildingViews[snap.Id.Value] = view;
                }

                view.position = new Vector3(snap.X, yPosition, snap.Z);
            }

            RemoveMissing(_buildingViews, alive);
        }

        private static Transform SpawnView(GameObject prefab, Transform parent, string name)
        {
            GameObject go;
            if (prefab != null)
                go = Instantiate(prefab, parent);
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(parent, false);
                go.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }

            go.name = name;
            return go.transform;
        }

        private static void RemoveMissing(Dictionary<uint, Transform> views, HashSet<uint> alive)
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
