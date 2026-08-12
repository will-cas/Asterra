using UnityEngine;
using Asterra.Core;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Upgrade Definition", fileName = "Upgrade_")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        public string Id = "upgrade_id";
        public string DisplayName = "Upgrade";
        public int GoldCost = 200;
        [Tooltip("Multiplies train duration (< 1 is faster).")]
        public float TrainTimeMultiplier = 1f;
        [Tooltip("Multiplies unit attack damage.")]
        public float UnitDamageMultiplier = 1f;

        public UpgradeDefData ToData()
        {
            return new UpgradeDefData
            {
                Id = Id,
                DisplayName = DisplayName,
                GoldCost = GoldCost,
                TrainTimeMultiplier = TrainTimeMultiplier,
                UnitDamageMultiplier = UnitDamageMultiplier,
            };
        }
    }
}
