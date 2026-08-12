using System.Collections.Generic;
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
            if (definitionId != null && definitionId.Contains("dryad"))
                return GetOrCreate("unit_dryad", BuildDryad);
            if (definitionId != null && definitionId.Contains("ember"))
                return GetOrCreate("unit_ember_raider", BuildEmberRaider);
            return GetOrCreate("unit_militia", BuildMilitia);
        }

        public static Mesh GetBuildingMesh(string definitionId)
        {
            if (IsKeep(definitionId))
                return GetOrCreate("building_keep", BuildKeep);
            return GetOrCreate("building_producer", BuildProducer);
        }

        public static bool IsKeep(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;
            return definitionId.Contains("keep")
                   || definitionId.Contains("heartwood")
                   || definitionId.Contains("citadel");
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

        private static Mesh GetOrCreate(string key, System.Func<Mesh> builder)
        {
            if (Cache.TryGetValue(key, out var mesh) && mesh != null)
                return mesh;
            mesh = builder();
            mesh.name = key;
            Cache[key] = mesh;
            return mesh;
        }

        private static Mesh BuildMilitia()
        {
            return Combine(
                Box(0, 0, 0, 0.7f, 1.4f, 0.5f),
                Box(0, 1.4f, 0, 0.45f, 0.45f, 0.45f),
                Box(0.45f, 0.7f, 0, 0.15f, 0.15f, 1.2f));
        }

        private static Mesh BuildDryad()
        {
            return Combine(
                Box(0, 0, 0, 0.55f, 1.6f, 0.45f),
                Box(0, 1.55f, 0, 0.7f, 0.35f, 0.7f));
        }

        private static Mesh BuildEmberRaider()
        {
            return Combine(
                Box(0, 0, 0, 0.8f, 1.3f, 0.55f),
                Box(0, 1.25f, 0, 0.4f, 0.4f, 0.4f),
                Box(-0.55f, 1.0f, 0, 0.35f, 0.25f, 0.5f),
                Box(0.55f, 1.0f, 0, 0.35f, 0.25f, 0.5f));
        }

        private static Mesh BuildKeep()
        {
            return Combine(
                Box(0, 0, 0, 6f, 3f, 6f),
                Box(0, 3f, 0, 3.5f, 5f, 3.5f),
                Box(-1.5f, 8f, -1.5f, 1.2f, 1.2f, 1.2f),
                Box(1.5f, 8f, -1.5f, 1.2f, 1.2f, 1.2f),
                Box(-1.5f, 8f, 1.5f, 1.2f, 1.2f, 1.2f),
                Box(1.5f, 8f, 1.5f, 1.2f, 1.2f, 1.2f));
        }

        private static Mesh BuildProducer()
        {
            return Combine(
                Box(0, 0, 0, 5f, 2.2f, 4f),
                Box(0, 2.2f, 0, 3f, 1.5f, 3f),
                Box(-2.2f, 0, -1.5f, 1f, 3.5f, 1f),
                Box(2.2f, 0, -1.5f, 1f, 3.5f, 1f));
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
