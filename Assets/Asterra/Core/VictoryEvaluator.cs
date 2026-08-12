using System.Collections.Generic;

namespace Asterra.Core
{
    /// <summary>
    /// 1v1 victory: destroy the enemy keep, or hold the contested territory for a duration.
    /// </summary>
    public sealed class VictoryEvaluator
    {
        private readonly HashSet<string> _keepDefIds = new();
        private readonly Dictionary<byte, float> _holdSeconds = new();
        private readonly float _requiredHoldSeconds;

        public VictoryEvaluator(IEnumerable<string> keepDefinitionIds, float requiredHoldSeconds = 90f)
        {
            _requiredHoldSeconds = requiredHoldSeconds;
            if (keepDefinitionIds != null)
            {
                foreach (var id in keepDefinitionIds)
                {
                    if (!string.IsNullOrEmpty(id))
                        _keepDefIds.Add(id);
                }
            }
        }

        public MatchResult Evaluate(IWorldQuery world, float deltaSeconds, IReadOnlyList<PlayerId> players)
        {
            if (world == null || players == null || players.Count == 0)
                return MatchResult.None;

            // Keep destruction: a player with no remaining keep loses; opponent wins.
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (HasKeep(world, player))
                    continue;

                PlayerId? winner = null;
                for (int j = 0; j < players.Count; j++)
                {
                    if (players[j] == player)
                        continue;
                    if (HasKeep(world, players[j]))
                    {
                        winner = players[j];
                        break;
                    }
                }

                if (winner.HasValue)
                    return new MatchResult(true, winner.Value, MatchEndReason.KeepDestroyed);
            }

            // Territory hold.
            if (world.Territories.Count > 0)
            {
                var territory = world.Territories[0];
                if (territory.HasController && territory.State == TerritoryState.Controlled)
                {
                    byte controller = territory.Controller.Value;
                    if (!_holdSeconds.ContainsKey(controller))
                        _holdSeconds[controller] = 0f;
                    _holdSeconds[controller] += deltaSeconds;

                    // Decay others.
                    var keys = new List<byte>(_holdSeconds.Keys);
                    for (int k = 0; k < keys.Count; k++)
                    {
                        if (keys[k] == controller)
                            continue;
                        _holdSeconds[keys[k]] = 0f;
                    }

                    if (_holdSeconds[controller] >= _requiredHoldSeconds)
                        return new MatchResult(true, territory.Controller, MatchEndReason.TerritoryHeld);
                }
                else
                {
                    _holdSeconds.Clear();
                }
            }

            return MatchResult.None;
        }

        public float GetHoldProgress(PlayerId player)
        {
            return _holdSeconds.TryGetValue(player.Value, out float value)
                ? value / _requiredHoldSeconds
                : 0f;
        }

        private bool HasKeep(IWorldQuery world, PlayerId player)
        {
            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner != player || b.State == BuildingState.Destroyed)
                    continue;
                if (_keepDefIds.Contains(b.DefinitionId))
                    return true;
            }

            return false;
        }
    }
}
