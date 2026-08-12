namespace Asterra.Core
{
    public enum UnitRole : byte
    {
        Infantry = 0,
        Ranged = 1,
        Cavalry = 2,
        Siege = 3,
        Builder = 4,
    }

    /// <summary>Plain-data unit stats used by the lockstep sim (SO wrappers copy into these).</summary>
    public sealed class UnitDefData
    {
        public string Id;
        public string DisplayName;
        public float MaxHealth = 100f;
        public float MoveSpeed = 4f;
        public float AttackDamage = 10f;
        public float AttackRange = 2f;
        public float AttackCooldown = 1f;
        public int GoldCost = 50;
        public float TrainSeconds = 5f;
        public bool IsBuilder;
        public bool CanGather;
        public int CarryCapacity = 10;
        public float GatherRate = 4f;
        public UnitRole Role = UnitRole.Infantry;
        public float BuildingDamageMultiplier = 1f;
    }

    public sealed class BuildingDefData
    {
        public string Id;
        public string DisplayName;
        public float MaxHealth = 500f;
        public int GoldCost = 100;
        public int TimberCost = 50;
        public float BuildSeconds = 8f;
        public float FootprintX = 4f;
        public float FootprintZ = 4f;
        public bool CanProduce;
        public string[] TrainableUnitIds = System.Array.Empty<string>();
        public int QueueCapacity = 3;
    }

    public sealed class UpgradeDefData
    {
        public string Id;
        public string DisplayName;
        public int GoldCost = 200;
        public float TrainTimeMultiplier = 1f;
        public float UnitDamageMultiplier = 1f;
    }
}
