using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Playable ground plus black void past the border so map edges are obvious.
    /// Void must NOT cover the playable basin — water sits below y=0 and would read as a black hole.
    /// </summary>
    public static class MapBorderVisual
    {
        public static void Ensure(Transform parent)
        {
            var existing = GameObject.Find("AsterraMapRoot");
            if (existing != null)
            {
                RepairVoid(existing.transform);
                return;
            }

            var root = new GameObject("AsterraMapRoot");
            root.transform.SetParent(parent, false);

            CreateGround(root.transform);
            CreateVoid(root.transform);
            CreateBorderFrame(root.transform);
        }

        /// <summary>
        /// Older builds placed one black plane under the whole map; replace with outer-only void.
        /// </summary>
        private static void RepairVoid(Transform root)
        {
            var legacy = root.Find("AsterraVoid");
            if (legacy != null)
                Object.Destroy(legacy.gameObject);

            if (root.Find("AsterraVoidNorth") != null)
                return;

            CreateVoid(root);
        }

        private static void CreateGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "AsterraGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(MapBounds.PlayableSize / 10f, 1f, MapBounds.PlayableSize / 10f);
            ground.transform.position = Vector3.zero;
            var groundCol = ground.GetComponent<Collider>();
            if (groundCol != null)
                Object.DestroyImmediate(groundCol);
            ApplyColor(ground.GetComponent<Renderer>(), new Color(0.28f, 0.36f, 0.22f));
        }

        private static void CreateVoid(Transform parent)
        {
            float play = MapBounds.PlayableHalfExtent;
            float outer = MapBounds.VoidHalfExtent;
            float y = -1.5f;
            Color voidColor = new Color(0.01f, 0.01f, 0.015f);

            // Four slabs outside the playable square — never under rivers/terrain.
            float mid = (play + outer) * 0.5f;
            float ring = outer - play;
            float span = outer * 2f;

            CreateVoidSlab(parent, "AsterraVoidNorth", new Vector3(0f, y, mid), new Vector3(span, 1f, ring), voidColor);
            CreateVoidSlab(parent, "AsterraVoidSouth", new Vector3(0f, y, -mid), new Vector3(span, 1f, ring), voidColor);
            CreateVoidSlab(parent, "AsterraVoidEast", new Vector3(mid, y, 0f), new Vector3(ring, 1f, play * 2f), voidColor);
            CreateVoidSlab(parent, "AsterraVoidWest", new Vector3(-mid, y, 0f), new Vector3(ring, 1f, play * 2f), voidColor);
        }

        private static void CreateVoidSlab(Transform parent, string name, Vector3 pos, Vector3 worldSize, Color color)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(slab.GetComponent<Collider>());
            slab.name = name;
            slab.transform.SetParent(parent, false);
            slab.transform.position = pos;
            slab.transform.localScale = worldSize;
            ApplyColor(slab.GetComponent<Renderer>(), color);
        }

        private static void CreateBorderFrame(Transform parent)
        {
            float half = MapBounds.PlayableHalfExtent;
            float t = MapBounds.BorderThickness;
            float h = 1.2f;
            Color border = new Color(0.08f, 0.09f, 0.11f);
            Color rim = new Color(0.55f, 0.52f, 0.42f);

            CreateWall(parent, "BorderNorth", new Vector3(0f, h * 0.5f, half + t * 0.5f), new Vector3(half * 2f + t * 2f, h, t), border);
            CreateWall(parent, "BorderSouth", new Vector3(0f, h * 0.5f, -half - t * 0.5f), new Vector3(half * 2f + t * 2f, h, t), border);
            CreateWall(parent, "BorderEast", new Vector3(half + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, half * 2f), border);
            CreateWall(parent, "BorderWest", new Vector3(-half - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, half * 2f), border);

            float rimT = 2.2f;
            float rimY = 0.15f;
            CreateWall(parent, "RimNorth", new Vector3(0f, rimY, half - rimT * 0.5f), new Vector3(half * 2f, 0.3f, rimT), rim);
            CreateWall(parent, "RimSouth", new Vector3(0f, rimY, -half + rimT * 0.5f), new Vector3(half * 2f, 0.3f, rimT), rim);
            CreateWall(parent, "RimEast", new Vector3(half - rimT * 0.5f, rimY, 0f), new Vector3(rimT, 0.3f, half * 2f - rimT * 2f), rim);
            CreateWall(parent, "RimWest", new Vector3(-half + rimT * 0.5f, rimY, 0f), new Vector3(rimT, 0.3f, half * 2f - rimT * 2f), rim);
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(wall.GetComponent<Collider>());
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            ApplyColor(wall.GetComponent<Renderer>(), color);
        }

        private static void ApplyColor(Renderer rend, Color color)
        {
            if (rend == null)
                return;
            var shader = Shader.Find("Asterra/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            rend.sharedMaterial = mat;
        }
    }
}
