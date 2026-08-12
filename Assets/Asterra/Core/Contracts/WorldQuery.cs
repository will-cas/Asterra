using System.Collections.Generic;

namespace Asterra.Core
{
    public readonly struct UnitSnapshot
    {
        public readonly SimEntityId Id;
        public readonly PlayerId Owner;
        public readonly FactionId Faction;
        public readonly string DefinitionId;
        public readonly float X;
        public readonly float Z;
        public readonly float Health;
        public readonly float MaxHealth;
        public readonly bool IsAlive;
        public readonly int CarryAmount;
        public readonly ResourceType CarryType;
        public readonly bool HasCarry;
        public readonly bool IsIdle;
        public readonly UnitStance Stance;

        public UnitSnapshot(
            SimEntityId id,
            PlayerId owner,
            FactionId faction,
            string definitionId,
            float x,
            float z,
            float health,
            float maxHealth,
            bool isAlive,
            int carryAmount,
            ResourceType carryType,
            bool hasCarry,
            bool isIdle,
            UnitStance stance)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            DefinitionId = definitionId;
            X = x;
            Z = z;
            Health = health;
            MaxHealth = maxHealth;
            IsAlive = isAlive;
            CarryAmount = carryAmount;
            CarryType = carryType;
            HasCarry = hasCarry;
            IsIdle = isIdle;
            Stance = stance;
        }
    }

    public readonly struct BuildingSnapshot
    {
        public readonly SimEntityId Id;
        public readonly PlayerId Owner;
        public readonly FactionId Faction;
        public readonly string DefinitionId;
        public readonly float X;
        public readonly float Z;
        public readonly BuildingState State;
        public readonly bool CanProduce;
        public readonly float Health;
        public readonly float MaxHealth;
        public readonly string ProductionUnitDefId;
        public readonly float ProductionProgress;
        public readonly int QueueCount;
        public readonly string QueuedUnitDefId;
        public readonly string Queue1DefId;
        public readonly string Queue2DefId;
        public readonly string Queue3DefId;
        public readonly float RallyX;
        public readonly float RallyZ;
        public readonly bool HasRally;
        public readonly float BuildProgress;
        public readonly float SightRadius;
        public readonly BuildingKind Kind;

        public BuildingSnapshot(
            SimEntityId id,
            PlayerId owner,
            FactionId faction,
            string definitionId,
            float x,
            float z,
            BuildingState state,
            bool canProduce,
            float health,
            float maxHealth,
            string productionUnitDefId,
            float productionProgress,
            int queueCount,
            string queuedUnitDefId,
            string queue1DefId,
            string queue2DefId,
            string queue3DefId,
            float rallyX,
            float rallyZ,
            bool hasRally,
            float buildProgress,
            float sightRadius,
            BuildingKind kind)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            DefinitionId = definitionId;
            X = x;
            Z = z;
            State = state;
            CanProduce = canProduce;
            Health = health;
            MaxHealth = maxHealth;
            ProductionUnitDefId = productionUnitDefId;
            ProductionProgress = productionProgress;
            QueueCount = queueCount;
            QueuedUnitDefId = queuedUnitDefId;
            Queue1DefId = queue1DefId;
            Queue2DefId = queue2DefId;
            Queue3DefId = queue3DefId;
            RallyX = rallyX;
            RallyZ = rallyZ;
            HasRally = hasRally;
            BuildProgress = buildProgress;
            SightRadius = sightRadius;
            Kind = kind;
        }
    }

    public readonly struct TerritorySnapshot
    {
        public readonly SimEntityId Id;
        public readonly float X;
        public readonly float Z;
        public readonly float Radius;
        public readonly TerritoryState State;
        public readonly PlayerId Controller;
        public readonly bool HasController;
        public readonly float CaptureProgress;

        public TerritorySnapshot(
            SimEntityId id,
            float x,
            float z,
            float radius,
            TerritoryState state,
            PlayerId controller,
            bool hasController,
            float captureProgress)
        {
            Id = id;
            X = x;
            Z = z;
            Radius = radius;
            State = state;
            Controller = controller;
            HasController = hasController;
            CaptureProgress = captureProgress;
        }
    }

    public readonly struct ResourceSnapshot
    {
        public readonly SimEntityId Id;
        public readonly ResourceType Type;
        public readonly int Remaining;
        public readonly float X;
        public readonly float Z;

        public ResourceSnapshot(SimEntityId id, ResourceType type, int remaining, float x, float z)
        {
            Id = id;
            Type = type;
            Remaining = remaining;
            X = x;
            Z = z;
        }
    }

    public readonly struct ProjectileSnapshot
    {
        public readonly float X;
        public readonly float Z;
        public readonly float TargetX;
        public readonly float TargetZ;

        public ProjectileSnapshot(float x, float z, float targetX, float targetZ)
        {
            X = x;
            Z = z;
            TargetX = targetX;
            TargetZ = targetZ;
        }
    }

    public interface IWorldQuery
    {
        IReadOnlyList<UnitSnapshot> Units { get; }
        IReadOnlyList<BuildingSnapshot> Buildings { get; }
        IReadOnlyList<TerritorySnapshot> Territories { get; }
        IReadOnlyList<ResourceSnapshot> Resources { get; }
        IReadOnlyList<CombatEvent> CombatEvents { get; }
        IReadOnlyList<ProjectileSnapshot> Projectiles { get; }
        bool HasUpgrade(PlayerId player, string upgradeDefId);
    }

    public interface IUpgradeState
    {
        bool Has(PlayerId player, string upgradeDefId);
        bool TryUnlock(PlayerId player, string upgradeDefId, int goldCost);
    }
}
