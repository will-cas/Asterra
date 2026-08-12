using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.AI
{
    /// <summary>
    /// Scripted opponent: gather, train builders/producer/combat mix, rally center, attack in force.
    /// </summary>
    public sealed class DummyEnemyCamp : IArmyBrain
    {
        private readonly string _keepDefId;
        private readonly string _producerDefId;
        private readonly string _builderDefId;
        private readonly string _infantryDefId;
        private readonly string _rangedDefId;
        private readonly string _cavalryDefId;
        private readonly int _trainEveryTicks;
        private int _lastTrainTick = -999;
        private int _lastBuildTick = -999;
        private int _lastGatherTick = -999;
        private int _lastRallyTick = -999;
        private int _combatCycle;

        public DummyEnemyCamp(
            PlayerId player,
            string keepDefId,
            string producerDefId,
            string builderDefId,
            string combatUnitDefId,
            int trainEveryTicks = 55)
            : this(
                player,
                keepDefId,
                producerDefId,
                builderDefId,
                combatUnitDefId,
                combatUnitDefId,
                combatUnitDefId,
                trainEveryTicks)
        {
        }

        public DummyEnemyCamp(
            PlayerId player,
            string keepDefId,
            string producerDefId,
            string builderDefId,
            string infantryDefId,
            string rangedDefId,
            string cavalryDefId,
            int trainEveryTicks = 55)
        {
            Player = player;
            _keepDefId = keepDefId;
            _producerDefId = producerDefId;
            _builderDefId = builderDefId;
            _infantryDefId = infantryDefId;
            _rangedDefId = string.IsNullOrEmpty(rangedDefId) ? infantryDefId : rangedDefId;
            _cavalryDefId = string.IsNullOrEmpty(cavalryDefId) ? infantryDefId : cavalryDefId;
            _trainEveryTicks = trainEveryTicks;
        }

        public PlayerId Player { get; }

        public IReadOnlyList<GameCommand> Think(in ArmyBrainContext context)
        {
            var commands = new List<GameCommand>(8);
            var world = context.World;
            var tick = (int)context.Tick.Value;

            SimEntityId? keepId = null;
            float keepX = 0f;
            float keepZ = 0f;
            SimEntityId? producerId = null;
            int builderCount = 0;
            int combatCount = 0;
            var builders = new List<SimEntityId>();
            var myCombat = new List<SimEntityId>();

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
                {
                    builderCount++;
                    builders.Add(u.Id);
                }
                else
                {
                    combatCount++;
                    myCombat.Add(u.Id);
                }
            }

            // Builders gather nearest resource (never send into combat).
            if (builders.Count > 0 && tick - _lastGatherTick >= 30)
            {
                if (TryFindNearestResource(world, keepX, keepZ, out var nodeId))
                {
                    commands.Add(new GatherCommand
                    {
                        Issuer = Player,
                        UnitIds = builders.ToArray(),
                        ResourceNodeId = nodeId,
                    });
                    _lastGatherTick = tick;
                }
            }

            // Rally producer / keep toward center.
            if (tick - _lastRallyTick >= 200)
            {
                if (producerId.HasValue)
                {
                    commands.Add(new SetRallyCommand
                    {
                        Issuer = Player,
                        BuildingId = producerId.Value,
                        TargetX = 0f,
                        TargetZ = 0f,
                    });
                    _lastRallyTick = tick;
                }
                else if (keepId.HasValue)
                {
                    commands.Add(new SetRallyCommand
                    {
                        Issuer = Player,
                        BuildingId = keepId.Value,
                        TargetX = keepX * 0.5f,
                        TargetZ = 0f,
                    });
                    _lastRallyTick = tick;
                }
            }

            // Train builders, then raise producer, then cycle combat mix.
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
            else if (!producerId.HasValue && builderCount > 0 && tick - _lastBuildTick >= 90)
            {
                float placeX = keepX > 0f ? keepX - 35f : keepX + 35f;
                commands.Add(new PlaceBuildingCommand
                {
                    Issuer = Player,
                    BuildingDefId = _producerDefId,
                    X = placeX,
                    Z = keepZ + 25f,
                    YawDegrees = 0f,
                });
                _lastBuildTick = tick;
            }
            else if (producerId.HasValue && tick - _lastTrainTick >= _trainEveryTicks)
            {
                string unitId = NextCombatUnitId();
                commands.Add(new TrainUnitCommand
                {
                    Issuer = Player,
                    BuildingId = producerId.Value,
                    UnitDefId = unitId,
                });
                _lastTrainTick = tick;
            }

            if (myCombat.Count == 0)
                return commands;

            // Attack when combat force is ready; otherwise hold / contest center.
            if (combatCount >= 3)
            {
                if (TryFindEnemyKeep(world, out var enemyKeep))
                {
                    commands.Add(new AttackCommand
                    {
                        Issuer = Player,
                        UnitIds = myCombat.ToArray(),
                        TargetId = enemyKeep,
                    });
                    return commands;
                }

                commands.Add(new MoveCommand
                {
                    Issuer = Player,
                    UnitIds = myCombat.ToArray(),
                    TargetX = 0f,
                    TargetZ = 0f,
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

            float holdX = keepX > 0f ? keepX - 10f : keepX + 10f;
            commands.Add(new MoveCommand
            {
                Issuer = Player,
                UnitIds = myCombat.ToArray(),
                TargetX = holdX,
                TargetZ = keepZ,
            });
            return commands;
        }

        private string NextCombatUnitId()
        {
            string id;
            switch (_combatCycle % 3)
            {
                case 0:
                    id = _infantryDefId;
                    break;
                case 1:
                    id = _rangedDefId;
                    break;
                default:
                    id = _cavalryDefId;
                    break;
            }

            _combatCycle++;
            return id;
        }

        private static bool TryFindNearestResource(
            IWorldQuery world,
            float fromX,
            float fromZ,
            out SimEntityId nodeId)
        {
            nodeId = default;
            if (world.Resources == null || world.Resources.Count == 0)
                return false;

            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < world.Resources.Count; i++)
            {
                var r = world.Resources[i];
                if (r.Remaining <= 0)
                    continue;
                float dx = r.X - fromX;
                float dz = r.Z - fromZ;
                float d2 = dx * dx + dz * dz;
                if (d2 < best)
                {
                    best = d2;
                    nodeId = r.Id;
                    found = true;
                }
            }

            return found;
        }

        private bool TryFindEnemyKeep(IWorldQuery world, out SimEntityId keepId)
        {
            keepId = default;
            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner == Player || b.State == BuildingState.Destroyed)
                    continue;
                if (!LooksLikeKeep(b.DefinitionId))
                    continue;
                keepId = b.Id;
                return true;
            }

            return false;
        }

        private static bool LooksLikeKeep(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;
            return definitionId.Contains("keep")
                   || definitionId.Contains("heartwood")
                   || definitionId.Contains("citadel");
        }
    }
}
