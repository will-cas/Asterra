using System;
using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.AI
{
    /// <summary>
    /// Offline skirmish opponent: macro phases, worker management, production, defence, and assault waves.
    /// Difficulty knobs come from <see cref="AiDifficultyTuning"/>.
    /// </summary>
    public sealed class SkirmishOpponentBrain : IArmyBrain
    {
        // Conservative afford estimates (AI asm cannot read Gameplay defs).
        private const int ApproxBuilderGold = 35;
        private const int ApproxCombatGold = 65;
        private const int ApproxCavalryGold = 120;
        private const int ApproxProducerGold = 140;
        private const int ApproxProducerTimber = 120;
        private const int ApproxTowerGold = 90;
        private const int ApproxTowerTimber = 70;
        private const int ApproxKeepTurretGold = 70;
        private const int ApproxKeepTurretTimber = 50;
        private const int ApproxOutpostGold = 150;
        private const int ApproxOutpostTimber = 70;
        private const int ApproxWallGold = 45;
        private const int ApproxWallTimber = 90;
        private const int ApproxUpgradeGold = 180;
        private const int ApproxPowerUnlockGold = 150;
        private const float BuilderPlaceRadius = 50f;
        private const float LocalGoldRadius = 220f;
        private readonly string _keepDefId;
        private readonly string _producerDefId;
        private readonly string _builderDefId;
        private readonly string _infantryDefId;
        private readonly string _rangedDefId;
        private readonly string _cavalryDefId;
        private readonly string _towerDefId;
        private readonly string _keepTurretDefId;
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
        private int _lastScoutTick = -999;
        private int _matchStartTick = -1;
        private int _combatCycle;
        private int _wallCycle;
        private int _placeRetry;
        private bool _powerUnlocked;
        private bool _upgradeStarted;
        private bool _hasSightedEnemy;
        private bool _hasLastSeenKeep;
        private SimEntityId _lastSeenKeepId;
        private float _lastSeenKeepX;
        private float _lastSeenKeepZ;
        private int _lastSeenKeepTick;
        private bool _hasExpandGold;
        private SimEntityId _expandGoldId;
        private float _expandGoldX;
        private float _expandGoldZ;
        private const float ExpandGoldMinDist = 120f;
        private const int LastSeenKeepTimeoutTicks = 600;

        public SkirmishOpponentBrain(
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

        public SkirmishOpponentBrain(
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

        public SkirmishOpponentBrain(
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

        public SkirmishOpponentBrain(
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

        public SkirmishOpponentBrain(
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
            AiDifficulty difficulty,
            string keepTurretDefId = null)
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
            _keepTurretDefId = keepTurretDefId;
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
            if (_matchStartTick < 0)
                _matchStartTick = tick;

            ApplyIncomeCheat(wallet, tick);

            var sense = Perceive(world, tick);
            CurrentPhase = ResolvePhase(in sense);
            LastDecision = "idle";

            IssueBuilderAssists(commands, in sense, tick);
            IssueRally(commands, world, in sense, tick);
            IssueTech(commands, in sense, wallet, world, tick);
            // Macro before gather so workers are not yanked off foundations the same tick.
            IssueMacro(commands, world, in sense, wallet, tick);
            IssueGather(commands, world, in sense, tick);
            IssueScout(commands, world, in sense, tick);

            if (sense.Combat.Count == 0)
                return commands;

            if (sense.UnderAttack || CurrentPhase == "Defend")
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

        private Perception Perceive(IWorldQuery world, int tick)
        {
            var p = new Perception();
            p.Builders = new List<SimEntityId>(4);
            p.IdleBuilders = new List<SimEntityId>(4);
            p.Combat = new List<SimEntityId>(16);
            p.CombatDefIds = new List<string>(16);
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
                    if (b.State == BuildingState.Active)
                    {
                        p.ActiveProducerCount++;
                        if (b.CanProduce)
                            p.ProducerId = b.Id;
                    }
                }
                else if (!string.IsNullOrEmpty(_keepTurretDefId) && b.DefinitionId == _keepTurretDefId)
                {
                    p.KeepTurretCount++;
                    p.TowerCount++;
                }
                else if (!string.IsNullOrEmpty(_towerDefId) && b.DefinitionId == _towerDefId)
                {
                    p.FreeTowerCount++;
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
                    p.CombatDefIds.Add(u.DefinitionId ?? string.Empty);
                }
            }

            // FoW-honest: only react to visible enemies; remember last-seen keep.
            UpdateLastSeenKeep(world, tick);

            p.UnderAttack = p.KeepHealthRatio < 0.88f
                             || TryFindNearestEnemyThreat(
                                 world,
                                 p.KeepX,
                                 p.KeepZ,
                                 _tuning.DefendThreatRadius,
                                 out _,
                                 out _,
                                 out _);

            p.HasLastSeenKeep = _hasLastSeenKeep;
            p.LastSeenKeepId = _lastSeenKeepId;
            p.LastSeenKeepX = _lastSeenKeepX;
            p.LastSeenKeepZ = _lastSeenKeepZ;
            p.HasSightedEnemy = _hasSightedEnemy;
            p.CanAssaultKeep = CanAssaultKeep(tick);
            UpdateExpandGold(world, in p);
            p.HasExpandGold = _hasExpandGold;
            p.ExpandGoldId = _expandGoldId;
            p.ExpandGoldX = _expandGoldX;
            p.ExpandGoldZ = _expandGoldZ;
            p.GoldNodeCount = CountGoldNodesNear(world, p.KeepX, p.KeepZ, LocalGoldRadius);
            p.DesiredBuilders = DesiredBuilderCount(p.GoldNodeCount, p.ActiveProducerCount);

            if (!string.IsNullOrEmpty(_basicUpgradeId) && world.HasUpgrade(Player, _basicUpgradeId))
                _upgradeStarted = true;
            if (!string.IsNullOrEmpty(_powerDefId) && world.HasPower(Player, _powerDefId))
                _powerUnlocked = true;

            return p;
        }

        private int CountGoldNodes(IWorldQuery world)
        {
            return CountGoldNodesNear(world, 0f, 0f, float.MaxValue);
        }

        private int CountGoldNodesNear(IWorldQuery world, float cx, float cz, float radius)
        {
            if (world.Resources == null)
                return 0;
            float r2 = radius >= float.MaxValue * 0.5f ? float.MaxValue : radius * radius;
            int n = 0;
            for (int i = 0; i < world.Resources.Count; i++)
            {
                var r = world.Resources[i];
                if (r.Remaining <= 0 || r.Type != ResourceType.Gold)
                    continue;
                if (r2 < float.MaxValue)
                {
                    float dx = r.X - cx;
                    float dz = r.Z - cz;
                    if (dx * dx + dz * dz > r2)
                        continue;
                }

                n++;
            }

            return n;
        }

        private int DesiredBuilderCount(int goldNodes, int activeProducers)
        {
            // Opening: only TargetBuilders until a producer is live — avoid greed-blocking place.
            if (activeProducers <= 0)
                return Math.Max(1, _tuning.TargetBuilders);

            int fromNodes = Math.Max(0, goldNodes) * Math.Max(0, _tuning.TargetWorkersPerNode);
            int desired = _tuning.TargetBuilders + Math.Max(0, fromNodes - _tuning.TargetWorkersPerNode);
            if (desired < _tuning.TargetBuilders)
                desired = _tuning.TargetBuilders;
            return Math.Min(_tuning.MaxBuilders, desired);
        }

        private void UpdateExpandGold(IWorldQuery world, in Perception sense)
        {
            _hasExpandGold = false;
            if (world.Resources == null || !sense.HasKeep)
                return;

            float homeBest = float.MaxValue;
            float homeX = 0f, homeZ = 0f;
            SimEntityId homeId = default;
            float expandBest = float.MaxValue;

            for (int i = 0; i < world.Resources.Count; i++)
            {
                var r = world.Resources[i];
                if (r.Remaining <= 0 || r.Type != ResourceType.Gold)
                    continue;
                float dx = r.X - sense.KeepX;
                float dz = r.Z - sense.KeepZ;
                float d2 = dx * dx + dz * dz;
                if (d2 < homeBest)
                {
                    homeBest = d2;
                    homeId = r.Id;
                    homeX = r.X;
                    homeZ = r.Z;
                }
            }

            float minExpand = ExpandGoldMinDist * ExpandGoldMinDist;
            for (int i = 0; i < world.Resources.Count; i++)
            {
                var r = world.Resources[i];
                if (r.Remaining <= 0 || r.Type != ResourceType.Gold)
                    continue;
                if (r.Id.Value == homeId.Value)
                    continue;
                float dx = r.X - sense.KeepX;
                float dz = r.Z - sense.KeepZ;
                float d2 = dx * dx + dz * dz;
                if (d2 < minExpand)
                    continue;
                if (d2 < expandBest)
                {
                    expandBest = d2;
                    _expandGoldId = r.Id;
                    _expandGoldX = r.X;
                    _expandGoldZ = r.Z;
                    _hasExpandGold = true;
                }
            }

            // If nothing beyond threshold, take the second-nearest gold as expand.
            if (!_hasExpandGold && homeBest < float.MaxValue)
            {
                float second = float.MaxValue;
                for (int i = 0; i < world.Resources.Count; i++)
                {
                    var r = world.Resources[i];
                    if (r.Remaining <= 0 || r.Type != ResourceType.Gold)
                        continue;
                    if (r.Id.Value == homeId.Value)
                        continue;
                    float dx = r.X - sense.KeepX;
                    float dz = r.Z - sense.KeepZ;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < second)
                    {
                        second = d2;
                        _expandGoldId = r.Id;
                        _expandGoldX = r.X;
                        _expandGoldZ = r.Z;
                        _hasExpandGold = true;
                    }
                }
            }

            _ = homeX;
            _ = homeZ;
        }

        private void UpdateLastSeenKeep(IWorldQuery world, int tick)
        {
            bool sawKeep = false;
            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner == Player || b.State == BuildingState.Destroyed)
                    continue;
                if (!LooksLikeKeep(b.DefinitionId))
                    continue;
                if (!world.IsVisibleTo(Player, b.X, b.Z))
                    continue;
                _hasLastSeenKeep = true;
                _lastSeenKeepId = b.Id;
                _lastSeenKeepX = b.X;
                _lastSeenKeepZ = b.Z;
                _lastSeenKeepTick = tick;
                _hasSightedEnemy = true;
                sawKeep = true;
                break;
            }

            if (!sawKeep && _hasLastSeenKeep && tick - _lastSeenKeepTick > LastSeenKeepTimeoutTicks)
            {
                _hasLastSeenKeep = false;
            }

            if (!_hasSightedEnemy)
            {
                for (int i = 0; i < world.Units.Count; i++)
                {
                    var u = world.Units[i];
                    if (!u.IsAlive || u.Owner == Player)
                        continue;
                    if (!world.IsVisibleTo(Player, u.X, u.Z))
                        continue;
                    _hasSightedEnemy = true;
                    break;
                }
            }
        }

        private bool CanAssaultKeep(int tick)
        {
            if (_hasLastSeenKeep || _hasSightedEnemy)
                return true;
            if (_tuning.RequireSightBeforeAssault)
                return false;
            int elapsed = tick - Math.Max(0, _matchStartTick);
            return elapsed >= _tuning.ScoutAssaultTimeoutTicks;
        }

        private string ResolvePhase(in Perception sense)
        {
            if (sense.UnderAttack)
                return "Defend";
            if (!sense.HasKeep)
                return "Opening";
            int wantBuilders = Math.Max(1, sense.DesiredBuilders / 2);
            if (sense.Builders.Count < wantBuilders || sense.ActiveProducerCount == 0)
                return "Opening";
            if (sense.ActiveProducerCount < _tuning.TargetProducers
                || sense.TowerCount < _tuning.TargetTowers
                || sense.OutpostCount < _tuning.TargetOutposts
                || sense.Builders.Count < sense.DesiredBuilders)
                return "EcoExpand";
            if (_tuning.PreferTech
                && ((!_upgradeStarted && !string.IsNullOrEmpty(_basicUpgradeId))
                    || (!_powerUnlocked && !string.IsNullOrEmpty(_powerDefId))))
                return "Tech";
            if (sense.Combat.Count < _tuning.AssaultArmySize)
                return "Mass";
            return "Attack";
        }

        private int ReactionPad => Math.Max(0, _tuning.ReactionDelayTicks);

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
            // Same-tick move/place must win over gather (command apply order).
            if (BlocksGather(LastDecision))
                return;

            // Leave workers free for construction assists / on-site progress.
            int reserve = sense.Constructing.Count > 0 ? Math.Min(2, sense.IdleBuilders.Count) : 0;
            int available = sense.IdleBuilders.Count - reserve;
            if (available <= 0)
                return;

            // Split idle workers: some to expand gold when we have an outpost or second producer.
            bool pushExpand = sense.HasExpandGold
                              && (sense.OutpostCount > 0 || sense.ActiveProducerCount >= 2)
                              && available >= 2;
            if (pushExpand)
            {
                int expandCount = Math.Min(
                    available / 2,
                    Math.Max(1, _tuning.TargetWorkersPerNode));
                var expandIds = new SimEntityId[expandCount];
                for (int i = 0; i < expandCount; i++)
                    expandIds[i] = sense.IdleBuilders[i];
                commands.Add(new GatherCommand
                {
                    Issuer = Player,
                    UnitIds = expandIds,
                    ResourceNodeId = sense.ExpandGoldId,
                });

                if (expandCount < available
                    && TryFindNearestResource(world, sense.KeepX, sense.KeepZ, out var homeNode))
                {
                    int homeCount = available - expandCount;
                    var homeIds = new SimEntityId[homeCount];
                    for (int i = 0; i < homeCount; i++)
                        homeIds[i] = sense.IdleBuilders[expandCount + i];
                    commands.Add(new GatherCommand
                    {
                        Issuer = Player,
                        UnitIds = homeIds,
                        ResourceNodeId = homeNode,
                    });
                }

                _lastGatherTick = tick;
                if (LastDecision == "idle")
                    LastDecision = "gather_expand";
                return;
            }

            if (!TryFindNearestResource(world, sense.KeepX, sense.KeepZ, out var nodeId))
                return;

            var gatherIds = new SimEntityId[available];
            for (int i = 0; i < available; i++)
                gatherIds[i] = sense.IdleBuilders[i];
            commands.Add(new GatherCommand
            {
                Issuer = Player,
                UnitIds = gatherIds,
                ResourceNodeId = nodeId,
            });
            _lastGatherTick = tick;
            if (LastDecision == "idle")
                LastDecision = "gather";
        }

        private static bool BlocksGather(string decision)
        {
            if (string.IsNullOrEmpty(decision))
                return false;
            if (decision == "assist_construct" || decision == "attach_tower" || decision == "move_builders_to_site")
                return true;
            return decision.StartsWith("place_", StringComparison.Ordinal);
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

            // Opening / EcoExpand: delay research; still unlock power once a producer exists.
            bool delayUpgrade = CurrentPhase == "Opening" || CurrentPhase == "EcoExpand";

            if (!_powerUnlocked
                && !string.IsNullOrEmpty(_powerDefId)
                && sense.ProducerId.HasValue
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

            if (delayUpgrade)
                return;

            bool armyReady = sense.Combat.Count >= _tuning.AssaultArmySize;
            bool goldCushion = CanAfford(wallet, ApproxUpgradeGold * 2, 0);
            if (!_upgradeStarted
                && !string.IsNullOrEmpty(_basicUpgradeId)
                && (armyReady || goldCushion)
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

        private void IssueScout(List<GameCommand> commands, IWorldQuery world, in Perception sense, int tick)
        {
            if (sense.Combat.Count == 0)
                return;
            if (tick - _lastScoutTick < Math.Max(30, _tuning.ScoutIntervalTicks + ReactionPad))
                return;
            // Scouting matters most before / while massing; still refresh during Attack.
            if (CurrentPhase == "Defend")
                return;

            SplitForces(sense, out var guard, out var main, out _);
            var reserved = new HashSet<uint>();
            for (int i = 0; i < guard.Length; i++)
                reserved.Add(guard[i].Value);

            if (!TryPickScout(sense, reserved, out var scoutId))
                return;

            float tx;
            float tz;
            if (sense.HasLastSeenKeep)
            {
                tx = sense.LastSeenKeepX;
                tz = sense.LastSeenKeepZ;
            }
            else if (sense.HasKeep)
            {
                // Push toward the opposite side of the map from our keep.
                tx = -sense.KeepX * 0.85f;
                tz = -sense.KeepZ * 0.35f;
                if (Math.Abs(tx) < 40f && Math.Abs(tz) < 40f)
                {
                    tx = sense.KeepX > 0f ? -280f : 280f;
                    tz = 40f;
                }
            }
            else
            {
                tx = 0f;
                tz = 120f;
            }

            // Hard/Insane: second scout on a flanking offset when we have enough bodies.
            if (Difficulty >= AiDifficulty.Hard
                && sense.Combat.Count >= 3
                && TryPickScout(sense, reserved, out var scout2, preferFirst: false))
            {
                reserved.Add(scoutId.Value);
                if (scout2.Value != scoutId.Value)
                {
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = new[] { scout2 },
                        TargetX = tx + 90f,
                        TargetZ = tz - 70f,
                    });
                }
            }

            commands.Add(new AttackMoveCommand
            {
                Issuer = Player,
                UnitIds = new[] { scoutId },
                TargetX = tx,
                TargetZ = tz,
            });
            _lastScoutTick = tick;
            LastDecision = "scout";
        }

        private bool TryPickScout(
            in Perception sense,
            HashSet<uint> reserved,
            out SimEntityId scoutId,
            bool preferFirst = true)
        {
            scoutId = default;
            int cavalryIdx = -1;
            int anyIdx = -1;
            for (int i = 0; i < sense.Combat.Count; i++)
            {
                var id = sense.Combat[i];
                if (reserved.Contains(id.Value))
                    continue;
                if (anyIdx < 0)
                    anyIdx = i;
                string def = i < sense.CombatDefIds.Count ? sense.CombatDefIds[i] : string.Empty;
                if (!string.IsNullOrEmpty(_cavalryDefId) && def == _cavalryDefId)
                {
                    cavalryIdx = i;
                    break;
                }
            }

            int pick = cavalryIdx >= 0 ? cavalryIdx : anyIdx;
            if (pick < 0)
                return false;
            if (!preferFirst && sense.Combat.Count > pick + 1)
            {
                for (int i = pick + 1; i < sense.Combat.Count; i++)
                {
                    if (reserved.Contains(sense.Combat[i].Value))
                        continue;
                    scoutId = sense.Combat[i];
                    return true;
                }
            }

            scoutId = sense.Combat[pick];
            return true;
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
            int wantBuilders = sense.DesiredBuilders > 0 ? sense.DesiredBuilders : _tuning.TargetBuilders;
            // Opening: place first producer as soon as we have one builder — don't wait for full worker count.
            int buildersForFirstProducer = 1;
            int buildersNeededForProducer = sense.ActiveProducerCount == 0
                ? buildersForFirstProducer
                : Math.Max(1, wantBuilders / 2);

            // 1) Barracks / producers up to TargetProducers (priority over worker greed).
            if (sense.ActiveProducerCount < _tuning.TargetProducers
                && sense.Builders.Count >= buildersNeededForProducer
                && canBuild
                && CanAfford(wallet, ApproxProducerGold, ApproxProducerTimber))
            {
                var offset = sense.ActiveProducerCount == 0
                    ? GetPlaceOffset(sense, 0)
                    : GetPlaceOffset(sense, 1 + sense.ActiveProducerCount);
                if (sense.ActiveProducerCount > 0)
                {
                    float side = sense.KeepX > 0f ? -1f : 1f;
                    offset = (side * (48f + sense.ActiveProducerCount * 12f), -28f - sense.ActiveProducerCount * 8f);
                }

                if (TryPlaceNearKeep(
                        commands,
                        world,
                        in sense,
                        _producerDefId,
                        offset,
                        tick))
                {
                    LastDecision = sense.ActiveProducerCount == 0 ? "place_producer" : "place_second_producer";
                    return;
                }
            }

            // 2) Builders to worker-math target (after first producer is underway / live).
            bool mayTrainBuilders = sense.ActiveProducerCount > 0
                                    || sense.Constructing.Count > 0
                                    || sense.Builders.Count < buildersForFirstProducer;
            if (mayTrainBuilders
                && sense.KeepId.HasValue
                && sense.Builders.Count < wantBuilders
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

            // 3) Keep turrets (attach-only) then free-place watchtowers.
            if (sense.ActiveProducerCount > 0
                && sense.TowerCount < _tuning.TargetTowers
                && sense.Builders.Count > 0
                && canBuild)
            {
                bool attached = false;
                if (!string.IsNullOrEmpty(_keepTurretDefId)
                    && sense.KeepId.HasValue
                    && sense.KeepAttachSlots > 0
                    && sense.KeepTurretCount < sense.KeepAttachSlots
                    && CanAfford(wallet, ApproxKeepTurretGold, ApproxKeepTurretTimber))
                {
                    byte slot = FirstFreeSlot(sense.KeepAttachMask, sense.KeepAttachSlots);
                    // Only issue when the chosen slot is actually free.
                    if ((sense.KeepAttachMask & (1 << slot)) == 0)
                    {
                        commands.Add(new AttachBuildingCommand
                        {
                            Issuer = Player,
                            ParentBuildingId = sense.KeepId.Value,
                            SlotIndex = slot,
                            BuildingDefId = _keepTurretDefId,
                        });
                        _lastBuildTick = tick;
                        LastDecision = "attach_tower";
                        attached = true;
                    }
                }

                if (attached)
                    return;

                if (!string.IsNullOrEmpty(_towerDefId)
                    && sense.FreeTowerCount < _tuning.TargetTowers
                    && CanAfford(wallet, ApproxTowerGold, ApproxTowerTimber))
                {
                    float side = (sense.FreeTowerCount % 2 == 0) ? -1f : 1f;
                    float dist = 26f + sense.FreeTowerCount * 6f;
                    if (TryPlaceNearKeep(
                            commands,
                            world,
                            in sense,
                            _towerDefId,
                            (side * dist, 16f + sense.FreeTowerCount * 4f),
                            tick))
                    {
                        LastDecision = "place_tower";
                        return;
                    }
                }
            }

            // 4) Outpost near expand gold (else keep-relative fallback).
            if (sense.ActiveProducerCount > 0
                && !string.IsNullOrEmpty(_outpostDefId)
                && sense.OutpostCount < _tuning.TargetOutposts
                && sense.Builders.Count > 0
                && canBuild
                && CanAfford(wallet, ApproxOutpostGold, ApproxOutpostTimber))
            {
                (float dx, float dz) offset;
                if (sense.HasExpandGold)
                {
                    offset = (sense.ExpandGoldX - sense.KeepX, sense.ExpandGoldZ - sense.KeepZ);
                    float len = MathF.Sqrt(offset.dx * offset.dx + offset.dz * offset.dz);
                    const float maxOutpostDist = 72f;
                    if (len > maxOutpostDist && len > 0.01f)
                    {
                        float s = maxOutpostDist / len;
                        offset = (offset.dx * s, offset.dz * s);
                    }
                    else if (len > 1f)
                    {
                        offset = (offset.dx * 0.9f, offset.dz * 0.9f);
                    }
                }
                else
                {
                    float ox = sense.KeepX > 0f ? -55f : 55f;
                    offset = (ox, 0f);
                }

                if (TryPlaceNearKeep(commands, world, in sense, _outpostDefId, offset, tick))
                {
                    LastDecision = sense.HasExpandGold ? "place_outpost_expand" : "place_outpost";
                    return;
                }
            }

            // 5) Walls.
            if (sense.ActiveProducerCount > 0
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

            // 6) Combat training from any active producer.
            if (sense.ActiveProducerCount > 0
                && canTrain
                && CanAfford(wallet, ApproxCombatGold, 0)
                && TryFindTrainableProducer(world, out var trainerId))
            {
                string unitId = NextCombatUnitId(sense.UnderAttack, wallet);
                int cost = unitId == _cavalryDefId ? ApproxCavalryGold : ApproxCombatGold;
                if (!CanAfford(wallet, cost, 0))
                    unitId = _infantryDefId;

                commands.Add(new TrainUnitCommand
                {
                    Issuer = Player,
                    BuildingId = trainerId,
                    UnitDefId = unitId,
                });
                _lastTrainTick = tick;
                LastDecision = "train_combat";
            }
        }

        private bool TryFindTrainableProducer(IWorldQuery world, out SimEntityId producerId)
        {
            producerId = default;
            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner != Player || b.State != BuildingState.Active)
                    continue;
                if (b.DefinitionId != _producerDefId || !b.CanProduce)
                    continue;
                producerId = b.Id;
                return true;
            }

            return false;
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

            // Foundations do not require on-site builders to place; still walk workers over for progress.
            if (!BuildersNear(world, x, z, BuilderPlaceRadius))
                MoveBuildersToward(commands, in sense, x, z);

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
            if (tick - _lastDefendTick < _tuning.DefendIntervalTicks + ReactionPad)
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
            if (tick - _lastCombatOrderTick < _tuning.CombatOrderIntervalTicks + ReactionPad)
                return;

            SplitForces(sense, out var guard, out var main, out var harass);

            int assaultNeed = _tuning.AssaultArmySize;
            bool openingLike = CurrentPhase == "Opening" || CurrentPhase == "EcoExpand";
            bool attackPhase = CurrentPhase == "Attack";

            // Retreat: under pressure mid-push or wave too small after losses.
            if (attackPhase
                && (sense.UnderAttack || main.Length + harass.Length < Math.Max(2, (int)(assaultNeed * 0.6f))))
            {
                var retreat = ConcatIds(main, harass);
                if (retreat.Length > 0)
                {
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = retreat,
                        TargetX = sense.KeepX,
                        TargetZ = sense.KeepZ,
                    });
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
                LastDecision = "retreat";
                return;
            }

            // Opening / EcoExpand: scout + home guard only (no keep assault).
            if (openingLike)
            {
                if (guard.Length > 0)
                {
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = guard,
                        TargetX = sense.KeepX,
                        TargetZ = sense.KeepZ,
                    });
                    _lastCombatOrderTick = tick;
                    LastDecision = "home_guard";
                }

                return;
            }

            // Contest territory while massing / teching.
            if (!attackPhase
                && sense.Combat.Count < assaultNeed
                && world.Territories != null
                && world.Territories.Count > 0)
            {
                for (int i = 0; i < world.Territories.Count; i++)
                {
                    var territory = world.Territories[i];
                    if (territory.HasController && territory.Controller == Player)
                        continue;
                    // Prefer territories we can see, else still contest (map control).
                    commands.Add(new CaptureTerritoryCommand
                    {
                        Issuer = Player,
                        TerritoryNodeId = territory.Id,
                    });
                    var movers = main.Length > 0 ? main : sense.Combat.ToArray();
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = movers,
                        TargetX = territory.X,
                        TargetZ = territory.Z,
                    });
                    OrderGuardHome(commands, guard, in sense);
                    _lastCombatOrderTick = tick;
                    LastDecision = "contest_territory";
                    return;
                }
            }

            if (sense.Combat.Count < assaultNeed || !sense.CanAssaultKeep)
            {
                float holdX = sense.KeepX > 0f ? sense.KeepX - 40f : sense.KeepX + 40f;
                var picket = main.Length > 0 ? main : sense.Combat.ToArray();
                commands.Add(new AttackMoveCommand
                {
                    Issuer = Player,
                    UnitIds = picket,
                    TargetX = holdX,
                    TargetZ = sense.KeepZ,
                });
                OrderGuardHome(commands, guard, in sense);
                _lastCombatOrderTick = tick;
                LastDecision = sense.CanAssaultKeep ? "hold_picket" : "wait_scout";
                return;
            }

            // Multi-prong assault.
            var attackers = main.Length > 0 ? main : sense.Combat.ToArray();
            float ekx = 0f;
            float ekz = 0f;
            SimEntityId visibleKeepId = default;
            bool foundVisibleKeep = TryFindEnemyKeep(world, out visibleKeepId, out ekx, out ekz);
            bool haveKeepTarget = sense.HasLastSeenKeep || foundVisibleKeep;
            float keepX = sense.HasLastSeenKeep ? sense.LastSeenKeepX : ekx;
            float keepZ = sense.HasLastSeenKeep ? sense.LastSeenKeepZ : ekz;
            SimEntityId keepTarget = sense.HasLastSeenKeep ? sense.LastSeenKeepId : visibleKeepId;
            bool keepVisible = foundVisibleKeep || (sense.HasLastSeenKeep && world.IsVisibleTo(Player, keepX, keepZ));

            if (haveKeepTarget)
            {
                if (keepVisible
                    && (sense.Combat.Count >= assaultNeed + 2 || _tuning.Aggression >= 0.8f))
                {
                    commands.Add(new AttackCommand
                    {
                        Issuer = Player,
                        UnitIds = attackers,
                        TargetId = keepTarget,
                    });
                    LastDecision = "assault_keep";
                }
                else
                {
                    commands.Add(new AttackMoveCommand
                    {
                        Issuer = Player,
                        UnitIds = attackers,
                        TargetX = keepX,
                        TargetZ = keepZ,
                    });
                    LastDecision = "attack_move_keep";
                }

                if (harass.Length > 0)
                    IssueHarassOrders(commands, world, in sense, harass, keepX, keepZ);

                OrderGuardHome(commands, guard, in sense);
                _lastCombatOrderTick = tick;
                return;
            }

            if (TryFindNearestEnemyThreat(world, sense.KeepX, sense.KeepZ, 400f, out _, out float tx, out float tz))
            {
                commands.Add(new AttackMoveCommand
                {
                    Issuer = Player,
                    UnitIds = attackers,
                    TargetX = tx,
                    TargetZ = tz,
                });
                if (harass.Length > 0)
                    IssueHarassOrders(commands, world, in sense, harass, tx, tz);
                OrderGuardHome(commands, guard, in sense);
                _lastCombatOrderTick = tick;
                LastDecision = "hunt_threat";
            }
        }

        private void IssueHarassOrders(
            List<GameCommand> commands,
            IWorldQuery world,
            in Perception sense,
            SimEntityId[] harass,
            float axisX,
            float axisZ)
        {
            if (TryFindVisibleEnemyWorker(world, out _, out float wx, out float wz)
                || TryFindVisibleEnemyResource(world, out wx, out wz))
            {
                commands.Add(new AttackMoveCommand
                {
                    Issuer = Player,
                    UnitIds = harass,
                    TargetX = wx,
                    TargetZ = wz,
                });
                LastDecision = "harass_eco";
                return;
            }

            // Flank offset from assault axis.
            float fx = axisX + (sense.KeepX > axisX ? -80f : 80f);
            float fz = axisZ + 60f;
            commands.Add(new AttackMoveCommand
            {
                Issuer = Player,
                UnitIds = harass,
                TargetX = fx,
                TargetZ = fz,
            });
            if (LastDecision == "assault_keep" || LastDecision == "attack_move_keep")
                LastDecision = "assault_multiprong";
        }

        private void OrderGuardHome(List<GameCommand> commands, SimEntityId[] guard, in Perception sense)
        {
            if (guard.Length == 0)
                return;
            commands.Add(new AttackMoveCommand
            {
                Issuer = Player,
                UnitIds = guard,
                TargetX = sense.KeepX,
                TargetZ = sense.KeepZ,
            });
        }

        private void SplitForces(
            in Perception sense,
            out SimEntityId[] guard,
            out SimEntityId[] main,
            out SimEntityId[] harass)
        {
            var combat = sense.Combat;
            int guardNeed = Math.Min(_tuning.HomeGuardSize, Math.Max(0, combat.Count - 1));
            int harassNeed = 0;
            if (_tuning.HarassSize > 0
                && combat.Count >= _tuning.AssaultArmySize + _tuning.HarassSize
                && Difficulty >= AiDifficulty.Hard)
            {
                harassNeed = Math.Min(_tuning.HarassSize, combat.Count - guardNeed - 1);
                if (harassNeed < 0)
                    harassNeed = 0;
            }

            if (combat.Count <= guardNeed)
            {
                guard = combat.ToArray();
                main = Array.Empty<SimEntityId>();
                harass = Array.Empty<SimEntityId>();
                return;
            }

            // Prefer ranged/cavalry for harass from the end of the list.
            var used = new bool[combat.Count];
            guard = new SimEntityId[guardNeed];
            for (int i = 0; i < guardNeed; i++)
            {
                guard[i] = combat[i];
                used[i] = true;
            }

            harass = new SimEntityId[harassNeed];
            int hFilled = 0;
            for (int pass = 0; pass < 2 && hFilled < harassNeed; pass++)
            {
                for (int i = combat.Count - 1; i >= 0 && hFilled < harassNeed; i--)
                {
                    if (used[i])
                        continue;
                    string def = i < sense.CombatDefIds.Count ? sense.CombatDefIds[i] : string.Empty;
                    bool preferred = def == _rangedDefId || def == _cavalryDefId;
                    if (pass == 0 && !preferred)
                        continue;
                    harass[hFilled++] = combat[i];
                    used[i] = true;
                }
            }

            if (hFilled < harassNeed)
            {
                var trimmed = new SimEntityId[hFilled];
                Array.Copy(harass, trimmed, hFilled);
                harass = trimmed;
            }

            int mainCount = 0;
            for (int i = 0; i < used.Length; i++)
            {
                if (!used[i])
                    mainCount++;
            }

            main = new SimEntityId[mainCount];
            int m = 0;
            for (int i = 0; i < combat.Count; i++)
            {
                if (used[i])
                    continue;
                main[m++] = combat[i];
            }
        }

        private static SimEntityId[] ConcatIds(SimEntityId[] a, SimEntityId[] b)
        {
            if (a == null || a.Length == 0)
                return b ?? Array.Empty<SimEntityId>();
            if (b == null || b.Length == 0)
                return a;
            var result = new SimEntityId[a.Length + b.Length];
            Array.Copy(a, 0, result, 0, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
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

        private bool TryFindNearestResource(
            IWorldQuery world,
            float fromX,
            float fromZ,
            out SimEntityId nodeId)
        {
            nodeId = default;
            if (world.Resources == null || world.Resources.Count == 0)
                return false;

            // Prefer visible nodes; fall back to nearest (workers know the economy).
            if (TryPickResource(world, fromX, fromZ, requireVisible: true, out nodeId))
                return true;
            return TryPickResource(world, fromX, fromZ, requireVisible: false, out nodeId);
        }

        private bool TryPickResource(
            IWorldQuery world,
            float fromX,
            float fromZ,
            bool requireVisible,
            out SimEntityId nodeId)
        {
            nodeId = default;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < world.Resources.Count; i++)
            {
                var r = world.Resources[i];
                if (r.Remaining <= 0)
                    continue;
                if (requireVisible && !world.IsVisibleTo(Player, r.X, r.Z))
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
                if (!world.IsVisibleTo(Player, b.X, b.Z))
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
                if (!world.IsVisibleTo(Player, u.X, u.Z))
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
                if (!world.IsVisibleTo(Player, b.X, b.Z))
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

        private bool TryFindVisibleEnemyWorker(IWorldQuery world, out SimEntityId id, out float x, out float z)
        {
            id = default;
            x = 0f;
            z = 0f;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (!u.IsAlive || u.Owner == Player)
                    continue;
                if (!world.IsVisibleTo(Player, u.X, u.Z))
                    continue;
                string def = u.DefinitionId ?? string.Empty;
                if (!def.Contains("builder") && !def.Contains("worker"))
                    continue;
                float d2 = u.X * u.X + u.Z * u.Z;
                if (d2 >= best)
                    continue;
                best = d2;
                id = u.Id;
                x = u.X;
                z = u.Z;
                found = true;
            }

            return found;
        }

        private bool TryFindVisibleEnemyResource(IWorldQuery world, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            // Prefer resources near last-seen keep / opposite of our keep.
            float preferX = _hasLastSeenKeep ? _lastSeenKeepX : 0f;
            float preferZ = _hasLastSeenKeep ? _lastSeenKeepZ : 0f;
            float best = float.MaxValue;
            bool found = false;
            if (world.Resources == null)
                return false;
            for (int i = 0; i < world.Resources.Count; i++)
            {
                var r = world.Resources[i];
                if (r.Remaining <= 0)
                    continue;
                if (!world.IsVisibleTo(Player, r.X, r.Z))
                    continue;
                float dx = r.X - preferX;
                float dz = r.Z - preferZ;
                float d2 = dx * dx + dz * dz;
                if (d2 >= best)
                    continue;
                best = d2;
                x = r.X;
                z = r.Z;
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
            public int ActiveProducerCount;
            public int TowerCount;
            public int KeepTurretCount;
            public int FreeTowerCount;
            public int OutpostCount;
            public int WallCount;
            public bool UnderAttack;
            public bool HasSightedEnemy;
            public bool HasLastSeenKeep;
            public SimEntityId LastSeenKeepId;
            public float LastSeenKeepX;
            public float LastSeenKeepZ;
            public bool CanAssaultKeep;
            public bool HasExpandGold;
            public SimEntityId ExpandGoldId;
            public float ExpandGoldX;
            public float ExpandGoldZ;
            public int GoldNodeCount;
            public int DesiredBuilders;
            public List<SimEntityId> Builders;
            public List<SimEntityId> IdleBuilders;
            public List<SimEntityId> Combat;
            public List<string> CombatDefIds;
            public List<(SimEntityId id, float x, float z)> Constructing;
        }
    }
}
