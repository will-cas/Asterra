using System;
using System.Collections.Generic;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Authoritative skirmish sim (plain data). Presentation mirrors snapshots + combat events.
    /// </summary>
    public sealed class SkirmishWorldSim : IWorldSim
    {
        private readonly IResourceWallet _wallet;
        private readonly IIdFactory _ids;
        private readonly DefinitionRegistry _defs;
        private readonly UpgradeState _upgrades;
        private readonly WorldEnvironmentSim _environment;

        private readonly List<SimUnit> _units = new();
        private readonly List<SimBuilding> _buildings = new();
        private readonly List<SimTerritory> _territories = new();
        private readonly List<SimResourceNode> _resources = new();
        private readonly List<SimDestructible> _destructibles = new();

        private readonly List<UnitSnapshot> _unitSnapshots = new();
        private readonly List<BuildingSnapshot> _buildingSnapshots = new();
        private readonly List<TerritorySnapshot> _territorySnapshots = new();
        private readonly List<ResourceSnapshot> _resourceSnapshots = new();
        private readonly List<CombatEvent> _combatEvents = new();
        private readonly List<ProjectileSnapshot> _projectileSnapshots = new();
        private readonly List<DestructibleSnapshot> _destructibleSnapshots = new();
        private readonly List<SimProjectile> _projectiles = new();

        private readonly Dictionary<uint, SimUnit> _unitsById = new();
        private readonly Dictionary<uint, SimBuilding> _buildingsById = new();
        private readonly Dictionary<uint, SimTerritory> _territoriesById = new();
        private readonly Dictionary<uint, SimResourceNode> _resourcesById = new();
        private readonly Dictionary<uint, SimDestructible> _destructiblesById = new();

        private float _gatherAcc;
        private float _incomeAcc;
        private ulong _mutationCounter;

        private sealed class CommanderAbilityRuntime
        {
            public float CooldownRemaining;
            public float BuffRemaining;
            public float ArmorBonus;
        }

        private readonly Dictionary<byte, CommanderAbilityRuntime> _commanderAbilities = new();

        private sealed class SimProjectile
        {
            public float X;
            public float Z;
            public float Speed;
            public float Damage;
            public SimEntityId TargetId;
            public bool VsBuilding;
        }

        public SkirmishWorldSim(
            IResourceWallet wallet,
            IIdFactory ids,
            DefinitionRegistry defs,
            WorldEnvironmentSim environment = null)
        {
            _wallet = wallet;
            _ids = ids;
            _defs = defs;
            _upgrades = new UpgradeState(wallet, defs);
            _environment = environment ?? new WorldEnvironmentSim();
        }

        /// <summary>World terrain / weather / time. Presentation and tests may query; orders stay command-driven.</summary>
        public WorldEnvironmentSim Environment => _environment;

        public IReadOnlyList<UnitSnapshot> Units => _unitSnapshots;
        public IReadOnlyList<BuildingSnapshot> Buildings => _buildingSnapshots;
        public IReadOnlyList<TerritorySnapshot> Territories => _territorySnapshots;
        public IReadOnlyList<ResourceSnapshot> Resources => _resourceSnapshots;
        public IReadOnlyList<CombatEvent> CombatEvents => _combatEvents;
        public IReadOnlyList<ProjectileSnapshot> Projectiles => _projectileSnapshots;
        public IReadOnlyList<DestructibleSnapshot> Destructibles => _destructibleSnapshots;

        public bool HasUpgrade(PlayerId player, string upgradeDefId) => _upgrades.Has(player, upgradeDefId);

        public bool TryGetCommanderAbilityStatus(PlayerId player, out float cooldownRemaining, out float buffRemaining)
        {
            if (_commanderAbilities.TryGetValue(player.Value, out var state))
            {
                cooldownRemaining = state.CooldownRemaining;
                buffRemaining = state.BuffRemaining;
                return true;
            }

            cooldownRemaining = 0f;
            buffRemaining = 0f;
            return false;
        }

        public SimUnit SpawnUnit(SimEntityId id, PlayerId owner, FactionId faction, string unitDefId, float x, float z)
        {
            if (!_defs.TryGetUnit(unitDefId, out var def))
                throw new InvalidOperationException($"Unknown unit def '{unitDefId}'.");

            var unit = new SimUnit(id, owner, faction, def, x, z);
            unit.AttackDamage *= _upgrades.UnitDamageMultiplier(owner);
            if (_commanderAbilities.TryGetValue(owner.Value, out var ability)
                && ability.BuffRemaining > 0f
                && ability.ArmorBonus > 0f)
            {
                unit.Armor += ability.ArmorBonus;
                unit.CommanderArmorBonus = ability.ArmorBonus;
            }

            _units.Add(unit);
            _unitsById[id.Value] = unit;
            RebuildSnapshots();
            return unit;
        }

        public SimBuilding SpawnBuilding(
            SimEntityId id,
            PlayerId owner,
            FactionId faction,
            string buildingDefId,
            float x,
            float z,
            bool startActive)
        {
            if (!_defs.TryGetBuilding(buildingDefId, out var def))
                throw new InvalidOperationException($"Unknown building def '{buildingDefId}'.");

            var building = new SimBuilding(id, owner, faction, def, x, z, startActive);
            building.RallyX = x + (owner.Value == 0 ? 18f : -18f);
            building.RallyZ = z;
            _buildings.Add(building);
            _buildingsById[id.Value] = building;
            RebuildSnapshots();
            return building;
        }

        public void AddTerritory(SimEntityId id, float x, float z, float radius, int goldPerSecond)
        {
            var node = new SimTerritory(id, x, z, radius)
            {
                GoldPerSecondWhenControlled = goldPerSecond,
            };
            _territories.Add(node);
            _territoriesById[id.Value] = node;
            RebuildSnapshots();
        }

        public void AddResourceNode(SimEntityId id, ResourceType type, int amount, float x, float z)
        {
            var node = new SimResourceNode(id, type, amount, x, z);
            _resources.Add(node);
            _resourcesById[id.Value] = node;
            RebuildSnapshots();
        }

        public SimDestructible SpawnDestructible(
            SimEntityId id,
            DestructibleDefData def,
            float x,
            float z,
            int linkedTraversalLinkId = -1)
        {
            var prop = new SimDestructible(id, def, x, z, linkedTraversalLinkId);
            _destructibles.Add(prop);
            _destructiblesById[id.Value] = prop;
            RebuildSnapshots();
            return prop;
        }

        /// <summary>Test / ability hook for applying structure damage without spawning an attacker.</summary>
        public void ApplyWorldDamage(SimEntityId targetId, float damage, bool vsStructure = true)
        {
            DealDamage(targetId, damage, vsStructure);
            RebuildSnapshots();
        }

        public void ApplyCommands(IReadOnlyList<GameCommand> commands)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                switch (command)
                {
                    case MoveCommand move:
                        ApplyMove(move);
                        break;
                    case AttackCommand attack:
                        ApplyAttack(attack);
                        break;
                    case PlaceBuildingCommand place:
                        ApplyPlaceBuilding(place);
                        break;
                    case TrainUnitCommand train:
                        ApplyTrain(train);
                        break;
                    case CaptureTerritoryCommand capture:
                        ApplyCaptureOrder(capture);
                        break;
                    case ChooseUpgradeCommand upgrade:
                        ApplyUpgrade(upgrade);
                        break;
                    case SetStanceCommand stance:
                        ApplyStance(stance);
                        break;
                    case GatherCommand gather:
                        ApplyGather(gather);
                        break;
                    case SetRallyCommand rally:
                        ApplySetRally(rally);
                        break;
                    case CancelProductionCommand cancel:
                        ApplyCancelProduction(cancel);
                        break;
                    case AttackMoveCommand attackMove:
                        ApplyAttackMove(attackMove);
                        break;
                    case StopCommand stop:
                        ApplyStop(stop);
                        break;
                    case PatrolCommand patrol:
                        ApplyPatrol(patrol);
                        break;
                    case ActivateCommanderAbilityCommand ability:
                        ApplyCommanderAbility(ability);
                        break;
                    default:
                        break;
                }
            }

            RebuildSnapshots();
        }

        public void Tick(float deltaSeconds)
        {
            _combatEvents.Clear();
            _environment.Tick(deltaSeconds);
            TickCommanderAbilities(deltaSeconds);
            TickConstruction(deltaSeconds);
            TickProduction(deltaSeconds);
            TickGather(deltaSeconds);
            TickMovement(deltaSeconds);
            TickCombat(deltaSeconds);
            TickTowers(deltaSeconds);
            TickProjectiles(deltaSeconds);
            TickTerritory(deltaSeconds);
            TickTerritoryIncome(deltaSeconds);
            CullDead();
            // Dirty regions are applied immediately on destroy; clear the queue for listeners/pathfinding.
            _environment.PathDirty.Clear();
            RebuildSnapshots();
        }

        public ulong ComputeWorldHash()
        {
            ulong hash = 14695981039346656037ul;
            hash ^= _mutationCounter;
            hash ^= _environment.Grid.MutationVersion * 1099511628211ul;
            hash ^= (ulong)_units.Count * 1099511628211ul;
            hash ^= (ulong)_buildings.Count * 1099511628211ul;
            hash ^= (ulong)_resources.Count * 1099511628211ul;
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                hash ^= u.Id.Value;
                hash ^= (ulong)(u.X * 100f);
                hash ^= (ulong)(u.Z * 100f);
                hash ^= (ulong)u.Health;
                hash ^= (ulong)u.CarryAmount;
            }

            for (int i = 0; i < _resources.Count; i++)
                hash ^= (ulong)_resources[i].Remaining * 31ul;

            for (int i = 0; i < _territories.Count; i++)
            {
                var t = _territories[i];
                hash ^= t.Id.Value * 17ul;
                hash ^= (ulong)t.State;
                if (t.Controller.HasValue)
                    hash ^= t.Controller.Value.Value;
            }

            return hash;
        }

        private void ApplyMove(MoveCommand move)
        {
            if (move.UnitIds == null)
                return;
            int count = CountOwnedAlive(move.UnitIds, move.Issuer);
            int index = 0;
            for (int i = 0; i < move.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(move.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != move.Issuer || !unit.IsAlive)
                    continue;
                FormationOffset(move.TargetX, move.TargetZ, index, count, out float tx, out float tz);
                index++;
                unit.MoveTargetX = tx;
                unit.MoveTargetZ = tz;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = false;
                unit.Patrolling = false;
                _mutationCounter ^= unit.Id.Value * 3ul;
            }
        }

        private void ApplyAttack(AttackCommand attack)
        {
            if (attack.UnitIds == null)
                return;
            for (int i = 0; i < attack.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(attack.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != attack.Issuer || !unit.IsAlive)
                    continue;
                unit.AttackTargetId = attack.TargetId;
                unit.MoveTargetX = null;
                unit.MoveTargetZ = null;
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = false;
                unit.Patrolling = false;
                _mutationCounter ^= unit.Id.Value * 733ul;
            }
        }

        private void ApplyAttackMove(AttackMoveCommand attackMove)
        {
            if (attackMove.UnitIds == null)
                return;
            int count = 0;
            for (int i = 0; i < attackMove.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(attackMove.UnitIds[i].Value, out var u))
                    continue;
                if (u.Owner == attackMove.Issuer && u.IsAlive && u.Role != UnitRole.Builder && u.AttackDamage > 0f)
                    count++;
            }

            int index = 0;
            for (int i = 0; i < attackMove.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(attackMove.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != attackMove.Issuer || !unit.IsAlive)
                    continue;
                if (unit.Role == UnitRole.Builder || unit.AttackDamage <= 0f)
                    continue;
                FormationOffset(attackMove.TargetX, attackMove.TargetZ, index, count, out float tx, out float tz);
                index++;
                unit.MoveTargetX = tx;
                unit.MoveTargetZ = tz;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = true;
                unit.Patrolling = false;
                _mutationCounter ^= unit.Id.Value * 743ul;
            }
        }

        private void ApplyStop(StopCommand stop)
        {
            if (stop.UnitIds == null)
                return;
            for (int i = 0; i < stop.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(stop.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != stop.Issuer || !unit.IsAlive)
                    continue;
                unit.MoveTargetX = null;
                unit.MoveTargetZ = null;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = false;
                unit.Patrolling = false;
                ClearTraversal(unit);
                _mutationCounter ^= unit.Id.Value * 17ul;
            }
        }

        private void ApplyPatrol(PatrolCommand patrol)
        {
            if (patrol.UnitIds == null)
                return;
            int count = CountOwnedAlive(patrol.UnitIds, patrol.Issuer);
            int index = 0;
            for (int i = 0; i < patrol.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(patrol.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != patrol.Issuer || !unit.IsAlive)
                    continue;
                FormationOffset(patrol.TargetX, patrol.TargetZ, index, count, out float bx, out float bz);
                index++;
                unit.PatrolAX = unit.X;
                unit.PatrolAZ = unit.Z;
                unit.PatrolBX = bx;
                unit.PatrolBZ = bz;
                unit.PatrolToB = true;
                unit.Patrolling = true;
                unit.MoveTargetX = bx;
                unit.MoveTargetZ = bz;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = false;
                _mutationCounter ^= unit.Id.Value * 19ul;
            }
        }

        private void ApplyGather(GatherCommand gather)
        {
            if (gather.UnitIds == null)
                return;
            if (!_resourcesById.TryGetValue(gather.ResourceNodeId.Value, out var node) || node.IsDepleted)
                return;

            for (int i = 0; i < gather.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(gather.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != gather.Issuer || !unit.IsAlive || !unit.CanGather)
                    continue;
                unit.GatherTargetId = gather.ResourceNodeId;
                unit.AttackTargetId = null;
                unit.ReturningToDeposit = unit.CarryAmount >= unit.CarryCapacity;
                unit.MoveTargetX = null;
                unit.MoveTargetZ = null;
                unit.AttackMoving = false;
                unit.Patrolling = false;
                _mutationCounter ^= unit.Id.Value * 911ul;
            }
        }

        private void ApplySetRally(SetRallyCommand rally)
        {
            if (!_buildingsById.TryGetValue(rally.BuildingId.Value, out var building))
                return;
            if (building.Owner != rally.Issuer)
                return;
            building.RallyX = rally.TargetX;
            building.RallyZ = rally.TargetZ;
            _mutationCounter ^= building.Id.Value * 409ul;
        }

        private void ApplyCancelProduction(CancelProductionCommand cancel)
        {
            if (!_buildingsById.TryGetValue(cancel.BuildingId.Value, out var building))
                return;
            if (building.Owner != cancel.Issuer)
                return;

            if (building.IsProducing)
            {
                if (_defs.TryGetUnit(building.ProductionUnitDefId, out var def))
                    _wallet.Add(cancel.Issuer, ResourceType.Gold, def.GoldCost / 2);
                building.ProductionUnitDefId = null;
                building.ProductionSecondsRemaining = 0f;
                building.ProductionSecondsTotal = 0f;
                TryStartNextQueued(building);
                _mutationCounter ^= building.Id.Value * 577ul;
                return;
            }

            if (building.QueueCount > 0)
            {
                string defId = building.Queue[0];
                ShiftQueue(building);
                if (_defs.TryGetUnit(defId, out var def))
                    _wallet.Add(cancel.Issuer, ResourceType.Gold, def.GoldCost);
                _mutationCounter ^= building.Id.Value * 579ul;
            }
        }

        private void ApplyPlaceBuilding(PlaceBuildingCommand place)
        {
            if (!_defs.TryGetBuilding(place.BuildingDefId, out var def))
                return;

            float x = place.X;
            float z = place.Z;
            bool snapWall = def.SnapToWallGrid || def.Kind == BuildingKind.Wall || def.Kind == BuildingKind.Gate;
            if (snapWall)
                WallPlacement.Snap(ref x, ref z, def.WallSegmentLength > 1f ? def.WallSegmentLength : WallPlacement.DefaultSegment);

            if (!IsInsidePlayable(x, z))
                return;

            if (!_environment.CanPlaceBuilding(x, z))
                return;

            if (!HasNearbyBuilder(place.Issuer, x, z, builderPlaceRadius: 55f))
                return;

            if (!_wallet.TrySpend(place.Issuer, ResourceType.Gold, def.GoldCost))
                return;
            if (!_wallet.TrySpend(place.Issuer, ResourceType.Timber, def.TimberCost))
            {
                _wallet.Add(place.Issuer, ResourceType.Gold, def.GoldCost);
                return;
            }

            var faction = ResolveFaction(place.Issuer);
            var building = SpawnBuilding(_ids.Next(), place.Issuer, faction, def.Id, x, z, startActive: false);
            if (snapWall)
                RefreshWallConnectionsAround(building);
            _mutationCounter ^= (ulong)def.Id.GetHashCode();
        }

        /// <summary>Recompute cardinal wall neighbour bits around a segment (placement / tests).</summary>
        public void RefreshWallConnectionsAround(SimBuilding hub)
        {
            float seg = hub.WallSegmentLength > 1f ? hub.WallSegmentLength : WallPlacement.DefaultSegment;
            float tol = seg * 0.35f;
            hub.WallLinks = 0;
            for (int i = 0; i < _buildings.Count; i++)
            {
                var other = _buildings[i];
                if (other.Id.Value == hub.Id.Value || other.State == BuildingState.Destroyed)
                    continue;
                if (other.Kind != BuildingKind.Wall && other.Kind != BuildingKind.Gate)
                    continue;
                float dx = other.X - hub.X;
                float dz = other.Z - hub.Z;
                float dist = MathF.Sqrt(dx * dx + dz * dz);
                if (dist < seg * 0.55f || dist > seg * 1.45f)
                    continue;
                if (MathF.Abs(dist - seg) > tol && MathF.Min(MathF.Abs(dx), MathF.Abs(dz)) > tol)
                    continue;

                int dir = WallPlacement.CardinalIndex(hub.X, hub.Z, other.X, other.Z);
                hub.WallLinks |= (byte)(1 << dir);
                int opp = (dir + 2) % 4;
                other.WallLinks |= (byte)(1 << opp);
            }
        }

        private void ApplyTrain(TrainUnitCommand train)
        {
            if (!_buildingsById.TryGetValue(train.BuildingId.Value, out var building))
                return;
            if (building.Owner != train.Issuer || !building.CanProduce)
                return;
            if (!Contains(building.TrainableUnitIds, train.UnitDefId))
                return;
            if (!_defs.TryGetUnit(train.UnitDefId, out var unitDef))
                return;

            int occupied = building.QueueCount + (building.IsProducing ? 1 : 0);
            if (occupied >= building.QueueCapacity)
                return;
            if (!_wallet.TrySpend(train.Issuer, ResourceType.Gold, unitDef.GoldCost))
                return;

            if (!building.IsProducing)
            {
                float trainMult = _upgrades.TrainTimeMultiplier(train.Issuer);
                building.ProductionUnitDefId = train.UnitDefId;
                building.ProductionSecondsTotal = unitDef.TrainSeconds * trainMult;
                building.ProductionSecondsRemaining = building.ProductionSecondsTotal;
            }
            else
            {
                building.Queue[building.QueueCount++] = train.UnitDefId;
            }

            _mutationCounter ^= building.Id.Value * 397ul;
        }

        private void ApplyCaptureOrder(CaptureTerritoryCommand capture)
        {
            if (!_territoriesById.TryGetValue(capture.TerritoryNodeId.Value, out var node))
                return;
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (unit.Owner != capture.Issuer || !unit.IsAlive)
                    continue;
                if (unit.Role == UnitRole.Builder)
                    continue;
                unit.MoveTargetX = node.X;
                unit.MoveTargetZ = node.Z;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
            }

            _mutationCounter ^= capture.TerritoryNodeId.Value * 541ul;
        }

        private void ApplyUpgrade(ChooseUpgradeCommand upgrade)
        {
            if (!_defs.TryGetUpgrade(upgrade.UpgradeDefId, out var def))
                return;
            if (!_upgrades.TryUnlock(upgrade.Issuer, def.Id, def.GoldCost))
                return;

            float dmgMult = _upgrades.UnitDamageMultiplier(upgrade.Issuer);
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (unit.Owner != upgrade.Issuer || !unit.IsAlive)
                    continue;
                if (!_defs.TryGetUnit(unit.DefinitionId, out var baseDef))
                    continue;
                unit.AttackDamage = baseDef.AttackDamage * dmgMult;
            }

            _mutationCounter ^= (ulong)def.Id.GetHashCode();
        }

        private void ApplyCommanderAbility(ActivateCommanderAbilityCommand cmd)
        {
            if (!TryResolvePlayerFaction(cmd.Issuer, out var faction))
                return;
            if (faction != FactionDefaultContent.IronCovenant.Id)
                return;

            if (!_commanderAbilities.TryGetValue(cmd.Issuer.Value, out var state))
            {
                state = new CommanderAbilityRuntime();
                _commanderAbilities[cmd.Issuer.Value] = state;
            }

            if (state.CooldownRemaining > 0f)
                return;

            // Clear any leftover buff before re-applying.
            if (state.BuffRemaining > 0f)
                ClearCommanderArmorBuff(cmd.Issuer, state.ArmorBonus);

            state.ArmorBonus = FactionDefaultContent.LucienIronWallArmorBonus;
            state.BuffRemaining = FactionDefaultContent.LucienIronWallDurationSeconds;
            state.CooldownRemaining = FactionDefaultContent.LucienIronWallCooldownSeconds;
            ApplyCommanderArmorBuff(cmd.Issuer, state.ArmorBonus);
            _mutationCounter ^= 0xA11CEUL ^ (ulong)(cmd.Issuer.Value + 1) * 97ul;
        }

        private void TickCommanderAbilities(float dt)
        {
            if (_commanderAbilities.Count == 0)
                return;

            foreach (var pair in _commanderAbilities)
            {
                byte key = pair.Key;
                var state = pair.Value;
                bool changed = false;
                if (state.CooldownRemaining > 0f)
                {
                    state.CooldownRemaining = Math.Max(0f, state.CooldownRemaining - dt);
                    changed = true;
                }

                if (state.BuffRemaining > 0f)
                {
                    state.BuffRemaining -= dt;
                    if (state.BuffRemaining <= 0f)
                    {
                        state.BuffRemaining = 0f;
                        ClearCommanderArmorBuff(new PlayerId(key), state.ArmorBonus);
                        state.ArmorBonus = 0f;
                    }

                    changed = true;
                }

                if (changed)
                    _mutationCounter ^= (ulong)(key + 1) * 13ul;
            }
        }

        private void ApplyCommanderArmorBuff(PlayerId owner, float bonus)
        {
            if (bonus <= 0f)
                return;
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (unit.Owner != owner || !unit.IsAlive)
                    continue;
                if (unit.CommanderArmorBonus > 0f)
                    continue;
                unit.Armor += bonus;
                unit.CommanderArmorBonus = bonus;
            }
        }

        private void ClearCommanderArmorBuff(PlayerId owner, float bonus)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (unit.Owner != owner || unit.CommanderArmorBonus <= 0f)
                    continue;
                float remove = bonus > 0f ? bonus : unit.CommanderArmorBonus;
                unit.Armor = Math.Max(0f, unit.Armor - remove);
                unit.CommanderArmorBonus = 0f;
            }
        }

        private bool TryResolvePlayerFaction(PlayerId player, out FactionId faction)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner == player && b.State != BuildingState.Destroyed)
                {
                    faction = b.Faction;
                    return true;
                }
            }

            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u.Owner == player && u.IsAlive)
                {
                    faction = u.Faction;
                    return true;
                }
            }

            faction = default;
            return false;
        }

        private bool IsIronWallActive(PlayerId owner)
        {
            return _commanderAbilities.TryGetValue(owner.Value, out var state) && state.BuffRemaining > 0f;
        }

        private float MitigateBuildingDamage(PlayerId owner, float damage)
        {
            if (!IsIronWallActive(owner))
                return damage;
            return CombatMath.ApplyArmor(damage, FactionDefaultContent.LucienIronWallBuildingMitigation);
        }

        private void ApplyStance(SetStanceCommand stance)
        {
            if (stance.UnitIds == null)
                return;
            for (int i = 0; i < stance.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(stance.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != stance.Issuer)
                    continue;
                unit.Stance = stance.Stance;
            }

            _mutationCounter ^= (ulong)stance.Stance;
        }

        private void TickConstruction(float dt)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State != BuildingState.Constructing)
                    continue;
                b.BuildSecondsRemaining -= dt;
                if (b.BuildSecondsRemaining <= 0f)
                {
                    b.State = BuildingState.Active;
                    _combatEvents.Add(new CombatEvent(CombatEventKind.BuildComplete, b.Id, b.X, b.Z, true));
                    AutoGatherNearbyBuilders(b.Owner, b.X, b.Z);
                }
            }
        }

        private void TickProduction(float dt)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (!b.IsProducing)
                    continue;
                b.ProductionSecondsRemaining -= dt;
                if (b.ProductionSecondsRemaining > 0f)
                    continue;

                string unitDefId = b.ProductionUnitDefId;
                b.ProductionUnitDefId = null;
                b.ProductionSecondsRemaining = 0f;
                b.ProductionSecondsTotal = 0f;

                float sx = b.RallyX ?? (b.X + 3f);
                float sz = b.RallyZ ?? b.Z;
                SpawnUnit(_ids.Next(), b.Owner, b.Faction, unitDefId, b.X + 3f, b.Z);
                // New unit immediately marches to rally.
                if (_units.Count > 0)
                {
                    var spawned = _units[_units.Count - 1];
                    spawned.MoveTargetX = sx;
                    spawned.MoveTargetZ = sz;
                }

                TryStartNextQueued(b);
            }
        }

        private void TryStartNextQueued(SimBuilding building)
        {
            if (building.IsProducing || building.QueueCount <= 0)
                return;
            string next = building.Queue[0];
            ShiftQueue(building);
            if (!_defs.TryGetUnit(next, out var unitDef))
                return;
            float trainMult = _upgrades.TrainTimeMultiplier(building.Owner);
            building.ProductionUnitDefId = next;
            building.ProductionSecondsTotal = unitDef.TrainSeconds * trainMult;
            building.ProductionSecondsRemaining = building.ProductionSecondsTotal;
        }

        private static void ShiftQueue(SimBuilding building)
        {
            for (int i = 1; i < building.QueueCount; i++)
                building.Queue[i - 1] = building.Queue[i];
            if (building.QueueCount > 0)
            {
                building.Queue[building.QueueCount - 1] = null;
                building.QueueCount--;
            }
        }

        private void TickGather(float dt)
        {
            _gatherAcc += dt;
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive || !unit.CanGather || !unit.GatherTargetId.HasValue)
                    continue;

                if (unit.ReturningToDeposit || unit.CarryAmount >= unit.CarryCapacity)
                {
                    unit.ReturningToDeposit = true;
                    if (!TryFindDeposit(unit.Owner, out float dx, out float dz))
                        continue;
                    if (Distance(unit.X, unit.Z, dx, dz) > 10f)
                    {
                        StepTowardAvoiding(unit, dx, dz, dt);
                        continue;
                    }

                    if (unit.CarryAmount > 0 && unit.CarryType.HasValue)
                    {
                        int deposited = unit.CarryAmount;
                        _wallet.Add(unit.Owner, unit.CarryType.Value, deposited);
                        _mutationCounter ^= (ulong)deposited * 13ul;
                        _combatEvents.Add(new CombatEvent(CombatEventKind.Deposit, unit.Id, unit.X, unit.Z, false));
                        unit.CarryAmount = 0;
                        unit.CarryType = null;
                    }

                    unit.ReturningToDeposit = false;
                    if (!_resourcesById.TryGetValue(unit.GatherTargetId.Value.Value, out var node) || node.IsDepleted)
                    {
                        unit.GatherTargetId = null;
                        continue;
                    }

                    continue;
                }

                if (!_resourcesById.TryGetValue(unit.GatherTargetId.Value.Value, out var resource) || resource.IsDepleted)
                {
                    unit.GatherTargetId = null;
                    continue;
                }

                if (Distance(unit.X, unit.Z, resource.X, resource.Z) > 8f)
                {
                    StepTowardAvoiding(unit, resource.X, resource.Z, dt);
                    continue;
                }

                int want = Math.Max(1, (int)(unit.GatherRate * dt + 0.999f));
                int space = unit.CarryCapacity - unit.CarryAmount;
                if (space <= 0)
                {
                    unit.ReturningToDeposit = true;
                    continue;
                }

                int taken = resource.Extract(Math.Min(want, space));
                if (taken <= 0)
                {
                    unit.GatherTargetId = null;
                    continue;
                }

                unit.CarryType = resource.Type;
                unit.CarryAmount += taken;
                if (unit.CarryAmount >= unit.CarryCapacity)
                    unit.ReturningToDeposit = true;
            }
        }

        private bool TryFindDeposit(PlayerId owner, out float x, out float z)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner != owner || b.State == BuildingState.Destroyed)
                    continue;
                x = b.X;
                z = b.Z;
                return true;
            }

            x = 0f;
            z = 0f;
            return false;
        }

        private void TickMovement(float dt)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive)
                    continue;
                if (unit.GatherTargetId.HasValue)
                    continue;

                if (TickUnitTraversal(unit, dt))
                    continue;

                if (unit.AttackTargetId.HasValue)
                {
                    if (TryGetAttackTargetPosition(unit.AttackTargetId.Value, out float tx, out float tz))
                    {
                        float dist = Distance(unit.X, unit.Z, tx, tz);
                        if (dist > unit.AttackRange)
                        {
                            if (unit.Stance != UnitStance.Hold)
                            {
                                TryBeginTraversal(unit, tx, tz);
                                if (unit.ActiveTraversalLinkId >= 0)
                                {
                                    TickUnitTraversal(unit, dt);
                                    continue;
                                }

                                StepTowardAvoiding(unit, tx, tz, dt);
                            }
                        }
                    }
                    else
                    {
                        unit.AttackTargetId = null;
                    }

                    continue;
                }

                // Stance auto-acquire when idle / attack-moving / patrolling.
                if (unit.Role != UnitRole.Builder && unit.AttackDamage > 0f
                    && (unit.AttackMoving || unit.Patrolling || unit.Stance == UnitStance.Aggressive
                        || unit.Stance == UnitStance.Defensive))
                {
                    float radius = unit.AttackMoving || unit.Patrolling
                        ? MathF.Max(18f, unit.AttackRange + 10f)
                        : unit.Stance == UnitStance.Defensive
                            ? MathF.Max(12f, unit.AttackRange + 4f)
                            : MathF.Max(22f, unit.AttackRange + 14f);
                    if (unit.Stance != UnitStance.Passive && unit.Stance != UnitStance.Hold)
                        TryAcquireInRadius(unit, radius);
                    else if (unit.AttackMoving || unit.Patrolling)
                        TryAcquireInRadius(unit, radius);
                }

                if (unit.AttackTargetId.HasValue)
                    continue;

                if (unit.Stance == UnitStance.Hold)
                    continue;

                if (unit.Patrolling)
                {
                    float destX = unit.PatrolToB ? unit.PatrolBX : unit.PatrolAX;
                    float destZ = unit.PatrolToB ? unit.PatrolBZ : unit.PatrolAZ;
                    if (Distance(unit.X, unit.Z, destX, destZ) <= 0.5f)
                    {
                        unit.PatrolToB = !unit.PatrolToB;
                        destX = unit.PatrolToB ? unit.PatrolBX : unit.PatrolAX;
                        destZ = unit.PatrolToB ? unit.PatrolBZ : unit.PatrolAZ;
                        unit.MoveTargetX = destX;
                        unit.MoveTargetZ = destZ;
                    }
                    else
                    {
                        unit.MoveTargetX = destX;
                        unit.MoveTargetZ = destZ;
                    }
                }

                if (!unit.MoveTargetX.HasValue || !unit.MoveTargetZ.HasValue)
                    continue;

                float mx = unit.MoveTargetX.Value;
                float mz = unit.MoveTargetZ.Value;
                if (Distance(unit.X, unit.Z, mx, mz) <= 0.35f)
                {
                    if (!unit.Patrolling)
                    {
                        unit.MoveTargetX = null;
                        unit.MoveTargetZ = null;
                        unit.AttackMoving = false;
                    }

                    continue;
                }

                TryBeginTraversal(unit, mx, mz);
                if (unit.ActiveTraversalLinkId >= 0)
                {
                    TickUnitTraversal(unit, dt);
                    continue;
                }

                StepTowardAvoiding(unit, mx, mz, dt);
            }

            // Soft unit separation.
            for (int i = 0; i < _units.Count; i++)
            {
                var a = _units[i];
                if (!a.IsAlive || a.ActiveTraversalLinkId >= 0)
                    continue;
                for (int j = i + 1; j < _units.Count; j++)
                {
                    var b = _units[j];
                    if (!b.IsAlive)
                        continue;
                    float dx = b.X - a.X;
                    float dz = b.Z - a.Z;
                    float d2 = dx * dx + dz * dz;
                    const float minDist = 4.2f;
                    if (d2 >= minDist * minDist || d2 < 0.0001f)
                        continue;
                    float d = MathF.Sqrt(d2);
                    float push = (minDist - d) * 0.5f;
                    float nx = dx / d;
                    float nz = dz / d;
                    TrySetUnitPosition(a, a.X - nx * push * 0.5f, a.Z - nz * push * 0.5f);
                    TrySetUnitPosition(b, b.X + nx * push * 0.5f, b.Z + nz * push * 0.5f);
                }
            }
        }

        private void TickCombat(float dt)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive)
                    continue;
                if (unit.AttackCooldownRemaining > 0f)
                    unit.AttackCooldownRemaining -= dt;
                if (unit.ActiveTraversalLinkId >= 0
                    && _environment.TraversalGraph.TryGetLink(unit.ActiveTraversalLinkId, out var travelLink)
                    && !travelLink.AllowsCombat)
                    continue;
                if (unit.Role == UnitRole.Builder || unit.AttackDamage <= 0f)
                    continue;
                if (!unit.AttackTargetId.HasValue)
                    continue;
                if (!TryGetDamageable(unit.AttackTargetId.Value, out var targetUnit, out var targetBuilding, out var targetDestructible, out float tx, out float tz, out PlayerId targetOwner))
                {
                    unit.AttackTargetId = null;
                    continue;
                }

                if (targetDestructible == null && targetOwner == unit.Owner)
                {
                    unit.AttackTargetId = null;
                    continue;
                }

                if (Distance(unit.X, unit.Z, tx, tz) > unit.AttackRange)
                    continue;
                if (unit.AttackCooldownRemaining > 0f)
                    continue;

                float damage = unit.AttackDamage;
                bool isStructure = targetBuilding != null || targetDestructible != null;
                if (isStructure)
                    damage *= unit.BuildingDamageMultiplier;
                else if (targetUnit != null)
                    damage *= CombatMath.RoleMultiplier(unit.Role, targetUnit.Role);

                if (unit.ProjectileSpeed > 0.1f)
                {
                    _projectiles.Add(new SimProjectile
                    {
                        X = unit.X,
                        Z = unit.Z,
                        Speed = unit.ProjectileSpeed,
                        Damage = damage,
                        TargetId = unit.AttackTargetId.Value,
                        VsBuilding = isStructure,
                    });
                }
                else
                {
                    DealDamage(unit.AttackTargetId.Value, damage, isStructure);
                }

                unit.AttackCooldownRemaining = unit.AttackCooldown;
                _mutationCounter ^= unit.Id.Value * 19ul;
            }
        }

        private void TickTowers(float dt)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State != BuildingState.Active || b.Kind != BuildingKind.Tower || b.AttackDamage <= 0f)
                    continue;
                if (b.AttackCooldownRemaining > 0f)
                {
                    b.AttackCooldownRemaining -= dt;
                    continue;
                }

                float best = b.AttackRange * b.AttackRange;
                SimEntityId? target = null;
                for (int u = 0; u < _units.Count; u++)
                {
                    var unit = _units[u];
                    if (!unit.IsAlive || unit.Owner == b.Owner)
                        continue;
                    float dx = unit.X - b.X;
                    float dz = unit.Z - b.Z;
                    float d2 = dx * dx + dz * dz;
                    if (d2 <= best)
                    {
                        best = d2;
                        target = unit.Id;
                    }
                }

                if (!target.HasValue)
                    continue;

                _projectiles.Add(new SimProjectile
                {
                    X = b.X,
                    Z = b.Z,
                    Speed = 28f,
                    Damage = b.AttackDamage,
                    TargetId = target.Value,
                    VsBuilding = false,
                });
                b.AttackCooldownRemaining = b.AttackCooldown;
            }
        }

        private void TickProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var p = _projectiles[i];
                if (!TryGetAttackTargetPosition(p.TargetId, out float tx, out float tz))
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                float dx = tx - p.X;
                float dz = tz - p.Z;
                float dist = MathF.Sqrt(dx * dx + dz * dz);
                float step = p.Speed * dt;
                if (dist <= step || dist < 0.001f)
                {
                    DealDamage(p.TargetId, p.Damage, p.VsBuilding);
                    _projectiles.RemoveAt(i);
                    continue;
                }

                p.X += dx / dist * step;
                p.Z += dz / dist * step;
                _projectiles[i] = p;
            }
        }

        private void TickTerritory(float dt)
        {
            for (int t = 0; t < _territories.Count; t++)
            {
                var node = _territories[t];
                var prevState = node.State;
                var prevController = node.Controller;
                float prevProgress = node.CaptureProgress;

                int[] presence = new int[8];
                for (int i = 0; i < _units.Count; i++)
                {
                    var unit = _units[i];
                    if (!unit.IsAlive)
                        continue;
                    if (Distance(unit.X, unit.Z, node.X, node.Z) > node.Radius)
                        continue;
                    if (unit.Owner.Value < presence.Length)
                        presence[unit.Owner.Value]++;
                }

                int bestPlayer = -1;
                int bestCount = 0;
                int second = 0;
                for (int p = 0; p < presence.Length; p++)
                {
                    if (presence[p] > bestCount)
                    {
                        second = bestCount;
                        bestCount = presence[p];
                        bestPlayer = p;
                    }
                    else if (presence[p] > second)
                    {
                        second = presence[p];
                    }
                }

                if (bestPlayer < 0 || bestCount == 0)
                {
                    if (node.State == TerritoryState.Contested)
                    {
                        node.CaptureProgress = Math.Max(0f, node.CaptureProgress - dt * 0.15f);
                        if (node.CaptureProgress <= 0f && !node.Controller.HasValue)
                            node.State = TerritoryState.Neutral;
                    }
                }
                else if (second > 0)
                {
                    if (node.State != TerritoryState.Contested)
                        _combatEvents.Add(new CombatEvent(CombatEventKind.CaptureContested, node.Id, node.X, node.Z, false));
                    node.State = TerritoryState.Contested;
                }
                else
                {
                    var capturer = new PlayerId((byte)bestPlayer);
                    if (node.Controller.HasValue && node.Controller.Value == capturer)
                    {
                        node.State = TerritoryState.Controlled;
                        node.CaptureProgress = 1f;
                    }
                    else
                    {
                        node.State = TerritoryState.Contested;
                        node.CaptureProgress = Math.Min(1f, node.CaptureProgress + dt * 0.25f);
                        if (node.CaptureProgress >= 1f)
                        {
                            if (prevController.HasValue && prevController.Value.Value != capturer.Value)
                                _combatEvents.Add(new CombatEvent(CombatEventKind.CaptureLost, node.Id, node.X, node.Z, false));
                            node.Controller = capturer;
                            node.State = TerritoryState.Controlled;
                            _combatEvents.Add(new CombatEvent(CombatEventKind.CaptureCompleted, node.Id, node.X, node.Z, false));
                            _mutationCounter ^= node.Id.Value * 101ul;
                        }
                    }
                }

                if (prevState != TerritoryState.Contested && node.State == TerritoryState.Contested && node.CaptureProgress > prevProgress)
                    _combatEvents.Add(new CombatEvent(CombatEventKind.CaptureStarted, node.Id, node.X, node.Z, false));
            }
        }

        private void TickTerritoryIncome(float dt)
        {
            _incomeAcc += dt;
            if (_incomeAcc < 1f)
                return;
            _incomeAcc -= 1f;
            for (int i = 0; i < _territories.Count; i++)
            {
                var node = _territories[i];
                if (!node.Controller.HasValue || node.State != TerritoryState.Controlled)
                    continue;
                _wallet.Add(node.Controller.Value, ResourceType.Gold, node.GoldPerSecondWhenControlled);
            }

            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State != BuildingState.Active || b.GoldPerSecond <= 0)
                    continue;
                _wallet.Add(b.Owner, ResourceType.Gold, b.GoldPerSecond);
            }
        }

        private void CullDead()
        {
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (_units[i].IsAlive)
                    continue;
                _unitsById.Remove(_units[i].Id.Value);
                _units.RemoveAt(i);
            }

            for (int i = _buildings.Count - 1; i >= 0; i--)
            {
                if (_buildings[i].State != BuildingState.Destroyed && _buildings[i].Health > 0f)
                    continue;
                _buildings[i].State = BuildingState.Destroyed;
                _buildingsById.Remove(_buildings[i].Id.Value);
                _buildings.RemoveAt(i);
            }

            for (int i = _destructibles.Count - 1; i >= 0; i--)
            {
                if (_destructibles[i].IsAlive)
                    continue;
                _destructiblesById.Remove(_destructibles[i].Id.Value);
                _destructibles.RemoveAt(i);
            }
        }

        private void TryAcquireInRadius(SimUnit unit, float acquire)
        {
            float acquire2 = acquire * acquire;
            float best = acquire2;
            SimEntityId? bestId = null;

            for (int i = 0; i < _units.Count; i++)
            {
                var other = _units[i];
                if (!other.IsAlive || other.Owner == unit.Owner)
                    continue;
                float dx = other.X - unit.X;
                float dz = other.Z - unit.Z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best)
                {
                    best = d2;
                    bestId = other.Id;
                }
            }

            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner == unit.Owner || b.State == BuildingState.Destroyed)
                    continue;
                float dx = b.X - unit.X;
                float dz = b.Z - unit.Z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best)
                {
                    best = d2;
                    bestId = b.Id;
                }
            }

            for (int i = 0; i < _destructibles.Count; i++)
            {
                var d = _destructibles[i];
                if (!d.IsAlive)
                    continue;
                float dx = d.X - unit.X;
                float dz = d.Z - unit.Z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best)
                {
                    best = d2;
                    bestId = d.Id;
                }
            }

            if (bestId.HasValue)
                unit.AttackTargetId = bestId;
        }

        private void DealDamage(SimEntityId targetId, float damage, bool preferBuilding)
        {
            if (preferBuilding
                && _buildingsById.TryGetValue(targetId.Value, out var preferredBuilding)
                && preferredBuilding.State != BuildingState.Destroyed)
            {
                preferredBuilding.Health -= MitigateBuildingDamage(preferredBuilding.Owner, damage);
                _combatEvents.Add(new CombatEvent(CombatEventKind.Hit, preferredBuilding.Id, preferredBuilding.X, preferredBuilding.Z, true));
                if (preferredBuilding.Health <= 0f)
                {
                    preferredBuilding.State = BuildingState.Destroyed;
                    _combatEvents.Add(new CombatEvent(CombatEventKind.Death, preferredBuilding.Id, preferredBuilding.X, preferredBuilding.Z, true));
                    OnBuildingDestroyed(preferredBuilding);
                }

                return;
            }

            if (_unitsById.TryGetValue(targetId.Value, out var targetUnit) && targetUnit.IsAlive)
            {
                float applied = CombatMath.ApplyArmor(damage, targetUnit.Armor);
                targetUnit.Health -= applied;
                _combatEvents.Add(new CombatEvent(CombatEventKind.Hit, targetUnit.Id, targetUnit.X, targetUnit.Z, false));
                if (targetUnit.Health <= 0f)
                    _combatEvents.Add(new CombatEvent(CombatEventKind.Death, targetUnit.Id, targetUnit.X, targetUnit.Z, false));
                return;
            }

            if (_destructiblesById.TryGetValue(targetId.Value, out var targetProp) && targetProp.IsAlive)
            {
                ApplyDestructibleDamage(targetProp, damage, preferBuilding ? DamageType.Siege : DamageType.Blunt);
                return;
            }

            if (_buildingsById.TryGetValue(targetId.Value, out var targetBuilding)
                && targetBuilding.State != BuildingState.Destroyed)
            {
                targetBuilding.Health -= MitigateBuildingDamage(targetBuilding.Owner, damage);
                _combatEvents.Add(new CombatEvent(CombatEventKind.Hit, targetBuilding.Id, targetBuilding.X, targetBuilding.Z, true));
                if (targetBuilding.Health <= 0f)
                {
                    targetBuilding.State = BuildingState.Destroyed;
                    _combatEvents.Add(new CombatEvent(CombatEventKind.Death, targetBuilding.Id, targetBuilding.X, targetBuilding.Z, true));
                    OnBuildingDestroyed(targetBuilding);
                }
            }
        }

        private void ApplyDestructibleDamage(SimDestructible prop, float damage, DamageType damageType)
        {
            if ((prop.Resistances & damageType) != 0)
                damage *= prop.ResistanceFactor;
            damage = CombatMath.ApplyArmor(damage, prop.Armor);
            prop.Health -= damage;
            if (prop.Health > 0f)
                prop.State = prop.Health < prop.MaxHealth * 0.5f ? DestructibleState.Damaged : DestructibleState.Intact;
            _combatEvents.Add(new CombatEvent(CombatEventKind.Hit, prop.Id, prop.X, prop.Z, false));
            if (prop.Health <= 0f)
            {
                prop.Health = 0f;
                prop.State = DestructibleState.Destroyed;
                _combatEvents.Add(new CombatEvent(CombatEventKind.WorldDestroyed, prop.Id, prop.X, prop.Z, false));
                FinalizeDestructible(prop);
            }
        }

        private void FinalizeDestructible(SimDestructible prop)
        {
            float r = prop.FootprintRadius;
            if (prop.ClearsTerrainOnDestroy)
            {
                _environment.Grid.FillWorldRect(
                    prop.X - r,
                    prop.Z - r,
                    prop.X + r,
                    prop.Z + r,
                    prop.ReplaceTerrainDefIndex);
                _environment.RebuildFeatureIndex();
            }

            if (prop.DisableTraversalOnDestroy && prop.LinkedTraversalLinkId >= 0)
            {
                _environment.TraversalGraph.SetLinkEnabled(prop.LinkedTraversalLinkId, false);
                for (int i = 0; i < _units.Count; i++)
                {
                    if (_units[i].ActiveTraversalLinkId == prop.LinkedTraversalLinkId)
                        ClearTraversal(_units[i]);
                }

                _environment.PathDirty.MarkRadius(prop.X, prop.Z, r + 8f, PathDirtyReason.BridgeDisabled);
            }
            else
            {
                _environment.PathDirty.MarkRadius(prop.X, prop.Z, r + 4f, PathDirtyReason.DestructibleCleared);
            }

            if (prop.ResourceDropType.HasValue && prop.ResourceDropAmount > 0)
            {
                AddResourceNode(
                    _ids.Next(),
                    prop.ResourceDropType.Value,
                    prop.ResourceDropAmount,
                    prop.X + 2f,
                    prop.Z + 2f);
            }

            _mutationCounter ^= prop.Id.Value * 1303ul;
        }

        private void OnBuildingDestroyed(SimBuilding building)
        {
            if (building.Kind == BuildingKind.Wall || building.Kind == BuildingKind.Tower)
            {
                _environment.PathDirty.MarkRadius(
                    building.X,
                    building.Z,
                    building.FootprintRadius + 6f,
                    PathDirtyReason.WallRemoved);
            }

            _mutationCounter ^= building.Id.Value * 1409ul;
        }

        private void AutoGatherNearbyBuilders(PlayerId owner, float x, float z)
        {
            if (!TryFindNearestResource(x, z, out var nodeId))
                return;
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive || unit.Owner != owner || !unit.CanGather)
                    continue;
                if (unit.GatherTargetId.HasValue || unit.AttackTargetId.HasValue)
                    continue;
                if (unit.MoveTargetX.HasValue || unit.Patrolling || unit.AttackMoving)
                    continue;
                float dx = unit.X - x;
                float dz = unit.Z - z;
                if (dx * dx + dz * dz > 70f * 70f)
                    continue;
                unit.GatherTargetId = nodeId;
                unit.AttackTargetId = null;
                unit.MoveTargetX = null;
                unit.MoveTargetZ = null;
            }
        }

        private bool TryFindNearestResource(float x, float z, out SimEntityId nodeId)
        {
            nodeId = default;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < _resources.Count; i++)
            {
                var r = _resources[i];
                if (r.IsDepleted)
                    continue;
                float dx = r.X - x;
                float dz = r.Z - z;
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

        private int CountOwnedAlive(SimEntityId[] ids, PlayerId owner)
        {
            int count = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (!_unitsById.TryGetValue(ids[i].Value, out var unit))
                    continue;
                if (unit.Owner == owner && unit.IsAlive)
                    count++;
            }

            return count;
        }

        private static void FormationOffset(float cx, float cz, int index, int count, out float x, out float z)
        {
            if (count <= 1)
            {
                x = cx;
                z = cz;
                return;
            }

            int cols = (int)MathF.Ceiling(MathF.Sqrt(count));
            int row = index / cols;
            int col = index % cols;
            float spacing = 5.5f;
            float ox = (col - (cols - 1) * 0.5f) * spacing;
            float oz = (row - (cols - 1) * 0.5f) * spacing;
            x = cx + ox;
            z = cz + oz;
        }

        private bool TryGetAttackTargetPosition(SimEntityId id, out float x, out float z)
        {
            if (_unitsById.TryGetValue(id.Value, out var unit) && unit.IsAlive)
            {
                x = unit.X;
                z = unit.Z;
                return true;
            }

            if (_buildingsById.TryGetValue(id.Value, out var building) && building.State != BuildingState.Destroyed)
            {
                x = building.X;
                z = building.Z;
                return true;
            }

            if (_destructiblesById.TryGetValue(id.Value, out var prop) && prop.IsAlive)
            {
                x = prop.X;
                z = prop.Z;
                return true;
            }

            x = 0f;
            z = 0f;
            return false;
        }

        private bool TryGetDamageable(
            SimEntityId id,
            out SimUnit targetUnit,
            out SimBuilding targetBuilding,
            out SimDestructible targetDestructible,
            out float x,
            out float z,
            out PlayerId owner)
        {
            targetUnit = null;
            targetBuilding = null;
            targetDestructible = null;
            if (_unitsById.TryGetValue(id.Value, out var unit) && unit.IsAlive)
            {
                targetUnit = unit;
                x = unit.X;
                z = unit.Z;
                owner = unit.Owner;
                return true;
            }

            if (_buildingsById.TryGetValue(id.Value, out var building) && building.State != BuildingState.Destroyed)
            {
                targetBuilding = building;
                x = building.X;
                z = building.Z;
                owner = building.Owner;
                return true;
            }

            if (_destructiblesById.TryGetValue(id.Value, out var prop) && prop.IsAlive)
            {
                targetDestructible = prop;
                x = prop.X;
                z = prop.Z;
                owner = new PlayerId(255); // neutral world object
                return true;
            }

            x = 0f;
            z = 0f;
            owner = default;
            return false;
        }

        private void ClearTraversal(SimUnit unit)
        {
            unit.ActiveTraversalLinkId = -1;
            unit.TraversalProgress = 0f;
            unit.TraversalForward = true;
        }

        private bool TickUnitTraversal(SimUnit unit, float dt)
        {
            if (unit.ActiveTraversalLinkId < 0)
                return false;
            if (!_environment.TraversalGraph.TryGetLink(unit.ActiveTraversalLinkId, out var link) || !link.Enabled)
            {
                ClearTraversal(unit);
                return false;
            }

            float dur = link.DurationSeconds > 0.05f ? link.DurationSeconds : 0.05f;
            unit.TraversalProgress += dt / dur;
            float t = unit.TraversalProgress;
            if (t > 1f)
                t = 1f;

            float ax = unit.TraversalForward ? link.StartX : link.EndX;
            float az = unit.TraversalForward ? link.StartZ : link.EndZ;
            float bx = unit.TraversalForward ? link.EndX : link.StartX;
            float bz = unit.TraversalForward ? link.EndZ : link.StartZ;
            // Traversal ignores terrain blockers (bridge over water, jump over gap).
            unit.X = ax + (bx - ax) * t;
            unit.Z = az + (bz - az) * t;

            if (unit.TraversalProgress >= 1f)
            {
                unit.X = bx;
                unit.Z = bz;
                ClearTraversal(unit);
            }

            return true;
        }

        private void TryBeginTraversal(SimUnit unit, float destX, float destZ)
        {
            if (unit.ActiveTraversalLinkId >= 0)
                return;
            if (!_environment.TraversalGraph.TryFindLinkForMove(
                    unit.X,
                    unit.Z,
                    destX,
                    destZ,
                    unit.TraversalCapabilities,
                    approachRadius: 0f,
                    out var link,
                    out bool forward))
                return;

            float approachX = forward ? link.StartX : link.EndX;
            float approachZ = forward ? link.StartZ : link.EndZ;
            if (Distance(unit.X, unit.Z, approachX, approachZ) > link.ApproachRadius)
                return;

            unit.ActiveTraversalLinkId = link.Id;
            unit.TraversalForward = forward;
            unit.TraversalProgress = 0f;
            _mutationCounter ^= (ulong)(link.Id + 1) * 911ul;
        }

        private void StepTowardAvoiding(SimUnit unit, float tx, float tz, float dt)
        {
            float dx = tx - unit.X;
            float dz = tz - unit.Z;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len <= 0.0001f)
                return;
            float nx = dx / len;
            float nz = dz / len;

            // Steer away from building footprints.
            float steerX = 0f;
            float steerZ = 0f;
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State == BuildingState.Destroyed)
                    continue;
                float bx = unit.X - b.X;
                float bz = unit.Z - b.Z;
                float dist = MathF.Sqrt(bx * bx + bz * bz);
                float avoid = b.FootprintRadius + 4f;
                if (dist < avoid && dist > 0.001f)
                {
                    float strength = (avoid - dist) / avoid;
                    steerX += (bx / dist) * strength;
                    steerZ += (bz / dist) * strength;
                }
            }

            for (int i = 0; i < _destructibles.Count; i++)
            {
                var d = _destructibles[i];
                if (!d.IsAlive || !d.BlocksMovement)
                    continue;
                float bx = unit.X - d.X;
                float bz = unit.Z - d.Z;
                float dist = MathF.Sqrt(bx * bx + bz * bz);
                float avoid = d.FootprintRadius + 3f;
                if (dist < avoid && dist > 0.001f)
                {
                    float strength = (avoid - dist) / avoid;
                    steerX += (bx / dist) * strength;
                    steerZ += (bz / dist) * strength;
                }
            }

            nx += steerX * 1.25f;
            nz += steerZ * 1.25f;
            float nlen = MathF.Sqrt(nx * nx + nz * nz);
            if (nlen > 0.0001f)
            {
                nx /= nlen;
                nz /= nlen;
            }

            float terrainMod = _environment.MovementModifier(unit.X, unit.Z, unit.TraversalCapabilities);
            if (terrainMod <= 0.0001f)
                return;

            float step = unit.MoveSpeed * terrainMod * dt;
            if (step >= len && steerX * steerX + steerZ * steerZ < 0.01f)
            {
                TrySetUnitPosition(unit, tx, tz);
                return;
            }

            TrySetUnitPosition(unit, unit.X + nx * step, unit.Z + nz * step);
        }

        /// <summary>
        /// Applies a position if the cell is traversable for the unit; otherwise leaves the unit in place.
        /// Records snow footprints when depth is present (ring buffer — no GameObjects).
        /// </summary>
        private bool TrySetUnitPosition(SimUnit unit, float x, float z)
        {
            if (!IsInsidePlayable(x, z))
                return false;
            if (!_environment.CanUnitEnter(x, z, unit.TraversalCapabilities))
                return false;

            float oldX = unit.X;
            float oldZ = unit.Z;
            unit.X = x;
            unit.Z = z;

            if (_environment.Grid.TryGetCell(x, z, out var cell) && cell.SnowDepth01 > 24)
            {
                float moved2 = (x - oldX) * (x - oldX) + (z - oldZ) * (z - oldZ);
                if (moved2 > 0.15f * 0.15f)
                    _environment.WeatherSim.Footprints.Add(x, z, (byte)Math.Min(255, 80 + cell.SnowDepth01 / 2));
            }

            return true;
        }

        private bool HasNearbyBuilder(PlayerId owner, float x, float z, float builderPlaceRadius)
        {
            float r2 = builderPlaceRadius * builderPlaceRadius;
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive || unit.Owner != owner)
                    continue;
                if (!_defs.TryGetUnit(unit.DefinitionId, out var def) || !def.IsBuilder)
                    continue;
                float dx = unit.X - x;
                float dz = unit.Z - z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }

        private static bool IsInsidePlayable(float x, float z)
        {
            float half = MapBounds.PlayableHalfExtent;
            return x >= -half && x <= half && z >= -half && z <= half;
        }

        private static float Distance(float ax, float az, float bx, float bz)
        {
            float dx = bx - ax;
            float dz = bz - az;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        private static bool Contains(string[] ids, string id)
        {
            if (ids == null)
                return false;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == id)
                    return true;
            }

            return false;
        }

        private FactionId ResolveFaction(PlayerId player)
        {
            return new FactionId(player.Value);
        }

        private void RebuildSnapshots()
        {
            _unitSnapshots.Clear();
            for (int i = 0; i < _units.Count; i++)
                _unitSnapshots.Add(_units[i].ToSnapshot());

            _buildingSnapshots.Clear();
            for (int i = 0; i < _buildings.Count; i++)
                _buildingSnapshots.Add(_buildings[i].ToSnapshot());

            _territorySnapshots.Clear();
            for (int i = 0; i < _territories.Count; i++)
                _territorySnapshots.Add(_territories[i].ToSnapshot());

            _resourceSnapshots.Clear();
            for (int i = 0; i < _resources.Count; i++)
                _resourceSnapshots.Add(_resources[i].ToSnapshot());

            _destructibleSnapshots.Clear();
            for (int i = 0; i < _destructibles.Count; i++)
                _destructibleSnapshots.Add(_destructibles[i].ToSnapshot());

            _projectileSnapshots.Clear();
            for (int i = 0; i < _projectiles.Count; i++)
            {
                var p = _projectiles[i];
                float tx = p.X;
                float tz = p.Z;
                if (TryGetAttackTargetPosition(p.TargetId, out float ax, out float az))
                {
                    tx = ax;
                    tz = az;
                }

                _projectileSnapshots.Add(new ProjectileSnapshot(p.X, p.Z, tx, tz));
            }
        }
    }
}
