using System;
using System.Collections.Generic;
using System.IO;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Asterra.EditorTools
{
    /// <summary>
    /// 3D map studio: landscape and place props in the Scene View, save to Shared/Maps.
    /// </summary>
    public sealed class MapCreatorWindow : EditorWindow
    {
        internal enum Tool
        {
            Move,
            Terrain,
            Raise,
            Lower,
            Texture,
            Blocked,
            KeepWest,
            KeepEast,
            Tower,
            Wall,
            Producer,
            Outpost,
            Gold,
            Timber,
            Territory,
            Tree,
            Rock,
            Bridge,
            Traversal,
            Objective,
            Talk,
            UnitWest,
            UnitEast,
            EraseOverlay,
            Farm,
            Ruin,
            Cottage,
            Mill,
            Shrine,
            Barn,
        }

        private enum SelectionKind
        {
            None,
            Keep,
            Unit,
            Resource,
            Territory,
            Destructible,
            Building,
            LinkStart,
            LinkEnd,
            Objective,
            TalkTrigger,
        }

        internal static MapCreatorWindow Current { get; private set; }

        private MapDefinition _map = NewBlank();
        private Tool _tool = Tool.Raise;
        private ushort _brushTerrain = DefaultTerrainCatalog.GrassShort;
        private string _brushTexture = TerrainSplat.Dirt;
        private float _brushRadius = 24f;
        private float _sculptStrength = 2.2f;
        private float _placeYaw;
        private readonly List<string> _undo = new();
        private readonly List<string> _redo = new();
        private bool _strokeUndoPushed;
        private float _lastPaintX = float.NaN;
        private float _lastPaintZ;
        private Vector2 _scroll;
        private string _status = "Orbit in the Scene View (Alt+LMB). Left-click to landscape or place props.";
        private Texture2D _minimap;
        private bool _minimapDirty = true;
        private const float Half = 450f;
        private const float Cell = 10f;
        private const int Res = 90;
        private bool _linkHasStart;
        private float _linkStartX;
        private float _linkStartZ;
        private string _linkType = "bridge";
        private string _objectiveKind = "reach";
        private string _talkId = "briefing";
        private bool _objectiveRequired;
        private int _attachLinkIndex = -1;
        private MapCreatorWorldPreview _world;
        private bool _framed;
        private bool _showTerrainTypes;
        private bool _showTraversal;
        private bool _showTalk;
        private SelectionKind _selKind;
        private int _selIndex = -1;
        private Vector3? _hover;

        [MenuItem("Asterra/Map Creator")]
        public static void Open()
        {
            var w = GetWindow<MapCreatorWindow>("Asterra Map Creator");
            w.minSize = new Vector2(360, 640);
            w.Show();
            var scene = SceneView.lastActiveSceneView ?? GetWindow<SceneView>();
            scene?.Focus();
        }

        private void OnEnable()
        {
            Current = this;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayMode;
            _world = new MapCreatorWorldPreview();
            _world.EnsureActive();
            _world.Sync(_map, rebuildTerrain: true);
            _framed = false;
        }

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            _world?.Dispose();
            _world = null;
            if (_minimap != null)
                DestroyImmediate(_minimap);
        }

        private void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                _world?.Dispose();
                _world = null;
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                _world = new MapCreatorWorldPreview();
                _world.EnsureActive();
                _world.Sync(_map, rebuildTerrain: true);
                _framed = false;
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            DrawMinimap();
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
                    PushUndo();
                    _map = NewBlank();
                    MarkDirty(terrain: true);
                    _status = "Blank map ready — sculpt hills and place keeps in the Scene View.";
                }
            }

            if (GUILayout.Button("Load…", EditorStyles.toolbarButton, GUILayout.Width(56)))
                LoadDialog();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(48)))
                SaveCurrent();
            if (GUILayout.Button("Save As…", EditorStyles.toolbarButton, GUILayout.Width(64)))
                SaveAsDialog();
            if (GUILayout.Button("Undo", EditorStyles.toolbarButton, GUILayout.Width(48)))
                UndoLast();
            if (GUILayout.Button("Redo", EditorStyles.toolbarButton, GUILayout.Width(48)))
                Redo();
            if (GUILayout.Button("Template: Greenveil", EditorStyles.toolbarButton))
            {
                PushUndo();
                ApplyGreenveilTerrainTemplate();
                _status = "Loaded Greenveil terrain as a starting landscape.";
            }

            if (GUILayout.Button("Frame world", EditorStyles.toolbarButton, GUILayout.Width(88)))
            {
                _world?.EnsureActive();
                _world?.Frame();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Id", GUILayout.Width(18));
            _map.id = EditorGUILayout.TextField(_map.id, GUILayout.Width(140));
            EditorGUILayout.LabelField("Name", GUILayout.Width(36));
            _map.displayName = EditorGUILayout.TextField(_map.displayName, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();
        }

        internal void DrawCompactTools()
        {
            EditorGUILayout.LabelField("Sculpt", EditorStyles.boldLabel);
            DrawToolGrid(new[]
            {
                Tool.Move, Tool.Raise, Tool.Lower, Tool.Terrain, Tool.Texture, Tool.Blocked,
            }, new[] { "Move", "Raise", "Lower", "Terrain", "Texture", "Blocked" }, 3);

            EditorGUILayout.LabelField("Place", EditorStyles.boldLabel);
            DrawToolGrid(new[]
            {
                Tool.KeepWest, Tool.KeepEast, Tool.Tower, Tool.Wall, Tool.Producer, Tool.Outpost,
                Tool.Tree, Tool.Rock, Tool.Bridge, Tool.Farm, Tool.Ruin, Tool.Cottage,
                Tool.Mill, Tool.Shrine, Tool.Barn, Tool.Gold, Tool.Timber, Tool.Territory,
                Tool.UnitWest, Tool.UnitEast, Tool.Traversal, Tool.Objective, Tool.Talk, Tool.EraseOverlay,
            }, new[]
            {
                "Keep W", "Keep E", "Tower", "Wall", "Producer", "Outpost",
                "Tree", "Rock", "Bridge", "Farm", "Ruin", "Cottage",
                "Mill", "Shrine", "Barn", "Gold", "Timber", "Territory",
                "Unit W", "Unit E", "Traversal", "Objective", "Talk", "Erase",
            }, 3);

            _brushRadius = EditorGUILayout.Slider("Brush", _brushRadius, 8f, 80f);
            if (_tool == Tool.Raise || _tool == Tool.Lower)
                _sculptStrength = EditorGUILayout.Slider("Sculpt", _sculptStrength, 0.4f, 8f);
            EditorGUILayout.LabelField($"Yaw {_placeYaw:0}°  (Q/E 15°, [ ] 90°)", EditorStyles.miniLabel);
            if (_tool == Tool.Texture)
                _brushTexture = DrawTexturePicker(_brushTexture);
        }

        private void DrawToolGrid(Tool[] tools, string[] labels, int cols)
        {
            int i = 0;
            while (i < tools.Length)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < cols && i < tools.Length; c++, i++)
                {
                    bool on = _tool == tools[i];
                    if (GUILayout.Toggle(on, labels[i], "Button") && !on)
                        _tool = tools[i];
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(248));
            DrawCompactTools();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                "Raise / Lower sculpt height (smooth falloff). Terrain paints gameplay type. Q/E rotate. Cmd+Z undo.",
                EditorStyles.wordWrappedMiniLabel);

            _showTerrainTypes = EditorGUILayout.Foldout(_showTerrainTypes, "Terrain types (Terrain tool)", true);
            if (_showTerrainTypes || _tool == Tool.Terrain)
                _brushTerrain = DrawTerrainPicker(_brushTerrain);

            _showTraversal = EditorGUILayout.Foldout(_showTraversal, "Traversal links", true);
            if (_showTraversal || _tool == Tool.Traversal)
            {
                _linkType = EditorGUILayout.TextField("New link type", _linkType);
                _attachLinkIndex = EditorGUILayout.IntField("Attach link # on place", _attachLinkIndex);
                EditorGUILayout.LabelField(
                    "Tree / rock / bridge: Attach link # (≥0) disables that crossing when the prop is destroyed. Farm / ruin / cottage / mill / shrine / barn are scenery (block movement, cannot be attacked).",
                    EditorStyles.wordWrappedMiniLabel);
            }

            if (_tool == Tool.Objective)
            {
                EditorGUILayout.LabelField("Objective", EditorStyles.boldLabel);
                _objectiveKind = EditorGUILayout.TextField("Kind", _objectiveKind);
                _objectiveRequired = EditorGUILayout.Toggle("Required (can end match if no keep/hold)", _objectiveRequired);
                EditorGUILayout.LabelField(
                    "Kinds: destroy_keeps, hold, optional_hold, reach, destroy_near, survive, protect. survive uses Hold seconds. protect is the building in the ring.",
                    EditorStyles.wordWrappedMiniLabel);
            }

            if (_tool == Tool.Talk)
            {
                EditorGUILayout.LabelField("Talk trigger", EditorStyles.boldLabel);
                _talkId = EditorGUILayout.TextField("Conversation id", _talkId);
                EditorGUILayout.LabelField(
                    "Places an enter-zone. Edit lines below (same id).",
                    EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space(4);
            _showTalk = EditorGUILayout.Foldout(_showTalk, "Conversations", true);
            if (_showTalk || _tool == Tool.Talk)
                DrawConversationEditor();

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
            EditorGUILayout.LabelField($"Height strokes: {_map.heightPaint?.Length ?? 0}");
            EditorGUILayout.LabelField($"Texture strokes: {_map.texturePaint?.Length ?? 0}");
            EditorGUILayout.LabelField($"Keeps: {_map.keeps?.Length ?? 0}");
            EditorGUILayout.LabelField($"Buildings: {_map.buildings?.Length ?? 0}");
            EditorGUILayout.LabelField($"Units: {_map.units?.Length ?? 0}");
            EditorGUILayout.LabelField($"Resources: {_map.resources?.Length ?? 0}");
            EditorGUILayout.LabelField($"Territories: {_map.territories?.Length ?? 0}");
            EditorGUILayout.LabelField($"Destructibles: {_map.destructibles?.Length ?? 0}");
            EditorGUILayout.LabelField($"Traversal links: {_map.traversalLinks?.Length ?? 0}");

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Clear overlays (keeps/units/…)"))
            {
                PushUndo();
                _map.keeps = Array.Empty<MapKeepSpawn>();
                _map.units = Array.Empty<MapUnitSpawn>();
                _map.buildings = Array.Empty<MapBuildingSpawn>();
                _map.resources = Array.Empty<MapResourceNode>();
                _map.territories = Array.Empty<MapTerritory>();
                _map.destructibles = Array.Empty<MapDestructible>();
                _map.blocked = Array.Empty<MapBlockedRect>();
                _map.traversalLinks = Array.Empty<MapTraversalLink>();
                _linkHasStart = false;
                ClearSelection();
                MarkDirty(terrain: true);
            }

            if (GUILayout.Button("Clear terrain strokes"))
            {
                PushUndo();
                _map.terrain = Array.Empty<MapTerrainPaint>();
                MarkDirty(terrain: true);
            }

            if (GUILayout.Button("Clear height sculpt"))
            {
                PushUndo();
                _map.heightPaint = Array.Empty<MapHeightPaint>();
                MarkDirty(terrain: true);
            }

            if (GUILayout.Button("Clear texture strokes"))
            {
                PushUndo();
                _map.texturePaint = Array.Empty<MapTexturePaint>();
                MarkDirty(terrain: true);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMinimap()
        {
            EditorGUILayout.BeginVertical();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            float size = Mathf.Min(position.width - 280f, 280f);
            size = Mathf.Max(160f, size);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));

            if (_minimapDirty || _minimap == null)
                RebuildMinimap();

            if (_minimap != null)
                GUI.DrawTexture(rect, _minimap, ScaleMode.StretchToFill);
            DrawMinimapMarkers(rect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField(
                "Overview only. The Scene View is the map — orbit, sculpt, and drop props there.",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawMinimapMarkers(Rect rect)
        {
            void Dot(float wx, float wz, Color c, float px)
            {
                float u = (wx + Half) / (Half * 2f);
                float v = (Half - wz) / (Half * 2f);
                float gx = rect.x + u * rect.width;
                float gy = rect.y + v * rect.height;
                EditorGUI.DrawRect(new Rect(gx - px * 0.5f, gy - px * 0.5f, px, px), c);
            }

            if (_map.keeps != null)
            {
                foreach (var k in _map.keeps)
                    Dot(k.x, k.z, k.seatIndex == 0 ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.95f, 0.25f, 0.2f), 10f);
            }

            if (_map.destructibles != null)
            {
                foreach (var d in _map.destructibles)
                    Dot(d.x, d.z, new Color(0.25f, 0.55f, 0.28f), 5f);
            }

            if (_map.resources != null)
            {
                foreach (var r in _map.resources)
                    Dot(r.x, r.z, Color.yellow, 5f);
            }

            if (_map.buildings != null)
            {
                foreach (var b in _map.buildings)
                    Dot(b.x, b.z, new Color(0.85f, 0.7f, 0.35f), 7f);
            }
        }

        private void DrawSceneHud(SceneView view)
        {
            Handles.BeginGUI();
            float width = Mathf.Max(420f, view.position.width - 28f);
            var bar = new Rect(12f, 12f, Mathf.Min(width, 920f), 78f);
            GUI.Box(bar, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(bar.x + 8f, bar.y + 6f, bar.width - 16f, bar.height - 8f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Asterra Map", EditorStyles.boldLabel, GUILayout.Width(92f));
            DrawHudTool(Tool.Raise, "Raise");
            DrawHudTool(Tool.Lower, "Lower");
            DrawHudTool(Tool.Terrain, "Paint");
            DrawHudTool(Tool.Tree, "Tree");
            DrawHudTool(Tool.Rock, "Rock");
            DrawHudTool(Tool.Tower, "Tower");
            DrawHudTool(Tool.Wall, "Wall");
            DrawHudTool(Tool.KeepWest, "Keep W");
            DrawHudTool(Tool.KeepEast, "Keep E");
            DrawHudTool(Tool.Move, "Move");
            DrawHudTool(Tool.EraseOverlay, "Erase");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label($"{_map.displayName}  ·  {_tool}  ·  {_status}", EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void DrawHudTool(Tool tool, string label)
        {
            var prev = GUI.backgroundColor;
            if (_tool == tool)
                GUI.backgroundColor = new Color(1f, 0.85f, 0.35f);
            if (GUILayout.Button(label, GUILayout.Height(22f), GUILayout.Width(64f)))
                _tool = tool;
            GUI.backgroundColor = prev;
        }

        private void OnSceneGUI(SceneView view)
        {
            if (_world == null || !_world.IsActive)
                return;
            if (EditorApplication.isPlaying)
                return;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                _status = "Close prefab isolation to edit the 3D map world.";
                return;
            }

            _world.EnsureSceneViewBound();
            if (!_world.IsShowingIn(view))
                return;

            if (!_framed)
            {
                _world.Frame();
                _framed = true;
            }

            DrawSceneHud(view);
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            var e = Event.current;
            bool overHud = e.mousePosition.y < 100f && e.mousePosition.x < 980f;
            if (overHud && e.button == 0
                && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
                e.Use();

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 hit = default;
            bool hitGround = !overHud && _world.TryPick(ray, out hit);
            _hover = hitGround ? hit : null;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                ClearSelection();
                _linkHasStart = false;
                e.Use();
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Z)
                && (e.command || e.control))
            {
                if (e.shift)
                    Redo();
                else
                    UndoLast();
                e.Use();
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Y && (e.command || e.control))
            {
                Redo();
                e.Use();
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Q || e.keyCode == KeyCode.E
                || e.keyCode == KeyCode.LeftBracket || e.keyCode == KeyCode.RightBracket))
            {
                float step = e.keyCode == KeyCode.LeftBracket || e.keyCode == KeyCode.RightBracket ? 90f : 15f;
                if (e.keyCode == KeyCode.Q || e.keyCode == KeyCode.LeftBracket)
                    step = -step;
                NudgeYaw(step);
                e.Use();
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
                && _selKind != SelectionKind.None)
            {
                PushUndo();
                EraseSelected();
                e.Use();
            }

            bool orbiting = e.alt || e.button == 1 || e.button == 2;
            if (!orbiting && hitGround && e.button == 0
                && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                if (_tool == Tool.Move)
                {
                    if (e.type == EventType.MouseDown)
                    {
                        PushUndo();
                        TrySelectAt(hit.x, hit.z);
                    }
                    if (_selKind != SelectionKind.None)
                    {
                        MoveSelected(hit.x, hit.z);
                        e.Use();
                        Repaint();
                        SceneView.RepaintAll();
                    }
                }
                else
                {
                    bool strokeTool = IsStrokeTool(_tool);
                    if (e.type == EventType.MouseDrag && !strokeTool)
                        return;
                    if (strokeTool && e.type == EventType.MouseDrag)
                    {
                        float dx = hit.x - _lastPaintX;
                        float dz = hit.z - _lastPaintZ;
                        float spacing = _brushRadius * 0.4f;
                        if (!float.IsNaN(_lastPaintX) && dx * dx + dz * dz < spacing * spacing)
                            return;
                    }

                    if (e.type == EventType.MouseDown && !_strokeUndoPushed)
                    {
                        PushUndo();
                        _strokeUndoPushed = true;
                    }

                    ApplyClick(hit.x, hit.z, e.shift);
                    if (strokeTool)
                    {
                        _lastPaintX = hit.x;
                        _lastPaintZ = hit.z;
                    }

                    e.Use();
                    Repaint();
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _lastPaintX = float.NaN;
                _strokeUndoPushed = false;
            }

            _world.DrawSceneGizmos(_map, _brushRadius, _hover);
            if (_selKind != SelectionKind.None && TryGetSelectedPos(out float sx, out float sz))
            {
                Handles.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                Handles.DrawWireDisc(new Vector3(sx, _world.GroundY(sx, sz) + 0.6f, sz), Vector3.up, 10f);
            }
        }

        private static bool IsStrokeTool(Tool tool) =>
            tool == Tool.Terrain || tool == Tool.Texture || tool == Tool.Raise || tool == Tool.Lower
            || tool == Tool.Blocked;

        private void MarkDirty(bool terrain)
        {
            _minimapDirty = true;
            _world?.Sync(_map, rebuildTerrain: terrain);
        }

        private void PushUndo()
        {
            _undo.Add(JsonUtility.ToJson(_map));
            if (_undo.Count > 48)
                _undo.RemoveAt(0);
            _redo.Clear();
        }

        private void UndoLast()
        {
            if (_undo.Count == 0)
                return;
            _redo.Add(JsonUtility.ToJson(_map));
            string json = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            ApplySnapshot(json);
            _status = "Undo";
        }

        private void Redo()
        {
            if (_redo.Count == 0)
                return;
            _undo.Add(JsonUtility.ToJson(_map));
            string json = _redo[_redo.Count - 1];
            _redo.RemoveAt(_redo.Count - 1);
            ApplySnapshot(json);
            _status = "Redo";
        }

        private void ApplySnapshot(string json)
        {
            var def = JsonUtility.FromJson<MapDefinition>(json);
            if (def == null)
                return;
            def.EnsureArrays();
            _map = def;
            ClearSelection();
            MarkDirty(terrain: true);
            Repaint();
            SceneView.RepaintAll();
        }

        private void NudgeYaw(float degrees)
        {
            if (CanNudgeYaw())
            {
                PushUndo();
                TryNudgeSelectedYaw(degrees);
                MarkDirty(terrain: false);
                _status = $"Rotated {degrees:+0;-0}°";
                Repaint();
                SceneView.RepaintAll();
                return;
            }

            _placeYaw = Mathf.Repeat(_placeYaw + degrees + 360f, 360f);
            _status = $"Place yaw {_placeYaw:0}°";
            Repaint();
        }

        private bool CanNudgeYaw()
        {
            _map.EnsureArrays();
            return _selKind switch
            {
                SelectionKind.Keep => InRange(_map.keeps, _selIndex),
                SelectionKind.Unit => InRange(_map.units, _selIndex),
                SelectionKind.Resource => InRange(_map.resources, _selIndex),
                SelectionKind.Destructible => InRange(_map.destructibles, _selIndex),
                SelectionKind.Building => InRange(_map.buildings, _selIndex),
                _ => false,
            };
        }

        private bool TryNudgeSelectedYaw(float degrees)
        {
            _map.EnsureArrays();
            switch (_selKind)
            {
                case SelectionKind.Keep when InRange(_map.keeps, _selIndex):
                    _map.keeps[_selIndex].yawDegrees = Mathf.Repeat(_map.keeps[_selIndex].yawDegrees + degrees + 360f, 360f);
                    return true;
                case SelectionKind.Unit when InRange(_map.units, _selIndex):
                    _map.units[_selIndex].yawDegrees = Mathf.Repeat(_map.units[_selIndex].yawDegrees + degrees + 360f, 360f);
                    return true;
                case SelectionKind.Resource when InRange(_map.resources, _selIndex):
                    _map.resources[_selIndex].yawDegrees = Mathf.Repeat(_map.resources[_selIndex].yawDegrees + degrees + 360f, 360f);
                    return true;
                case SelectionKind.Destructible when InRange(_map.destructibles, _selIndex):
                    _map.destructibles[_selIndex].yawDegrees = Mathf.Repeat(_map.destructibles[_selIndex].yawDegrees + degrees + 360f, 360f);
                    return true;
                case SelectionKind.Building when InRange(_map.buildings, _selIndex):
                    _map.buildings[_selIndex].yawDegrees = Mathf.Repeat(_map.buildings[_selIndex].yawDegrees + degrees + 360f, 360f);
                    return true;
                default:
                    return false;
            }
        }

        private static string TitleForKind(string kind)
        {
            switch ((kind ?? "").ToLowerInvariant())
            {
                case "destroy_keeps": return "Destroy the enemy keep";
                case "hold": return "Hold territory";
                case "optional_hold": return "Hold (optional)";
                case "destroy_near": return "Destroy nearby";
                case "survive": return "Survive";
                case "protect": return "Protect this building";
                default: return "Reach this ground";
            }
        }

        private void DrawConversationEditor()
        {
            _map.EnsureArrays();
            if (GUILayout.Button("Add line"))
            {
                PushUndo();
                Append(ref _map.conversations, new MapConversationLine
                {
                    id = string.IsNullOrEmpty(_talkId) ? "briefing" : _talkId,
                    speaker = "Speaker",
                    text = "…",
                });
            }

            for (int i = 0; i < _map.conversations.Length; i++)
            {
                var line = _map.conversations[i];
                EditorGUILayout.BeginVertical("box");
                line.id = EditorGUILayout.TextField("Id", line.id ?? "talk");
                line.speaker = EditorGUILayout.TextField("Speaker", line.speaker ?? "");
                line.text = EditorGUILayout.TextField("Text", line.text ?? "");
                if (GUILayout.Button("Remove line"))
                {
                    _map.conversations = RemoveAt(_map.conversations, i);
                    break;
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void ApplyClick(float wx, float wz, bool shift)
        {
            _map.EnsureArrays();
            bool terrainChanged = false;
            switch (_tool)
            {
                case Tool.Terrain:
                    AddTerrainDisk(wx, wz, _brushRadius, _brushTerrain);
                    _status = $"Terrain {_brushTerrain} @ ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
                case Tool.Raise:
                    AddHeightDisk(wx, wz, _brushRadius, shift ? _sculptStrength * 2.2f : _sculptStrength);
                    _status = $"Raise {_sculptStrength:0.0} @ ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
                case Tool.Lower:
                    AddHeightDisk(wx, wz, _brushRadius, -(shift ? _sculptStrength * 2.2f : _sculptStrength));
                    _status = $"Lower {_sculptStrength:0.0} @ ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
                case Tool.Texture:
                    Append(ref _map.texturePaint, new MapTexturePaint
                    {
                        shape = "disk",
                        x = wx,
                        z = wz,
                        radius = _brushRadius,
                        layer = _brushTexture,
                        strength = 0.85f,
                    });
                    _status = $"Texture {_brushTexture} @ ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
                case Tool.Blocked:
                    float r = _brushRadius;
                    Append(ref _map.blocked, new MapBlockedRect
                    {
                        minX = wx - r, minZ = wz - r, maxX = wx + r, maxZ = wz + r, blocked = true,
                    });
                    _status = $"Blocked @ ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
                case Tool.KeepWest:
                    UpsertKeep(0, wx, wz);
                    _map.cameraFocusX = wx;
                    _map.cameraFocusZ = wz;
                    _status = $"West keep @ ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
                case Tool.KeepEast:
                    UpsertKeep(1, wx, wz);
                    _status = $"East keep @ ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
                case Tool.Tower:
                    PlaceBuilding("tower", wx, wz, shift ? 1 : 0);
                    break;
                case Tool.Wall:
                    PlaceBuilding("wall", wx, wz, shift ? 1 : 0);
                    break;
                case Tool.Producer:
                    PlaceBuilding("producer", wx, wz, shift ? 1 : 0);
                    break;
                case Tool.Outpost:
                    PlaceBuilding("outpost", wx, wz, shift ? 1 : 0);
                    break;
                case Tool.Gold:
                    Append(ref _map.resources, new MapResourceNode
                    {
                        type = "gold", amount = shift ? 2500 : 2000, x = wx, z = wz, yawDegrees = _placeYaw,
                    });
                    EnsureLandDisk(wx, wz, 12f);
                    _status = $"Gold @ ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
                case Tool.Timber:
                    Append(ref _map.resources, new MapResourceNode
                    {
                        type = "timber", amount = shift ? 2000 : 1600, x = wx, z = wz, yawDegrees = _placeYaw,
                    });
                    EnsureLandDisk(wx, wz, 12f);
                    _status = $"Timber @ ({wx:0},{wz:0})";
                    terrainChanged = true;
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
                case Tool.Farm:
                    PlaceDestructible("farm", wx, wz);
                    break;
                case Tool.Ruin:
                    PlaceDestructible("crumbling_tower", wx, wz);
                    break;
                case Tool.Cottage:
                    PlaceDestructible("cottage", wx, wz);
                    break;
                case Tool.Mill:
                    PlaceDestructible("mill", wx, wz);
                    break;
                case Tool.Shrine:
                    PlaceDestructible("shrine", wx, wz);
                    break;
                case Tool.Barn:
                    PlaceDestructible("barn", wx, wz);
                    break;
                case Tool.Traversal:
                    if (!_linkHasStart)
                    {
                        _linkHasStart = true;
                        _linkStartX = wx;
                        _linkStartZ = wz;
                        _status = $"Traversal start @ ({wx:0},{wz:0}) — click the far bank";
                    }
                    else
                    {
                        Append(ref _map.traversalLinks, new MapTraversalLink
                        {
                            startX = _linkStartX,
                            startZ = _linkStartZ,
                            endX = wx,
                            endZ = wz,
                            type = _linkType,
                            durationSeconds = 1.25f,
                            approachRadius = 8f,
                            enabled = true,
                        });
                        _linkHasStart = false;
                        _status = $"Traversal {_linkType} to ({wx:0},{wz:0})";
                    }
                    break;
                case Tool.Objective:
                    Append(ref _map.objectives, new MapObjective
                    {
                        id = "obj_" + (_map.objectives.Length + 1),
                        title = TitleForKind(_objectiveKind),
                        kind = string.IsNullOrEmpty(_objectiveKind) ? "reach" : _objectiveKind,
                        required = _objectiveRequired,
                        x = wx,
                        z = wz,
                        radius = Mathf.Max(12f, _brushRadius),
                        holdSeconds = 90f,
                    });
                    _status = $"Objective {_objectiveKind} @ ({wx:0},{wz:0})";
                    break;
                case Tool.Talk:
                    Append(ref _map.talkTriggers, new MapTalkTrigger
                    {
                        conversationId = string.IsNullOrEmpty(_talkId) ? "briefing" : _talkId,
                        when = "enter",
                        x = wx,
                        z = wz,
                        radius = Mathf.Max(12f, _brushRadius),
                    });
                    _status = $"Talk '{_talkId}' enter @ ({wx:0},{wz:0})";
                    break;
                case Tool.UnitWest:
                    Append(ref _map.units, new MapUnitSpawn
                    {
                        seatIndex = 0, role = shift ? "builder" : "basic", x = wx, z = wz, yawDegrees = _placeYaw,
                    });
                    _status = $"West unit @ ({wx:0},{wz:0})";
                    break;
                case Tool.UnitEast:
                    Append(ref _map.units, new MapUnitSpawn
                    {
                        seatIndex = 1, role = shift ? "builder" : "basic", x = wx, z = wz, yawDegrees = _placeYaw,
                    });
                    _status = $"East unit @ ({wx:0},{wz:0})";
                    break;
                case Tool.EraseOverlay:
                    EraseNear(wx, wz, 18f);
                    _status = $"Erased overlays near ({wx:0},{wz:0})";
                    terrainChanged = true;
                    break;
            }

            MarkDirty(terrainChanged);
        }

        private void TrySelectAt(float x, float z)
        {
            _map.EnsureArrays();
            float best = 22f * 22f;
            SelectionKind kind = SelectionKind.None;
            int index = -1;

            void Consider(SelectionKind k, int i, float ax, float az)
            {
                float dx = ax - x;
                float dz = az - z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best)
                {
                    best = d2;
                    kind = k;
                    index = i;
                }
            }

            if (_map.keeps != null)
            {
                for (int i = 0; i < _map.keeps.Length; i++)
                    Consider(SelectionKind.Keep, i, _map.keeps[i].x, _map.keeps[i].z);
            }

            if (_map.units != null)
            {
                for (int i = 0; i < _map.units.Length; i++)
                    Consider(SelectionKind.Unit, i, _map.units[i].x, _map.units[i].z);
            }

            if (_map.resources != null)
            {
                for (int i = 0; i < _map.resources.Length; i++)
                    Consider(SelectionKind.Resource, i, _map.resources[i].x, _map.resources[i].z);
            }

            if (_map.territories != null)
            {
                for (int i = 0; i < _map.territories.Length; i++)
                    Consider(SelectionKind.Territory, i, _map.territories[i].x, _map.territories[i].z);
            }

            if (_map.destructibles != null)
            {
                for (int i = 0; i < _map.destructibles.Length; i++)
                    Consider(SelectionKind.Destructible, i, _map.destructibles[i].x, _map.destructibles[i].z);
            }

            if (_map.buildings != null)
            {
                for (int i = 0; i < _map.buildings.Length; i++)
                    Consider(SelectionKind.Building, i, _map.buildings[i].x, _map.buildings[i].z);
            }

            if (_map.traversalLinks != null)
            {
                for (int i = 0; i < _map.traversalLinks.Length; i++)
                {
                    var link = _map.traversalLinks[i];
                    Consider(SelectionKind.LinkStart, i, link.startX, link.startZ);
                    Consider(SelectionKind.LinkEnd, i, link.endX, link.endZ);
                }
            }

            if (_map.objectives != null)
            {
                for (int i = 0; i < _map.objectives.Length; i++)
                    Consider(SelectionKind.Objective, i, _map.objectives[i].x, _map.objectives[i].z);
            }

            if (_map.talkTriggers != null)
            {
                for (int i = 0; i < _map.talkTriggers.Length; i++)
                    Consider(SelectionKind.TalkTrigger, i, _map.talkTriggers[i].x, _map.talkTriggers[i].z);
            }

            _selKind = kind;
            _selIndex = index;
            _status = kind == SelectionKind.None
                ? "Nothing nearby to move."
                : $"Moving {kind} #{index} — drag in the world, Delete to remove.";
        }

        private void MoveSelected(float x, float z)
        {
            if (!TryGetSelectedRef(out var set))
                return;
            set(x, z);
            MarkDirty(terrain: false);
        }

        private bool TryGetSelectedPos(out float x, out float z)
        {
            x = z = 0f;
            switch (_selKind)
            {
                case SelectionKind.Keep when InRange(_map.keeps, _selIndex):
                    x = _map.keeps[_selIndex].x;
                    z = _map.keeps[_selIndex].z;
                    return true;
                case SelectionKind.Unit when InRange(_map.units, _selIndex):
                    x = _map.units[_selIndex].x;
                    z = _map.units[_selIndex].z;
                    return true;
                case SelectionKind.Resource when InRange(_map.resources, _selIndex):
                    x = _map.resources[_selIndex].x;
                    z = _map.resources[_selIndex].z;
                    return true;
                case SelectionKind.Territory when InRange(_map.territories, _selIndex):
                    x = _map.territories[_selIndex].x;
                    z = _map.territories[_selIndex].z;
                    return true;
                case SelectionKind.Destructible when InRange(_map.destructibles, _selIndex):
                    x = _map.destructibles[_selIndex].x;
                    z = _map.destructibles[_selIndex].z;
                    return true;
                case SelectionKind.Building when InRange(_map.buildings, _selIndex):
                    x = _map.buildings[_selIndex].x;
                    z = _map.buildings[_selIndex].z;
                    return true;
                case SelectionKind.LinkStart when InRange(_map.traversalLinks, _selIndex):
                    x = _map.traversalLinks[_selIndex].startX;
                    z = _map.traversalLinks[_selIndex].startZ;
                    return true;
                case SelectionKind.LinkEnd when InRange(_map.traversalLinks, _selIndex):
                    x = _map.traversalLinks[_selIndex].endX;
                    z = _map.traversalLinks[_selIndex].endZ;
                    return true;
                case SelectionKind.Objective when InRange(_map.objectives, _selIndex):
                    x = _map.objectives[_selIndex].x;
                    z = _map.objectives[_selIndex].z;
                    return true;
                case SelectionKind.TalkTrigger when InRange(_map.talkTriggers, _selIndex):
                    x = _map.talkTriggers[_selIndex].x;
                    z = _map.talkTriggers[_selIndex].z;
                    return true;
                default:
                    return false;
            }
        }

        private bool TryGetSelectedRef(out Action<float, float> set)
        {
            set = null;
            switch (_selKind)
            {
                case SelectionKind.Keep when InRange(_map.keeps, _selIndex):
                    set = (x, z) =>
                    {
                        _map.keeps[_selIndex].x = x;
                        _map.keeps[_selIndex].z = z;
                    };
                    return true;
                case SelectionKind.Unit when InRange(_map.units, _selIndex):
                    set = (x, z) =>
                    {
                        _map.units[_selIndex].x = x;
                        _map.units[_selIndex].z = z;
                    };
                    return true;
                case SelectionKind.Resource when InRange(_map.resources, _selIndex):
                    set = (x, z) =>
                    {
                        _map.resources[_selIndex].x = x;
                        _map.resources[_selIndex].z = z;
                    };
                    return true;
                case SelectionKind.Territory when InRange(_map.territories, _selIndex):
                    set = (x, z) =>
                    {
                        _map.territories[_selIndex].x = x;
                        _map.territories[_selIndex].z = z;
                    };
                    return true;
                case SelectionKind.Destructible when InRange(_map.destructibles, _selIndex):
                    set = (x, z) =>
                    {
                        _map.destructibles[_selIndex].x = x;
                        _map.destructibles[_selIndex].z = z;
                    };
                    return true;
                case SelectionKind.Building when InRange(_map.buildings, _selIndex):
                    set = (x, z) =>
                    {
                        _map.buildings[_selIndex].x = x;
                        _map.buildings[_selIndex].z = z;
                    };
                    return true;
                case SelectionKind.LinkStart when InRange(_map.traversalLinks, _selIndex):
                    set = (x, z) =>
                    {
                        _map.traversalLinks[_selIndex].startX = x;
                        _map.traversalLinks[_selIndex].startZ = z;
                    };
                    return true;
                case SelectionKind.LinkEnd when InRange(_map.traversalLinks, _selIndex):
                    set = (x, z) =>
                    {
                        _map.traversalLinks[_selIndex].endX = x;
                        _map.traversalLinks[_selIndex].endZ = z;
                    };
                    return true;
                case SelectionKind.Objective when InRange(_map.objectives, _selIndex):
                    set = (x, z) =>
                    {
                        _map.objectives[_selIndex].x = x;
                        _map.objectives[_selIndex].z = z;
                    };
                    return true;
                case SelectionKind.TalkTrigger when InRange(_map.talkTriggers, _selIndex):
                    set = (x, z) =>
                    {
                        _map.talkTriggers[_selIndex].x = x;
                        _map.talkTriggers[_selIndex].z = z;
                    };
                    return true;
                default:
                    return false;
            }
        }

        private void EraseSelected()
        {
            _map.EnsureArrays();
            switch (_selKind)
            {
                case SelectionKind.Keep:
                    _map.keeps = RemoveAt(_map.keeps, _selIndex);
                    break;
                case SelectionKind.Unit:
                    _map.units = RemoveAt(_map.units, _selIndex);
                    break;
                case SelectionKind.Resource:
                    _map.resources = RemoveAt(_map.resources, _selIndex);
                    break;
                case SelectionKind.Territory:
                    _map.territories = RemoveAt(_map.territories, _selIndex);
                    break;
                case SelectionKind.Destructible:
                    _map.destructibles = RemoveAt(_map.destructibles, _selIndex);
                    break;
                case SelectionKind.Building:
                    _map.buildings = RemoveAt(_map.buildings, _selIndex);
                    break;
                case SelectionKind.LinkStart:
                case SelectionKind.LinkEnd:
                    RemoveLinkAt(_selIndex);
                    ClearSelection();
                    return;
                case SelectionKind.Objective:
                    _map.objectives = RemoveAt(_map.objectives, _selIndex);
                    break;
                case SelectionKind.TalkTrigger:
                    _map.talkTriggers = RemoveAt(_map.talkTriggers, _selIndex);
                    break;
            }

            ClearSelection();
            MarkDirty(terrain: false);
            _status = "Removed selection.";
        }

        private void ClearSelection()
        {
            _selKind = SelectionKind.None;
            _selIndex = -1;
        }

        private void UpsertKeep(int seat, float x, float z)
        {
            var list = new List<MapKeepSpawn>(_map.keeps ?? Array.Empty<MapKeepSpawn>());
            list.RemoveAll(k => k.seatIndex == seat);
            list.Add(new MapKeepSpawn { seatIndex = seat, x = x, z = z, yawDegrees = _placeYaw });
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

        private void AddHeightDisk(float x, float z, float radius, float delta)
        {
            _map.EnsureArrays();
            Append(ref _map.heightPaint, new MapHeightPaint
            {
                x = x,
                z = z,
                radius = radius,
                delta = delta,
                falloff = 0.85f,
            });
        }

        private void PlaceBuilding(string role, float wx, float wz, int seat)
        {
            Append(ref _map.buildings, new MapBuildingSpawn
            {
                seatIndex = seat,
                role = role,
                x = wx,
                z = wz,
                yawDegrees = _placeYaw,
            });
            _status = $"{role} (seat {seat}) @ ({wx:0},{wz:0}) yaw {_placeYaw:0}°";
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
            _map.buildings = Filter(_map.buildings, b => !Near(b.x, b.z));
            _map.objectives = Filter(_map.objectives, o => !Near(o.x, o.z));
            _map.talkTriggers = Filter(_map.talkTriggers, t => !Near(t.x, t.z));
            _map.texturePaint = Filter(_map.texturePaint, t => !Near(t.x, t.z));
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

            ClearSelection();
        }

        private void PlaceDestructible(string catalogId, float wx, float wz)
        {
            Append(ref _map.destructibles, new MapDestructible
            {
                catalogId = catalogId,
                x = wx,
                z = wz,
                yawDegrees = _placeYaw,
                linkedTraversalLinkId = _attachLinkIndex,
            });
            string linkNote = _attachLinkIndex >= 0 ? $" link#{_attachLinkIndex}" : string.Empty;
            _status = $"{catalogId} @ ({wx:0},{wz:0}) yaw {_placeYaw:0}°{linkNote}";
        }

        private void RemoveLinkAt(int index)
        {
            _map.EnsureArrays();
            if (index < 0 || index >= _map.traversalLinks.Length)
                return;
            var list = new List<MapTraversalLink>(_map.traversalLinks);
            list.RemoveAt(index);
            _map.traversalLinks = list.ToArray();
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
            MarkDirty(terrain: false);
            Repaint();
        }

        private void RebuildMinimap()
        {
            _map.EnsureArrays();
            if (_minimap == null || _minimap.width != Res)
            {
                if (_minimap != null)
                    DestroyImmediate(_minimap);
                _minimap = new Texture2D(Res, Res, TextureFormat.RGBA32, false)
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

            if (_map.texturePaint != null)
            {
                for (int i = 0; i < _map.texturePaint.Length; i++)
                {
                    var paint = _map.texturePaint[i];
                    if (paint == null)
                        continue;
                    var tint = TerrainSplat.PreviewTint(paint.layer);
                    StampTexturePreview(pixels, paint, tint);
                }
            }

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

            _minimap.SetPixels32(pixels);
            _minimap.Apply(false);
            _minimapDirty = false;
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

        private static string DrawTexturePicker(string current)
        {
            var labels = new[] { "grass", "dirt", "rock", "sand" };
            int idx = 1;
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == current)
                    idx = i;
            }

            idx = GUILayout.SelectionGrid(idx, labels, 4);
            return labels[Mathf.Clamp(idx, 0, labels.Length - 1)];
        }

        private void StampTexturePreview(Color32[] pixels, MapTexturePaint paint, Color32 tint)
        {
            float radius = paint.radius > 0.5f ? paint.radius : 16f;
            bool disk = string.IsNullOrEmpty(paint.shape) || paint.shape.ToLowerInvariant() != "rect";
            float minX = disk ? paint.x - radius : paint.minX;
            float minZ = disk ? paint.z - radius : paint.minZ;
            float maxX = disk ? paint.x + radius : paint.maxX;
            float maxZ = disk ? paint.z + radius : paint.maxZ;
            float r2 = radius * radius;
            ForCellsInRect(minX, minZ, maxX, maxZ, (cx, cz) =>
            {
                if (disk)
                {
                    float wx = -Half + (cx + 0.5f) * Cell;
                    float wz = -Half + (cz + 0.5f) * Cell;
                    float dx = wx - paint.x;
                    float dz = wz - paint.z;
                    if (dx * dx + dz * dz > r2)
                        return;
                }

                int idx = cz * Res + cx;
                var c = pixels[idx];
                pixels[idx] = new Color32(
                    (byte)((c.r + tint.r) / 2),
                    (byte)((c.g + tint.g) / 2),
                    (byte)((c.b + tint.b) / 2),
                    255);
            });
        }

        private static ushort DrawTerrainPicker(ushort current)
        {
            var labels = new[]
            {
                "Bare", "Short", "Long", "Rock", "Swamp", "Forest", "Tree", "Beach",
                "Mtn", "Hill", "River", "Lake", "Ocean", "Fall", "Ice+", "Ice-",
                "Trench", "NoEnt", "Shallow", "Deep", "Fast",
                "Berm", "Spike", "Debris", "Crater", "Scorch", "Gap",
                "Road", "Mud", "Rubble", "Snow",
            };
            int idx = Mathf.Clamp(current, 0, labels.Length - 1);
            idx = GUILayout.SelectionGrid(idx, labels, 4);
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
                PushUndo();
                _map = def;
                ClearSelection();
                _framed = false;
                MarkDirty(terrain: true);
                _status = $"Loaded {path}";
            }
            catch (Exception e)
            {
                _status = "Load failed: " + e.Message;
            }
        }

        private void ApplyGreenveilTerrainTemplate()
        {
            var src = BuiltinMaps.LushForest();
            _map.terrain = src.terrain;
            _map.keeps = src.keeps;
            _map.cameraFocusX = src.cameraFocusX;
            _map.cameraFocusZ = src.cameraFocusZ;
            _map.defaultTerrain = src.defaultTerrain;
            MarkDirty(terrain: true);
        }

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

        private static T[] RemoveAt<T>(T[] source, int index)
        {
            if (source == null || index < 0 || index >= source.Length)
                return source ?? Array.Empty<T>();
            var list = new List<T>(source);
            list.RemoveAt(index);
            return list.ToArray();
        }

        private static bool InRange<T>(T[] source, int index) =>
            source != null && index >= 0 && index < source.Length;
    }
}
