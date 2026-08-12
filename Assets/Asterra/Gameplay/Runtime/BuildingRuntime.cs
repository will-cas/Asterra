using UnityEngine;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class BuildingRuntime : MonoBehaviour, IBuilding
    {
        [SerializeField] private BuildingDefinition definition;

        public EntityId Id { get; private set; }
        public PlayerId Owner { get; private set; }
        public FactionId Faction { get; private set; }
        public string DefinitionId => definition != null ? definition.Id : string.Empty;
        public BuildingState State { get; private set; } = BuildingState.Constructing;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool CanProduce => State == BuildingState.Active && definition != null && definition.CanProduce;

        public void Initialize(EntityId id, PlayerId owner, FactionId faction, BuildingDefinition def)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            definition = def;
            MaxHealth = def.MaxHealth;
            Health = def.MaxHealth;
            State = BuildingState.Constructing;
        }

        public void SetState(BuildingState state) => State = state;

        public void ApplyDamage(float amount)
        {
            if (State == BuildingState.Destroyed)
                return;
            Health = Mathf.Max(0f, Health - amount);
            if (Health <= 0f)
                State = BuildingState.Destroyed;
        }
    }
}
