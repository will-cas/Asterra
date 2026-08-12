using System;
using Asterra.Core;

namespace Asterra.Gameplay.Sim
{
    public sealed class SimUnit : IUnit
    {
        public SimEntityId Id { get; }
        public PlayerId Owner { get; }
        public FactionId Faction { get; }
        public string DefinitionId { get; }
        public float Health { get; set; }
        public float MaxHealth { get; }
        public UnitStance Stance { get; set; } = UnitStance.Aggressive;
        public bool IsAlive => Health > 0f;

        public float X;
        public float Z;
        public float MoveSpeed;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown;
        public float AttackCooldownRemaining;
        public bool CanGather;
        public int CarryCapacity;
        public float GatherRate;
        public float BuildingDamageMultiplier;
        public UnitRole Role;

        public float? MoveTargetX;
        public float? MoveTargetZ;
        public SimEntityId? AttackTargetId;
        public SimEntityId? GatherTargetId;
        public ResourceType? CarryType;
        public int CarryAmount;
        public bool ReturningToDeposit;
        public bool AttackMoving;

        public SimUnit(SimEntityId id, PlayerId owner, FactionId faction, UnitDefData def, float x, float z)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            DefinitionId = def.Id;
            MaxHealth = def.MaxHealth;
            Health = def.MaxHealth;
            MoveSpeed = def.MoveSpeed;
            AttackDamage = def.AttackDamage;
            AttackRange = def.AttackRange;
            AttackCooldown = def.AttackCooldown;
            CanGather = def.CanGather || def.IsBuilder;
            CarryCapacity = def.CarryCapacity > 0 ? def.CarryCapacity : 10;
            GatherRate = def.GatherRate > 0f ? def.GatherRate : 4f;
            BuildingDamageMultiplier = def.BuildingDamageMultiplier > 0f ? def.BuildingDamageMultiplier : 1f;
            Role = def.IsBuilder ? UnitRole.Builder : def.Role;
            X = x;
            Z = z;
        }

        public UnitSnapshot ToSnapshot()
        {
            bool hasCarry = CarryAmount > 0 && CarryType.HasValue;
            return new UnitSnapshot(
                Id,
                Owner,
                Faction,
                DefinitionId,
                X,
                Z,
                Health,
                MaxHealth,
                IsAlive,
                CarryAmount,
                hasCarry ? CarryType.Value : ResourceType.Gold,
                hasCarry);
        }
    }

    public sealed class SimBuilding : IBuilding
    {
        public const int MaxQueue = 4;

        public SimEntityId Id { get; }
        public PlayerId Owner { get; }
        public FactionId Faction { get; }
        public string DefinitionId { get; }
        public BuildingState State { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; }
        public bool CanProduce => State == BuildingState.Active && _canProduce;

        public float X;
        public float Z;
        public float FootprintRadius;
        public float BuildSecondsTotal;
        public float BuildSecondsRemaining;
        public string[] TrainableUnitIds;
        public string ProductionUnitDefId;
        public float ProductionSecondsRemaining;
        public float ProductionSecondsTotal;
        public readonly string[] Queue = new string[MaxQueue];
        public int QueueCount;
        public int QueueCapacity;
        public float? RallyX;
        public float? RallyZ;
        public bool IsProducing => !string.IsNullOrEmpty(ProductionUnitDefId);

        private readonly bool _canProduce;

        public SimBuilding(
            SimEntityId id,
            PlayerId owner,
            FactionId faction,
            BuildingDefData def,
            float x,
            float z,
            bool startActive)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            DefinitionId = def.Id;
            MaxHealth = def.MaxHealth;
            Health = def.MaxHealth;
            X = x;
            Z = z;
            FootprintRadius = MathF.Max(def.FootprintX, def.FootprintZ) * 0.65f;
            if (FootprintRadius < 6f)
                FootprintRadius = 6f;
            _canProduce = def.CanProduce;
            TrainableUnitIds = def.TrainableUnitIds ?? System.Array.Empty<string>();
            QueueCapacity = def.QueueCapacity > 0 ? System.Math.Min(def.QueueCapacity, MaxQueue) : 3;
            BuildSecondsTotal = def.BuildSeconds;
            if (startActive)
            {
                State = BuildingState.Active;
                BuildSecondsRemaining = 0f;
            }
            else
            {
                State = BuildingState.Constructing;
                BuildSecondsRemaining = def.BuildSeconds;
            }
        }

        public BuildingSnapshot ToSnapshot()
        {
            float prodProgress = 0f;
            if (IsProducing && ProductionSecondsTotal > 0.001f)
                prodProgress = 1f - (ProductionSecondsRemaining / ProductionSecondsTotal);
            float buildProgress = 1f;
            if (State == BuildingState.Constructing && BuildSecondsTotal > 0.001f)
                buildProgress = 1f - (BuildSecondsRemaining / BuildSecondsTotal);

            return new BuildingSnapshot(
                Id,
                Owner,
                Faction,
                DefinitionId,
                X,
                Z,
                State,
                CanProduce,
                Health,
                MaxHealth,
                ProductionUnitDefId,
                prodProgress,
                QueueCount + (IsProducing ? 1 : 0),
                QueueCount > 0 ? Queue[0] : null,
                RallyX ?? (X + 18f),
                RallyZ ?? Z,
                RallyX.HasValue,
                buildProgress);
        }
    }

    public sealed class SimTerritory : ITerritoryNode
    {
        public SimEntityId Id { get; }
        public TerritoryState State { get; set; } = TerritoryState.Neutral;
        public PlayerId? Controller { get; set; }
        public float CaptureProgress { get; set; }

        public float X;
        public float Z;
        public float Radius;
        public int GoldPerSecondWhenControlled = 5;

        public SimTerritory(SimEntityId id, float x, float z, float radius)
        {
            Id = id;
            X = x;
            Z = z;
            Radius = radius;
        }

        public TerritorySnapshot ToSnapshot()
        {
            var has = Controller.HasValue;
            return new TerritorySnapshot(
                Id,
                X,
                Z,
                Radius,
                State,
                has ? Controller.Value : new PlayerId(0),
                has,
                CaptureProgress);
        }
    }

    public sealed class SimResourceNode : IResourceNode
    {
        public SimEntityId Id { get; }
        public ResourceType Type { get; }
        public int Remaining { get; private set; }
        public bool IsDepleted => Remaining <= 0;

        public float X;
        public float Z;

        public SimResourceNode(SimEntityId id, ResourceType type, int amount, float x, float z)
        {
            Id = id;
            Type = type;
            Remaining = amount;
            X = x;
            Z = z;
        }

        public int Extract(int requested)
        {
            int taken = requested > Remaining ? Remaining : requested;
            Remaining -= taken;
            return taken;
        }

        public ResourceSnapshot ToSnapshot()
        {
            return new ResourceSnapshot(Id, Type, Remaining, X, Z);
        }
    }
}
