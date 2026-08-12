using Unity.Entities;

namespace Asterra.Simulation
{
    /// <summary>
    /// DOTS unit representation. Presentation stays in Gameplay via EntityId bridging.
    /// </summary>
    public struct UnitSim : IComponentData
    {
        public uint Id;
        public byte Owner;
        public byte Faction;
        public float Health;
        public float MaxHealth;
        public float MoveSpeed;
        public float AttackDamage;
        public float AttackRange;
    }

    public struct UnitPosition : IComponentData
    {
        public float X;
        public float Z;
    }

    public struct UnitVelocity : IComponentData
    {
        public float X;
        public float Z;
    }

    /// <summary>Stub system group — implement move/combat jobs in Phase 2.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnitSimBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Reserved for world init / singleton creation.
        }

        public void OnUpdate(ref SystemState state)
        {
            // Intentionally empty until Phase 2 stress slice.
        }
    }
}
