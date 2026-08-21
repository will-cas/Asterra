using System.Collections.Generic;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Audio;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using Asterra.Gameplay.Sim;
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
        [SerializeField] private float screenPickPixels = 72f;
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

        private SimEntityId? _selectedBuilding;
        private bool _placeMode;
        private string _placeBuildingDefId;
        private float _placeYawDegrees;
        private bool _attackMoveArmed;
        private bool _patrolArmed;
        private GameObject _ghost;
        private Renderer _ghostRenderer;

        private readonly List<SimEntityId>[] _controlGroups = CreateControlGroups();
        private int _lastGroupTapIndex = -1;
        private float _lastGroupTapTime = -10f;

        public SelectionState Selection => _selection;
        public bool IsPlaceMode => _placeMode;
        public bool IsAttackMoveArmed => _attackMoveArmed;
        public bool IsPatrolArmed => _patrolArmed;
        public SimEntityId? SelectedBuilding => _selectedBuilding;
        public OrderCursorMode CurrentCursorMode { get; private set; } = OrderCursorMode.Select;
        public int IdleWorkerCount => CountIdleWorkers();
        public bool HasBuilderSelected => HasSelectedBuilder();
        public bool HasCombatUnitSelected => HasSelectedCombatUnit();

        private static List<SimEntityId>[] CreateControlGroups()
        {
            var groups = new List<SimEntityId>[10];
            for (int i = 0; i < groups.Length; i++)
                groups[i] = new List<SimEntityId>();
            return groups;
        }

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
            {
                bridge.BindSelection(() => _selection.Selected);
                bridge.BindSelectedBuilding(() => _selectedBuilding);
            }
        }

        public void EnterPlaceMode()
        {
            EnterPlaceMode(_roster != null ? _roster.ProducerBuildingId : null);
        }

        public void EnterPlaceMode(string buildingDefId)
        {
            if (!HasSelectedBuilder() || string.IsNullOrEmpty(buildingDefId))
                return;
            CancelAttackMoveArm();
            CancelPatrolArm();
            _placeBuildingDefId = buildingDefId;
            _placeMode = true;
            _placeYawDegrees = 0f;
            EnsureGhost();
            ResizeGhostForCurrentBuilding();
        }

        public void CancelPlaceMode()
        {
            _placeMode = false;
            _placeBuildingDefId = null;
            _placeYawDegrees = 0f;
            if (_ghost != null)
                _ghost.SetActive(false);
        }

        public void CancelAttackMoveArm()
        {
            _attackMoveArmed = false;
        }

        public void CancelPatrolArm()
        {
            _patrolArmed = false;
        }

        public void StopSelected()
        {
            var ids = GetOrderUnitIds();
            if (ids.Length == 0 || _commands == null)
                return;
            _commands.SubmitLocal(new StopCommand
            {
                Issuer = _local,
                UnitIds = ids,
            });
        }

        public void SetSelectedStance(UnitStance stance)
        {
            var ids = GetOrderUnitIds();
            if (ids.Length == 0 || _commands == null)
                return;
            _commands.SubmitLocal(new SetStanceCommand
            {
                Issuer = _local,
                UnitIds = ids,
                Stance = stance,
            });
            MatchFeedback.Show($"Stance: {stance}");
        }

        public void SelectIdleWorker()
        {
            if (_world == null)
                return;
            var idle = new List<SimEntityId>();
            for (int i = 0; i < _world.Units.Count; i++)
            {
                var u = _world.Units[i];
                if (!u.IsAlive || u.Owner != _local || !u.IsIdle)
                    continue;
                if (!FactionDefaultContent.IsBuilderUnitId(u.DefinitionId))
                    continue;
                idle.Add(u.Id);
            }

            if (idle.Count == 0)
            {
                MatchFeedback.Show("No idle workers", AsterraSfx.Invalid);
                return;
            }

            _selectedBuilding = null;
            CancelPlaceMode();
            _selection.Set(idle);
            FocusOnSelection();
            MatchFeedback.Show($"Idle workers: {idle.Count}");
        }

        public bool CanUseCommanderAbility =>
            _roster != null
            && _roster.PowerIds != null
            && _roster.PowerIds.Length > 0
            && _commands != null;

        public bool HasAnyPowerUnlocked
        {
            get
            {
                if (_roster?.PowerIds == null || _world == null)
                    return false;
                for (int i = 0; i < _roster.PowerIds.Length; i++)
                {
                    if (_world.HasPower(_local, _roster.PowerIds[i]))
                        return true;
                }

                return false;
            }
        }

        public void ActivateCommanderAbility()
        {
            if (_roster?.PowerIds == null || _roster.PowerIds.Length == 0)
                return;
            ActivateCommanderAbility(_roster.PowerIds[0]);
        }

        public void ActivateCommanderAbility(string powerDefId)
        {
            if (_commands == null || _roster == null || string.IsNullOrEmpty(powerDefId))
                return;

            if (_world != null && !_world.HasPower(_local, powerDefId))
            {
                string name = powerDefId;
                if (match != null && match.Definitions != null && match.Definitions.TryGetPower(powerDefId, out var p))
                    name = p.DisplayName;
                MatchFeedback.Show($"Unlock {name} at your keep first");
                return;
            }

            if (_world != null
                && _world.TryGetCommanderAbilityStatus(_local, powerDefId, out float cd, out _)
                && cd > 0.05f)
            {
                MatchFeedback.Show($"Cooling down ({cd:0}s)");
                return;
            }

            _commands.SubmitLocal(new ActivateCommanderAbilityCommand
            {
                Issuer = _local,
                PowerDefId = powerDefId,
            });
            if (match != null && match.Definitions != null && match.Definitions.TryGetPower(powerDefId, out var def))
            {
                string effect = def.Effect == PowerEffectKind.ArmorAura ? $"+{def.EffectMagnitude:0} armor"
                    : def.Effect == PowerEffectKind.MoveSpeedAura ? $"+{def.EffectMagnitude:0.#} move"
                    : $"+{def.EffectMagnitude:0} damage";
                MatchFeedback.Show($"{def.DisplayName} — {effect} for {def.DurationSeconds:0}s");
            }
        }

        public void UnlockPower()
        {
            if (_roster?.PowerIds == null || _roster.PowerIds.Length == 0)
                return;
            UnlockPower(_roster.PowerIds[0]);
        }

        public void UnlockPower(string powerDefId)
        {
            if (_commands == null || string.IsNullOrEmpty(powerDefId))
                return;
            if (_world != null && _world.HasPower(_local, powerDefId))
                return;
            _commands.SubmitLocal(new UnlockPowerCommand
            {
                Issuer = _local,
                PowerDefId = powerDefId,
            });
            if (match != null && match.Definitions != null && match.Definitions.TryGetPower(powerDefId, out var def))
                MatchFeedback.Show($"Unlocking {def.DisplayName}");
        }

        public void ResearchUpgrade()
        {
            if (_roster == null)
                return;
            ResearchUpgrade(_roster.BasicUpgradeId);
        }

        public void ResearchUpgrade(string upgradeDefId)
        {
            if (_commands == null || string.IsNullOrEmpty(upgradeDefId))
                return;
            if (!_selectedBuilding.HasValue)
            {
                MatchFeedback.Show("Select a keep or barracks to research", AsterraSfx.Invalid);
                return;
            }

            if (_world != null && _world.HasUpgrade(_local, upgradeDefId))
            {
                MatchFeedback.Show("Already researched", AsterraSfx.Invalid);
                return;
            }

            _commands.SubmitLocal(new ChooseUpgradeCommand
            {
                Issuer = _local,
                UpgradeDefId = upgradeDefId,
                BuildingId = _selectedBuilding.Value,
            });
            MatchFeedback.Show("Research started", AsterraSfx.OrderResearch);
        }

        public void ApplyUpgradeToSelected()
        {
            if (_roster == null)
                return;
            ApplyUpgradeToSelected(_roster.BasicUpgradeId);
        }

        public void ApplyUpgradeToSelected(string upgradeDefId)
        {
            if (_commands == null || _selection == null || _selection.Selected.Count == 0)
                return;
            if (_world == null || !_world.HasUpgrade(_local, upgradeDefId))
            {
                MatchFeedback.Show("Research equipment at barracks first", AsterraSfx.Invalid);
                return;
            }

            var selected = _selection.Selected;
            var pending = new System.Collections.Generic.List<SimEntityId>(selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                var id = selected[i];
                bool already = false;
                bool skip = false;
                for (int u = 0; u < _world.Units.Count; u++)
                {
                    var snap = _world.Units[u];
                    if (snap.Id.Value != id.Value)
                        continue;
                    if (FactionDefaultContent.IsBuilderUnitId(snap.DefinitionId))
                        skip = true;
                    else
                        already = snap.HasAppliedEquipment(upgradeDefId);
                    break;
                }

                if (skip || already)
                    continue;
                pending.Add(id);
            }

            if (pending.Count == 0)
            {
                MatchFeedback.Show("Already equipped on selection", AsterraSfx.Invalid);
                return;
            }

            _commands.SubmitLocal(new ApplyUnitUpgradeCommand
            {
                Issuer = _local,
                UpgradeDefId = upgradeDefId,
                UnitIds = pending.ToArray(),
            });
        }

        public void AttachToKeep(byte slotIndex, string buildingDefId)
        {
            if (!_selectedBuilding.HasValue || _commands == null || string.IsNullOrEmpty(buildingDefId))
                return;
            _commands.SubmitLocal(new AttachBuildingCommand
            {
                Issuer = _local,
                ParentBuildingId = _selectedBuilding.Value,
                SlotIndex = slotIndex,
                BuildingDefId = buildingDefId,
            });
            MatchFeedback.Show("Attaching to keep", AsterraSfx.OrderBuild);
        }

        public void JumpToBuilding(SimEntityId buildingId)
        {
            if (_world == null)
                return;
            for (int i = 0; i < _world.Buildings.Count; i++)
            {
                var b = _world.Buildings[i];
                if (b.Id != buildingId)
                    continue;
                var cam = FindFirstObjectByType<RtsCameraRig>();
                if (cam != null)
                    cam.FocusOn(b.X, b.Z);
                _selectedBuilding = buildingId;
                return;
            }
        }

        public void FocusOnSelection()
        {
            if (_selection == null || _selection.Selected.Count == 0 || _world == null)
                return;
            float sx = 0f;
            float sz = 0f;
            int n = 0;
            for (int i = 0; i < _selection.Selected.Count; i++)
            {
                var id = _selection.Selected[i];
                for (int u = 0; u < _world.Units.Count; u++)
                {
                    var unit = _world.Units[u];
                    if (unit.Id != id || !unit.IsAlive)
                        continue;
                    sx += unit.X;
                    sz += unit.Z;
                    n++;
                    break;
                }
            }

            if (n == 0)
                return;
            var cam = FindFirstObjectByType<RtsCameraRig>();
            if (cam != null)
                cam.FocusOn(sx / n, sz / n);
        }

        private int CountIdleWorkers()
        {
            if (_world == null)
                return 0;
            int count = 0;
            for (int i = 0; i < _world.Units.Count; i++)
            {
                var u = _world.Units[i];
                if (!u.IsAlive || u.Owner != _local || !u.IsIdle)
                    continue;
                if (FactionDefaultContent.IsBuilderUnitId(u.DefinitionId))
                    count++;
            }

            return count;
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

            if (match?.Definitions != null && match.Wallet != null
                && match.Definitions.TryGetUnit(defId, out var unitDef))
            {
                if (!match.Wallet.CanAfford(_local, ResourceType.Gold, unitDef.GoldCost))
                {
                    MatchFeedback.Show("Not enough gold", AsterraSfx.Invalid);
                    return;
                }
            }

            if (!_selectedBuilding.HasValue
                || !TryGetBuildingSnapshot(_selectedBuilding.Value, out var building)
                || !building.CanProduce)
            {
                MatchFeedback.Show("Cannot train here", AsterraSfx.Invalid);
                return;
            }

            _commands.SubmitLocal(new TrainUnitCommand
            {
                Issuer = _local,
                BuildingId = _selectedBuilding.Value,
                UnitDefId = defId,
            });
            AsterraAudio.Play(AsterraSfx.OrderTrain, 0.7f);
            MatchFeedback.Show("Training...");
        }

        private bool TryResolveCaptureTarget(out SimEntityId territoryId)
        {
            territoryId = default;
            if (_world == null || _world.Territories.Count == 0)
                return false;

            float fromX = 0f, fromZ = 0f;
            int n = 0;
            for (int i = 0; i < _selection.Selected.Count; i++)
            {
                var id = _selection.Selected[i];
                for (int u = 0; u < _world.Units.Count; u++)
                {
                    var snap = _world.Units[u];
                    if (snap.Id != id || !snap.IsAlive)
                        continue;
                    fromX += snap.X;
                    fromZ += snap.Z;
                    n++;
                    break;
                }
            }

            if (n == 0 && TryRaycastGround(out float gx, out float gz))
            {
                fromX = gx;
                fromZ = gz;
                n = 1;
            }

            if (n == 0)
            {
                territoryId = _world.Territories[0].Id;
                return true;
            }

            fromX /= n;
            fromZ /= n;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < _world.Territories.Count; i++)
            {
                var t = _world.Territories[i];
                float dx = t.X - fromX;
                float dz = t.Z - fromZ;
                float d2 = dx * dx + dz * dz;
                if (d2 < best)
                {
                    best = d2;
                    territoryId = t.Id;
                    found = true;
                }
            }

            return found;
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
                if (_patrolArmed)
                    CancelPatrolArm();
                CancelPlaceMode();
            }

            if (_attackMoveArmed)
            {
                HandleAttackMoveArmedPointer();
                return;
            }

            if (_patrolArmed)
            {
                HandlePatrolArmedPointer();
                return;
            }

            if (_placeMode)
            {
                HandlePlaceModeRotation();

                if (UnityEngine.Input.GetMouseButtonDown(1))
                {
                    CancelPlaceMode();
                    return;
                }

                if (UnityEngine.Input.GetMouseButtonDown(0) && !IsPointerOverUi())
                {
                    if (TryRaycastGround(out float x, out float z) && CanPlaceAt(x, z))
                    {
                        string defId = string.IsNullOrEmpty(_placeBuildingDefId)
                            ? _roster.ProducerBuildingId
                            : _placeBuildingDefId;
                        if (IsWallLikeDef(defId))
                            WallPlacement.Snap(ref x, ref z);
                        _commands.SubmitLocal(new PlaceBuildingCommand
                        {
                            Issuer = _local,
                            BuildingDefId = defId,
                            X = x,
                            Z = z,
                            YawDegrees = _placeYawDegrees,
                        });
                        // Send selected builders to the foundation so construction can start.
                        var builders = GetSelectedBuilderIds();
                        if (builders.Length > 0)
                        {
                            _commands.SubmitLocal(new MoveCommand
                            {
                                Issuer = _local,
                                UnitIds = builders,
                                TargetX = x,
                                TargetZ = z,
                            });
                        }

                        MatchFeedback.Show("Building ordered — send builder to site", AsterraSfx.OrderBuild);
                        if (!IsAdditiveModifierHeld())
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
                // Swallow world select when the click is still over HUD (e.g. Train Builder).
                if (!IsPointerOverUi())
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
                        MatchFeedback.Show("Gathering...", AsterraSfx.OrderGather);
                        return;
                    }
                }

                if (TryPickHostileEntity(out var hostile))
                {
                    _commands.SubmitLocal(new AttackCommand
                    {
                        Issuer = _local,
                        UnitIds = unitIds,
                        TargetId = hostile.Id,
                    });
                    AsterraAudio.Play(AsterraSfx.OrderAttack, 0.8f);
                    return;
                }

                if (TryPickEntity(out var targetView))
                {
                    if (targetView.Owner == _local && !targetView.IsUnit
                        && TryGetBuildingSnapshot(targetView.Id, out var gb)
                        && gb.AllowsGarrison)
                    {
                        _commands.SubmitLocal(new EnterGarrisonCommand
                        {
                            Issuer = _local,
                            UnitIds = unitIds,
                            BuildingId = targetView.Id,
                        });
                        MatchFeedback.Show("Garrisoning...", AsterraSfx.OrderMove);
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
                    AsterraAudio.Play(AsterraSfx.OrderMove, 0.7f);
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
                    MatchFeedback.Show("Attack-move ordered", AsterraSfx.OrderAttack);
                }

                CancelAttackMoveArm();
                return;
            }

            if (rmb)
                CancelAttackMoveArm();
        }

        private void HandlePatrolArmedPointer()
        {
            if (IsPointerOverUi())
                return;

            bool lmb = UnityEngine.Input.GetMouseButtonDown(0);
            bool rmb = UnityEngine.Input.GetMouseButtonDown(1);
            if (!lmb && !rmb)
                return;

            if (lmb && TryRaycastGround(out float x, out float z))
            {
                x = Mathf.Clamp(x, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                z = Mathf.Clamp(z, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                var unitIds = GetOrderUnitIds();
                if (unitIds.Length > 0)
                {
                    _commands.SubmitLocal(new PatrolCommand
                    {
                        Issuer = _local,
                        UnitIds = unitIds,
                        TargetX = x,
                        TargetZ = z,
                    });
                    MatchFeedback.Show("Patrol ordered", AsterraSfx.OrderMove);
                }

                CancelPatrolArm();
                return;
            }

            if (rmb)
                CancelPatrolArm();
        }

        private void HandleClickSelect(bool additive)
        {
            if (TryPickEntity(out var view, preferUnits: true))
            {
                if (view.IsUnit && view.Owner == _local)
                {
                    _selectedBuilding = null;
                    if (additive)
                        _selection.Toggle(view.Id);
                    else
                        _selection.Set(new[] { view.Id });
                    return;
                }

                if (!view.IsUnit && view.Owner == _local)
                {
                    // Prefer keeping unit selection when a unit is under / near the cursor.
                    if (TryPickOwnedUnitNearCursor(out var nearbyUnit))
                    {
                        _selectedBuilding = null;
                        if (additive)
                            _selection.Toggle(nearbyUnit.Id);
                        else
                            _selection.Set(new[] { nearbyUnit.Id });
                        return;
                    }

                    _selectedBuilding = view.Id;
                    _selection.Clear();
                    return;
                }

                // Enemy / neutral under cursor: keep current selection (do not clear).
                return;
            }

            if (!additive && TryRaycastGround(out _, out _))
            {
                _selection.Clear();
                _selectedBuilding = null;
            }
        }

        private bool TryPickOwnedUnitNearCursor(out EntityView unit)
        {
            unit = null;
            if (rigCamera == null)
                return false;
            var ray = rigCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 5000f, clickMask);
            float best = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var view = hits[i].collider != null
                    ? hits[i].collider.GetComponentInParent<EntityView>()
                    : null;
                if (view == null || !view.IsUnit || view.Owner != _local || !view.IsRevealed)
                    continue;
                float d = hits[i].distance;
                if (d < best)
                {
                    best = d;
                    unit = view;
                }
            }

            if (unit != null)
                return true;
            return TryScreenPickOwnedUnit(out unit, maxPixels: 48f);
        }

        private bool TryPickHostileEntity(out EntityView view)
        {
            view = null;
            if (rigCamera == null)
                return false;

            var ray = rigCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 5000f, clickMask);
            float best = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i].collider != null
                    ? hits[i].collider.GetComponentInParent<EntityView>()
                    : null;
                if (candidate == null || candidate.Owner == _local || !candidate.IsRevealed)
                    continue;
                float d = hits[i].distance;
                if (d < best)
                {
                    best = d;
                    view = candidate;
                }
            }

            if (view != null)
                return true;

            Vector3 mouse = UnityEngine.Input.mousePosition;
            float bestPx2 = 64f * 64f;
            var views = FindObjectsByType<EntityView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var candidate = views[i];
                if (candidate == null || candidate.Owner == _local || !candidate.IsRevealed)
                    continue;
                Vector3 sp = rigCamera.WorldToScreenPoint(candidate.transform.position + Vector3.up * 4f);
                if (sp.z <= 0f)
                    continue;
                float dx = sp.x - mouse.x;
                float dy = sp.y - mouse.y;
                float d2 = dx * dx + dy * dy;
                if (d2 < bestPx2)
                {
                    bestPx2 = d2;
                    view = candidate;
                }
            }

            return view != null;
        }

        private bool TryScreenPickOwnedUnit(out EntityView unit, float maxPixels)
        {
            unit = null;
            if (rigCamera == null)
                return false;
            Vector3 mouse = UnityEngine.Input.mousePosition;
            float best = maxPixels * maxPixels;
            var views = FindObjectsByType<EntityView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var candidate = views[i];
                if (candidate == null || !candidate.IsUnit || candidate.Owner != _local || !candidate.IsRevealed)
                    continue;
                Vector3 sp = rigCamera.WorldToScreenPoint(candidate.transform.position + Vector3.up * 4f);
                if (sp.z <= 0f)
                    continue;
                float dx = sp.x - mouse.x;
                float dy = sp.y - mouse.y;
                float d2 = dx * dx + dy * dy;
                if (d2 < best)
                {
                    best = d2;
                    unit = candidate;
                }
            }

            return unit != null;
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
            HandleControlGroupHotkeys();

            if (UnityEngine.Input.GetKeyDown(KeyCode.B))
            {
                if (HasSelectedBuilder())
                    EnterPlaceMode(_roster.ProducerBuildingId);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.N) && HasSelectedBuilder())
                EnterPlaceMode(_roster.TowerBuildingId);
            if (UnityEngine.Input.GetKeyDown(KeyCode.M) && HasSelectedBuilder())
                EnterPlaceMode(_roster.WallBuildingId);
            if (UnityEngine.Input.GetKeyDown(KeyCode.O) && HasSelectedBuilder())
                EnterPlaceMode(_roster.OutpostBuildingId);

            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                if (_selectedBuilding.HasValue)
                {
                    _selection.Clear();
                    TrainFromSelectedBuilding();
                }
                else if (TryFindOwnedKeep(out var keepId))
                {
                    _selection.Clear();
                    _selectedBuilding = keepId;
                    TrainFromSelectedBuilding();
                }
                else if (TryFindOwnedProducer(out var buildingId))
                {
                    _selection.Clear();
                    _selectedBuilding = buildingId;
                    TrainFromSelectedBuilding();
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.X) && _selectedBuilding.HasValue)
                CancelProduction();

            if (UnityEngine.Input.GetKeyDown(KeyCode.C) && _world.Territories.Count > 0)
            {
                if (!TryResolveCaptureTarget(out var territoryId))
                {
                    MatchFeedback.Show("No territory nearby", AsterraSfx.Invalid);
                }
                else if (_selection.Selected.Count == 0)
                {
                    MatchFeedback.Show("Select units to capture", AsterraSfx.Invalid);
                }
                else
                {
                    var ids = new SimEntityId[_selection.Selected.Count];
                    for (int i = 0; i < _selection.Selected.Count; i++)
                        ids[i] = _selection.Selected[i];
                    _commands.SubmitLocal(new CaptureTerritoryCommand
                    {
                        Issuer = _local,
                        TerritoryNodeId = territoryId,
                        UnitIds = ids,
                    });
                    MatchFeedback.Show("Capture ordered");
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.U))
            {
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift))
                    ApplyUpgradeToSelected();
                else
                    ResearchUpgrade();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Q))
            {
                if (!_placeMode)
                    ActivateCommanderAbility();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                CancelPlaceMode();
                CancelPatrolArm();
                _attackMoveArmed = true;
                MatchFeedback.Show("Attack-move: click ground");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                CancelPlaceMode();
                CancelAttackMoveArm();
                _patrolArmed = true;
                MatchFeedback.Show("Patrol: click ground");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.S))
                StopSelected();

            if (UnityEngine.Input.GetKeyDown(KeyCode.F))
                SetSelectedStance(UnitStance.Aggressive);
            if (UnityEngine.Input.GetKeyDown(KeyCode.G))
                SetSelectedStance(UnitStance.Defensive);
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
                SetSelectedStance(UnitStance.Hold);

            if (UnityEngine.Input.GetKeyDown(KeyCode.Period) || UnityEngine.Input.GetKeyDown(KeyCode.I))
                SelectIdleWorker();

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                _selectedBuilding = null;
                AutoSelectOwnedUnits();
            }
        }

        private void HandleControlGroupHotkeys()
        {
            bool assign = UnityEngine.Input.GetKey(KeyCode.LeftControl)
                          || UnityEngine.Input.GetKey(KeyCode.RightControl)
                          || UnityEngine.Input.GetKey(KeyCode.LeftCommand)
                          || UnityEngine.Input.GetKey(KeyCode.RightCommand);

            for (int g = 1; g <= 9; g++)
            {
                var key = KeyCode.Alpha0 + g;
                if (!UnityEngine.Input.GetKeyDown(key))
                    continue;

                if (assign)
                {
                    AssignControlGroup(g);
                    MatchFeedback.Show($"Group {g} assigned");
                }
                else
                {
                    RecallControlGroup(g);
                }
            }
        }

        private void AssignControlGroup(int index)
        {
            if (index < 1 || index > 9 || _selection == null)
                return;
            var list = _controlGroups[index];
            list.Clear();
            for (int i = 0; i < _selection.Selected.Count; i++)
                list.Add(_selection.Selected[i]);
        }

        private void RecallControlGroup(int index)
        {
            if (index < 1 || index > 9)
                return;
            var list = _controlGroups[index];
            if (list.Count == 0)
            {
                MatchFeedback.Show($"Group {index} empty");
                return;
            }

            // Prune dead / foreign.
            var alive = new List<SimEntityId>();
            for (int i = 0; i < list.Count; i++)
            {
                var id = list[i];
                for (int u = 0; u < _world.Units.Count; u++)
                {
                    var unit = _world.Units[u];
                    if (unit.Id != id || !unit.IsAlive || unit.Owner != _local)
                        continue;
                    alive.Add(id);
                    break;
                }
            }

            list.Clear();
            list.AddRange(alive);
            if (alive.Count == 0)
            {
                MatchFeedback.Show($"Group {index} empty");
                return;
            }

            _selectedBuilding = null;
            CancelPlaceMode();
            _selection.Set(alive);

            float now = Time.unscaledTime;
            if (_lastGroupTapIndex == index && now - _lastGroupTapTime < 0.35f)
                FocusOnSelection();
            _lastGroupTapIndex = index;
            _lastGroupTapTime = now;
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

            if (IsWallLikeDef(_placeBuildingDefId))
                WallPlacement.Snap(ref x, ref z);

            bool ok = CanPlaceAt(x, z);
            _ghost.SetActive(true);
            _ghost.transform.position = new Vector3(x, 0.2f, z);
            _ghost.transform.rotation = Quaternion.Euler(0f, _placeYawDegrees, 0f);
            ResizeGhostForCurrentBuilding();
            if (_ghostRenderer != null)
            {
                var color = ok ? new Color(0.25f, 0.85f, 0.4f, 0.55f) : new Color(0.9f, 0.2f, 0.2f, 0.55f);
                SetMatColor(_ghostRenderer.sharedMaterial, color);
            }
        }

        private void HandlePlaceModeRotation()
        {
            int steps = 0;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.LeftBracket))
                steps = -1;
            else if (UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetKeyDown(KeyCode.RightBracket))
                steps = 1;

            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                steps = scroll > 0f ? 1 : -1;

            if (steps == 0)
                return;

            _placeYawDegrees = Mathf.Repeat(_placeYawDegrees + steps * 90f, 360f);
            ResizeGhostForCurrentBuilding();
            MatchFeedback.Show($"Facing {_placeYawDegrees:0}°");
        }

        private bool IsWallLikeDef(string defId)
        {
            if (string.IsNullOrEmpty(defId) || match == null || match.Definitions == null)
                return false;
            if (!match.Definitions.TryGetBuilding(defId, out var def))
                return false;
            return def.Kind == BuildingKind.Wall || def.Kind == BuildingKind.Gate || def.SnapToWallGrid;
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
                if (TryRaycastGround(out float ax, out float az) && !IsPassableForSelection(ax, az))
                    CurrentCursorMode = OrderCursorMode.Invalid;
                else
                    CurrentCursorMode = OrderCursorMode.Attack;
                return;
            }

            if (_patrolArmed)
            {
                if (TryRaycastGround(out float px, out float pz) && !IsPassableForSelection(px, pz))
                    CurrentCursorMode = OrderCursorMode.Invalid;
                else
                    CurrentCursorMode = OrderCursorMode.Move;
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

                if (TryRaycastGround(out float gx, out float gz) && !IsPassableForSelection(gx, gz))
                {
                    CurrentCursorMode = OrderCursorMode.Invalid;
                    return;
                }

                CurrentCursorMode = OrderCursorMode.Move;
                return;
            }

            CurrentCursorMode = OrderCursorMode.Select;
        }

        private bool IsPassableForSelection(float x, float z)
        {
            if (match?.World is not SkirmishWorldSim sim)
                return true;

            var env = sim.Environment;
            if (env == null)
                return true;

            // Outside map.
            if (x < -MapBounds.PlayableHalfExtent || x > MapBounds.PlayableHalfExtent
                || z < -MapBounds.PlayableHalfExtent || z > MapBounds.PlayableHalfExtent)
                return false;

            var caps = SelectionTraversalCapabilities();
            // Sample a small footprint so corner/blocked edges read as impassable.
            float r = 1.2f;
            if (!env.CanUnitEnter(x, z, caps))
                return false;
            if (!env.CanUnitEnter(x + r, z, caps))
                return false;
            if (!env.CanUnitEnter(x - r, z, caps))
                return false;
            if (!env.CanUnitEnter(x, z + r, caps))
                return false;
            if (!env.CanUnitEnter(x, z - r, caps))
                return false;
            return true;
        }

        private TraversalCapability SelectionTraversalCapabilities()
        {
            // Default land. Prefer water if every selected unit is water-only (boats).
            if (_selection == null || _selection.Selected.Count == 0 || match?.Definitions == null || _world == null)
                return TraversalCapability.Land;

            bool anyLand = false;
            bool anyWater = false;
            bool anyFlying = false;
            for (int i = 0; i < _selection.Selected.Count; i++)
            {
                uint id = _selection.Selected[i].Value;
                for (int u = 0; u < _world.Units.Count; u++)
                {
                    var snap = _world.Units[u];
                    if (snap.Id.Value != id)
                        continue;
                    if (!match.Definitions.TryGetUnit(snap.DefinitionId, out var def))
                        break;
                    var c = def.TraversalCapabilities;
                    if (c == 0)
                        c = TraversalCapability.Land;
                    if ((c & TraversalCapability.Land) != 0)
                        anyLand = true;
                    if ((c & TraversalCapability.Water) != 0)
                        anyWater = true;
                    if ((c & TraversalCapability.Flying) != 0)
                        anyFlying = true;
                    break;
                }
            }

            if (anyFlying)
                return TraversalCapability.Flying;
            if (anyWater && !anyLand)
                return TraversalCapability.Water;
            return TraversalCapability.Land;
        }

        private bool CanPlaceAt(float x, float z)
        {
            if (x < -MapBounds.PlayableHalfExtent || x > MapBounds.PlayableHalfExtent
                || z < -MapBounds.PlayableHalfExtent || z > MapBounds.PlayableHalfExtent)
                return false;

            if (!HasSelectedBuilder())
                return false;

            if (match != null && match.World is SkirmishWorldSim sim
                && sim.Environment != null
                && !sim.Environment.CanPlaceBuilding(x, z))
                return false;

            if (match != null && match.Definitions != null && match.Wallet != null)
            {
                string defId = string.IsNullOrEmpty(_placeBuildingDefId)
                    ? (_roster != null ? _roster.ProducerBuildingId : null)
                    : _placeBuildingDefId;
                if (!string.IsNullOrEmpty(defId) && match.Definitions.TryGetBuilding(defId, out var def))
                {
                    if (!match.Wallet.CanAfford(_local, ResourceType.Gold, def.GoldCost))
                        return false;
                    if (!match.Wallet.CanAfford(_local, ResourceType.Timber, def.TimberCost))
                        return false;

                    float placeR = Mathf.Max(def.FootprintX, def.FootprintZ) * 0.5f;
                    for (int i = 0; i < _world.Buildings.Count; i++)
                    {
                        var b = _world.Buildings[i];
                        if (b.State == BuildingState.Destroyed)
                            continue;
                        float dx = x - b.X;
                        float dz = z - b.Z;
                        float otherR = FactionDefaultContent.IsKeepBuildingId(b.DefinitionId) ? 9f : 6f;
                        if (b.Kind == BuildingKind.Wall || b.Kind == BuildingKind.Gate)
                            otherR = 4f;
                        else if (b.Kind == BuildingKind.Tower)
                            otherR = 5f;
                        float min = placeR + otherR;
                        if (dx * dx + dz * dz < min * min)
                            return false;
                    }
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

        private bool HasSelectedCombatUnit()
        {
            if (_selection == null || _world == null)
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
            ResizeGhostForCurrentBuilding();
        }

        private void ResizeGhostForCurrentBuilding()
        {
            if (_ghost == null)
                return;
            Vector3 scale = new Vector3(14f, 6f, 12f);
            string defId = _placeBuildingDefId;
            if (!string.IsNullOrEmpty(defId) && match != null && match.Definitions != null
                && match.Definitions.TryGetBuilding(defId, out var def))
            {
                switch (def.Kind)
                {
                    case BuildingKind.Tower:
                        scale = new Vector3(6f, 14f, 6f);
                        break;
                    case BuildingKind.Wall:
                    case BuildingKind.Gate:
                        scale = new Vector3(
                            Mathf.Max(def.FootprintX, 14f),
                            5f,
                            Mathf.Max(def.FootprintZ, 4f));
                        break;
                    case BuildingKind.Outpost:
                        scale = new Vector3(8f, 7f, 8f);
                        break;
                    case BuildingKind.Producer:
                        scale = new Vector3(14f, 6f, 12f);
                        break;
                    case BuildingKind.Keep:
                        scale = new Vector3(16f, 10f, 16f);
                        break;
                    case BuildingKind.Generic:
                    case BuildingKind.Special:
                        scale = new Vector3(10f, 6f, 10f);
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(def.Kind), def.Kind, null);
                }

                bool sideways = Mathf.Abs(Mathf.DeltaAngle(_placeYawDegrees, 90f)) < 1f
                                || Mathf.Abs(Mathf.DeltaAngle(_placeYawDegrees, 270f)) < 1f;
                if (sideways && (def.Kind == BuildingKind.Wall || def.Kind == BuildingKind.Gate))
                    scale = new Vector3(scale.z, scale.y, scale.x);
            }

            _ghost.transform.localScale = scale;
            _ghost.transform.rotation = Quaternion.Euler(0f, _placeYawDegrees, 0f);
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

        private bool TryGetBuildingSnapshot(SimEntityId id, out BuildingSnapshot snap)
        {
            snap = default;
            if (match == null || match.World == null)
                return false;
            var buildings = match.World.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].Id.Value != id.Value)
                    continue;
                snap = buildings[i];
                return true;
            }

            return false;
        }

        private bool TryPickEntity(out EntityView view) => TryPickEntity(out view, preferUnits: false);

        private bool TryPickEntity(out EntityView view, bool preferUnits)
        {
            view = null;
            if (rigCamera == null)
                return false;

            var ray = rigCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            int hitCount = Physics.RaycastNonAlloc(ray, _rayHits, 5000f, clickMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            EntityView bestUnit = null;
            float bestUnitDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                var candidate = _rayHits[i].collider.GetComponentInParent<EntityView>();
                if (candidate == null || !candidate.IsRevealed)
                    continue;
                float d = _rayHits[i].distance;
                if (preferUnits && candidate.IsUnit && d < bestUnitDist)
                {
                    bestUnitDist = d;
                    bestUnit = candidate;
                }

                if (d < bestDist)
                {
                    bestDist = d;
                    view = candidate;
                }
            }

            if (preferUnits && bestUnit != null && bestUnitDist <= bestDist + 12f)
            {
                view = bestUnit;
                return true;
            }

            if (view != null)
                return true;

            return TryScreenPickEntity(out view, preferUnits);
        }

        private bool TryScreenPickEntity(out EntityView view) => TryScreenPickEntity(out view, preferUnits: false);

        private bool TryScreenPickEntity(out EntityView view, bool preferUnits)
        {
            view = null;
            Vector3 mouse = UnityEngine.Input.mousePosition;
            float maxPx2 = screenPickPixels * screenPickPixels;
            float bestPx2 = maxPx2;
            float bestUnitPx2 = maxPx2;
            EntityView bestUnit = null;
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
                if (preferUnits && candidate.IsUnit && d2 < bestUnitPx2)
                {
                    bestUnitPx2 = d2;
                    bestUnit = candidate;
                }

                if (d2 < bestPx2)
                {
                    bestPx2 = d2;
                    view = candidate;
                }
            }

            if (preferUnits && bestUnit != null)
            {
                view = bestUnit;
                return true;
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

        private static bool IsPointerOverUi() =>
            HudClickBlocker.ContainsScreenPoint(UnityEngine.Input.mousePosition);

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
            if (_selection.Selected.Count == 0)
                return System.Array.Empty<SimEntityId>();

            var arr = new SimEntityId[_selection.Selected.Count];
            for (int i = 0; i < _selection.Selected.Count; i++)
                arr[i] = _selection.Selected[i];
            return arr;
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
