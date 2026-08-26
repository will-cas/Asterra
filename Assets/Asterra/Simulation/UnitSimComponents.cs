using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Asterra.Simulation
{
    /// <summary>
    /// DOTS unit representation. Presentation stays in Gameplay via SimEntityId bridging.
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
        public float Armor;
        public byte Alive;
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

    /// <summary>Maps sim entity ids → ECS entities for combat resolution.</summary>
    public struct UnitIdMap : IComponentData
    {
        public NativeHashMap<uint, Entity> Map;
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
                if (sim.ValueRO.Alive == 0 || target.ValueRO.HasTarget == 0)
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

    /// <summary>Decrements attack cooldown.</summary>
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
                if (sim.ValueRO.Alive == 0)
                    continue;
                if (sim.ValueRO.AttackCooldownRemaining > 0f)
                    sim.ValueRW.AttackCooldownRemaining -= dt;
            }
        }
    }

    /// <summary>
    /// Applies damage when an attacker has a live attack target within range.
    /// Requires a <see cref="UnitIdMap"/> singleton on the world.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitCombatCooldownSystem))]
    public partial struct UnitCombatStrikeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<UnitIdMap>(out var idMap) || !idMap.Map.IsCreated)
                return;

            var em = state.EntityManager;
            foreach (var (sim, pos, atk) in
                     SystemAPI.Query<RefRW<UnitSim>, RefRO<UnitPosition>, RefRO<UnitAttackTarget>>())
            {
                if (sim.ValueRO.Alive == 0 || atk.ValueRO.HasTarget == 0)
                    continue;
                if (sim.ValueRO.AttackCooldownRemaining > 0f)
                    continue;
                if (!idMap.Map.TryGetValue(atk.ValueRO.TargetId, out var targetEntity) || !em.Exists(targetEntity))
                    continue;
                if (!em.HasComponent<UnitSim>(targetEntity) || !em.HasComponent<UnitPosition>(targetEntity))
                    continue;

                var targetSim = em.GetComponentData<UnitSim>(targetEntity);
                if (targetSim.Alive == 0 || targetSim.Owner == sim.ValueRO.Owner)
                    continue;

                var targetPos = em.GetComponentData<UnitPosition>(targetEntity);
                float dx = targetPos.X - pos.ValueRO.X;
                float dz = targetPos.Z - pos.ValueRO.Z;
                float range = sim.ValueRO.AttackRange + 1.5f;
                if (dx * dx + dz * dz > range * range)
                    continue;

                float dmg = math.max(0.5f, sim.ValueRO.AttackDamage - targetSim.Armor * 0.35f);
                targetSim.Health -= dmg;
                if (targetSim.Health <= 0f)
                {
                    targetSim.Health = 0f;
                    targetSim.Alive = 0;
                }

                em.SetComponentData(targetEntity, targetSim);
                sim.ValueRW.AttackCooldownRemaining = sim.ValueRO.AttackCooldown;
            }
        }
    }

    /// <summary>Deterministic FNV-1a style hash over living unit state (for desync / soak).</summary>
    public static class DotsWorldHash
    {
        public static ulong Compute(EntityManager em)
        {
            ulong hash = 14695981039346656037ul;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSim>(), ComponentType.ReadOnly<UnitPosition>());
            var sims = query.ToComponentDataArray<UnitSim>(Allocator.Temp);
            var positions = query.ToComponentDataArray<UnitPosition>(Allocator.Temp);
            try
            {
                // Sort by Id for determinism.
                for (int i = 0; i < sims.Length; i++)
                {
                    for (int j = i + 1; j < sims.Length; j++)
                    {
                        if (sims[j].Id < sims[i].Id)
                        {
                            var tmpSim = sims[i];
                            sims[i] = sims[j];
                            sims[j] = tmpSim;
                            var tmpPos = positions[i];
                            positions[i] = positions[j];
                            positions[j] = tmpPos;
                        }
                    }
                }

                for (int i = 0; i < sims.Length; i++)
                {
                    var s = sims[i];
                    var p = positions[i];
                    hash = Mix(hash, s.Id);
                    hash = Mix(hash, s.Owner);
                    hash = Mix(hash, (ulong)(s.Health * 1000f));
                    hash = Mix(hash, (ulong)(p.X * 100f));
                    hash = Mix(hash, (ulong)(p.Z * 100f));
                    hash = Mix(hash, s.Alive);
                }
            }
            finally
            {
                sims.Dispose();
                positions.Dispose();
            }

            return hash;
        }

        private static ulong Mix(ulong hash, ulong v)
        {
            hash ^= v;
            hash *= 1099511628211ul;
            return hash;
        }
    }
}
