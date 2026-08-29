using System.Text;
using Asterra.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Asterra.Gameplay
{
    /// <summary>
    /// DOTS world-hash + component smoke without depending on automatic system discovery.
    /// Move/combat systems run in play-mode worlds; this validates the hash path and id map.
    /// </summary>
    public static class DotsSimSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "spawn + id map", SpawnAndMap());
            Expect(ref fails, sb, "manual move step", ManualMove());
            Expect(ref fails, sb, "hash stable for identical worlds", HashStable());

            sb.Append(fails == 0 ? "DotsSimSelfTest: OK" : $"DotsSimSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool SpawnAndMap()
        {
            using var world = new Unity.Entities.World("AsterraDotsMapTest");
            var em = world.EntityManager;
            var map = new NativeHashMap<uint, Entity>(8, Allocator.Temp);
            var e = Spawn(em, map, 7, 0, 1f, 2f);
            bool ok = map.TryGetValue(7, out var found) && found == e;
            map.Dispose();
            return ok;
        }

        private static bool ManualMove()
        {
            using var world = new Unity.Entities.World("AsterraDotsMoveTest");
            var em = world.EntityManager;
            var map = new NativeHashMap<uint, Entity>(4, Allocator.Temp);
            var e = Spawn(em, map, 1, 0, 0f, 0f);
            var sim = em.GetComponentData<UnitSim>(e);
            var pos = em.GetComponentData<UnitPosition>(e);
            float tx = 10f;
            float tz = 0f;
            float dt = 0.05f;
            for (int i = 0; i < 80; i++)
            {
                float dx = tx - pos.X;
                float dz = tz - pos.Z;
                float len = math.sqrt(dx * dx + dz * dz);
                float step = sim.MoveSpeed * dt;
                if (step >= len)
                {
                    pos.X = tx;
                    pos.Z = tz;
                    break;
                }

                pos.X += dx / len * step;
                pos.Z += dz / len * step;
            }

            em.SetComponentData(e, pos);
            map.Dispose();
            return math.abs(pos.X - 10f) < 0.15f;
        }

        private static bool HashStable()
        {
            ulong ha = BuildHash();
            ulong hb = BuildHash();
            return ha == hb && ha != 0ul;
        }

        private static ulong BuildHash()
        {
            using var world = new Unity.Entities.World("AsterraDotsHashTest");
            var em = world.EntityManager;
            var map = new NativeHashMap<uint, Entity>(8, Allocator.Temp);
            Spawn(em, map, 1, 0, 0f, 0f);
            Spawn(em, map, 2, 1, 3f, 1f);
            ulong hash = DotsWorldHash.Compute(em);
            map.Dispose();
            return hash;
        }

        private static Entity Spawn(EntityManager em, NativeHashMap<uint, Entity> map, uint id, byte owner, float x, float z)
        {
            var e = em.CreateEntity();
            em.AddComponentData(e, new UnitSim
            {
                Id = id,
                Owner = owner,
                Health = 100f,
                MaxHealth = 100f,
                MoveSpeed = 8f,
                AttackDamage = 10f,
                AttackRange = 2f,
                AttackCooldown = 1f,
                Alive = 1,
            });
            em.AddComponentData(e, new UnitPosition { X = x, Z = z });
            em.AddComponentData(e, new UnitMoveTarget { HasTarget = 0 });
            em.AddComponentData(e, new UnitAttackTarget { HasTarget = 0 });
            map[id] = e;
            return e;
        }

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
