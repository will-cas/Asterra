using UnityEngine;

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
        public GameObject PresentationPrefab;
    }
}
