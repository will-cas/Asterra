using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Near-top-down RTS camera: WASD/arrows/edge pan, scroll zoom.</summary>
    public sealed class RtsCameraRig : MonoBehaviour
    {
        [SerializeField] private Camera rigCamera;
        [SerializeField] private float panSpeed = 180f;
        [SerializeField] private float zoomSpeed = 320f;
        [SerializeField] private float minHeight = 80f;
        [SerializeField] private float maxHeight = 480f;
        [SerializeField] private float edgePanPixels = 14f;
        [SerializeField] private Vector3 lookAt = new Vector3(-320f, 0f, 0f);
        [SerializeField] private float defaultHeight = 240f;
        [SerializeField] private float defaultBack = 42f;

        private void Awake()
        {
            if (rigCamera == null)
                rigCamera = Camera.main;
            if (rigCamera == null)
            {
                var go = new GameObject("AsterraCamera");
                rigCamera = go.AddComponent<Camera>();
                go.tag = "MainCamera";
                go.AddComponent<AudioListener>();
            }

            rigCamera.fieldOfView = 48f;
            rigCamera.nearClipPlane = 0.3f;
            rigCamera.farClipPlane = 3500f;
            FocusOn(lookAt.x, lookAt.z, defaultHeight, defaultBack);
        }

        /// <summary>Point the camera at a ground position with a steep look-down angle.</summary>
        public void FocusOn(float x, float z, float height = 240f, float back = 42f)
        {
            if (rigCamera == null)
                rigCamera = Camera.main;
            if (rigCamera == null)
                return;

            lookAt = new Vector3(x, 0f, z);
            ApplyPose(height, back);
        }

        private void Update()
        {
            if (rigCamera == null)
                return;

            var t = rigCamera.transform;
            Vector3 flatForward = Vector3.forward;
            Vector3 flatRight = Vector3.right;
            Vector3 move = Vector3.zero;

            bool panNorth = UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)
                            || UnityEngine.Input.mousePosition.y >= Screen.height - edgePanPixels;
            bool panSouth = UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)
                            || UnityEngine.Input.mousePosition.y <= edgePanPixels;
            bool panEast = UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)
                           || UnityEngine.Input.mousePosition.x >= Screen.width - edgePanPixels;
            bool panWest = UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)
                           || UnityEngine.Input.mousePosition.x <= edgePanPixels;

            if (panNorth) move += flatForward;
            if (panSouth) move -= flatForward;
            if (panEast) move += flatRight;
            if (panWest) move -= flatRight;

            if (move.sqrMagnitude > 0f)
            {
                Vector3 delta = move.normalized * (panSpeed * Time.deltaTime);
                lookAt += delta;
                lookAt.x = Mathf.Clamp(lookAt.x, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
                lookAt.z = Mathf.Clamp(lookAt.z, -MapBounds.PlayableHalfExtent, MapBounds.PlayableHalfExtent);
            }

            float height = t.position.y;
            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                height = Mathf.Clamp(height - scroll * zoomSpeed * Time.deltaTime, minHeight, maxHeight);

            float back = Mathf.Max(18f, height * 0.175f);
            ApplyPose(height, back);
        }

        private void ApplyPose(float height, float back)
        {
            rigCamera.transform.position = new Vector3(lookAt.x, height, lookAt.z - back);
            rigCamera.transform.LookAt(lookAt);
        }
    }
}
