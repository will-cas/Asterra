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
        public readonly bool IsGarrisoned;
        public readonly float SightRadius;

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
            UnitStance stance,
            bool isGarrisoned = false,
            float sightRadius = 110f)
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
            IsGarrisoned = isGarrisoned;
            SightRadius = sightRadius;
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
        public readonly byte WallLinks;
        public readonly int GarrisonCount;
        public readonly int GarrisonCapacity;
        public readonly bool AllowsGarrison;

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
            BuildingKind kind,
            byte wallLinks = 0,
            int garrisonCount = 0,
            int garrisonCapacity = 0,
            bool allowsGarrison = false)
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
            WallLinks = wallLinks;
            GarrisonCount = garrisonCount;
            GarrisonCapacity = garrisonCapacity;
            AllowsGarrison = allowsGarrison;
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

    public readonly struct DestructibleSnapshot
    {
        public readonly SimEntityId Id;
        public readonly string DefinitionId;
        public readonly float X;
        public readonly float Z;
        public readonly float Health;
        public readonly float MaxHealth;
        public readonly Asterra.Core.World.DestructibleState State;
        public readonly float FootprintRadius;
        public readonly int LinkedTraversalLinkId;

        public DestructibleSnapshot(
            SimEntityId id,
            string definitionId,
            float x,
            float z,
            float health,
            float maxHealth,
            Asterra.Core.World.DestructibleState state,
            float footprintRadius,
            int linkedTraversalLinkId)
        {
            Id = id;
            DefinitionId = definitionId;
            X = x;
            Z = z;
            Health = health;
            MaxHealth = maxHealth;
            State = state;
            FootprintRadius = footprintRadius;
            LinkedTraversalLinkId = linkedTraversalLinkId;
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
        IReadOnlyList<DestructibleSnapshot> Destructibles { get; }
        bool HasUpgrade(PlayerId player, string upgradeDefId);
        /// <summary>Commander ability timers. Returns false if the player has no ability state.</summary>
        bool TryGetCommanderAbilityStatus(PlayerId player, out float cooldownRemaining, out float buffRemaining);
    }

    public interface IUpgradeState
    {
        bool Has(PlayerId player, string upgradeDefId);
        bool TryUnlock(PlayerId player, string upgradeDefId, int goldCost);
    }
}
