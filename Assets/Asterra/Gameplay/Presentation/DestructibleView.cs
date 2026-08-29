using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Clickable world prop for trees / rocks / bridges (sim destructibles).</summary>
    public sealed class DestructibleView : MonoBehaviour
    {
        public SimEntityId Id { get; private set; }
        public string DefinitionId { get; private set; }

        private MeshRenderer _renderer;
        private Color _baseColor;

        public void Initialize(SimEntityId id, string definitionId, Color baseColor)
        {
            Id = id;
            DefinitionId = definitionId;
            _baseColor = baseColor;
            _renderer = GetComponent<MeshRenderer>();
        }

        public void SetDamaged(bool damaged)
        {
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
