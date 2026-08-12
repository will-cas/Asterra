using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Short-lived top-of-screen toast for order feedback.</summary>
    public sealed class MatchFeedback : MonoBehaviour
    {
        private static MatchFeedback _instance;

        [SerializeField] private float defaultSeconds = 2.2f;

        public string CurrentMessage { get; private set; } = string.Empty;
        public float ExpireAt { get; private set; }

        public bool HasActiveMessage =>
            !string.IsNullOrEmpty(CurrentMessage) && Time.unscaledTime < ExpireAt;

        public static MatchFeedback Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                _instance = FindFirstObjectByType<MatchFeedback>();
                if (_instance != null)
                    return _instance;

                var go = new GameObject("MatchFeedback");
                _instance = go.AddComponent<MatchFeedback>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public static void Show(string message, float seconds = -1f)
        {
            Instance.ShowInternal(message, seconds);
        }

        private void ShowInternal(string message, float seconds)
        {
            CurrentMessage = message ?? string.Empty;
            float duration = seconds > 0f ? seconds : defaultSeconds;
            ExpireAt = Time.unscaledTime + duration;
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(CurrentMessage) && Time.unscaledTime >= ExpireAt)
                CurrentMessage = string.Empty;
        }
    }
}
