using System.Collections.Generic;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using UnityEngine;

namespace Asterra.Gameplay.Player
{
    /// <summary>
    /// Keyboard orders plus click / drag-box select and right-click move or attack.
    /// Mac: ⌘/Control additive select; Control-click or two-finger click = order.
    /// </summary>
    public sealed class LocalOrderController : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Camera rigCamera;
        [SerializeField] private LayerMask clickMask = ~0;
        [SerializeField] private float buildingSelectRadius = 90f;
        [SerializeField] private float screenPickPixels = 48f;
        [SerializeField] private float dragThresholdPixels = 8f;

        private SelectionState _selection;
        private ICommandBus _commands;
        private IWorldQuery _world;
        private PlayerId _local;
        private FactionRoster _roster;
        private readonly RaycastHit[] _rayHits = new RaycastHit[32];

        private bool _pointerDown;
        private bool _isDragging;
        private Vector3 _dragStartScreen;
        private Vector3 _dragCurrentScreen;

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

        private void OnGUI()
        {
            if (!_isDragging)
                return;

            Rect rect = ScreenRectFromPoints(_dragStartScreen, _dragCurrentScreen);
            // Unity GUI y is top-down; Input mouse y is bottom-up.
            Rect guiRect = new Rect(rect.xMin, Screen.height - rect.yMax, rect.width, rect.height);
            Color fill = new Color(0.2f, 0.75f, 0.35f, 0.18f);
            Color edge = new Color(0.35f, 0.95f, 0.45f, 0.9f);
            DrawScreenRect(guiRect, fill);
            DrawScreenRectBorder(guiRect, 2f, edge);
        }

        private void HandlePointer()
        {
            if (rigCamera == null)
                rigCamera = Camera.main;
            if (rigCamera == null)
                return;

            if (UnityEngine.Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            {
                _pointerDown = true;
                _isDragging = false;
                _dragStartScreen = UnityEngine.Input.mousePosition;
                _dragCurrentScreen = _dragStartScreen;
            }

            if (_pointerDown && UnityEngine.Input.GetMouseButton(0))
            {
                _dragCurrentScreen = UnityEngine.Input.mousePosition;
                if (!_isDragging)
                {
                    float dx = _dragCurrentScreen.x - _dragStartScreen.x;
                    float dy = _dragCurrentScreen.y - _dragStartScreen.y;
                    if (dx * dx + dy * dy >= dragThresholdPixels * dragThresholdPixels)
                        _isDragging = true;
                }
            }

            if (_pointerDown && UnityEngine.Input.GetMouseButtonUp(0))
            {
                bool additive = IsAdditiveModifierHeld();
                if (_isDragging)
                {
                    SelectOwnedInScreenRect(
                        ScreenRectFromPoints(_dragStartScreen, _dragCurrentScreen),
                        additive);
                }
                else
                {
                    HandleClickSelect(additive);
                }

                _pointerDown = false;
                _isDragging = false;
            }

            // Right click / Mac Control-click: move or attack
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
                    x = Mathf.Clamp(x, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                    z = Mathf.Clamp(z, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
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

        private void HandleClickSelect(bool additive)
        {
            if (TryPickEntity(out var view))
            {
                if (view.IsUnit && view.Owner == _local)
                {
                    if (additive)
                        _selection.Toggle(view.Id);
                    else
                        _selection.Set(new[] { view.Id });
                    return;
                }

                if (!view.IsUnit && view.Owner == _local)
                {
                    SelectOwnedNear(view.transform.position.x, view.transform.position.z, buildingSelectRadius);
                    return;
                }
            }

            if (!additive && TryRaycastGround(out _, out _))
                _selection.Clear();
        }

        private void SelectOwnedInScreenRect(Rect screenRect, bool additive)
        {
            var ids = additive ? new List<SimEntityId>(_selection.Selected) : new List<SimEntityId>();
            var views = FindObjectsByType<EntityView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null || !view.IsUnit || view.Owner != _local || !view.IsRevealed)
                    continue;

                Vector3 screen = rigCamera.WorldToScreenPoint(view.transform.position + Vector3.up * 4f);
                if (screen.z <= 0f)
                    continue;
                if (!screenRect.Contains(new Vector2(screen.x, screen.y)))
                    continue;

                if (!ids.Contains(view.Id))
                    ids.Add(view.Id);
            }

            _selection.Set(ids);
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

                x = Mathf.Clamp(x, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                z = Mathf.Clamp(z, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);

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

        private static bool IsAdditiveModifierHeld()
        {
            // Prefer Shift / ⌘ on Mac — Control-click is reserved for right-click orders.
            return UnityEngine.Input.GetKey(KeyCode.LeftShift)
                   || UnityEngine.Input.GetKey(KeyCode.RightShift)
                   || UnityEngine.Input.GetKey(KeyCode.LeftCommand)
                   || UnityEngine.Input.GetKey(KeyCode.RightCommand);
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

        private static Rect ScreenRectFromPoints(Vector3 a, Vector3 b)
        {
            float xMin = Mathf.Min(a.x, b.x);
            float xMax = Mathf.Max(a.x, b.x);
            float yMin = Mathf.Min(a.y, b.y);
            float yMax = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static void DrawScreenRect(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
        {
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
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
