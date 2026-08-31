using Asterra.Core;
using Asterra.Core.World;

namespace Asterra.Gameplay.Content
{
    public static class DefaultDestructibleCatalog
    {
        public const string TreeId = "destructible_tree";
        public const string BridgeId = "destructible_bridge";
        public const string RockId = "destructible_rock";
        public const string FarmId = "scenery_farm";
        public const string CrumblingTowerId = "scenery_crumbling_tower";
        public const string CottageId = "scenery_cottage";
        public const string MillId = "scenery_mill";
        public const string ShrineId = "scenery_shrine";
        public const string BarnId = "scenery_barn";

        public static DestructibleDefData Tree()
        {
            return new DestructibleDefData
            {
                Id = TreeId,
                DisplayName = "Tree",
                MaxHealth = 90f,
                Armor = 0f,
                Resistances = DamageType.Pierce,
                ResistanceFactor = 0.7f,
                BlocksMovement = true,
                BlocksLos = true,
                ClearsTerrainOnDestroy = true,
                ReplaceTerrainDefIndex = DefaultTerrainCatalog.GrassShort,
                ResourceDropType = null,
                ResourceDropAmount = 0,
                FootprintRadius = 5f,
                ProvidesCover = true,
            };
        }

        public static DestructibleDefData Bridge()
        {
            return new DestructibleDefData
            {
                Id = BridgeId,
                DisplayName = "Bridge",
                MaxHealth = 400f,
                Armor = 2f,
                Resistances = DamageType.Slash | DamageType.Pierce,
                ResistanceFactor = 0.55f,
                BlocksMovement = false,
                ClearsTerrainOnDestroy = true,
                // Collapse into river water under the deck.
                ReplaceTerrainDefIndex = DefaultTerrainCatalog.WaterRiver,
                DisableTraversalOnDestroy = true,
                FootprintRadius = 12f,
            };
        }

        public static DestructibleDefData Rock()
        {
            return new DestructibleDefData
            {
                Id = RockId,
                DisplayName = "Boulder",
                MaxHealth = 160f,
                Armor = 3f,
                Resistances = DamageType.Slash | DamageType.Pierce,
                ResistanceFactor = 0.4f,
                BlocksMovement = true,
                ClearsTerrainOnDestroy = true,
                ReplaceTerrainDefIndex = DefaultTerrainCatalog.GrassShort,
                ResourceDropType = ResourceType.Gold,
                ResourceDropAmount = 5,
                FootprintRadius = 6f,
            };
        }

        public static bool IsScenery(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;
            string id = definitionId.ToLowerInvariant();
            return id.StartsWith("scenery_")
                   || id.Contains("farm")
                   || id.Contains("crumbling")
                   || id.Contains("cottage")
                   || id.Contains("mill")
                   || id.Contains("shrine")
                   || id.Contains("barn");
        }

        public static DestructibleDefData FromCatalogId(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId))
                return Rock();
            string id = catalogId.ToLowerInvariant();
            if (id.Contains("tree"))
                return Tree();
            if (id.Contains("bridge"))
                return Bridge();
            if (id.Contains("farm"))
                return Farm();
            if (id.Contains("crumbling") || id.Contains("ruin_tower") || id.Contains("crumbling_tower"))
                return CrumblingTower();
            if (id.Contains("cottage"))
                return Cottage();
            if (id.Contains("mill"))
                return Mill();
            if (id.Contains("shrine"))
                return Shrine();
            if (id.Contains("barn"))
                return Barn();
            if (id.Contains("rock"))
                return Rock();
            if (IsScenery(id))
                return Cottage();
            return Rock();
        }

        public static DestructibleDefData Farm() => Scenery(FarmId, "Farm", 10f);
        public static DestructibleDefData CrumblingTower() => Scenery(CrumblingTowerId, "Crumbling Tower", 8f);
        public static DestructibleDefData Cottage() => Scenery(CottageId, "Cottage", 7f);
        public static DestructibleDefData Mill() => Scenery(MillId, "Mill", 8f);
        public static DestructibleDefData Shrine() => Scenery(ShrineId, "Shrine", 6f);
        public static DestructibleDefData Barn() => Scenery(BarnId, "Barn", 9f);

        private static DestructibleDefData Scenery(string id, string displayName, float radius)
        {
            return new DestructibleDefData
            {
                Id = id,
                DisplayName = displayName,
                MaxHealth = 1000000f,
                Invulnerable = true,
                BlocksMovement = true,
                BlocksLos = true,
                ClearsTerrainOnDestroy = false,
                DisableTraversalOnDestroy = false,
                FootprintRadius = radius,
                ProvidesCover = true,
            };
        }
    }
}
