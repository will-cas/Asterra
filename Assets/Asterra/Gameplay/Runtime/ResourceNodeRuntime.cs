using UnityEngine;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class ResourceNodeRuntime : MonoBehaviour, IResourceNode
    {
        [SerializeField] private ResourceType type = ResourceType.Gold;
        [SerializeField] private int remaining = 1000;

        public SimEntityId Id { get; private set; }
        public ResourceType Type => type;
        public int Remaining => remaining;
        public bool IsDepleted => remaining <= 0;

        public void Initialize(SimEntityId id, ResourceType resourceType, int amount)
        {
            Id = id;
            type = resourceType;
            remaining = amount;
        }

        public int Extract(int requested)
        {
            int taken = Mathf.Min(requested, remaining);
            remaining -= taken;
            return taken;
        }
    }
}
