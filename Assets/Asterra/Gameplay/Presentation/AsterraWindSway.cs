using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    public enum AsterraPropMotion
    {
        Wind = 0,
        Spin = 1,
        Pulse = 2,
        Bob = 3,
    }

    /// <summary>Client-only procedural motion for world props (trees, reeds, crystals, resources).</summary>
    public sealed class AsterraWindSway : MonoBehaviour
    {
        public AsterraPropMotion Kind = AsterraPropMotion.Wind;
        public float Amount = 1f;
        public float Speed = 1f;

        private Quaternion _restRot;
        private Vector3 _restPos;
        private Vector3 _restScale;
        private float _phase;
        private bool _captured;

        public static AsterraWindSway Add(GameObject go, AsterraPropMotion kind, float amount, float speed = 1f)
        {
            if (go == null)
                return null;
            var sway = go.GetComponent<AsterraWindSway>();
            if (sway == null)
                sway = go.AddComponent<AsterraWindSway>();
            sway.Kind = kind;
            sway.Amount = amount;
            sway.Speed = speed;
            sway.CaptureRest();
            return sway;
        }

        public void CaptureRest()
        {
            _restRot = transform.localRotation;
            _restPos = transform.localPosition;
            _restScale = transform.localScale;
            if (_restScale.sqrMagnitude < 1e-8f)
                _restScale = Vector3.one;
            Vector3 p = transform.position;
            _phase = Mathf.Repeat(p.x * 0.17f + p.z * 0.13f, Mathf.PI * 2f);
            _captured = true;
        }

        private void OnEnable()
        {
            if (!_captured)
                CaptureRest();
        }

        private void LateUpdate()
        {
            if (!_captured)
                CaptureRest();

            Vector4 wind = Shader.GetGlobalVector("_AsterraWind");
            float intensity = Mathf.Clamp(wind.y, 0.12f, 1.6f);
            float t = Time.time * Speed + _phase;
            float gust = 0.65f + 0.35f * Mathf.Sin(t * 0.37f + wind.x);

            switch (Kind)
            {
                case AsterraPropMotion.Spin:
                    transform.localRotation = _restRot * Quaternion.Euler(0f, t * 42f * Amount, 0f);
                    transform.localPosition = _restPos + new Vector3(0f, Mathf.Sin(t * 1.6f) * 0.08f * Amount, 0f);
                    transform.localScale = _restScale;
                    break;
                case AsterraPropMotion.Pulse:
                {
                    float u = 1f + Mathf.Sin(t * 2.4f) * 0.06f * Amount;
                    transform.localRotation = _restRot * Quaternion.Euler(Mathf.Sin(t) * 2f, t * 18f * Amount, Mathf.Cos(t * 0.8f) * 2f);
                    transform.localScale = _restScale * u;
                    transform.localPosition = _restPos;
                    break;
                }
                case AsterraPropMotion.Bob:
                    transform.localRotation = _restRot * Quaternion.Euler(0f, Mathf.Sin(t * 0.7f) * 8f * Amount, 0f);
                    transform.localPosition = _restPos + new Vector3(0f, Mathf.Sin(t * 1.35f) * 0.12f * Amount, 0f);
                    transform.localScale = _restScale;
                    break;
                default:
                {
                    float amp = Amount * intensity * gust;
                    float pitch = Mathf.Sin(t * 0.85f) * 4.8f * amp;
                    float roll = Mathf.Sin(t * 1.07f + 1.3f) * 3.2f * amp;
                    float yaw = Mathf.Sin(t * 0.45f) * 2.4f * amp;
                    transform.localRotation = _restRot * Quaternion.Euler(pitch, yaw, roll);
                    transform.localPosition = _restPos;
                    transform.localScale = _restScale;
                    break;
                }
            }
        }
    }
}
