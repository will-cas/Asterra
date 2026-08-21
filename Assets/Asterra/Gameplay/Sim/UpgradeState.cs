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

        /// <summary>Marks an upgrade unlocked after a timed research that already paid gold.</summary>
        public bool MarkUnlocked(PlayerId player, string upgradeDefId)
        {
            if (Has(player, upgradeDefId))
                return false;
            _unlocked.Add((player.Value, upgradeDefId));
            return true;
        }

        public void CaptureUnlocked(System.Collections.Generic.List<string> into)
        {
            if (into == null)
                return;
            foreach (var key in _unlocked)
                into.Add(key.player + "|" + key.upgrade);
        }

        public float TrainTimeMultiplier(PlayerId player)
        {
            float mult = 1f;
            foreach (var key in _unlocked)
            {
                if (key.player != player.Value)
                    continue;
                if (_defs.TryGetUpgrade(key.upgrade, out var def) && def.Kind == UpgradeKind.Keep)
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

    /// <summary>Tracks which commander powers each player has unlocked.</summary>
    public sealed class PowerState
    {
        private readonly IResourceWallet _wallet;
        private readonly HashSet<(byte player, string power)> _unlocked = new();

        public PowerState(IResourceWallet wallet)
        {
            _wallet = wallet;
        }

        public bool Has(PlayerId player, string powerDefId)
        {
            return _unlocked.Contains((player.Value, powerDefId));
        }

        public bool TryUnlock(PlayerId player, string powerDefId, int goldCost)
        {
            if (Has(player, powerDefId))
                return false;
            if (!_wallet.TrySpend(player, ResourceType.Gold, goldCost))
                return false;
            _unlocked.Add((player.Value, powerDefId));
            return true;
        }

        public bool MarkUnlocked(PlayerId player, string powerDefId)
        {
            if (Has(player, powerDefId))
                return false;
            _unlocked.Add((player.Value, powerDefId));
            return true;
        }

        public void CaptureUnlocked(System.Collections.Generic.List<string> into)
        {
            if (into == null)
                return;
            foreach (var key in _unlocked)
                into.Add(key.player + "|" + key.power);
        }
    }
}
