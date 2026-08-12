using System.Collections.Generic;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using UnityEngine;

namespace Asterra.Gameplay.Player
{
    /// <summary>
    /// Keyboard orders plus click-to-select / right-click move or attack.
    /// </summary>
    public sealed class LocalOrderController : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Camera rigCamera;
        [SerializeField] private LayerMask clickMask = ~0;
        [SerializeField] private float buildingSelectRadius = 90f;
        [SerializeField] private float screenPickPixels = 48f;

        private SelectionState _selection;
        private ICommandBus _commands;
        private IWorldQuery _world;
        private PlayerId _local;
        private FactionRoster _roster;
        private readonly RaycastHit[] _rayHits = new RaycastHit[32];

        public SelectionState Selection => _selection;

        public void Bind(MatchBootstrap bootstrap)
        {
            match = bootstrap;
            _selection = new SelectionState();
            _commands = bootstrap.Commands;
            _world = bootstrap.World;
            _local = bootstrap.Session.LocalPlayer;
            _roster = bootstrap.PlayerRoster ?? FactionDefaultContent.IronCovenant;
            if (rigCamera == null)
                rigCamera = Camera.main;
            AutoSelectOwnedUnits();
            BindPresentationSelection();
        }

        public void BindPresentationSelection()
        {
            if (_selection == null)
                return;
            var bridge = FindFirstObjectByType<SimPresentationBridge>();
            if (bridge != null)
                bridge.BindSelection(() => _selection.Selected);
        }

        private void Update()
        {
            if (match == null || _commands == null || match.Result.IsOver)
                return;

            HandlePointer();
            HandleHotkeys();
        }

        private void HandlePointer()
        {
            if (rigCamera == null)
                rigCamera = Camera.main;
            if (rigCamera == null)
                return;

            // Left click: select
            if (UnityEngine.Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            {
                if (TryPickEntity(out var view))
                {
                    if (view.IsUnit && view.Owner == _local)
                    {
                        if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift))
                            _selection.Toggle(view.Id);
                        else
                            _selection.Set(new[] { view.Id });
                    }
                    else if (!view.IsUnit && view.Owner == _local)
                    {
                        // Select all units near own building as a convenience.
                        SelectOwnedNear(view.transform.position.x, view.transform.position.z, buildingSelectRadius);
                    }
                }
                else if (TryRaycastGround(out _, out _))
                {
                    if (!UnityEngine.Input.GetKey(KeyCode.LeftShift) && !UnityEngine.Input.GetKey(KeyCode.RightShift))
                        _selection.Clear();
                }
            }

            // Right click: move or attack
            if (UnityEngine.Input.GetMouseButtonDown(1) && !IsPointerOverUi())
            {
                var unitIds = GetOrderUnitIds();
                if (unitIds.Length == 0)
                    return;

                if (TryPickEntity(out var targetView))
                {
                    if (targetView.Owner != _local && targetView.IsRevealed)
                    {
                        _commands.SubmitLocal(new AttackCommand
                        {
                            Issuer = _local,
                            UnitIds = unitIds,
                            TargetId = targetView.Id,
                        });
                        return;
                    }
                }

                if (TryRaycastGround(out float x, out float z))
                {
                    _commands.SubmitLocal(new MoveCommand
                    {
                        Issuer = _local,
                        UnitIds = unitIds,
                        TargetX = x,
                        TargetZ = z,
                    });
                }
            }
        }

        private void HandleHotkeys()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.B))
            {
                if (!TryRaycastGround(out float x, out float z))
                {
                    x = -300f;
                    z = 30f;
                }

                _commands.SubmitLocal(new PlaceBuildingCommand
                {
                    Issuer = _local,
                    BuildingDefId = _roster.ProducerBuildingId,
                    X = x,
                    Z = z,
                    YawDegrees = 0f,
                });
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                if (TryFindOwnedProducer(out var buildingId))
                {
                    _commands.SubmitLocal(new TrainUnitCommand
                    {
                        Issuer = _local,
                        BuildingId = buildingId,
                        UnitDefId = _roster.BasicUnitId,
                    });
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.C) && _world.Territories.Count > 0)
            {
                _commands.SubmitLocal(new CaptureTerritoryCommand
                {
                    Issuer = _local,
                    TerritoryNodeId = _world.Territories[0].Id,
                });
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.U))
            {
                _commands.SubmitLocal(new ChooseUpgradeCommand
                {
                    Issuer = _local,
                    UpgradeDefId = _roster.BasicUpgradeId,
                });
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                if (TryFindHostile(out var target))
                {
                    _commands.SubmitLocal(new AttackCommand
                    {
                        Issuer = _local,
                        UnitIds = GetOrderUnitIds(),
                        TargetId = target,
                    });
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                AutoSelectOwnedUnits();
        }

        private bool TryPickEntity(out EntityView view)
        {
            view = null;
            if (rigCamera == null)
                return false;

            var ray = rigCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            int hitCount = Physics.RaycastNonAlloc(ray, _rayHits, 5000f, clickMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                var candidate = _rayHits[i].collider.GetComponentInParent<EntityView>();
                if (candidate == null || !candidate.IsRevealed)
                    continue;
                if (_rayHits[i].distance < bestDist)
                {
                    bestDist = _rayHits[i].distance;
                    view = candidate;
                }
            }

            if (view != null)
                return true;

            return TryScreenPickEntity(out view);
        }

        private bool TryScreenPickEntity(out EntityView view)
        {
            view = null;
            Vector3 mouse = UnityEngine.Input.mousePosition;
            float maxPx2 = screenPickPixels * screenPickPixels;
            float bestPx2 = maxPx2;
            var views = FindObjectsByType<EntityView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var candidate = views[i];
                if (candidate == null || !candidate.IsRevealed)
                    continue;

                Vector3 world = candidate.transform.position + Vector3.up * (candidate.IsUnit ? 4f : 8f);
                Vector3 screen = rigCamera.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                    continue;

                float dx = screen.x - mouse.x;
                float dy = screen.y - mouse.y;
                float d2 = dx * dx + dy * dy;
                if (d2 < bestPx2)
                {
                    bestPx2 = d2;
                    view = candidate;
                }
            }

            return view != null;
        }

        private bool TryRaycastGround(out float x, out float z)
        {
            x = 0f;
            z = 0f;
            var ray = rigCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                var point = ray.GetPoint(enter);
                x = point.x;
                z = point.z;
                return true;
            }

            return false;
        }

        private void SelectOwnedNear(float x, float z, float radius)
        {
            var ids = new List<SimEntityId>();
            float r2 = radius * radius;
            for (int i = 0; i < _world.Units.Count; i++)
            {
                var u = _world.Units[i];
                if (u.Owner != _local || !u.IsAlive)
                    continue;
                float dx = u.X - x;
                float dz = u.Z - z;
                if (dx * dx + dz * dz <= r2)
                    ids.Add(u.Id);
            }

            _selection.Set(ids);
        }

        private static bool IsPointerOverUi()
        {
            return false;
        }

        private SimEntityId[] GetOrderUnitIds()
        {
            if (_selection.Selected.Count > 0)
            {
                var arr = new SimEntityId[_selection.Selected.Count];
                for (int i = 0; i < _selection.Selected.Count; i++)
                    arr[i] = _selection.Selected[i];
                return arr;
            }

            AutoSelectOwnedUnits();
            var fallback = new SimEntityId[_selection.Selected.Count];
            for (int i = 0; i < _selection.Selected.Count; i++)
                fallback[i] = _selection.Selected[i];
            return fallback;
        }

        private void AutoSelectOwnedUnits()
        {
            var ids = new List<SimEntityId>();
            for (int i = 0; i < _world.Units.Count; i++)
            {
                var u = _world.Units[i];
                if (u.Owner == _local && u.IsAlive)
                    ids.Add(u.Id);
            }

            _selection.Set(ids);
        }

        private bool TryFindOwnedProducer(out SimEntityId buildingId)
        {
            for (int i = 0; i < _world.Buildings.Count; i++)
            {
                var b = _world.Buildings[i];
                if (b.Owner == _local && b.CanProduce)
                {
                    buildingId = b.Id;
                    return true;
                }
            }

            buildingId = default;
            return false;
        }

        private bool TryFindHostile(out SimEntityId targetId)
        {
            var fog = FindFirstObjectByType<FogOfWarPresenter>();
            for (int i = 0; i < _world.Units.Count; i++)
            {
                var u = _world.Units[i];
                if (!u.IsAlive || u.Owner == _local)
                    continue;
                if (fog != null && !fog.IsWorldVisible(u.X, u.Z))
                    continue;
                targetId = u.Id;
                return true;
            }

            for (int i = 0; i < _world.Buildings.Count; i++)
            {
                var b = _world.Buildings[i];
                if (b.Owner == _local || b.State == BuildingState.Destroyed)
                    continue;
                if (fog != null && !fog.IsWorldVisible(b.X, b.Z))
                    continue;
                targetId = b.Id;
                return true;
            }

            targetId = default;
            return false;
        }
    }
}
