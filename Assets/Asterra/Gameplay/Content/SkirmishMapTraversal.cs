using Asterra.Core.World;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay.Content
{
    /// <summary>Registers bridges, jumps, magic crossings, and shore links per skirmish map.</summary>
    public static class SkirmishMapTraversal
    {
        public static void Apply(WorldEnvironmentSim environment, SkirmishMapId map)
        {
            if (environment == null)
                return;

            var g = environment.TraversalGraph;
            switch (map)
            {
                case SkirmishMapId.TwinKeeps:
                    PaintTwinKeeps(g, environment.Grid);
                    break;
                case SkirmishMapId.RiverCrossing:
                    PaintRiverCrossing(g, environment.Grid);
                    break;
                case SkirmishMapId.BlackridgePass:
                    PaintBlackridgePass(g, environment.Grid);
                    break;
            }
        }

        private static void PaintTwinKeeps(TraversalGraph g, WorldTerrainGrid grid)
        {
            // Magic crossing over the southern swamp pocket.
            g.AddLink(
                -20f, -55f, 20f, -55f,
                TraversalLinkType.MagicCrossing,
                TraversalCapability.Magic,
                durationSeconds: 0.9f,
                allowsCombat: false,
                requiresAnimation: true);

            // Jump over the NW tree cluster gap (tree cells block normal walk).
            g.AddLink(
                -125f, -95f, -95f, -65f,
                TraversalLinkType.TreeGap,
                TraversalCapability.Jump,
                durationSeconds: 0.7f,
                allowsCombat: false,
                requiresAnimation: true,
                approachRadius: 10f);

            // Paint a visual/logical bridge deck across a tiny ditch (gap cells optional).
            grid.FillWorldRect(-10f, 55f, 10f, 75f, DefaultTerrainCatalog.GrassShort);
            g.AddLink(
                0f, 55f, 0f, 75f,
                TraversalLinkType.Bridge,
                TraversalCapability.Land,
                durationSeconds: 0.8f,
                allowsCombat: true,
                isDestructible: true);
        }

        private static void PaintRiverCrossing(TraversalGraph g, WorldTerrainGrid grid)
        {
            // Permanent bridge deck + traversal link (can be disabled when destroyed later).
            grid.FillWorldRect(55f, -28f, 75f, 28f, DefaultTerrainCatalog.Beach);
            g.AddLink(
                65f, -32f, 65f, 32f,
                TraversalLinkType.Bridge,
                TraversalCapability.Land,
                durationSeconds: 1.4f,
                allowsCombat: true,
                isDestructible: true,
                approachRadius: 10f);

            // Magic crossing west of fords (mages / flagged units only).
            g.AddLink(
                -220f, -32f, -220f, 32f,
                TraversalLinkType.MagicCrossing,
                TraversalCapability.Magic,
                durationSeconds: 1.1f,
                allowsCombat: false,
                requiresAnimation: true,
                approachRadius: 10f);

            // Shore transitions for boats (land ↔ ocean mouth).
            g.AddLink(
                -370f, -50f, -400f, 0f,
                TraversalLinkType.ShoreTransition,
                TraversalCapability.Amphibious,
                durationSeconds: 1.5f,
                allowsCombat: false,
                approachRadius: 12f);
            g.AddLink(
                370f, 50f, 400f, 0f,
                TraversalLinkType.ShoreTransition,
                TraversalCapability.Amphibious,
                durationSeconds: 1.5f,
                allowsCombat: false,
                approachRadius: 12f);
        }

        private static void PaintBlackridgePass(TraversalGraph g, WorldTerrainGrid grid)
        {
            // Jump down from north high ground into the pass.
            g.AddLink(
                0f, 95f, 0f, 45f,
                TraversalLinkType.JumpDown,
                TraversalCapability.Land,
                durationSeconds: 0.85f,
                allowsCombat: false,
                requiresAnimation: true,
                approachRadius: 10f);

            // Jump up requires Jump capability.
            g.AddLink(
                0f, 45f, 0f, 95f,
                TraversalLinkType.JumpUp,
                TraversalCapability.Jump,
                durationSeconds: 1.1f,
                allowsCombat: false,
                requiresAnimation: true,
                approachRadius: 10f);

            // Jump over a rock shoulder gap on the west mouth.
            grid.FillWorldRect(-155f, -5f, -145f, 5f, DefaultTerrainCatalog.NoEntry);
            g.AddLink(
                -160f, 0f, -140f, 0f,
                TraversalLinkType.JumpOver,
                TraversalCapability.Jump,
                durationSeconds: 0.75f,
                allowsCombat: false,
                requiresAnimation: true,
                approachRadius: 9f);
        }
    }
}
