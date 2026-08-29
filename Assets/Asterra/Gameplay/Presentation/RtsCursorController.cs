using Asterra.Gameplay.Player;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>RTS cursors loaded from Resources/Asterra/Cursors (Kenney CC0).</summary>
    public sealed class RtsCursorController : MonoBehaviour
    {
        [SerializeField] private LocalOrderController orders;

        private Texture2D _select;
        private Texture2D _move;
        private Texture2D _attack;
        private Texture2D _build;
        private Texture2D _invalid;
        private Texture2D _train;
        private Texture2D _gather;
        private OrderCursorMode _last = (OrderCursorMode)255;

        private void Awake()
        {
            if (orders == null)
                orders = FindFirstObjectByType<LocalOrderController>();

            _select = LoadCursor("select");
            _move = LoadCursor("move");
            _attack = LoadCursor("attack");
            _build = LoadCursor("build");
            _invalid = LoadCursor("invalid");
            _train = LoadCursor("train");
            _gather = LoadCursor("gather");
            Apply(OrderCursorMode.Select);
        }

        private static Texture2D LoadCursor(string name)
        {
            var src = Resources.Load<Texture2D>("Asterra/Cursors/" + name);
            if (src == null)
            {
                Debug.LogError($"[Asterra] Missing cursor Resources/Asterra/Cursors/{name}");
                return null;
            }

            // Cursor.SetCursor requires a readable Texture2D.
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "cursor_" + name,
            };
            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            tex.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        private void LateUpdate()
        {
            if (orders == null)
                orders = FindFirstObjectByType<LocalOrderController>();
            if (orders == null)
                return;

            var mode = orders.CurrentCursorMode;
            if (mode == _last)
                return;
            Apply(mode);
        }

        private void OnDestroy()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void Apply(OrderCursorMode mode)
        {
            _last = mode;
            Texture2D tex;
            Vector2 hotspot;
            switch (mode)
            {
                case OrderCursorMode.Move:
                    tex = _move;
                    hotspot = HotspotCenter(tex);
                    break;
                case OrderCursorMode.Attack:
                    tex = _attack;
                    hotspot = HotspotCenter(tex);
                    break;
                case OrderCursorMode.Build:
                    tex = _build;
                    hotspot = HotspotBottom(tex);
                    break;
                case OrderCursorMode.Invalid:
                    tex = _invalid;
                    hotspot = HotspotCenter(tex);
                    break;
                case OrderCursorMode.Train:
                    tex = _train;
                    hotspot = new Vector2(4f, 4f);
                    break;
                case OrderCursorMode.Gather:
                    tex = _gather;
                    hotspot = HotspotCenter(tex);
                    break;
                case OrderCursorMode.Select:
                    tex = _select;
                    hotspot = new Vector2(4f, 4f);
                    break;
                default:
                {
                    OrderCursorMode unreachable = mode;
                    tex = _select;
                    hotspot = new Vector2(4f, 4f);
                    _ = unreachable;
                    break;
                }
            }

            if (tex != null)
                Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
        }

        private static Vector2 HotspotCenter(Texture2D tex)
        {
            if (tex == null)
                return Vector2.zero;
            return new Vector2(tex.width * 0.5f, tex.height * 0.5f);
        }

        private static Vector2 HotspotBottom(Texture2D tex)
        {
            if (tex == null)
                return Vector2.zero;
            return new Vector2(tex.width * 0.5f, tex.height * 0.85f);
        }
    }
}
