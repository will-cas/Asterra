using Asterra.Core;
using Asterra.Gameplay.Content;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>OnGUI minimap: units, buildings, resources, territory; click to pan camera.</summary>
    public sealed class MinimapPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private float mapSize = 180f;
        [SerializeField] private float margin = 12f;

        private RtsCameraRig _cameraRig;
        private FogOfWarPresenter _fog;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
        }

        private void OnGUI()
        {
            if (match == null || match.World == null || match.Session == null)
                return;
            if (_cameraRig == null)
                _cameraRig = FindFirstObjectByType<RtsCameraRig>();
            if (_fog == null)
                _fog = FindFirstObjectByType<FogOfWarPresenter>();

            float size = mapSize;
            Rect mapRect = new Rect(Screen.width - size - margin, Screen.height - size - margin, size, size);

            // Ground + fog-ish dark backdrop
            DrawRect(mapRect, new Color(0.12f, 0.16f, 0.12f, 0.92f));
            DrawRectBorder(mapRect, 2f, new Color(0.35f, 0.4f, 0.35f, 0.95f));

            var local = match.Session.LocalPlayer;
            float half = MapBounds.PlayableHalfExtent;

            // Territory circles
            var territories = match.World.Territories;
            for (int i = 0; i < territories.Count; i++)
            {
                var t = territories[i];
                Vector2 c = WorldToMinimap(t.X, t.Z, mapRect, half);
                float r = (t.Radius / (half * 2f)) * size;
                DrawCircle(c, r, new Color(0.35f, 0.7f, 0.95f, 0.35f));
            }

            // Resources
            var resources = match.World.Resources;
            if (resources != null)
            {
                for (int i = 0; i < resources.Count; i++)
                {
                    var r = resources[i];
                    if (r.Remaining <= 0)
                        continue;
                    Vector2 p = WorldToMinimap(r.X, r.Z, mapRect, half);
                    var color = r.Type == ResourceType.Gold
                        ? new Color(0.95f, 0.82f, 0.2f)
                        : new Color(0.55f, 0.35f, 0.18f);
                    DrawRect(new Rect(p.x - 2f, p.y - 2f, 4f, 4f), color);
                }
            }

            // Buildings as squares
            var buildings = match.World.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                if (b.State == BuildingState.Destroyed)
                    continue;
                bool own = b.Owner == local;
                if (!own && _fog != null && !_fog.IsWorldVisible(b.X, b.Z))
                    continue;
                Vector2 p = WorldToMinimap(b.X, b.Z, mapRect, half);
                var color = own ? Color.white : new Color(0.9f, 0.2f, 0.2f);
                DrawRect(new Rect(p.x - 3f, p.y - 3f, 6f, 6f), color);
            }

            // Units as dots
            var units = match.World.Units;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (!u.IsAlive)
                    continue;
                bool own = u.Owner == local;
                if (!own && _fog != null && !_fog.IsWorldVisible(u.X, u.Z))
                    continue;
                Vector2 p = WorldToMinimap(u.X, u.Z, mapRect, half);
                var color = own ? Color.white : new Color(0.95f, 0.25f, 0.2f);
                DrawRect(new Rect(p.x - 2f, p.y - 2f, 4f, 4f), color);
            }

            // Click to pan
            var e = Event.current;
            if (e != null && e.type == EventType.MouseDown && e.button == 0 && mapRect.Contains(e.mousePosition))
            {
                float nx = (e.mousePosition.x - mapRect.x) / mapRect.width;
                float nz = 1f - (e.mousePosition.y - mapRect.y) / mapRect.height;
                float wx = Mathf.Lerp(-half, half, nx);
                float wz = Mathf.Lerp(-half, half, nz);
                if (_cameraRig == null)
                    _cameraRig = FindFirstObjectByType<RtsCameraRig>();
                if (_cameraRig != null)
                    _cameraRig.FocusOn(wx, wz);
                e.Use();
            }
        }

        private static Vector2 WorldToMinimap(float x, float z, Rect mapRect, float half)
        {
            float nx = Mathf.InverseLerp(-half, half, x);
            float nz = Mathf.InverseLerp(-half, half, z);
            return new Vector2(
                mapRect.x + nx * mapRect.width,
                mapRect.y + (1f - nz) * mapRect.height);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void DrawRectBorder(Rect rect, float thickness, Color color)
        {
            DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        private static void DrawCircle(Vector2 center, float radius, Color color)
        {
            // Approximate with a filled square for simplicity / perf.
            DrawRect(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), color);
        }
    }
}
