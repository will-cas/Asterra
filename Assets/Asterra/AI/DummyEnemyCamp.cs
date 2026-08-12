using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.AI
{
    /// <summary>
    /// Scripted opponent: train builders from keep, raise a producer, train combat, contest center.
    /// </summary>
    public sealed class DummyEnemyCamp : IArmyBrain
    {
        private readonly string _keepDefId;
        private readonly string _producerDefId;
        private readonly string _builderDefId;
        private readonly string _combatUnitDefId;
        private readonly int _trainEveryTicks;
        private int _lastTrainTick = -999;
        private int _lastBuildTick = -999;

        public DummyEnemyCamp(
            PlayerId player,
            string keepDefId,
            string producerDefId,
            string builderDefId,
            string combatUnitDefId,
            int trainEveryTicks = 80)
        {
            Player = player;
            _keepDefId = keepDefId;
            _producerDefId = producerDefId;
            _builderDefId = builderDefId;
            _combatUnitDefId = combatUnitDefId;
            _trainEveryTicks = trainEveryTicks;
        }

        public PlayerId Player { get; }

        public IReadOnlyList<GameCommand> Think(in ArmyBrainContext context)
        {
            var commands = new List<GameCommand>(4);
            var world = context.World;
            var tick = (int)context.Tick.Value;

            SimEntityId? keepId = null;
            float keepX = 0f;
            float keepZ = 0f;
            SimEntityId? producerId = null;
            int builderCount = 0;
            int combatCount = 0;

            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner != Player)
                    continue;
                if (b.DefinitionId == _keepDefId && b.CanProduce)
                {
                    keepId = b.Id;
                    keepX = b.X;
                    keepZ = b.Z;
                }
                else if (b.DefinitionId == _producerDefId && b.CanProduce)
                {
                    producerId = b.Id;
                }
            }

            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (u.Owner != Player || !u.IsAlive)
                    continue;
                if (u.DefinitionId == _builderDefId)
                    builderCount++;
                else
                    combatCount++;
            }

            if (keepId.HasValue && builderCount < 2 && tick - _lastTrainTick >= _trainEveryTicks)
            {
                commands.Add(new TrainUnitCommand
                {
                    Issuer = Player,
                    BuildingId = keepId.Value,
                    UnitDefId = _builderDefId,
                });
                _lastTrainTick = tick;
            }
            else if (producerId.HasValue && tick - _lastTrainTick >= _trainEveryTicks)
            {
                commands.Add(new TrainUnitCommand
                {
                    Issuer = Player,
                    BuildingId = producerId.Value,
                    UnitDefId = _combatUnitDefId,
                });
                _lastTrainTick = tick;
            }
            else if (!producerId.HasValue && builderCount > 0 && tick - _lastBuildTick >= 120)
            {
                commands.Add(new PlaceBuildingCommand
                {
                    Issuer = Player,
                    BuildingDefId = _producerDefId,
                    X = keepX - 35f,
                    Z = keepZ + 25f,
                    YawDegrees = 0f,
                });
                _lastBuildTick = tick;
            }

            var myCombat = new List<SimEntityId>();
            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (u.Owner != Player || !u.IsAlive)
                    continue;
                if (u.DefinitionId == _builderDefId)
                    continue;
                myCombat.Add(u.Id);
            }

            if (myCombat.Count == 0)
                return commands;

            SimEntityId? hostile = null;
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
                    UnitIds = myCombat.ToArray(),
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

            commands.Add(new MoveCommand
            {
                Issuer = Player,
                UnitIds = myCombat.ToArray(),
                TargetX = keepX - 10f,
                TargetZ = keepZ,
            });
            return commands;
        }
    }
}
