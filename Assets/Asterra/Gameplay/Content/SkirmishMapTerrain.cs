using Asterra.Core.World;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay.Content
{
    /// <summary>
    /// Logical terrain layouts per skirmish map. Presentation can mirror these later;
    /// gameplay queries the grid only (not meshes).
    /// </summary>
    public static class SkirmishMapTerrain
    {
        public static void Apply(WorldEnvironmentSim environment, SkirmishMapId map)
        {
            if (environment == null)
                return;

            var grid = environment.Grid;
            switch (map)
            {
                case SkirmishMapId.TwinKeeps:
                    PaintTwinKeeps(grid);
                    break;
                case SkirmishMapId.RiverCrossing:
                    PaintRiverCrossing(grid);
                    break;
                case SkirmishMapId.BlackridgePass:
                    PaintBlackridgePass(grid);
                    break;
            }

            environment.RebuildFeatureIndex();
        }

        /// <summary>
        /// Open basin: forests and long grass on the flanks, clear center for the territory fight.
        /// </summary>
        private static void PaintTwinKeeps(WorldTerrainGrid grid)
        {
            grid.FillWorldRect(-380f, -40f, -320f, 40f, DefaultTerrainCatalog.GrassBare);
            grid.FillWorldRect(320f, -40f, 380f, 40f, DefaultTerrainCatalog.GrassBare);

            grid.FillWorldRect(-200f, 80f, 200f, 160f, DefaultTerrainCatalog.GrassLong);
            grid.FillWorldRect(-200f, -160f, 200f, -80f, DefaultTerrainCatalog.GrassLong);

            grid.FillWorldRect(-140f, -110f, -60f, -40f, DefaultTerrainCatalog.Forest);
            grid.FillWorldRect(60f, 40f, 140f, 110f, DefaultTerrainCatalog.Forest);
            grid.FillWorldRect(-120f, -90f, -100f, -70f, DefaultTerrainCatalog.Tree);
            grid.FillWorldRect(100f, 70f, 120f, 90f, DefaultTerrainCatalog.Tree);

            grid.FillWorldRect(-60f, 40f, -30f, 70f, DefaultTerrainCatalog.Rock);
            grid.FillWorldRect(30f, -70f, 60f, -40f, DefaultTerrainCatalog.Rock);

            grid.FillWorldRect(-30f, -50f, 30f, -20f, DefaultTerrainCatalog.Swamp);

            grid.FillWorldRect(-220f, -30f, -180f, 30f, DefaultTerrainCatalog.Hill);
            grid.FillWorldRect(180f, -30f, 220f, 30f, DefaultTerrainCatalog.Hill);

            grid.FillWorldRect(-50f, -50f, 50f, 50f, DefaultTerrainCatalog.GrassShort);

            EnsureLandDisk(grid, -80f, 60f, 12f);
            EnsureLandDisk(grid, -100f, -70f, 12f);
            EnsureLandDisk(grid, 280f, -70f, 12f);
            EnsureLandDisk(grid, -280f, 70f, 12f);
        }

        /// <summary>
        /// East–west river with beach banks and multiple fords so land armies can contest mid.
        /// </summary>
        private static void PaintRiverCrossing(WorldTerrainGrid grid)
        {
            grid.FillWorldRect(-450f, -28f, 450f, 28f, DefaultTerrainCatalog.WaterRiver);

            grid.FillWorldRect(-450f, -40f, -380f, 40f, DefaultTerrainCatalog.WaterOcean);
            grid.FillWorldRect(380f, -40f, 450f, 40f, DefaultTerrainCatalog.WaterOcean);

            grid.FillWorldRect(160f, -50f, 220f, 50f, DefaultTerrainCatalog.WaterLake);

            grid.FillWorldRect(-360f, -20f, -330f, 20f, DefaultTerrainCatalog.WaterWaterfall);

            grid.FillWorldRect(-380f, -48f, 380f, -28f, DefaultTerrainCatalog.Beach);
            grid.FillWorldRect(-380f, 28f, 380f, 48f, DefaultTerrainCatalog.Beach);

            grid.FillWorldRect(-35f, -28f, 35f, 28f, DefaultTerrainCatalog.Beach);
            grid.FillWorldRect(-150f, -28f, -110f, 28f, DefaultTerrainCatalog.Beach);
            grid.FillWorldRect(110f, -28f, 150f, 28f, DefaultTerrainCatalog.Beach);

            grid.FillWorldRect(175f, -15f, 205f, 15f, DefaultTerrainCatalog.IceThick);

            grid.FillWorldRect(-80f, 48f, -20f, 90f, DefaultTerrainCatalog.Swamp);
            grid.FillWorldRect(20f, -90f, 80f, -48f, DefaultTerrainCatalog.Swamp);

            grid.FillWorldRect(-200f, -160f, -120f, -70f, DefaultTerrainCatalog.Forest);
            grid.FillWorldRect(120f, 70f, 200f, 160f, DefaultTerrainCatalog.Forest);

            grid.FillWorldRect(-320f, -260f, -260f, -180f, DefaultTerrainCatalog.Hill);
            grid.FillWorldRect(260f, 180f, 320f, 260f, DefaultTerrainCatalog.Hill);

            EnsureLandDisk(grid, 0f, 0f, 18f, DefaultTerrainCatalog.Beach);
            EnsureLandDisk(grid, -120f, 8f, 10f, DefaultTerrainCatalog.Beach);
            EnsureLandDisk(grid, -40f, -12f, 10f, DefaultTerrainCatalog.Beach);
            EnsureLandDisk(grid, 50f, 10f, 10f, DefaultTerrainCatalog.Beach);
            EnsureLandDisk(grid, 130f, -8f, 10f, DefaultTerrainCatalog.Beach);
            EnsureLandDisk(grid, -250f, -160f, 12f);
            EnsureLandDisk(grid, 250f, 160f, 12f);
            EnsureLandDisk(grid, -300f, -220f, 20f, DefaultTerrainCatalog.GrassBare);
            EnsureLandDisk(grid, 300f, 220f, 20f, DefaultTerrainCatalog.GrassBare);
        }

        /// <summary>
        /// Mountain walls with a central trench pass and hill ramps to high-ground territories.
        /// </summary>
        private static void PaintBlackridgePass(WorldTerrainGrid grid)
        {
            grid.FillWorldRect(-450f, 90f, -60f, 450f, DefaultTerrainCatalog.Mountain);
            grid.FillWorldRect(60f, 90f, 450f, 450f, DefaultTerrainCatalog.Mountain);
            grid.FillWorldRect(-450f, -450f, -60f, -90f, DefaultTerrainCatalog.Mountain);
            grid.FillWorldRect(60f, -450f, 450f, -90f, DefaultTerrainCatalog.Mountain);

            grid.SetBlockedRect(-450f, 200f, -200f, 450f, blocked: true);
            grid.SetBlockedRect(200f, 200f, 450f, 450f, blocked: true);
            grid.SetBlockedRect(-450f, -450f, -200f, -200f, blocked: true);
            grid.SetBlockedRect(200f, -450f, 450f, -200f, blocked: true);

            grid.FillWorldRect(-55f, 55f, 55f, 150f, DefaultTerrainCatalog.Hill);
            grid.FillWorldRect(-55f, -150f, 55f, -55f, DefaultTerrainCatalog.Hill);

            grid.FillWorldRect(-160f, -55f, 160f, 55f, DefaultTerrainCatalog.GrassShort);

            grid.FillWorldRect(-45f, -18f, 45f, 18f, DefaultTerrainCatalog.Trench);

            grid.FillWorldRect(-160f, -70f, -100f, -55f, DefaultTerrainCatalog.Rock);
            grid.FillWorldRect(-160f, 55f, -100f, 70f, DefaultTerrainCatalog.Rock);
            grid.FillWorldRect(100f, -70f, 160f, -55f, DefaultTerrainCatalog.Rock);
            grid.FillWorldRect(100f, 55f, 160f, 70f, DefaultTerrainCatalog.Rock);

            grid.FillWorldRect(-280f, -40f, -170f, 40f, DefaultTerrainCatalog.GrassLong);
            grid.FillWorldRect(170f, -40f, 280f, 40f, DefaultTerrainCatalog.GrassLong);

            grid.FillWorldRect(-30f, 25f, 30f, 55f, DefaultTerrainCatalog.Forest);
            grid.FillWorldRect(-30f, -55f, 30f, -25f, DefaultTerrainCatalog.Forest);

            grid.FillWorldRect(-400f, -40f, -320f, 40f, DefaultTerrainCatalog.GrassBare);
            grid.FillWorldRect(320f, -40f, 400f, 40f, DefaultTerrainCatalog.GrassBare);

            EnsureLandDisk(grid, 0f, 0f, 20f, DefaultTerrainCatalog.Trench);
            EnsureLandDisk(grid, 0f, 110f, 16f, DefaultTerrainCatalog.Hill);
            EnsureLandDisk(grid, 0f, -110f, 16f, DefaultTerrainCatalog.Hill);
            EnsureLandDisk(grid, -40f, 0f, 10f);
            EnsureLandDisk(grid, 40f, 0f, 10f);
            EnsureLandDisk(grid, -360f, 0f, 22f, DefaultTerrainCatalog.GrassBare);
            EnsureLandDisk(grid, 360f, 0f, 22f, DefaultTerrainCatalog.GrassBare);
        }

        private static void EnsureLandDisk(
            WorldTerrainGrid grid,
            float x,
            float z,
            float radius,
            ushort defIndex = DefaultTerrainCatalog.GrassShort)
        {
            grid.FillWorldRect(x - radius, z - radius, x + radius, z + radius, defIndex);
        }
    }
}
