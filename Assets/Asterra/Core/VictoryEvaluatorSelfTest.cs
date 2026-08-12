using System.Text;

namespace Asterra.Core
{
    public static class VictoryEvaluatorSelfTest
    {
        public static string Run()
        {
            var keeps = new[] { "building_iron_keep", "building_heartwood" };
            var eval = new VictoryEvaluator(keeps, requiredHoldSeconds: 2f);
            var world = new FakeWorld();
            world.AddBuilding(new PlayerId(0), "building_iron_keep");
            world.AddBuilding(new PlayerId(1), "building_heartwood");

            var players = new[] { new PlayerId(0), new PlayerId(1) };
            var none = eval.Evaluate(world, 1f, players);
            if (none.IsOver)
                throw new System.InvalidOperationException("Should not end yet.");

            world.ClearBuildings(new PlayerId(1));
            var keepWin = eval.Evaluate(world, 0.1f, players);
            if (!keepWin.IsOver || keepWin.Winner.Value != 0 || keepWin.Reason != MatchEndReason.KeepDestroyed)
                throw new System.InvalidOperationException("Keep victory failed.");

            var holdEval = new VictoryEvaluator(keeps, requiredHoldSeconds: 2f);
            world = new FakeWorld();
            world.AddBuilding(new PlayerId(0), "building_iron_keep");
            world.AddBuilding(new PlayerId(1), "building_heartwood");
            world.SetTerritory(new PlayerId(0));
            var mid = holdEval.Evaluate(world, 1f, players);
            if (mid.IsOver)
                throw new System.InvalidOperationException("Hold ended too early.");
            var holdWin = holdEval.Evaluate(world, 1.5f, players);
            if (!holdWin.IsOver || holdWin.Reason != MatchEndReason.TerritoryHeld)
                throw new System.InvalidOperationException("Territory hold victory failed.");

            var sb = new StringBuilder();
            sb.AppendLine("[Asterra Victory]");
            sb.AppendLine("status=ok");
            return sb.ToString();
        }

        private sealed class FakeWorld : IWorldQuery
        {
            private readonly System.Collections.Generic.List<BuildingSnapshot> _buildings = new();
            private readonly System.Collections.Generic.List<TerritorySnapshot> _territories = new();
            private readonly System.Collections.Generic.List<UnitSnapshot> _units = new();

            public System.Collections.Generic.IReadOnlyList<UnitSnapshot> Units => _units;
            public System.Collections.Generic.IReadOnlyList<BuildingSnapshot> Buildings => _buildings;
            public System.Collections.Generic.IReadOnlyList<TerritorySnapshot> Territories => _territories;

            public bool HasUpgrade(PlayerId player, string upgradeDefId) => false;

            public void AddBuilding(PlayerId owner, string def)
            {
                _buildings.Add(new BuildingSnapshot(
                    new EntityId((uint)(_buildings.Count + 1)),
                    owner,
                    def,
                    0f,
                    0f,
                    BuildingState.Active,
                    true));
            }

            public void ClearBuildings(PlayerId owner)
            {
                _buildings.RemoveAll(b => b.Owner == owner);
            }

            public void SetTerritory(PlayerId controller)
            {
                _territories.Clear();
                _territories.Add(new TerritorySnapshot(
                    new EntityId(99),
                    0f,
                    0f,
                    40f,
                    TerritoryState.Controlled,
                    controller,
                    true,
                    1f));
            }
        }
    }
}
