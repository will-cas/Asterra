using Asterra.Gameplay.Player;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Procedural RTS cursors: select / move / attack / build / invalid / train.</summary>
    public sealed class RtsCursorController : MonoBehaviour
    {
        [SerializeField] private LocalOrderController orders;
        [SerializeField] private int size = 32;

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

            _select = MakeSelect();
            _move = MakeMove();
            _attack = MakeAttack();
            _build = MakeBuild();
            _invalid = MakeInvalid();
            _train = MakeTrain();
            _gather = MakeGather();
            Apply(OrderCursorMode.Select);
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
                    hotspot = new Vector2(size * 0.5f, size * 0.5f);
                    break;
                case OrderCursorMode.Attack:
                    tex = _attack;
                    hotspot = new Vector2(size * 0.5f, size * 0.5f);
                    break;
                case OrderCursorMode.Build:
                    tex = _build;
                    hotspot = new Vector2(size * 0.5f, size * 0.85f);
                    break;
                case OrderCursorMode.Invalid:
                    tex = _invalid;
                    hotspot = new Vector2(size * 0.5f, size * 0.5f);
                    break;
                case OrderCursorMode.Train:
                    tex = _train;
                    hotspot = new Vector2(4f, 4f);
                    break;
                case OrderCursorMode.Gather:
                    tex = _gather;
                    hotspot = new Vector2(size * 0.5f, size * 0.5f);
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

            Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
        }

        private Texture2D MakeSelect()
        {
            var tex = Blank();
            // Arrow pointer
            for (int y = 2; y < 18; y++)
            {
                for (int x = 2; x <= 2 + (y - 2) / 2; x++)
                    Plot(tex, x, size - 1 - y, Color.white);
            }

            Outline(tex);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeMove()
        {
            var tex = Blank();
            int c = size / 2;
            // Four-way arrows
            DrawLine(tex, c, 4, c, size - 5, new Color(0.35f, 0.95f, 0.45f));
            DrawLine(tex, 4, c, size - 5, c, new Color(0.35f, 0.95f, 0.45f));
            FillTri(tex, c, 3, c - 4, 10, c + 4, 10, new Color(0.35f, 0.95f, 0.45f));
            FillTri(tex, c, size - 4, c - 4, size - 11, c + 4, size - 11, new Color(0.35f, 0.95f, 0.45f));
            FillTri(tex, 3, c, 10, c - 4, 10, c + 4, new Color(0.35f, 0.95f, 0.45f));
            FillTri(tex, size - 4, c, size - 11, c - 4, size - 11, c + 4, new Color(0.35f, 0.95f, 0.45f));
            tex.Apply();
            return tex;
        }

        private Texture2D MakeAttack()
        {
            var tex = Blank();
            int c = size / 2;
            var red = new Color(0.95f, 0.25f, 0.2f);
            DrawLine(tex, 6, 6, size - 7, size - 7, red);
            DrawLine(tex, size - 7, 6, 6, size - 7, red);
            DrawCircle(tex, c, c, 10, red);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeBuild()
        {
            var tex = Blank();
            var green = new Color(0.3f, 0.9f, 0.45f);
            // Hammer-ish
            DrawLine(tex, 8, size - 10, size - 10, 8, green);
            for (int i = 0; i < 6; i++)
                DrawLine(tex, 8 + i, size - 14, 14 + i, size - 8, green);
            DrawRect(tex, size - 14, 6, 8, 6, green);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeInvalid()
        {
            var tex = Blank();
            var red = new Color(0.95f, 0.2f, 0.2f);
            int c = size / 2;
            DrawCircle(tex, c, c, 11, red);
            DrawLine(tex, 8, 8, size - 9, size - 9, red);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeTrain()
        {
            var tex = Blank();
            var gold = new Color(0.95f, 0.8f, 0.25f);
            // Plus over house
            DrawRect(tex, 8, 14, 16, 12, gold);
            FillTri(tex, 16, 6, 6, 14, 26, 14, gold);
            DrawLine(tex, 16, 18, 16, 26, Color.white);
            DrawLine(tex, 12, 22, 20, 22, Color.white);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeGather()
        {
            var tex = Blank();
            var amber = new Color(0.95f, 0.75f, 0.2f);
            // Pickaxe-ish
            DrawLine(tex, 8, size - 8, size - 10, 8, amber);
            DrawRect(tex, size - 16, 6, 10, 8, amber);
            DrawCircle(tex, 10, size - 10, 4, amber);
            tex.Apply();
            return tex;
        }

        private Texture2D Blank()
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);
            return tex;
        }

        private static void Plot(Texture2D tex, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
                return;
            tex.SetPixel(x, y, c);
        }

        private static void Outline(Texture2D tex)
        {
            // Soft black outline around opaque pixels.
            for (int y = 1; y < tex.height - 1; y++)
            for (int x = 1; x < tex.width - 1; x++)
            {
                if (tex.GetPixel(x, y).a > 0.5f)
                    continue;
                bool near = tex.GetPixel(x + 1, y).a > 0.5f
                            || tex.GetPixel(x - 1, y).a > 0.5f
                            || tex.GetPixel(x, y + 1).a > 0.5f
                            || tex.GetPixel(x, y - 1).a > 0.5f;
                if (near)
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0.85f));
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                Plot(tex, x0, y0, c);
                Plot(tex, x0 + 1, y0, c);
                Plot(tex, x0, y0 + 1, c);
                if (x0 == x1 && y0 == y1)
                    break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            for (int a = 0; a < 360; a += 3)
            {
                float rad = a * Mathf.Deg2Rad;
                int x = cx + Mathf.RoundToInt(Mathf.Cos(rad) * r);
                int y = cy + Mathf.RoundToInt(Mathf.Sin(rad) * r);
                Plot(tex, x, y, c);
            }
        }

        private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                Plot(tex, xx, yy, c);
        }

        private static void FillTri(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color c)
        {
            int minX = Mathf.Min(x0, Mathf.Min(x1, x2));
            int maxX = Mathf.Max(x0, Mathf.Max(x1, x2));
            int minY = Mathf.Min(y0, Mathf.Min(y1, y2));
            int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                if (PointInTri(x, y, x0, y0, x1, y1, x2, y2))
                    Plot(tex, x, y, c);
            }
        }

        private static bool PointInTri(int px, int py, int x0, int y0, int x1, int y1, int x2, int y2)
        {
            float d1 = Sign(px, py, x0, y0, x1, y1);
            float d2 = Sign(px, py, x1, y1, x2, y2);
            float d3 = Sign(px, py, x2, y2, x0, y0);
            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private static float Sign(int px, int py, int x1, int y1, int x2, int y2)
        {
            return (px - x2) * (y1 - y2) - (x1 - x2) * (py - y2);
        }
    }
}
