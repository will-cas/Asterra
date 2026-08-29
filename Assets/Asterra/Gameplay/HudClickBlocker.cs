using System.Collections.Generic;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>
    /// IMGUI HUD regions that should consume pointer input so world select/orders do not fire.
    /// Call <see cref="Block"/> from OnGUI with GUI-space rects (origin top-left).
    /// Update-time queries use the previous frame's rects (OnGUI runs after Update).
    /// </summary>
    public static class HudClickBlocker
    {
        private static readonly List<Rect> Building = new(32);
        private static readonly List<Rect> Queryable = new(32);
        private static int _buildFrame = -1;
        private static int _publishedFrame = -1;

        public static void BeginFrame()
        {
            // Optional; publishing happens on first Block of a frame / EndFrame.
        }

        public static void Block(Rect guiRect)
        {
            int f = Time.frameCount;
            if (f != _buildFrame)
            {
                // Publish whatever we finished last frame before starting a new build list.
                if (_buildFrame >= 0 && _buildFrame != _publishedFrame)
                    PublishBuildingToQueryable();
                Building.Clear();
                _buildFrame = f;
            }

            if (guiRect.width <= 0f || guiRect.height <= 0f)
                return;
            Building.Add(guiRect);
        }

        /// <summary>Call from a LateUpdate/OnGUI end so Update next frame sees this frame's rects.</summary>
        public static void PublishFrame()
        {
            if (_buildFrame != Time.frameCount)
                return;
            PublishBuildingToQueryable();
        }

        private static void PublishBuildingToQueryable()
        {
            Queryable.Clear();
            for (int i = 0; i < Building.Count; i++)
                Queryable.Add(Building[i]);
            _publishedFrame = _buildFrame;
        }

        /// <summary>Screen-space point from Input.mousePosition (origin bottom-left).</summary>
        public static bool ContainsScreenPoint(Vector3 screenPos)
        {
            // Prefer last published OnGUI rects (always previous-or-current completed frame).
            var list = Queryable;
            // OnGUI rects use top-left origin; Input.mousePosition uses bottom-left.
            var p = new Vector2(screenPos.x, Screen.height - screenPos.y);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Contains(p))
                    return true;
            }

            return false;
        }
    }

    /// <summary>Publishes HUD hit rects after OnGUI so Update can query them next frame.</summary>
    public sealed class HudClickBlockerPublisher : MonoBehaviour
    {
        private void LateUpdate() => HudClickBlocker.PublishFrame();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (FindFirstObjectByType<HudClickBlockerPublisher>() != null)
                return;
            var go = new GameObject("HudClickBlockerPublisher");
            DontDestroyOnLoad(go);
            go.AddComponent<HudClickBlockerPublisher>();
        }
    }
}
