using System;
using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Core.World
{
    /// <summary>
    /// Deterministic grid A* over <see cref="WorldTerrainGrid"/> path costs, with optional traversal-link bridges.
    /// Falls back to direct steer when start/goal are off-grid or search is exhausted.
    /// </summary>
    public sealed class GridAStarPathfindingService : IPathfindingService
    {
        private const int MaxExpanded = 2500;
        private const int MaxPathCells = 96;

        private readonly WorldTerrainGrid _grid;
        private readonly TraversalGraph _traversal;
        private readonly DirectSteerPathfindingService _fallback;

        private readonly float[] _gScore;
        private readonly float[] _fScore;
        private readonly int[] _cameFrom;
        private readonly byte[] _closed;
        private readonly int[] _openHeap;
        private int _openCount;
        private ulong _searchStamp;
        private readonly ulong[] _visitStamp;

        public GridAStarPathfindingService(WorldTerrainGrid grid, TraversalGraph traversal = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _traversal = traversal;
            _fallback = new DirectSteerPathfindingService(grid, traversal);
            int n = grid.Width * grid.Height;
            _gScore = new float[n];
            _fScore = new float[n];
            _cameFrom = new int[n];
            _closed = new byte[n];
            _openHeap = new int[n + 1];
            _visitStamp = new ulong[n];
        }

        public bool TryGetPath(float fromX, float fromZ, float toX, float toZ, List<(float x, float z)> pathOut) =>
            TryGetPath(fromX, fromZ, toX, toZ, TraversalCapability.Land, pathOut);

        public bool TryGetPath(
            float fromX,
            float fromZ,
            float toX,
            float toZ,
            TraversalCapability capabilities,
            List<(float x, float z)> pathOut)
        {
            if (pathOut == null)
                return false;
            pathOut.Clear();

            if (!_grid.TryWorldToCell(fromX, fromZ, out int sx, out int sz)
                || !_grid.TryWorldToCell(toX, toZ, out int gx, out int gz))
                return _fallback.TryGetPath(fromX, fromZ, toX, toZ, capabilities, pathOut);

            int start = Index(sx, sz);
            int goal = Index(gx, gz);
            if (start == goal)
            {
                pathOut.Add((toX, toZ));
                return true;
            }

            float startCost = _grid.GetPathCost(fromX, fromZ, capabilities);
            float goalCost = _grid.GetPathCost(toX, toZ, capabilities);
            if (startCost >= TerrainDefData.PathCostBlocked || goalCost >= TerrainDefData.PathCostBlocked)
            {
                // Destination blocked — try traversal link bridge via fallback.
                return _fallback.TryGetPath(fromX, fromZ, toX, toZ, capabilities, pathOut);
            }

            if (!RunAStar(start, goal, sx, sz, gx, gz, capabilities))
                return _fallback.TryGetPath(fromX, fromZ, toX, toZ, capabilities, pathOut);

            Reconstruct(goal, pathOut, toX, toZ);
            if (_traversal != null
                && LooksLineBlocked(fromX, fromZ, toX, toZ, capabilities)
                && _traversal.TryFindLinkForPath(fromX, fromZ, toX, toZ, capabilities, out var link, out bool forward))
            {
                // Prefer explicit link if A* still crossed a gap poorly — prepend link then goal.
                pathOut.Clear();
                if (forward)
                {
                    pathOut.Add((link.StartX, link.StartZ));
                    pathOut.Add((link.EndX, link.EndZ));
                }
                else
                {
                    pathOut.Add((link.EndX, link.EndZ));
                    pathOut.Add((link.StartX, link.StartZ));
                }

                pathOut.Add((toX, toZ));
            }

            return pathOut.Count > 0;
        }

        public void RequestFlowField(float toX, float toZ, int fieldId)
        {
            // Flow-field cache reserved for mass army moves; A* covers ordered paths today.
        }

        private bool RunAStar(int start, int goal, int sx, int sz, int gx, int gz, TraversalCapability capabilities)
        {
            _searchStamp++;
            if (_searchStamp == 0)
            {
                Array.Clear(_visitStamp, 0, _visitStamp.Length);
                _searchStamp = 1;
            }

            _openCount = 0;
            _gScore[start] = 0f;
            _fScore[start] = Heuristic(sx, sz, gx, gz);
            _cameFrom[start] = -1;
            _visitStamp[start] = _searchStamp;
            _closed[start] = 0;
            HeapPush(start);

            int expanded = 0;
            while (_openCount > 0 && expanded < MaxExpanded)
            {
                int current = HeapPop();
                if (_closed[current] == 1 && _visitStamp[current] == _searchStamp)
                    continue;
                _closed[current] = 1;
                expanded++;

                if (current == goal)
                    return true;

                int cx = current % _grid.Width;
                int cz = current / _grid.Width;
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0)
                            continue;
                        int nx = cx + dx;
                        int nz = cz + dz;
                        if (nx < 0 || nz < 0 || nx >= _grid.Width || nz >= _grid.Height)
                            continue;
                        int neighbor = Index(nx, nz);
                        if (_visitStamp[neighbor] == _searchStamp && _closed[neighbor] == 1)
                            continue;

                        _grid.CellCenter(nx, nz, out float wx, out float wz);
                        float stepCost = _grid.GetPathCost(wx, wz, capabilities);
                        if (stepCost >= TerrainDefData.PathCostBlocked)
                            continue;
                        if (dx != 0 && dz != 0)
                            stepCost *= 1.41421356f;

                        float tentative = (_visitStamp[current] == _searchStamp ? _gScore[current] : float.PositiveInfinity) + stepCost;
                        if (_visitStamp[neighbor] != _searchStamp || tentative < _gScore[neighbor])
                        {
                            _visitStamp[neighbor] = _searchStamp;
                            _cameFrom[neighbor] = current;
                            _gScore[neighbor] = tentative;
                            _fScore[neighbor] = tentative + Heuristic(nx, nz, gx, gz);
                            _closed[neighbor] = 0;
                            HeapPush(neighbor);
                        }
                    }
                }
            }

            return false;
        }

        private void Reconstruct(int goal, List<(float x, float z)> pathOut, float finalX, float finalZ)
        {
            var stack = new int[MaxPathCells];
            int count = 0;
            int cur = goal;
            while (cur >= 0 && count < MaxPathCells)
            {
                stack[count++] = cur;
                if (_visitStamp[cur] != _searchStamp)
                    break;
                cur = _cameFrom[cur];
            }

            // Skip start cell; emit every other cell to shorten, then exact goal.
            for (int i = count - 2; i >= 0; i -= 2)
            {
                int idx = stack[i];
                int cx = idx % _grid.Width;
                int cz = idx / _grid.Width;
                _grid.CellCenter(cx, cz, out float x, out float z);
                pathOut.Add((x, z));
            }

            pathOut.Add((finalX, finalZ));
        }

        private bool LooksLineBlocked(float fromX, float fromZ, float toX, float toZ, TraversalCapability capabilities)
        {
            float mx = (fromX + toX) * 0.5f;
            float mz = (fromZ + toZ) * 0.5f;
            return _grid.GetPathCost(mx, mz, capabilities) >= TerrainDefData.PathCostBlocked;
        }

        private static float Heuristic(int ax, int az, int bx, int bz)
        {
            int dx = Math.Abs(ax - bx);
            int dz = Math.Abs(az - bz);
            int ortho = Math.Abs(dx - dz);
            int diag = Math.Min(dx, dz);
            return diag * 1.41421356f + ortho;
        }

        private int Index(int x, int z) => z * _grid.Width + x;

        private void HeapPush(int node)
        {
            _openHeap[++_openCount] = node;
            int i = _openCount;
            while (i > 1)
            {
                int p = i >> 1;
                if (_fScore[_openHeap[p]] <= _fScore[_openHeap[i]])
                    break;
                int tmp = _openHeap[p];
                _openHeap[p] = _openHeap[i];
                _openHeap[i] = tmp;
                i = p;
            }
        }

        private int HeapPop()
        {
            int root = _openHeap[1];
            _openHeap[1] = _openHeap[_openCount--];
            int i = 1;
            while (true)
            {
                int l = i << 1;
                int r = l + 1;
                if (l > _openCount)
                    break;
                int best = l;
                if (r <= _openCount && _fScore[_openHeap[r]] < _fScore[_openHeap[l]])
                    best = r;
                if (_fScore[_openHeap[i]] <= _fScore[_openHeap[best]])
                    break;
                int tmp = _openHeap[i];
                _openHeap[i] = _openHeap[best];
                _openHeap[best] = tmp;
                i = best;
            }

            return root;
        }
    }
}
