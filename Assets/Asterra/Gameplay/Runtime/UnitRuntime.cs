using UnityEngine;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class UnitRuntime : MonoBehaviour, IUnit
    {
        [SerializeField] private UnitDefinition definition;

        public EntityId Id { get; private set; }
        public PlayerId Owner { get; private set; }
        public FactionId Faction { get; private set; }
        public string DefinitionId => definition != null ? definition.Id : string.Empty;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public UnitStance Stance { get; private set; } = UnitStance.Aggressive;
        public bool IsAlive => Health > 0f;

        public void Initialize(EntityId id, PlayerId owner, FactionId faction, UnitDefinition def)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            definition = def;
            MaxHealth = def.MaxHealth;
            Health = def.MaxHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive)
                return;
            Health = Mathf.Max(0f, Health - amount);
        }

        public void SetStance(UnitStance stance) => Stance = stance;
    }
}
