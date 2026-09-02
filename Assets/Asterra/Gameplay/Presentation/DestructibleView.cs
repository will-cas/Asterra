using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Clickable world prop for trees / rocks / bridges (sim destructibles).</summary>
    public sealed class DestructibleView : MonoBehaviour
    {
        public const float FallSeconds = 0.62f;

        public SimEntityId Id { get; private set; }
        public string DefinitionId { get; private set; }
        public bool IsFalling => _falling;
        public bool FallFinished => _falling && Time.time >= _fallUntil;

        private MeshRenderer _renderer;
        private Color _baseColor;
        private Vector3 _worldPos;
        private float _yaw;
        private float _phase;
        private bool _posed;
        private bool _tree;
        private bool _mill;
        private bool _flag;
        private bool _rock;
        private bool _bridge;
        private bool _falling;
        private float _fallUntil;
        private Vector3 _restScale = Vector3.one;
        private bool _damaged;

        public void Initialize(SimEntityId id, string definitionId, Color baseColor)
        {
            Id = id;
            DefinitionId = definitionId ?? string.Empty;
            _baseColor = baseColor;
            _renderer = GetComponent<MeshRenderer>();
            _phase = (id.Value % 97) * 0.21f;
            string def = DefinitionId.ToLowerInvariant();
            _tree = def.Contains("tree") || def.Contains("heartwood") || def.Contains("grove");
            _mill = def.Contains("mill");
            _flag = def.Contains("banner") || def.Contains("flag") || def.Contains("standard");
            _rock = def.Contains("rock") || def.Contains("stone") || def.Contains("boulder");
            _bridge = def.Contains("bridge") || def.Contains("span");
            _restScale = transform.localScale;
        }

        public void SetWorldPose(Vector3 worldPos, float yawDegrees)
        {
            if (_falling)
                return;
            _worldPos = worldPos;
            _yaw = yawDegrees;
            _posed = true;
        }

        public void BeginFall()
        {
            if (_falling)
                return;
            _falling = true;
            _fallUntil = Time.time + FallSeconds;
        }

        private void LateUpdate()
        {
            if (!_posed)
                return;

            Vector4 wind = Shader.GetGlobalVector("_AsterraWind");
            float intensity = Mathf.Clamp(wind.y, 0.12f, 1.6f);
            float t = Time.time + _phase;
            float pitch = 0f;
            float roll = 0f;
            float yaw = _yaw;
            Vector3 pos = _worldPos;
            Vector3 scale = _restScale;

            if (_falling)
            {
                float u = 1f - Mathf.Clamp01((_fallUntil - Time.time) / FallSeconds);
                float ease = u * u;
                if (_tree)
                {
                    pitch = ease * 88f;
                    pos.y -= ease * 0.4f;
                }
                else if (_rock)
                {
                    pos.y -= ease * 1.2f;
                    scale = Vector3.Lerp(_restScale, new Vector3(_restScale.x * 1.35f, _restScale.y * 0.35f, _restScale.z * 1.35f), ease);
                    roll = ease * 25f;
                }
                else if (_bridge)
                {
                    pitch = ease * 18f;
                    pos.y -= ease * 2.2f;
                    roll = Mathf.Sin(t * 22f) * (1f - ease) * 8f;
                }
                else
                {
                    pitch = ease * 40f;
                    pos.y -= ease * 0.8f;
                }

                transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, roll));
                transform.localScale = scale;
                return;
            }

            if (_tree)
            {
                float amp = (_damaged ? 7.5f : 4.5f) * intensity;
                pitch = Mathf.Sin(t * 0.75f) * amp + (_damaged ? 8f : 0f);
                roll = Mathf.Sin(t * 0.95f + 0.8f) * amp * 0.7f;
                yaw += Mathf.Sin(t * 0.4f) * amp * 0.35f;
            }
            else if (_mill)
            {
                yaw = _yaw + t * 80f;
            }
            else if (_flag)
            {
                yaw += Mathf.Sin(t * 5.5f) * 14f * intensity;
                pitch = Mathf.Sin(t * 7f) * 8f * intensity;
            }
            else
            {
                yaw += Mathf.Sin(t * 0.55f) * 1.6f * intensity;
                if (_damaged)
                    pitch = 6f;
            }

            transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, roll));
        }

        public void SetDamaged(bool damaged)
        {
            _damaged = damaged;
            if (_renderer == null || _renderer.sharedMaterial == null)
                return;
            var c = damaged
                ? Color.Lerp(_baseColor, new Color(0.55f, 0.35f, 0.2f), 0.45f)
                : _baseColor;
            if (_renderer.sharedMaterial.HasProperty("_Color"))
                _renderer.sharedMaterial.color = c;
            if (_renderer.sharedMaterial.HasProperty("_BaseColor"))
                _renderer.sharedMaterial.SetColor("_BaseColor", c);
        }
    }
}
