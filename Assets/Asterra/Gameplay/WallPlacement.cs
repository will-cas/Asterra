using System;

namespace Asterra.Gameplay
{
    /// <summary>Snaps wall / gate footprints to a shared world grid so segments align.</summary>
    public static class WallPlacement
    {
        public const float DefaultSegment = 14f;

        public static void Snap(ref float x, ref float z, float segmentLength = DefaultSegment)
        {
            float s = segmentLength > 1f ? segmentLength : DefaultSegment;
            x = MathF.Round(x / s) * s;
            z = MathF.Round(z / s) * s;
        }

        public static int CardinalIndex(float fromX, float fromZ, float toX, float toZ)
        {
            float dx = toX - fromX;
            float dz = toZ - fromZ;
            if (MathF.Abs(dx) >= MathF.Abs(dz))
                return dx >= 0f ? 1 : 3; // E : W
            return dz >= 0f ? 0 : 2; // N : S
        }
    }
}
