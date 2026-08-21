using System;
using System.Collections.Generic;
using System.IO;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using UnityEditor;
using UnityEngine;

namespace Asterra.EditorTools
{
    /// <summary>
    /// Designer map painter. Saves to Assets/Asterra/Shared/Maps/*.map.json
    /// (mirrored to StreamingAssets). Custom maps appear in the offline match menu.
    /// </summary>
    public sealed class MapCreatorWindow : EditorWindow
    {
        private enum Tool
        {
            Terrain,
            Blocked,
            KeepWest,
            KeepEast,
            Gold,
            Timber,
            Territory,
            Tree,
            Rock,
            Bridge,
            Traversal,
            UnitWest,
            UnitEast,
            EraseOverlay,
        }

        private MapDefinition _map = NewBlank();
        private Tool _tool = Tool.Terrain;
        private ushort _brushTerrain = DefaultTerrainCatalog.GrassShort;
        private float _brushRadius = 20f;
        private Vector2 _scroll;
        private string _status = "Paint terrain, place keeps (west/east), save, then pick the map in Offline Skirmish.";
        private Texture2D _preview;
        private bool _previewDirty = true;
        private const float Half = 450f;
        private const float Cell = 10f;
        private const int Res = 90;
        private bool _linkHasStart;
        private float _linkStartX;
        private float _linkStartZ;
        private string _linkType = "bridge";
        private int _attachLinkIndex = -1;

        [MenuItem("Asterra/Map Creator")]
        public static void Open()
        {
            var w = GetWindow<MapCreatorWindow>("Asterra Map Creator");
            w.minSize = new Vector2(920, 640);
            w.Show();
        }

        private void OnDisable()
        {
            if (_preview != null)
                DestroyImmediate(_preview);
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            DrawCanvas();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                if (EditorUtility.DisplayDialog("New Map", "Discard current map?", "New", "Cancel"))
                {
                    _map = NewBlank();
                    _previewDirty = true;
                    _status = "Blank map ready.";
                }
            }

            if (GUILayout.Button("Load…", EditorStyles.toolbarButton, GUILayout.Width(56)))
                LoadDialog();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(48)))
                SaveCurrent();
            if (GUILayout.Button("Save As…", EditorStyles.toolbarButton, GUILayout.Width(64)))
                SaveAsDialog();
            if (GUILayout.Button("Template: Twin Keeps terrain", EditorStyles.toolbarButton))
            {
                ApplyTwinKeepsTerrainTemplate();
                _status = "Loaded Twin Keeps terrain strokes as a starting template.";
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Id", GUILayout.Width(18));
            _map.id = EditorGUILayout.TextField(_map.id, GUILayout.Width(140));
            EditorGUILayout.LabelField("Name", GUILayout.Width(36));
            _map.displayName = EditorGUILayout.TextField(_map.displayName, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(220));
            EditorGUILayout.LabelField("Tool", EditorStyles.boldLabel);
            _tool = (Tool)GUILayout.SelectionGrid(
                (int)_tool,
                new[]
                {
                    "Terrain", "Blocked", "Keep W", "Keep E", "Gold", "Timber",
                    "Territory", "Tree", "Rock", "Bridge", "Traversal", "Unit W",
                    "Unit E", "Erase",
                },
                2);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Terrain brush", EditorStyles.boldLabel);
            _brushTerrain = DrawTerrainPicker(_brushTerrain);
            _brushRadius = EditorGUILayout.Slider("Radius", _brushRadius, 8f, 80f);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Traversal links", EditorStyles.boldLabel);
            _linkType = EditorGUILayout.TextField("New link type", _linkType);
            EditorGUILayout.LabelField(
                "Traversal tool: click start, then end. Bridge tool places a bridge prop.",
                EditorStyles.wordWrappedMiniLabel);
            _attachLinkIndex = EditorGUILayout.IntField("Attach link # on place", _attachLinkIndex);
            EditorGUILayout.LabelField(
                "Tree/Rock/Bridge: set Attach link # (≥0) to wire linkedTraversalLinkId.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(4);
            _map.EnsureArrays();
            for (int i = 0; i < _map.traversalLinks.Length; i++)
            {
                var link = _map.traversalLinks[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Link {i}", EditorStyles.boldLabel);
                link.type = EditorGUILayout.TextField("Type", link.type ?? "bridge");
                link.startX = EditorGUILayout.FloatField("Start X", link.startX);
                link.startZ = EditorGUILayout.FloatField("Start Z", link.startZ);
                link.endX = EditorGUILayout.FloatField("End X", link.endX);
                link.endZ = EditorGUILayout.FloatField("End Z", link.endZ);
                link.durationSeconds = EditorGUILayout.FloatField("Duration", link.durationSeconds);
                link.approachRadius = EditorGUILayout.FloatField("Approach", link.approachRadius);
                link.enabled = EditorGUILayout.Toggle("Enabled", link.enabled);
                if (GUILayout.Button("Remove link"))
                {
                    RemoveLinkAt(i);
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Camera focus", EditorStyles.boldLabel);
            _map.cameraFocusX = EditorGUILayout.FloatField("X", _map.cameraFocusX);
            _map.cameraFocusZ = EditorGUILayout.FloatField("Z", _map.cameraFocusZ);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Counts", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Terrain strokes: {_map.terrain?.Length ?? 0}");
            EditorGUILayout.LabelField($"Keeps: {_map.keeps?.Length ?? 0}");
            EditorGUILayout.LabelField($"Units: {_map.units?.Length ?? 0}");
            EditorGUILayout.LabelField($"Resources: {_map.resources?.Length ?? 0}");
            EditorGUILayout.LabelField($"Territories: {_map.territories?.Length ?? 0}");
            EditorGUILayout.LabelField($"Destructibles: {_map.destructibles?.Length ?? 0}");
            EditorGUILayout.LabelField($"Traversal links: {_map.traversalLinks?.Length ?? 0}");

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Clear overlays (keeps/units/…)"))
            {
                _map.keeps = Array.Empty<MapKeepSpawn>();
                _map.units = Array.Empty<MapUnitSpawn>();
                _map.buildings = Array.Empty<MapBuildingSpawn>();
                _map.resources = Array.Empty<MapResourceNode>();
                _map.territories = Array.Empty<MapTerritory>();
                _map.destructibles = Array.Empty<MapDestructible>();
                _map.blocked = Array.Empty<MapBlockedRect>();
                _map.traversalLinks = Array.Empty<MapTraversalLink>();
                _linkHasStart = false;
                _previewDirty = true;
            }

            if (GUILayout.Button("Clear terrain strokes"))
            {
                _map.terrain = Array.Empty<MapTerrainPaint>();
                _previewDirty = true;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCanvas()
        {
            EditorGUILayout.BeginVertical();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            float size = Mathf.Min(position.width - 260f, position.height - 120f);
            size = Mathf.Max(420f, size);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));

            if (_previewDirty || _preview == null)
                RebuildPreview();

            if (_preview != null)
                GUI.DrawTexture(rect, _preview, ScaleMode.StretchToFill);

            Handles.BeginGUI();
            DrawOverlayMarkers(rect);
            Handles.EndGUI();

            HandleCanvasInput(rect);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField(
                "Click map to place. Traversal: start→end. Yellow lines = links. Bridge tool = prop. Attach link # wires destroy→disable link.",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void HandleCanvasInput(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                WorldFromGui(rect, e.mousePosition, out float wx, out float wz);
                ApplyClick(wx, wz, e.shift);
                e.Use();
                Repaint();
            }
        }

        private void ApplyClick(float wx, float wz, bool shift)
        {
            _map.EnsureArrays();
            switch (_tool)
            {
                case Tool.Terrain:
                    AddTerrainDisk(wx, wz, _brushRadius, _brushTerrain);
                    _status = $"Terrain {_brushTerrain} @ ({wx:0},{wz:0})";
                    break;
                case Tool.Blocked:
                    float r = _brushRadius;
                    Append(ref _map.blocked, new MapBlockedRect
                    {
                        minX = wx - r, minZ = wz - r, maxX = wx + r, maxZ = wz + r, blocked = true,
                    });
                    _status = $"Blocked @ ({wx:0},{wz:0})";
                    break;
                case Tool.KeepWest:
                    UpsertKeep(0, wx, wz);
                    _map.cameraFocusX = wx;
                    _map.cameraFocusZ = wz;
                    _status = $"West keep @ ({wx:0},{wz:0})";
                    break;
                case Tool.KeepEast:
                    UpsertKeep(1, wx, wz);
                    _status = $"East keep @ ({wx:0},{wz:0})";
                    break;
                case Tool.Gold:
                    Append(ref _map.resources, new MapResourceNode
                    {
                        type = "gold", amount = shift ? 2500 : 2000, x = wx, z = wz,
                    });
                    EnsureLandDisk(wx, wz, 12f);
                    _status = $"Gold @ ({wx:0},{wz:0})";
                    break;
                case Tool.Timber:
                    Append(ref _map.resources, new MapResourceNode
                    {
                        type = "timber", amount = shift ? 2000 : 1600, x = wx, z = wz,
                    });
                    EnsureLandDisk(wx, wz, 12f);
                    _status = $"Timber @ ({wx:0},{wz:0})";
                    break;
                case Tool.Territory:
                    Append(ref _map.territories, new MapTerritory
                    {
                        x = wx, z = wz, radius = 36f, goldPerSecond = 8,
                    });
                    _status = $"Territory @ ({wx:0},{wz:0})";
                    break;
                case Tool.Tree:
                    PlaceDestructible("tree", wx, wz);
                    break;
                case Tool.Rock:
                    PlaceDestructible("rock", wx, wz);
                    break;
                case Tool.Bridge:
                    PlaceDestructible("bridge", wx, wz);
                    break;
                case Tool.Traversal:
                    if (!_linkHasStart)
                    {
                        _linkHasStart = true;
                        _linkStartX = wx;
                        _linkStartZ = wz;
                        _status = $"Traversal start @ ({wx:0},{wz:0}) — click end point";
                    }
                    else
                    {
                        Append(ref _map.traversalLinks, new MapTraversalLink
                        {
                            startX = _linkStartX,
                            startZ = _linkStartZ,
                            endX = wx,
                            endZ = wz,
                            type = string.IsNullOrEmpty(_linkType) ? "bridge" : _linkType,
                            durationSeconds = 1.25f,
                            approachRadius = 8f,
                            enabled = true,
                        });
                        int idx = _map.traversalLinks.Length - 1;
                        _linkHasStart = false;
                        _status = $"Traversal link #{idx} {_linkStartX:0},{_linkStartZ:0} → {wx:0},{wz:0}";
                    }

                    break;
                case Tool.UnitWest:
                    Append(ref _map.units, new MapUnitSpawn
                    {
                        seatIndex = 0, role = shift ? "builder" : "basic", x = wx, z = wz,
                    });
                    _status = $"West unit @ ({wx:0},{wz:0})";
                    break;
                case Tool.UnitEast:
                    Append(ref _map.units, new MapUnitSpawn
                    {
                        seatIndex = 1, role = shift ? "builder" : "basic", x = wx, z = wz,
                    });
                    _status = $"East unit @ ({wx:0},{wz:0})";
                    break;
                case Tool.EraseOverlay:
                    EraseNear(wx, wz, 18f);
                    _status = $"Erased overlays near ({wx:0},{wz:0})";
                    break;
            }

            _previewDirty = true;
        }

        private void UpsertKeep(int seat, float x, float z)
        {
            var list = new List<MapKeepSpawn>(_map.keeps ?? Array.Empty<MapKeepSpawn>());
            list.RemoveAll(k => k.seatIndex == seat);
            list.Add(new MapKeepSpawn { seatIndex = seat, x = x, z = z });
            _map.keeps = list.ToArray();
            EnsureLandDisk(x, z, 40f);
        }

        private void AddTerrainDisk(float x, float z, float radius, ushort terrain)
        {
            Append(ref _map.terrain, new MapTerrainPaint
            {
                shape = "disk",
                x = x,
                z = z,
                radius = radius,
                terrainIndex = terrain,
            });
        }

        private void EnsureLandDisk(float x, float z, float radius)
        {
            AddTerrainDisk(x, z, radius, DefaultTerrainCatalog.GrassBare);
        }

        private void EraseNear(float x, float z, float radius)
        {
            float r2 = radius * radius;
            bool Near(float ax, float az) => (ax - x) * (ax - x) + (az - z) * (az - z) <= r2;

            _map.keeps = Filter(_map.keeps, k => !Near(k.x, k.z));
            _map.units = Filter(_map.units, u => !Near(u.x, u.z));
            _map.resources = Filter(_map.resources, r => !Near(r.x, r.z));
            _map.territories = Filter(_map.territories, t => !Near(t.x, t.z));
            _map.destructibles = Filter(_map.destructibles, d => !Near(d.x, d.z));
            if (_map.traversalLinks != null && _map.traversalLinks.Length > 0)
            {
                var kept = new List<MapTraversalLink>();
                for (int i = 0; i < _map.traversalLinks.Length; i++)
                {
                    var link = _map.traversalLinks[i];
                    if (Near(link.startX, link.startZ) || Near(link.endX, link.endZ))
                        continue;
                    kept.Add(link);
                }

                _map.traversalLinks = kept.ToArray();
            }
        }

        private void PlaceDestructible(string catalogId, float wx, float wz)
        {
            Append(ref _map.destructibles, new MapDestructible
            {
                catalogId = catalogId,
                x = wx,
                z = wz,
                linkedTraversalLinkId = _attachLinkIndex,
            });
            string linkNote = _attachLinkIndex >= 0 ? $" link#{_attachLinkIndex}" : string.Empty;
            _status = $"{catalogId} @ ({wx:0},{wz:0}){linkNote}";
        }

        private void RemoveLinkAt(int index)
        {
            _map.EnsureArrays();
            if (index < 0 || index >= _map.traversalLinks.Length)
                return;
            var list = new List<MapTraversalLink>(_map.traversalLinks);
            list.RemoveAt(index);
            _map.traversalLinks = list.ToArray();
            // Remap destructible link ids after removal.
            if (_map.destructibles != null)
            {
                for (int i = 0; i < _map.destructibles.Length; i++)
                {
                    var d = _map.destructibles[i];
                    if (d.linkedTraversalLinkId == index)
                        d.linkedTraversalLinkId = -1;
                    else if (d.linkedTraversalLinkId > index)
                        d.linkedTraversalLinkId--;
                }
            }

            _status = $"Removed traversal link #{index}";
            Repaint();
        }

        private void DrawOverlayMarkers(Rect rect)
        {
            void Dot(float wx, float wz, Color c, float px = 8f)
            {
                GuiFromWorld(rect, wx, wz, out float gx, out float gy);
                EditorGUI.DrawRect(new Rect(gx - px * 0.5f, gy - px * 0.5f, px, px), c);
            }

            if (_map.keeps != null)
            {
                foreach (var k in _map.keeps)
                    Dot(k.x, k.z, k.seatIndex == 0 ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.95f, 0.25f, 0.2f), 14f);
            }

            if (_map.resources != null)
            {
                foreach (var r in _map.resources)
                {
                    bool gold = string.Equals(r.type, "gold", StringComparison.OrdinalIgnoreCase);
                    Dot(r.x, r.z, gold ? Color.yellow : new Color(0.55f, 0.35f, 0.15f), 7f);
                }
            }

            if (_map.territories != null)
            {
                foreach (var t in _map.territories)
                    Dot(t.x, t.z, new Color(0.4f, 0.7f, 1f, 0.9f), 10f);
            }

            if (_map.units != null)
            {
                foreach (var u in _map.units)
                    Dot(u.x, u.z, u.seatIndex == 0 ? Color.cyan : new Color(1f, 0.5f, 0.8f), 5f);
            }

            if (_map.destructibles != null)
            {
                foreach (var d in _map.destructibles)
                {
                    Color c = Color.gray;
                    if (string.Equals(d.catalogId, "bridge", StringComparison.OrdinalIgnoreCase))
                        c = new Color(0.75f, 0.55f, 0.25f);
                    else if (string.Equals(d.catalogId, "tree", StringComparison.OrdinalIgnoreCase))
                        c = new Color(0.25f, 0.55f, 0.28f);
                    Dot(d.x, d.z, c, 6f);
                }
            }

            if (_map.traversalLinks != null)
            {
                Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                for (int i = 0; i < _map.traversalLinks.Length; i++)
                {
                    var link = _map.traversalLinks[i];
                    GuiFromWorld(rect, link.startX, link.startZ, out float ax, out float ay);
                    GuiFromWorld(rect, link.endX, link.endZ, out float bx, out float by);
                    Handles.DrawLine(new Vector3(ax, ay, 0f), new Vector3(bx, by, 0f));
                    Dot(link.startX, link.startZ, Color.yellow, 5f);
                    Dot(link.endX, link.endZ, Color.yellow, 5f);
                }
            }

            if (_linkHasStart)
                Dot(_linkStartX, _linkStartZ, new Color(1f, 0.4f, 0.1f), 9f);
        }

        private void RebuildPreview()
        {
            _map.EnsureArrays();
            if (_preview == null || _preview.width != Res)
            {
                if (_preview != null)
                    DestroyImmediate(_preview);
                _preview = new Texture2D(Res, Res, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }

            var cells = new ushort[Res * Res];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = _map.defaultTerrain;

            for (int i = 0; i < _map.terrain.Length; i++)
                StampPaint(cells, _map.terrain[i]);

            var pixels = new Color32[Res * Res];
            for (int i = 0; i < cells.Length; i++)
                pixels[i] = TerrainColor(cells[i]);

            // Blocked overlay
            for (int i = 0; i < _map.blocked.Length; i++)
            {
                var b = _map.blocked[i];
                ForCellsInRect(b.minX, b.minZ, b.maxX, b.maxZ, (cx, cz) =>
                {
                    int idx = cz * Res + cx;
                    var c = pixels[idx];
                    pixels[idx] = new Color32(
                        (byte)(c.r * 0.4f), (byte)(c.g * 0.4f), (byte)(c.b * 0.4f), 255);
                });
            }

            _preview.SetPixels32(pixels);
            _preview.Apply(false);
            _previewDirty = false;
        }

        private static void StampPaint(ushort[] cells, MapTerrainPaint paint)
        {
            if (paint == null)
                return;
            string shape = string.IsNullOrEmpty(paint.shape) ? "rect" : paint.shape.ToLowerInvariant();
            if (shape == "disk")
            {
                float cx = paint.x;
                float cz = paint.z;
                float r = paint.radius > 0.5f ? paint.radius : 10f;
                StampRect(cells, cx - r, cz - r, cx + r, cz + r, paint.terrainIndex);
                return;
            }

            StampRect(cells, paint.minX, paint.minZ, paint.maxX, paint.maxZ, paint.terrainIndex);
        }

        private static void StampRect(ushort[] cells, float minX, float minZ, float maxX, float maxZ, ushort def)
        {
            ForCellsInRect(minX, minZ, maxX, maxZ, (cx, cz) => { cells[cz * Res + cx] = def; });
        }

        private static void ForCellsInRect(float minX, float minZ, float maxX, float maxZ, Action<int, int> fn)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt((minX + Half) / Cell), 0, Res - 1);
            int x1 = Mathf.Clamp(Mathf.FloorToInt((maxX + Half) / Cell), 0, Res - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((minZ + Half) / Cell), 0, Res - 1);
            int z1 = Mathf.Clamp(Mathf.FloorToInt((maxZ + Half) / Cell), 0, Res - 1);
            if (x0 > x1) (x0, x1) = (x1, x0);
            if (z0 > z1) (z0, z1) = (z1, z0);
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
                fn(x, z);
        }

        private static void WorldFromGui(Rect rect, Vector2 gui, out float wx, out float wz)
        {
            float u = Mathf.Clamp01((gui.x - rect.x) / rect.width);
            float v = Mathf.Clamp01((gui.y - rect.y) / rect.height);
            wx = Mathf.Lerp(-Half, Half, u);
            // Texture v=0 is bottom in SetPixels but GUI y grows down — flip so north is up.
            wz = Mathf.Lerp(Half, -Half, v);
        }

        private static void GuiFromWorld(Rect rect, float wx, float wz, out float gx, out float gy)
        {
            float u = (wx + Half) / (Half * 2f);
            float v = (Half - wz) / (Half * 2f);
            gx = rect.x + u * rect.width;
            gy = rect.y + v * rect.height;
        }

        private static ushort DrawTerrainPicker(ushort current)
        {
            var labels = new[]
            {
                "Bare", "Short", "Long", "Rock", "Swamp", "Forest", "Tree", "Beach",
                "Mtn", "Hill", "River", "Lake", "Ocean", "Fall", "Ice+", "Ice-",
                "Trench", "NoEnt", "Shallow", "Deep", "Fast",
            };
            int idx = Mathf.Clamp(current, 0, labels.Length - 1);
            idx = GUILayout.SelectionGrid(idx, labels, 3);
            return (ushort)idx;
        }

        private static Color32 TerrainColor(ushort def)
        {
            switch (def)
            {
                case DefaultTerrainCatalog.GrassBare: return new Color32(160, 150, 90, 255);
                case DefaultTerrainCatalog.GrassShort: return new Color32(90, 140, 70, 255);
                case DefaultTerrainCatalog.GrassLong: return new Color32(60, 110, 50, 255);
                case DefaultTerrainCatalog.Rock: return new Color32(120, 120, 120, 255);
                case DefaultTerrainCatalog.Swamp: return new Color32(70, 90, 50, 255);
                case DefaultTerrainCatalog.Forest: return new Color32(30, 80, 40, 255);
                case DefaultTerrainCatalog.Tree: return new Color32(20, 60, 30, 255);
                case DefaultTerrainCatalog.Beach: return new Color32(210, 190, 130, 255);
                case DefaultTerrainCatalog.Mountain: return new Color32(90, 85, 80, 255);
                case DefaultTerrainCatalog.Hill: return new Color32(110, 130, 80, 255);
                case DefaultTerrainCatalog.WaterRiver:
                case DefaultTerrainCatalog.WaterShallow: return new Color32(70, 140, 190, 255);
                case DefaultTerrainCatalog.WaterDeep:
                case DefaultTerrainCatalog.WaterOcean: return new Color32(30, 70, 140, 255);
                case DefaultTerrainCatalog.WaterFast: return new Color32(50, 160, 200, 255);
                case DefaultTerrainCatalog.WaterLake: return new Color32(40, 100, 160, 255);
                case DefaultTerrainCatalog.WaterWaterfall: return new Color32(150, 200, 220, 255);
                case DefaultTerrainCatalog.IceThick:
                case DefaultTerrainCatalog.IceThin: return new Color32(200, 220, 240, 255);
                case DefaultTerrainCatalog.Trench: return new Color32(80, 60, 40, 255);
                case DefaultTerrainCatalog.NoEntry: return new Color32(20, 20, 20, 255);
                default: return new Color32(100, 100, 100, 255);
            }
        }

        private void SaveCurrent()
        {
            _map.id = MapCatalog.SanitizeId(_map.id);
            if (string.IsNullOrEmpty(_map.displayName))
                _map.displayName = _map.id;
            string path = MapCatalog.Save(_map);
            AssetDatabase.Refresh();
            _status = $"Saved {path} (also mirrored to StreamingAssets). Appears in match menu as '{_map.displayName} ★'.";
        }

        private void SaveAsDialog()
        {
            string dir = MapCatalog.SharedMapsDirectory;
            Directory.CreateDirectory(dir);
            string path = EditorUtility.SaveFilePanel("Save Asterra Map", dir, _map.id + ".map.json", "json");
            if (string.IsNullOrEmpty(path))
                return;
            string file = Path.GetFileName(path);
            if (file.EndsWith(".map.json", StringComparison.OrdinalIgnoreCase))
                _map.id = MapCatalog.SanitizeId(file.Substring(0, file.Length - ".map.json".Length));
            else if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                _map.id = MapCatalog.SanitizeId(Path.GetFileNameWithoutExtension(file));
            SaveCurrent();
        }

        private void LoadDialog()
        {
            string dir = MapCatalog.SharedMapsDirectory;
            Directory.CreateDirectory(dir);
            string path = EditorUtility.OpenFilePanel("Load Asterra Map", dir, "json");
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                var def = JsonUtility.FromJson<MapDefinition>(File.ReadAllText(path));
                if (def == null)
                    throw new Exception("Parse failed");
                def.EnsureArrays();
                _map = def;
                _previewDirty = true;
                _status = $"Loaded {path}";
            }
            catch (Exception e)
            {
                _status = "Load failed: " + e.Message;
            }
        }

        private void ApplyTwinKeepsTerrainTemplate()
        {
            _map.terrain = new[]
            {
                Rect(-380, -40, -320, 40, DefaultTerrainCatalog.GrassBare),
                Rect(320, -40, 380, 40, DefaultTerrainCatalog.GrassBare),
                Rect(-200, 80, 200, 160, DefaultTerrainCatalog.GrassLong),
                Rect(-200, -160, 200, -80, DefaultTerrainCatalog.GrassLong),
                Rect(-140, -110, -60, -40, DefaultTerrainCatalog.Forest),
                Rect(60, 40, 140, 110, DefaultTerrainCatalog.Forest),
                Rect(-50, -50, 50, 50, DefaultTerrainCatalog.GrassShort),
                Rect(-220, -30, -180, 30, DefaultTerrainCatalog.Hill),
                Rect(180, -30, 220, 30, DefaultTerrainCatalog.Hill),
            };
            if (_map.keeps == null || _map.keeps.Length == 0)
            {
                _map.keeps = new[]
                {
                    new MapKeepSpawn { seatIndex = 0, x = -350f, z = 0f },
                    new MapKeepSpawn { seatIndex = 1, x = 350f, z = 0f },
                };
            }

            _map.cameraFocusX = -320f;
            _map.cameraFocusZ = 0f;
            _previewDirty = true;
        }

        private static MapTerrainPaint Rect(float minX, float minZ, float maxX, float maxZ, ushort t) =>
            new MapTerrainPaint
            {
                shape = "rect", minX = minX, minZ = minZ, maxX = maxX, maxZ = maxZ, terrainIndex = t,
            };

        private static MapDefinition NewBlank()
        {
            return new MapDefinition
            {
                id = "custom_arena",
                displayName = "Custom Arena",
                formatVersion = 1,
                defaultTerrain = DefaultTerrainCatalog.GrassShort,
                cameraFocusX = -320f,
                cameraFocusZ = 0f,
                keeps = new[]
                {
                    new MapKeepSpawn { seatIndex = 0, x = -320f, z = 0f },
                    new MapKeepSpawn { seatIndex = 1, x = 320f, z = 0f },
                },
                units = new[]
                {
                    new MapUnitSpawn { seatIndex = 0, role = "basic", x = -290f, z = -15f },
                    new MapUnitSpawn { seatIndex = 0, role = "basic", x = -290f, z = 15f },
                    new MapUnitSpawn { seatIndex = 0, role = "builder", x = -270f, z = 0f },
                    new MapUnitSpawn { seatIndex = 1, role = "basic", x = 290f, z = -15f },
                    new MapUnitSpawn { seatIndex = 1, role = "basic", x = 290f, z = 15f },
                    new MapUnitSpawn { seatIndex = 1, role = "builder", x = 270f, z = 0f },
                },
                territories = new[]
                {
                    new MapTerritory { x = 0f, z = 0f, radius = 40f, goldPerSecond = 8 },
                },
                resources = new[]
                {
                    new MapResourceNode { type = "gold", amount = 2200, x = -80f, z = 60f },
                    new MapResourceNode { type = "timber", amount = 1800, x = 80f, z = -60f },
                    new MapResourceNode { type = "gold", amount = 2400, x = -260f, z = 50f },
                    new MapResourceNode { type = "timber", amount = 2000, x = -260f, z = -50f },
                    new MapResourceNode { type = "gold", amount = 2400, x = 260f, z = -50f },
                    new MapResourceNode { type = "timber", amount = 2000, x = 260f, z = 50f },
                },
            };
        }

        private static void Append<T>(ref T[] array, T item)
        {
            array ??= Array.Empty<T>();
            var next = new T[array.Length + 1];
            Array.Copy(array, next, array.Length);
            next[array.Length] = item;
            array = next;
        }

        private static T[] Filter<T>(T[] source, Func<T, bool> pred)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<T>();
            var list = new List<T>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                if (pred(source[i]))
                    list.Add(source[i]);
            }

            return list.ToArray();
        }
    }
}
