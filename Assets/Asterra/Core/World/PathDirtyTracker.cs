using System.Collections.Generic;

namespace Asterra.Core.World
{
    /// <summary>
    /// Tracks local regions that need path/terrain refresh after destruction.
    /// Avoids full-map rebuilds — consumers process and clear the queue each tick.
    /// </summary>
    public sealed class PathDirtyTracker
    {
        private readonly List<PathDirtyRegion> _regions = new();

        public IReadOnlyList<PathDirtyRegion> Regions => _regions;
        public int Count => _regions.Count;

        public void Mark(float minX, float minZ, float maxX, float maxZ, PathDirtyReason reason)
        {
            if (minX > maxX)
            {
                float t = minX;
                minX = maxX;
                maxX = t;
            }

            if (minZ > maxZ)
            {
                float t = minZ;
                minZ = maxZ;
                maxZ = t;
            }

            _regions.Add(new PathDirtyRegion(minX, minZ, maxX, maxZ, reason));
        }

        public void MarkRadius(float x, float z, float radius, PathDirtyReason reason)
        {
            Mark(x - radius, z - radius, x + radius, z + radius, reason);
        }

        public void Clear() => _regions.Clear();
    }

    public enum PathDirtyReason : byte
    {
        DestructibleCleared = 0,
        BridgeDisabled = 1,
        WallRemoved = 2,
        TerrainPainted = 3,
        WallAdded = 4,
    }

    public readonly struct PathDirtyRegion
    {
        public readonly float MinX;
        public readonly float MinZ;
        public readonly float MaxX;
        public readonly float MaxZ;
        public readonly PathDirtyReason Reason;

        public PathDirtyRegion(float minX, float minZ, float maxX, float maxZ, PathDirtyReason reason)
        {
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
            Reason = reason;
        }
    }
}
