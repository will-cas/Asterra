using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay.Content
{
    /// <summary>Spawns destructible world props and links them to traversal / terrain.</summary>
    public static class SkirmishMapDestructibles
    {
        public static void Apply(SkirmishWorldSim world, IIdFactory ids, SkirmishMapId map)
        {
            switch (map)
            {
                case SkirmishMapId.TwinKeeps:
                    ApplyTwinKeeps(world, ids);
                    break;
                case SkirmishMapId.RiverCrossing:
                    ApplyRiverCrossing(world, ids);
                    break;
                case SkirmishMapId.BlackridgePass:
                    ApplyBlackridge(world, ids);
                    break;
            }
        }

        private static void ApplyTwinKeeps(SkirmishWorldSim world, IIdFactory ids)
        {
            // Tree stands that currently block cells — destroying opens grass.
            world.SpawnDestructible(ids.Next(), DefaultDestructibleCatalog.Tree(), -110f, -80f);
            world.SpawnDestructible(ids.Next(), DefaultDestructibleCatalog.Tree(), 110f, 80f);
            world.SpawnDestructible(ids.Next(), DefaultDestructibleCatalog.Rock(), -45f, 55f);
            world.SpawnDestructible(ids.Next(), DefaultDestructibleCatalog.Rock(), 45f, -55f);
        }

        private static void ApplyRiverCrossing(SkirmishWorldSim world, IIdFactory ids)
        {
            int bridgeLink = FindFirstLink(world.Environment, TraversalLinkType.Bridge);
            world.SpawnDestructible(
                ids.Next(),
                DefaultDestructibleCatalog.Bridge(),
                65f,
                0f,
                linkedTraversalLinkId: bridgeLink);
        }

        private static void ApplyBlackridge(SkirmishWorldSim world, IIdFactory ids)
        {
            world.SpawnDestructible(ids.Next(), DefaultDestructibleCatalog.Rock(), -130f, -62f);
            world.SpawnDestructible(ids.Next(), DefaultDestructibleCatalog.Rock(), 130f, 62f);
            int jumpOver = FindFirstLink(world.Environment, TraversalLinkType.JumpOver);
            if (jumpOver >= 0)
            {
                // Optional: boulder plugging the jump gap — not required for link.
            }
        }

        private static int FindFirstLink(WorldEnvironmentSim env, TraversalLinkType type)
        {
            var links = env.TraversalGraph.Links;
            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].Type == type)
                    return links[i].Id;
            }

            return -1;
        }
    }
}
