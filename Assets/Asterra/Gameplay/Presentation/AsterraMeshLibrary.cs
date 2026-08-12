using System.Collections.Generic;
using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Runtime low-poly meshes matching Assets/Asterra/Shared/Art/Meshes/*.obj
    /// (regenerate OBJs via tools/meshgen for Blender editing).
    /// </summary>
    public static class AsterraMeshLibrary
    {
        private static readonly Dictionary<string, Mesh> Cache = new();

        public static Mesh GetUnitMesh(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return GetOrCreate("unit_militia", BuildMilitia);

            if (definitionId.Contains("builder"))
                return GetOrCreate("unit_builder", BuildBuilder);
            if (definitionId.Contains("archer"))
                return GetOrCreate("unit_archer", BuildArcher);
            if (definitionId.Contains("knight") || definitionId.Contains("cavalry"))
                return GetOrCreate("unit_cavalry", BuildCavalry);
            if (definitionId.Contains("catapult") || definitionId.Contains("siege") || definitionId.Contains("mortar"))
                return GetOrCreate("unit_siege", BuildSiege);
            if (definitionId.Contains("dryad"))
                return GetOrCreate("unit_dryad", BuildDryad);
            if (definitionId.Contains("ember"))
                return GetOrCreate("unit_ember_raider", BuildEmberRaider);
            return GetOrCreate("unit_militia", BuildMilitia);
        }

        public static Mesh GetBuildingMesh(string definitionId)
        {
            if (IsKeep(definitionId))
                return GetOrCreate("building_keep", BuildKeep);
            if (!string.IsNullOrEmpty(definitionId))
            {
                if (definitionId.Contains("tower") || definitionId.Contains("watchtower"))
                    return GetOrCreate("building_tower", BuildTower);
                if (definitionId.Contains("palisade") || definitionId.Contains("wall"))
                    return GetOrCreate("building_wall", BuildWall);
                if (definitionId.Contains("outpost"))
                    return GetOrCreate("building_outpost", BuildOutpost);
            }

            return GetOrCreate("building_producer", BuildProducer);
        }

        public static Mesh GetResourceMesh(ResourceType type)
        {
            return type == ResourceType.Gold
                ? GetOrCreate("resource_gold", BuildGoldNugget)
                : GetOrCreate("resource_timber", BuildTimberLog);
        }

        public static bool IsKeep(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;
            return definitionId.Contains("keep")
                   || definitionId.Contains("heartwood")
                   || definitionId.Contains("citadel");
        }

        public static UnitRole InferRole(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return UnitRole.Infantry;
            if (definitionId.Contains("builder"))
                return UnitRole.Builder;
            if (definitionId.Contains("archer") || definitionId.Contains("bow"))
                return UnitRole.Ranged;
            if (definitionId.Contains("knight") || definitionId.Contains("cavalry") || definitionId.Contains("rider"))
                return UnitRole.Cavalry;
            if (definitionId.Contains("catapult") || definitionId.Contains("siege") || definitionId.Contains("mortar")
                || definitionId.Contains("engine"))
                return UnitRole.Siege;
            return UnitRole.Infantry;
        }

        public static Color RoleAccent(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Infantry:
                    return new Color(0.92f, 0.93f, 0.96f);
                case UnitRole.Ranged:
                    return new Color(0.25f, 0.85f, 0.95f);
                case UnitRole.Cavalry:
                    return new Color(0.95f, 0.78f, 0.22f);
                case UnitRole.Siege:
                    return new Color(0.95f, 0.5f, 0.18f);
                case UnitRole.Builder:
                    return new Color(0.95f, 0.88f, 0.25f);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public static float RoleScaleMultiplier(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Cavalry:
                    return 1.15f;
                case UnitRole.Siege:
                    return 1.25f;
                case UnitRole.Ranged:
                    return 0.95f;
                case UnitRole.Builder:
                    return 0.9f;
                case UnitRole.Infantry:
                    return 1f;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public static Color FactionColor(byte factionIndex)
        {
            switch (factionIndex)
            {
                case 0: return new Color(0.55f, 0.58f, 0.65f); // iron
                case 1: return new Color(0.25f, 0.55f, 0.32f); // verdant
                case 2: return new Color(0.65f, 0.28f, 0.18f); // ashen
                default: return Color.gray;
            }
        }

        public static Color ResourceColor(ResourceType type)
        {
            return type == ResourceType.Gold
                ? new Color(0.98f, 0.84f, 0.18f)
                : new Color(0.48f, 0.3f, 0.14f);
        }

        private static Mesh GetOrCreate(string key, System.Func<Mesh> builder)
        {
            if (Cache.TryGetValue(key, out var mesh) && mesh != null)
                return mesh;
            mesh = builder();
            mesh.name = key;
            Cache[key] = mesh;
            return mesh;
        }

        // --- Units: readable silhouettes at RTS camera distance ---

        private static Mesh BuildMilitia()
        {
            // Compact infantry with shield + short spear.
            return Combine(
                Box(0, 0, 0, 0.75f, 1.35f, 0.55f),
                Box(0, 1.35f, 0, 0.48f, 0.48f, 0.48f),
                Box(-0.55f, 0.55f, 0.05f, 0.55f, 0.7f, 0.12f),
                Box(0.5f, 0.75f, 0, 0.14f, 0.14f, 1.35f));
        }

        private static Mesh BuildBuilder()
        {
            // Worker with hammer / tool silhouette.
            return Combine(
                Box(0, 0, 0, 0.7f, 1.15f, 0.55f),
                Box(0, 1.15f, 0, 0.42f, 0.42f, 0.42f),
                Box(0.7f, 0.55f, 0, 1.05f, 0.16f, 0.16f),
                Box(1.15f, 0.55f, 0, 0.35f, 0.55f, 0.28f),
                Box(-0.45f, 0.35f, 0.2f, 0.35f, 0.35f, 0.35f));
        }

        private static Mesh BuildArcher()
        {
            // Slim body + vertical bow + horizontal string.
            return Combine(
                Box(0, 0, 0, 0.5f, 1.4f, 0.42f),
                Box(0, 1.4f, 0, 0.38f, 0.38f, 0.38f),
                Box(0.05f, 0.85f, 0.55f, 0.1f, 1.15f, 0.1f),
                Box(0.05f, 0.85f, -0.55f, 0.1f, 1.15f, 0.1f),
                Box(0.05f, 1.35f, 0, 0.08f, 0.08f, 1.1f),
                Box(0.05f, 0.35f, 0, 0.08f, 0.08f, 1.1f),
                Box(0.45f, 0.9f, 0, 0.55f, 0.08f, 0.08f));
        }

        private static Mesh BuildCavalry()
        {
            // Longer horse body + rider.
            return Combine(
                Box(0, 0.05f, 0, 1.55f, 0.65f, 0.55f),
                Box(0.7f, 0.55f, 0, 0.4f, 0.4f, 0.4f),
                Box(-0.15f, 0.7f, 0, 0.55f, 0.85f, 0.42f),
                Box(-0.15f, 1.5f, 0, 0.38f, 0.35f, 0.38f),
                Box(0.95f, 0.15f, 0, 0.25f, 0.2f, 0.2f),
                Box(-0.7f, 0.0f, 0.22f, 0.18f, 0.35f, 0.18f),
                Box(-0.7f, 0.0f, -0.22f, 0.18f, 0.35f, 0.18f),
                Box(0.55f, 0.0f, 0.22f, 0.18f, 0.35f, 0.18f),
                Box(0.55f, 0.0f, -0.22f, 0.18f, 0.35f, 0.18f));
        }

        private static Mesh BuildSiege()
        {
            // Wagon chassis, wheels, and angled arm.
            return Combine(
                Box(0, 0.35f, 0, 1.6f, 0.5f, 1.0f),
                Box(0, 0.85f, 0, 0.75f, 0.65f, 0.75f),
                Box(0.35f, 1.15f, 0, 1.35f, 0.16f, 0.16f),
                Box(0.95f, 1.25f, 0, 0.35f, 0.35f, 0.35f),
                Box(-0.65f, 0.0f, 0.55f, 0.35f, 0.35f, 0.18f),
                Box(-0.65f, 0.0f, -0.55f, 0.35f, 0.35f, 0.18f),
                Box(0.65f, 0.0f, 0.55f, 0.35f, 0.35f, 0.18f),
                Box(0.65f, 0.0f, -0.55f, 0.35f, 0.35f, 0.18f));
        }

        private static Mesh BuildDryad()
        {
            return Combine(
                Box(0, 0, 0, 0.55f, 1.6f, 0.45f),
                Box(0, 1.55f, 0, 0.85f, 0.4f, 0.85f),
                Box(0, 1.9f, 0, 0.45f, 0.35f, 0.45f));
        }

        private static Mesh BuildEmberRaider()
        {
            return Combine(
                Box(0, 0, 0, 0.85f, 1.3f, 0.55f),
                Box(0, 1.25f, 0, 0.42f, 0.42f, 0.42f),
                Box(-0.6f, 1.0f, 0, 0.4f, 0.28f, 0.55f),
                Box(0.6f, 1.0f, 0, 0.4f, 0.28f, 0.55f));
        }

        // --- Buildings ---

        private static Mesh BuildKeep()
        {
            // Wide base keep with corner battlements — largest silhouette.
            return Combine(
                Box(0, 0, 0, 7f, 3.2f, 7f),
                Box(0, 3.2f, 0, 4.2f, 5.5f, 4.2f),
                Box(0, 8.5f, 0, 2.2f, 1.8f, 2.2f),
                Box(-2.0f, 8.7f, -2.0f, 1.4f, 1.6f, 1.4f),
                Box(2.0f, 8.7f, -2.0f, 1.4f, 1.6f, 1.4f),
                Box(-2.0f, 8.7f, 2.0f, 1.4f, 1.6f, 1.4f),
                Box(2.0f, 8.7f, 2.0f, 1.4f, 1.6f, 1.4f));
        }

        private static Mesh BuildProducer()
        {
            // Barracks / hall: wide roofed hall with twin chimneys.
            return Combine(
                Box(0, 0, 0, 5.5f, 2.4f, 4.4f),
                Box(0, 2.4f, 0, 4.2f, 1.2f, 3.4f),
                Box(0, 3.5f, 0, 5.8f, 0.45f, 0.9f),
                Box(-2.4f, 0, -1.6f, 1.1f, 4.2f, 1.1f),
                Box(2.4f, 0, -1.6f, 1.1f, 4.2f, 1.1f),
                Box(0, 0.2f, 2.3f, 1.6f, 2.0f, 0.35f));
        }

        private static Mesh BuildTower()
        {
            // Tall thin watchtower with lookout cupola.
            return Combine(
                Box(0, 0, 0, 2.6f, 1.6f, 2.6f),
                Box(0, 1.6f, 0, 1.7f, 8.5f, 1.7f),
                Box(0, 10.0f, 0, 2.6f, 1.0f, 2.6f),
                Box(0, 11.0f, 0, 1.2f, 1.4f, 1.2f),
                Box(0, 12.3f, 0, 0.35f, 1.2f, 0.35f));
        }

        private static Mesh BuildWall()
        {
            // Flat palisade segment with stake tops.
            return Combine(
                Box(0, 0, 0, 11f, 3.6f, 1.4f),
                Box(-4.5f, 3.6f, 0, 0.9f, 1.4f, 0.9f),
                Box(-1.5f, 3.6f, 0, 0.9f, 1.6f, 0.9f),
                Box(1.5f, 3.6f, 0, 0.9f, 1.4f, 0.9f),
                Box(4.5f, 3.6f, 0, 0.9f, 1.6f, 0.9f));
        }

        private static Mesh BuildOutpost()
        {
            // Medium post with flag-like top.
            return Combine(
                Box(0, 0, 0, 3.8f, 1.8f, 3.8f),
                Box(0, 1.8f, 0, 2.4f, 2.8f, 2.4f),
                Box(0, 4.6f, 0, 0.4f, 3.2f, 0.4f),
                Box(0.7f, 6.8f, 0, 1.5f, 0.9f, 0.12f),
                Box(0.15f, 7.4f, 0, 0.25f, 0.25f, 0.25f));
        }

        // --- Resources ---

        private static Mesh BuildGoldNugget()
        {
            // Stacked / chunky gold crystals.
            return Combine(
                Box(0, 0, 0, 1.4f, 0.9f, 1.1f),
                Box(0.45f, 0.7f, 0.15f, 0.9f, 0.85f, 0.75f),
                Box(-0.4f, 0.55f, -0.25f, 0.7f, 0.7f, 0.65f),
                Box(0.1f, 1.35f, 0, 0.55f, 0.55f, 0.5f));
        }

        private static Mesh BuildTimberLog()
        {
            // Horizontal log + stump.
            return Combine(
                Box(0, 0.35f, 0, 2.4f, 0.7f, 0.7f),
                Box(-0.95f, 0.0f, 0, 0.55f, 0.7f, 0.55f),
                Box(0.95f, 0.0f, 0, 0.55f, 0.7f, 0.55f),
                Box(0.2f, 0.85f, 0.15f, 1.4f, 0.45f, 0.45f));
        }

        private static Mesh Combine(params MeshPart[] parts)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            foreach (var part in parts)
            {
                int offset = verts.Count;
                verts.AddRange(part.Verts);
                for (int i = 0; i < part.Tris.Length; i++)
                    tris.Add(part.Tris[i] + offset);
            }

            var mesh = new Mesh { name = "combined" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static MeshPart Box(float cx, float cy, float cz, float sx, float sy, float sz)
        {
            float hx = sx * 0.5f;
            float hz = sz * 0.5f;
            var v = new[]
            {
                new Vector3(cx - hx, cy, cz - hz),
                new Vector3(cx + hx, cy, cz - hz),
                new Vector3(cx + hx, cy, cz + hz),
                new Vector3(cx - hx, cy, cz + hz),
                new Vector3(cx - hx, cy + sy, cz - hz),
                new Vector3(cx + hx, cy + sy, cz - hz),
                new Vector3(cx + hx, cy + sy, cz + hz),
                new Vector3(cx - hx, cy + sy, cz + hz),
            };
            var t = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 6, 2, 3, 7, 6,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
            };
            return new MeshPart(v, t);
        }

        private readonly struct MeshPart
        {
            public readonly Vector3[] Verts;
            public readonly int[] Tris;

            public MeshPart(Vector3[] verts, int[] tris)
            {
                Verts = verts;
                Tris = tris;
            }
        }
    }
}
