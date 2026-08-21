using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.Gameplay.Sim
{
    public sealed class DefinitionRegistry
    {
        private readonly Dictionary<string, UnitDefData> _units = new();
        private readonly Dictionary<string, BuildingDefData> _buildings = new();
        private readonly Dictionary<string, UpgradeDefData> _upgrades = new();
        private readonly Dictionary<string, PowerDefData> _powers = new();

        public void Register(UnitDefData def) => _units[def.Id] = def;
        public void Register(BuildingDefData def) => _buildings[def.Id] = def;
        public void Register(UpgradeDefData def) => _upgrades[def.Id] = def;
        public void Register(PowerDefData def) => _powers[def.Id] = def;

        public bool TryGetUnit(string id, out UnitDefData def) => _units.TryGetValue(id, out def);
        public bool TryGetBuilding(string id, out BuildingDefData def) => _buildings.TryGetValue(id, out def);
        public bool TryGetUpgrade(string id, out UpgradeDefData def) => _upgrades.TryGetValue(id, out def);
        public bool TryGetPower(string id, out PowerDefData def) => _powers.TryGetValue(id, out def);
    }
}
