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
        private readonly PowerState _powers;
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
        private readonly System.Collections.Generic.List<(float x, float z)> _pathScratch = new();

        private sealed class CommanderAbilityRuntime
        {
            public string PowerDefId;
            public float CooldownRemaining;
            public float BuffRemaining;
            public float ArmorBonus;
            public float MoveBonus;
            public float DamageBonus;
            public float BuildingMitigation;
            public PowerEffectKind Effect;
        }

        private readonly Dictionary<string, CommanderAbilityRuntime> _commanderAbilities = new();

        private static string AbilityKey(byte player, string powerId) => player + ":" + (powerId ?? string.Empty);

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
            _powers = new PowerState(wallet);
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

        public bool HasPower(PlayerId player, string powerDefId) => _powers.Has(player, powerDefId);

        public bool TryGetCommanderAbilityStatus(PlayerId player, out float cooldownRemaining, out float buffRemaining)
        {
            if (!TryResolvePlayerFaction(player, out var faction))
            {
                cooldownRemaining = 0f;
                buffRemaining = 0f;
                return false;
            }

            var roster = FactionDefaultContent.Get(faction);
            string primary = roster.PowerIds != null && roster.PowerIds.Length > 0
                ? roster.PowerIds[0]
                : roster.PowerId;
            return TryGetCommanderAbilityStatus(player, primary, out cooldownRemaining, out buffRemaining);
        }

        public bool TryGetCommanderAbilityStatus(
            PlayerId player,
            string powerDefId,
            out float cooldownRemaining,
            out float buffRemaining)
        {
            if (string.IsNullOrEmpty(powerDefId))
            {
                cooldownRemaining = 0f;
                buffRemaining = 0f;
                return false;
            }

            if (_commanderAbilities.TryGetValue(AbilityKey(player.Value, powerDefId), out var state))
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
            foreach (var pair in _commanderAbilities)
            {
                if (!pair.Key.StartsWith(owner.Value + ":", StringComparison.Ordinal))
                    continue;
                if (pair.Value.BuffRemaining > 0f)
                    ApplyPowerBuffToUnit(unit, pair.Value);
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
            SetBuildingBlocked(building, blocked: true);
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
                    case ApplyUnitUpgradeCommand applyUnitUpgrade:
                        ApplyUnitUpgrade(applyUnitUpgrade);
                        break;
                    case UnlockPowerCommand unlockPower:
                        ApplyUnlockPower(unlockPower);
                        break;
                    case AttachBuildingCommand attach:
                        ApplyAttachBuilding(attach);
                        break;
                    case EnterGarrisonCommand enter:
                        ApplyEnterGarrison(enter);
                        break;
                    case ExitGarrisonCommand exit:
                        ApplyExitGarrison(exit);
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
            TickResearch(deltaSeconds);
            TickGather(deltaSeconds);
            TickMovement(deltaSeconds);
            TickCombat(deltaSeconds);
            TickTowers(deltaSeconds);
            TickProjectiles(deltaSeconds);
            TickTerritory(deltaSeconds);
            TickTerritoryIncome(deltaSeconds);
            CullDead();
            RepathUnitsForDirtyRegions();
            _environment.PathDirty.Clear();
            RebuildSnapshots();
        }

        private void RepathUnitsForDirtyRegions()
        {
            var dirty = _environment.PathDirty;
            if (dirty == null || dirty.Count == 0)
                return;

            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive || unit.IsGarrisoned || !unit.MoveTargetX.HasValue)
                    continue;

                bool intersects = false;
                for (int r = 0; r < dirty.Regions.Count; r++)
                {
                    var region = dirty.Regions[r];
                    if (UnitPathIntersectsDirty(unit, region))
                    {
                        intersects = true;
                        break;
                    }
                }

                if (!intersects)
                    continue;

                AssignUnitPath(unit, unit.MoveTargetX.Value, unit.MoveTargetZ ?? unit.Z);
            }
        }

        private static bool UnitPathIntersectsDirty(SimUnit unit, PathDirtyRegion region)
        {
            if (PointInDirty(unit.X, unit.Z, region))
                return true;
            if (unit.MoveTargetX.HasValue && PointInDirty(unit.MoveTargetX.Value, unit.MoveTargetZ ?? unit.Z, region))
                return true;
            for (int i = unit.PathIndex; i < unit.PathCount; i++)
            {
                if (PointInDirty(unit.PathPointsX[i], unit.PathPointsZ[i], region))
                    return true;
            }

            return false;
        }

        private static bool PointInDirty(float x, float z, PathDirtyRegion region)
        {
            const float pad = 8f;
            return x >= region.MinX - pad && x <= region.MaxX + pad
                   && z >= region.MinZ - pad && z <= region.MaxZ + pad;
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
                ClampFormationSlot(unit, ref tx, ref tz);
                index++;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = false;
                unit.Patrolling = false;
                AssignUnitPath(unit, tx, tz);
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
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = false;
                unit.Patrolling = false;
                if (TryGetAttackTargetPosition(attack.TargetId, out float tx, out float tz))
                {
                    float engage = GetEngagementRange(unit, attack.TargetId);
                    float approach = MathF.Max(2f, engage * 0.85f);
                    float dx = tx - unit.X;
                    float dz = tz - unit.Z;
                    float dist = MathF.Sqrt(dx * dx + dz * dz);
                    if (dist > engage)
                    {
                        float scale = (dist - approach) / dist;
                        AssignUnitPath(unit, unit.X + dx * scale, unit.Z + dz * scale);
                    }
                    else
                    {
                        unit.MoveTargetX = null;
                        unit.MoveTargetZ = null;
                        unit.ClearPath();
                    }
                }
                else
                {
                    unit.MoveTargetX = null;
                    unit.MoveTargetZ = null;
                    unit.ClearPath();
                }
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
                ClampFormationSlot(unit, ref tx, ref tz);
                index++;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = true;
                unit.Patrolling = false;
                AssignUnitPath(unit, tx, tz);
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
                ClampFormationSlot(unit, ref bx, ref bz);
                index++;
                unit.PatrolAX = unit.X;
                unit.PatrolAZ = unit.Z;
                unit.PatrolBX = bx;
                unit.PatrolBZ = bz;
                unit.PatrolToB = true;
                unit.Patrolling = true;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackMoving = false;
                AssignUnitPath(unit, bx, bz);
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
            // Keep turrets are attach-only (pad slots on the keep).
            if (place.BuildingDefId == FactionDefaultContent.KeepTurretId)
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

            if (OverlapsAnyBuilding(x, z, MathF.Max(def.FootprintX, def.FootprintZ) * 0.5f))
                return;

            // Foundations may be placed without a builder on site; construction only
            // advances while a builder is within ConstructionWorkRadius.
            if (!_wallet.TrySpend(place.Issuer, ResourceType.Gold, def.GoldCost))
                return;
            if (!_wallet.TrySpend(place.Issuer, ResourceType.Timber, def.TimberCost))
            {
                _wallet.Add(place.Issuer, ResourceType.Gold, def.GoldCost);
                return;
            }

            var faction = ResolveFaction(place.Issuer);
            var building = SpawnBuilding(_ids.Next(), place.Issuer, faction, def.Id, x, z, startActive: false);
            building.YawDegrees = NormalizeYaw90(place.YawDegrees);
            ApplyBuildingYawToFootprint(building);
            if (snapWall)
                RefreshWallConnectionsAround(building);
            AttractBuildersToSite(place.Issuer, x, z, BuilderAttractSearchRadius);
            _mutationCounter ^= (ulong)def.Id.GetHashCode();
        }

        private static float NormalizeYaw90(float yaw)
        {
            float y = yaw % 360f;
            if (y < 0f)
                y += 360f;
            int quarter = (int)MathF.Round(y / 90f) & 3;
            return quarter * 90f;
        }

        private static void ApplyBuildingYawToFootprint(SimBuilding building)
        {
            float yaw = building.YawDegrees;
            bool swap = MathF.Abs(yaw - 90f) < 1f || MathF.Abs(yaw - 270f) < 1f;
            if (!swap)
                return;

            float hx = building.FootprintHalfX;
            building.FootprintHalfX = building.FootprintHalfZ;
            building.FootprintHalfZ = hx;
            building.FootprintRadius = MathF.Max(building.FootprintHalfX, building.FootprintHalfZ);
            if (building.Kind == BuildingKind.Wall)
                building.FootprintRadius = MathF.Max(building.FootprintRadius, MathF.Min(building.FootprintHalfX, building.FootprintHalfZ) + 1f);
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
            if (unitDef.IsLeader && (PlayerHasLivingLeader(train.Issuer) || PlayerHasLeaderInProduction(train.Issuer)))
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

        private bool PlayerHasLivingLeader(PlayerId player)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u.Owner != player || !u.IsAlive)
                    continue;
                if (_defs.TryGetUnit(u.DefinitionId, out var def) && def.IsLeader)
                    return true;
            }

            return false;
        }

        private bool PlayerHasLeaderInProduction(PlayerId player)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner != player || b.State == BuildingState.Destroyed)
                    continue;
                if (!string.IsNullOrEmpty(b.ProductionUnitDefId)
                    && _defs.TryGetUnit(b.ProductionUnitDefId, out var producing)
                    && producing.IsLeader)
                    return true;
                for (int q = 0; q < b.QueueCount; q++)
                {
                    if (_defs.TryGetUnit(b.Queue[q], out var queued) && queued.IsLeader)
                        return true;
                }
            }

            return false;
        }

        private void ApplyCaptureOrder(CaptureTerritoryCommand capture)
        {
            if (!_territoriesById.TryGetValue(capture.TerritoryNodeId.Value, out var node))
                return;

            bool filterUnits = capture.UnitIds != null && capture.UnitIds.Length > 0;
            if (filterUnits)
            {
                for (int i = 0; i < capture.UnitIds.Length; i++)
                {
                    if (!_unitsById.TryGetValue(capture.UnitIds[i].Value, out var unit))
                        continue;
                    if (unit.Owner != capture.Issuer || !unit.IsAlive || unit.IsGarrisoned)
                        continue;
                    if (unit.Role == UnitRole.Builder)
                        continue;
                    unit.AttackTargetId = null;
                    unit.GatherTargetId = null;
                    unit.AttackMoving = true;
                    AssignUnitPath(unit, node.X, node.Z);
                }
            }
            else
            {
                for (int i = 0; i < _units.Count; i++)
                {
                    var unit = _units[i];
                    if (unit.Owner != capture.Issuer || !unit.IsAlive || unit.IsGarrisoned)
                        continue;
                    if (unit.Role == UnitRole.Builder)
                        continue;
                    unit.AttackTargetId = null;
                    unit.GatherTargetId = null;
                    unit.AttackMoving = true;
                    AssignUnitPath(unit, node.X, node.Z);
                }
            }

            _mutationCounter ^= capture.TerritoryNodeId.Value * 541ul;
        }

        private void ApplyUpgrade(ChooseUpgradeCommand upgrade)
        {
            if (!_defs.TryGetUpgrade(upgrade.UpgradeDefId, out var def))
                return;
            if (_upgrades.Has(upgrade.Issuer, def.Id))
                return;
            if (!_buildingsById.TryGetValue(upgrade.BuildingId.Value, out var building))
                return;
            if (building.Owner != upgrade.Issuer || building.State != BuildingState.Active)
                return;
            if (building.IsResearching)
                return;

            bool keepTech = def.Kind == UpgradeKind.Keep;
            bool atKeep = building.Kind == BuildingKind.Keep;
            bool atProducer = building.Kind == BuildingKind.Producer;
            if (keepTech && !atKeep)
                return;
            if (!keepTech && !atProducer && !atKeep)
                return;
            // Equipment researched at producer (or keep as fallback if no producer UI).
            if (!keepTech && !atProducer)
                return;

            if (!_wallet.TrySpend(upgrade.Issuer, ResourceType.Gold, def.GoldCost))
                return;

            float seconds = def.ResearchSeconds > 0.1f ? def.ResearchSeconds : 0.1f;
            building.ResearchUpgradeDefId = def.Id;
            building.ResearchSecondsTotal = seconds;
            building.ResearchSecondsRemaining = seconds;
            _mutationCounter ^= building.Id.Value * 773ul;
        }

        private void CompleteResearch(SimBuilding building, UpgradeDefData def)
        {
            if (!_upgrades.MarkUnlocked(building.Owner, def.Id))
                return;

            if (def.KeepHealthBonus > 0f)
                ApplyKeepHealthBonus(building.Owner, def.KeepHealthBonus);
            if (def.KeepSightBonus > 0f)
                ApplyKeepSightBonus(building.Owner, def.KeepSightBonus);

            _combatEvents.Add(new CombatEvent(CombatEventKind.ResearchComplete, building.Id, building.X, building.Z, true));
            _mutationCounter ^= (ulong)def.Id.GetHashCode();
        }

        private void ApplyKeepHealthBonus(PlayerId owner, float bonus)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner != owner || b.State == BuildingState.Destroyed || b.Kind != BuildingKind.Keep)
                    continue;
                b.MaxHealth += bonus;
                b.Health += bonus;
            }
        }

        private void ApplyKeepSightBonus(PlayerId owner, float bonus)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner != owner || b.State == BuildingState.Destroyed || b.Kind != BuildingKind.Keep)
                    continue;
                b.SightRadius += bonus;
            }
        }

        private void ApplyUnitUpgrade(ApplyUnitUpgradeCommand cmd)
        {
            if (cmd.UnitIds == null || cmd.UnitIds.Length == 0)
                return;
            if (!_defs.TryGetUpgrade(cmd.UpgradeDefId, out var def))
                return;
            if (def.Kind != UpgradeKind.Equipment)
                return;
            if (!_upgrades.Has(cmd.Issuer, def.Id))
                return;

            for (int i = 0; i < cmd.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(cmd.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != cmd.Issuer || !unit.IsAlive)
                    continue;
                if (!ApplyEquipmentToUnit(unit, def))
                    continue;
                _combatEvents.Add(new CombatEvent(CombatEventKind.UpgradeApplied, unit.Id, unit.X, unit.Z, false));
            }
        }

        private bool ApplyEquipmentToUnit(SimUnit unit, UpgradeDefData def)
        {
            if (unit.Role == UnitRole.Builder)
                return false;
            if (!unit.TryRecordEquipment(def.Id))
                return false;
            if (!_defs.TryGetUnit(unit.DefinitionId, out var baseDef))
                return false;

            unit.Armor += def.ArmorBonus;
            float dmg = baseDef.AttackDamage;
            for (int i = 0; i < unit.AppliedEquipmentCount; i++)
            {
                if (!_defs.TryGetUpgrade(unit.AppliedEquipmentIds[i], out var applied))
                    continue;
                dmg = dmg * applied.UnitDamageMultiplier + applied.AttackDamageBonus;
            }

            unit.AttackDamage = dmg + unit.CommanderDamageBonus;
            _mutationCounter ^= unit.Id.Value * 911ul;
            return true;
        }

        private void ApplyResearchedEquipmentToNewUnit(SimUnit unit)
        {
            if (unit.Role == UnitRole.Builder)
                return;
            var roster = FactionDefaultContent.Get(unit.Faction);
            var ids = roster.EquipmentUpgradeIds;
            if (ids == null)
                return;
            for (int i = 0; i < ids.Length; i++)
            {
                if (!_upgrades.Has(unit.Owner, ids[i]))
                    continue;
                if (!_defs.TryGetUpgrade(ids[i], out var def) || def.Kind != UpgradeKind.Equipment)
                    continue;
                ApplyEquipmentToUnit(unit, def);
            }
        }

        private void ApplyUnlockPower(UnlockPowerCommand cmd)
        {
            if (!_defs.TryGetPower(cmd.PowerDefId, out var def))
                return;
            if (!TryResolvePlayerFaction(cmd.Issuer, out var faction))
                return;
            if (!RosterContainsPower(FactionDefaultContent.Get(faction), def.Id))
                return;
            if (!_powers.TryUnlock(cmd.Issuer, def.Id, def.UnlockGoldCost))
                return;
            _mutationCounter ^= (ulong)def.Id.GetHashCode() * 17ul;
        }

        private static bool RosterContainsPower(FactionRoster roster, string powerId)
        {
            if (roster.PowerIds != null)
            {
                for (int i = 0; i < roster.PowerIds.Length; i++)
                {
                    if (roster.PowerIds[i] == powerId)
                        return true;
                }
            }

            return roster.PowerId == powerId;
        }

        private void ApplyAttachBuilding(AttachBuildingCommand cmd)
        {
            if (!_buildingsById.TryGetValue(cmd.ParentBuildingId.Value, out var parent))
                return;
            if (parent.Owner != cmd.Issuer || parent.State == BuildingState.Destroyed)
                return;
            if (parent.Kind != BuildingKind.Keep || parent.AttachmentSlotCount <= 0)
                return;
            if (cmd.SlotIndex >= parent.AttachmentSlotCount)
                return;
            if (parent.AttachmentOccupantIds[cmd.SlotIndex] != 0)
                return;
            if (!_defs.TryGetBuilding(cmd.BuildingDefId, out var def))
                return;
            if (!Contains(parent.AttachmentAllowedBuildingIds, cmd.BuildingDefId))
                return;

            GetAttachmentWorldPos(parent, cmd.SlotIndex, out float x, out float z);
            if (!IsInsidePlayable(x, z))
                return;
            if (!_wallet.TrySpend(cmd.Issuer, ResourceType.Gold, def.GoldCost))
                return;
            if (!_wallet.TrySpend(cmd.Issuer, ResourceType.Timber, def.TimberCost))
            {
                _wallet.Add(cmd.Issuer, ResourceType.Gold, def.GoldCost);
                return;
            }

            var child = SpawnBuilding(_ids.Next(), cmd.Issuer, parent.Faction, def.Id, x, z, startActive: false);
            child.ParentBuildingId = parent.Id;
            child.AttachmentSlotIndex = cmd.SlotIndex;
            parent.AttachmentOccupantIds[cmd.SlotIndex] = child.Id.Value;
            _mutationCounter ^= child.Id.Value * 1601ul;
        }

        private static void GetAttachmentWorldPos(SimBuilding parent, byte slot, out float x, out float z)
        {
            float r = parent.AttachmentRadius > 1f ? parent.AttachmentRadius : 22f;
            switch (slot % 4)
            {
                case 0: x = parent.X; z = parent.Z + r; break;
                case 1: x = parent.X + r; z = parent.Z; break;
                case 2: x = parent.X; z = parent.Z - r; break;
                default: x = parent.X - r; z = parent.Z; break;
            }
        }

        private void ApplyCommanderAbility(ActivateCommanderAbilityCommand cmd)
        {
            if (string.IsNullOrEmpty(cmd.PowerDefId))
                return;
            if (!TryResolvePlayerFaction(cmd.Issuer, out var faction))
                return;
            var roster = FactionDefaultContent.Get(faction);
            if (!RosterContainsPower(roster, cmd.PowerDefId))
                return;
            if (!_defs.TryGetPower(cmd.PowerDefId, out var power))
                return;
            if (!_powers.Has(cmd.Issuer, power.Id))
                return;

            string key = AbilityKey(cmd.Issuer.Value, power.Id);
            if (!_commanderAbilities.TryGetValue(key, out var state))
            {
                state = new CommanderAbilityRuntime { PowerDefId = power.Id };
                _commanderAbilities[key] = state;
            }

            if (state.CooldownRemaining > 0f)
                return;

            if (state.BuffRemaining > 0f)
                ClearPowerBuff(cmd.Issuer, state);

            state.PowerDefId = power.Id;
            state.Effect = power.Effect;
            state.ArmorBonus = power.Effect == PowerEffectKind.ArmorAura ? power.EffectMagnitude : 0f;
            state.MoveBonus = power.Effect == PowerEffectKind.MoveSpeedAura ? power.EffectMagnitude : 0f;
            state.DamageBonus = power.Effect == PowerEffectKind.DamageAura ? power.EffectMagnitude : 0f;
            state.BuildingMitigation = power.BuildingMitigation;
            state.BuffRemaining = power.DurationSeconds;
            state.CooldownRemaining = power.CooldownSeconds;
            ApplyPowerBuff(cmd.Issuer, state);
            // Presentation cue at keep / army center.
            float fx = 0f;
            float fz = 0f;
            int n = 0;
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner != cmd.Issuer || b.State == BuildingState.Destroyed)
                    continue;
                if (!FactionDefaultContent.IsKeepBuildingId(b.DefinitionId))
                    continue;
                fx = b.X;
                fz = b.Z;
                n = 1;
                break;
            }

            if (n == 0)
            {
                for (int i = 0; i < _units.Count; i++)
                {
                    var u = _units[i];
                    if (!u.IsAlive || u.Owner != cmd.Issuer)
                        continue;
                    fx += u.X;
                    fz += u.Z;
                    n++;
                }

                if (n > 0)
                {
                    fx /= n;
                    fz /= n;
                }
            }

            _combatEvents.Add(new CombatEvent(CombatEventKind.PowerActivated, default, fx, fz, false, cmd.Issuer.Value));
            _mutationCounter ^= 0xA11CEUL ^ (ulong)(cmd.Issuer.Value + 1) * 97ul;
        }

        private void TickCommanderAbilities(float dt)
        {
            if (_commanderAbilities.Count == 0)
                return;

            foreach (var pair in _commanderAbilities)
            {
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
                        byte player = 0;
                        int colon = pair.Key.IndexOf(':');
                        if (colon > 0 && byte.TryParse(pair.Key.Substring(0, colon), out var p))
                            player = p;
                        ClearPowerBuff(new PlayerId(player), state);
                        state.ArmorBonus = 0f;
                        state.MoveBonus = 0f;
                        state.DamageBonus = 0f;
                        state.BuildingMitigation = 0f;
                    }

                    changed = true;
                }

                if (changed)
                    _mutationCounter ^= (ulong)pair.Key.GetHashCode();
            }
        }

        private void ApplyPowerBuff(PlayerId owner, CommanderAbilityRuntime state)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (unit.Owner != owner || !unit.IsAlive)
                    continue;
                ApplyPowerBuffToUnit(unit, state);
            }
        }

        private static void ApplyPowerBuffToUnit(SimUnit unit, CommanderAbilityRuntime state)
        {
            if (state.ArmorBonus > 0f && unit.CommanderArmorBonus <= 0f)
            {
                unit.Armor += state.ArmorBonus;
                unit.CommanderArmorBonus = state.ArmorBonus;
            }

            if (state.MoveBonus > 0f && unit.CommanderMoveBonus <= 0f)
            {
                unit.MoveSpeed += state.MoveBonus;
                unit.CommanderMoveBonus = state.MoveBonus;
            }

            if (state.DamageBonus > 0f && unit.CommanderDamageBonus <= 0f)
            {
                unit.AttackDamage += state.DamageBonus;
                unit.CommanderDamageBonus = state.DamageBonus;
            }
        }

        private void ClearPowerBuff(PlayerId owner, CommanderAbilityRuntime state)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (unit.Owner != owner)
                    continue;
                if (unit.CommanderArmorBonus > 0f)
                {
                    float remove = state.ArmorBonus > 0f ? state.ArmorBonus : unit.CommanderArmorBonus;
                    unit.Armor = Math.Max(0f, unit.Armor - remove);
                    unit.CommanderArmorBonus = 0f;
                }

                if (unit.CommanderMoveBonus > 0f)
                {
                    float remove = state.MoveBonus > 0f ? state.MoveBonus : unit.CommanderMoveBonus;
                    unit.MoveSpeed = Math.Max(0.5f, unit.MoveSpeed - remove);
                    unit.CommanderMoveBonus = 0f;
                }

                if (unit.CommanderDamageBonus > 0f)
                {
                    float remove = state.DamageBonus > 0f ? state.DamageBonus : unit.CommanderDamageBonus;
                    unit.AttackDamage = Math.Max(0f, unit.AttackDamage - remove);
                    unit.CommanderDamageBonus = 0f;
                }
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
            foreach (var pair in _commanderAbilities)
            {
                if (!pair.Key.StartsWith(owner.Value + ":", StringComparison.Ordinal))
                    continue;
                if (pair.Value.BuffRemaining > 0f && pair.Value.BuildingMitigation > 0f)
                    return true;
            }

            return false;
        }

        private float MitigateBuildingDamage(PlayerId owner, float damage)
        {
            float mitigation = 0f;
            foreach (var pair in _commanderAbilities)
            {
                if (!pair.Key.StartsWith(owner.Value + ":", StringComparison.Ordinal))
                    continue;
                if (pair.Value.BuffRemaining > 0f && pair.Value.BuildingMitigation > mitigation)
                    mitigation = pair.Value.BuildingMitigation;
            }

            if (mitigation <= 0f)
                return damage;
            return CombatMath.ApplyArmor(damage, mitigation);
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

        /// <summary>Max distance from building center for a builder to advance construction.</summary>
        public const float ConstructionWorkRadius = 16f;

        /// <summary>Search range used when auto-pulling idle/gathering builders to a site.</summary>
        public const float BuilderAttractSearchRadius = 220f;

        private void TickConstruction(float dt)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State != BuildingState.Constructing)
                    continue;

                // Construction only advances while a builder is on site.
                if (!HasNearbyBuilder(b.Owner, b.X, b.Z, ConstructionWorkRadius))
                {
                    AttractBuildersToSite(b.Owner, b.X, b.Z, BuilderAttractSearchRadius);
                    continue;
                }

                b.BuildSecondsRemaining -= dt;
                if (b.BuildSecondsRemaining <= 0f)
                {
                    b.State = BuildingState.Active;
                    SetBuildingBlocked(b, true);
                    _combatEvents.Add(new CombatEvent(CombatEventKind.BuildComplete, b.Id, b.X, b.Z, true));
                    AutoGatherNearbyBuilders(b.Owner, b.X, b.Z);
                }
            }
        }

        /// <summary>Send builders toward a constructing site so they can work.</summary>
        private void AttractBuildersToSite(PlayerId owner, float x, float z, float searchRadius)
        {
            float r2 = searchRadius * searchRadius;
            float workR2 = ConstructionWorkRadius * ConstructionWorkRadius;
            int sent = 0;
            for (int i = 0; i < _units.Count && sent < 2; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive || unit.Owner != owner || unit.IsGarrisoned)
                    continue;
                if (!_defs.TryGetUnit(unit.DefinitionId, out var def) || !def.IsBuilder)
                    continue;
                if (unit.AttackTargetId.HasValue)
                    continue;
                float dx = unit.X - x;
                float dz = unit.Z - z;
                float d2 = dx * dx + dz * dz;
                if (d2 > r2)
                    continue;
                if (d2 <= workR2)
                    continue; // already on site and working

                // Pull off gather / idle so foundations do not stall forever.
                unit.GatherTargetId = null;
                unit.ReturningToDeposit = false;
                unit.AttackTargetId = null;
                unit.AttackMoving = false;
                unit.Patrolling = false;
                AssignUnitPath(unit, x, z);
                sent++;
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

                if (_defs.TryGetUnit(unitDefId, out var finishedDef)
                    && finishedDef.IsLeader
                    && PlayerHasLivingLeader(b.Owner))
                {
                    _wallet.Add(b.Owner, ResourceType.Gold, finishedDef.GoldCost);
                    TryStartNextQueued(b);
                    continue;
                }

                float sx = b.RallyX ?? (b.X + b.FootprintRadius + 8f);
                float sz = b.RallyZ ?? b.Z;
                if (!TryFindSpawnNearBuilding(b, 2.2f, out float spawnX, out float spawnZ))
                {
                    spawnX = b.X + b.FootprintRadius + 6f;
                    spawnZ = b.Z;
                }

                SpawnUnit(_ids.Next(), b.Owner, b.Faction, unitDefId, spawnX, spawnZ);
                // New unit immediately marches to rally.
                if (_units.Count > 0)
                {
                    var spawned = _units[_units.Count - 1];
                    ApplyResearchedEquipmentToNewUnit(spawned);
                    spawned.MoveTargetX = sx;
                    spawned.MoveTargetZ = sz;
                    _combatEvents.Add(new CombatEvent(CombatEventKind.TrainComplete, b.Id, spawnX, spawnZ, false));
                }

                TryStartNextQueued(b);
            }
        }

        private void TickResearch(float dt)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (!b.IsResearching)
                    continue;
                b.ResearchSecondsRemaining -= dt;
                if (b.ResearchSecondsRemaining > 0f)
                    continue;

                string upId = b.ResearchUpgradeDefId;
                b.ResearchUpgradeDefId = null;
                b.ResearchSecondsRemaining = 0f;
                b.ResearchSecondsTotal = 0f;
                if (_defs.TryGetUpgrade(upId, out var def))
                    CompleteResearch(b, def);
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
            if (unitDef.IsLeader && (PlayerHasLivingLeader(building.Owner) || PlayerHasLeaderInProduction(building.Owner)))
            {
                _wallet.Add(building.Owner, ResourceType.Gold, unitDef.GoldCost);
                TryStartNextQueued(building);
                return;
            }

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
                    if (!TryFindDeposit(unit.Owner, unit.X, unit.Z, out float dx, out float dz))
                        continue;
                    if (Distance(unit.X, unit.Z, dx, dz) > 10f)
                    {
                        if (!unit.MoveTargetX.HasValue
                            || Distance(unit.MoveTargetX.Value, unit.MoveTargetZ ?? unit.Z, dx, dz) > 4f)
                            AssignUnitPath(unit, dx, dz);
                        else if (unit.PathCount > 0)
                            StepAlongPathOrSteer(unit, dx, dz, dt);
                        else
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
                    unit.ClearPath();
                    unit.MoveTargetX = null;
                    unit.MoveTargetZ = null;
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
                    if (!unit.MoveTargetX.HasValue
                        || Distance(unit.MoveTargetX.Value, unit.MoveTargetZ ?? unit.Z, resource.X, resource.Z) > 4f)
                        AssignUnitPath(unit, resource.X, resource.Z);
                    else if (unit.PathCount > 0)
                        StepAlongPathOrSteer(unit, resource.X, resource.Z, dt);
                    else
                        StepTowardAvoiding(unit, resource.X, resource.Z, dt);
                    continue;
                }

                unit.ClearPath();
                unit.MoveTargetX = null;
                unit.MoveTargetZ = null;
                unit.GatherProgress += unit.GatherRate * dt;
                int want = (int)unit.GatherProgress;
                if (want <= 0)
                    continue;
                unit.GatherProgress -= want;
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

        private void StepAlongPathOrSteer(SimUnit unit, float fallbackX, float fallbackZ, float dt)
        {
            if (unit.PathCount > 0 && unit.PathIndex < unit.PathCount)
            {
                float wx = unit.PathPointsX[unit.PathIndex];
                float wz = unit.PathPointsZ[unit.PathIndex];
                if (Distance(unit.X, unit.Z, wx, wz) <= 0.6f)
                {
                    unit.PathIndex++;
                    if (unit.PathIndex >= unit.PathCount)
                    {
                        unit.ClearPath();
                        StepTowardAvoiding(unit, fallbackX, fallbackZ, dt);
                        return;
                    }

                    wx = unit.PathPointsX[unit.PathIndex];
                    wz = unit.PathPointsZ[unit.PathIndex];
                }

                StepTowardAvoiding(unit, wx, wz, dt);
                return;
            }

            StepTowardAvoiding(unit, fallbackX, fallbackZ, dt);
        }

        private bool TryFindDeposit(PlayerId owner, float fromX, float fromZ, out float x, out float z)
        {
            float bestKeep = float.MaxValue;
            float bestAny = float.MaxValue;
            float keepX = 0f, keepZ = 0f, anyX = 0f, anyZ = 0f;
            bool foundKeep = false, foundAny = false;

            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner != owner || b.State == BuildingState.Destroyed)
                    continue;
                // Walls/gates are not drop-offs.
                if (b.Kind == BuildingKind.Wall || b.Kind == BuildingKind.Gate)
                    continue;
                float d = Distance(fromX, fromZ, b.X, b.Z);
                float d2 = d * d;
                if (b.Kind == BuildingKind.Keep)
                {
                    if (d2 < bestKeep)
                    {
                        bestKeep = d2;
                        keepX = b.X;
                        keepZ = b.Z;
                        foundKeep = true;
                    }
                }
                else if (b.State == BuildingState.Active && d2 < bestAny)
                {
                    bestAny = d2;
                    anyX = b.X;
                    anyZ = b.Z;
                    foundAny = true;
                }
            }

            if (foundKeep)
            {
                x = keepX;
                z = keepZ;
                return true;
            }

            if (foundAny)
            {
                x = anyX;
                z = anyZ;
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
                if (!unit.IsAlive || unit.IsGarrisoned)
                    continue;
                if (unit.GatherTargetId.HasValue)
                    continue;

                if (TickUnitTraversal(unit, dt))
                    continue;

                if (unit.AttackTargetId.HasValue)
                {
                    if (TryGetAttackTargetPosition(unit.AttackTargetId.Value, out float tx, out float tz))
                    {
                        float engage = GetEngagementRange(unit, unit.AttackTargetId.Value);
                        float dist = Distance(unit.X, unit.Z, tx, tz);
                        if (dist > engage)
                        {
                            if (unit.Stance != UnitStance.Hold)
                            {
                                TryBeginTraversal(unit, tx, tz);
                                if (unit.ActiveTraversalLinkId >= 0)
                                {
                                    TickUnitTraversal(unit, dt);
                                    continue;
                                }

                                float approach = MathF.Max(2f, engage * 0.85f);
                                float dx = tx - unit.X;
                                float dz = tz - unit.Z;
                                float scale = (dist - approach) / dist;
                                float ax = unit.X + dx * scale;
                                float az = unit.Z + dz * scale;
                                if (!unit.MoveTargetX.HasValue
                                    || Distance(unit.MoveTargetX.Value, unit.MoveTargetZ ?? unit.Z, ax, az) > 6f)
                                    AssignUnitPath(unit, ax, az);
                                else if (unit.PathCount > 0)
                                    StepAlongPathOrSteer(unit, ax, az, dt);
                                else
                                    StepTowardAvoiding(unit, ax, az, dt);
                            }
                        }
                        else
                        {
                            unit.ClearPath();
                            unit.MoveTargetX = null;
                            unit.MoveTargetZ = null;
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
                if (unit.TryGetPathWaypoint(out float wx, out float wz))
                {
                    if (Distance(unit.X, unit.Z, wx, wz) <= 1.2f)
                    {
                        unit.PathIndex++;
                        if (!unit.TryGetPathWaypoint(out wx, out wz))
                        {
                            wx = mx;
                            wz = mz;
                        }
                    }
                    else
                    {
                        mx = wx;
                        mz = wz;
                    }
                }

                if (Distance(unit.X, unit.Z, unit.MoveTargetX.Value, unit.MoveTargetZ.Value) <= 0.35f)
                {
                    if (!unit.Patrolling)
                    {
                        unit.MoveTargetX = null;
                        unit.MoveTargetZ = null;
                        unit.AttackMoving = false;
                        unit.ClearPath();
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

            // Soft unit separation using collision radii.
            for (int i = 0; i < _units.Count; i++)
            {
                var a = _units[i];
                if (!a.IsAlive || a.ActiveTraversalLinkId >= 0 || a.IsGarrisoned)
                    continue;
                for (int j = i + 1; j < _units.Count; j++)
                {
                    var b = _units[j];
                    if (!b.IsAlive || b.IsGarrisoned)
                        continue;
                    // Don't shove attackers off their melee target.
                    if (a.AttackTargetId.HasValue && a.AttackTargetId.Value.Value == b.Id.Value)
                        continue;
                    if (b.AttackTargetId.HasValue && b.AttackTargetId.Value.Value == a.Id.Value)
                        continue;
                    float dx = b.X - a.X;
                    float dz = b.Z - a.Z;
                    float d2 = dx * dx + dz * dz;
                    float minDist = a.CollisionRadius + b.CollisionRadius;
                    if (d2 >= minDist * minDist || d2 < 0.0001f)
                        continue;
                    float d = MathF.Sqrt(d2);
                    float push = (minDist - d) * 0.55f;
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
                if (!unit.IsAlive || unit.IsGarrisoned)
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

                if (Distance(unit.X, unit.Z, tx, tz) > GetEngagementRange(unit, unit.AttackTargetId.Value))
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
                    if (!unit.IsAlive || unit.Owner == b.Owner || unit.IsGarrisoned)
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
                if (!other.IsAlive || other.Owner == unit.Owner || other.IsGarrisoned)
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
                if (targetUnit.IsGarrisoned && targetUnit.GarrisonBuildingId.HasValue)
                {
                    DealDamage(targetUnit.GarrisonBuildingId.Value, damage, preferBuilding: true);
                    return;
                }

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
            SetBuildingBlocked(building, blocked: false);
            if (building.Kind == BuildingKind.Wall || building.Kind == BuildingKind.Tower || building.Kind == BuildingKind.Keep)
            {
                _environment.PathDirty.MarkRadius(
                    building.X,
                    building.Z,
                    building.FootprintRadius + 6f,
                    PathDirtyReason.WallRemoved);
            }

            // Clear parent slot if this was an attachment.
            if (building.ParentBuildingId.HasValue
                && _buildingsById.TryGetValue(building.ParentBuildingId.Value.Value, out var parent)
                && building.AttachmentSlotIndex < parent.AttachmentSlotCount
                && parent.AttachmentOccupantIds[building.AttachmentSlotIndex] == building.Id.Value)
            {
                parent.AttachmentOccupantIds[building.AttachmentSlotIndex] = 0;
            }

            // Destroy keep attachments with the keep.
            if (building.AttachmentSlotCount > 0)
            {
                for (int i = 0; i < building.AttachmentSlotCount; i++)
                {
                    uint childId = building.AttachmentOccupantIds[i];
                    if (childId == 0)
                        continue;
                    building.AttachmentOccupantIds[i] = 0;
                    if (_buildingsById.TryGetValue(childId, out var child)
                        && child.State != BuildingState.Destroyed)
                    {
                        child.State = BuildingState.Destroyed;
                        child.Health = 0f;
                        OnBuildingDestroyed(child);
                    }
                }
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

        private void ClampFormationSlot(SimUnit unit, ref float x, ref float z)
        {
            if (CanUnitOccupy(unit, x, z) && IsInsidePlayable(x, z))
                return;

            float cx = x;
            float cz = z;
            for (int ring = 1; ring <= 8; ring++)
            {
                float step = 2.5f * ring;
                for (int i = 0; i < 8; i++)
                {
                    float a = i * (MathF.PI * 0.25f);
                    float px = cx + MathF.Cos(a) * step;
                    float pz = cz + MathF.Sin(a) * step;
                    if (!IsInsidePlayable(px, pz))
                        continue;
                    if (!CanUnitOccupy(unit, px, pz))
                        continue;
                    x = px;
                    z = pz;
                    return;
                }
            }

            // Fall back to center of click if no slot found.
            x = cx;
            z = cz;
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

        /// <summary>
        /// Center-to-center range that accounts for collision / footprints so melee
        /// can actually fire after hard building blocks and unit separation.
        /// </summary>
        private float GetEngagementRange(SimUnit attacker, SimEntityId targetId)
        {
            float range = attacker.AttackRange + attacker.CollisionRadius * 0.5f;
            if (_unitsById.TryGetValue(targetId.Value, out var targetUnit) && targetUnit.IsAlive)
                return range + targetUnit.CollisionRadius * 0.5f + 0.75f;

            if (_buildingsById.TryGetValue(targetId.Value, out var building)
                && building.State != BuildingState.Destroyed)
            {
                // Stand at the AABB edge (half-extent + a short melee reach).
                float edge = MathF.Max(building.FootprintHalfX, building.FootprintHalfZ);
                return attacker.AttackRange + edge + 1.25f;
            }

            if (_destructiblesById.TryGetValue(targetId.Value, out var prop) && prop.IsAlive)
                return range + prop.FootprintRadius + 1.0f;

            return range;
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


        private void AssignUnitPath(SimUnit unit, float tx, float tz)
        {
            unit.MoveTargetX = tx;
            unit.MoveTargetZ = tz;
            unit.ClearPath();
            _pathScratch.Clear();
            if (_environment.Pathfinding.TryGetPath(unit.X, unit.Z, tx, tz, unit.TraversalCapabilities, _pathScratch))
                unit.SetPath(_pathScratch);
        }

        private void ApplyEnterGarrison(EnterGarrisonCommand enter)
        {
            if (enter.UnitIds == null)
                return;
            if (!_buildingsById.TryGetValue(enter.BuildingId.Value, out var building))
                return;
            if (building.Owner != enter.Issuer || building.State == BuildingState.Destroyed || !building.AllowsGarrison)
                return;
            for (int i = 0; i < enter.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(enter.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != enter.Issuer || !unit.IsAlive || unit.IsGarrisoned)
                    continue;
                if (Distance(unit.X, unit.Z, building.X, building.Z) > building.FootprintRadius + 28f)
                    continue;
                if (!building.TryAddGarrison(unit.Id.Value))
                    break;
                unit.GarrisonBuildingId = building.Id;
                unit.MoveTargetX = null;
                unit.MoveTargetZ = null;
                unit.AttackTargetId = null;
                unit.GatherTargetId = null;
                unit.ClearPath();
                unit.ActiveTraversalLinkId = -1;
                unit.X = building.X;
                unit.Z = building.Z;
                _mutationCounter ^= unit.Id.Value * 1009ul;
            }
        }

        private void ApplyExitGarrison(ExitGarrisonCommand exit)
        {
            if (!_buildingsById.TryGetValue(exit.BuildingId.Value, out var building))
                return;
            if (building.Owner != exit.Issuer || building.GarrisonCount <= 0)
                return;
            int n = building.GarrisonCount;
            for (int i = n - 1; i >= 0; i--)
            {
                uint uid = building.GarrisonUnitIds[i];
                if (!_unitsById.TryGetValue(uid, out var unit))
                {
                    building.TryRemoveGarrison(uid);
                    continue;
                }
                building.TryRemoveGarrison(uid);
                unit.GarrisonBuildingId = null;
                float ang = i * 0.9f;
                unit.X = building.X + MathF.Cos(ang) * (building.FootprintRadius + 8f);
                unit.Z = building.Z + MathF.Sin(ang) * (building.FootprintRadius + 8f);
                unit.ClearPath();
                _mutationCounter ^= unit.Id.Value * 1013ul;
            }
        }

        /// <summary>Shared vision query for FoW / AI. Circles from owned units and buildings.</summary>
        public bool IsVisibleTo(PlayerId player, float x, float z)
        {
            float visScale = _environment.CombinedVisibility();
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (!u.IsAlive || u.Owner != player || u.IsGarrisoned)
                    continue;
                float r = u.SightRadius * visScale;
                float dx = x - u.X;
                float dz = z - u.Z;
                if (dx * dx + dz * dz <= r * r)
                    return true;
            }

            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.Owner != player || b.State == BuildingState.Destroyed)
                    continue;
                float r = (b.SightRadius > 1f ? b.SightRadius : 85f) * visScale;
                float dx = x - b.X;
                float dz = z - b.Z;
                if (dx * dx + dz * dz <= r * r)
                    return true;
            }

            return false;
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

            float steerX = 0f;
            float steerZ = 0f;
            float unitR = unit.CollisionRadius;

            // Soft push off impassable cell edges / corners before contact freeze.
            AddBlockedTerrainRepulsion(unit, ref steerX, ref steerZ);

            uint attackTarget = unit.AttackTargetId.HasValue ? unit.AttackTargetId.Value.Value : 0u;

            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State == BuildingState.Destroyed)
                    continue;
                // Don't steer away from the building we're trying to hit.
                if (attackTarget != 0u && b.Id.Value == attackTarget)
                    continue;
                float bx = unit.X - b.X;
                float bz = unit.Z - b.Z;
                float dist = MathF.Sqrt(bx * bx + bz * bz);
                float avoid = b.FootprintRadius + unitR + 2.5f;
                if (dist < avoid && dist > 0.001f)
                {
                    float strength = (avoid - dist) / avoid;
                    steerX += (bx / dist) * strength * 2.2f;
                    steerZ += (bz / dist) * strength * 2.2f;
                }
            }

            for (int i = 0; i < _destructibles.Count; i++)
            {
                var d = _destructibles[i];
                if (!d.IsAlive || !d.BlocksMovement)
                    continue;
                if (attackTarget != 0u && d.Id.Value == attackTarget)
                    continue;
                float bx = unit.X - d.X;
                float bz = unit.Z - d.Z;
                float dist = MathF.Sqrt(bx * bx + bz * bz);
                float avoid = d.FootprintRadius + unitR + 1.5f;
                if (dist < avoid && dist > 0.001f)
                {
                    float strength = (avoid - dist) / avoid;
                    steerX += (bx / dist) * strength * 1.8f;
                    steerZ += (bz / dist) * strength * 1.8f;
                }
            }

            // Light unit–unit steering so stacks don't charge into each other.
            for (int i = 0; i < _units.Count; i++)
            {
                var other = _units[i];
                if (other.Id.Value == unit.Id.Value || !other.IsAlive || other.IsGarrisoned)
                    continue;
                // Allow closing on the unit we're attacking (soft sep still applies in TickMovement).
                if (attackTarget != 0u && other.Id.Value == attackTarget)
                    continue;
                float bx = unit.X - other.X;
                float bz = unit.Z - other.Z;
                float dist = MathF.Sqrt(bx * bx + bz * bz);
                float avoid = unitR + other.CollisionRadius;
                if (dist < avoid && dist > 0.001f)
                {
                    float strength = (avoid - dist) / avoid;
                    steerX += (bx / dist) * strength * 1.1f;
                    steerZ += (bz / dist) * strength * 1.1f;
                }
            }

            nx += steerX;
            nz += steerZ;
            float nlen = MathF.Sqrt(nx * nx + nz * nz);
            if (nlen > 0.0001f)
            {
                nx /= nlen;
                nz /= nlen;
            }

            float terrainMod = _environment.MovementModifier(unit.X, unit.Z, unit.TraversalCapabilities);
            if (terrainMod <= 0.0001f)
            {
                TryUnstickFromBlocked(unit);
                return;
            }

            float step = unit.MoveSpeed * terrainMod * dt;
            if (step >= len && steerX * steerX + steerZ * steerZ < 0.04f)
            {
                if (TrySetUnitPosition(unit, tx, tz))
                    return;
                // Destination blocked (corner / footprint) — keep sliding toward it.
            }

            if (!TrySetUnitPosition(unit, unit.X + nx * step, unit.Z + nz * step))
            {
                // Axis slides first (walls), then angled slides to escape corners.
                if (!TrySetUnitPosition(unit, unit.X + nx * step, unit.Z)
                    && !TrySetUnitPosition(unit, unit.X, unit.Z + nz * step)
                    && !TryCornerSlide(unit, nx, nz, step))
                {
                    TryUnstickFromBlocked(unit);
                }
            }
        }

        /// <summary>
        /// When both axis slides fail at an obstacle corner, try diagonal/side steps
        /// so units don't freeze against impassable cell corners.
        /// </summary>
        private bool TryCornerSlide(SimUnit unit, float nx, float nz, float step)
        {
            // Rotate preferred direction by ±35°, ±70°, ±110°.
            float[] angles =
            {
                0.61f, -0.61f, 1.22f, -1.22f, 1.92f, -1.92f,
            };
            for (int i = 0; i < angles.Length; i++)
            {
                float a = angles[i];
                float ca = MathF.Cos(a);
                float sa = MathF.Sin(a);
                float sx = nx * ca - nz * sa;
                float sz = nx * sa + nz * ca;
                float len = MathF.Sqrt(sx * sx + sz * sz);
                if (len < 0.0001f)
                    continue;
                sx /= len;
                sz /= len;
                if (TrySetUnitPosition(unit, unit.X + sx * step, unit.Z + sz * step))
                    return true;
            }

            // Shorter axis nudges help when a full step still clips the blocked cell.
            float half = step * 0.45f;
            if (TrySetUnitPosition(unit, unit.X + nx * half, unit.Z)
                || TrySetUnitPosition(unit, unit.X, unit.Z + nz * half))
                return true;

            return false;
        }

        /// <summary>If somehow overlapping impassable terrain, push toward nearest open sample.</summary>
        private void TryUnstickFromBlocked(SimUnit unit)
        {
            if (CanUnitOccupy(unit, unit.X, unit.Z)
                && !OverlapsBuildingFootprint(unit.X, unit.Z, unit.CollisionRadius,
                    unit.AttackTargetId.HasValue ? unit.AttackTargetId.Value.Value : 0u))
                return;

            float bestX = unit.X;
            float bestZ = unit.Z;
            float bestScore = float.MaxValue;
            const float ring = 3.5f;
            for (int i = 0; i < 12; i++)
            {
                float a = i * (MathF.PI * 2f / 12f);
                float px = unit.X + MathF.Cos(a) * ring;
                float pz = unit.Z + MathF.Sin(a) * ring;
                if (!CanUnitOccupy(unit, px, pz))
                    continue;
                if (OverlapsBuildingFootprint(px, pz, unit.CollisionRadius,
                        unit.AttackTargetId.HasValue ? unit.AttackTargetId.Value.Value : 0u))
                    continue;
                float dx = px - unit.X;
                float dz = pz - unit.Z;
                float score = dx * dx + dz * dz;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestX = px;
                    bestZ = pz;
                }
            }

            if (bestScore < float.MaxValue)
            {
                unit.X = bestX;
                unit.Z = bestZ;
            }
        }

        /// <summary>
        /// Applies a position if terrain and footprints allow it; otherwise leaves the unit in place.
        /// </summary>
        private bool TrySetUnitPosition(SimUnit unit, float x, float z)
        {
            if (!IsInsidePlayable(x, z))
                return false;
            if (!CanUnitOccupy(unit, x, z))
                return false;

            uint ignoreBuilding = 0;
            if (unit.AttackTargetId.HasValue)
                ignoreBuilding = unit.AttackTargetId.Value.Value;
            if (OverlapsBuildingFootprint(x, z, unit.CollisionRadius, ignoreBuilding))
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

        /// <summary>
        /// Center + radius samples so bodies can't clip into blocked cell corners.
        /// </summary>
        private bool CanUnitOccupy(SimUnit unit, float x, float z)
        {
            var caps = unit.TraversalCapabilities;
            if (!_environment.CanUnitEnter(x, z, caps))
                return false;

            float r = unit.CollisionRadius * 0.55f;
            if (r < 0.35f)
                return true;

            // Four diagonal probes catch cell-corner traps the center sample misses.
            if (!_environment.CanUnitEnter(x + r, z + r, caps))
                return false;
            if (!_environment.CanUnitEnter(x + r, z - r, caps))
                return false;
            if (!_environment.CanUnitEnter(x - r, z + r, caps))
                return false;
            if (!_environment.CanUnitEnter(x - r, z - r, caps))
                return false;
            return true;
        }

        private void AddBlockedTerrainRepulsion(SimUnit unit, ref float steerX, ref float steerZ)
        {
            var caps = unit.TraversalCapabilities;
            float probe = MathF.Max(2.2f, unit.CollisionRadius * 1.4f);
            float pushX = 0f;
            float pushZ = 0f;
            int hits = 0;
            for (int i = 0; i < 8; i++)
            {
                float a = i * (MathF.PI * 0.25f);
                float px = unit.X + MathF.Cos(a) * probe;
                float pz = unit.Z + MathF.Sin(a) * probe;
                if (_environment.CanUnitEnter(px, pz, caps))
                    continue;
                // Repel opposite the blocked sample.
                pushX -= MathF.Cos(a);
                pushZ -= MathF.Sin(a);
                hits++;
            }

            if (hits == 0)
                return;
            pushX /= hits;
            pushZ /= hits;
            float plen = MathF.Sqrt(pushX * pushX + pushZ * pushZ);
            if (plen < 0.0001f)
                return;
            // Strong enough to bias slides off corners without fighting the move order.
            float strength = 0.85f + hits * 0.12f;
            steerX += (pushX / plen) * strength;
            steerZ += (pushZ / plen) * strength;
        }

        private void SetBuildingBlocked(SimBuilding building, bool blocked)
        {
            float pad = 0.5f;
            _environment.Grid.SetBlockedRect(
                building.X - building.FootprintHalfX - pad,
                building.Z - building.FootprintHalfZ - pad,
                building.X + building.FootprintHalfX + pad,
                building.Z + building.FootprintHalfZ + pad,
                blocked);
            _environment.PathDirty.MarkRadius(
                building.X,
                building.Z,
                building.FootprintRadius + 8f,
                blocked ? PathDirtyReason.WallAdded : PathDirtyReason.WallRemoved);
        }

        private bool OverlapsBuildingFootprint(float x, float z, float radius, uint ignoreBuildingId = 0)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State == BuildingState.Destroyed)
                    continue;
                if (ignoreBuildingId != 0 && b.Id.Value == ignoreBuildingId)
                    continue;
                float dx = x - b.X;
                float dz = z - b.Z;
                // Use AABB half-extents for walls/elongated footprints instead of fat circles.
                float hx = b.FootprintHalfX + radius;
                float hz = b.FootprintHalfZ + radius;
                if (MathF.Abs(dx) < hx && MathF.Abs(dz) < hz)
                    return true;
            }

            return false;
        }

        private bool OverlapsAnyBuilding(float x, float z, float footprintRadius, SimEntityId? ignore = null)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State == BuildingState.Destroyed)
                    continue;
                if (ignore.HasValue && b.Id.Value == ignore.Value.Value)
                    continue;
                float dx = x - b.X;
                float dz = z - b.Z;
                float min = b.FootprintRadius + footprintRadius;
                if (dx * dx + dz * dz < min * min)
                    return true;
            }

            return false;
        }

        private bool TryFindSpawnNearBuilding(SimBuilding building, float unitRadius, out float x, out float z)
        {
            float dist = building.FootprintRadius + unitRadius + 3f;
            for (int i = 0; i < 12; i++)
            {
                float ang = i * (MathF.PI / 6f);
                float sx = building.X + MathF.Cos(ang) * dist;
                float sz = building.Z + MathF.Sin(ang) * dist;
                if (!IsInsidePlayable(sx, sz))
                    continue;
                if (!_environment.CanUnitEnter(sx, sz, TraversalCapability.Land))
                    continue;
                if (OverlapsBuildingFootprint(sx, sz, unitRadius))
                    continue;
                x = sx;
                z = sz;
                return true;
            }

            x = building.X + dist;
            z = building.Z;
            return false;
        }

        private bool HasNearbyBuilder(PlayerId owner, float x, float z, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive || unit.Owner != owner || unit.IsGarrisoned)
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

        // --- Offline save / load -------------------------------------------------

        public void CaptureInto(Asterra.Gameplay.Save.MatchSaveData data)
        {
            if (data == null)
                return;

            data.timeOfDay01 = _environment.TimeOfDaySim.Time01;
            data.weatherKind = (int)_environment.WeatherSim.Current.Kind;
            data.weatherIntensity = _environment.WeatherSim.Current.Intensity;

            var upgrades = new System.Collections.Generic.List<string>(16);
            _upgrades.CaptureUnlocked(upgrades);
            data.unlockedUpgrades = upgrades.ToArray();
            var powers = new System.Collections.Generic.List<string>(8);
            _powers.CaptureUnlocked(powers);
            data.unlockedPowers = powers.ToArray();

            var abilities = new System.Collections.Generic.List<Asterra.Gameplay.Save.AbilitySave>(_commanderAbilities.Count);
            foreach (var pair in _commanderAbilities)
            {
                int sep = pair.Key.IndexOf(':');
                byte player = sep > 0 ? byte.Parse(pair.Key.Substring(0, sep)) : (byte)0;
                string powerId = sep >= 0 && sep + 1 < pair.Key.Length ? pair.Key.Substring(sep + 1) : pair.Value.PowerDefId;
                var a = pair.Value;
                abilities.Add(new Asterra.Gameplay.Save.AbilitySave
                {
                    player = player,
                    powerId = powerId,
                    cooldownRemaining = a.CooldownRemaining,
                    buffRemaining = a.BuffRemaining,
                    armorBonus = a.ArmorBonus,
                    moveBonus = a.MoveBonus,
                    damageBonus = a.DamageBonus,
                    buildingMitigation = a.BuildingMitigation,
                    effect = (int)a.Effect,
                });
            }

            data.abilities = abilities.ToArray();

            var units = new System.Collections.Generic.List<Asterra.Gameplay.Save.UnitSave>(_units.Count);
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (!u.IsAlive)
                    continue;
                units.Add(new Asterra.Gameplay.Save.UnitSave
                {
                    id = u.Id.Value,
                    owner = u.Owner.Value,
                    faction = u.Faction.Value,
                    definitionId = u.DefinitionId,
                    x = u.X,
                    z = u.Z,
                    health = u.Health,
                    maxHealth = u.MaxHealth,
                    stance = (int)u.Stance,
                    attackCooldownRemaining = u.AttackCooldownRemaining,
                    armor = u.Armor,
                    attackDamage = u.AttackDamage,
                    equipment0 = u.AppliedEquipmentCount > 0 ? u.AppliedEquipmentIds[0] : null,
                    equipment1 = u.AppliedEquipmentCount > 1 ? u.AppliedEquipmentIds[1] : null,
                    equipment2 = u.AppliedEquipmentCount > 2 ? u.AppliedEquipmentIds[2] : null,
                    equipment3 = u.AppliedEquipmentCount > 3 ? u.AppliedEquipmentIds[3] : null,
                    equipmentCount = u.AppliedEquipmentCount,
                    commanderArmorBonus = u.CommanderArmorBonus,
                    commanderMoveBonus = u.CommanderMoveBonus,
                    commanderDamageBonus = u.CommanderDamageBonus,
                    carryAmount = u.CarryAmount,
                    carryType = u.CarryType.HasValue ? (int)u.CarryType.Value : 0,
                    hasCarry = u.CarryAmount > 0 && u.CarryType.HasValue,
                    returningToDeposit = u.ReturningToDeposit,
                    attackMoving = u.AttackMoving,
                    patrolling = u.Patrolling,
                    patrolAX = u.PatrolAX,
                    patrolAZ = u.PatrolAZ,
                    patrolBX = u.PatrolBX,
                    patrolBZ = u.PatrolBZ,
                    patrolToB = u.PatrolToB,
                    hasMoveTarget = u.MoveTargetX.HasValue,
                    moveTargetX = u.MoveTargetX ?? 0f,
                    moveTargetZ = u.MoveTargetZ ?? 0f,
                    hasAttackTarget = u.AttackTargetId.HasValue,
                    attackTargetId = u.AttackTargetId.HasValue ? u.AttackTargetId.Value.Value : 0u,
                    hasGatherTarget = u.GatherTargetId.HasValue,
                    gatherTargetId = u.GatherTargetId.HasValue ? u.GatherTargetId.Value.Value : 0u,
                    isGarrisoned = u.IsGarrisoned,
                    garrisonBuildingId = u.GarrisonBuildingId.HasValue ? u.GarrisonBuildingId.Value.Value : 0u,
                });
            }

            data.units = units.ToArray();

            var buildings = new System.Collections.Generic.List<Asterra.Gameplay.Save.BuildingSave>(_buildings.Count);
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                if (b.State == BuildingState.Destroyed)
                    continue;
                buildings.Add(new Asterra.Gameplay.Save.BuildingSave
                {
                    id = b.Id.Value,
                    owner = b.Owner.Value,
                    faction = b.Faction.Value,
                    definitionId = b.DefinitionId,
                    x = b.X,
                    z = b.Z,
                    state = (int)b.State,
                    health = b.Health,
                    maxHealth = b.MaxHealth,
                    yawDegrees = b.YawDegrees,
                    buildSecondsRemaining = b.BuildSecondsRemaining,
                    buildSecondsTotal = b.BuildSecondsTotal,
                    productionUnitDefId = b.ProductionUnitDefId,
                    productionSecondsRemaining = b.ProductionSecondsRemaining,
                    productionSecondsTotal = b.ProductionSecondsTotal,
                    researchUpgradeDefId = b.ResearchUpgradeDefId,
                    researchSecondsRemaining = b.ResearchSecondsRemaining,
                    researchSecondsTotal = b.ResearchSecondsTotal,
                    queue0 = b.QueueCount > 0 ? b.Queue[0] : null,
                    queue1 = b.QueueCount > 1 ? b.Queue[1] : null,
                    queue2 = b.QueueCount > 2 ? b.Queue[2] : null,
                    queue3 = b.QueueCount > 3 ? b.Queue[3] : null,
                    queueCount = b.QueueCount,
                    hasRally = b.RallyX.HasValue,
                    rallyX = b.RallyX ?? b.X,
                    rallyZ = b.RallyZ ?? b.Z,
                    attackCooldownRemaining = b.AttackCooldownRemaining,
                    wallLinks = b.WallLinks,
                    hasParent = b.ParentBuildingId.HasValue,
                    parentBuildingId = b.ParentBuildingId.HasValue ? b.ParentBuildingId.Value.Value : 0u,
                    attachmentSlotIndex = b.AttachmentSlotIndex,
                    attach0 = b.AttachmentOccupantIds[0],
                    attach1 = b.AttachmentOccupantIds[1],
                    attach2 = b.AttachmentOccupantIds[2],
                    attach3 = b.AttachmentOccupantIds[3],
                    garrison0 = b.GarrisonCount > 0 ? b.GarrisonUnitIds[0] : 0u,
                    garrison1 = b.GarrisonCount > 1 ? b.GarrisonUnitIds[1] : 0u,
                    garrison2 = b.GarrisonCount > 2 ? b.GarrisonUnitIds[2] : 0u,
                    garrison3 = b.GarrisonCount > 3 ? b.GarrisonUnitIds[3] : 0u,
                    garrison4 = b.GarrisonCount > 4 ? b.GarrisonUnitIds[4] : 0u,
                    garrison5 = b.GarrisonCount > 5 ? b.GarrisonUnitIds[5] : 0u,
                    garrison6 = b.GarrisonCount > 6 ? b.GarrisonUnitIds[6] : 0u,
                    garrison7 = b.GarrisonCount > 7 ? b.GarrisonUnitIds[7] : 0u,
                    garrisonCount = b.GarrisonCount,
                });
            }

            data.buildings = buildings.ToArray();

            var territories = new Asterra.Gameplay.Save.TerritorySave[_territories.Count];
            for (int i = 0; i < _territories.Count; i++)
            {
                var t = _territories[i];
                territories[i] = new Asterra.Gameplay.Save.TerritorySave
                {
                    id = t.Id.Value,
                    x = t.X,
                    z = t.Z,
                    radius = t.Radius,
                    goldPerSecond = t.GoldPerSecondWhenControlled,
                    state = (int)t.State,
                    hasController = t.Controller.HasValue,
                    controller = t.Controller.HasValue ? t.Controller.Value.Value : (byte)0,
                    captureProgress = t.CaptureProgress,
                };
            }

            data.territories = territories;

            var resources = new Asterra.Gameplay.Save.ResourceSave[_resources.Count];
            for (int i = 0; i < _resources.Count; i++)
            {
                var r = _resources[i];
                resources[i] = new Asterra.Gameplay.Save.ResourceSave
                {
                    id = r.Id.Value,
                    type = (int)r.Type,
                    amount = r.Remaining,
                    x = r.X,
                    z = r.Z,
                };
            }

            data.resources = resources;

            var destructibles = new System.Collections.Generic.List<Asterra.Gameplay.Save.DestructibleSave>(_destructibles.Count);
            for (int i = 0; i < _destructibles.Count; i++)
            {
                var d = _destructibles[i];
                destructibles.Add(new Asterra.Gameplay.Save.DestructibleSave
                {
                    id = d.Id.Value,
                    definitionId = d.DefinitionId,
                    x = d.X,
                    z = d.Z,
                    health = d.Health,
                    state = (int)d.State,
                    linkedTraversalLinkId = d.LinkedTraversalLinkId,
                });
            }

            data.destructibles = destructibles.ToArray();
        }

        public void RestoreFrom(Asterra.Gameplay.Save.MatchSaveData data)
        {
            if (data == null)
                return;

            ClearEntitiesForRestore();

            if (data.unlockedUpgrades != null)
            {
                for (int i = 0; i < data.unlockedUpgrades.Length; i++)
                {
                    if (!TryParsePlayerKey(data.unlockedUpgrades[i], out byte p, out string id))
                        continue;
                    _upgrades.MarkUnlocked(new PlayerId(p), id);
                }
            }

            if (data.unlockedPowers != null)
            {
                for (int i = 0; i < data.unlockedPowers.Length; i++)
                {
                    if (!TryParsePlayerKey(data.unlockedPowers[i], out byte p, out string id))
                        continue;
                    _powers.MarkUnlocked(new PlayerId(p), id);
                }
            }

            _commanderAbilities.Clear();
            if (data.abilities != null)
            {
                for (int i = 0; i < data.abilities.Length; i++)
                {
                    var a = data.abilities[i];
                    if (string.IsNullOrEmpty(a.powerId))
                        continue;
                    _commanderAbilities[AbilityKey(a.player, a.powerId)] = new CommanderAbilityRuntime
                    {
                        PowerDefId = a.powerId,
                        CooldownRemaining = a.cooldownRemaining,
                        BuffRemaining = a.buffRemaining,
                        ArmorBonus = a.armorBonus,
                        MoveBonus = a.moveBonus,
                        DamageBonus = a.damageBonus,
                        BuildingMitigation = a.buildingMitigation,
                        Effect = (PowerEffectKind)a.effect,
                    };
                }
            }

            if (data.territories != null)
            {
                for (int i = 0; i < data.territories.Length; i++)
                {
                    var t = data.territories[i];
                    AddTerritory(new SimEntityId(t.id), t.x, t.z, t.radius, t.goldPerSecond);
                    if (_territoriesById.TryGetValue(t.id, out var node))
                    {
                        node.State = (TerritoryState)t.state;
                        node.CaptureProgress = t.captureProgress;
                        node.Controller = t.hasController ? new PlayerId(t.controller) : (PlayerId?)null;
                    }
                }
            }

            if (data.resources != null)
            {
                for (int i = 0; i < data.resources.Length; i++)
                {
                    var r = data.resources[i];
                    AddResourceNode(new SimEntityId(r.id), (ResourceType)r.type, r.amount, r.x, r.z);
                }
            }

            if (data.buildings != null)
            {
                for (int i = 0; i < data.buildings.Length; i++)
                {
                    var b = data.buildings[i];
                    bool startActive = b.state == (int)BuildingState.Active;
                    var building = SpawnBuilding(
                        new SimEntityId(b.id),
                        new PlayerId(b.owner),
                        new FactionId(b.faction),
                        b.definitionId,
                        b.x,
                        b.z,
                        startActive);
                    if (building == null)
                        continue;
                    building.State = (BuildingState)b.state;
                    building.Health = b.health;
                    building.MaxHealth = b.maxHealth > 0.1f ? b.maxHealth : building.MaxHealth;
                    building.YawDegrees = b.yawDegrees;
                    building.BuildSecondsRemaining = b.buildSecondsRemaining;
                    building.BuildSecondsTotal = b.buildSecondsTotal > 0.1f ? b.buildSecondsTotal : building.BuildSecondsTotal;
                    building.ProductionUnitDefId = b.productionUnitDefId;
                    building.ProductionSecondsRemaining = b.productionSecondsRemaining;
                    building.ProductionSecondsTotal = b.productionSecondsTotal;
                    building.ResearchUpgradeDefId = b.researchUpgradeDefId;
                    building.ResearchSecondsRemaining = b.researchSecondsRemaining;
                    building.ResearchSecondsTotal = b.researchSecondsTotal;
                    building.QueueCount = 0;
                    void Enqueue(string q)
                    {
                        if (string.IsNullOrEmpty(q) || building.QueueCount >= SimBuilding.MaxQueue)
                            return;
                        building.Queue[building.QueueCount++] = q;
                    }

                    Enqueue(b.queue0);
                    Enqueue(b.queue1);
                    Enqueue(b.queue2);
                    Enqueue(b.queue3);
                    if (b.queueCount > 0 && building.QueueCount == 0)
                    {
                        // queueCount may be set without strings if empty slots — ignore
                    }

                    if (b.hasRally)
                    {
                        building.RallyX = b.rallyX;
                        building.RallyZ = b.rallyZ;
                    }

                    building.AttackCooldownRemaining = b.attackCooldownRemaining;
                    building.WallLinks = b.wallLinks;
                    if (b.hasParent)
                    {
                        building.ParentBuildingId = new SimEntityId(b.parentBuildingId);
                        building.AttachmentSlotIndex = b.attachmentSlotIndex;
                    }

                    building.AttachmentOccupantIds[0] = b.attach0;
                    building.AttachmentOccupantIds[1] = b.attach1;
                    building.AttachmentOccupantIds[2] = b.attach2;
                    building.AttachmentOccupantIds[3] = b.attach3;
                    building.GarrisonCount = 0;
                    void AddGar(uint id)
                    {
                        if (id == 0 || building.GarrisonCount >= SimBuilding.MaxGarrison)
                            return;
                        building.GarrisonUnitIds[building.GarrisonCount++] = id;
                    }

                    AddGar(b.garrison0);
                    AddGar(b.garrison1);
                    AddGar(b.garrison2);
                    AddGar(b.garrison3);
                    AddGar(b.garrison4);
                    AddGar(b.garrison5);
                    AddGar(b.garrison6);
                    AddGar(b.garrison7);
                }
            }

            if (data.units != null)
            {
                for (int i = 0; i < data.units.Length; i++)
                {
                    var u = data.units[i];
                    var unit = SpawnUnit(
                        new SimEntityId(u.id),
                        new PlayerId(u.owner),
                        new FactionId(u.faction),
                        u.definitionId,
                        u.x,
                        u.z);
                    unit.Health = u.health;
                    unit.Stance = (UnitStance)u.stance;
                    unit.AttackCooldownRemaining = u.attackCooldownRemaining;
                    unit.Armor = u.armor;
                    unit.AttackDamage = u.attackDamage;
                    unit.CommanderArmorBonus = u.commanderArmorBonus;
                    unit.CommanderMoveBonus = u.commanderMoveBonus;
                    unit.CommanderDamageBonus = u.commanderDamageBonus;
                    unit.AppliedEquipmentCount = 0;
                    void AddEq(string eq)
                    {
                        if (string.IsNullOrEmpty(eq) || unit.AppliedEquipmentCount >= SimUnit.MaxAppliedEquipment)
                            return;
                        unit.AppliedEquipmentIds[unit.AppliedEquipmentCount++] = eq;
                        if (string.IsNullOrEmpty(unit.AppliedUpgradeId))
                            unit.AppliedUpgradeId = eq;
                    }

                    AddEq(u.equipment0);
                    AddEq(u.equipment1);
                    AddEq(u.equipment2);
                    AddEq(u.equipment3);
                    if (u.hasCarry)
                    {
                        unit.CarryAmount = u.carryAmount;
                        unit.CarryType = (ResourceType)u.carryType;
                    }

                    unit.ReturningToDeposit = u.returningToDeposit;
                    unit.AttackMoving = u.attackMoving;
                    unit.Patrolling = u.patrolling;
                    unit.PatrolAX = u.patrolAX;
                    unit.PatrolAZ = u.patrolAZ;
                    unit.PatrolBX = u.patrolBX;
                    unit.PatrolBZ = u.patrolBZ;
                    unit.PatrolToB = u.patrolToB;
                    if (u.hasMoveTarget)
                    {
                        unit.MoveTargetX = u.moveTargetX;
                        unit.MoveTargetZ = u.moveTargetZ;
                    }

                    if (u.hasAttackTarget)
                        unit.AttackTargetId = new SimEntityId(u.attackTargetId);
                    if (u.hasGatherTarget)
                        unit.GatherTargetId = new SimEntityId(u.gatherTargetId);
                    if (u.isGarrisoned)
                        unit.GarrisonBuildingId = new SimEntityId(u.garrisonBuildingId);
                }
            }

            if (data.destructibles != null)
            {
                for (int i = 0; i < data.destructibles.Length; i++)
                {
                    var d = data.destructibles[i];
                    var def = ResolveDestructibleDef(d.definitionId);
                    if (def == null)
                        continue;
                    var prop = SpawnDestructible(
                        new SimEntityId(d.id),
                        def,
                        d.x,
                        d.z,
                        d.linkedTraversalLinkId);
                    prop.Health = d.health;
                    prop.State = (DestructibleState)d.state;
                    if (prop.State == DestructibleState.Destroyed)
                        FinalizeDestructible(prop);
                }
            }

            _environment.TimeOfDaySim.SetTime01(data.timeOfDay01);
            if (data.weatherKind >= 0)
                _environment.WeatherSim.ForceTransitionTo((WeatherKind)data.weatherKind);

            RebuildSnapshots();
        }

        private void ClearEntitiesForRestore()
        {
            for (int i = 0; i < _buildings.Count; i++)
                SetBuildingBlocked(_buildings[i], blocked: false);

            _units.Clear();
            _unitsById.Clear();
            _buildings.Clear();
            _buildingsById.Clear();
            _territories.Clear();
            _territoriesById.Clear();
            _resources.Clear();
            _resourcesById.Clear();
            _destructibles.Clear();
            _destructiblesById.Clear();
            _projectiles.Clear();
            _combatEvents.Clear();
            _commanderAbilities.Clear();
            RebuildSnapshots();
        }

        private static bool TryParsePlayerKey(string key, out byte player, out string id)
        {
            player = 0;
            id = null;
            if (string.IsNullOrEmpty(key))
                return false;
            int sep = key.IndexOf('|');
            if (sep <= 0 || sep + 1 >= key.Length)
                return false;
            if (!byte.TryParse(key.Substring(0, sep), out player))
                return false;
            id = key.Substring(sep + 1);
            return !string.IsNullOrEmpty(id);
        }

        private DestructibleDefData ResolveDestructibleDef(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return DefaultDestructibleCatalog.Rock();
            if (definitionId.Contains("tree"))
                return DefaultDestructibleCatalog.Tree();
            if (definitionId.Contains("bridge"))
                return DefaultDestructibleCatalog.Bridge();
            return DefaultDestructibleCatalog.Rock();
        }
    }
}
