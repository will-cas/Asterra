using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>
    /// Playable ground plus black void past the border so map edges are obvious.
    /// </summary>
    public static class MapBorderVisual
    {
        public static void Ensure(Transform parent)
        {
            if (GameObject.Find("AsterraMapRoot") != null)
                return;

            var root = new GameObject("AsterraMapRoot");
            root.transform.SetParent(parent, false);

            CreateGround(root.transform);
            CreateVoid(root.transform);
            CreateBorderFrame(root.transform);
        }

        private static void CreateGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "AsterraGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(MapBounds.PlayableSize / 10f, 1f, MapBounds.PlayableSize / 10f);
            ground.transform.position = Vector3.zero;
            ApplyColor(ground.GetComponent<Renderer>(), new Color(0.28f, 0.36f, 0.22f));
        }

        private static void CreateVoid(Transform parent)
        {
            var voidPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Object.Destroy(voidPlane.GetComponent<Collider>());
            voidPlane.name = "AsterraVoid";
            voidPlane.transform.SetParent(parent, false);
            float voidSize = MapBounds.VoidHalfExtent * 2f;
            voidPlane.transform.localScale = new Vector3(voidSize / 10f, 1f, voidSize / 10f);
            voidPlane.transform.position = new Vector3(0f, -0.5f, 0f);
            ApplyColor(voidPlane.GetComponent<Renderer>(), new Color(0.01f, 0.01f, 0.015f));
        }

        private static void CreateBorderFrame(Transform parent)
        {
            float half = MapBounds.PlayableHalfExtent;
            float t = MapBounds.BorderThickness;
            float h = 1.2f;
            Color border = new Color(0.08f, 0.09f, 0.11f);
            Color rim = new Color(0.55f, 0.52f, 0.42f);

            // Outer black walls just past the playable edge.
            CreateWall(parent, "BorderNorth", new Vector3(0f, h * 0.5f, half + t * 0.5f), new Vector3(half * 2f + t * 2f, h, t), border);
            CreateWall(parent, "BorderSouth", new Vector3(0f, h * 0.5f, -half - t * 0.5f), new Vector3(half * 2f + t * 2f, h, t), border);
            CreateWall(parent, "BorderEast", new Vector3(half + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, half * 2f), border);
            CreateWall(parent, "BorderWest", new Vector3(-half - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, half * 2f), border);

            // Thin lit rim on the playable side so the edge reads clearly.
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
