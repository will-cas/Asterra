using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Clickable view bound to a sim entity.</summary>
    public sealed class EntityView : MonoBehaviour
    {
        public const float UnitVisualScale = 8f;
        public const float BuildingVisualScale = 2.5f;

        public SimEntityId Id { get; private set; }
        public bool IsUnit { get; private set; }
        public PlayerId Owner { get; private set; }
        public string DefinitionId { get; private set; }
        public bool IsRevealed { get; private set; } = true;

        private Transform _selectionRing;
        private Renderer _renderer;
        private Collider _pickCollider;

        public void Initialize(SimEntityId id, bool isUnit, PlayerId owner, string definitionId, byte factionIndex)
        {
            Id = id;
            IsUnit = isUnit;
            Owner = owner;
            DefinitionId = definitionId;

            float visualScale = isUnit ? UnitVisualScale : BuildingVisualScale;
            transform.localScale = Vector3.one * visualScale;

            var filter = gameObject.GetComponent<MeshFilter>();
            if (filter == null)
                filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = isUnit
                ? AsterraMeshLibrary.GetUnitMesh(definitionId)
                : AsterraMeshLibrary.GetBuildingMesh(definitionId);

            _renderer = gameObject.GetComponent<MeshRenderer>();
            if (_renderer == null)
                _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = CreateColorMaterial(AsterraMeshLibrary.FactionColor(factionIndex));

            EnsurePickCollider(isUnit, filter.sharedMesh);
            EnsureSelectionRing(isUnit);
            SetSelected(false);
            SetRevealed(true);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionRing != null)
                _selectionRing.gameObject.SetActive(selected && IsRevealed);
        }

        public void SetRevealed(bool revealed)
        {
            IsRevealed = revealed;
            if (_renderer != null)
                _renderer.enabled = revealed;
            if (_pickCollider != null)
                _pickCollider.enabled = revealed;
            if (!revealed && _selectionRing != null)
                _selectionRing.gameObject.SetActive(false);
        }

        private void EnsurePickCollider(bool isUnit, Mesh mesh)
        {
            var existing = gameObject.GetComponent<Collider>();
            if (existing != null)
                Object.Destroy(existing);

            // Generous local pick volume so troops stay clickable from high RTS cameras.
            var sphere = gameObject.AddComponent<SphereCollider>();
            if (isUnit)
            {
                sphere.center = new Vector3(0f, 0.8f, 0f);
                sphere.radius = 1.6f;
            }
            else
            {
                float height = mesh != null ? mesh.bounds.size.y : 6f;
                float radius = mesh != null
                    ? Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z, 3f)
                    : 4f;
                sphere.center = new Vector3(0f, height * 0.35f, 0f);
                sphere.radius = radius * 1.15f;
            }

            _pickCollider = sphere;
        }

        private void EnsureSelectionRing(bool isUnit)
        {
            if (_selectionRing != null)
                return;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.name = "SelectionRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            float scale = isUnit ? 2.4f : 5.5f;
            ring.transform.localScale = new Vector3(scale, 0.04f, scale);
            var rend = ring.GetComponent<Renderer>();
            rend.sharedMaterial = CreateColorMaterial(new Color(1f, 0.85f, 0.2f, 0.9f));
            _selectionRing = ring.transform;
        }

        private static Material CreateColorMaterial(Color color)
        {
            var shader = Shader.Find("Asterra/UnlitColor");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Hidden/InternalColored");

            if (shader == null)
            {
                Debug.LogError("[Asterra] No usable color shader found.");
                return new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var mat = new Material(shader);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}
