using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Clickable resource node for gather orders.</summary>
    public sealed class ResourceNodeView : MonoBehaviour
    {
        public SimEntityId Id { get; private set; }
        public ResourceType Type { get; private set; }

        public void Initialize(SimEntityId id, ResourceType type)
        {
            Id = id;
            Type = type;
        }
    }
}
