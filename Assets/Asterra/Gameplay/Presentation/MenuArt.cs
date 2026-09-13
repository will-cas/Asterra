using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Loads authored menu stills and bakes faction crest meshes to OnGUI textures.</summary>
    public static class MenuArt
    {
        private static Texture2D _hubDiorama;
        private static Texture2D _blackridgePreview;
        private static bool _hubTried;
        private static bool _mapTried;
        private static readonly Dictionary<string, Texture2D> CrestIcons = new();

        public static Texture2D HubDiorama
        {
            get
            {
                if (!_hubTried)
                {
                    _hubTried = true;
                    _hubDiorama = LoadPng("hub_diorama_blackridge.png");
                }
                return _hubDiorama;
            }
        }

        public static Texture2D BlackridgeMapPreview
        {
            get
            {
                if (!_mapTried)
                {
                    _mapTried = true;
                    _blackridgePreview = LoadPng("map_preview_blackridge.png");
                }
                return _blackridgePreview;
            }
        }

        public static Texture2D CrestIcon(string factionDefinitionId, Color accent, bool muted = false)
        {
            string crestKey = AsterraMeshLibrary.CrestKeyForFaction(factionDefinitionId);
            if (string.IsNullOrEmpty(crestKey))
                return null;
            string cacheKey = crestKey + "_64png" + (muted ? "_m" : "_f") + ColorUtility.ToHtmlStringRGB(accent);
            if (CrestIcons.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            // Prefer Art PNG flats when present; mesh bake is fallback.
            Texture2D tex = LoadCrestPng(crestKey);
            if (tex != null)
            {
                if (muted)
                    tex = TintCrestMuted(tex, accent);
            }
            else
            {
                tex = BakeCrest(crestKey, accent, muted);
            }

            CrestIcons[cacheKey] = tex;
            return tex;
        }

        private static Texture2D LoadCrestPng(string crestKey)
        {
            // Art/UI/Menu/crests/crest_<faction>.png — crestKey already includes crest_ prefix.
            return LoadPng(Path.Combine("crests", crestKey + ".png"));
        }

        private static Texture2D TintCrestMuted(Texture2D src, Color accent)
        {
            if (src == null)
                return null;
            int w = src.width;
            int h = src.height;
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = src.name + "_muted",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = src.GetPixels();
            Color gray = new Color(0.45f, 0.45f, 0.48f, 1f);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                if (p.a < 0.01f)
                    continue;
                Color c = Color.Lerp(p, gray, 0.5f);
                c.a = p.a;
                pixels[i] = c;
            }
            dst.SetPixels(pixels);
            dst.Apply(false, false);
            return dst;
        }

        private static Texture2D BakeCrest(string crestKey, Color accent, bool muted)
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
            {
                name = crestKey,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[s * s];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            Mesh mesh = AsterraMeshLibrary.GetCrestMesh(crestKey);
            if (mesh == null || mesh.vertexCount < 3)
            {
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }

            var verts = mesh.vertices;
            var tris = mesh.triangles;
            if (verts == null || tris == null || tris.Length < 3)
            {
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }

            // Crests are authored face-on in XY.
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }
            float span = Mathf.Max(0.001f, Mathf.Max(maxX - minX, maxY - minY));
            float pad = span * 0.12f;
            minX -= pad; maxX += pad; minY -= pad; maxY += pad;
            float w = maxX - minX;
            float h = maxY - minY;

            // Coming tiles: ~50% chroma (UX).
            Color ink = muted
                ? Color.Lerp(Color.Lerp(accent, Color.white, 0.15f), new Color(0.45f, 0.45f, 0.48f), 0.5f)
                : Color.Lerp(accent, Color.white, 0.2f);
            Color edge = muted
                ? Color.Lerp(ink, Color.black, 0.25f)
                : Color.Lerp(accent, Color.white, 0.45f);

            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                Vector3 a = verts[tris[t]];
                Vector3 b = verts[tris[t + 1]];
                Vector3 c = verts[tris[t + 2]];
                // Flip Y so crest reads upright in OnGUI (tex y0 = bottom).
                RasterTri(pixels, s,
                    ToPx(a.x, minX, w, s), (s - 1) - ToPx(a.y, minY, h, s),
                    ToPx(b.x, minX, w, s), (s - 1) - ToPx(b.y, minY, h, s),
                    ToPx(c.x, minX, w, s), (s - 1) - ToPx(c.y, minY, h, s),
                    ink, edge);
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static float ToPx(float v, float min, float span, int size)
        {
            return ((v - min) / span) * (size - 1);
        }

        private static void RasterTri(
            Color[] pixels, int size,
            float x0, float y0, float x1, float y1, float x2, float y2,
            Color fill, Color edge)
        {
            int minXi = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(x0, Mathf.Min(x1, x2))), 0, size - 1);
            int maxXi = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(x0, Mathf.Max(x1, x2))), 0, size - 1);
            int minYi = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(y0, Mathf.Min(y1, y2))), 0, size - 1);
            int maxYi = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(y0, Mathf.Max(y1, y2))), 0, size - 1);
            float area = Edge(x0, y0, x1, y1, x2, y2);
            if (Mathf.Abs(area) < 1e-5f)
                return;
            for (int y = minYi; y <= maxYi; y++)
            for (int x = minXi; x <= maxXi; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float w0 = Edge(x1, y1, x2, y2, px, py);
                float w1 = Edge(x2, y2, x0, y0, px, py);
                float w2 = Edge(x0, y0, x1, y1, px, py);
                if (area < 0f)
                {
                    w0 = -w0; w1 = -w1; w2 = -w2; area = -area;
                }
                if (w0 < 0f || w1 < 0f || w2 < 0f)
                    continue;
                // Edge proximity for a slight rim.
                float minW = Mathf.Min(w0, Mathf.Min(w1, w2)) / area;
                Color c = minW < 0.08f ? edge : fill;
                int idx = y * size + x;
                // Overwrite — crests are opaque icons on dark tiles.
                pixels[idx] = c;
            }
        }

        private static float Edge(float ax, float ay, float bx, float by, float cx, float cy)
        {
            return (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);
        }

        private static Texture2D LoadPng(string fileName)
        {
            string path = Path.Combine(
                Application.dataPath, "Asterra", "Shared", "Art", "UI", "Menu");
            // Allow subfolders like crests/crest_mundor_crown.png
            path = Path.Combine(path, fileName.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                return null;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = Path.GetFileNameWithoutExtension(fileName),
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                if (!tex.LoadImage(bytes))
                {
                    Object.Destroy(tex);
                    return null;
                }
                return tex;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Asterra] Menu art load failed for {fileName}: {ex.Message}");
                return null;
            }
        }
    }
}
