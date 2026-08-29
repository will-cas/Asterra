using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Clickable view bound to a sim entity.</summary>
    public sealed class EntityView : MonoBehaviour
    {
        // Free-art OBJs are ~1.3–1.85 local units tall; these multipliers put them at RTS map size.
        public const float UnitVisualScale = 14f;
        public const float BuildingVisualScale = 18f;
        public const float BuildingCollapseSeconds = 0.42f;

        public SimEntityId Id { get; private set; }
        public bool IsUnit { get; private set; }
        public PlayerId Owner { get; private set; }
        public string DefinitionId { get; private set; }
        public bool IsRevealed { get; private set; } = true;
        /// <summary>True while a building death collapse is playing (delay Destroy).</summary>
        public bool IsCollapsing => !IsUnit && _collapsing;
        public bool CollapseFinished => _collapsing && Time.time >= _collapseUntil;

        private Transform _selectionRing;
        private Transform _selectionRingInner;
        private Transform _selectionRingCore;
        private Renderer _renderer;
        private Renderer[] _troopRenderers;
        private Transform[] _troopHosts;
        private int _squadSize = 1;
        private Renderer _teamBandRenderer;
        private Collider _pickCollider;
        private Color _baseColor = Color.gray;
        private Color _factionColor = Color.gray;
        private float _hitFlashUntil;
        private Transform _hpRoot;
        private Transform _hpFill;
        private float _health = 1f;
        private float _maxHealth = 1f;
        private float _hitBobUntil;
        private float _yaw;
        private Vector3 _lastPos;
        private bool _hasLastPos;
        private Transform _bodyRoot;

        private BuildingState _buildingState = BuildingState.Active;
        private float _buildProgress = 1f;
        private float _completePopUntil;
        private float _collapseUntil;
        private bool _collapsing;
        private float _animPhase;
        private GameObject _scaffold;
        private bool _isWorking;

        public void Initialize(SimEntityId id, bool isUnit, PlayerId owner, string definitionId, byte factionIndex)
        {
            Initialize(id, isUnit, owner, definitionId, factionIndex, squadSize: 0);
        }

        public void Initialize(
            SimEntityId id,
            bool isUnit,
            PlayerId owner,
            string definitionId,
            byte factionIndex,
            int squadSize)
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
                _squadSize = squadSize > 0
                    ? Mathf.Clamp(squadSize, 1, UnitSquadVisual.MaxSquadSize)
                    : UnitSquadVisual.ResolveSquadSize(definitionId);
            }
            else
            {
                _squadSize = 1;
                if (!string.IsNullOrEmpty(definitionId) && definitionId.Contains("turret"))
                    visualScale *= 1.35f;
            }

            transform.localScale = Vector3.one * visualScale;

            // Body root lets us bob/hit-punch without fighting world position sync.
            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            _bodyRoot = body.transform;

            Mesh mesh = isUnit
                ? AsterraMeshLibrary.GetUnitMesh(definitionId)
                : AsterraMeshLibrary.GetBuildingMesh(definitionId);

            _baseColor = AsterraMeshLibrary.FactionBodyColor(factionIndex, isUnit, definitionId);
            BuildTroopMeshes(mesh, isUnit);

            EnsurePickCollider(isUnit, mesh);
            EnsureSelectionRing(isUnit);
            if (isUnit)
                EnsureTeamBand();
            EnsureHealthBar(isUnit);
            SetSelected(false);
            SetRevealed(true);
            SetHealth(1f, 1f);
            _animPhase = (id.Value % 97) * 0.173f;
            if (!isUnit)
                EnsureScaffold();
        }

        private void BuildTroopMeshes(Mesh mesh, bool isUnit)
        {
            int count = isUnit ? Mathf.Max(1, _squadSize) : 1;
            _troopRenderers = new Renderer[count];
            _troopHosts = new Transform[count];
            float troopScale = UnitSquadVisual.TroopLocalScale(count);

            for (int i = 0; i < count; i++)
            {
                Transform host = _bodyRoot;
                if (count > 1)
                {
                    var troop = new GameObject("Troop_" + i);
                    troop.transform.SetParent(_bodyRoot, false);
                    troop.transform.localPosition = UnitSquadVisual.TroopOffset(i, count);
                    troop.transform.localRotation = Quaternion.Euler(0f, (Id.Value * 17 + i * 41) % 50 - 25f, 0f);
                    troop.transform.localScale = Vector3.one * troopScale;
                    host = troop.transform;
                }

                _troopHosts[i] = host;
                var filter = host.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var rend = host.gameObject.AddComponent<MeshRenderer>();
                // Unique material instance per troop so hit-flash does not tint siblings wrong.
                rend.material = CreateColorMaterial(_baseColor);
                _troopRenderers[i] = rend;
            }

            _renderer = _troopRenderers[0];
        }

        /// <summary>Drive construction rise / complete pop from sim BuildProgress + State.</summary>
        public void SetBuildingVisual(BuildingState state, float buildProgress)
        {
            if (IsUnit || _collapsing)
                return;

            bool wasConstructing = _buildingState == BuildingState.Constructing
                                   || _buildingState == BuildingState.Ghost;
            _buildingState = state;
            _buildProgress = Mathf.Clamp01(buildProgress <= 0.001f && state == BuildingState.Active
                ? 1f
                : buildProgress);

            if (wasConstructing && state == BuildingState.Active)
            {
                _completePopUntil = Time.time + 0.38f;
                PlayBuildCompleteFlash();
                if (_bodyRoot != null)
                {
                    _bodyRoot.localPosition = Vector3.zero;
                    _bodyRoot.localRotation = Quaternion.identity;
                    _bodyRoot.localScale = Vector3.one;
                }
            }

            if (_scaffold != null)
                _scaffold.SetActive(state == BuildingState.Constructing || state == BuildingState.Ghost);
        }

        /// <summary>Builder hammer bob while actively constructing a site.</summary>
        public void SetWorking(bool working)
        {
            if (_isWorking == working)
                return;
            _isWorking = working;
            if (!working && _bodyRoot != null && IsUnit)
            {
                _bodyRoot.localPosition = Vector3.zero;
                _bodyRoot.localRotation = Quaternion.identity;
                _bodyRoot.localScale = Vector3.one;
            }
        }

        /// <summary>Start a short collapse animation before the view is destroyed.</summary>
        public void BeginCollapse(float duration = BuildingCollapseSeconds)
        {
            if (IsUnit || _collapsing)
                return;
            _collapsing = true;
            _collapseUntil = Time.time + Mathf.Max(0.12f, duration);
            _hitFlashUntil = Time.time + 0.12f;
            ApplyBodyColor(new Color(0.35f, 0.12f, 0.1f));
            if (_scaffold != null)
                _scaffold.SetActive(false);
            if (_hpRoot != null)
                _hpRoot.gameObject.SetActive(false);
            if (_selectionRing != null)
                _selectionRing.gameObject.SetActive(false);
            if (_selectionRingInner != null)
                _selectionRingInner.gameObject.SetActive(false);
            if (_selectionRingCore != null)
                _selectionRingCore.gameObject.SetActive(false);
        }

        public void PlayBuildCompleteFlash()
        {
            if (IsUnit)
                return;
            _hitFlashUntil = Time.time + 0.22f;
            ApplyBodyColor(Color.Lerp(_baseColor, new Color(0.55f, 1f, 0.95f), 0.65f));
        }

        /// <summary>
        /// Orient / stretch wall segment from sim neighbour bits (N=1,E=2,S=4,W=8)
        /// and/or placement yaw (0/90/180/270).
        /// </summary>
        public void ApplyWallLinks(byte links, float yawDegrees = 0f)
        {
            if (IsUnit)
                return;
            bool ew = (links & 2) != 0 || (links & 8) != 0;
            bool ns = (links & 1) != 0 || (links & 4) != 0;
            float yaw = yawDegrees;
            float sx = BuildingVisualScale * 1.35f;
            float sy = BuildingVisualScale;
            float sz = BuildingVisualScale * 0.85f;
            // Mesh is long on local X. Align that axis with the run of the wall.
            if (ew && !ns)
            {
                // Neighbours east/west → span E–W → yaw 0.
                yaw = 0f;
            }
            else if (ns && !ew)
            {
                // Neighbours north/south → span N–S → yaw 90.
                yaw = 90f;
            }
            else if (ew && ns)
            {
                // Corner / cross: keep placement yaw, slightly squarer footprint.
                sx = BuildingVisualScale * 1.15f;
                sz = BuildingVisualScale * 1.15f;
            }
            else
            {
                // Isolated: honour placement yaw; keep long-on-X (no axis swap).
                yaw = yawDegrees;
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
                var fillRend = _hpFill.GetComponent<Renderer>();
                if (fillRend != null && fillRend.sharedMaterial != null)
                {
                    Color c = ratio > 0.55f
                        ? new Color(0.25f, 0.85f, 0.35f, 0.95f)
                        : ratio > 0.25f
                            ? new Color(0.95f, 0.75f, 0.2f, 0.95f)
                            : new Color(0.95f, 0.25f, 0.2f, 0.95f);
                    SetMatColor(fillRend.sharedMaterial, c);
                }
            }
        }

        private float _powerAuraUntil;
        private byte _equipmentFlags;
        private Transform[] _flameRoots;
        private Renderer[] _flameRenderers;
        private Color _equippedBodyTint = Color.clear;

        public const byte EquipFlagFlameWeapon = 1;
        public const byte EquipFlagReinforcedArmour = 2;

        public void SetPowerAura(float seconds)
        {
            _powerAuraUntil = Time.time + Mathf.Max(0.1f, seconds);
            ApplyBodyColor(Color.Lerp(EffectiveBodyColor(), new Color(0.75f, 0.45f, 1f), 0.55f));
        }

        /// <summary>Persistent equipment cue from sim (flame blades, armour sheen).</summary>
        public void SetEquipmentVisuals(byte flags)
        {
            if (!IsUnit)
                return;
            if (_equipmentFlags == flags)
                return;
            _equipmentFlags = flags;

            bool flame = (flags & EquipFlagFlameWeapon) != 0;
            bool armour = (flags & EquipFlagReinforcedArmour) != 0;
            _equippedBodyTint = armour
                ? Color.Lerp(_baseColor, new Color(0.75f, 0.82f, 0.95f), 0.45f)
                : Color.clear;

            EnsureFlameFx(flame);
            if (_hitFlashUntil <= 0f && Time.time >= _powerAuraUntil)
                ApplyBodyColor(EffectiveBodyColor());
        }

        private Color EffectiveBodyColor()
        {
            if (_equippedBodyTint.a > 0.01f)
                return _equippedBodyTint;
            return _baseColor;
        }

        private void EnsureFlameFx(bool enabled)
        {
            int troopCount = _troopHosts != null && _troopHosts.Length > 0
                ? _troopHosts.Length
                : 1;

            if (!enabled)
            {
                if (_flameRoots != null)
                {
                    for (int i = 0; i < _flameRoots.Length; i++)
                    {
                        if (_flameRoots[i] != null)
                            _flameRoots[i].gameObject.SetActive(false);
                    }
                }

                return;
            }

            if (_flameRoots == null || _flameRoots.Length != troopCount)
            {
                if (_flameRoots != null)
                {
                    for (int i = 0; i < _flameRoots.Length; i++)
                    {
                        if (_flameRoots[i] != null)
                            Destroy(_flameRoots[i].gameObject);
                    }
                }

                _flameRoots = new Transform[troopCount];
                var flameRends = new System.Collections.Generic.List<Renderer>(troopCount * 3);
                for (int t = 0; t < troopCount; t++)
                {
                    Transform parent = _troopHosts != null && t < _troopHosts.Length && _troopHosts[t] != null
                        ? _troopHosts[t]
                        : (_bodyRoot != null ? _bodyRoot : transform);
                    var root = new GameObject("FlameWeapon");
                    root.transform.SetParent(parent, false);
                    // Weapon side of low-poly infantry / cavalry meshes (local to each troop).
                    root.transform.localPosition = new Vector3(0.55f, 0.9f, 0.15f);
                    _flameRoots[t] = root.transform;

                    for (int i = 0; i < 3; i++)
                    {
                        var ember = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Object.Destroy(ember.GetComponent<Collider>());
                        ember.name = "Ember" + i;
                        ember.transform.SetParent(_flameRoots[t], false);
                        ember.transform.localPosition = new Vector3(
                            0.05f * i,
                            0.15f + i * 0.22f,
                            -0.05f * i);
                        ember.transform.localScale = Vector3.one * (0.18f - i * 0.03f);
                        var rend = ember.GetComponent<Renderer>();
                        rend.sharedMaterial = CreateColorMaterial(new Color(1f, 0.45f, 0.08f, 0.95f));
                        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        flameRends.Add(rend);
                    }
                }

                _flameRenderers = flameRends.ToArray();
            }

            for (int i = 0; i < _flameRoots.Length; i++)
            {
                if (_flameRoots[i] != null)
                    _flameRoots[i].gameObject.SetActive(true);
            }
        }

        public void SetHitFlash()
        {
            _hitFlashUntil = Time.time + (IsUnit ? 0.18f : 0.22f);
            _hitBobUntil = Time.time + (IsUnit ? 0.22f : 0.32f);
            ApplyBodyColor(new Color(1f, 0.25f, 0.2f));
        }

        /// <summary>Client sync: face move direction and apply hit bob / building anim.</summary>
        public void SyncPresentation(Vector3 worldPos)
        {
            if (_hasLastPos && IsUnit)
            {
                Vector3 delta = worldPos - _lastPos;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.0004f)
                {
                    _yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                }
            }

            _lastPos = worldPos;
            _hasLastPos = true;
            if (!_collapsing)
                transform.position = worldPos;

            ApplyBodyMotion();

            if (_hpRoot != null && Camera.main != null)
            {
                _hpRoot.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
            }
        }

        private void LateUpdate()
        {
            if (_hitFlashUntil > 0f && Time.time >= _hitFlashUntil)
            {
                _hitFlashUntil = 0f;
                if (!_collapsing)
                {
                    if (Time.time < _powerAuraUntil)
                        ApplyBodyColor(Color.Lerp(EffectiveBodyColor(), new Color(0.75f, 0.45f, 1f), 0.55f));
                    else
                    {
                        Color restore = _buildingState == BuildingState.Constructing || _buildingState == BuildingState.Ghost
                            ? ConstructionTint(_baseColor)
                            : EffectiveBodyColor();
                        ApplyBodyColor(restore);
                    }
                }
            }

            if (_powerAuraUntil > 0f && Time.time >= _powerAuraUntil && _hitFlashUntil <= 0f && !_collapsing)
            {
                _powerAuraUntil = 0f;
                ApplyBodyColor(EffectiveBodyColor());
            }

            AnimateFlameFx();

            // Collapse / idle continue even when the sim no longer syncs this entity.
            if (!IsUnit && _bodyRoot != null && (_collapsing || _buildingState == BuildingState.Active))
                ApplyBodyMotion();
        }

        private void AnimateFlameFx()
        {
            if (_flameRoots == null || _flameRenderers == null)
                return;

            bool anyActive = false;
            for (int r = 0; r < _flameRoots.Length; r++)
            {
                if (_flameRoots[r] != null && _flameRoots[r].gameObject.activeSelf)
                {
                    anyActive = true;
                    break;
                }
            }

            if (!anyActive)
                return;

            float t = Time.time + _animPhase;
            float pulse = 0.85f + Mathf.Sin(t * 14f) * 0.15f;
            for (int r = 0; r < _flameRoots.Length; r++)
            {
                if (_flameRoots[r] != null && _flameRoots[r].gameObject.activeSelf)
                    _flameRoots[r].localScale = Vector3.one * pulse;
            }

            for (int i = 0; i < _flameRenderers.Length; i++)
            {
                var rend = _flameRenderers[i];
                if (rend == null)
                    continue;
                float u = 0.5f + 0.5f * Mathf.Sin(t * (10f + i * 3f) + i);
                Color c = Color.Lerp(
                    new Color(1f, 0.25f, 0.05f, 0.95f),
                    new Color(1f, 0.9f, 0.25f, 0.95f),
                    u);
                SetMatColor(rend.sharedMaterial, c);
                float s = (0.16f - (i % 3) * 0.028f) * (0.75f + u * 0.55f);
                rend.transform.localScale = Vector3.one * s;
            }
        }

        private void ApplyBodyMotion()
        {
            if (_bodyRoot == null)
                return;

            if (IsUnit)
            {
                float ut = Time.time + _animPhase;
                if (_isWorking)
                {
                    float swing = Mathf.Sin(ut * 11f);
                    _bodyRoot.localPosition = new Vector3(0f, Mathf.Abs(swing) * 0.4f, 0f);
                    _bodyRoot.localRotation = Quaternion.Euler(swing * 14f, 0f, swing * -6f);
                    _bodyRoot.localScale = Vector3.one;
                    return;
                }

                float bob = 0f;
                if (Time.time < _hitBobUntil)
                {
                    float u = 1f - ((_hitBobUntil - Time.time) / 0.22f);
                    bob = Mathf.Sin(u * Mathf.PI) * 0.35f;
                }

                _bodyRoot.localPosition = new Vector3(0f, bob, 0f);
                _bodyRoot.localRotation = Quaternion.identity;
                _bodyRoot.localScale = Vector3.one;
                return;
            }

            float t = Time.time;
            Vector3 pos = Vector3.zero;
            Vector3 scale = Vector3.one;
            Quaternion rot = Quaternion.identity;

            if (_collapsing)
            {
                float u = 1f - Mathf.Clamp01((_collapseUntil - t) / BuildingCollapseSeconds);
                float ease = u * u;
                pos = new Vector3(
                    Mathf.Sin((t + _animPhase) * 28f) * 0.15f * (1f - ease),
                    -ease * 1.1f,
                    Mathf.Cos((t + _animPhase) * 22f) * 0.12f * (1f - ease));
                scale = new Vector3(
                    Mathf.Lerp(1f, 1.35f, ease),
                    Mathf.Lerp(1f, 0.15f, ease),
                    Mathf.Lerp(1f, 1.25f, ease));
                rot = Quaternion.Euler(ease * 55f, ease * 25f, ease * -40f);
                ApplyBodyColor(Color.Lerp(new Color(0.45f, 0.15f, 0.1f), new Color(0.12f, 0.08f, 0.07f), ease));
            }
            else if (_buildingState == BuildingState.Constructing || _buildingState == BuildingState.Ghost)
            {
                float p = Mathf.Clamp01(_buildProgress);
                // Rise from the ground: short → full height, slight under-scale in XZ.
                float rise = Mathf.SmoothStep(0.12f, 1f, p);
                scale = new Vector3(
                    Mathf.Lerp(0.72f, 1f, p),
                    rise,
                    Mathf.Lerp(0.72f, 1f, p));
                pos = new Vector3(0f, (rise - 1f) * 0.55f, 0f);
                float wobble = Mathf.Sin((t + _animPhase) * 6f) * 0.8f * (1f - p);
                rot = Quaternion.Euler(0f, wobble, 0f);
                if (_hitFlashUntil <= 0f)
                    ApplyBodyColor(ConstructionTint(_baseColor));
                if (_scaffold != null)
                {
                    float scaffoldPulse = 0.85f + 0.15f * Mathf.Sin((t + _animPhase) * 4f);
                    _scaffold.transform.localScale = new Vector3(1.15f, rise * scaffoldPulse, 1.15f);
                    _scaffold.transform.localPosition = new Vector3(0f, rise * 0.35f, 0f);
                }
            }
            else
            {
                // Idle: very light sway so keeps/towers feel alive.
                float sway = Mathf.Sin((t + _animPhase) * 1.15f) * 0.55f;
                float breath = 1f + Mathf.Sin((t + _animPhase) * 0.7f) * 0.012f;
                rot = Quaternion.Euler(0f, sway, 0f);
                scale = new Vector3(breath, 1f + (breath - 1f) * 0.5f, breath);
                pos = Vector3.zero;

                if (t < _completePopUntil)
                {
                    float u = 1f - ((_completePopUntil - t) / 0.38f);
                    float pop = Mathf.Sin(u * Mathf.PI);
                    scale *= 1f + pop * 0.12f;
                    pos.y = pop * 0.25f;
                }

                if (t < _hitBobUntil)
                {
                    float u = 1f - ((_hitBobUntil - t) / 0.32f);
                    float punch = Mathf.Sin(u * Mathf.PI);
                    pos += new Vector3(
                        Mathf.Sin((t + _animPhase) * 40f) * punch * 0.22f,
                        punch * 0.18f,
                        Mathf.Cos((t + _animPhase) * 36f) * punch * 0.18f);
                    rot *= Quaternion.Euler(punch * 4f, 0f, punch * -3f);
                }
            }

            _bodyRoot.localPosition = pos;
            _bodyRoot.localRotation = rot;
            _bodyRoot.localScale = scale;
        }

        private static Color ConstructionTint(Color baseColor)
        {
            Color scaffold = new Color(0.55f, 0.48f, 0.32f, 1f);
            return Color.Lerp(baseColor, scaffold, 0.45f);
        }

        private void EnsureScaffold()
        {
            if (_scaffold != null || _bodyRoot == null)
                return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripColliderImmediate(go);
            go.name = "Scaffold";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            go.transform.localScale = new Vector3(1.15f, 0.2f, 1.15f);
            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = CreateColorMaterial(new Color(0.75f, 0.62f, 0.35f, 0.55f));
            go.SetActive(false);
            _scaffold = go;
        }

        public void SetSelected(bool selected)
        {
            bool on = selected && IsRevealed;
            if (_selectionRing != null)
                _selectionRing.gameObject.SetActive(on);
            if (_selectionRingInner != null)
                _selectionRingInner.gameObject.SetActive(on);
            if (_selectionRingCore != null)
                _selectionRingCore.gameObject.SetActive(on);
        }

        public void SetRevealed(bool revealed)
        {
            IsRevealed = revealed;
            if (_renderer != null)
                _renderer.enabled = revealed;
            if (_troopRenderers != null)
            {
                for (int i = 0; i < _troopRenderers.Length; i++)
                {
                    if (_troopRenderers[i] != null)
                        _troopRenderers[i].enabled = revealed;
                }
            }
            if (_teamBandRenderer != null)
                _teamBandRenderer.enabled = revealed;
            // Keep pick volumes enabled for owned-side queries; FOW only hides mesh.
            if (_pickCollider != null)
                _pickCollider.enabled = true;
            if (!revealed)
            {
                if (_selectionRing != null)
                    _selectionRing.gameObject.SetActive(false);
                if (_selectionRingInner != null)
                    _selectionRingInner.gameObject.SetActive(false);
                if (_selectionRingCore != null)
                    _selectionRingCore.gameObject.SetActive(false);
            }

            if (_hpRoot != null)
            {
                float ratio = _maxHealth > 0f ? _health / _maxHealth : 1f;
                _hpRoot.gameObject.SetActive(revealed && ratio < 0.999f);
            }
        }

        private void EnsurePickCollider(bool isUnit, Mesh mesh)
        {
            var existing = gameObject.GetComponents<Collider>();
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null)
                    Object.DestroyImmediate(existing[i]);
            }

            // Generous local pick volume so troops stay clickable from high RTS cameras.
            var sphere = gameObject.AddComponent<SphereCollider>();
            if (isUnit)
            {
                sphere.center = new Vector3(0f, 0.8f, 0f);
                // Keep unit pick tight so large squads don't swallow building clicks.
                float squadBoost = _squadSize > 1 ? 1f + Mathf.Sqrt(_squadSize) * 0.08f : 1f;
                sphere.radius = 1.35f * Mathf.Min(squadBoost, 1.55f);
            }
            else
            {
                float height = mesh != null ? Mathf.Max(mesh.bounds.size.y, 4f) : 6f;
                float extentX = mesh != null ? Mathf.Max(mesh.bounds.extents.x, 2.8f) : 3.5f;
                float extentZ = mesh != null ? Mathf.Max(mesh.bounds.extents.z, 2.8f) : 3.5f;
                float radius = Mathf.Max(extentX, extentZ) * 1.15f;
                radius = Mathf.Clamp(radius, 3.5f, 7.5f);
                sphere.center = new Vector3(0f, height * 0.4f, 0f);
                sphere.radius = radius;
            }

            _pickCollider = sphere;
        }

        private static void StripColliderImmediate(GameObject go)
        {
            if (go == null)
                return;
            var col = go.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
        }

        private void EnsureSelectionRing(bool isUnit)
        {
            if (_selectionRing != null)
                return;

            float outer = isUnit ? (_squadSize > 1 ? 1.35f + _squadSize * 0.12f : 2.6f) : 5.8f;
            if (isUnit && _squadSize > 1)
                outer = Mathf.Clamp(outer, 4.0f, 7.5f);
            float mid = outer * 0.86f;
            float inner = outer * 0.74f;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripColliderImmediate(ring);
            ring.name = "SelectionRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            ring.transform.localScale = new Vector3(outer, 0.045f, outer);
            var ringRend = ring.GetComponent<Renderer>();
            ringRend.sharedMaterial = CreateColorMaterial(new Color(1f, 0.92f, 0.25f, 0.95f));
            ringRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _selectionRing = ring.transform;

            var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripColliderImmediate(hole);
            hole.name = "SelectionRingInner";
            hole.transform.SetParent(transform, false);
            hole.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            hole.transform.localScale = new Vector3(mid, 0.04f, mid);
            var holeRend = hole.GetComponent<Renderer>();
            holeRend.sharedMaterial = CreateColorMaterial(new Color(0.04f, 0.06f, 0.05f, 0.7f));
            holeRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _selectionRingInner = hole.transform;

            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripColliderImmediate(core);
            core.name = "SelectionRingCore";
            core.transform.SetParent(transform, false);
            core.transform.localPosition = new Vector3(0f, 0.125f, 0f);
            core.transform.localScale = new Vector3(inner, 0.03f, inner);
            var coreRend = core.GetComponent<Renderer>();
            coreRend.sharedMaterial = CreateColorMaterial(new Color(0.12f, 0.14f, 0.1f, 0.35f));
            coreRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _selectionRingCore = core.transform;
        }

        private void EnsureTeamBand()
        {
            if (_teamBandRenderer != null)
                return;

            var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripColliderImmediate(band);
            band.name = "TeamBand";
            band.transform.SetParent(transform, false);
            band.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            band.transform.localScale = new Vector3(0.95f, 0.18f, 0.55f);
            _teamBandRenderer = band.GetComponent<Renderer>();
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
            StripColliderImmediate(bg);
            bg.name = "HpBg";
            bg.transform.SetParent(_hpRoot, false);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = Vector3.one;
            bg.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.08f, 0.08f, 0.1f, 0.85f));

            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            StripColliderImmediate(fill);
            fill.name = "HpFill";
            fill.transform.SetParent(_hpRoot, false);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fill.transform.localScale = Vector3.one;
            fill.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.25f, 0.85f, 0.35f, 0.95f));
            _hpFill = fill.transform;
        }

        private void ApplyBodyColor(Color color)
        {
            if (_troopRenderers != null && _troopRenderers.Length > 0)
            {
                for (int i = 0; i < _troopRenderers.Length; i++)
                {
                    var rend = _troopRenderers[i];
                    if (rend == null)
                        continue;
                    var mat = rend.material;
                    if (mat != null)
                        SetMatColor(mat, color);
                }

                return;
            }

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
