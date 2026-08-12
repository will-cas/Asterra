using UnityEngine;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class CommanderRuntime : MonoBehaviour, ICommander
    {
        [SerializeField] private CommanderDefinition definition;

        public EntityId Id { get; private set; }
        public PlayerId Owner { get; private set; }
        public FactionId Faction { get; private set; }
        public string DefinitionId => definition != null ? definition.Id : string.Empty;
        public int Level { get; private set; } = 1;

        public void Initialize(EntityId id, PlayerId owner, FactionId faction, CommanderDefinition def, int level = 1)
        {
            Id = id;
            Owner = owner;
            Faction = faction;
            definition = def;
            Level = level;
        }

        public void SetLevel(int level) => Level = Mathf.Max(1, level);
    }
}
