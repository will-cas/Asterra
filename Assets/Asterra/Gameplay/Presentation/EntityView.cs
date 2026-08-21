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
        private Transform _selectionRingInner;
        private Renderer _renderer;
        private Renderer _teamBandRenderer;
        private Collider _pickCollider;
        private Color _baseColor = Color.gray;
        private Color _factionColor = Color.gray;
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
            _factionColor = AsterraMeshLibrary.FactionColor(factionIndex);
            if (isUnit)
            {
                var role = AsterraMeshLibrary.InferRole(definitionId);
                visualScale *= AsterraMeshLibrary.RoleScaleMultiplier(role);
                Color accent = AsterraMeshLibrary.RoleAccent(role);
                _baseColor = Color.Lerp(_factionColor, accent, 0.35f);
            }
            else
            {
                _baseColor = _factionColor;
            }

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
            _renderer.sharedMaterial = CreateColorMaterial(_baseColor);

            EnsurePickCollider(isUnit, filter.sharedMesh);
            EnsureSelectionRing(isUnit);
            if (isUnit)
                EnsureTeamBand();
            EnsureHealthBar(isUnit);
            SetSelected(false);
            SetRevealed(true);
            SetHealth(1f, 1f);
        }

        /// <summary>
        /// Orient / stretch wall segment from sim neighbour bits (N=1,E=2,S=4,W=8).
        /// </summary>
        public void ApplyWallLinks(byte links)
        {
            if (IsUnit)
                return;
            bool ew = (links & 2) != 0 || (links & 8) != 0;
            bool ns = (links & 1) != 0 || (links & 4) != 0;
            float yaw = 0f;
            float sx = BuildingVisualScale;
            float sy = BuildingVisualScale;
            float sz = BuildingVisualScale;
            if (ew && !ns)
            {
                yaw = 90f;
                sx = BuildingVisualScale * 1.35f;
                sz = BuildingVisualScale * 0.85f;
            }
            else if (ns && !ew)
            {
                yaw = 0f;
                sx = BuildingVisualScale * 0.85f;
                sz = BuildingVisualScale * 1.35f;
            }
            else if (ew && ns)
            {
                sx = BuildingVisualScale * 1.15f;
                sz = BuildingVisualScale * 1.15f;
            }

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            transform.localScale = new Vector3(sx, sy, sz);
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
            if (_selectionRingInner != null)
                _selectionRingInner.gameObject.SetActive(selected && IsRevealed);
        }

        public void SetRevealed(bool revealed)
        {
            IsRevealed = revealed;
            if (_renderer != null)
                _renderer.enabled = revealed;
            if (_teamBandRenderer != null)
                _teamBandRenderer.enabled = revealed;
            if (_pickCollider != null)
                _pickCollider.enabled = revealed;
            if (!revealed)
            {
                if (_selectionRing != null)
                    _selectionRing.gameObject.SetActive(false);
                if (_selectionRingInner != null)
                    _selectionRingInner.gameObject.SetActive(false);
            }

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

            // Outer bright ring + darker inner disc for readable selection at distance.
            float outer = isUnit ? 2.8f : 6.2f;
            float inner = isUnit ? 2.15f : 5.0f;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.name = "SelectionRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ring.transform.localScale = new Vector3(outer, 0.055f, outer);
            ring.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(1f, 0.92f, 0.2f, 0.95f));
            _selectionRing = ring.transform;

            var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(hole.GetComponent<Collider>());
            hole.name = "SelectionRingInner";
            hole.transform.SetParent(transform, false);
            hole.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            hole.transform.localScale = new Vector3(inner, 0.04f, inner);
            hole.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.05f, 0.08f, 0.05f, 0.55f));
            _selectionRingInner = hole.transform;
        }

        private void EnsureTeamBand()
        {
            if (_teamBandRenderer != null)
                return;

            var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(band.GetComponent<Collider>());
            band.name = "TeamBand";
            band.transform.SetParent(transform, false);
            band.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            band.transform.localScale = new Vector3(0.95f, 0.18f, 0.55f);
            _teamBandRenderer = band.GetComponent<Renderer>();
            // Brighter stripe so faction reads on top of role-tinted body.
            Color stripe = Color.Lerp(_factionColor, Color.white, 0.35f);
            stripe.a = 1f;
            _teamBandRenderer.sharedMaterial = CreateColorMaterial(stripe);
        }

        private void EnsureHealthBar(bool isUnit)
        {
            if (_hpRoot != null)
                return;

            float y = isUnit ? 2.35f : 10.5f;
            float width = isUnit ? 1.5f : 3.4f;

            var root = new GameObject("HealthBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, y, 0f);
            root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            root.transform.localScale = new Vector3(width, 0.18f, 1f);
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
