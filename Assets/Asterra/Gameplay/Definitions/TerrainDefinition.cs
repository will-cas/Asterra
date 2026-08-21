using UnityEngine;
using Asterra.Core.World;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Terrain Definition", fileName = "Terrain_")]
    public sealed class TerrainDefinition : ScriptableObject
    {
        public string Id = "terrain_grass_short";
        public string DisplayName = "Short Grass";
        public TerrainCategory Category = TerrainCategory.GrassShort;
        public float MovementSpeedModifier = 1f;
        public float PathfindingCost = 1f;
        public TraversalCapability RequiredCapabilities = TraversalCapability.Land;
        public float VisibilityModifier = 1f;
        public float SoundNoiseModifier = 1f;
        public float CombatModifier = 1f;
        public bool AllowsBuilding = true;
        public bool AllowsResourceGathering = true;
        public float ResourceGatherModifier = 1f;
        public bool IsDestructible;
        public bool CanChangeAtRuntime = true;
        public float DrainageRate = 1f;
        public float WaterlogSensitivity = 1f;
        public float CoverBonus;
        public float LosBlockFactor;

        public TerrainDefData ToData()
        {
            return new TerrainDefData
            {
                Id = Id,
                DisplayName = DisplayName,
                Category = Category,
                MovementSpeedModifier = MovementSpeedModifier,
                PathfindingCost = PathfindingCost,
                RequiredCapabilities = RequiredCapabilities,
                VisibilityModifier = VisibilityModifier,
                SoundNoiseModifier = SoundNoiseModifier,
                CombatModifier = CombatModifier,
                AllowsBuilding = AllowsBuilding,
                AllowsResourceGathering = AllowsResourceGathering,
                ResourceGatherModifier = ResourceGatherModifier,
                IsDestructible = IsDestructible,
                CanChangeAtRuntime = CanChangeAtRuntime,
                DrainageRate = DrainageRate,
                WaterlogSensitivity = WaterlogSensitivity,
                CoverBonus = CoverBonus,
                LosBlockFactor = LosBlockFactor,
            };
        }
    }
}
