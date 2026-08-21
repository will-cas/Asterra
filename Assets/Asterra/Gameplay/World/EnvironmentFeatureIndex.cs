using Asterra.Core.World;

namespace Asterra.Gameplay.World
{
    /// <summary>
    /// Coarse spatial summary of vegetation / cover cells so systems can query "is there forest nearby?"
    /// without scanning every cell every frame. Chunk size is in terrain cells.
    /// </summary>
    public sealed class EnvironmentFeatureIndex
    {
        public const int ChunkCells = 5;

        private readonly WorldTerrainGrid _grid;
        private readonly byte[] _forestCount;
        private readonly byte[] _longGrassCount;
        private readonly byte[] _swampCount;
        private readonly byte[] _waterCount;
        private readonly int _chunksX;
        private readonly int _chunksZ;

        public EnvironmentFeatureIndex(WorldTerrainGrid grid)
        {
            _grid = grid;
            _chunksX = (grid.Width + ChunkCells - 1) / ChunkCells;
            _chunksZ = (grid.Height + ChunkCells - 1) / ChunkCells;
            int n = _chunksX * _chunksZ;
            _forestCount = new byte[n];
            _longGrassCount = new byte[n];
            _swampCount = new byte[n];
            _waterCount = new byte[n];
        }

        public void Rebuild()
        {
            for (int i = 0; i < _forestCount.Length; i++)
            {
                _forestCount[i] = 0;
                _longGrassCount[i] = 0;
                _swampCount[i] = 0;
                _waterCount[i] = 0;
            }

            for (int cz = 0; cz < _grid.Height; cz++)
            {
                for (int cx = 0; cx < _grid.Width; cx++)
                {
                    if (!_grid.TryGetCell(
                            _grid.OriginX + (cx + 0.5f) * _grid.CellSize,
                            _grid.OriginZ + (cz + 0.5f) * _grid.CellSize,
                            out var cell))
                        continue;

                    var def = _grid.GetDef(cell.TerrainDefIndex);
                    int chunk = ChunkIndex(cx, cz);
                    switch (def.Category)
                    {
                        case TerrainCategory.Forest:
                        case TerrainCategory.Tree:
                            Bump(_forestCount, chunk);
                            break;
                        case TerrainCategory.GrassLong:
                            Bump(_longGrassCount, chunk);
                            break;
                        case TerrainCategory.Swamp:
                            Bump(_swampCount, chunk);
                            break;
                        case TerrainCategory.WaterRiver:
                        case TerrainCategory.WaterLake:
                        case TerrainCategory.WaterOcean:
                        case TerrainCategory.WaterWaterfall:
                            Bump(_waterCount, chunk);
                            break;
                    }
                }
            }
        }

        public bool HasForestNear(float worldX, float worldZ, float radiusWorld)
        {
            return HasFeatureNear(worldX, worldZ, radiusWorld, _forestCount);
        }

        public bool HasLongGrassNear(float worldX, float worldZ, float radiusWorld)
        {
            return HasFeatureNear(worldX, worldZ, radiusWorld, _longGrassCount);
        }

        public bool HasWaterNear(float worldX, float worldZ, float radiusWorld)
        {
            return HasFeatureNear(worldX, worldZ, radiusWorld, _waterCount);
        }

        public float SampleVisibilityModifier(float worldX, float worldZ)
        {
            if (!_grid.TryGetCell(worldX, worldZ, out var cell))
                return 1f;
            return _grid.GetDef(cell.TerrainDefIndex).VisibilityModifier;
        }

        public float SampleCoverBonus(float worldX, float worldZ)
        {
            if (!_grid.TryGetCell(worldX, worldZ, out var cell))
                return 0f;
            return _grid.GetDef(cell.TerrainDefIndex).CoverBonus;
        }

        private bool HasFeatureNear(float worldX, float worldZ, float radiusWorld, byte[] counts)
        {
            if (!_grid.TryWorldToCell(worldX, worldZ, out int cx, out int cz))
                return false;

            int cellRadius = (int)System.Math.Ceiling(radiusWorld / _grid.CellSize) + ChunkCells;
            int minCx = System.Math.Max(0, cx - cellRadius);
            int maxCx = System.Math.Min(_grid.Width - 1, cx + cellRadius);
            int minCz = System.Math.Max(0, cz - cellRadius);
            int maxCz = System.Math.Min(_grid.Height - 1, cz + cellRadius);

            int minChunkX = minCx / ChunkCells;
            int maxChunkX = maxCx / ChunkCells;
            int minChunkZ = minCz / ChunkCells;
            int maxChunkZ = maxCz / ChunkCells;
            for (int z = minChunkZ; z <= maxChunkZ; z++)
            {
                for (int x = minChunkX; x <= maxChunkX; x++)
                {
                    if (counts[z * _chunksX + x] > 0)
                        return true;
                }
            }

            return false;
        }

        private int ChunkIndex(int cellX, int cellZ) =>
            (cellZ / ChunkCells) * _chunksX + (cellX / ChunkCells);

        private static void Bump(byte[] counts, int index)
        {
            if (counts[index] < 255)
                counts[index]++;
        }
    }
}
