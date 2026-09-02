using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Clickable view bound to a sim entity.</summary>
    public sealed class EntityView : MonoBehaviour
    {
        public const float UnitVisualScale = 8f;
        public const float BuildingVisualScale = 2.5f;
        public const float BuildingCollapseSeconds = 0.42f;

        public SimEntityId Id { get; private set; }
        public bool IsUnit { get; private set; }
        public PlayerId Owner { get; private set; }
        public string DefinitionId { get; private set; }
        public bool IsRevealed { get; private set; } = true;
        /// <summary>True while a building death collapse is playing (delay Destroy).</summary>
        public bool IsCollapsing => !IsUnit && _collapsing;
        public bool CollapseFinished => _collapsing && Time.time >= _collapseUntil;
        public const float UnitDeathSeconds = 0.58f;
        public bool IsDying => IsUnit && _dying;
        public bool DeathFinished => _dying && Time.time >= _deathUntil;

        private bool _dying;
        private float _deathUntil;
        private bool _garrisonedHidden;
        private float _runWeight;
        private float _wadeWeight;
        private float _carryWeight;
        private float _deathWeight;
        private bool _researching;
        private float _research01;
        private BuildingKind _buildingKind;
        private bool _buildingDisabled;
        private bool _boat;
        private UnitStance _stance = UnitStance.Aggressive;
        private float _ackUntil;
        private bool _stunned;
        private bool _airborne;
        private float _slopePitch;
        private float _spawnUntil;
        private sbyte _outcome;
        private Transform _blobShadow;
        private Transform _banner;

        private Transform _selectionRing;
        private Renderer _renderer;
        private Renderer[] _troopRenderers;
        private Transform[] _troopHosts;
        private Vector3[] _troopRestPos;
        private Quaternion[] _troopRestRot;
        private Vector3[] _troopRestScale;
        private int _squadSize = 1;
        private UnitRole _unitRole;
        private bool _locomoting;
        private bool _attacking;
        private bool _gathering;
        private bool _unitIdle = true;
        private bool _hasCarry;
        private float _speed01;
        private float _moveWeight;
        private float _attackWeight;
        private float _gatherWeight;
        private float _idleWeight;
        private float _gait;
        private int _gaitFrame = -1;
        private float _hitWeight;
        private bool _producing;
        private float _production01;
        private Collider _pickCollider;
        private Color _baseColor = Color.gray;
        private Color _factionColor = Color.gray;
        private Texture2D _albedo;
        private float _uvScale = 0.18f;
        private string _pbrKey = "cloth";
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
            Initialize(id, isUnit, owner, definitionId, factionIndex, squadSize, AsterraMeshLibrary.FactionColor(factionIndex));
        }

        public void Initialize(
            SimEntityId id,
            bool isUnit,
            PlayerId owner,
            string definitionId,
            byte factionIndex,
            int squadSize,
            Color teamColor)
        {
            Id = id;
            IsUnit = isUnit;
            Owner = owner;
            DefinitionId = definitionId;

            float visualScale = isUnit ? UnitVisualScale : BuildingVisualScale;
            _factionColor = teamColor;
            _unitRole = isUnit ? AsterraMeshLibrary.InferRole(definitionId) : UnitRole.Infantry;
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
                visualScale *= AsterraMeshLibrary.BuildingVisualMultiplier(definitionId, factionIndex);
            }

            transform.localScale = Vector3.one * visualScale;

            // Body root lets us bob/hit-punch without fighting world position sync.
            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            _bodyRoot = body.transform;

            Mesh mesh = isUnit
                ? AsterraMeshLibrary.GetUnitMesh(definitionId)
                : AsterraMeshLibrary.GetBuildingMesh(definitionId, factionIndex);

            _baseColor = new Color(1f, 1f, 1f, 1f);
            _albedo = AsterraMeshLibrary.GetBodyAlbedo(isUnit, definitionId, factionIndex);
            _uvScale = AsterraMeshLibrary.BodyUvScale(isUnit);
            _pbrKey = AsterraPbrLibrary.BodySetKey(isUnit, definitionId);
            BuildTroopMeshes(mesh, isUnit);

            if (isUnit)
                _spawnUntil = Time.time + 0.38f;
            EnsurePickCollider(isUnit, mesh);
            EnsureSelectionRing(isUnit);
            EnsureHealthBar(isUnit);
            SetSelected(false);
            SetRevealed(true);
            SetHealth(1f, 1f);
            _animPhase = (id.Value % 97) * 0.173f;
            if (isUnit)
                EnsureBlobShadow();
            if (!isUnit)
                EnsureScaffold();
            if (!isUnit && AsterraMeshLibrary.IsKeep(definitionId))
                EnsureKeepBanner();
        }

        private void BuildTroopMeshes(Mesh mesh, bool isUnit)
        {
            int count = isUnit ? Mathf.Max(1, _squadSize) : 1;
            _troopRenderers = new Renderer[count];
            _troopHosts = new Transform[count];
            _troopRestPos = new Vector3[count];
            _troopRestRot = new Quaternion[count];
            _troopRestScale = new Vector3[count];
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
                _troopRestPos[i] = host.localPosition;
                _troopRestRot[i] = host.localRotation;
                _troopRestScale[i] = host.localScale;
                var filter = host.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var rend = host.gameObject.AddComponent<MeshRenderer>();
                // Unique material instance per troop so hit-flash does not tint siblings wrong.
                rend.material = CreateBodyMaterial(mesh, isUnit);
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
            }

            if (_scaffold != null)
                _scaffold.SetActive(state == BuildingState.Constructing || state == BuildingState.Ghost);
        }

        public void SetBuildingActivity(
            bool producing,
            float production01,
            bool researching = false,
            float research01 = 0f,
            BuildingKind kind = BuildingKind.Generic,
            bool disabled = false)
        {
            _producing = producing;
            _production01 = Mathf.Clamp01(production01);
            _researching = researching;
            _research01 = Mathf.Clamp01(research01);
            _buildingKind = kind;
            _buildingDisabled = disabled;
        }

        public void BeginDeath()
        {
            if (!IsUnit || _dying)
                return;
            _dying = true;
            _deathUntil = Time.time + UnitDeathSeconds;
            _garrisonedHidden = false;
            if (_bodyRoot != null)
                _bodyRoot.gameObject.SetActive(true);
            if (_blobShadow != null)
                _blobShadow.gameObject.SetActive(true);
            SetHitFlash();
            if (_hpRoot != null)
                _hpRoot.gameObject.SetActive(false);
            if (_selectionRing != null)
                _selectionRing.gameObject.SetActive(false);
        }

        public void SetGarrisoned(bool garrisoned)
        {
            if (!IsUnit)
                return;
            bool wasHidden = _garrisonedHidden;
            _garrisonedHidden = garrisoned;
            if (_bodyRoot != null)
                _bodyRoot.gameObject.SetActive(!garrisoned);
            if (_blobShadow != null)
                _blobShadow.gameObject.SetActive(!garrisoned);
            if (wasHidden && !garrisoned && IsUnit)
                _spawnUntil = Time.time + 0.28f;
        }

        public void SetOutcomePose(bool victorious)
        {
            _outcome = (sbyte)(victorious ? 1 : -1);
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
        }

        private void EnsureKeepBanner()
        {
            if (_banner != null || _bodyRoot == null)
                return;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripColliderImmediate(pole);
            pole.name = "BannerPole";
            pole.transform.SetParent(_bodyRoot, false);
            pole.transform.localPosition = new Vector3(0.35f, 1.15f, 0.1f);
            pole.transform.localScale = new Vector3(0.04f, 0.55f, 0.04f);
            pole.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial(new Color(0.25f, 0.18f, 0.12f));

            var cloth = GameObject.CreatePrimitive(PrimitiveType.Quad);
            StripColliderImmediate(cloth);
            cloth.name = "BannerCloth";
            cloth.transform.SetParent(pole.transform, false);
            cloth.transform.localPosition = new Vector3(0.28f, 0.35f, 0f);
            cloth.transform.localScale = new Vector3(0.55f, 0.7f, 1f);
            var rend = cloth.GetComponent<Renderer>();
            rend.sharedMaterial = CreateColorMaterial(_factionColor);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _banner = cloth.transform;
            AsterraWindSway.Add(cloth, AsterraPropMotion.Wind, 1.8f, 1.6f);
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
            else
            {
                // Isolated segment: honour placement yaw and elongate along local X.
                bool sideways = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 1f
                                || Mathf.Abs(Mathf.DeltaAngle(yaw, 270f)) < 1f;
                if (sideways)
                {
                    sx = BuildingVisualScale * 0.85f;
                    sz = BuildingVisualScale * 1.35f;
                }
                else
                {
                    sx = BuildingVisualScale * 1.35f;
                    sz = BuildingVisualScale * 0.85f;
                }
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

            if (!IsUnit)
            {
                if (ratio < 0.38f && _buildingState == BuildingState.Active)
                    EnsureFlameFx(true);
                else if (ratio >= 0.45f)
                    EnsureFlameFx(false);
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
            SyncPresentation(worldPos, locomoting: false, attacking: false, idle: true, hasCarry: false);
        }

        public void SyncPresentation(Vector3 worldPos, bool locomoting, bool attacking, bool idle, bool hasCarry)
        {
            SyncPresentation(
                worldPos,
                locomoting,
                attacking,
                idle,
                hasCarry,
                running: false,
                gathering: hasCarry && !locomoting && !attacking,
                stunned: false,
                airborne: false,
                boat: false,
                wade: 0f,
                carryTimber: false,
                stance: UnitStance.Aggressive);
        }

        public void SyncPresentation(
            Vector3 worldPos,
            bool locomoting,
            bool attacking,
            bool idle,
            bool hasCarry,
            bool running,
            bool gathering,
            bool stunned,
            bool airborne,
            bool boat,
            float wade,
            bool carryTimber,
            UnitStance stance,
            float slopeDegrees = 0f,
            float facingYaw = 0f,
            bool hasFacing = false)
        {
            if (_dying || _garrisonedHidden)
            {
                ApplyBodyMotion();
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            if (_hasLastPos && IsUnit)
            {
                Vector3 delta = worldPos - _lastPos;
                delta.y = 0f;
                float dist = delta.magnitude;
                _speed01 = Mathf.Clamp01(dist / (dt * 7.5f));
                float targetYaw = _yaw;
                // Ignore sub-cell jitter — Atan2 of noise makes the unit pirouette while walking.
                if (dist > 0.18f)
                    targetYaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                else if (hasFacing)
                    targetYaw = facingYaw;
                _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, dt * (running ? 280f : 160f));
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }

            _locomoting = !stunned && (locomoting || _speed01 > 0.12f);
            _attacking = attacking && !stunned;
            _unitIdle = idle && !_locomoting && !attacking;
            _hasCarry = hasCarry;
            _gathering = gathering && !stunned;
            _stunned = stunned;
            _airborne = airborne;
            _boat = boat;
            _stance = stance;
            _runWeight = Mathf.MoveTowards(_runWeight, running && _locomoting ? 1f : 0f, dt * 4f);
            _wadeWeight = Mathf.MoveTowards(_wadeWeight, wade, dt * 3f);
            _carryWeight = Mathf.MoveTowards(_carryWeight, hasCarry ? (carryTimber ? 0.85f : 0.55f) : 0f, dt * 4f);
            _slopePitch = Mathf.MoveTowards(_slopePitch, Mathf.Clamp(slopeDegrees, -18f, 18f), dt * 40f);

            _lastPos = worldPos;
            _hasLastPos = true;
            if (!_collapsing)
            {
                float y = worldPos.y;
                if (airborne)
                    y += 2.4f + Mathf.Sin(Time.time * 3.5f + _animPhase) * 0.35f;
                transform.position = new Vector3(worldPos.x, y, worldPos.z);
            }

            ApplyBodyMotion();

            if (_hpRoot != null && Camera.main != null)
                _hpRoot.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
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

            // Units already posed in SyncPresentation; collapse still needs a tick when sync skips the view.
            if (_bodyRoot != null && (!IsUnit && (_collapsing || _buildingState == BuildingState.Active)
                                      || (IsUnit && _dying)))
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

        private void ApplyUnitMotion()
        {
            float dt = Time.deltaTime;
            float t = Time.time + _animPhase;
            float hitBob = 0f;
            _hitWeight = Time.time < _hitBobUntil ? 1f : Mathf.MoveTowards(_hitWeight, 0f, dt * 8f);
            if (Time.time < _hitBobUntil)
            {
                float u = 1f - ((_hitBobUntil - Time.time) / 0.22f);
                hitBob = Mathf.Sin(u * Mathf.PI) * 0.08f;
            }

            float moveTarget = (_locomoting && !_stunned) ? Mathf.Lerp(0.35f, 1f, _speed01) : 0f;
            float attackTarget = _attacking ? 1f : 0f;
            float gatherTarget = (_gathering && !_locomoting && !_attacking) ? 1f : 0f;
            float idleTarget = (_stunned || (!_locomoting && !_attacking)) ? (_stunned ? 0.2f : 1f) : 0.12f;
            if (_stance == UnitStance.Hold || _stance == UnitStance.Passive)
                idleTarget = Mathf.Max(idleTarget, 0.7f);
            float hp01 = _maxHealth > 0.01f ? _health / _maxHealth : 1f;
            if (hp01 < 0.28f && !_locomoting)
                idleTarget = Mathf.Max(idleTarget, 0.85f);
            _moveWeight = Mathf.MoveTowards(_moveWeight, moveTarget, dt * (_locomoting ? 3.2f : 4.5f));
            _attackWeight = Mathf.MoveTowards(_attackWeight, attackTarget, dt * 6f);
            _gatherWeight = Mathf.MoveTowards(_gatherWeight, gatherTarget, dt * 5f);
            _idleWeight = Mathf.MoveTowards(_idleWeight, idleTarget, dt * 2.4f);
            _deathWeight = _dying
                ? 1f - Mathf.Clamp01((_deathUntil - Time.time) / UnitDeathSeconds)
                : 0f;
            float rain = Shader.GetGlobalVector("_AsterraWind").y;
            if (rain > 0.7f && !_locomoting)
                _idleWeight = Mathf.Max(_idleWeight, 0.5f);

            float freq = _unitRole switch
            {
                UnitRole.Cavalry => 7.4f,
                UnitRole.Siege => 4.2f,
                UnitRole.Ranged => 8.6f,
                UnitRole.Builder => 7.6f,
                _ => 8.2f,
            };
            if (_gaitFrame != Time.frameCount)
            {
                _gait += dt * freq * Mathf.Lerp(0.15f, 1f, _moveWeight);
                if (_gatherWeight > 0.2f || _attackWeight > 0.2f)
                    _gait += dt * 3.5f * Mathf.Max(_gatherWeight, _attackWeight);
                _gait = Mathf.Repeat(_gait, Mathf.PI * 8f);
                _gaitFrame = Time.frameCount;
            }

            bool squad = _troopHosts != null && _troopHosts.Length > 1;
            Vector3 bodyPos = new Vector3(0f, hitBob, 0f);
            Quaternion bodyRot = Quaternion.identity;
            Vector3 bodyScale = Vector3.one;

            if (!squad)
            {
                SampleUnitPose(t, 0, out bodyPos, out bodyRot, out bodyScale);
                bodyPos.y += hitBob;
            }

            _bodyRoot.localPosition = bodyPos;
            _bodyRoot.localRotation = bodyRot;
            _bodyRoot.localScale = bodyScale;

            if (!squad)
            {
                PushUnitAnim(_troopRenderers != null && _troopRenderers.Length > 0 ? _troopRenderers[0] : _renderer, 0);
                return;
            }

            for (int i = 0; i < _troopHosts.Length; i++)
            {
                var host = _troopHosts[i];
                if (host == null)
                    continue;
                SampleUnitPose(t, i, out Vector3 pos, out Quaternion rot, out Vector3 scale);
                host.localPosition = _troopRestPos[i] + pos;
                host.localRotation = _troopRestRot[i] * rot;
                host.localScale = Vector3.Scale(_troopRestScale[i], scale);
                if (_troopRenderers != null && i < _troopRenderers.Length)
                    PushUnitAnim(_troopRenderers[i], i);
            }
        }

        private void PushUnitAnim(Renderer rend, int index)
        {
            if (rend == null)
                return;
            var mat = rend.sharedMaterial;
            if (mat == null || !mat.HasProperty("_AnimParams"))
                return;
            float phase = _gait + _animPhase + index * 0.85f;
            float role = _boat ? 5f : (float)_unitRole;
            mat.SetVector("_AnimParams", new Vector4(phase, _moveWeight, _attackWeight, _gatherWeight));
            mat.SetVector("_AnimParams2", new Vector4(
                _idleWeight,
                Mathf.Max(_hitWeight, _stunned ? 0.6f : 0f),
                role,
                Time.time + _animPhase + index * 0.37f));
            if (mat.HasProperty("_AnimParams3"))
                mat.SetVector("_AnimParams3", new Vector4(_deathWeight, _runWeight, _wadeWeight, _carryWeight));
        }

        private void SampleUnitPose(float t, int index, out Vector3 pos, out Quaternion rot, out Vector3 scale)
        {
            float phase = _animPhase + index * 0.73f;
            float bobAmp;
            float lean;
            switch (_unitRole)
            {
                case UnitRole.Cavalry:
                    bobAmp = 0.07f;
                    lean = 9f;
                    break;
                case UnitRole.Siege:
                    bobAmp = 0.025f;
                    lean = 4f;
                    break;
                case UnitRole.Ranged:
                    bobAmp = 0.045f;
                    lean = 6f;
                    break;
                case UnitRole.Builder:
                    bobAmp = 0.05f;
                    lean = 5f;
                    break;
                default:
                    bobAmp = 0.055f;
                    lean = 7f;
                    break;
            }

            float loc = _moveWeight;
            float gait = _gait + phase;
            float step = Mathf.Abs(Mathf.Sin(gait));
            float plant = Mathf.Max(0f, -Mathf.Sin(gait * 2f));

            pos = new Vector3(0f, (step * bobAmp * 0.45f + plant * bobAmp * 0.35f) * loc, 0f);
            float pitch = -lean * 0.22f * loc - _slopePitch * 0.35f;
            float roll = 0f;
            if (_runWeight > 0.2f)
                pitch -= 4f * _runWeight;
            if (_wadeWeight > 0.1f)
                pos.y -= 0.04f * _wadeWeight;

            if (_attackWeight > 0.01f)
            {
                float strike = Mathf.Max(0f, Mathf.Sin((t + phase) * 9.5f));
                pos.z += strike * 0.035f * _attackWeight;
                pitch -= strike * 4f * _attackWeight;
            }
            else if (_gatherWeight > 0.01f)
            {
                float chop = Mathf.Sin((t + phase) * 8.2f);
                pitch += chop * 4f * _gatherWeight;
                pos.y += Mathf.Abs(chop) * 0.01f * _gatherWeight;
            }
            else if (_idleWeight > 0.5f)
            {
                pos.y += Mathf.Sin((t + phase) * 1.35f) * 0.008f * _idleWeight;
                roll += Mathf.Sin((t + phase) * 0.6f) * 1.4f * _idleWeight;
            }

            if (_hasCarry && _attackWeight < 0.2f)
                pitch += 3f;

            rot = Quaternion.Euler(pitch, 0f, roll);
            scale = Vector3.one;
            if (Time.time < _ackUntil)
            {
                float u = 1f - ((_ackUntil - Time.time) / 0.32f);
                pitch -= Mathf.Sin(u * Mathf.PI) * 8f;
                rot = Quaternion.Euler(pitch, 0f, roll);
            }
            if (Time.time < _spawnUntil)
            {
                float u = 1f - ((_spawnUntil - Time.time) / 0.38f);
                pos.y += (1f - u) * 0.12f;
                scale = new Vector3(1f, 0.55f + u * 0.45f, 1f);
            }
            if (_deathWeight > 0.01f)
            {
                pos.y -= _deathWeight * 0.35f;
                pitch += _deathWeight * 55f;
                roll += _deathWeight * 25f;
                rot = Quaternion.Euler(pitch, 0f, roll);
                scale = Vector3.Lerp(scale, new Vector3(1.1f, 0.25f, 1.1f), _deathWeight);
            }
            else if (_outcome != 0 && !_locomoting)
            {
                if (_outcome > 0)
                {
                    pos.y += Mathf.Abs(Mathf.Sin((t + phase) * 6f)) * 0.04f;
                    pitch -= 8f;
                }
                else
                {
                    pitch += 18f;
                    pos.y -= 0.04f;
                }
                rot = Quaternion.Euler(pitch, 0f, roll);
            }
            float breathe = 1f + Mathf.Sin((t + phase) * 1.35f) * 0.012f * _idleWeight;
            if (Time.time >= _spawnUntil && _deathWeight < 0.01f)
                scale = new Vector3(1f, breathe, 1f);
        }

        private void ApplyBodyMotion()
        {
            if (_bodyRoot == null)
                return;

            if (IsUnit)
            {
                ApplyUnitMotion();
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
                float work = (_producing ? 1f + _production01 * 0.8f : 1f)
                             * (_researching ? 1.25f : 1f);
                bool mill = DefinitionId != null && DefinitionId.Contains("mill");
                bool gate = _buildingKind == BuildingKind.Gate;
                float sway = Mathf.Sin((t + _animPhase) * 1.15f * work) * (0.55f + (_producing || _researching ? 1.4f : 0f));
                if (_buildingDisabled)
                    sway = 0f;
                float breath = 1f + Mathf.Sin((t + _animPhase) * (0.7f + work * 0.4f)) * (0.012f + (_producing ? 0.02f : 0f));
                rot = mill
                    ? Quaternion.Euler(0f, (t + _animPhase) * 70f, 0f)
                    : Quaternion.Euler(_buildingDisabled ? 6f : 0f, sway + (gate ? 12f : 0f), _buildingDisabled ? 4f : 0f);
                scale = new Vector3(breath, 1f + (breath - 1f) * 0.5f, breath);
                pos = Vector3.zero;
                if (_producing)
                    pos.y = Mathf.Abs(Mathf.Sin((t + _animPhase) * 5.5f)) * 0.04f;

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
            bool on = selected && IsRevealed && !_dying && !_garrisonedHidden;
            if (on && (_selectionRing == null || !_selectionRing.gameObject.activeSelf))
                _ackUntil = Time.time + 0.32f;
            if (_selectionRing != null)
                _selectionRing.gameObject.SetActive(on);
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
            // Keep pick volumes enabled for owned-side queries; FOW only hides mesh.
            if (_pickCollider != null)
                _pickCollider.enabled = true;
            if (!revealed)
            {
                if (_selectionRing != null)
                    _selectionRing.gameObject.SetActive(false);
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

        private void EnsureBlobShadow()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            StripColliderImmediate(quad);
            quad.name = "BlobShadow";
            quad.transform.SetParent(transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float worldSize = _squadSize > 1
                ? Mathf.Clamp(4.4f + _squadSize * 0.28f, 4.4f, 10f)
                : 2.9f;
            float inv = 1f / Mathf.Max(0.01f, transform.lossyScale.x);
            quad.transform.localPosition = new Vector3(0f, 0.04f * inv, 0f);
            quad.transform.localScale = new Vector3(worldSize * inv, worldSize * inv, 1f);
            var rend = quad.GetComponent<Renderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            var shader = Shader.Find("Asterra/BlobShadow")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", new Color(0f, 0f, 0f, 0.42f));
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.42f));
            rend.sharedMaterial = mat;
            _blobShadow = quad.transform;
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

            var ring = new GameObject("SelectionHalo");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            ring.transform.localScale = new Vector3(outer, 1f, outer);
            var filter = ring.AddComponent<MeshFilter>();
            filter.sharedMesh = HaloRingMesh();
            var ringRend = ring.AddComponent<MeshRenderer>();
            Color halo = _factionColor;
            halo.a = 0.95f;
            ringRend.sharedMaterial = CreateColorMaterial(halo);
            ringRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRend.receiveShadows = false;
            _selectionRing = ring.transform;
        }

        private static Mesh s_haloRing;

        private static Mesh HaloRingMesh()
        {
            if (s_haloRing != null)
                return s_haloRing;

            const int segs = 48;
            const float inner = 0.78f;
            var verts = new Vector3[segs * 2];
            var uv = new Vector2[segs * 2];
            var tris = new int[segs * 6];
            for (int i = 0; i < segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2f;
                float x = Mathf.Sin(a);
                float z = Mathf.Cos(a);
                verts[i] = new Vector3(x, 0f, z);
                verts[i + segs] = new Vector3(x * inner, 0f, z * inner);
                uv[i] = new Vector2(1f, i / (float)segs);
                uv[i + segs] = new Vector2(0f, i / (float)segs);
            }

            for (int i = 0; i < segs; i++)
            {
                int n = (i + 1) % segs;
                int t = i * 6;
                tris[t] = i;
                tris[t + 1] = n;
                tris[t + 2] = i + segs;
                tris[t + 3] = n;
                tris[t + 4] = n + segs;
                tris[t + 5] = i + segs;
            }

            s_haloRing = new Mesh { name = "AsterraSelectionHalo" };
            s_haloRing.vertices = verts;
            s_haloRing.uv = uv;
            s_haloRing.triangles = tris;
            s_haloRing.RecalculateNormals();
            s_haloRing.RecalculateBounds();
            return s_haloRing;
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

        private Material CreateBodyMaterial(Mesh mesh, bool isUnit)
        {
            string key = string.IsNullOrEmpty(_pbrKey)
                ? AsterraPbrLibrary.BodySetKey(isUnit, DefinitionId)
                : _pbrKey;
            var mat = AsterraPbrLibrary.CreateLit(_baseColor, key, AsterraPbrLibrary.MetallicForSet(key));
            if (mat.HasProperty("_UvScale"))
                mat.SetFloat("_UvScale", Mathf.Max(0.08f, _uvScale));
            AsterraPbrLibrary.ApplyTeamDye(mat, _factionColor, building: !isUnit, key, mesh);
            if (mat.HasProperty("_AnimBounds") && mesh != null)
            {
                Bounds b = mesh.bounds;
                mat.SetVector("_AnimBounds", new Vector4(b.min.y, b.max.y, 0f, 0f));
            }
            return mat;
        }

        private static Material CreateColorMaterial(Color color)
        {
            return CreateColorMaterial(color, null, 0.18f);
        }

        private static Material CreateColorMaterial(Color color, Texture2D albedo, float uvScale)
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
            if (albedo != null)
                mat.SetTexture("_MainTex", albedo);
            mat.SetFloat("_UvScale", uvScale);
            mat.SetFloat("_TexBlend", albedo != null ? 0.72f : 0f);
            return mat;
        }
    }
}
