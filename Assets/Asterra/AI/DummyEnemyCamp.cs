using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.AI
{
    /// <summary>
    /// Phase-1 scripted opponent: train from keep, march to center territory, attack hostiles in range intent.
    /// </summary>
    public sealed class DummyEnemyCamp : IArmyBrain
    {
        private readonly string _keepDefId;
        private readonly string _unitDefId;
        private readonly int _trainEveryTicks;
        private int _lastTrainTick = -999;

        public DummyEnemyCamp(
            PlayerId player,
            string keepDefId,
            string unitDefId,
            int trainEveryTicks = 80)
        {
            Player = player;
            _keepDefId = keepDefId;
            _unitDefId = unitDefId;
            _trainEveryTicks = trainEveryTicks;
        }

        public PlayerId Player { get; }

        public IReadOnlyList<GameCommand> Think(in ArmyBrainContext context)
        {
            var commands = new List<GameCommand>(4);
            var world = context.World;
            var tick = (int)context.Tick.Value;

            EntityId? keepId = null;
            float keepX = 0f;
            float keepZ = 0f;
            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner != Player)
                    continue;
                if (b.DefinitionId != _keepDefId)
                    continue;
                if (!b.CanProduce)
                    continue;
                keepId = b.Id;
                keepX = b.X;
                keepZ = b.Z;
                break;
            }

            if (keepId.HasValue && tick - _lastTrainTick >= _trainEveryTicks)
            {
                if (context.Wallet.CanAfford(Player, ResourceType.Gold, 50))
                {
                    commands.Add(new TrainUnitCommand
                    {
                        Issuer = Player,
                        BuildingId = keepId.Value,
                        UnitDefId = _unitDefId,
                    });
                    _lastTrainTick = tick;
                }
            }

            var myUnits = new List<EntityId>();
            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (u.Owner == Player && u.IsAlive)
                    myUnits.Add(u.Id);
            }

            if (myUnits.Count == 0)
                return commands;

            EntityId? hostile = null;
            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (!u.IsAlive || u.Owner == Player)
                    continue;
                hostile = u.Id;
                break;
            }

            if (hostile.HasValue)
            {
                commands.Add(new AttackCommand
                {
                    Issuer = Player,
                    UnitIds = myUnits.ToArray(),
                    TargetId = hostile.Value,
                });
                return commands;
            }

            if (world.Territories.Count > 0)
            {
                var territory = world.Territories[0];
                bool ours = territory.HasController && territory.Controller == Player;
                if (!ours)
                {
                    commands.Add(new CaptureTerritoryCommand
                    {
                        Issuer = Player,
                        TerritoryNodeId = territory.Id,
                    });
                    return commands;
                }
            }

            // Idle near keep.
            commands.Add(new MoveCommand
            {
                Issuer = Player,
                UnitIds = myUnits.ToArray(),
                TargetX = keepX - 10f,
                TargetZ = keepZ,
            });
            return commands;
        }
    }
}
