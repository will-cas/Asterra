using Asterra.Core;
using Asterra.Gameplay.Content;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// OnGUI minimap: terrain stamp from the active map definition, units/buildings/resources,
    /// camera focus; click to pan. World mapping matches <see cref="MapPreviewBuilder"/>.
    /// </summary>
    public sealed class MinimapPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;
        [SerializeField] private float mapSize = 180f;
        [SerializeField] private float margin = 12f;

        private RtsCameraRig _cameraRig;
        private FogOfWarPresenter _fog;
        private Texture2D _terrain;
        private string _terrainMapKey;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
        }

        private void OnDestroy()
        {
            DestroyTerrain();
        }

        private void OnGUI()
        {
            if (match == null || match.World == null || match.Session == null)
                return;
            if (_cameraRig == null)
                _cameraRig = FindFirstObjectByType<RtsCameraRig>();
            if (_fog == null)
                _fog = FindFirstObjectByType<FogOfWarPresenter>();

            EnsureTerrainStamp();

            float size = mapSize * HudStyle.Scale;
            float m = margin * HudStyle.Scale;
            Rect mapRect = new Rect(Screen.width - size - m, Screen.height - size - m, size, size);
            HudClickBlocker.Block(mapRect);

            HudStyle.DrawFrame(
                mapRect,
                new Color(0.05f, 0.07f, 0.08f, 0.98f),
                new Color(0.45f, 0.55f, 0.4f, 0.95f),
                2f);

            var texRect = new Rect(mapRect.x + 3f, mapRect.y + 3f, mapRect.width - 6f, mapRect.height - 6f);
            if (_terrain != null)
                GUI.DrawTexture(texRect, _terrain, ScaleMode.StretchToFill);
            else
                DrawRect(texRect, new Color(0.18f, 0.28f, 0.16f, 0.95f));

            var local = match.Session.LocalPlayer;

            // Territory circles
            var territories = match.World.Territories;
            for (int i = 0; i < territories.Count; i++)
            {
                var t = territories[i];
                MapPreviewBuilder.WorldToPreviewGui(texRect, t.X, t.Z, out float cx, out float cy);
                float half = MapPreviewBuilder.Half;
                float r = (t.Radius / (half * 2f)) * texRect.width;
                Color fill;
                if (t.State == TerritoryState.Contested)
                    fill = new Color(0.95f, 0.75f, 0.2f, 0.4f);
                else if (t.State == TerritoryState.Controlled && t.HasController && t.Controller == local)
                    fill = new Color(0.25f, 0.75f, 0.4f, 0.4f);
                else if (t.State == TerritoryState.Controlled && t.HasController)
                    fill = new Color(0.9f, 0.25f, 0.2f, 0.4f);
                else
                    fill = new Color(0.45f, 0.55f, 0.7f, 0.28f);

                DrawCircle(new Vector2(cx, cy), r, fill);
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
                    MapPreviewBuilder.WorldToPreviewGui(texRect, r.X, r.Z, out float px, out float py);
                    var color = r.Type == ResourceType.Gold
                        ? new Color(0.95f, 0.82f, 0.2f)
                        : new Color(0.55f, 0.35f, 0.18f);
                    DrawRect(new Rect(px - 2f, py - 2f, 4f, 4f), color);
                }
            }

            // Buildings
            var buildings = match.World.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                if (b.State == BuildingState.Destroyed)
                    continue;
                bool own = b.Owner == local;
                if (!own && _fog != null && !_fog.IsWorldVisible(b.X, b.Z))
                    continue;
                MapPreviewBuilder.WorldToPreviewGui(texRect, b.X, b.Z, out float px, out float py);
                var color = own ? Color.white : new Color(0.9f, 0.2f, 0.2f);
                float s = FactionDefaultContent.IsKeepBuildingId(b.DefinitionId) ? 7f : 5f;
                DrawRect(new Rect(px - s * 0.5f, py - s * 0.5f, s, s), color);
            }

            // Units
            var units = match.World.Units;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (!u.IsAlive)
                    continue;
                bool own = u.Owner == local;
                if (!own && _fog != null && !_fog.IsWorldVisible(u.X, u.Z))
                    continue;
                MapPreviewBuilder.WorldToPreviewGui(texRect, u.X, u.Z, out float px, out float py);
                var color = own ? Color.white : new Color(0.95f, 0.25f, 0.2f);
                DrawRect(new Rect(px - 2f, py - 2f, 4f, 4f), color);
            }

            DrawCameraFocus(texRect, mapRect);

            var e = Event.current;
            if (e != null && e.type == EventType.MouseDown && e.button == 0 && mapRect.Contains(e.mousePosition))
            {
                float half = MapPreviewBuilder.Half;
                float nx = (e.mousePosition.x - texRect.x) / texRect.width;
                float nz = 1f - (e.mousePosition.y - texRect.y) / texRect.height;
                float wx = Mathf.Lerp(-half, half, nx);
                float wz = Mathf.Lerp(-half, half, nz);
                if (_cameraRig == null)
                    _cameraRig = FindFirstObjectByType<RtsCameraRig>();
                if (_cameraRig != null)
                    _cameraRig.FocusOn(wx, wz);
                e.Use();
            }
        }

        private void EnsureTerrainStamp()
        {
            string key = match != null ? match.MapKey : null;
            if (string.IsNullOrEmpty(key))
                key = MapCatalog.BlackridgePassId;
            if (_terrain != null && _terrainMapKey == key)
                return;

            DestroyTerrain();
            _terrain = MapPreviewBuilder.Build(key);
            _terrainMapKey = key;
        }

        private void DestroyTerrain()
        {
            if (_terrain == null)
                return;
            Destroy(_terrain);
            _terrain = null;
            _terrainMapKey = null;
        }

        private void DrawCameraFocus(Rect texRect, Rect mapRect)
        {
            if (_cameraRig == null)
                return;

            _cameraRig.GetFocusXZ(out float fx, out float fz);
            MapPreviewBuilder.WorldToPreviewGui(texRect, fx, fz, out float cx, out float cy);
            var center = new Vector2(cx, cy);

            float half = MapPreviewBuilder.Half;
            float height = Mathf.Max(40f, _cameraRig.CameraHeight);
            float viewHalfWorld = Mathf.Clamp(height * 0.42f, 55f, 220f);
            float halfPxX = (viewHalfWorld / (half * 2f)) * texRect.width;
            float halfPxY = (viewHalfWorld * 0.75f / (half * 2f)) * texRect.height;

            Rect viewRect = new Rect(
                center.x - halfPxX,
                center.y - halfPxY,
                halfPxX * 2f,
                halfPxY * 2f);
            viewRect = ClampRectTo(viewRect, texRect);

            DrawRect(viewRect, new Color(0.35f, 0.9f, 1f, 0.12f));
            DrawRectBorder(viewRect, 2f, new Color(0.45f, 0.95f, 1f, 0.95f));

            const float arm = 7f;
            const float thick = 2f;
            var cross = new Color(1f, 0.95f, 0.35f, 1f);
            DrawRect(new Rect(center.x - arm, center.y - thick * 0.5f, arm * 2f, thick), cross);
            DrawRect(new Rect(center.x - thick * 0.5f, center.y - arm, thick, arm * 2f), cross);
            DrawRect(new Rect(center.x - 3f, center.y - 3f, 6f, 6f), new Color(1f, 0.85f, 0.15f, 0.95f));
        }

        private static Rect ClampRectTo(Rect inner, Rect bounds)
        {
            float xMin = Mathf.Max(inner.xMin, bounds.xMin);
            float yMin = Mathf.Max(inner.yMin, bounds.yMin);
            float xMax = Mathf.Min(inner.xMax, bounds.xMax);
            float yMax = Mathf.Min(inner.yMax, bounds.yMax);
            if (xMax <= xMin || yMax <= yMin)
                return new Rect(bounds.center.x - 4f, bounds.center.y - 4f, 8f, 8f);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
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
            DrawRect(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), color);
        }
    }
}
