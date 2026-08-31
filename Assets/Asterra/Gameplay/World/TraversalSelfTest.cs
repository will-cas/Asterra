using System.Collections.Generic;
using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Bridges, jumps, magic crossings, boats.</summary>
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
            Expect(ref fails, sb, "boat on ocean", env.CanUnitEnter(0f, -400f, TraversalCapability.Water));
            Expect(ref fails, sb, "land not on ocean", !env.CanUnitEnter(0f, -400f, TraversalCapability.Land));
            Expect(ref fails, sb, "bridge deck land", env.CanUnitEnter(0f, 0f, TraversalCapability.Land));

            var path = new List<(float x, float z)>();
            bool okPath = env.Pathfinding.TryGetPath(-65f, -10f, 65f, 10f, TraversalCapability.Land, path);
            Expect(ref fails, sb, "path across river", okPath && path.Count >= 2);

            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();
            var sim = new SkirmishWorldSim(wallet, ids, defs, env);
            var player = new PlayerId(0);
            var faction = new FactionId(0);
            var unit = sim.SpawnUnit(ids.Next(), player, faction, FactionDefaultContent.VeiledApprenticeId, -50f, 0f);
            unit.MoveTargetX = 50f;
            unit.MoveTargetZ = 0f;

            bool started = false;
            for (int i = 0; i < 40; i++)
            {
                sim.Tick(0.1f);
                if (unit.ActiveTraversalLinkId >= 0)
                    started = true;
            }

            Expect(ref fails, sb, "bridge traversal started", started || unit.X > 0f);
            Expect(ref fails, sb, "crossed east", unit.X > 10f);

            Expect(ref fails, sb, "magic link exists",
                env.TraversalGraph.TryFindLinkForMove(
                    -40f, 200f, 40f, 200f, TraversalCapability.Magic, 0f, out _, out _));
            Expect(ref fails, sb, "land cannot use magic link",
                !env.TraversalGraph.TryFindLinkForMove(
                    -40f, 200f, 40f, 200f, TraversalCapability.Land, 0f, out _, out _));

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
                !env.TraversalGraph.TryFindLinkForMove(-40f, 0f, 40f, 0f, TraversalCapability.Land, 0f, out _, out _));

            var boatEnv = new WorldEnvironmentSim();
            SkirmishMapTerrain.Apply(boatEnv, SkirmishMapId.RiverCrossing);
            var boatSim = new SkirmishWorldSim(wallet, ids, defs, boatEnv);
            var boat = boatSim.SpawnUnit(ids.Next(), player, faction, FactionDefaultContent.RiverBoatId, 0f, -400f);
            Expect(ref fails, sb, "boat caps water", boat.TraversalCapabilities == TraversalCapability.Water);
            boat.MoveTargetX = 8f;
            boat.MoveTargetZ = -390f;
            float boatStartZ = boat.Z;
            for (int i = 0; i < 20; i++)
                boatSim.Tick(0.1f);
            Expect(ref fails, sb, "boat moved on water",
                System.Math.Abs(boat.X) + System.Math.Abs(boat.Z - boatStartZ) > 1.5f);

            var relic = new WorldEnvironmentSim();
            SkirmishMapTerrain.Apply(relic, SkirmishMapId.AncientRelic);
            SkirmishMapTraversal.Apply(relic, SkirmishMapId.AncientRelic);
            Expect(ref fails, sb, "jump down for land",
                relic.TraversalGraph.TryFindLinkForMove(0f, 95f, 0f, 40f, TraversalCapability.Land, 0f, out var down, out _)
                && down.Type == TraversalLinkType.JumpDown);
            Expect(ref fails, sb, "jump up needs Jump",
                !relic.TraversalGraph.TryFindLinkForMove(0f, 45f, 0f, 95f, TraversalCapability.Land, 0f, out _, out _));
            Expect(ref fails, sb, "pathfinder can jump up",
                relic.TraversalGraph.TryFindLinkForMove(
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
