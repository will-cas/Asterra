using System;
using System.Collections.Generic;

namespace Asterra.Core.World
{
    /// <summary>
    /// Chunk-friendly XZ terrain grid. Authoritative for gameplay queries; visuals stay in presentation.
    /// Default fill is short grass. Pathfinding / weather systems will mutate cells in later phases.
    /// </summary>
    public sealed class WorldTerrainGrid : ITerrainMap, INoEntryMap
    {
        private readonly TerrainDefData[] _defs;
        private readonly Dictionary<string, int> _defIndexById;
        private readonly TerrainCell[] _cells;
        private readonly bool[] _blocked;

        /// <summary>Increments when cells or blockers change — fold into world hash for lockstep.</summary>
        public ulong MutationVersion { get; private set; }

        public float CellSize { get; }
        public int Width { get; }
        public int Height { get; }
        public float OriginX { get; }
        public float OriginZ { get; }

        public WorldTerrainGrid(
            int width,
            int height,
            float cellSize,
            float originX,
            float originZ,
            TerrainDefData[] defs,
            ushort defaultDefIndex = 0)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (defs == null || defs.Length == 0)
                throw new ArgumentException("At least one terrain def is required.", nameof(defs));

            Width = width;
            Height = height;
            CellSize = cellSize;
            OriginX = originX;
            OriginZ = originZ;
            _defs = defs;
            _defIndexById = new Dictionary<string, int>(defs.Length);
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i] == null || string.IsNullOrEmpty(defs[i].Id))
                    throw new ArgumentException($"Terrain def at {i} is missing Id.");
                _defIndexById[defs[i].Id] = i;
            }

            int count = width * height;
            _cells = new TerrainCell[count];
            _blocked = new bool[count];
            for (int i = 0; i < count; i++)
            {
                _cells[i] = new TerrainCell
                {
                    TerrainDefIndex = defaultDefIndex,
                    Ice = IceState.None,
                };
            }
        }

        /// <summary>
        /// Creates a playable-area grid aligned to <paramref name="playableHalfExtent"/>
        /// (matches MapBounds.PlayableHalfExtent = 450 by default).
        /// </summary>
        public static WorldTerrainGrid CreatePlayableDefault(float playableHalfExtent = 450f, float cellSize = 10f)
        {
            int cells = (int)Math.Ceiling((playableHalfExtent * 2f) / cellSize);
            if (cells < 1)
                cells = 1;

            var defs = new[]
            {
                TerrainDefData.CreateDefaultGrassShort(),
                TerrainDefData.CreateNoEntry(),
            };

            float origin = -playableHalfExtent;
            return new WorldTerrainGrid(cells, cells, cellSize, origin, origin, defs, defaultDefIndex: 0);
        }

        public bool TryWorldToCell(float worldX, float worldZ, out int cellX, out int cellZ)
        {
            cellX = (int)Math.Floor((worldX - OriginX) / CellSize);
            cellZ = (int)Math.Floor((worldZ - OriginZ) / CellSize);
            return cellX >= 0 && cellZ >= 0 && cellX < Width && cellZ < Height;
        }

        public void CellCenter(int cellX, int cellZ, out float worldX, out float worldZ)
        {
            worldX = OriginX + (cellX + 0.5f) * CellSize;
            worldZ = OriginZ + (cellZ + 0.5f) * CellSize;
        }

        public bool TryGetCell(float worldX, float worldZ, out TerrainCell cell)
        {
            if (!TryWorldToCell(worldX, worldZ, out int cx, out int cz))
            {
                cell = default;
                return false;
            }

            cell = _cells[Index(cx, cz)];
            return true;
        }

        public bool TryGetCellAt(int cellX, int cellZ, out TerrainCell cell)
        {
            if (!InBounds(cellX, cellZ))
            {
                cell = default;
                return false;
            }

            cell = _cells[Index(cellX, cellZ)];
            return true;
        }

        public bool IsBlockedAt(int cellX, int cellZ) =>
            InBounds(cellX, cellZ) && _blocked[Index(cellX, cellZ)];

        public bool TryGetDef(string terrainDefId, out TerrainDefData def)
        {
            if (terrainDefId != null && _defIndexById.TryGetValue(terrainDefId, out int idx))
            {
                def = _defs[idx];
                return true;
            }

            def = null;
            return false;
        }

        public bool TryGetDefIndex(string terrainDefId, out int index)
        {
            if (terrainDefId != null && _defIndexById.TryGetValue(terrainDefId, out index))
                return true;
            index = -1;
            return false;
        }

        public TerrainDefData GetDef(ushort defIndex)
        {
            if (defIndex >= _defs.Length)
                return _defs[0];
            return _defs[defIndex];
        }

        public void SetCellDef(int cellX, int cellZ, ushort defIndex)
        {
            if (!InBounds(cellX, cellZ))
                return;
            if (defIndex >= _defs.Length)
                return;
            int i = Index(cellX, cellZ);
            var cell = _cells[i];
            if (cell.TerrainDefIndex == defIndex)
                return;
            cell.TerrainDefIndex = defIndex;
            _cells[i] = cell;
            MutationVersion++;
        }

        /// <summary>Paint an axis-aligned world rect with a terrain def (inclusive cell coverage).</summary>
        public void FillWorldRect(float minX, float minZ, float maxX, float maxZ, ushort defIndex)
        {
            if (defIndex >= _defs.Length)
                return;
            int minCx = Clamp((int)Math.Floor((minX - OriginX) / CellSize), 0, Width - 1);
            int minCz = Clamp((int)Math.Floor((minZ - OriginZ) / CellSize), 0, Height - 1);
            int maxCx = Clamp((int)Math.Floor((maxX - OriginX) / CellSize), 0, Width - 1);
            int maxCz = Clamp((int)Math.Floor((maxZ - OriginZ) / CellSize), 0, Height - 1);
            if (minCx > maxCx)
            {
                int t = minCx;
                minCx = maxCx;
                maxCx = t;
            }

            if (minCz > maxCz)
            {
                int t = minCz;
                minCz = maxCz;
                maxCz = t;
            }

            for (int z = minCz; z <= maxCz; z++)
            {
                for (int x = minCx; x <= maxCx; x++)
                    SetCellDef(x, z, defIndex);
            }
        }

        public void SetCell(int cellX, int cellZ, TerrainCell cell)
        {
            if (!InBounds(cellX, cellZ))
                return;
            _cells[Index(cellX, cellZ)] = cell;
            MutationVersion++;
        }

        public float GetMovementModifier(float worldX, float worldZ, TraversalCapability capabilities)
        {
            if (!IsTraversable(worldX, worldZ, capabilities))
                return 0f;
            if (!TryGetCell(worldX, worldZ, out var cell))
                return 1f;
            var def = GetDef(cell.TerrainDefIndex);
            float mod = def.MovementSpeedModifier;

            if (cell.Ice == IceState.Thin)
                mod *= 0.85f;
            else if (cell.Ice == IceState.Thick)
                mod *= 0.95f;
            else if (cell.Ice == IceState.Broken)
                mod *= 0.55f;

            if (cell.SnowDepth01 > 40)
                mod *= 1f - cell.SnowDepth01 / 255f * 0.35f;
            if ((cell.Flags & TerrainCell.FlagMuddy) != 0 || cell.Waterlog01 > 140)
                mod *= 0.85f;

            return mod;
        }

        public float GetCoverBonus(float worldX, float worldZ)
        {
            if (!TryGetCell(worldX, worldZ, out var cell))
                return 0f;
            return GetDef(cell.TerrainDefIndex).CoverBonus;
        }

        public float GetCombatModifier(float worldX, float worldZ)
        {
            if (!TryGetCell(worldX, worldZ, out var cell))
                return 1f;
            float combat = GetDef(cell.TerrainDefIndex).CombatModifier;
            if (combat < 0.2f)
                return 1f;
            return combat;
        }

        public float GetPathCost(float worldX, float worldZ, TraversalCapability capabilities)
        {
            if (IsBlocked(worldX, worldZ))
                return TerrainDefData.PathCostBlocked;
            if (!TryGetCell(worldX, worldZ, out var cell))
                return TerrainDefData.PathCostBlocked;
            var def = GetDef(cell.TerrainDefIndex);
            if (!IsCellTraversable(def, cell, capabilities))
                return TerrainDefData.PathCostBlocked;
            float cost = def.PathfindingCost;
            if (cell.SnowDepth01 > 80)
                cost += 0.5f;
            if ((cell.Flags & TerrainCell.FlagMuddy) != 0)
                cost += 0.75f;
            return cost;
        }

        public bool IsTraversable(float worldX, float worldZ, TraversalCapability capabilities)
        {
            if (IsBlocked(worldX, worldZ))
                return false;
            if (!TryGetCell(worldX, worldZ, out var cell))
                return false;
            return IsCellTraversable(GetDef(cell.TerrainDefIndex), cell, capabilities);
        }

        private static bool IsCellTraversable(TerrainDefData def, TerrainCell cell, TraversalCapability capabilities)
        {
            if (def.IsTraversableBy(capabilities))
                return true;

            // Intact ice over water acts as a land bridge overlay.
            if (cell.Ice == IceState.Thin || cell.Ice == IceState.Thick || cell.Ice == IceState.FrozenWater)
            {
                if ((capabilities & TraversalCapability.Flying) != 0)
                    return true;
                if ((capabilities & TraversalCapability.Land) != 0)
                    return true;
            }

            return false;
        }

        public bool AllowsBuilding(float worldX, float worldZ)
        {
            if (IsBlocked(worldX, worldZ))
                return false;
            if (!TryResolveDef(worldX, worldZ, out var def))
                return false;
            return def.AllowsBuilding;
        }

        public bool IsBlocked(float worldX, float worldZ)
        {
            if (!TryWorldToCell(worldX, worldZ, out int cx, out int cz))
                return true; // outside grid = no-entry
            return _blocked[Index(cx, cz)];
        }

        public void SetBlockedRect(float minX, float minZ, float maxX, float maxZ, bool blocked)
        {
            int minCx = Clamp((int)Math.Floor((minX - OriginX) / CellSize), 0, Width - 1);
            int minCz = Clamp((int)Math.Floor((minZ - OriginZ) / CellSize), 0, Height - 1);
            int maxCx = Clamp((int)Math.Floor((maxX - OriginX) / CellSize), 0, Width - 1);
            int maxCz = Clamp((int)Math.Floor((maxZ - OriginZ) / CellSize), 0, Height - 1);
            if (minCx > maxCx)
            {
                int t = minCx;
                minCx = maxCx;
                maxCx = t;
            }

            if (minCz > maxCz)
            {
                int t = minCz;
                minCz = maxCz;
                maxCz = t;
            }

            for (int z = minCz; z <= maxCz; z++)
            {
                for (int x = minCx; x <= maxCx; x++)
                {
                    int i = Index(x, z);
                    if (_blocked[i] == blocked)
                        continue;
                    _blocked[i] = blocked;
                    MutationVersion++;
                }
            }
        }

        private bool TryResolveDef(float worldX, float worldZ, out TerrainDefData def)
        {
            if (!TryGetCell(worldX, worldZ, out var cell))
            {
                def = null;
                return false;
            }

            def = GetDef(cell.TerrainDefIndex);
            return def != null;
        }

        private int Index(int cellX, int cellZ) => cellZ * Width + cellX;

        private bool InBounds(int cellX, int cellZ) =>
            cellX >= 0 && cellZ >= 0 && cellX < Width && cellZ < Height;

        private static int Clamp(int v, int min, int max)
        {
            if (v < min)
                return min;
            if (v > max)
                return max;
            return v;
        }
    }
}
