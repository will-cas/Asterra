using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Asterra.EditorTools
{
    [Overlay(typeof(SceneView), "Asterra Map", true)]
    internal sealed class MapCreatorOverlay : IMGUIOverlay, ITransientOverlay
    {
        public bool visible => MapCreatorWindow.Current != null;

        public override void OnGUI()
        {
            var window = MapCreatorWindow.Current;
            if (window == null)
                return;
            window.DrawCompactTools();
        }
    }
}
