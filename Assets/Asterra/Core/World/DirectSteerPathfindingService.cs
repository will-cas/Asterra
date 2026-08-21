using System;
using System.Collections.Generic;
using Asterra.Core.World;

namespace Asterra.Core
{
    /// <summary>
    /// Steer pathfinding with optional traversal-link waypoints (bridges / jumps / crossings).
    /// </summary>
    public sealed class DirectSteerPathfindingService : IPathfindingService
    {
        private readonly ITerrainMap _terrain;
        private readonly TraversalGraph _traversal;

        public DirectSteerPathfindingService(ITerrainMap terrain = null, TraversalGraph traversal = null)
        {
            _terrain = terrain;
            _traversal = traversal;
        }

        public bool TryGetPath(float fromX, float fromZ, float toX, float toZ, List<(float x, float z)> pathOut)
        {
            return TryGetPath(fromX, fromZ, toX, toZ, TraversalCapability.Land, pathOut);
        }

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

            bool destOk = _terrain == null || _terrain.IsTraversable(toX, toZ, capabilities);
            if (destOk && (_terrain == null || _terrain.IsTraversable(fromX, fromZ, capabilities)))
            {
                // Direct path when both ends are fine — still may insert a link if a gap sits between.
                if (_traversal != null
                    && _traversal.TryFindLinkForPath(fromX, fromZ, toX, toZ, capabilities, out var link, out bool forward))
                {
                    // Only insert if the straight-line midpoints look blocked.
                    if (LooksBlocked(fromX, fromZ, toX, toZ, capabilities))
                    {
                        AppendLink(pathOut, link, forward);
                        pathOut.Add((toX, toZ));
                        return true;
                    }
                }

                pathOut.Add((toX, toZ));
                return true;
            }

            if (_traversal != null
                && _traversal.TryFindLinkForPath(fromX, fromZ, toX, toZ, capabilities, out var gapLink, out bool gapForward))
            {
                AppendLink(pathOut, gapLink, gapForward);
                if (destOk)
                    pathOut.Add((toX, toZ));
                return pathOut.Count > 0;
            }

            if (destOk)
            {
                pathOut.Add((toX, toZ));
                return true;
            }

            return false;
        }

        public void RequestFlowField(float toX, float toZ, int fieldId)
        {
            // Flow fields arrive in a later pathfinding slice.
        }

        private bool LooksBlocked(float fromX, float fromZ, float toX, float toZ, TraversalCapability capabilities)
        {
            if (_terrain == null)
                return false;
            float mx = (fromX + toX) * 0.5f;
            float mz = (fromZ + toZ) * 0.5f;
            return !_terrain.IsTraversable(mx, mz, capabilities);
        }

        private static void AppendLink(List<(float x, float z)> pathOut, TraversalLink link, bool forward)
        {
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
        }
    }
}
