using Asterra.Core;
using Asterra.Core.World;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Presentation helper: combat units render as a troop block (still one sim entity).
    /// Infantry ≈ company, ranged/cavalry smaller blocks; heroes/builders/siege stay solo.
    /// </summary>
    public static class UnitSquadVisual
    {
        public const int MaxSquadSize = 24;

        public static int ResolveSquadSize(UnitDefData def)
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
                return Mathf.Clamp(def.SquadSize, 1, MaxSquadSize);

            return DefaultForRole(def.Role);
        }

        public static int ResolveSquadSize(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return 1;
            if (definitionId.Contains("builder")
                || definitionId.Contains("lucien")
                || definitionId.Contains("captain")
                || definitionId.Contains("hierophant")
                || definitionId.Contains("leader")
                || definitionId.Contains("boat")
                || definitionId.Contains("catapult")
                || definitionId.Contains("siege")
                || definitionId.Contains("ballista")
                || definitionId.Contains("mortar")
                || definitionId.Contains("guardian"))
                return 1;

            var role = AsterraMeshLibrary.InferRole(definitionId);
            return DefaultForRole(role);
        }

        public static int DefaultForRole(UnitRole role)
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

        /// <summary>Local body-space offset for troop index (wide rank formation, easy to read).</summary>
        public static Vector3 TroopOffset(int index, int squadSize)
        {
            squadSize = Mathf.Clamp(squadSize, 1, MaxSquadSize);
            if (squadSize <= 1 || index < 0 || index >= squadSize)
                return Vector3.zero;

            int cols = ColumnsFor(squadSize);
            int rows = (squadSize + cols - 1) / cols;
            int col = index % cols;
            int row = index / cols;
            float spacing = SpacingFor(squadSize);
            float x = (col - (cols - 1) * 0.5f) * spacing;
            float z = ((rows - 1) * 0.5f - row) * spacing;
            float jx = ((index * 37) % 7 - 3) * 0.012f;
            float jz = ((index * 53) % 7 - 3) * 0.012f;
            return new Vector3(x + jx, 0f, z + jz);
        }

        public static float TroopLocalScale(int squadSize)
        {
            // Keep individuals large enough to read at RTS camera distance.
            // Parent EntityView already applies UnitVisualScale (~8).
            if (squadSize <= 1)
                return 1f;
            if (squadSize <= 2)
                return 0.88f;
            if (squadSize <= 6)
                return 0.66f;
            if (squadSize <= 12)
                return 0.55f;
            return 0.5f; // 16–24 company — still chunky silhouettes
        }

        private static int ColumnsFor(int squadSize)
        {
            if (squadSize <= 2)
                return squadSize;
            if (squadSize <= 6)
                return 3;
            if (squadSize <= 12)
                return 4;
            if (squadSize <= 16)
                return 4;
            return 5;
        }

        private static float SpacingFor(int squadSize)
        {
            // Local spacing × UnitVisualScale(~8) ≈ world gap between troop centres.
            // 0.55 * 8 ≈ 4.4u — wide ranks so companies read as many soldiers.
            if (squadSize <= 2)
                return 0.72f;
            if (squadSize <= 6)
                return 0.64f;
            if (squadSize <= 12)
                return 0.58f;
            return 0.55f;
        }
    }
}
