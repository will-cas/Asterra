using System;
using System.Collections.Generic;

namespace Asterra.Core.World
{
    /// <summary>
    /// Bidirectional traversal connections (bridges, jumps, magic crossings).
    /// Indexed by nearby terrain cells for cheap queries.
    /// </summary>
    public sealed class TraversalGraph : ITraversalGraph
    {
        private readonly List<TraversalLink> _links = new();
        private readonly Dictionary<long, List<int>> _byCell = new();
        private readonly WorldTerrainGrid _grid;

        public IReadOnlyList<TraversalLink> Links => _links;

        public TraversalGraph(WorldTerrainGrid grid = null)
        {
            _grid = grid;
        }

        public int AddLink(
            float startX,
            float startZ,
            float endX,
            float endZ,
            TraversalLinkType type,
            TraversalCapability allowedCapabilities,
            float durationSeconds = 1.25f,
            bool allowsCombat = false,
            bool enabled = true,
            bool isDestructible = true,
            bool canBeBlocked = true,
            bool requiresAnimation = false,
            float approachRadius = 8f)
        {
            int id = _links.Count;
            var link = new TraversalLink(
                id,
                startX,
                startZ,
                endX,
                endZ,
                type,
                allowedCapabilities,
                durationSeconds,
                allowsCombat,
                enabled,
                isDestructible,
                canBeBlocked,
                requiresAnimation,
                approachRadius);
            _links.Add(link);
            IndexEndpoint(startX, startZ, id);
            IndexEndpoint(endX, endZ, id);
            return id;
        }

        /// <summary>Legacy helper matching older TraversalLink construction.</summary>
        public int AddLink(TraversalLink link)
        {
            return AddLink(
                link.StartX,
                link.StartZ,
                link.EndX,
                link.EndZ,
                link.Type,
                link.AllowedCapabilities,
                link.DurationSeconds,
                link.AllowsCombat,
                link.Enabled,
                link.IsDestructible,
                link.CanBeBlocked,
                link.RequiresAnimation,
                link.ApproachRadius);
        }

        public void SetLinkEnabled(int linkId, bool enabled)
        {
            if (linkId < 0 || linkId >= _links.Count)
                return;
            var L = _links[linkId];
            _links[linkId] = new TraversalLink(
                L.Id,
                L.StartX,
                L.StartZ,
                L.EndX,
                L.EndZ,
                L.Type,
                L.AllowedCapabilities,
                L.DurationSeconds,
                L.AllowsCombat,
                enabled,
                L.IsDestructible,
                L.CanBeBlocked,
                L.RequiresAnimation,
                L.ApproachRadius);
        }

        public bool TryGetLink(int linkId, out TraversalLink link)
        {
            if (linkId < 0 || linkId >= _links.Count)
            {
                link = default;
                return false;
            }

            link = _links[linkId];
            return true;
        }

        public bool TryGetLinksFrom(int cellX, int cellZ, List<TraversalLink> results)
        {
            if (results == null)
                return false;
            results.Clear();
            long key = CellKey(cellX, cellZ);
            if (!_byCell.TryGetValue(key, out var ids))
                return false;
            for (int i = 0; i < ids.Count; i++)
            {
                var link = _links[ids[i]];
                if (link.Enabled)
                    results.Add(link);
            }

            return results.Count > 0;
        }

        public bool TryFindLinkForMove(
            float unitX,
            float unitZ,
            float destX,
            float destZ,
            TraversalCapability capabilities,
            float approachRadius,
            out TraversalLink link,
            out bool forward)
        {
            return TryFindLinkInternal(
                unitX,
                unitZ,
                destX,
                destZ,
                capabilities,
                approachRadius,
                requireNearEndpoint: true,
                out link,
                out forward);
        }

        /// <summary>
        /// Path-planning helper: choose a link that improves progress toward dest even if the unit
        /// is not yet at the approach radius (waypoints are inserted for steering).
        /// </summary>
        public bool TryFindLinkForPath(
            float fromX,
            float fromZ,
            float destX,
            float destZ,
            TraversalCapability capabilities,
            out TraversalLink link,
            out bool forward)
        {
            return TryFindLinkInternal(
                fromX,
                fromZ,
                destX,
                destZ,
                capabilities,
                approachRadius: float.MaxValue,
                requireNearEndpoint: false,
                out link,
                out forward);
        }

        private bool TryFindLinkInternal(
            float unitX,
            float unitZ,
            float destX,
            float destZ,
            TraversalCapability capabilities,
            float approachRadius,
            bool requireNearEndpoint,
            out TraversalLink link,
            out bool forward)
        {
            link = default;
            forward = true;
            float bestScore = float.MaxValue;
            bool found = false;
            float destDistFromUnit = Dist(unitX, unitZ, destX, destZ);

            for (int i = 0; i < _links.Count; i++)
            {
                var candidate = _links[i];
                if (!candidate.Allows(capabilities))
                    continue;

                float radius = approachRadius > 0f ? approachRadius : candidate.ApproachRadius;

                float dStart = Dist(unitX, unitZ, candidate.StartX, candidate.StartZ);
                if (!requireNearEndpoint || dStart <= radius)
                {
                    float endToDest = Dist(candidate.EndX, candidate.EndZ, destX, destZ);
                    float startToDest = Dist(candidate.StartX, candidate.StartZ, destX, destZ);
                    if (endToDest + 1f < startToDest && endToDest < destDistFromUnit)
                    {
                        float score = dStart + endToDest;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            link = candidate;
                            forward = true;
                            found = true;
                        }
                    }
                }

                float dEnd = Dist(unitX, unitZ, candidate.EndX, candidate.EndZ);
                if (!requireNearEndpoint || dEnd <= radius)
                {
                    float startToDest = Dist(candidate.StartX, candidate.StartZ, destX, destZ);
                    float endToDest = Dist(candidate.EndX, candidate.EndZ, destX, destZ);
                    if (startToDest + 1f < endToDest && startToDest < destDistFromUnit)
                    {
                        float score = dEnd + startToDest;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            link = candidate;
                            forward = false;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        private void IndexEndpoint(float x, float z, int linkId)
        {
            if (_grid == null)
                return;
            if (!_grid.TryWorldToCell(x, z, out int cx, out int cz))
                return;

            // Index neighbouring cells so approach radius queries stay cheap.
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx;
                    int nz = cz + dz;
                    if (nx < 0 || nz < 0 || nx >= _grid.Width || nz >= _grid.Height)
                        continue;
                    long key = CellKey(nx, nz);
                    if (!_byCell.TryGetValue(key, out var list))
                    {
                        list = new List<int>(2);
                        _byCell[key] = list;
                    }

                    if (!list.Contains(linkId))
                        list.Add(linkId);
                }
            }
        }

        private static long CellKey(int cx, int cz) => ((long)cz << 32) ^ (uint)cx;

        private static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = bx - ax;
            float dz = bz - az;
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }
}
