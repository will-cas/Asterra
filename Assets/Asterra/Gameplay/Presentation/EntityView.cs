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
        private Color _baseColor = Color.gray;
        private float _hitFlashUntil;
        private Transform _hpRoot;
        private Transform _hpFill;
        private float _health = 1f;
        private float _maxHealth = 1f;

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
            _baseColor = AsterraMeshLibrary.FactionColor(factionIndex);
            _renderer.sharedMaterial = CreateColorMaterial(_baseColor);

            EnsurePickCollider(isUnit, filter.sharedMesh);
            EnsureSelectionRing(isUnit);
            EnsureHealthBar(isUnit);
            SetSelected(false);
            SetRevealed(true);
            SetHealth(1f, 1f);
        }

        public void SetHealth(float health, float max)
        {
            _health = Mathf.Max(0f, health);
            _maxHealth = Mathf.Max(0.01f, max);
            float ratio = Mathf.Clamp01(_health / _maxHealth);

            if (_hpRoot != null)
                _hpRoot.gameObject.SetActive(IsRevealed && ratio < 0.999f);

            if (_hpFill != null)
            {
                _hpFill.localScale = new Vector3(Mathf.Max(0.02f, ratio), 1f, 1f);
                _hpFill.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, -0.01f);
            }
        }

        public void SetHitFlash()
        {
            _hitFlashUntil = Time.time + 0.18f;
            ApplyBodyColor(new Color(1f, 0.25f, 0.2f));
        }

        private void LateUpdate()
        {
            if (_hitFlashUntil <= 0f)
                return;
            if (Time.time < _hitFlashUntil)
                return;
            _hitFlashUntil = 0f;
            ApplyBodyColor(_baseColor);
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
            if (_hpRoot != null)
            {
                float ratio = _maxHealth > 0f ? _health / _maxHealth : 1f;
                _hpRoot.gameObject.SetActive(revealed && ratio < 0.999f);
            }
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

        private void EnsureHealthBar(bool isUnit)
        {
            if (_hpRoot != null)
                return;

            float y = isUnit ? 2.2f : 9.5f;
            float width = isUnit ? 1.4f : 3.2f;

            var root = new GameObject("HealthBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, y, 0f);
            root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            root.transform.localScale = new Vector3(width, 0.16f, 1f);
            _hpRoot = root.transform;

            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(bg.GetComponent<Collider>());
            bg.name = "HpBg";
            bg.transform.SetParent(_hpRoot, false);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = Vector3.one;
            bg.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.08f, 0.08f, 0.1f, 0.85f));

            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(fill.GetComponent<Collider>());
            fill.name = "HpFill";
            fill.transform.SetParent(_hpRoot, false);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fill.transform.localScale = Vector3.one;
            fill.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.25f, 0.85f, 0.35f, 0.95f));
            _hpFill = fill.transform;
        }

        private void ApplyBodyColor(Color color)
        {
            if (_renderer == null || _renderer.sharedMaterial == null)
                return;
            SetMatColor(_renderer.sharedMaterial, color);
        }

        private static void SetMatColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
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
            SetMatColor(mat, color);
            return mat;
        }
    }
}
