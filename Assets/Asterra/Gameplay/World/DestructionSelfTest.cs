using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Phase-5 regression: destructibles open paths / disable bridges locally.</summary>
    public static class DestructionSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();

            // Tree destruction opens terrain.
            var env = new WorldEnvironmentSim();
            SkirmishMapTerrain.Apply(env, SkirmishMapId.TwinKeeps);
            SkirmishMapTraversal.Apply(env, SkirmishMapId.TwinKeeps);
            Expect(ref fails, sb, "tree cell blocked before", !env.CanUnitEnter(-110f, -80f, TraversalCapability.Land));

            var sim = new SkirmishWorldSim(wallet, ids, defs, env);
            SkirmishMapDestructibles.Apply(sim, ids, SkirmishMapId.TwinKeeps);
            Expect(ref fails, sb, "spawned destructibles", sim.Destructibles.Count >= 2);

            var treeId = FindDestructible(sim, DefaultDestructibleCatalog.TreeId);
            Expect(ref fails, sb, "found tree", treeId.HasValue);
            Smash(sim, treeId.Value, 500f);
            sim.Tick(0.05f);

            Expect(ref fails, sb, "tree opened path", env.CanUnitEnter(-110f, -80f, TraversalCapability.Land));
            Expect(ref fails, sb, "timber drop node", HasNearbyResource(sim, ResourceType.Timber, -110f, -80f, 20f));

            // Bridge destruction disables traversal + floods deck.
            var riverEnv = new WorldEnvironmentSim();
            SkirmishMapTerrain.Apply(riverEnv, SkirmishMapId.RiverCrossing);
            SkirmishMapTraversal.Apply(riverEnv, SkirmishMapId.RiverCrossing);
            var riverSim = new SkirmishWorldSim(wallet, ids, defs, riverEnv);
            SkirmishMapDestructibles.Apply(riverSim, ids, SkirmishMapId.RiverCrossing);

            int bridgeLink = FindBridgeLink(riverEnv);
            Expect(ref fails, sb, "bridge link on", bridgeLink >= 0
                && riverEnv.TraversalGraph.TryGetLink(bridgeLink, out var bl) && bl.Enabled);
            Expect(ref fails, sb, "bridge deck walkable", riverEnv.CanUnitEnter(65f, 0f, TraversalCapability.Land));

            var bridgeId = FindDestructible(riverSim, DefaultDestructibleCatalog.BridgeId);
            Expect(ref fails, sb, "found bridge prop", bridgeId.HasValue);
            Smash(riverSim, bridgeId.Value, 2000f);
            riverSim.Tick(0.05f);

            Expect(ref fails, sb, "bridge link off",
                riverEnv.TraversalGraph.TryGetLink(bridgeLink, out var bl2) && !bl2.Enabled);
            Expect(ref fails, sb, "deck flooded", !riverEnv.CanUnitEnter(65f, 0f, TraversalCapability.Land));
            Expect(ref fails, sb, "deck is water for boats", riverEnv.CanUnitEnter(65f, 0f, TraversalCapability.Water));

            // Wall removal marks path dirty during destroy (cleared after tick).
            var wallEnv = new WorldEnvironmentSim();
            var wallSim = new SkirmishWorldSim(wallet, ids, defs, wallEnv);
            var wall = wallSim.SpawnBuilding(
                ids.Next(),
                new PlayerId(1),
                new FactionId(1),
                FactionDefaultContent.PalisadeId,
                0f,
                0f,
                startActive: true);
            Smash(wallSim, wall.Id, 5000f);
            Expect(ref fails, sb, "wall destroyed event path",
                wall.State == BuildingState.Destroyed || !ContainsBuilding(wallSim, wall.Id));
            wallSim.Tick(0.05f);

            sb.Append(fails == 0 ? "DestructionSelfTest: OK" : $"DestructionSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static void Smash(SkirmishWorldSim sim, SimEntityId id, float damage)
        {
            sim.ApplyWorldDamage(id, damage, vsStructure: true);
        }

        private static SimEntityId? FindDestructible(SkirmishWorldSim sim, string defId)
        {
            for (int i = 0; i < sim.Destructibles.Count; i++)
            {
                if (sim.Destructibles[i].DefinitionId == defId)
                    return sim.Destructibles[i].Id;
            }

            return null;
        }

        private static int FindBridgeLink(WorldEnvironmentSim env)
        {
            for (int i = 0; i < env.TraversalGraph.Links.Count; i++)
            {
                if (env.TraversalGraph.Links[i].Type == TraversalLinkType.Bridge)
                    return env.TraversalGraph.Links[i].Id;
            }

            return -1;
        }

        private static bool HasNearbyResource(SkirmishWorldSim sim, ResourceType type, float x, float z, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < sim.Resources.Count; i++)
            {
                var r = sim.Resources[i];
                if (r.Type != type)
                    continue;
                float dx = r.X - x;
                float dz = r.Z - z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }

        private static bool ContainsBuilding(SkirmishWorldSim sim, SimEntityId id)
        {
            for (int i = 0; i < sim.Buildings.Count; i++)
            {
                if (sim.Buildings[i].Id.Value == id.Value)
                    return true;
            }

            return false;
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
