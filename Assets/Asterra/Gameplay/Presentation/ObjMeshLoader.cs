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
            var positions = new List<Vector3>(256);
            var texcoords = new List<Vector2>(256);
            var outPos = new List<Vector3>(256);
            var outUv = new List<Vector2>(256);
            var tris = new List<int>(512);
            bool anyUv = false;

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length < 2 || line[0] == '#')
                    continue;
                if (line.StartsWith("v "))
                {
                    var p = Split(line);
                    if (p.Length < 4)
                        continue;
                    positions.Add(new Vector3(F(p[1]), F(p[2]), F(p[3])));
                }
                else if (line.StartsWith("vt "))
                {
                    var p = Split(line);
                    if (p.Length < 3)
                        continue;
                    texcoords.Add(new Vector2(F(p[1]), F(p[2])));
                    anyUv = true;
                }
                else if (line.StartsWith("f "))
                {
                    var p = Split(line);
                    if (p.Length < 4)
                        continue;
                    int i0 = EmitCorner(p[1], positions, texcoords, outPos, outUv);
                    int i1 = EmitCorner(p[2], positions, texcoords, outPos, outUv);
                    for (int t = 3; t < p.Length; t++)
                    {
                        int i2 = EmitCorner(p[t], positions, texcoords, outPos, outUv);
                        tris.Add(i0);
                        tris.Add(i1);
                        tris.Add(i2);
                        i1 = i2;
                    }
                }
            }

            if (outPos.Count == 0 || tris.Count < 3)
                return null;

            var mesh = new Mesh { name = name, indexFormat = outPos.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(outPos);
            mesh.SetTriangles(tris, 0);
            if (anyUv)
                mesh.SetUVs(0, outUv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int EmitCorner(
            string token,
            List<Vector3> positions,
            List<Vector2> texcoords,
            List<Vector3> outPos,
            List<Vector2> outUv)
        {
            ParseFaceCorner(token, positions.Count, texcoords.Count, out int vi, out int ti);
            outPos.Add(positions[vi]);
            if (ti >= 0 && ti < texcoords.Count)
                outUv.Add(texcoords[ti]);
            else
                outUv.Add(Vector2.zero);
            return outPos.Count - 1;
        }

        private static void ParseFaceCorner(string token, int vertCount, int uvCount, out int vi, out int ti)
        {
            ti = -1;
            int slash = token.IndexOf('/');
            if (slash < 0)
            {
                vi = FaceIndex(token, vertCount);
                return;
            }

            vi = FaceIndex(token.Substring(0, slash), vertCount);
            int slash2 = token.IndexOf('/', slash + 1);
            string uvTok = slash2 >= 0
                ? token.Substring(slash + 1, slash2 - slash - 1)
                : token.Substring(slash + 1);
            if (uvTok.Length > 0)
                ti = FaceIndex(uvTok, uvCount);
        }

        private static int FaceIndex(string num, int count)
        {
            int idx = int.Parse(num, CultureInfo.InvariantCulture);
            if (idx < 0)
                return count + idx;
            return idx - 1;
        }

        private static string[] Split(string line)
        {
            return line.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
        }

        private static float F(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }
    }
}
