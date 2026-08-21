using UnityEngine;
using Asterra.Core;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Building Definition", fileName = "Building_")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        public string Id = "building_id";
        public string DisplayName = "Building";
        public FactionDefinition Faction;
        public float MaxHealth = 500f;
        public int GoldCost = 100;
        public int TimberCost = 50;
        public float BuildSeconds = 8f;
        public Vector2 Footprint = new Vector2(4f, 4f);
        public bool CanProduce;
        public UnitDefinition[] TrainableUnits;
        public int QueueCapacity = 3;
        public BuildingKind Kind = BuildingKind.Generic;
        public BuildingCategory Category = BuildingCategory.Special;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown = 1.5f;
        public float SightRadius;
        public int GoldPerSecond;
        public bool AllowsGarrison;
        public int GarrisonCapacity;
        public float CommandRadius;
        public bool SnapToWallGrid;
        public float WallSegmentLength = 14f;
        public GameObject PresentationPrefab;

        public BuildingDefData ToData()
        {
            string[] trainIds = System.Array.Empty<string>();
            if (TrainableUnits != null && TrainableUnits.Length > 0)
            {
                trainIds = new string[TrainableUnits.Length];
                for (int i = 0; i < TrainableUnits.Length; i++)
                    trainIds[i] = TrainableUnits[i] != null ? TrainableUnits[i].Id : string.Empty;
            }

            return new BuildingDefData
            {
                Id = Id,
                DisplayName = DisplayName,
                MaxHealth = MaxHealth,
                GoldCost = GoldCost,
                TimberCost = TimberCost,
                BuildSeconds = BuildSeconds,
                FootprintX = Footprint.x,
                FootprintZ = Footprint.y,
                CanProduce = CanProduce,
                TrainableUnitIds = trainIds,
                QueueCapacity = QueueCapacity,
                Kind = Kind,
                Category = Category,
                AttackDamage = AttackDamage,
                AttackRange = AttackRange,
                AttackCooldown = AttackCooldown,
                SightRadius = SightRadius,
                GoldPerSecond = GoldPerSecond,
                AllowsGarrison = AllowsGarrison,
                GarrisonCapacity = GarrisonCapacity,
                CommandRadius = CommandRadius,
                SnapToWallGrid = SnapToWallGrid,
                WallSegmentLength = WallSegmentLength,
            };
        }
    }
}
