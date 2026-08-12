using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Clickable view bound to a sim entity.</summary>
    public sealed class EntityView : MonoBehaviour
    {
        public EntityId Id { get; private set; }
        public bool IsUnit { get; private set; }
        public PlayerId Owner { get; private set; }
        public string DefinitionId { get; private set; }

        private Transform _selectionRing;
        private Renderer _renderer;

        public void Initialize(EntityId id, bool isUnit, PlayerId owner, string definitionId, byte factionIndex)
        {
            Id = id;
            IsUnit = isUnit;
            Owner = owner;
            DefinitionId = definitionId;

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

            var col = gameObject.GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                if (filter.sharedMesh != null)
                    box.center = filter.sharedMesh.bounds.center;
                if (filter.sharedMesh != null)
                    box.size = filter.sharedMesh.bounds.size;
            }

            EnsureSelectionRing(isUnit);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionRing != null)
                _selectionRing.gameObject.SetActive(selected);
        }

        private void EnsureSelectionRing(bool isUnit)
        {
            if (_selectionRing != null)
                return;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.name = "SelectionRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            float scale = isUnit ? 1.6f : 7f;
            ring.transform.localScale = new Vector3(scale, 0.05f, scale);
            var rend = ring.GetComponent<Renderer>();
            rend.sharedMaterial = CreateColorMaterial(new Color(1f, 0.85f, 0.2f, 0.85f));
            _selectionRing = ring.transform;
        }

        private static Material CreateColorMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Diffuse");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            return mat;
        }
    }
}
