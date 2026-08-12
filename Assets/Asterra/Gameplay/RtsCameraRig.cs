using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Simple RTS camera: WASD/edge pan, scroll zoom.</summary>
    public sealed class RtsCameraRig : MonoBehaviour
    {
        [SerializeField] private Camera rigCamera;
        [SerializeField] private float panSpeed = 160f;
        [SerializeField] private float zoomSpeed = 280f;
        [SerializeField] private float minHeight = 40f;
        [SerializeField] private float maxHeight = 420f;
        [SerializeField] private float edgePanPixels = 12f;
        [SerializeField] private Vector3 lookAt = new Vector3(-320f, 0f, 0f);

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

            rigCamera.fieldOfView = 50f;
            rigCamera.nearClipPlane = 0.3f;
            rigCamera.farClipPlane = 2500f;
            FocusOn(lookAt.x, lookAt.z, height: 150f, back: 200f);
        }

        /// <summary>Point the camera at a ground position (typically the local army).</summary>
        public void FocusOn(float x, float z, float height = 150f, float back = 200f)
        {
            if (rigCamera == null)
                rigCamera = Camera.main;
            if (rigCamera == null)
                return;

            lookAt = new Vector3(x, 0f, z);
            rigCamera.transform.position = new Vector3(x, height, z - back);
            rigCamera.transform.LookAt(lookAt);
        }

        private void Update()
        {
            if (rigCamera == null)
                return;

            var t = rigCamera.transform;
            Vector3 flatForward = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
            Vector3 flatRight = Vector3.ProjectOnPlane(t.right, Vector3.up).normalized;
            Vector3 move = Vector3.zero;

            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.mousePosition.y >= Screen.height - edgePanPixels)
                move += flatForward;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.mousePosition.y <= edgePanPixels)
                move -= flatForward;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.mousePosition.x >= Screen.width - edgePanPixels)
                move += flatRight;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.mousePosition.x <= edgePanPixels)
                move -= flatRight;

            if (move.sqrMagnitude > 0f)
                t.position += move.normalized * (panSpeed * Time.deltaTime);

            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 pos = t.position;
                pos += t.forward * (scroll * zoomSpeed * Time.deltaTime);
                pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
                t.position = pos;
            }
        }
    }
}
