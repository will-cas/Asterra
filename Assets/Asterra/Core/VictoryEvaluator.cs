using System.Collections.Generic;

namespace Asterra.Core
{
    /// <summary>
    /// 1v1 victory: destroy the enemy keep, or hold contested territory for a duration.
    /// Hold progress ticks for the player who controls the most Controlled nodes (any node counts).
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

        public float RequiredHoldSeconds => _requiredHoldSeconds;

        public MatchResult Evaluate(IWorldQuery world, float deltaSeconds, IReadOnlyList<PlayerId> players)
        {
            if (world == null || players == null || players.Count == 0)
                return MatchResult.None;

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

            if (world.Territories.Count > 0)
            {
                if (TryGetLeadingController(world, out byte controller, out _))
                {
                    if (!_holdSeconds.ContainsKey(controller))
                        _holdSeconds[controller] = 0f;
                    _holdSeconds[controller] += deltaSeconds;

                    var keys = new List<byte>(_holdSeconds.Keys);
                    for (int k = 0; k < keys.Count; k++)
                    {
                        if (keys[k] == controller)
                            continue;
                        _holdSeconds[keys[k]] = 0f;
                    }

                    if (_holdSeconds[controller] >= _requiredHoldSeconds)
                        return new MatchResult(true, new PlayerId(controller), MatchEndReason.TerritoryHeld);
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

        public float GetHoldSeconds(PlayerId player)
        {
            return _holdSeconds.TryGetValue(player.Value, out float value) ? value : 0f;
        }

        public void SetHoldSeconds(PlayerId player, float seconds)
        {
            if (seconds <= 0f)
            {
                _holdSeconds.Remove(player.Value);
                return;
            }

            _holdSeconds[player.Value] = seconds;
        }

        /// <summary>Player controlling the most Controlled territories, or false if none / tie.</summary>
        public static bool TryGetLeadingController(IWorldQuery world, out byte controller, out int controlledCount)
        {
            controller = 0;
            controlledCount = 0;
            if (world?.Territories == null || world.Territories.Count == 0)
                return false;

            var counts = new Dictionary<byte, int>();
            for (int i = 0; i < world.Territories.Count; i++)
            {
                var t = world.Territories[i];
                if (!t.HasController || t.State != TerritoryState.Controlled)
                    continue;
                byte c = t.Controller.Value;
                counts.TryGetValue(c, out int n);
                counts[c] = n + 1;
            }

            if (counts.Count == 0)
                return false;

            byte best = 0;
            int bestCount = 0;
            bool tie = false;
            foreach (var pair in counts)
            {
                if (pair.Value > bestCount)
                {
                    best = pair.Key;
                    bestCount = pair.Value;
                    tie = false;
                }
                else if (pair.Value == bestCount)
                {
                    tie = true;
                }
            }

            if (tie || bestCount <= 0)
                return false;

            controller = best;
            controlledCount = bestCount;
            return true;
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
