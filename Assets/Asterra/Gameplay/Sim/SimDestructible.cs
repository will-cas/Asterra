using Asterra.Core;
using Asterra.Core.World;

namespace Asterra.Gameplay.Sim
{
    public sealed class SimDestructible
    {
        public SimEntityId Id { get; }
        public string DefinitionId { get; }
        public float X;
        public float Z;
        public float Health { get; set; }
        public float MaxHealth { get; }
        public float Armor;
        public DamageType Resistances;
        public float ResistanceFactor;
        public DestructibleState State { get; set; }
        public float FootprintRadius;
        public bool ClearsTerrainOnDestroy;
        public ushort ReplaceTerrainDefIndex;
        public bool DisableTraversalOnDestroy;
        public int LinkedTraversalLinkId = -1;
        public float YawDegrees;
        public ResourceType? ResourceDropType;
        public int ResourceDropAmount;
        public bool BlocksMovement;
        public bool Invulnerable;

        public bool IsAlive => State == DestructibleState.Intact || State == DestructibleState.Damaged;

        public SimDestructible(
            SimEntityId id,
            DestructibleDefData def,
            float x,
            float z,
            int linkedTraversalLinkId = -1,
            float yawDegrees = 0f)
        {
            Id = id;
            DefinitionId = def.Id;
            X = x;
            Z = z;
            YawDegrees = yawDegrees;
            MaxHealth = def.MaxHealth;
            Health = def.MaxHealth;
            Armor = def.Armor;
            Resistances = def.Resistances;
            ResistanceFactor = def.ResistanceFactor > 0f ? def.ResistanceFactor : 0.5f;
            State = DestructibleState.Intact;
            FootprintRadius = def.FootprintRadius > 0f ? def.FootprintRadius : 4f;
            ClearsTerrainOnDestroy = def.ClearsTerrainOnDestroy;
            ReplaceTerrainDefIndex = def.ReplaceTerrainDefIndex;
            DisableTraversalOnDestroy = def.DisableTraversalOnDestroy;
            LinkedTraversalLinkId = linkedTraversalLinkId;
            ResourceDropType = def.ResourceDropType;
            ResourceDropAmount = def.ResourceDropAmount;
            BlocksMovement = def.BlocksMovement;
            Invulnerable = def.Invulnerable;
        }

        public DestructibleSnapshot ToSnapshot()
        {
            return new DestructibleSnapshot(
                Id,
                DefinitionId,
                X,
                Z,
                Health,
                MaxHealth,
                State,
                FootprintRadius,
                LinkedTraversalLinkId,
                YawDegrees);
        }
    }
}
