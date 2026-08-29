using UnityEngine;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Commander Definition", fileName = "Commander_")]
    public sealed class CommanderDefinition : ScriptableObject
    {
        public string Id = "commander_id";
        public string DisplayName = "Commander";
        public FactionDefinition Faction;
        public string PassiveDescription;
        public string PassivePowerId;
        public string ActiveAbilityId;
        public float ActiveCooldownSeconds = 60f;
        public GameObject PresentationPrefab;
    }
}
