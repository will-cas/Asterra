using System.Collections.Generic;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using UnityEngine;

namespace Asterra.Gameplay.Player
{
    public enum OrderCursorMode : byte
    {
        Select = 0,
        Move = 1,
        Attack = 2,
        Build = 3,
        Invalid = 4,
        Train = 5,
        Gather = 6,
    }

    /// <summary>
    /// Click/drag select, keep train builders, builder place mode, gather/rally, context orders.
    /// </summary>
    public sealed class LocalOrderController : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private Camera rigCamera;
        [SerializeField] private LayerMask clickMask = ~0;
        [SerializeField] private float screenPickPixels = 48f;
        [SerializeField] private float dragThresholdPixels = 8f;
        [SerializeField] private float builderPlaceRadius = 55f;

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

        private SimEntityId? _selectedBuilding;
        private bool _placeMode;
        private bool _attackMoveArmed;
        private GameObject _ghost;
        private Renderer _ghostRenderer;

        public SelectionState Selection => _selection;
        public bool IsPlaceMode => _placeMode;
        public bool IsAttackMoveArmed => _attackMoveArmed;
        public SimEntityId? SelectedBuilding => _selectedBuilding;
        public OrderCursorMode CurrentCursorMode { get; private set; } = OrderCursorMode.Select;

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

            if (FindFirstObjectByType<RtsCursorController>() == null)
                gameObject.AddComponent<RtsCursorController>();
        }

        public void BindPresentationSelection()
        {
            if (_selection == null)
                return;
            var bridge = FindFirstObjectByType<SimPresentationBridge>();
            if (bridge != null)
                bridge.BindSelection(() => _selection.Selected);
        }

        public void EnterPlaceMode()
        {
            if (!HasSelectedBuilder())
                return;
            CancelAttackMoveArm();
            _placeMode = true;
            EnsureGhost();
        }

        public void CancelPlaceMode()
        {
            _placeMode = false;
            if (_ghost != null)
                _ghost.SetActive(false);
        }

        public void CancelAttackMoveArm()
        {
            _attackMoveArmed = false;
        }

        public void TrainFromSelectedBuilding()
        {
            if (!_selectedBuilding.HasValue || _roster == null)
                return;

            string unitId = _roster.BasicUnitId;
            for (int i = 0; i < _world.Buildings.Count; i++)
            {
                var b = _world.Buildings[i];
                if (b.Id != _selectedBuilding.Value)
                    continue;
                unitId = FactionDefaultContent.IsKeepBuildingId(b.DefinitionId)
                    ? _roster.BuilderUnitId
                    : _roster.BasicUnitId;
                break;
            }

            TrainUnit(unitId);
        }

        public void TrainUnit(string defId)
        {
            if (!_selectedBuilding.HasValue || _commands == null || string.IsNullOrEmpty(defId))
                return;

            _commands.SubmitLocal(new TrainUnitCommand
            {
                Issuer = _local,
                BuildingId = _selectedBuilding.Value,
                UnitDefId = defId,
            });
        }

        public void CancelProduction()
        {
            if (!_selectedBuilding.HasValue || _commands == null)
                return;

            _commands.SubmitLocal(new CancelProductionCommand
            {
                Issuer = _local,
                BuildingId = _selectedBuilding.Value,
            });
        }

        private void Update()
        {
            if (match == null || _commands == null || match.Result.IsOver)
                return;

            HandlePointer();
            HandleHotkeys();
            UpdateGhost();
            UpdateCursorMode();
        }

        private void OnGUI()
        {
            if (!_isDragging)
                return;

            Rect rect = ScreenRectFromPoints(_dragStartScreen, _dragCurrentScreen);
            Rect guiRect = new Rect(rect.xMin, Screen.height - rect.yMax, rect.width, rect.height);
            DrawScreenRect(guiRect, new Color(0.2f, 0.75f, 0.35f, 0.18f));
            DrawScreenRectBorder(guiRect, 2f, new Color(0.35f, 0.95f, 0.45f, 0.9f));
        }

        private void OnDestroy()
        {
            if (_ghost != null)
                Destroy(_ghost);
        }

        private void HandlePointer()
        {
            if (rigCamera == null)
                rigCamera = Camera.main;
            if (rigCamera == null)
                return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (_attackMoveArmed)
                    CancelAttackMoveArm();
                CancelPlaceMode();
            }

            if (_attackMoveArmed)
            {
                HandleAttackMoveArmedPointer();
                return;
            }

            if (_placeMode)
            {
                if (UnityEngine.Input.GetMouseButtonDown(1))
                {
                    CancelPlaceMode();
                    return;
                }

                if (UnityEngine.Input.GetMouseButtonDown(0) && !IsPointerOverUi())
                {
                    if (TryRaycastGround(out float x, out float z) && CanPlaceAt(x, z))
                    {
                        _commands.SubmitLocal(new PlaceBuildingCommand
                        {
                            Issuer = _local,
                            BuildingDefId = _roster.ProducerBuildingId,
                            X = x,
                            Z = z,
                            YawDegrees = 0f,
                        });
                        MatchFeedback.Show("Building ordered");
                        CancelPlaceMode();
                    }
                }

                return;
            }

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
                    _selectedBuilding = null;
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

            if (UnityEngine.Input.GetMouseButtonDown(1) && !IsPointerOverUi())
            {
                // Building selected: RMB ground sets rally.
                if (_selectedBuilding.HasValue && TryRaycastGround(out float rx, out float rz))
                {
                    rx = Mathf.Clamp(rx, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                    rz = Mathf.Clamp(rz, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                    _commands.SubmitLocal(new SetRallyCommand
                    {
                        Issuer = _local,
                        BuildingId = _selectedBuilding.Value,
                        TargetX = rx,
                        TargetZ = rz,
                    });
                    return;
                }

                var unitIds = GetOrderUnitIds();
                if (unitIds.Length == 0)
                    return;

                if (TryPickResource(out var resource))
                {
                    var builders = GetSelectedBuilderIds();
                    if (builders.Length > 0)
                    {
                        _commands.SubmitLocal(new GatherCommand
                        {
                            Issuer = _local,
                            UnitIds = builders,
                            ResourceNodeId = resource.Id,
                        });
                        MatchFeedback.Show("Gathering...");
                        return;
                    }
                }

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

                if (TryRaycastGround(out float mx, out float mz))
                {
                    mx = Mathf.Clamp(mx, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                    mz = Mathf.Clamp(mz, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                    _commands.SubmitLocal(new MoveCommand
                    {
                        Issuer = _local,
                        UnitIds = unitIds,
                        TargetX = mx,
                        TargetZ = mz,
                    });
                }
            }
        }

        private void HandleAttackMoveArmedPointer()
        {
            if (IsPointerOverUi())
                return;

            bool lmb = UnityEngine.Input.GetMouseButtonDown(0);
            bool rmb = UnityEngine.Input.GetMouseButtonDown(1);
            if (!lmb && !rmb)
                return;

            if (TryRaycastGround(out float x, out float z))
            {
                x = Mathf.Clamp(x, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                z = Mathf.Clamp(z, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                var unitIds = GetOrderUnitIds();
                if (unitIds.Length > 0)
                {
                    _commands.SubmitLocal(new AttackMoveCommand
                    {
                        Issuer = _local,
                        UnitIds = unitIds,
                        TargetX = x,
                        TargetZ = z,
                    });
                }

                CancelAttackMoveArm();
                return;
            }

            if (rmb)
                CancelAttackMoveArm();
        }

        private void HandleClickSelect(bool additive)
        {
            if (TryPickEntity(out var view))
            {
                if (!view.IsUnit && view.Owner == _local)
                {
                    _selectedBuilding = view.Id;
                    if (!additive)
                        _selection.Clear();
                    return;
                }

                if (view.IsUnit && view.Owner == _local)
                {
                    _selectedBuilding = null;
                    if (additive)
                        _selection.Toggle(view.Id);
                    else
                        _selection.Set(new[] { view.Id });
                    return;
                }
            }

            if (!additive && TryRaycastGround(out _, out _))
            {
                _selection.Clear();
                _selectedBuilding = null;
            }
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
                if (HasSelectedBuilder())
                    EnterPlaceMode();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                if (_selectedBuilding.HasValue)
                    TrainFromSelectedBuilding();
                else if (TryFindOwnedKeep(out var keepId))
                {
                    _selectedBuilding = keepId;
                    TrainFromSelectedBuilding();
                }
                else if (TryFindOwnedProducer(out var buildingId))
                {
                    _selectedBuilding = buildingId;
                    TrainFromSelectedBuilding();
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.X) && _selectedBuilding.HasValue)
                CancelProduction();

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
                CancelPlaceMode();
                _attackMoveArmed = true;
                MatchFeedback.Show("Attack-move: click ground");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                _selectedBuilding = null;
                AutoSelectOwnedUnits();
            }
        }

        private void UpdateGhost()
        {
            if (!_placeMode)
                return;
            EnsureGhost();
            if (_ghost == null || rigCamera == null)
                return;

            if (!TryRaycastGround(out float x, out float z))
            {
                _ghost.SetActive(false);
                return;
            }

            bool ok = CanPlaceAt(x, z);
            _ghost.SetActive(true);
            _ghost.transform.position = new Vector3(x, 0.2f, z);
            if (_ghostRenderer != null)
            {
                var color = ok ? new Color(0.25f, 0.85f, 0.4f, 0.55f) : new Color(0.9f, 0.2f, 0.2f, 0.55f);
                SetMatColor(_ghostRenderer.sharedMaterial, color);
            }
        }

        private void UpdateCursorMode()
        {
            if (_placeMode)
            {
                if (TryRaycastGround(out float x, out float z))
                    CurrentCursorMode = CanPlaceAt(x, z) ? OrderCursorMode.Build : OrderCursorMode.Invalid;
                else
                    CurrentCursorMode = OrderCursorMode.Invalid;
                return;
            }

            if (_attackMoveArmed)
            {
                CurrentCursorMode = OrderCursorMode.Attack;
                return;
            }

            if (_selectedBuilding.HasValue)
            {
                CurrentCursorMode = OrderCursorMode.Train;
                return;
            }

            if (_selection != null && _selection.Selected.Count > 0)
            {
                if (HasSelectedBuilder() && TryPickResource(out _))
                {
                    CurrentCursorMode = OrderCursorMode.Gather;
                    return;
                }

                if (TryPickEntity(out var hover) && hover.Owner != _local && hover.IsRevealed)
                {
                    CurrentCursorMode = OrderCursorMode.Attack;
                    return;
                }

                CurrentCursorMode = OrderCursorMode.Move;
                return;
            }

            CurrentCursorMode = OrderCursorMode.Select;
        }

        private bool CanPlaceAt(float x, float z)
        {
            if (x < -MapBounds.PlayableHalfExtent || x > MapBounds.PlayableHalfExtent
                || z < -MapBounds.PlayableHalfExtent || z > MapBounds.PlayableHalfExtent)
                return false;

            if (!HasSelectedBuilderNear(x, z))
                return false;

            if (match != null && match.Definitions != null && match.Wallet != null && _roster != null)
            {
                if (match.Definitions.TryGetBuilding(_roster.ProducerBuildingId, out var def))
                {
                    if (!match.Wallet.CanAfford(_local, ResourceType.Gold, def.GoldCost))
                        return false;
                    if (!match.Wallet.CanAfford(_local, ResourceType.Timber, def.TimberCost))
                        return false;
                }
            }

            return true;
        }

        private bool HasSelectedBuilder()
        {
            if (_selection == null)
                return false;
            for (int i = 0; i < _selection.Selected.Count; i++)
            {
                var id = _selection.Selected[i];
                for (int u = 0; u < _world.Units.Count; u++)
                {
                    var unit = _world.Units[u];
                    if (unit.Id == id && unit.IsAlive && unit.Owner == _local
                        && FactionDefaultContent.IsBuilderUnitId(unit.DefinitionId))
                        return true;
                }
            }

            return false;
        }

        private SimEntityId[] GetSelectedBuilderIds()
        {
            var list = new List<SimEntityId>();
            if (_selection == null)
                return list.ToArray();
            for (int i = 0; i < _selection.Selected.Count; i++)
            {
                var id = _selection.Selected[i];
                for (int u = 0; u < _world.Units.Count; u++)
                {
                    var unit = _world.Units[u];
                    if (unit.Id == id && unit.IsAlive && unit.Owner == _local
                        && FactionDefaultContent.IsBuilderUnitId(unit.DefinitionId))
                    {
                        list.Add(id);
                        break;
                    }
                }
            }

            return list.ToArray();
        }

        private bool HasSelectedBuilderNear(float x, float z)
        {
            float r2 = builderPlaceRadius * builderPlaceRadius;
            if (_selection == null)
                return false;
            for (int i = 0; i < _selection.Selected.Count; i++)
            {
                var id = _selection.Selected[i];
                for (int u = 0; u < _world.Units.Count; u++)
                {
                    var unit = _world.Units[u];
                    if (unit.Id != id || !unit.IsAlive || unit.Owner != _local)
                        continue;
                    if (!FactionDefaultContent.IsBuilderUnitId(unit.DefinitionId))
                        continue;
                    float dx = unit.X - x;
                    float dz = unit.Z - z;
                    if (dx * dx + dz * dz <= r2)
                        return true;
                }
            }

            // Also allow any owned builder nearby (sim rule).
            for (int u = 0; u < _world.Units.Count; u++)
            {
                var unit = _world.Units[u];
                if (!unit.IsAlive || unit.Owner != _local)
                    continue;
                if (!FactionDefaultContent.IsBuilderUnitId(unit.DefinitionId))
                    continue;
                float dx = unit.X - x;
                float dz = unit.Z - z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }

        private void EnsureGhost()
        {
            if (_ghost != null)
                return;
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(_ghost.GetComponent<Collider>());
            _ghost.name = "BuildGhost";
            _ghost.transform.localScale = new Vector3(14f, 6f, 12f);
            _ghostRenderer = _ghost.GetComponent<Renderer>();
            var shader = Shader.Find("Asterra/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                SetMatColor(mat, new Color(0.25f, 0.85f, 0.4f, 0.55f));
                _ghostRenderer.sharedMaterial = mat;
            }

            _ghost.SetActive(false);
        }

        private static void SetMatColor(Material mat, Color color)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }

        private static bool IsAdditiveModifierHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftShift)
                   || UnityEngine.Input.GetKey(KeyCode.RightShift)
                   || UnityEngine.Input.GetKey(KeyCode.LeftCommand)
                   || UnityEngine.Input.GetKey(KeyCode.RightCommand);
        }

        private bool TryPickResource(out ResourceNodeView view)
        {
            view = null;
            if (rigCamera == null)
                return false;

            var ray = rigCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            int hitCount = Physics.RaycastNonAlloc(ray, _rayHits, 5000f, clickMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                var candidate = _rayHits[i].collider.GetComponentInParent<ResourceNodeView>();
                if (candidate == null)
                    continue;
                if (_rayHits[i].distance < bestDist)
                {
                    bestDist = _rayHits[i].distance;
                    view = candidate;
                }
            }

            if (view != null)
                return true;

            return TryScreenPickResource(out view);
        }

        private bool TryScreenPickResource(out ResourceNodeView view)
        {
            view = null;
            Vector3 mouse = UnityEngine.Input.mousePosition;
            float maxPx2 = screenPickPixels * screenPickPixels;
            float bestPx2 = maxPx2;
            var views = FindObjectsByType<ResourceNodeView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var candidate = views[i];
                if (candidate == null)
                    continue;

                Vector3 world = candidate.transform.position + Vector3.up * 2f;
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

        private static bool IsPointerOverUi() => false;

        private static Rect ScreenRectFromPoints(Vector3 a, Vector3 b)
        {
            return Rect.MinMaxRect(
                Mathf.Min(a.x, b.x),
                Mathf.Min(a.y, b.y),
                Mathf.Max(a.x, b.x),
                Mathf.Max(a.y, b.y));
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
                if (b.Owner == _local && b.CanProduce && !FactionDefaultContent.IsKeepBuildingId(b.DefinitionId))
                {
                    buildingId = b.Id;
                    return true;
                }
            }

            buildingId = default;
            return false;
        }

        private bool TryFindOwnedKeep(out SimEntityId buildingId)
        {
            for (int i = 0; i < _world.Buildings.Count; i++)
            {
                var b = _world.Buildings[i];
                if (b.Owner == _local && FactionDefaultContent.IsKeepBuildingId(b.DefinitionId) && b.CanProduce)
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
