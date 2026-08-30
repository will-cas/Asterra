using Asterra.Core;
using Asterra.Core.World;

namespace Asterra.Gameplay.Content
{
    public static class DefaultDestructibleCatalog
    {
        public const string TreeId = "destructible_tree";
        public const string BridgeId = "destructible_bridge";
        public const string RockId = "destructible_rock";

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
    }
}
