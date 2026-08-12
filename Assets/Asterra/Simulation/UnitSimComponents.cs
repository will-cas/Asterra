using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Asterra.Simulation
{
    /// <summary>
    /// DOTS unit representation. Presentation stays in Gameplay via EntityId bridging.
    /// Mirrors <c>Asterra.Gameplay.Sim.SimUnit</c> fields for the Phase 2 port.
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
        public float AttackCooldown;
        public float AttackCooldownRemaining;
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

    public struct UnitMoveTarget : IComponentData
    {
        public float X;
        public float Z;
        public byte HasTarget;
    }

    public struct UnitAttackTarget : IComponentData
    {
        public uint TargetId;
        public byte HasTarget;
    }

    /// <summary>Advances units toward move targets.</summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnitMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            foreach (var (sim, pos, target) in
                     SystemAPI.Query<RefRO<UnitSim>, RefRW<UnitPosition>, RefRW<UnitMoveTarget>>())
            {
                if (target.ValueRO.HasTarget == 0)
                    continue;

                float dx = target.ValueRO.X - pos.ValueRO.X;
                float dz = target.ValueRO.Z - pos.ValueRO.Z;
                float lenSq = dx * dx + dz * dz;
                if (lenSq <= 0.0625f)
                {
                    pos.ValueRW.X = target.ValueRO.X;
                    pos.ValueRW.Z = target.ValueRO.Z;
                    target.ValueRW.HasTarget = 0;
                    continue;
                }

                float len = math.sqrt(lenSq);
                float step = sim.ValueRO.MoveSpeed * dt;
                if (step >= len)
                {
                    pos.ValueRW.X = target.ValueRO.X;
                    pos.ValueRW.Z = target.ValueRO.Z;
                    target.ValueRW.HasTarget = 0;
                }
                else
                {
                    pos.ValueRW.X += dx / len * step;
                    pos.ValueRW.Z += dz / len * step;
                }
            }
        }
    }

    /// <summary>
    /// Decrements attack cooldown and applies damage when attacker/target share a spatial query.
    /// Full id→entity lookup lands with the Phase 2 world singleton map; this tick only cools down.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitMoveSystem))]
    public partial struct UnitCombatCooldownSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            foreach (var sim in SystemAPI.Query<RefRW<UnitSim>>())
            {
                if (sim.ValueRO.AttackCooldownRemaining > 0f)
                    sim.ValueRW.AttackCooldownRemaining -= dt;
            }
        }
    }
}
