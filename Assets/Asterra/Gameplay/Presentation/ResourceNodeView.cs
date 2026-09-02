using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Clickable resource node for gather orders.</summary>
    public sealed class ResourceNodeView : MonoBehaviour
    {
        public SimEntityId Id { get; private set; }
        public ResourceType Type { get; private set; }

        private Vector3 _worldPos;
        private Vector3 _restScale;
        private float _phase;
        private bool _posed;
        private int _maxRemaining = 1;
        private float _fill01 = 1f;

        public void Initialize(SimEntityId id, ResourceType type)
        {
            Id = id;
            Type = type;
            _restScale = transform.localScale;
            _phase = (id.Value % 71) * 0.19f;
        }

        public void SetWorldPose(Vector3 worldPos)
        {
            _worldPos = worldPos;
            _posed = true;
        }

        public void SetRemaining(int remaining)
        {
            remaining = Mathf.Max(0, remaining);
            if (remaining > _maxRemaining)
                _maxRemaining = remaining;
            _fill01 = _maxRemaining > 0 ? Mathf.Clamp01(remaining / (float)_maxRemaining) : 0f;
        }

        private void LateUpdate()
        {
            if (!_posed)
                return;

            float t = Time.time + _phase;
            float shrink = 0.45f + 0.55f * _fill01;
            if (Type == ResourceType.Gold)
            {
                transform.SetPositionAndRotation(
                    _worldPos + new Vector3(0f, Mathf.Sin(t * 1.7f) * 0.28f * _fill01, 0f),
                    Quaternion.Euler(12f, t * 38f, 8f));
                float pulse = 1f + Mathf.Sin(t * 2.4f) * 0.04f;
                transform.localScale = _restScale * pulse * shrink;
            }
            else
            {
                transform.SetPositionAndRotation(
                    _worldPos,
                    Quaternion.Euler(
                        Mathf.Sin(t * 0.8f) * 4f,
                        18f + Mathf.Sin(t * 0.35f) * 6f,
                        Mathf.Cos(t * 0.6f) * 3f));
                transform.localScale = _restScale * shrink;
            }
        }
    }
}
