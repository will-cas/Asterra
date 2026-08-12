using Asterra.Core;
using Asterra.Gameplay.Content;
using UnityEngine;

namespace Asterra.Gameplay.Player
{
    /// <summary>
    /// Issues local lockstep commands from simple keyboard shortcuts (no scene raycasts required).
    /// Keys are intentionally crude for headless/dev iteration before UI polish.
    /// </summary>
    public sealed class LocalOrderController : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;

        private SelectionState _selection;
        private ICommandBus _commands;
        private IWorldQuery _world;
        private PlayerId _local;

        public SelectionState Selection => _selection;

        public void Bind(MatchBootstrap bootstrap)
        {
            match = bootstrap;
            _selection = new SelectionState();
            _commands = bootstrap.Commands;
            _world = bootstrap.World;
            _local = bootstrap.Session.LocalPlayer;
            AutoSelectOwnedUnits();
        }

        private void Update()
        {
            if (match == null || _commands == null)
                return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.B))
            {
                _commands.SubmitLocal(new PlaceBuildingCommand
                {
                    Issuer = _local,
                    BuildingDefId = SkirmishDefaultContent.BarracksId,
                    X = -300f,
                    Z = 30f,
                    YawDegrees = 0f,
                });
            }

            // T — train militia from first owned producer
            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                if (TryFindOwnedProducer(out var buildingId))
                {
                    _commands.SubmitLocal(new TrainUnitCommand
                    {
                        Issuer = _local,
                        BuildingId = buildingId,
                        UnitDefId = SkirmishDefaultContent.MilitiaId,
                    });
                }
            }

            // C — capture center territory
            if (UnityEngine.Input.GetKeyDown(KeyCode.C) && _world.Territories.Count > 0)
            {
                _commands.SubmitLocal(new CaptureTerritoryCommand
                {
                    Issuer = _local,
                    TerritoryNodeId = _world.Territories[0].Id,
                });
            }

            // U — unlock militia training upgrade
            if (UnityEngine.Input.GetKeyDown(KeyCode.U))
            {
                _commands.SubmitLocal(new ChooseUpgradeCommand
                {
                    Issuer = _local,
                    UpgradeDefId = SkirmishDefaultContent.MilitiaTrainingId,
                });
            }

            // A — attack first hostile unit with selection (or all owned)
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

            // M — move selection toward map center
            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                _commands.SubmitLocal(new MoveCommand
                {
                    Issuer = _local,
                    UnitIds = GetOrderUnitIds(),
                    TargetX = 0f,
                    TargetZ = 0f,
                });
            }

            // R — refresh selection to all owned living units
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                AutoSelectOwnedUnits();
        }

        private EntityId[] GetOrderUnitIds()
        {
            if (_selection.Selected.Count > 0)
            {
                var arr = new EntityId[_selection.Selected.Count];
                for (int i = 0; i < _selection.Selected.Count; i++)
                    arr[i] = _selection.Selected[i];
                return arr;
            }

            AutoSelectOwnedUnits();
            var fallback = new EntityId[_selection.Selected.Count];
            for (int i = 0; i < _selection.Selected.Count; i++)
                fallback[i] = _selection.Selected[i];
            return fallback;
        }

        private void AutoSelectOwnedUnits()
        {
            var ids = new System.Collections.Generic.List<EntityId>();
            for (int i = 0; i < _world.Units.Count; i++)
            {
                var u = _world.Units[i];
                if (u.Owner == _local && u.IsAlive)
                    ids.Add(u.Id);
            }

            _selection.Set(ids);
        }

        private bool TryFindOwnedProducer(out EntityId buildingId)
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

        private bool TryFindHostile(out EntityId targetId)
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
