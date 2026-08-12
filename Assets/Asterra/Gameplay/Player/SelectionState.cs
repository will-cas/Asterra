using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Gameplay.Player
{
    /// <summary>Local-only selection. Never networked.</summary>
    public sealed class SelectionState
    {
        private readonly List<EntityId> _selected = new();

        public IReadOnlyList<EntityId> Selected => _selected;

        public void Clear() => _selected.Clear();

        public void Set(IEnumerable<EntityId> ids)
        {
            _selected.Clear();
            if (ids == null)
                return;
            foreach (var id in ids)
                _selected.Add(id);
        }

        public void Toggle(EntityId id)
        {
            int index = _selected.IndexOf(id);
            if (index >= 0)
                _selected.RemoveAt(index);
            else
                _selected.Add(id);
        }

        public bool Contains(EntityId id) => _selected.Contains(id);
    }
}
