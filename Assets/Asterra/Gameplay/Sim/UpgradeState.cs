using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Gameplay.Sim
{
    public sealed class UpgradeState : IUpgradeState
    {
        private readonly IResourceWallet _wallet;
        private readonly DefinitionRegistry _defs;
        private readonly HashSet<(byte player, string upgrade)> _unlocked = new();

        public UpgradeState(IResourceWallet wallet, DefinitionRegistry defs)
        {
            _wallet = wallet;
            _defs = defs;
        }

        public bool Has(PlayerId player, string upgradeDefId)
        {
            return _unlocked.Contains((player.Value, upgradeDefId));
        }

        public bool TryUnlock(PlayerId player, string upgradeDefId, int goldCost)
        {
            if (Has(player, upgradeDefId))
                return false;
            if (!_wallet.TrySpend(player, ResourceType.Gold, goldCost))
                return false;
            _unlocked.Add((player.Value, upgradeDefId));
            return true;
        }

        public float TrainTimeMultiplier(PlayerId player)
        {
            float mult = 1f;
            foreach (var key in _unlocked)
            {
                if (key.player != player.Value)
                    continue;
                if (_defs.TryGetUpgrade(key.upgrade, out var def))
                    mult *= def.TrainTimeMultiplier;
            }

            return mult;
        }

        public float UnitDamageMultiplier(PlayerId player)
        {
            float mult = 1f;
            foreach (var key in _unlocked)
            {
                if (key.player != player.Value)
                    continue;
                if (_defs.TryGetUpgrade(key.upgrade, out var def))
                    mult *= def.UnitDamageMultiplier;
            }

            return mult;
        }
    }
}
