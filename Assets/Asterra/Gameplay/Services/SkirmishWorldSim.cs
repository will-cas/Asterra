using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Placeholder world sim for the GO vertical slice. Replace internals with Entities systems later.
    /// </summary>
    public sealed class SkirmishWorldSim : IWorldSim
    {
        private readonly IResourceWallet _wallet;
        private readonly List<IUnit> _units = new();
        private readonly List<IBuilding> _buildings = new();
        private ulong _mutationCounter;

        public SkirmishWorldSim(IResourceWallet wallet)
        {
            _wallet = wallet;
        }

        public void Register(IUnit unit) => _units.Add(unit);
        public void Register(IBuilding building) => _buildings.Add(building);

        public void ApplyCommands(IReadOnlyList<GameCommand> commands)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                switch (commands[i])
                {
                    case TrainUnitCommand train:
                        // Production system owns fulfillment; count mutation for hash.
                        _mutationCounter ^= (ulong)train.BuildingId.Value * 397ul;
                        break;
                    case PlaceBuildingCommand place:
                        _mutationCounter ^= (ulong)place.BuildingDefId.GetHashCode();
                        break;
                    case MoveCommand move:
                        _mutationCounter ^= (ulong)move.UnitIds.Length * 911ul;
                        break;
                    case AttackCommand attack:
                        _mutationCounter ^= (ulong)attack.TargetId.Value * 733ul;
                        break;
                    case ChooseUpgradeCommand upgrade:
                        _mutationCounter ^= (ulong)upgrade.UpgradeDefId.GetHashCode();
                        break;
                    case CaptureTerritoryCommand capture:
                        _mutationCounter ^= (ulong)capture.TerritoryNodeId.Value * 541ul;
                        break;
                    case SetStanceCommand stance:
                        _mutationCounter ^= (ulong)stance.Stance;
                        break;
                    default:
                        break;
                }
            }
        }

        public void Tick(float deltaSeconds)
        {
            // Gathering / combat / construction advance in later phases.
            _ = deltaSeconds;
            _ = _wallet;
        }

        public ulong ComputeWorldHash()
        {
            ulong hash = 14695981039346656037ul;
            hash ^= _mutationCounter;
            hash ^= (ulong)_units.Count * 1099511628211ul;
            hash ^= (ulong)_buildings.Count * 1099511628211ul;
            return hash;
        }
    }
}
