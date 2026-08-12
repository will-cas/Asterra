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

        public float? MoveTargetX;
        public float? MoveTargetZ;
        public SimEntityId? AttackTargetId;

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
            X = x;
            Z = z;
        }

        public UnitSnapshot ToSnapshot()
        {
            return new UnitSnapshot(Id, Owner, Faction, DefinitionId, X, Z, Health, MaxHealth, IsAlive);
        }
    }

    public sealed class SimBuilding : IBuilding
    {
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
        public float BuildSecondsRemaining;
        public string[] TrainableUnitIds;
        public string ProductionUnitDefId;
        public float ProductionSecondsRemaining;
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
            _canProduce = def.CanProduce;
            TrainableUnitIds = def.TrainableUnitIds ?? System.Array.Empty<string>();
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
            return new BuildingSnapshot(Id, Owner, Faction, DefinitionId, X, Z, State, CanProduce);
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
    }
}
