using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class FactionCatalog : IFactionCatalog
    {
        private readonly List<IFaction> _factions = new();

        public FactionCatalog(IEnumerable<FactionDefinition> definitions)
        {
            foreach (var def in definitions)
            {
                if (def == null)
                    continue;
                _factions.Add(new FactionView(def));
            }
        }

        public IFaction Get(FactionId id)
        {
            for (int i = 0; i < _factions.Count; i++)
            {
                if (_factions[i].Id == id)
                    return _factions[i];
            }

            throw new KeyNotFoundException($"Faction {id.Value} not found.");
        }

        public IReadOnlyList<IFaction> All => _factions;

        private sealed class FactionView : IFaction
        {
            private readonly FactionDefinition _def;

            public FactionView(FactionDefinition def) => _def = def;

            public FactionId Id => new FactionId(_def.FactionIndex);
            public string DisplayName => _def.DisplayName;
            public string DefinitionId => _def.Id;
        }
    }
}
