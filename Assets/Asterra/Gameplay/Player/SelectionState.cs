using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Gameplay.Player
{
    /// <summary>Local-only selection. Never networked.</summary>
    public sealed class SelectionState
    {
        private readonly List<SimEntityId> _selected = new();

        public IReadOnlyList<SimEntityId> Selected => _selected;

        public void Clear() => _selected.Clear();

        public void Set(IEnumerable<SimEntityId> ids)
        {
            _selected.Clear();
            if (ids == null)
                return;
            foreach (var id in ids)
                _selected.Add(id);
        }

        public void Toggle(SimEntityId id)
        {
            int index = _selected.IndexOf(id);
            if (index >= 0)
                _selected.RemoveAt(index);
            else
                _selected.Add(id);
        }

        public bool Contains(SimEntityId id) => _selected.Contains(id);
    }
}
