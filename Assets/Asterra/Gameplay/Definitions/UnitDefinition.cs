using UnityEngine;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Unit Definition", fileName = "Unit_")]
    public sealed class UnitDefinition : ScriptableObject
    {
        public string Id = "unit_id";
        public string DisplayName = "Unit";
        public FactionDefinition Faction;
        public float MaxHealth = 100f;
        public float MoveSpeed = 4f;
        public float AttackDamage = 10f;
        public float AttackRange = 2f;
        public float AttackCooldown = 1f;
        public int GoldCost = 50;
        public float TrainSeconds = 5f;
        public GameObject PresentationPrefab;
    }
}
