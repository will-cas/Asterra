using System.Collections.Generic;
using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Phase-4 regression: bridges, jumps, magic crossings, boats.</summary>
    public static class TraversalSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            var env = new WorldEnvironmentSim();
            SkirmishMapTerrain.Apply(env, SkirmishMapId.RiverCrossing);
            SkirmishMapTraversal.Apply(env, SkirmishMapId.RiverCrossing);

            Expect(ref fails, sb, "has links", env.TraversalGraph.Links.Count >= 3);
            Expect(ref fails, sb, "boat on ocean", env.CanUnitEnter(-390f, 0f, TraversalCapability.Water));
            Expect(ref fails, sb, "land not on ocean", !env.CanUnitEnter(-390f, 0f, TraversalCapability.Land));
            Expect(ref fails, sb, "bridge deck land", env.CanUnitEnter(65f, 0f, TraversalCapability.Land));

            // Pathfinding inserts bridge waypoints when mid is water.
            var path = new List<(float x, float z)>();
            bool okPath = env.Pathfinding.TryGetPath(-65f, -40f, 65f, 40f, TraversalCapability.Land, path);
            Expect(ref fails, sb, "path across river", okPath && path.Count >= 2);

            // Sim: unit walks onto bridge link and crosses.
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();
            var sim = new SkirmishWorldSim(wallet, ids, defs, env);
            var player = new PlayerId(0);
            var faction = new FactionId(0);
            var unit = sim.SpawnUnit(ids.Next(), player, faction, FactionDefaultContent.MilitiaId, 65f, -32f);
            unit.MoveTargetX = 65f;
            unit.MoveTargetZ = 32f;

            bool started = false;
            for (int i = 0; i < 40; i++)
            {
                sim.Tick(0.1f);
                if (unit.ActiveTraversalLinkId >= 0)
                    started = true;
            }

            Expect(ref fails, sb, "bridge traversal started", started || unit.Z > 0f);
            Expect(ref fails, sb, "crossed north", unit.Z > 10f);

            // Magic crossing rejects land-only units.
            Expect(ref fails, sb, "magic link exists",
                env.TraversalGraph.TryFindLinkForMove(
                    -220f, -32f, -220f, 32f, TraversalCapability.Magic, 0f, out _, out _));
            Expect(ref fails, sb, "land cannot use magic link",
                !env.TraversalGraph.TryFindLinkForMove(
                    -220f, -32f, -220f, 32f, TraversalCapability.Land, 0f, out _, out _));

            // Disable bridge → link no longer chosen.
            int bridgeId = -1;
            for (int i = 0; i < env.TraversalGraph.Links.Count; i++)
            {
                if (env.TraversalGraph.Links[i].Type == TraversalLinkType.Bridge)
                {
                    bridgeId = env.TraversalGraph.Links[i].Id;
                    break;
                }
            }

            Expect(ref fails, sb, "found bridge id", bridgeId >= 0);
            env.TraversalGraph.SetLinkEnabled(bridgeId, false);
            Expect(ref fails, sb, "disabled bridge ignored",
                !env.TraversalGraph.TryFindLinkForMove(65f, -32f, 65f, 32f, TraversalCapability.Land, 0f, out _, out _));

            // Boat movement on water.
            var boatEnv = new WorldEnvironmentSim();
            SkirmishMapTerrain.Apply(boatEnv, SkirmishMapId.RiverCrossing);
            var boatSim = new SkirmishWorldSim(wallet, ids, defs, boatEnv);
            var boat = boatSim.SpawnUnit(ids.Next(), player, faction, FactionDefaultContent.RiverBoatId, -390f, 0f);
            Expect(ref fails, sb, "boat caps water", boat.TraversalCapabilities == TraversalCapability.Water);
            boat.MoveTargetX = -385f;
            boat.MoveTargetZ = 15f;
            float boatStartX = boat.X;
            for (int i = 0; i < 20; i++)
                boatSim.Tick(0.1f);
            Expect(ref fails, sb, "boat moved on water",
                System.Math.Abs(boat.X - boatStartX) + System.Math.Abs(boat.Z - 0f) > 1.5f);

            // Jump-up requires Jump on Blackridge.
            var br = new WorldEnvironmentSim();
            SkirmishMapTerrain.Apply(br, SkirmishMapId.BlackridgePass);
            SkirmishMapTraversal.Apply(br, SkirmishMapId.BlackridgePass);
            Expect(ref fails, sb, "jump down for land",
                br.TraversalGraph.TryFindLinkForMove(0f, 95f, 0f, 40f, TraversalCapability.Land, 0f, out var down, out _)
                && down.Type == TraversalLinkType.JumpDown);
            Expect(ref fails, sb, "jump up needs Jump",
                !br.TraversalGraph.TryFindLinkForMove(0f, 45f, 0f, 95f, TraversalCapability.Land, 0f, out _, out _));
            Expect(ref fails, sb, "pathfinder can jump up",
                br.TraversalGraph.TryFindLinkForMove(
                    0f, 45f, 0f, 95f, TraversalCapability.Land | TraversalCapability.Jump, 0f, out var up, out _)
                && up.Type == TraversalLinkType.JumpUp);

            sb.Append(fails == 0 ? "TraversalSelfTest: OK" : $"TraversalSelfTest: FAIL ({fails})");
            return sb.ToString();
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
