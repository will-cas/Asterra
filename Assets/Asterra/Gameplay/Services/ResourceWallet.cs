using System;
using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class ResourceWallet : IResourceWallet
    {
        private readonly Dictionary<(byte player, ResourceType type), int> _balances = new();

        public int Get(PlayerId player, ResourceType type)
        {
            return _balances.TryGetValue((player.Value, type), out int value) ? value : 0;
        }

        public bool CanAfford(PlayerId player, ResourceType type, int amount)
        {
            return amount <= 0 || Get(player, type) >= amount;
        }

        public bool TrySpend(PlayerId player, ResourceType type, int amount)
        {
            if (!CanAfford(player, type, amount))
                return false;
            if (amount > 0)
                _balances[(player.Value, type)] = Get(player, type) - amount;
            return true;
        }

        public void Add(PlayerId player, ResourceType type, int amount)
        {
            if (amount == 0)
                return;
            _balances[(player.Value, type)] = Get(player, type) + amount;
        }

        public void Seed(PlayerId player, ResourceType type, int amount)
        {
            _balances[(player.Value, type)] = Math.Max(0, amount);
        }
    }
}
