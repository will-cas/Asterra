using UnityEngine;
using Asterra.Core;
using Asterra.Core.World;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Unit Definition", fileName = "Unit_")]
    public sealed class UnitDefinition : ScriptableObject
    {
        public string Id = "unit_id";
        public string DisplayName = "Unit";
        public FactionDefinition Faction;
        public float MaxHealth = 100f;
        public float MoveSpeed = 4f;
        public float AttackDamage = 10f;
        public float AttackRange = 2f;
        public float AttackCooldown = 1f;
        public int GoldCost = 50;
        public float TrainSeconds = 5f;
        public bool IsBuilder;
        public bool CanGather;
        public int CarryCapacity = 10;
        public float GatherRate = 4f;
        public UnitRole Role = UnitRole.Infantry;
        public float BuildingDamageMultiplier = 1f;
        public float Armor;
        public float ProjectileSpeed;
        public TraversalCapability TraversalCapabilities = TraversalCapability.Land;
        public float SightRadius = 110f;
        public GameObject PresentationPrefab;

        public UnitDefData ToData()
        {
            return new UnitDefData
            {
                Id = Id,
                DisplayName = DisplayName,
                MaxHealth = MaxHealth,
                MoveSpeed = MoveSpeed,
                AttackDamage = AttackDamage,
                AttackRange = AttackRange,
                AttackCooldown = AttackCooldown,
                GoldCost = GoldCost,
                TrainSeconds = TrainSeconds,
                IsBuilder = IsBuilder,
                CanGather = CanGather,
                CarryCapacity = CarryCapacity,
                GatherRate = GatherRate,
                Role = Role,
                BuildingDamageMultiplier = BuildingDamageMultiplier,
                Armor = Armor,
                ProjectileSpeed = ProjectileSpeed,
                TraversalCapabilities = TraversalCapabilities,
                SightRadius = SightRadius,
            };
        }
    }
}
