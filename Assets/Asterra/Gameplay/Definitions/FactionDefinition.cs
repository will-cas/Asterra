using UnityEngine;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Faction Definition", fileName = "Faction_")]
    public sealed class FactionDefinition : ScriptableObject
    {
        public string Id = "faction_id";
        public string DisplayName = "Faction";
        [Range(0, 2)] public byte FactionIndex;
        public string LoreBlurb;
        public UnitDefinition[] StartingUnits;
        public BuildingDefinition[] StartingBuildings;
        public CommanderDefinition DefaultCommander;
        public PowerDefinition[] Powers;
        [Tooltip("When empty, FactionDefaultContent supplies unit/building ids for this index.")]
        public bool PreferCodeRosterFallback = true;
    }
}
