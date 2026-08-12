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
        [SerializeField] private float doubleClickSelectRadius = 8f;

        private SelectionState _selection;
        private ICommandBus _commands;
        private IWorldQuery _world;
        private PlayerId _local;
        private FactionRoster _roster;

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
                if (TryRaycastEntity(out var view))
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
                        SelectOwnedNear(view.transform.position.x, view.transform.position.z, doubleClickSelectRadius);
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

                if (TryRaycastEntity(out var targetView))
                {
                    if (targetView.Owner != _local)
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

        private bool TryRaycastEntity(out EntityView view)
        {
            view = null;
            var ray = rigCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 5000f, clickMask))
                return false;
            view = hit.collider.GetComponentInParent<EntityView>();
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
            var ids = new System.Collections.Generic.List<SimEntityId>();
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
            var ids = new System.Collections.Generic.List<SimEntityId>();
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
            for (int i = 0; i < _world.Units.Count; i++)
            {
                var u = _world.Units[i];
                if (u.IsAlive && u.Owner != _local)
                {
                    targetId = u.Id;
                    return true;
                }
            }

            for (int i = 0; i < _world.Buildings.Count; i++)
            {
                var b = _world.Buildings[i];
                if (b.Owner != _local && b.State != BuildingState.Destroyed)
                {
                    targetId = b.Id;
                    return true;
                }
            }

            targetId = default;
            return false;
        }
    }
}
