using Asterra.Core.World;

namespace Asterra.Core
{
    /// <summary>
    /// One sim entity is a battalion (BFME/DoW block). Presentation troop count and
    /// collision/formation spacing share these rules.
    /// </summary>
    public static class BattalionRules
    {
        public const int MaxMembers = 24;

        public static int MemberCount(UnitDefData def)
        {
            if (def == null)
                return 1;
            if (def.IsLeader || def.IsBuilder)
                return 1;
            if (def.Role == UnitRole.Siege)
                return 1;
            if (def.TraversalCapabilities == TraversalCapability.Water)
                return 1;
            if (def.SquadSize > 0)
            {
                int n = def.SquadSize;
                if (n < 1)
                    n = 1;
                if (n > MaxMembers)
                    n = MaxMembers;
                return n;
            }

            return DefaultMembers(def.Role);
        }

        public static int DefaultMembers(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Infantry:
                    return 16;
                case UnitRole.Ranged:
                    return 12;
                case UnitRole.Cavalry:
                    return 6;
                default:
                    return 1;
            }
        }

        public static float CollisionRadius(UnitDefData def)
        {
            int n = MemberCount(def);
            if (def != null && def.CollisionRadius > 4f)
                return def.CollisionRadius;
            if (n <= 1)
                return def != null && def.CollisionRadius > 0.1f ? def.CollisionRadius : 2.2f;
            if (n <= 6)
                return 5.2f;
            if (n <= 12)
                return 6.4f;
            return 7.2f;
        }

        public static float MoveOrderSpacing(int selectedBlocks)
        {
            if (selectedBlocks <= 1)
                return 0f;
            return 15.5f;
        }
    }
}
