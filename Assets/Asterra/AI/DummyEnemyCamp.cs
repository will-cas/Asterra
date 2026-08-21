using System;
using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.AI
{
    /// <summary>
    /// Offline skirmish opponent: macro phases, worker management, production, defence, and assault waves.
    /// Difficulty knobs come from <see cref="AiDifficultyTuning"/>.
    /// </summary>
    public sealed class DummyEnemyCamp : IArmyBrain
    {
        // Conservative afford estimates (AI asm cannot read Gameplay defs).
        private const int ApproxBuilderGold = 35;
        private const int ApproxCombatGold = 65;
        private const int ApproxCavalryGold = 120;
        private const int ApproxProducerGold = 160;
        private const int ApproxProducerTimber = 80;
        private const int ApproxTowerGold = 130;
        private const int ApproxTowerTimber = 60;
        private const int ApproxOutpostGold = 150;
        private const int ApproxOutpostTimber = 70;
        private const int ApproxWallGold = 45;
        private const int ApproxWallTimber = 40;
        private const int ApproxUpgradeGold = 180;
        private const int ApproxPowerUnlockGold = 150;
        private const float BuilderPlaceRadius = 50f;
        private readonly string _keepDefId;
        private readonly string _producerDefId;
        private readonly string _builderDefId;
        private readonly string _infantryDefId;
        private readonly string _rangedDefId;
        private readonly string _cavalryDefId;
        private readonly string _towerDefId;
        private readonly string _outpostDefId;
        private readonly string _wallDefId;
        private readonly string _basicUpgradeId;
        private readonly string _powerDefId;
        private readonly AiDifficultyTuning _tuning;

        private int _lastTrainTick = -999;
        private int _lastBuildTick = -999;
        private int _lastGatherTick = -999;
        private int _lastRallyTick = -999;
        private int _lastCombatOrderTick = -999;
        private int _lastDefendTick = -999;
        private int _lastCheatTick = -999;
        private int _lastTechTick = -999;
        private int _lastBuilderAssistTick = -999;
        private int _combatCycle;
        private int _wallCycle;
        private int _placeRetry;
        private bool _powerUnlocked;
        private bool _upgradeStarted;

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
                null,
                null,
                null,
                null,
                null,
                AiDifficulty.Normal)
        {
            _ = trainEveryTicks;
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
            : this(
                player,
                keepDefId,
                producerDefId,
                builderDefId,
                infantryDefId,
                rangedDefId,
                cavalryDefId,
                null,
                null,
                null,
                null,
                null,
                AiDifficulty.Normal)
        {
            _ = trainEveryTicks;
        }

        public DummyEnemyCamp(
            PlayerId player,
            string keepDefId,
            string producerDefId,
            string builderDefId,
            string infantryDefId,
            string rangedDefId,
            string cavalryDefId,
            string towerDefId,
            string outpostDefId,
            string wallDefId,
            int trainEveryTicks = 45)
            : this(
                player,
                keepDefId,
                producerDefId,
                builderDefId,
                infantryDefId,
                rangedDefId,
                cavalryDefId,
                towerDefId,
                outpostDefId,
                wallDefId,
                null,
                null,
                AiDifficulty.Normal)
        {
            _ = trainEveryTicks;
        }

        public DummyEnemyCamp(
            PlayerId player,
            string keepDefId,
            string producerDefId,
            string builderDefId,
            string infantryDefId,
            string rangedDefId,
            string cavalryDefId,
            string towerDefId,
            string outpostDefId,
            string wallDefId,
            AiDifficulty difficulty)
            : this(
                player,
                keepDefId,
                producerDefId,
                builderDefId,
                infantryDefId,
                rangedDefId,
                cavalryDefId,
                towerDefId,
                outpostDefId,
                wallDefId,
                null,
                null,
                difficulty)
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
            string towerDefId,
            string outpostDefId,
            string wallDefId,
            string basicUpgradeId,
            string powerDefId,
            AiDifficulty difficulty)
        {
            Player = player;
            Difficulty = difficulty;
            _tuning = AiDifficultyTuning.For(difficulty);
            _keepDefId = keepDefId;
            _producerDefId = producerDefId;
            _builderDefId = builderDefId;
            _infantryDefId = infantryDefId;
            _rangedDefId = string.IsNullOrEmpty(rangedDefId) ? infantryDefId : rangedDefId;
            _cavalryDefId = string.IsNullOrEmpty(cavalryDefId) ? infantryDefId : cavalryDefId;
            _towerDefId = towerDefId;
            _outpostDefId = outpostDefId;
            _wallDefId = wallDefId;
            _basicUpgradeId = basicUpgradeId;
            _powerDefId = powerDefId;
            CurrentPhase = "Opening";
            LastDecision = "boot";
        }

        public PlayerId Player { get; }
        public AiDifficulty Difficulty { get; }
        public string CurrentPhase { get; private set; }
        public string LastDecision { get; private set; }

        public IReadOnlyList<GameCommand> Think(in ArmyBrainContext context)
        {
            var commands = new List<GameCommand>(16);
            var world = context.World;
            var wallet = context.Wallet;
            var tick = (int)context.Tick.Value;

            ApplyIncomeCheat(wallet, tick);

            var sense = Perceive(world);
            CurrentPhase = ResolvePhase(in sense);
            LastDecision = "idle";

            IssueBuilderAssists(commands, in sense, tick);
            IssueGather(commands, world, in sense, tick);
            IssueRally(commands, world, in sense, tick);
            IssueTech(commands, in sense, wallet, world, tick);
            IssueMacro(commands, world, in sense, wallet, tick);

            if (sense.Combat.Count == 0)
                return commands;

            if (sense.UnderAttack)
            {
                IssueDefend(commands, world, in sense, tick);
                return commands;
            }

            IssueMilitary(commands, world, in sense, tick);
            return commands;
        }

        private void ApplyIncomeCheat(IResourceWallet wallet, int tick)
        {
            if (!_tuning.UseGoldCheat || wallet == null || _tuning.CheatIntervalTicks <= 0)
                return;
            if (tick - _lastCheatTick < _tuning.CheatIntervalTicks)
                return;
            wallet.Add(Player, ResourceType.Gold, _tuning.GoldCheatAmount);
            _lastCheatTick = tick;
        }

        private Perception Perceive(IWorldQuery world)
        {
            var p = new Perception();
            p.Builders = new List<SimEntityId>(4);
            p.IdleBuilders = new List<SimEntityId>(4);
            p.Combat = new List<SimEntityId>(16);
            p.Constructing = new List<(SimEntityId id, float x, float z)>(4);

            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner != Player || b.State == BuildingState.Destroyed)
                    continue;

                if (b.State == BuildingState.Constructing || b.State == BuildingState.Ghost)
                    p.Constructing.Add((b.Id, b.X, b.Z));

                if (b.DefinitionId == _keepDefId)
                {
                    p.KeepX = b.X;
                    p.KeepZ = b.Z;
                    p.KeepHealthRatio = b.MaxHealth > 0.01f ? b.Health / b.MaxHealth : 1f;
                    p.HasKeep = true;
                    if (b.CanProduce)
                        p.KeepId = b.Id;
                    if (b.AttachmentSlotCount > 0)
                    {
                        p.KeepAttachSlots = b.AttachmentSlotCount;
                        p.KeepAttachMask = b.AttachmentOccupiedMask;
                    }
                }
                else if (b.DefinitionId == _producerDefId)
                {
                    p.ProducerCount++;
                    if (b.CanProduce && b.State == BuildingState.Active)
                        p.ProducerId = b.Id;
                }
                else if (!string.IsNullOrEmpty(_towerDefId) && b.DefinitionId == _towerDefId)
                {
                    p.TowerCount++;
                }
                else if (!string.IsNullOrEmpty(_outpostDefId) && b.DefinitionId == _outpostDefId)
                {
                    p.OutpostCount++;
                }
                else if (!string.IsNullOrEmpty(_wallDefId) && b.DefinitionId == _wallDefId)
                {
                    p.WallCount++;
                }
            }

            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (u.Owner != Player || !u.IsAlive || u.IsGarrisoned)
                    continue;
                if (u.DefinitionId == _builderDefId)
                {
                    p.Builders.Add(u.Id);
                    if (u.IsIdle)
                        p.IdleBuilders.Add(u.Id);
                }
                else
                {
                    p.Combat.Add(u.Id);
                }
            }

            p.UnderAttack = p.KeepHealthRatio < 0.88f
                             || TryFindNearestEnemyThreat(
                                 world,
                                 p.KeepX,
                                 p.KeepZ,
                                 _tuning.DefendThreatRadius,
                                 out _,
                                 out _,
                                 out _);

            if (!string.IsNullOrEmpty(_basicUpgradeId) && world.HasUpgrade(Player, _basicUpgradeId))
                _upgradeStarted = true;
            if (!string.IsNullOrEmpty(_powerDefId) && world.HasPower(Player, _powerDefId))
                _powerUnlocked = true;

            return p;
        }

        private string ResolvePhase(in Perception sense)
        {
            if (sense.UnderAttack)
                return "Defend";
            if (!sense.HasKeep)
                return "Opening";
            if (sense.Builders.Count < Math.Max(1, _tuning.TargetBuilders / 2) || !sense.ProducerId.HasValue)
                return "Opening";
            if (sense.ProducerId.HasValue
                && (sense.TowerCount < _tuning.TargetTowers
                    || sense.OutpostCount < _tuning.TargetOutposts
                    || sense.Builders.Count < _tuning.TargetBuilders))
                return "EcoExpand";
            if (_tuning.PreferTech
                && ((!_upgradeStarted && !string.IsNullOrEmpty(_basicUpgradeId))
                    || (!_powerUnlocked && !string.IsNullOrEmpty(_powerDefId))))
                return "Tech";
            if (sense.Combat.Count < _tuning.AssaultArmySize)
                return "Mass";
            return "Attack";
        }

        private void IssueBuilderAssists(List<GameCommand> commands, in Perception sense, int tick)
        {
            if (sense.Constructing.Count == 0 || sense.Builders.Count == 0)
                return;
            if (tick - _lastBuilderAssistTick < Math.Max(18, _tuning.GatherIntervalTicks / 2))
                return;

            var site = sense.Constructing[0];
            var movers = sense.IdleBuilders.Count > 0 ? sense.IdleBuilders : sense.Builders;
            int take = Math.Min(movers.Count, Math.Max(1, movers.Count / 2));
            if (take <= 0)
                return;

            var ids = new SimEntityId[take];
            for (int i = 0; i < take; i++)
                ids[i] = movers[i];

            commands.Add(new MoveCommand
            {
                Issuer = Player,
                UnitIds = ids,
                TargetX = site.x,
                TargetZ = site.z,
            });
            _lastBuilderAssistTick = tick;
            LastDecision = "assist_construct";
        }

        private void IssueGather(List<GameCommand> commands, IWorldQuery world, in Perception sense, int tick)
        {
            if (sense.IdleBuilders.Count == 0 || tick - _lastGatherTick < _tuning.GatherIntervalTicks)
                return;
            if (sense.Constructing.Count > 0 && sense.IdleBuilders.Count <= 1)
                return;
            if (!TryFindNearestResource(world, sense.KeepX, sense.KeepZ, out var nodeId))
                return;

            commands.Add(new GatherCommand
            {
                Issuer = Player,
                UnitIds = sense.IdleBuilders.ToArray(),
                ResourceNodeId = nodeId,
            });
            _lastGatherTick = tick;
            if (LastDecision == "idle")
                LastDecision = "gather";
        }

        private void IssueRally(List<GameCommand> commands, IWorldQuery world, in Perception sense, int tick)
        {
            if (tick - _lastRallyTick < _tuning.RallyIntervalTicks)
                return;

            float rallyX;
            float rallyZ;
            if (TryFindEnemyKeep(world, out _, out float ekx, out float ekz))
            {
                float t = 0.35f + _tuning.Aggression * 0.35f;
                rallyX = sense.KeepX + (ekx - sense.KeepX) * t;
                rallyZ = sense.KeepZ + (ekz - sense.KeepZ) * t;
            }
            else
            {
                rallyX = sense.KeepX > 0f ? sense.KeepX - 40f : sense.KeepX + 40f;
                rallyZ = sense.KeepZ;
            }

            if (sense.ProducerId.HasValue)
            {
                commands.Add(new SetRallyCommand
                {
                    Issuer = Player,
                    BuildingId = sense.ProducerId.Value,
                    TargetX = rallyX,
                    TargetZ = rallyZ,
                });
                _lastRallyTick = tick;
            }
            else if (sense.KeepId.HasValue)
            {
                commands.Add(new SetRallyCommand
                {
                    Issuer = Player,
                    BuildingId = sense.KeepId.Value,
                    TargetX = rallyX,
                    TargetZ = rallyZ,
                });
                _lastRallyTick = tick;
            }
        }

        private void IssueTech(
            List<GameCommand> commands,
            in Perception sense,
            IResourceWallet wallet,
            IWorldQuery world,
            int tick)
        {
            if (!_tuning.PreferTech || tick - _lastTechTick < 80)
                return;
            if (!sense.KeepId.HasValue && !sense.ProducerId.HasValue)
                return;

            if (!_powerUnlocked
                && !string.IsNullOrEmpty(_powerDefId)
                && CanAfford(wallet, ApproxPowerUnlockGold, 0))
            {
                if (!world.HasPower(Player, _powerDefId))
                {
                    commands.Add(new UnlockPowerCommand
                    {
                        Issuer = Player,
                        PowerDefId = _powerDefId,
                    });
                    LastDecision = "unlock_power";
                    _lastTechTick = tick;
                    return;
                }

                _powerUnlocked = true;
            }

            if (_powerUnlocked
                && !string.IsNullOrEmpty(_powerDefId)
                && sense.UnderAttack
                && tick - _lastTechTick >= 40)
            {
                commands.Add(new ActivateCommanderAbilityCommand
                {
                    Issuer = Player,
                    PowerDefId = _powerDefId,
                });
                LastDecision = "activate_power";
                _lastTechTick = tick;
                return;
            }

            if (!_upgradeStarted
                && !string.IsNullOrEmpty(_basicUpgradeId)
                && CanAfford(wallet, ApproxUpgradeGold, 0)
                && !world.HasUpgrade(Player, _basicUpgradeId))
            {
                var buildingId = sense.ProducerId ?? sense.KeepId;
                if (buildingId.HasValue)
                {
                    commands.Add(new ChooseUpgradeCommand
                    {
                        Issuer = Player,
                        BuildingId = buildingId.Value,
                        UpgradeDefId = _basicUpgradeId,
                    });
                    _upgradeStarted = true;
                    LastDecision = "research_upgrade";
                    _lastTechTick = tick;
                }
            }
        }

        private void IssueMacro(
            List<GameCommand> commands,
            IWorldQuery world,
            in Perception sense,
            IResourceWallet wallet,
            int tick)
        {
            bool canTrain = tick - _lastTrainTick >= _tuning.TrainIntervalTicks;
            bool canBuild = tick - _lastBuildTick >= _tuning.BuildIntervalTicks;

            // 1) Builders to target count.
            if (sense.KeepId.HasValue
                && sense.Builders.Count < _tuning.TargetBuilders
                && canTrain
                && CanAfford(wallet, ApproxBuilderGold, 0))
            {
                commands.Add(new TrainUnitCommand
                {
                    Issuer = Player,
                    BuildingId = sense.KeepId.Value,
                    UnitDefId = _builderDefId,
                });
                _lastTrainTick = tick;
                LastDecision = "train_builder";
                return;
            }

            // 2) Barracks / producer.
            if (!sense.ProducerId.HasValue
                && sense.ProducerCount == 0
                && sense.Builders.Count > 0
                && canBuild
                && CanAfford(wallet, ApproxProducerGold, ApproxProducerTimber))
            {
                if (TryPlaceNearKeep(
                        commands,
                        world,
                        in sense,
                        _producerDefId,
                        GetPlaceOffset(sense, 0),
                        tick))
                {
                    LastDecision = "place_producer";
                    return;
                }
            }

            // 3) Towers.
            if (sense.ProducerId.HasValue
                && !string.IsNullOrEmpty(_towerDefId)
                && sense.TowerCount < _tuning.TargetTowers
                && sense.Builders.Count > 0
                && canBuild
                && CanAfford(wallet, ApproxTowerGold, ApproxTowerTimber))
            {
                // Prefer attach slot on keep when available.
                if (sense.KeepId.HasValue
                    && sense.KeepAttachSlots > 0
                    && sense.TowerCount < sense.KeepAttachSlots)
                {
                    byte slot = FirstFreeSlot(sense.KeepAttachMask, sense.KeepAttachSlots);
                    commands.Add(new AttachBuildingCommand
                    {
                        Issuer = Player,
                        ParentBuildingId = sense.KeepId.Value,
                        SlotIndex = slot,
                        BuildingDefId = _towerDefId,
                    });
                    _lastBuildTick = tick;
                    LastDecision = "attach_tower";
                    return;
                }

                float side = (sense.TowerCount % 2 == 0) ? -1f : 1f;
                float dist = 26f + sense.TowerCount * 6f;
                if (TryPlaceNearKeep(
                        commands,
                        world,
                        in sense,
                        _towerDefId,
                        (side * dist, 16f + sense.TowerCount * 4f),
                        tick))
                {
                    LastDecision = "place_tower";
                    return;
                }
            }

            // 4) Outpost.
            if (sense.ProducerId.HasValue
                && !string.IsNullOrEmpty(_outpostDefId)
                && sense.OutpostCount < _tuning.TargetOutposts
                && sense.Builders.Count > 0
                && canBuild
                && CanAfford(wallet, ApproxOutpostGold, ApproxOutpostTimber))
            {
                float ox = sense.KeepX > 0f ? -55f : 55f;
                if (TryPlaceNearKeep(commands, world, in sense, _outpostDefId, (ox, 0f), tick))
                {
                    LastDecision = "place_outpost";
                    return;
                }
            }

            // 5) Walls (Hard+ / Insane primarily via TargetWalls).
            if (sense.ProducerId.HasValue
                && !string.IsNullOrEmpty(_wallDefId)
                && sense.WallCount < _tuning.TargetWalls
                && sense.Builders.Count > 0
                && canBuild
                && CanAfford(wallet, ApproxWallGold, ApproxWallTimber))
            {
                float wx = sense.KeepX > 0f ? -42f : 42f;
                float wz = (_wallCycle % 2 == 0 ? -14f : 14f) + (_wallCycle / 2) * 10f;
                float yaw = MathF.Abs(sense.KeepX) > MathF.Abs(sense.KeepZ) ? 90f : 0f;
                if (TryPlaceNearKeep(
                        commands,
                        world,
                        in sense,
                        _wallDefId,
                        (wx, wz),
                        tick,
                        yaw))
                {
                    _wallCycle++;
                    LastDecision = "place_wall";
                    return;
                }
            }

            // 6) Combat training.
            if (sense.ProducerId.HasValue
                && canTrain
                && CanAfford(wallet, ApproxCombatGold, 0))
            {
                string unitId = NextCombatUnitId(sense.UnderAttack, wallet);
                int cost = unitId == _cavalryDefId ? ApproxCavalryGold : ApproxCombatGold;
                if (!CanAfford(wallet, cost, 0))
                    unitId = _infantryDefId;

                commands.Add(new TrainUnitCommand
                {
                    Issuer = Player,
                    BuildingId = sense.ProducerId.Value,
                    UnitDefId = unitId,
                });
                _lastTrainTick = tick;
                LastDecision = "train_combat";
            }
        }

        private bool TryPlaceNearKeep(
            List<GameCommand> commands,
            IWorldQuery world,
            in Perception sense,
            string buildingDefId,
            (float dx, float dz) offset,
            int tick,
            float yaw = 0f)
        {
            float baseX = sense.KeepX + offset.dx;
            float baseZ = sense.KeepZ + offset.dz;
            // Retry ring offsets so placements are less sticky on blocked cells.
            float angle = (_placeRetry % 8) * (MathF.PI * 0.25f);
            float ring = (_placeRetry / 8) * 8f;
            float x = baseX + MathF.Cos(angle) * ring;
            float z = baseZ + MathF.Sin(angle) * ring;
            _placeRetry++;

            if (!BuildersNear(world, x, z, BuilderPlaceRadius))
            {
                MoveBuildersToward(commands, in sense, x, z);
                LastDecision = "move_builders_to_site";
                // Still attempt place; sim rejects if too far — next tick builders will be closer.
            }

            commands.Add(new PlaceBuildingCommand
            {
                Issuer = Player,
                BuildingDefId = buildingDefId,
                X = x,
                Z = z,
                YawDegrees = yaw,
            });
            _lastBuildTick = tick;
            return true;
        }

        private void MoveBuildersToward(List<GameCommand> commands, in Perception sense, float x, float z)
        {
            var movers = sense.IdleBuilders.Count > 0 ? sense.IdleBuilders : sense.Builders;
            if (movers.Count == 0)
                return;
            int take = Math.Min(movers.Count, 2);
            var ids = new SimEntityId[take];
            for (int i = 0; i < take; i++)
                ids[i] = movers[i];
            commands.Add(new MoveCommand
            {
                Issuer = Player,
                UnitIds = ids,
                TargetX = x,
                TargetZ = z,
            });
        }

        private bool BuildersNear(IWorldQuery world, float x, float z, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (u.Owner != Player || !u.IsAlive || u.DefinitionId != _builderDefId)
                    continue;
                float dx = u.X - x;
                float dz = u.Z - z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }

        private static (float dx, float dz) GetPlaceOffset(in Perception sense, int index)
        {
            float side = sense.KeepX > 0f ? -1f : 1f;
            switch (index % 3)
            {
                case 1: return (side * 28f, -22f);
                case 2: return (side * 42f, 12f);
                default: return (side * 35f, 25f);
            }
        }

        private static byte FirstFreeSlot(byte occupiedMask, int slotCount)
        {
            for (byte i = 0; i < slotCount && i < 8; i++)
            {
                if ((occupiedMask & (1 << i)) == 0)
                    return i;
            }

            return 0;
        }

        private void IssueDefend(List<GameCommand> commands, IWorldQuery world, in Perception sense, int tick)
        {
            if (tick - _lastDefendTick < _tuning.DefendIntervalTicks)
                return;

            if (TryFindNearestEnemyThreat(
                    world,
                    sense.KeepX,
                    sense.KeepZ,
                    _tuning.DefendThreatRadius + 30f,
                    out var threatId,
                    out _,
                    out _))
            {
                commands.Add(new AttackCommand
                {
                    Issuer = Player,
                    UnitIds = sense.Combat.ToArray(),
                    TargetId = threatId,
                });
            }
            else
            {
                commands.Add(new AttackMoveCommand
                {
                    Issuer = Player,
                    UnitIds = sense.Combat.ToArray(),
                    TargetX = sense.KeepX,
                    TargetZ = sense.KeepZ,
                });
            }

            if (_powerUnlocked && !string.IsNullOrEmpty(_powerDefId))
            {
                commands.Add(new ActivateCommanderAbilityCommand
                {
                    Issuer = Player,
                    PowerDefId = _powerDefId,
                });
            }

            _lastDefendTick = tick;
            LastDecision = "defend";
        }

        private void IssueMilitary(List<GameCommand> commands, IWorldQuery world, in Perception sense, int tick)
        {
            if (tick - _lastCombatOrderTick < _tuning.CombatOrderIntervalTicks)
                return;

            SplitForces(sense.Combat, out var guard, out var wave);

            int assaultNeed = _tuning.AssaultArmySize;
            if (sense.UnderAttack)
                assaultNeed = Math.Max(2, assaultNeed - 2);

            // Contest territory while massing.
            if (wave.Length + guard.Length >= 2
                && sense.Combat.Count < assaultNeed
                && world.Territories != null
                && world.Territories.Count > 0)
            {
                for (int i = 0; i < world.Territories.Count; i++)
                {
                    var territory = world.Territories[i];
                    if (territory.HasController && territory.Controller == Player)
                        continue;
                    commands.Add(new CaptureTerritoryCommand
                    {
                        Issuer = Player,
                        TerritoryNodeId = territory.Id,
                    });
                    var movers = wave.Length > 0 ? wave : sense.Combat.ToArray();
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = movers,
                        TargetX = territory.X,
                        TargetZ = territory.Z,
                    });
                    if (guard.Length > 0)
                    {
                        commands.Add(new AttackMoveCommand
                        {
                            Issuer = Player,
                            UnitIds = guard,
                            TargetX = sense.KeepX,
                            TargetZ = sense.KeepZ,
                        });
                    }

                    _lastCombatOrderTick = tick;
                    LastDecision = "contest_territory";
                    return;
                }
            }

            if (sense.Combat.Count < assaultNeed)
            {
                // Hold a forward picket with the wave; guard stays home.
                float holdX = sense.KeepX > 0f ? sense.KeepX - 40f : sense.KeepX + 40f;
                var picket = wave.Length > 0 ? wave : sense.Combat.ToArray();
                commands.Add(new AttackMoveCommand
                {
                    Issuer = Player,
                    UnitIds = picket,
                    TargetX = holdX,
                    TargetZ = sense.KeepZ,
                });
                if (guard.Length > 0)
                {
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = guard,
                        TargetX = sense.KeepX,
                        TargetZ = sense.KeepZ,
                    });
                }

                _lastCombatOrderTick = tick;
                LastDecision = "hold_picket";
                return;
            }

            // Assault wave.
            var attackers = wave.Length > 0 ? wave : sense.Combat.ToArray();
            if (TryFindEnemyKeep(world, out var enemyKeep, out float ekx, out float ekz))
            {
                if (sense.Combat.Count >= assaultNeed + 2 || _tuning.Aggression >= 0.8f)
                {
                    commands.Add(new AttackCommand
                    {
                        Issuer = Player,
                        UnitIds = attackers,
                        TargetId = enemyKeep,
                    });
                    LastDecision = "assault_keep";
                }
                else
                {
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = attackers,
                        TargetX = ekx,
                        TargetZ = ekz,
                    });
                    LastDecision = "attack_move_keep";
                }

                if (guard.Length > 0)
                {
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = guard,
                        TargetX = sense.KeepX,
                        TargetZ = sense.KeepZ,
                    });
                }

                _lastCombatOrderTick = tick;
                return;
            }

            if (TryFindNearestEnemyThreat(world, sense.KeepX, sense.KeepZ, 400f, out var threatId, out float tx, out float tz))
            {
                commands.Add(new AttackMoveCommand
                {
                    Issuer = Player,
                    UnitIds = attackers,
                    TargetX = tx,
                    TargetZ = tz,
                });
                _lastCombatOrderTick = tick;
                LastDecision = "hunt_threat";
            }
        }

        private void SplitForces(List<SimEntityId> combat, out SimEntityId[] guard, out SimEntityId[] wave)
        {
            int guardNeed = Math.Min(_tuning.HomeGuardSize, Math.Max(0, combat.Count - 1));
            if (combat.Count <= guardNeed)
            {
                guard = combat.ToArray();
                wave = Array.Empty<SimEntityId>();
                return;
            }

            guard = new SimEntityId[guardNeed];
            wave = new SimEntityId[combat.Count - guardNeed];
            for (int i = 0; i < guardNeed; i++)
                guard[i] = combat[i];
            for (int i = 0; i < wave.Length; i++)
                wave[i] = combat[guardNeed + i];
        }

        private string NextCombatUnitId(bool underAttack, IResourceWallet wallet)
        {
            string id;
            if (underAttack)
            {
                id = _combatCycle % 2 == 0 ? _rangedDefId : _infantryDefId;
            }
            else
            {
                switch (_combatCycle % 4)
                {
                    case 0:
                        id = _infantryDefId;
                        break;
                    case 1:
                        id = _rangedDefId;
                        break;
                    case 2:
                        id = CanAfford(wallet, ApproxCavalryGold, 0) ? _cavalryDefId : _infantryDefId;
                        break;
                    default:
                        id = _infantryDefId;
                        break;
                }
            }

            _combatCycle++;
            return id;
        }

        private bool CanAfford(IResourceWallet wallet, int gold, int timber)
        {
            if (wallet == null)
                return true;
            if (gold > 0 && !wallet.CanAfford(Player, ResourceType.Gold, gold))
                return false;
            if (timber > 0 && !wallet.CanAfford(Player, ResourceType.Timber, timber))
                return false;
            return true;
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

        private bool TryFindEnemyKeep(IWorldQuery world, out SimEntityId keepId, out float x, out float z)
        {
            keepId = default;
            x = 0f;
            z = 0f;
            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner == Player || b.State == BuildingState.Destroyed)
                    continue;
                if (!LooksLikeKeep(b.DefinitionId))
                    continue;
                keepId = b.Id;
                x = b.X;
                z = b.Z;
                return true;
            }

            return false;
        }

        private bool TryFindNearestEnemyThreat(
            IWorldQuery world,
            float fromX,
            float fromZ,
            float maxDist,
            out SimEntityId threatId,
            out float x,
            out float z)
        {
            threatId = default;
            x = 0f;
            z = 0f;
            float best = maxDist * maxDist;
            bool found = false;

            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (!u.IsAlive || u.Owner == Player)
                    continue;
                float dx = u.X - fromX;
                float dz = u.Z - fromZ;
                float d2 = dx * dx + dz * dz;
                if (d2 >= best)
                    continue;
                best = d2;
                threatId = u.Id;
                x = u.X;
                z = u.Z;
                found = true;
            }

            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner == Player || b.State == BuildingState.Destroyed)
                    continue;
                float dx = b.X - fromX;
                float dz = b.Z - fromZ;
                float d2 = dx * dx + dz * dz;
                if (d2 >= best)
                    continue;
                if (!LooksLikeKeep(b.DefinitionId) && d2 > 80f * 80f)
                    continue;
                best = d2;
                threatId = b.Id;
                x = b.X;
                z = b.Z;
                found = true;
            }

            return found;
        }

        private static bool LooksLikeKeep(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;
            return definitionId.Contains("keep")
                   || definitionId.Contains("heartwood")
                   || definitionId.Contains("citadel");
        }

        private struct Perception
        {
            public bool HasKeep;
            public SimEntityId? KeepId;
            public SimEntityId? ProducerId;
            public float KeepX;
            public float KeepZ;
            public float KeepHealthRatio;
            public int KeepAttachSlots;
            public byte KeepAttachMask;
            public int ProducerCount;
            public int TowerCount;
            public int OutpostCount;
            public int WallCount;
            public bool UnderAttack;
            public List<SimEntityId> Builders;
            public List<SimEntityId> IdleBuilders;
            public List<SimEntityId> Combat;
            public List<(SimEntityId id, float x, float z)> Constructing;
        }
    }
}
