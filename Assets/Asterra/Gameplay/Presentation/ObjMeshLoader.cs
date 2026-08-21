using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Loads Wavefront OBJ meshes from Assets/Asterra/Shared/Art/Meshes at runtime (Editor + player).</summary>
    public static class ObjMeshLoader
    {
        private static readonly Dictionary<string, Mesh> Cache = new();

        public static bool TryLoad(string meshName, out Mesh mesh)
        {
            mesh = null;
            if (string.IsNullOrEmpty(meshName))
                return false;
            if (Cache.TryGetValue(meshName, out mesh) && mesh != null)
                return true;

            string path = Path.Combine(Application.dataPath, "Asterra", "Shared", "Art", "Meshes", meshName + ".obj");
            if (!File.Exists(path))
                return false;

            try
            {
                mesh = Parse(File.ReadAllText(path), meshName);
                if (mesh == null)
                    return false;
                Cache[meshName] = mesh;
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Asterra] OBJ load failed for {meshName}: {ex.Message}");
                return false;
            }
        }

        private static Mesh Parse(string text, string name)
        {
            var verts = new List<Vector3>(128);
            var tris = new List<int>(256);
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length < 2 || line[0] == '#')
                    continue;
                if (line.StartsWith("v "))
                {
                    var p = line.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length < 4)
                        continue;
                    float x = float.Parse(p[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(p[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(p[3], CultureInfo.InvariantCulture);
                    verts.Add(new Vector3(x, y, z));
                }
                else if (line.StartsWith("f "))
                {
                    var p = line.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length < 4)
                        continue;
                    int i0 = ParseFaceIndex(p[1], verts.Count);
                    int i1 = ParseFaceIndex(p[2], verts.Count);
                    for (int t = 3; t < p.Length; t++)
                    {
                        int i2 = ParseFaceIndex(p[t], verts.Count);
                        tris.Add(i0);
                        tris.Add(i1);
                        tris.Add(i2);
                        i1 = i2;
                    }
                }
            }

            if (verts.Count == 0 || tris.Count < 3)
                return null;

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int ParseFaceIndex(string token, int vertCount)
        {
            // v / v/vt / v/vt/vn / v//vn
            int slash = token.IndexOf('/');
            string num = slash >= 0 ? token.Substring(0, slash) : token;
            int idx = int.Parse(num, CultureInfo.InvariantCulture);
            if (idx < 0)
                idx = vertCount + idx; // negative indices
            else
                idx -= 1;
            return idx;
        }
    }
}
