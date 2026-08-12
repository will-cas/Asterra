using System;
using System.Collections.Generic;
using Asterra.Core;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Authoritative Phase-1 skirmish sim (plain data). Presentation bridges can mirror snapshots later.
    /// </summary>
    public sealed class SkirmishWorldSim : IWorldSim
    {
        private readonly IResourceWallet _wallet;
        private readonly IIdFactory _ids;
        private readonly DefinitionRegistry _defs;
        private readonly UpgradeState _upgrades;

        private readonly List<SimUnit> _units = new();
        private readonly List<SimBuilding> _buildings = new();
        private readonly List<SimTerritory> _territories = new();
        private readonly List<SimResourceNode> _resources = new();

        private readonly List<UnitSnapshot> _unitSnapshots = new();
        private readonly List<BuildingSnapshot> _buildingSnapshots = new();
        private readonly List<TerritorySnapshot> _territorySnapshots = new();

        private readonly Dictionary<uint, SimUnit> _unitsById = new();
        private readonly Dictionary<uint, SimBuilding> _buildingsById = new();
        private readonly Dictionary<uint, SimTerritory> _territoriesById = new();

        private float _gatherAcc;
        private ulong _mutationCounter;

        public SkirmishWorldSim(IResourceWallet wallet, IIdFactory ids, DefinitionRegistry defs)
        {
            _wallet = wallet;
            _ids = ids;
            _defs = defs;
            _upgrades = new UpgradeState(wallet, defs);
        }

        public IReadOnlyList<UnitSnapshot> Units => _unitSnapshots;
        public IReadOnlyList<BuildingSnapshot> Buildings => _buildingSnapshots;
        public IReadOnlyList<TerritorySnapshot> Territories => _territorySnapshots;

        public bool HasUpgrade(PlayerId player, string upgradeDefId) => _upgrades.Has(player, upgradeDefId);

        public SimUnit SpawnUnit(SimEntityId id, PlayerId owner, FactionId faction, string unitDefId, float x, float z)
        {
            if (!_defs.TryGetUnit(unitDefId, out var def))
                throw new InvalidOperationException($"Unknown unit def '{unitDefId}'.");

            var unit = new SimUnit(id, owner, faction, def, x, z);
            unit.AttackDamage *= _upgrades.UnitDamageMultiplier(owner);
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
            _resources.Add(new SimResourceNode(id, type, amount, x, z));
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
                    default:
                        break;
                }
            }

            RebuildSnapshots();
        }

        public void Tick(float deltaSeconds)
        {
            TickConstruction(deltaSeconds);
            TickProduction(deltaSeconds);
            TickMovement(deltaSeconds);
            TickCombat(deltaSeconds);
            TickTerritory(deltaSeconds);
            TickTerritoryIncome(deltaSeconds);
            CullDead();
            RebuildSnapshots();
        }

        public ulong ComputeWorldHash()
        {
            ulong hash = 14695981039346656037ul;
            hash ^= _mutationCounter;
            hash ^= (ulong)_units.Count * 1099511628211ul;
            hash ^= (ulong)_buildings.Count * 1099511628211ul;
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                hash ^= u.Id.Value;
                hash ^= (ulong)(u.X * 100f);
                hash ^= (ulong)(u.Z * 100f);
                hash ^= (ulong)u.Health;
            }

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
            for (int i = 0; i < move.UnitIds.Length; i++)
            {
                if (!_unitsById.TryGetValue(move.UnitIds[i].Value, out var unit))
                    continue;
                if (unit.Owner != move.Issuer || !unit.IsAlive)
                    continue;
                unit.MoveTargetX = move.TargetX;
                unit.MoveTargetZ = move.TargetZ;
                unit.AttackTargetId = null;
                _mutationCounter ^= unit.Id.Value * 911ul;
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
                _mutationCounter ^= unit.Id.Value * 733ul;
            }
        }

        private void ApplyPlaceBuilding(PlaceBuildingCommand place)
        {
            if (!_defs.TryGetBuilding(place.BuildingDefId, out var def))
                return;
            if (!_wallet.TrySpend(place.Issuer, ResourceType.Gold, def.GoldCost))
                return;
            if (!_wallet.TrySpend(place.Issuer, ResourceType.Timber, def.TimberCost))
            {
                _wallet.Add(place.Issuer, ResourceType.Gold, def.GoldCost);
                return;
            }

            var faction = ResolveFaction(place.Issuer);
            SpawnBuilding(_ids.Next(), place.Issuer, faction, def.Id, place.X, place.Z, startActive: false);
            _mutationCounter ^= (ulong)def.Id.GetHashCode();
        }

        private void ApplyTrain(TrainUnitCommand train)
        {
            if (!_buildingsById.TryGetValue(train.BuildingId.Value, out var building))
                return;
            if (building.Owner != train.Issuer || !building.CanProduce || building.IsProducing)
                return;
            if (!Contains(building.TrainableUnitIds, train.UnitDefId))
                return;
            if (!_defs.TryGetUnit(train.UnitDefId, out var unitDef))
                return;
            if (!_wallet.TrySpend(train.Issuer, ResourceType.Gold, unitDef.GoldCost))
                return;

            float trainMult = _upgrades.TrainTimeMultiplier(train.Issuer);
            building.ProductionUnitDefId = train.UnitDefId;
            building.ProductionSecondsRemaining = unitDef.TrainSeconds * trainMult;
            _mutationCounter ^= building.Id.Value * 397ul;
        }

        private void ApplyCaptureOrder(CaptureTerritoryCommand capture)
        {
            // Explicit order: move all issuer units toward the node (simple Phase-1 behaviour).
            if (!_territoriesById.TryGetValue(capture.TerritoryNodeId.Value, out var node))
                return;
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (unit.Owner != capture.Issuer || !unit.IsAlive)
                    continue;
                unit.MoveTargetX = node.X;
                unit.MoveTargetZ = node.Z;
                unit.AttackTargetId = null;
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
                    b.State = BuildingState.Active;
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
                SpawnUnit(_ids.Next(), b.Owner, b.Faction, unitDefId, b.X + 3f, b.Z);
            }
        }

        private void TickMovement(float dt)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (!unit.IsAlive)
                    continue;

                if (unit.AttackTargetId.HasValue)
                {
                    if (TryGetAttackTargetPosition(unit.AttackTargetId.Value, out float tx, out float tz))
                    {
                        float dist = Distance(unit.X, unit.Z, tx, tz);
                        if (dist > unit.AttackRange)
                            StepToward(unit, tx, tz, dt);
                    }

                    continue;
                }

                if (!unit.MoveTargetX.HasValue || !unit.MoveTargetZ.HasValue)
                    continue;

                float mx = unit.MoveTargetX.Value;
                float mz = unit.MoveTargetZ.Value;
                if (Distance(unit.X, unit.Z, mx, mz) <= 0.25f)
                {
                    unit.MoveTargetX = null;
                    unit.MoveTargetZ = null;
                    continue;
                }

                StepToward(unit, mx, mz, dt);
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
                if (!unit.AttackTargetId.HasValue)
                    continue;
                if (!TryGetDamageable(unit.AttackTargetId.Value, out var applyDamage, out float tx, out float tz, out PlayerId targetOwner))
                {
                    unit.AttackTargetId = null;
                    continue;
                }

                if (targetOwner == unit.Owner)
                {
                    unit.AttackTargetId = null;
                    continue;
                }

                if (Distance(unit.X, unit.Z, tx, tz) > unit.AttackRange)
                    continue;
                if (unit.AttackCooldownRemaining > 0f)
                    continue;

                applyDamage(unit.AttackDamage);
                unit.AttackCooldownRemaining = unit.AttackCooldown;
                _mutationCounter ^= unit.Id.Value * 19ul;
            }
        }

        private void TickTerritory(float dt)
        {
            for (int t = 0; t < _territories.Count; t++)
            {
                var node = _territories[t];
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

                    continue;
                }

                if (second > 0)
                {
                    node.State = TerritoryState.Contested;
                    continue;
                }

                var capturer = new PlayerId((byte)bestPlayer);
                if (node.Controller.HasValue && node.Controller.Value == capturer)
                {
                    node.State = TerritoryState.Controlled;
                    node.CaptureProgress = 1f;
                    continue;
                }

                node.State = TerritoryState.Contested;
                node.CaptureProgress = Math.Min(1f, node.CaptureProgress + dt * 0.25f);
                if (node.CaptureProgress >= 1f)
                {
                    node.Controller = capturer;
                    node.State = TerritoryState.Controlled;
                    _mutationCounter ^= node.Id.Value * 101ul;
                }
            }
        }

        private void TickTerritoryIncome(float dt)
        {
            _gatherAcc += dt;
            if (_gatherAcc < 1f)
                return;
            _gatherAcc -= 1f;
            for (int i = 0; i < _territories.Count; i++)
            {
                var node = _territories[i];
                if (!node.Controller.HasValue || node.State != TerritoryState.Controlled)
                    continue;
                _wallet.Add(node.Controller.Value, ResourceType.Gold, node.GoldPerSecondWhenControlled);
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

            x = 0f;
            z = 0f;
            return false;
        }

        private bool TryGetDamageable(
            SimEntityId id,
            out Action<float> applyDamage,
            out float x,
            out float z,
            out PlayerId owner)
        {
            if (_unitsById.TryGetValue(id.Value, out var unit) && unit.IsAlive)
            {
                applyDamage = dmg => unit.Health -= dmg;
                x = unit.X;
                z = unit.Z;
                owner = unit.Owner;
                return true;
            }

            if (_buildingsById.TryGetValue(id.Value, out var building) && building.State != BuildingState.Destroyed)
            {
                applyDamage = dmg =>
                {
                    building.Health -= dmg;
                    if (building.Health <= 0f)
                        building.State = BuildingState.Destroyed;
                };
                x = building.X;
                z = building.Z;
                owner = building.Owner;
                return true;
            }

            applyDamage = null;
            x = 0f;
            z = 0f;
            owner = default;
            return false;
        }

        private static void StepToward(SimUnit unit, float tx, float tz, float dt)
        {
            float dx = tx - unit.X;
            float dz = tz - unit.Z;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len <= 0.0001f)
                return;
            float step = unit.MoveSpeed * dt;
            if (step >= len)
            {
                unit.X = tx;
                unit.Z = tz;
                return;
            }

            unit.X += dx / len * step;
            unit.Z += dz / len * step;
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
            // Phase-1: player 0 → faction 0, player 1 → faction 1.
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
        }
    }
}
