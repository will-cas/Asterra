using System;
using System.Collections.Generic;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    public readonly struct ConversationBeat
    {
        public readonly string Speaker;
        public readonly string Text;

        public ConversationBeat(string speaker, string text)
        {
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
        }
    }

    public readonly struct ObjectiveHudRow
    {
        public readonly string Title;
        public readonly bool Required;
        public readonly bool Complete;
        public readonly float Progress;

        public ObjectiveHudRow(string title, bool required, bool complete, float progress)
        {
            Title = title;
            Required = required;
            Complete = complete;
            Progress = progress;
        }
    }

    /// <summary>Map-authored objectives, talk triggers, and a conversation queue.</summary>
    public sealed class MapScriptRuntime
    {
        public const string KindDestroyKeeps = "destroy_keeps";
        public const string KindHold = "hold";
        public const string KindOptionalHold = "optional_hold";
        public const string KindReach = "reach";
        public const string KindDestroyNear = "destroy_near";
        public const string KindSurvive = "survive";
        public const string KindProtect = "protect";

        private MapDefinition _map;
        private PlayerId _local;
        private readonly bool[] _objDone = new bool[32];
        private readonly bool[] _triggerDone = new bool[32];
        private readonly Queue<ConversationBeat> _talk = new();
        private ConversationBeat _current;
        private bool _hasCurrent;
        private bool _started;
        private bool _optionalHoldDone;
        private readonly float[] _surviveAcc = new float[32];
        private readonly bool[] _protectSeen = new bool[32];
        private bool _customVictory;
        private bool _customDefeat;

        public bool OptionalHoldComplete => _optionalHoldDone;
        public bool HasTalk => _hasCurrent;
        public ConversationBeat CurrentTalk => _current;

        public void Bind(MapDefinition map, PlayerId local)
        {
            _map = map;
            _local = local;
            _started = false;
            _optionalHoldDone = false;
            _customVictory = false;
            _customDefeat = false;
            _hasCurrent = false;
            _talk.Clear();
            Array.Clear(_objDone, 0, _objDone.Length);
            Array.Clear(_triggerDone, 0, _triggerDone.Length);
            Array.Clear(_surviveAcc, 0, _surviveAcc.Length);
            Array.Clear(_protectSeen, 0, _protectSeen.Length);
            map?.EnsureArrays();
        }

        public void EnqueueTalk(string speaker, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            var beat = new ConversationBeat(speaker, text);
            if (!_hasCurrent)
            {
                _current = beat;
                _hasCurrent = true;
            }
            else
            {
                _talk.Enqueue(beat);
            }
        }

        public void EnqueueConversation(string id)
        {
            if (_map?.conversations == null || string.IsNullOrEmpty(id))
                return;
            for (int i = 0; i < _map.conversations.Length; i++)
            {
                var line = _map.conversations[i];
                if (line != null && line.id == id)
                    EnqueueTalk(line.speaker, line.text);
            }
        }

        public bool AdvanceTalk()
        {
            if (_talk.Count > 0)
            {
                _current = _talk.Dequeue();
                _hasCurrent = true;
                return true;
            }

            _hasCurrent = false;
            _current = default;
            return false;
        }

        public void Tick(IWorldQuery world, VictoryEvaluator victory, float dt)
        {
            if (_map == null || world == null)
                return;
            if (!_started)
            {
                _started = true;
                FireTriggers("start", world);
            }

            FireTriggers("enter", world);
            UpdateObjectives(world, victory, dt);
        }

        public bool TryCustomVictory(out MatchResult result)
        {
            result = MatchResult.None;
            if (!_customVictory)
                return false;
            result = new MatchResult(true, _local, MatchEndReason.ObjectivesComplete);
            return true;
        }

        public bool TryCustomDefeat(out MatchResult result)
        {
            result = MatchResult.None;
            if (!_customDefeat)
                return false;
            result = new MatchResult(true, RivalOf(_local), MatchEndReason.ObjectiveFailed);
            return true;
        }

        private static PlayerId RivalOf(PlayerId local)
        {
            return new PlayerId(local.Value == 0 ? (byte)1 : (byte)0);
        }

        public int CopyHudRows(ObjectiveHudRow[] dest)
        {
            if (dest == null || _map?.objectives == null)
                return 0;
            int n = 0;
            for (int i = 0; i < _map.objectives.Length && n < dest.Length; i++)
            {
                var o = _map.objectives[i];
                if (o == null)
                    continue;
                dest[n++] = new ObjectiveHudRow(
                    string.IsNullOrEmpty(o.title) ? o.kind : o.title,
                    o.required,
                    i < _objDone.Length && _objDone[i],
                    ProgressOf(i, o));
            }

            return n;
        }

        private float ProgressOf(int index, MapObjective o)
        {
            if (index < _objDone.Length && _objDone[index])
                return 1f;
            string kind = (o.kind ?? "").ToLowerInvariant();
            if (kind == KindSurvive && index < _surviveAcc.Length)
            {
                float need = o.holdSeconds > 0.05f ? o.holdSeconds : 90f;
                return MathfClamp01(_surviveAcc[index] / need);
            }

            if (kind == KindProtect && index < _protectSeen.Length && _protectSeen[index])
                return 0.5f;
            return 0f;
        }

        private static float MathfClamp01(float v)
        {
            if (v < 0f)
                return 0f;
            if (v > 1f)
                return 1f;
            return v;
        }

        private void UpdateObjectives(IWorldQuery world, VictoryEvaluator victory, float dt)
        {
            if (_map.objectives == null)
                return;
            bool allRequired = true;
            int requiredCount = 0;
            bool usesKeepOrHold = false;
            for (int i = 0; i < _map.objectives.Length; i++)
            {
                var o = _map.objectives[i];
                if (o == null)
                    continue;
                string kind = (o.kind ?? KindDestroyKeeps).ToLowerInvariant();
                if (kind == KindDestroyKeeps || kind == KindHold)
                    usesKeepOrHold = true;
                if (kind == KindSurvive && i < _surviveAcc.Length && !(_objDone[i]))
                    _surviveAcc[i] += dt;
                if (kind == KindProtect)
                    UpdateProtect(i, o, world);

                bool done = i < _objDone.Length && _objDone[i];
                if (!done)
                {
                    done = Evaluate(kind, o, world, victory, i);
                    if (done && i < _objDone.Length)
                    {
                        _objDone[i] = true;
                        EnqueueConversation(o.onCompleteTalkId);
                        FireObjectiveTriggers(o.id);
                        if (kind == KindOptionalHold)
                            _optionalHoldDone = true;
                    }
                }

                bool failConstraint = kind == KindProtect;
                if (o.required && !failConstraint)
                {
                    requiredCount++;
                    if (!done)
                        allRequired = false;
                }
            }

            if (requiredCount > 0 && allRequired && !usesKeepOrHold)
                _customVictory = true;
        }

        private bool Evaluate(string kind, MapObjective o, IWorldQuery world, VictoryEvaluator victory, int index)
        {
            switch (kind)
            {
                case KindOptionalHold:
                case KindHold:
                    return victory != null && victory.GetHoldProgress(_local) >= 0.999f;
                case KindReach:
                    return LocalUnitInRadius(world, o.x, o.z, o.radius > 1f ? o.radius : 28f);
                case KindDestroyNear:
                    return NoDestructiblesInRadius(world, o.x, o.z, o.radius > 1f ? o.radius : 28f);
                case KindSurvive:
                    float need = o.holdSeconds > 0.05f ? o.holdSeconds : 90f;
                    return index < _surviveAcc.Length && _surviveAcc[index] >= need;
                case KindProtect:
                    return false;
                case KindDestroyKeeps:
                    return false;
                default:
                    return false;
            }
        }

        private void UpdateProtect(int index, MapObjective o, IWorldQuery world)
        {
            if (index >= _protectSeen.Length)
                return;
            float r = o.radius > 1f ? o.radius : 36f;
            bool alive = LocalBuildingAliveInRadius(world, o.x, o.z, r);
            if (alive)
                _protectSeen[index] = true;
            else if (_protectSeen[index] && o.required)
                _customDefeat = true;
        }

        private bool LocalBuildingAliveInRadius(IWorldQuery world, float x, float z, float radius)
        {
            if (world.Buildings == null)
                return false;
            float r2 = radius * radius;
            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.Owner != _local || b.State == BuildingState.Destroyed)
                    continue;
                float dx = b.X - x;
                float dz = b.Z - z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }

        private bool LocalUnitInRadius(IWorldQuery world, float x, float z, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < world.Units.Count; i++)
            {
                var u = world.Units[i];
                if (u.Owner != _local || !u.IsAlive)
                    continue;
                float dx = u.X - x;
                float dz = u.Z - z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }

        private static bool NoDestructiblesInRadius(IWorldQuery world, float x, float z, float radius)
        {
            if (world.Destructibles == null || world.Destructibles.Count == 0)
                return true;
            float r2 = radius * radius;
            bool any = false;
            for (int i = 0; i < world.Destructibles.Count; i++)
            {
                var d = world.Destructibles[i];
                float dx = d.X - x;
                float dz = d.Z - z;
                if (dx * dx + dz * dz > r2)
                    continue;
                any = true;
                if (d.State != DestructibleState.Destroyed)
                    return false;
            }

            return any;
        }

        private void FireTriggers(string when, IWorldQuery world)
        {
            if (_map.talkTriggers == null)
                return;
            for (int i = 0; i < _map.talkTriggers.Length; i++)
            {
                if (i < _triggerDone.Length && _triggerDone[i])
                    continue;
                var t = _map.talkTriggers[i];
                if (t == null || !string.Equals(t.when, when, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (when == "enter"
                    && !LocalUnitInRadius(world, t.x, t.z, t.radius > 1f ? t.radius : 28f))
                    continue;
                if (i < _triggerDone.Length)
                    _triggerDone[i] = true;
                EnqueueConversation(t.conversationId);
            }
        }

        private void FireObjectiveTriggers(string objectiveId)
        {
            if (_map.talkTriggers == null || string.IsNullOrEmpty(objectiveId))
                return;
            for (int i = 0; i < _map.talkTriggers.Length; i++)
            {
                var t = _map.talkTriggers[i];
                if (t == null || !string.Equals(t.when, "objective", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (t.objectiveId != objectiveId)
                    continue;
                if (i < _triggerDone.Length && _triggerDone[i])
                    continue;
                if (i < _triggerDone.Length)
                    _triggerDone[i] = true;
                EnqueueConversation(t.conversationId);
            }
        }
    }
}
