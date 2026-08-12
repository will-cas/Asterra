using UnityEngine;
using Asterra.Core;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Resource Definition", fileName = "Resource_")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        public ResourceType Type = ResourceType.Gold;
        public string DisplayName = "Gold";
        public int DefaultNodeAmount = 1000;
        public Sprite Icon;
    }
}
