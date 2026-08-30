using System.Collections.Generic;
using Asterra.Core;
using Asterra.Core.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Smooth fantasy heightfield from the sim terrain grid (continuous slopes, not voxel boxes).
    /// </summary>
    public sealed class TerrainGridPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private float yBias;
        [SerializeField] private int maxTreeProps = 160;
        [SerializeField] private int maxDecoProps = 420;
        [SerializeField] private bool hideFlatGround = true;
        [SerializeField] private int heightBlurPasses = 2;
        [SerializeField] private int cellSubdivisions = 2;

        private Transform _root;
        private WorldTerrainGrid _builtForGrid;
        private int _builtCellFingerprint = int.MinValue;
        private Material _terrainMat;
        private Material _waterMat;
        private Material _trunkMat;
        private Material _canopyMat;
        private Material _rockMat;
        private Material _bushMat;
        private Material _reedMat;
        private Material _crystalMat;
        private Texture2D _detailTex;
        private MapTexturePaint[] _texturePaint = System.Array.Empty<MapTexturePaint>();
        private int _texturePaintHash;

        private float[] _heightSamples;
        private int _heightW;
        private int _heightH;
        private float _heightOriginX;
        private float _heightOriginZ;
        private float _heightCellSize = 1f;

        /// <summary>
        /// Units render at <see cref="EntityView.UnitVisualScale"/> (~14 world units tall).
        /// Props were authored for 1-unit silhouettes — multiply into that space.
        /// </summary>
        private const float PropToUnitScale = 5.5f;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
        }

        private void LateUpdate()
        {
            if (match == null || match.World == null || !match.IsMatchRunning)
                return;

            var sim = match.World as global::Asterra.Gameplay.SkirmishWorldSim;
            if (sim == null)
                return;

            var grid = sim.Environment.Grid;
            if (grid == null)
                return;

            int fingerprint = ComputeLayoutFingerprint(grid);
            if (_root != null && _builtForGrid == grid && _builtCellFingerprint == fingerprint)
                return;

            Rebuild(grid, fingerprint);
        }

        private int ComputeLayoutFingerprint(WorldTerrainGrid grid)
        {
            unchecked
            {
                // MutationVersion must dominate: sparse cell samples miss small digs/berms.
                int hash = grid.Width * 73856093 ^ grid.Height * 19349663;
                hash = (hash * 16777619) ^ (int)grid.MutationVersion;
                hash = (hash * 16777619) ^ (int)(grid.MutationVersion >> 32);
                int step = Mathf.Max(1, grid.Width / 12);
                for (int cz = 0; cz < grid.Height; cz += step)
                {
                    for (int cx = 0; cx < grid.Width; cx += step)
                    {
                        if (!grid.TryGetCellAt(cx, cz, out var cell))
                            continue;
                        hash = (hash * 16777619) ^ cell.TerrainDefIndex;
                        if (grid.IsBlockedAt(cx, cz))
                            hash ^= 0xA5A5;
                    }
                }

                hash = (hash * 16777619) ^ _texturePaintHash;
                return hash;
            }
        }

        public void SetTextureStrokes(MapTexturePaint[] strokes)
        {
            _texturePaint = strokes ?? System.Array.Empty<MapTexturePaint>();
            _texturePaintHash = TerrainSplat.HashStrokes(_texturePaint);
            _builtCellFingerprint = int.MinValue;
        }

        private void Rebuild(WorldTerrainGrid grid, int fingerprint)
        {
            _builtForGrid = grid;
            _builtCellFingerprint = fingerprint;

            if (_root != null)
                Destroy(_root.gameObject);

            _root = new GameObject("TerrainPaint").transform;
            _root.SetParent(transform, false);
            var propRoot = new GameObject("TerrainProps").transform;
            propRoot.SetParent(_root, false);

            if (hideFlatGround)
                HideFlatGround();

            EnsureMaterials();

            int w = grid.Width;
            int h = grid.Height;
            var categories = new TerrainCategory[w * h];
            var defIndices = new ushort[w * h];
            var rawHeight = new float[w * h];
            var splat = new Color[w * h];

            for (int cz = 0; cz < h; cz++)
            {
                for (int cx = 0; cx < w; cx++)
                {
                    int i = cz * w + cx;
                    var cat = ResolveCategory(grid, cx, cz);
                    categories[i] = cat;
                    ushort defIndex = 0;
                    if (grid.TryGetCellAt(cx, cz, out var cell))
                        defIndex = cell.TerrainDefIndex;
                    defIndices[i] = defIndex;
                    rawHeight[i] = yBias + HeightFor(cat, defIndex) + MicroRelief(cx, cz, cat);
                    splat[i] = TerrainSplat.WeightsFor(cat, defIndex);
                }
            }

            TerrainSplat.ApplyStrokes(
                splat, w, h, grid.OriginX, grid.OriginZ, grid.CellSize, _texturePaint);

            var smoothHeight = BoxBlur(rawHeight, w, h, heightBlurPasses);
            for (int i = 0; i < smoothHeight.Length; i++)
            {
                var cat = categories[i];
                float target = yBias + HeightFor(cat, defIndices[i]);
                if (IsWater(cat))
                    smoothHeight[i] = Mathf.Min(smoothHeight[i], target + 0.35f);
                else if (cat == TerrainCategory.Mountain)
                    smoothHeight[i] = Mathf.Lerp(smoothHeight[i], Mathf.Max(smoothHeight[i], target), 0.72f);
                else if (cat == TerrainCategory.Hill)
                    smoothHeight[i] = Mathf.Lerp(smoothHeight[i], Mathf.Max(smoothHeight[i], target), 0.45f);
                else if (cat == TerrainCategory.Trench || cat == TerrainCategory.Gap)
                    smoothHeight[i] = Mathf.Min(smoothHeight[i], target + 0.05f);
            }

            BuildContinuousMesh(grid, smoothHeight, splat, categories);
            CacheHeightSamples(grid, smoothHeight);
            BuildWaterSurface(grid, smoothHeight, categories);
            ScatterTrees(grid, smoothHeight, categories, propRoot);
            ScatterDeco(grid, smoothHeight, categories, propRoot);
        }

        private void CacheHeightSamples(WorldTerrainGrid grid, float[] heights)
        {
            _heightW = grid.Width;
            _heightH = grid.Height;
            _heightCellSize = Mathf.Max(0.01f, grid.CellSize);
            _heightOriginX = grid.OriginX;
            _heightOriginZ = grid.OriginZ;
            if (_heightSamples == null || _heightSamples.Length != heights.Length)
                _heightSamples = new float[heights.Length];
            System.Array.Copy(heights, _heightSamples, heights.Length);
        }

        /// <summary>Bilinear sample of the painted terrain height at a world XZ (for unit footing / rings).</summary>
        public float SampleHeight(float worldX, float worldZ)
        {
            if (_heightSamples == null || _heightW < 2 || _heightH < 2)
                return yBias;

            float fx = (worldX - _heightOriginX) / _heightCellSize - 0.5f;
            float fz = (worldZ - _heightOriginZ) / _heightCellSize - 0.5f;
            int x0 = Mathf.FloorToInt(fx);
            int z0 = Mathf.FloorToInt(fz);
            float tx = fx - x0;
            float tz = fz - z0;
            x0 = Mathf.Clamp(x0, 0, _heightW - 1);
            z0 = Mathf.Clamp(z0, 0, _heightH - 1);
            int x1 = Mathf.Min(x0 + 1, _heightW - 1);
            int z1 = Mathf.Min(z0 + 1, _heightH - 1);

            float h00 = _heightSamples[z0 * _heightW + x0];
            float h10 = _heightSamples[z0 * _heightW + x1];
            float h01 = _heightSamples[z1 * _heightW + x0];
            float h11 = _heightSamples[z1 * _heightW + x1];
            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(h0, h1, tz);
        }

        private void BuildContinuousMesh(
            WorldTerrainGrid grid,
            float[] heights,
            Color[] colors,
            TerrainCategory[] categories)
        {
            int w = grid.Width;
            int h = grid.Height;
            int sub = Mathf.Clamp(cellSubdivisions, 1, 3);

            var landColors = colors;

            int cw = w + 1;
            int ch = h + 1;
            var cornerH = new float[cw * ch];
            var cornerC = new Color[cw * ch];
            for (int cz = 0; cz < ch; cz++)
            {
                for (int cx = 0; cx < cw; cx++)
                {
                    float hSum = 0f;
                    Color cSum = Color.clear;
                    int n = 0;
                    AccumulateCorner(heights, landColors, w, h, cx - 1, cz - 1, ref hSum, ref cSum, ref n);
                    AccumulateCorner(heights, landColors, w, h, cx, cz - 1, ref hSum, ref cSum, ref n);
                    AccumulateCorner(heights, landColors, w, h, cx - 1, cz, ref hSum, ref cSum, ref n);
                    AccumulateCorner(heights, landColors, w, h, cx, cz, ref hSum, ref cSum, ref n);
                    int i = cz * cw + cx;
                    cornerH[i] = n > 0 ? hSum / n : 0f;
                    cornerC[i] = n > 0 ? cSum / n : TerrainSplat.WeightsFor(TerrainCategory.GrassShort, 0);
                }
            }

            int vertsX = w * sub + 1;
            int vertsZ = h * sub + 1;
            var verts = new Vector3[vertsX * vertsZ];
            var cols = new Color[vertsX * vertsZ];
            float cell = grid.CellSize;
            float originX = grid.OriginX;
            float originZ = grid.OriginZ;

            for (int vz = 0; vz < vertsZ; vz++)
            {
                for (int vx = 0; vx < vertsX; vx++)
                {
                    float u = vx / (float)sub;
                    float v = vz / (float)sub;
                    float worldX = originX + u * cell;
                    float worldZ = originZ + v * cell;
                    BilinearCorner(cornerH, cornerC, cw, ch, u, v, out float y, out Color c);
                    // Keep water beds flatter so the water sheet sits cleanly.
                    float ripple = Mathf.PerlinNoise(worldX * 0.035f + 3.1f, worldZ * 0.035f) * 0.65f
                                   - Mathf.PerlinNoise(worldX * 0.012f + 9.7f, worldZ * 0.012f) * 0.4f;
                    int cx = Mathf.Clamp(Mathf.FloorToInt(u), 0, w - 1);
                    int cz = Mathf.Clamp(Mathf.FloorToInt(v), 0, h - 1);
                    if (!IsWater(categories[cz * w + cx]))
                        y += ripple;

                    int idx = vz * vertsX + vx;
                    verts[idx] = new Vector3(worldX, y, worldZ);
                    cols[idx] = c;
                }
            }

            int quadsX = vertsX - 1;
            int quadsZ = vertsZ - 1;
            var tris = new int[quadsX * quadsZ * 6];
            int t = 0;
            for (int vz = 0; vz < quadsZ; vz++)
            {
                for (int vx = 0; vx < quadsX; vx++)
                {
                    int i = vz * vertsX + vx;
                    tris[t++] = i;
                    tris[t++] = i + vertsX;
                    tris[t++] = i + vertsX + 1;
                    tris[t++] = i;
                    tris[t++] = i + vertsX + 1;
                    tris[t++] = i + 1;
                }
            }

            var mesh = new Mesh { name = "FantasyTerrain" };
            if (verts.Length > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            var go = new GameObject("TerrainSurface");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = _terrainMat;
            rend.shadowCastingMode = ShadowCastingMode.On;
            rend.receiveShadows = true;
        }

        /// <summary>
        /// Continuous lit water sheet (shared corner verts) — no per-cell grid seams.
        /// </summary>
        private void BuildWaterSurface(WorldTerrainGrid grid, float[] heights, TerrainCategory[] categories)
        {
            int w = grid.Width;
            int h = grid.Height;
            int cw = w + 1;
            int ch = h + 1;
            const float waterLift = 0.85f;

            var cornerY = new float[cw * ch];
            var cornerN = new int[cw * ch];
            int waterCells = 0;

            for (int cz = 0; cz < h; cz++)
            {
                for (int cx = 0; cx < w; cx++)
                {
                    if (!IsWater(categories[cz * w + cx]))
                        continue;
                    waterCells++;
                    float y = heights[cz * w + cx] + waterLift;
                    AccumulateWaterCorner(cornerY, cornerN, cw, cx, cz, y);
                    AccumulateWaterCorner(cornerY, cornerN, cw, cx + 1, cz, y);
                    AccumulateWaterCorner(cornerY, cornerN, cw, cx, cz + 1, y);
                    AccumulateWaterCorner(cornerY, cornerN, cw, cx + 1, cz + 1, y);
                }
            }

            if (waterCells == 0)
                return;

            float cell = grid.CellSize;
            float originX = grid.OriginX;
            float originZ = grid.OriginZ;
            var remap = new int[cw * ch];
            var verts = new List<Vector3>(waterCells * 2);
            for (int i = 0; i < remap.Length; i++)
                remap[i] = -1;

            for (int cz = 0; cz < ch; cz++)
            {
                for (int cx = 0; cx < cw; cx++)
                {
                    int i = cz * cw + cx;
                    if (cornerN[i] <= 0)
                        continue;
                    remap[i] = verts.Count;
                    float y = cornerY[i] / cornerN[i];
                    verts.Add(new Vector3(originX + cx * cell, y, originZ + cz * cell));
                }
            }

            var tris = new List<int>(waterCells * 6);
            for (int cz = 0; cz < h; cz++)
            {
                for (int cx = 0; cx < w; cx++)
                {
                    if (!IsWater(categories[cz * w + cx]))
                        continue;
                    int i = cz * cw + cx;
                    int a = remap[i];
                    int b = remap[i + cw];
                    int c = remap[i + cw + 1];
                    int d = remap[i + 1];
                    if (a < 0 || b < 0 || c < 0 || d < 0)
                        continue;
                    tris.Add(a);
                    tris.Add(b);
                    tris.Add(c);
                    tris.Add(a);
                    tris.Add(c);
                    tris.Add(d);
                }
            }

            if (verts.Count == 0 || tris.Count == 0)
                return;

            var mesh = new Mesh { name = "WaterSurface" };
            if (verts.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("WaterSurface");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = _waterMat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = true;
        }

        private static void AccumulateWaterCorner(
            float[] cornerY,
            int[] cornerN,
            int cw,
            int cx,
            int cz,
            float y)
        {
            int i = cz * cw + cx;
            cornerY[i] += y;
            cornerN[i]++;
        }

        private void ScatterTrees(
            WorldTerrainGrid grid,
            float[] heights,
            TerrainCategory[] categories,
            Transform propRoot)
        {
            int w = grid.Width;
            int h = grid.Height;
            int spawned = 0;

            for (int cz = 0; cz < h; cz++)
            {
                for (int cx = 0; cx < w; cx++)
                {
                    if (spawned >= maxTreeProps)
                        return;

                    var cat = categories[cz * w + cx];
                    if (cat != TerrainCategory.Forest && cat != TerrainCategory.Tree)
                        continue;

                    int hash = cx * 73856093 ^ cz * 19349663;
                    bool dense = cat == TerrainCategory.Tree;
                    if ((hash & (dense ? 1 : 3)) != 0)
                        continue;

                    grid.CellCenter(cx, cz, out float wx, out float wz);
                    wx += (((hash >> 3) & 7) / 7f - 0.5f) * grid.CellSize * 0.55f;
                    wz += (((hash >> 6) & 7) / 7f - 0.5f) * grid.CellSize * 0.55f;
                    if (NearSimTreeDestructible(wx, wz))
                        continue;
                    float groundY = heights[cz * w + cx] + Mathf.PerlinNoise(wx * 0.05f, wz * 0.05f) * 0.3f;
                    SpawnFantasyTree(propRoot, wx, groundY, wz, dense, hash);
                    spawned++;
                }
            }
        }

        private bool NearSimTreeDestructible(float x, float z)
        {
            if (match?.World?.Destructibles == null)
                return false;
            const float r2 = 14f * 14f;
            for (int i = 0; i < match.World.Destructibles.Count; i++)
            {
                var d = match.World.Destructibles[i];
                if (d.State == DestructibleState.Destroyed)
                    continue;
                if (d.DefinitionId == null || !d.DefinitionId.Contains("tree"))
                    continue;
                float dx = d.X - x;
                float dz = d.Z - z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }

        private void SpawnFantasyTree(Transform parent, float x, float groundY, float z, bool dense, int hash)
        {
            float scale = (dense ? 1.15f : 0.85f) * (0.85f + ((hash >> 9) & 7) / 14f) * PropToUnitScale;
            float trunkH = (dense ? 5.2f : 3.8f) * scale;
            float trunkR = (dense ? 0.42f : 0.3f) * scale;

            var root = new GameObject(dense ? "Heartwood" : "GroveTree");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, groundY, z);
            root.transform.rotation = Quaternion.Euler(0f, hash & 359, 0f);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(trunk.GetComponent<Collider>());
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, trunkH * 0.45f, 0f);
            trunk.transform.localScale = new Vector3(trunkR, trunkH * 0.45f, trunkR * 0.92f);
            trunk.GetComponent<Renderer>().sharedMaterial = _trunkMat;
            trunk.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;

            int layers = dense ? 3 : 2;
            for (int i = 0; i < layers; i++)
            {
                var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(canopy.GetComponent<Collider>());
                canopy.transform.SetParent(root.transform, false);
                float y = trunkH * (0.7f + i * 0.18f);
                float ox = (((hash >> (i * 3)) & 3) * 0.35f - 0.35f) * PropToUnitScale * 0.25f;
                float oz = (((hash >> (i * 3 + 2)) & 3) * 0.35f - 0.35f) * PropToUnitScale * 0.25f;
                float s = (dense ? 2.8f : 2.15f) * scale * (1f - i * 0.12f);
                canopy.transform.localPosition = new Vector3(ox, y, oz);
                canopy.transform.localScale = new Vector3(s, s * 0.72f, s);
                canopy.GetComponent<Renderer>().sharedMaterial = _canopyMat;
                canopy.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            }
        }

        private void ScatterDeco(
            WorldTerrainGrid grid,
            float[] heights,
            TerrainCategory[] categories,
            Transform propRoot)
        {
            int w = grid.Width;
            int h = grid.Height;
            int spawned = 0;
            var decoRoot = new GameObject("Deco").transform;
            decoRoot.SetParent(propRoot, false);

            for (int cz = 0; cz < h; cz++)
            {
                for (int cx = 0; cx < w; cx++)
                {
                    if (spawned >= maxDecoProps)
                        return;

                    var cat = categories[cz * w + cx];
                    int hash = cx * 374761393 ^ cz * 668265263;
                    grid.CellCenter(cx, cz, out float wx, out float wz);
                    wx += (((hash >> 2) & 15) / 15f - 0.5f) * grid.CellSize * 0.7f;
                    wz += (((hash >> 6) & 15) / 15f - 0.5f) * grid.CellSize * 0.7f;
                    float groundY = heights[cz * w + cx];

                    switch (cat)
                    {
                        case TerrainCategory.Rock:
                        case TerrainCategory.Mountain:
                            if ((hash & 7) == 0)
                            {
                                SpawnRockCluster(decoRoot, wx, groundY, wz, hash, cat == TerrainCategory.Mountain);
                                spawned++;
                            }
                            if (cat == TerrainCategory.Mountain && (hash & 63) == 0)
                            {
                                SpawnCrystal(decoRoot, wx + 1.2f, groundY, wz - 0.8f, hash);
                                spawned++;
                            }
                            break;

                        case TerrainCategory.Hill:
                        case TerrainCategory.GrassLong:
                        case TerrainCategory.GrassShort:
                            if ((hash & 15) == 0)
                            {
                                SpawnBush(decoRoot, wx, groundY, wz, hash);
                                spawned++;
                            }
                            break;

                        case TerrainCategory.Forest:
                            if ((hash & 11) == 0)
                            {
                                SpawnBush(decoRoot, wx, groundY, wz, hash);
                                spawned++;
                            }
                            break;

                        case TerrainCategory.Beach:
                        case TerrainCategory.Swamp:
                        case TerrainCategory.WaterRiver:
                            if ((hash & 9) == 0 && cat != TerrainCategory.WaterRiver)
                            {
                                SpawnReed(decoRoot, wx, groundY, wz, hash);
                                spawned++;
                            }
                            else if (cat == TerrainCategory.Swamp && (hash & 5) == 0)
                            {
                                SpawnReed(decoRoot, wx, groundY, wz, hash);
                                spawned++;
                            }
                            break;

                        case TerrainCategory.GrassBare:
                            if ((hash & 31) == 0)
                            {
                                SpawnRockCluster(decoRoot, wx, groundY, wz, hash, large: false);
                                spawned++;
                            }
                            break;
                    }
                }
            }
        }

        private void SpawnRockCluster(Transform parent, float x, float groundY, float z, int hash, bool large)
        {
            var root = new GameObject("Rocks");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, groundY, z);
            root.transform.rotation = Quaternion.Euler(0f, hash & 359, 0f);

            int count = large ? 3 : 2;
            for (int i = 0; i < count; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(rock.GetComponent<Collider>());
                rock.transform.SetParent(root.transform, false);
                // ~waist–chest for field rocks; larger mountain boulders vs ~14-tall units.
                float s = (large ? 2.0f : 1.15f) * PropToUnitScale * (0.7f + ((hash >> (i * 4)) & 7) / 12f);
                float ox = (((hash >> (i * 3)) & 5) * 0.45f - 0.9f) * PropToUnitScale * 0.35f;
                float oz = (((hash >> (i * 3 + 1)) & 5) * 0.45f - 0.9f) * PropToUnitScale * 0.35f;
                rock.transform.localPosition = new Vector3(ox, s * 0.28f, oz);
                rock.transform.localRotation = Quaternion.Euler(
                    12f + (hash & 40),
                    30f * i,
                    8f + ((hash >> 4) & 25));
                rock.transform.localScale = new Vector3(s, s * (0.55f + (i % 2) * 0.2f), s * 0.85f);
                rock.GetComponent<Renderer>().sharedMaterial = _rockMat;
                rock.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            }
        }

        private void SpawnBush(Transform parent, float x, float groundY, float z, int hash)
        {
            var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(bush.GetComponent<Collider>());
            bush.name = "Bush";
            bush.transform.SetParent(parent, false);
            // Knee-to-waist shrubs next to infantry.
            float s = (1.05f + ((hash >> 5) & 7) * 0.08f) * PropToUnitScale * 0.85f;
            bush.transform.position = new Vector3(x, groundY + s * 0.28f, z);
            bush.transform.localScale = new Vector3(s, s * 0.62f, s);
            bush.GetComponent<Renderer>().sharedMaterial = _bushMat;
            bush.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private void SpawnReed(Transform parent, float x, float groundY, float z, int hash)
        {
            var root = new GameObject("Reeds");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, groundY, z);

            int stems = 3 + (hash & 2);
            for (int i = 0; i < stems; i++)
            {
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(stem.GetComponent<Collider>());
                stem.transform.SetParent(root.transform, false);
                float h = (1.35f + ((hash >> (i + 2)) & 3) * 0.28f) * PropToUnitScale;
                float ox = (((i * 3 + (hash & 3)) % 5) * 0.22f - 0.4f) * PropToUnitScale * 0.3f;
                float oz = (((i * 5 + (hash >> 2)) % 5) * 0.22f - 0.4f) * PropToUnitScale * 0.3f;
                stem.transform.localPosition = new Vector3(ox, h * 0.5f, oz);
                stem.transform.localRotation = Quaternion.Euler(
                    ((hash >> i) & 7) - 3,
                    i * 40f,
                    ((hash >> (i + 3)) & 7) - 3);
                stem.transform.localScale = new Vector3(0.08f * PropToUnitScale * 0.45f, h * 0.5f, 0.08f * PropToUnitScale * 0.45f);
                stem.GetComponent<Renderer>().sharedMaterial = _reedMat;
                stem.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private void SpawnCrystal(Transform parent, float x, float groundY, float z, int hash)
        {
            var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(crystal.GetComponent<Collider>());
            crystal.name = "AetherCrystal";
            crystal.transform.SetParent(parent, false);
            float h = (1.8f + ((hash >> 8) & 3) * 0.35f) * PropToUnitScale;
            float w = 0.45f * PropToUnitScale;
            crystal.transform.position = new Vector3(x, groundY + h * 0.45f, z);
            crystal.transform.rotation = Quaternion.Euler(-18f, hash & 359, 12f);
            crystal.transform.localScale = new Vector3(w, h, w);
            crystal.GetComponent<Renderer>().sharedMaterial = _crystalMat;
            crystal.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private void EnsureMaterials()
        {
            if (_detailTex == null)
                _detailTex = BuildDetailTexture(128);

            if (_terrainMat == null)
            {
                var shader = Shader.Find("Asterra/TerrainSplat")
                             ?? Shader.Find("Asterra/TerrainLit")
                             ?? Shader.Find("Asterra/VertexColorLit")
                             ?? Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard");
                _terrainMat = new Material(shader);
                if (_terrainMat.HasProperty("_Color"))
                    _terrainMat.SetColor("_Color", Color.white);
                if (_terrainMat.HasProperty("_BaseColor"))
                    _terrainMat.SetColor("_BaseColor", Color.white);
                if (_terrainMat.HasProperty("_AmbientFloor"))
                    _terrainMat.SetFloat("_AmbientFloor", 0.32f);
                if (_terrainMat.HasProperty("_UvScale"))
                    _terrainMat.SetFloat("_UvScale", 0.085f);
                if (_terrainMat.HasProperty("_GrassTex"))
                    _terrainMat.SetTexture("_GrassTex", AsterraMeshLibrary.GetTerrainAlbedo("grass"));
                if (_terrainMat.HasProperty("_DirtTex"))
                    _terrainMat.SetTexture("_DirtTex", AsterraMeshLibrary.GetTerrainAlbedo("dirt"));
                if (_terrainMat.HasProperty("_RockTex"))
                    _terrainMat.SetTexture("_RockTex", AsterraMeshLibrary.GetTerrainAlbedo("rock"));
                if (_terrainMat.HasProperty("_SandTex"))
                    _terrainMat.SetTexture("_SandTex", AsterraMeshLibrary.GetTerrainAlbedo("sand"));
                if (_terrainMat.HasProperty("_Gloss"))
                    _terrainMat.SetFloat("_Gloss", 0.42f);
                if (_terrainMat.HasProperty("_DetailTex"))
                    _terrainMat.SetTexture("_DetailTex", _detailTex);
                if (_terrainMat.HasProperty("_DetailScale"))
                    _terrainMat.SetFloat("_DetailScale", 0.07f);
                if (_terrainMat.HasProperty("_DetailStrength"))
                    _terrainMat.SetFloat("_DetailStrength", 0.5f);
                if (_terrainMat.HasProperty("_MacroScale"))
                    _terrainMat.SetFloat("_MacroScale", 0.011f);
                if (_terrainMat.HasProperty("_MacroStrength"))
                    _terrainMat.SetFloat("_MacroStrength", 0.25f);
            }

            if (_trunkMat == null)
                _trunkMat = CreateSimpleLit(new Color(0.55f, 0.38f, 0.2f), 0.12f, AsterraMeshLibrary.GetPropAlbedo("bark"));
            if (_waterMat == null)
            {
                _waterMat = CreateSimpleLit(new Color(0.28f, 0.62f, 0.92f), 0.95f, null);
                if (_waterMat.HasProperty("_Metallic"))
                    _waterMat.SetFloat("_Metallic", 0.05f);
            }
            if (_canopyMat == null)
                _canopyMat = CreateSimpleLit(new Color(0.45f, 0.75f, 0.32f), 0.15f, AsterraMeshLibrary.GetPropAlbedo("leaf"));
            if (_rockMat == null)
                _rockMat = CreateSimpleLit(new Color(0.72f, 0.7f, 0.66f), 0.2f, AsterraMeshLibrary.GetPropAlbedo("rock"));
            if (_bushMat == null)
                _bushMat = CreateSimpleLit(new Color(0.4f, 0.62f, 0.28f), 0.12f, AsterraMeshLibrary.GetPropAlbedo("bush"));
            if (_reedMat == null)
                _reedMat = CreateSimpleLit(new Color(0.45f, 0.62f, 0.28f), 0.1f, AsterraMeshLibrary.GetPropAlbedo("leaf"));
            if (_crystalMat == null)
            {
                _crystalMat = CreateSimpleLit(new Color(0.45f, 0.75f, 1f), 0.85f, AsterraMeshLibrary.GetPropAlbedo("gold"));
                if (_crystalMat.HasProperty("_EmissionColor"))
                {
                    _crystalMat.EnableKeyword("_EMISSION");
                    _crystalMat.SetColor("_EmissionColor", new Color(0.25f, 0.55f, 0.9f) * 1.4f);
                }
            }
        }

        private static Texture2D BuildDetailTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGB24, mipChain: true, linear: true);
            tex.name = "TerrainDetailNoise";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float n =
                        Mathf.PerlinNoise(u * 6.1f, v * 6.1f) * 0.55f
                        + Mathf.PerlinNoise(u * 17.3f + 2.2f, v * 17.3f) * 0.3f
                        + Mathf.PerlinNoise(u * 41f + 8f, v * 41f) * 0.15f;
                    // Subtle warm/cool flecks for fantasy soil variation.
                    float fleck = Mathf.PerlinNoise(u * 29f + 4f, v * 29f);
                    Color c = new Color(
                        Mathf.Clamp01(n * 0.95f + fleck * 0.08f),
                        Mathf.Clamp01(n),
                        Mathf.Clamp01(n * 0.92f + (1f - fleck) * 0.06f));
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            return tex;
        }

        private static Material CreateSimpleLit(Color color, float smoothness, Texture2D albedo)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (albedo != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", albedo);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", albedo);
            }

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", smoothness > 0.6f ? 0.15f : 0.02f);
            return mat;
        }

        private static void AccumulateCorner(
            float[] heights,
            Color[] colors,
            int w,
            int h,
            int cx,
            int cz,
            ref float hSum,
            ref Color cSum,
            ref int n)
        {
            if (cx < 0 || cz < 0 || cx >= w || cz >= h)
                return;
            int i = cz * w + cx;
            hSum += heights[i];
            cSum += colors[i];
            n++;
        }

        private static void BilinearCorner(
            float[] cornerH,
            Color[] cornerC,
            int cw,
            int ch,
            float u,
            float v,
            out float y,
            out Color c)
        {
            u = Mathf.Clamp(u, 0f, cw - 1.001f);
            v = Mathf.Clamp(v, 0f, ch - 1.001f);
            int x0 = Mathf.FloorToInt(u);
            int z0 = Mathf.FloorToInt(v);
            int x1 = Mathf.Min(x0 + 1, cw - 1);
            int z1 = Mathf.Min(z0 + 1, ch - 1);
            float tx = u - x0;
            float tz = v - z0;

            float h00 = cornerH[z0 * cw + x0];
            float h10 = cornerH[z0 * cw + x1];
            float h01 = cornerH[z1 * cw + x0];
            float h11 = cornerH[z1 * cw + x1];
            y = Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);

            Color c00 = cornerC[z0 * cw + x0];
            Color c10 = cornerC[z0 * cw + x1];
            Color c01 = cornerC[z1 * cw + x0];
            Color c11 = cornerC[z1 * cw + x1];
            c = Color.Lerp(Color.Lerp(c00, c10, tx), Color.Lerp(c01, c11, tx), tz);
        }

        private static float[] BoxBlur(float[] src, int w, int h, int passes)
        {
            var a = (float[])src.Clone();
            var b = new float[src.Length];
            for (int p = 0; p < passes; p++)
            {
                for (int z = 0; z < h; z++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float sum = 0f;
                        int count = 0;
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int zz = z + dz;
                            if (zz < 0 || zz >= h)
                                continue;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int xx = x + dx;
                                if (xx < 0 || xx >= w)
                                    continue;
                                sum += a[zz * w + xx];
                                count++;
                            }
                        }

                        b[z * w + x] = sum / count;
                    }
                }

                var tmp = a;
                a = b;
                b = tmp;
            }

            return a;
        }

        private static TerrainCategory ResolveCategory(WorldTerrainGrid grid, int cx, int cz)
        {
            if (!grid.TryGetCellAt(cx, cz, out var cell))
                return TerrainCategory.NoEntry;

            var def = grid.GetDef(cell.TerrainDefIndex);
            var category = def.Category;
            if (grid.IsBlockedAt(cx, cz)
                && category != TerrainCategory.Mountain
                && category != TerrainCategory.Tree
                && category != TerrainCategory.NoEntry)
                category = TerrainCategory.NoEntry;

            if (cell.Ice == IceState.Thin || cell.Ice == IceState.Thick || cell.Ice == IceState.FrozenWater)
                category = TerrainCategory.Ice;

            return category;
        }

        private static bool IsWater(TerrainCategory category) =>
            category == TerrainCategory.WaterRiver
            || category == TerrainCategory.WaterLake
            || category == TerrainCategory.WaterOcean
            || category == TerrainCategory.WaterWaterfall
            || category == TerrainCategory.Ice;

        private static float MicroRelief(int cx, int cz, TerrainCategory category)
        {
            if (IsWater(category) || category == TerrainCategory.Mountain)
                return 0f;
            return (((cx * 17 + cz * 29) & 7) - 3) * 0.08f;
        }

        private static void HideFlatGround()
        {
            var ground = GameObject.Find("AsterraGround");
            if (ground == null)
                return;
            var rend = ground.GetComponent<Renderer>();
            if (rend != null)
                rend.enabled = false;
        }

        public void ClearPaint()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
                _root = null;
            }

            _builtForGrid = null;
            _builtCellFingerprint = int.MinValue;
            _texturePaint = System.Array.Empty<MapTexturePaint>();
            _texturePaintHash = 0;
        }

        private static float HeightFor(TerrainCategory category, ushort defIndex = 0)
        {
            if (defIndex == DefaultTerrainCatalog.WaterShallow)
                return -0.18f;
            if (defIndex == DefaultTerrainCatalog.WaterDeep)
                return -0.55f;
            if (defIndex == DefaultTerrainCatalog.WaterFast)
                return -0.4f;
            if (defIndex == DefaultTerrainCatalog.Road)
                return 0.04f;
            if (defIndex == DefaultTerrainCatalog.Mud)
                return -0.14f;
            if (defIndex == DefaultTerrainCatalog.Rubble)
                return 0.4f;
            if (defIndex == DefaultTerrainCatalog.Snow)
                return 0.12f;

            switch (category)
            {
                case TerrainCategory.WaterOcean: return -0.55f;
                case TerrainCategory.WaterLake: return -0.45f;
                case TerrainCategory.WaterRiver: return -0.35f;
                case TerrainCategory.WaterWaterfall: return -0.2f;
                case TerrainCategory.Beach: return -0.15f;
                case TerrainCategory.Swamp: return -0.35f;
                case TerrainCategory.Trench: return -2.6f;
                case TerrainCategory.Gap: return -3.5f;
                case TerrainCategory.Ice: return -0.55f;
                case TerrainCategory.Hill: return 7.5f;
                case TerrainCategory.Mountain: return 24f;
                case TerrainCategory.Rock: return 2.8f;
                case TerrainCategory.Forest: return 0.55f;
                case TerrainCategory.Tree: return 0.7f;
                case TerrainCategory.GrassLong: return 0.35f;
                case TerrainCategory.GrassBare: return 0.08f;
                case TerrainCategory.NoEntry: return 0.6f;
                default: return 0f;
            }
        }

        private static Color ColorFor(TerrainCategory category, ushort defIndex = 0)
        {
            if (defIndex == DefaultTerrainCatalog.WaterShallow)
                return new Color(0.45f, 0.72f, 0.82f);
            if (defIndex == DefaultTerrainCatalog.WaterDeep)
                return new Color(0.14f, 0.32f, 0.58f);
            if (defIndex == DefaultTerrainCatalog.WaterFast)
                return new Color(0.28f, 0.62f, 0.88f);

            switch (category)
            {
                case TerrainCategory.GrassBare: return new Color(0.5f, 0.54f, 0.32f);
                case TerrainCategory.GrassShort: return new Color(0.36f, 0.5f, 0.26f);
                case TerrainCategory.GrassLong: return new Color(0.28f, 0.44f, 0.22f);
                case TerrainCategory.Rock: return new Color(0.55f, 0.52f, 0.48f);
                case TerrainCategory.Swamp: return new Color(0.3f, 0.38f, 0.26f);
                case TerrainCategory.Forest: return new Color(0.2f, 0.36f, 0.18f);
                case TerrainCategory.Tree: return new Color(0.17f, 0.32f, 0.15f);
                case TerrainCategory.Beach: return new Color(0.8f, 0.72f, 0.5f);
                case TerrainCategory.Mountain: return new Color(0.5f, 0.48f, 0.46f);
                case TerrainCategory.Hill: return new Color(0.42f, 0.52f, 0.3f);
                case TerrainCategory.WaterRiver: return new Color(0.32f, 0.58f, 0.78f);
                case TerrainCategory.WaterLake: return new Color(0.24f, 0.5f, 0.72f);
                case TerrainCategory.WaterOcean: return new Color(0.18f, 0.4f, 0.65f);
                case TerrainCategory.WaterWaterfall: return new Color(0.48f, 0.7f, 0.88f);
                case TerrainCategory.Ice: return new Color(0.86f, 0.93f, 0.98f);
                case TerrainCategory.Trench: return new Color(0.55f, 0.44f, 0.3f);
                case TerrainCategory.Gap: return new Color(0.32f, 0.3f, 0.28f);
                case TerrainCategory.NoEntry: return new Color(0.22f, 0.2f, 0.2f);
                default: return new Color(0.36f, 0.46f, 0.28f);
            }
        }
    }
}
