using System;
using System.Collections.Generic;
using Asterra.Core.World;

namespace Asterra.Core
{
    /// <summary>
    /// Steer pathfinding with optional traversal-link waypoints (bridges / jumps / crossings).
    /// Fails when the straight corridor is blocked and no usable link exists.
    /// </summary>
    public sealed class DirectSteerPathfindingService : IPathfindingService
    {
        private const int LineSamples = 8;

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

            bool startOk = _terrain == null || _terrain.IsTraversable(fromX, fromZ, capabilities);
            bool destOk = _terrain == null || _terrain.IsTraversable(toX, toZ, capabilities);
            if (!startOk || !destOk)
            {
                if (_traversal != null
                    && _traversal.TryFindLinkForPath(fromX, fromZ, toX, toZ, capabilities, out var gapLink, out bool gapForward))
                {
                    AppendLink(pathOut, gapLink, gapForward);
                    if (destOk)
                        pathOut.Add((toX, toZ));
                    return pathOut.Count > 0;
                }

                return false;
            }

            bool corridorBlocked = LineBlocked(fromX, fromZ, toX, toZ, capabilities);
            if (corridorBlocked)
            {
                if (_traversal != null
                    && _traversal.TryFindLinkForPath(fromX, fromZ, toX, toZ, capabilities, out var link, out bool forward))
                {
                    AppendLink(pathOut, link, forward);
                    pathOut.Add((toX, toZ));
                    return true;
                }

                return false;
            }

            pathOut.Add((toX, toZ));
            return true;
        }

        public void RequestFlowField(float toX, float toZ, int fieldId)
        {
            // Flow fields arrive in a later pathfinding slice.
        }

        private bool LineBlocked(float fromX, float fromZ, float toX, float toZ, TraversalCapability capabilities)
        {
            if (_terrain == null)
                return false;
            for (int i = 1; i < LineSamples; i++)
            {
                float t = i / (float)LineSamples;
                float x = fromX + (toX - fromX) * t;
                float z = fromZ + (toZ - fromZ) * t;
                if (!_terrain.IsTraversable(x, z, capabilities))
                    return true;
            }

            return false;
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
