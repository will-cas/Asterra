using System;
using System.Collections.Generic;

namespace Asterra.Core.World
{
    /// <summary>
    /// Deterministic day/night clock. Presentation reads lighting fields; sim uses visibility / temperature bias.
    /// Time01 = 0 at dawn start; wraps each <see cref="DayLengthSeconds"/>.
    /// </summary>
    public sealed class TimeOfDaySystem : ITimeOfDaySystem
    {
        private readonly List<TimeOfDayEvent> _events = new();
        private TimeOfDayPhase _phase;
        private float _time01;

        private const float DawnEnd = 0.08f;
        private const float MorningEnd = 0.28f;
        private const float AfternoonEnd = 0.52f;
        private const float EveningEnd = 0.65f;
        private const float DuskEnd = 0.75f;

        public float DayLengthSeconds { get; }
        public float Time01 => _time01;
        public TimeOfDayPhase Phase => _phase;
        public bool IsDay => _phase != TimeOfDayPhase.Night && _phase != TimeOfDayPhase.Dusk;
        public bool IsNight => _phase == TimeOfDayPhase.Night;
        public float SunIntensity { get; private set; } = 1f;
        public float AmbientIntensity { get; private set; } = 0.35f;
        public float ShadowStrength { get; private set; } = 0.85f;
        public float VisibilityModifier { get; private set; } = 1f;
        public float TemperatureBias { get; private set; }
        public float SunDirX { get; private set; }
        public float SunDirY { get; private set; } = 1f;
        public float SunDirZ { get; private set; }
        public IReadOnlyList<TimeOfDayEvent> Events => _events;

        public TimeOfDaySystem(float dayLengthSeconds = 1200f, float startTime01 = 0.3f)
        {
            DayLengthSeconds = dayLengthSeconds > 30f ? dayLengthSeconds : 1200f;
            _time01 = Wrap01(startTime01);
            _phase = PhaseFromTime(_time01);
            RecomputeLighting();
        }

        public void ClearEvents() => _events.Clear();

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || DayLengthSeconds <= 0f)
                return;

            ClearEvents();
            var previous = _phase;
            _time01 = Wrap01(_time01 + deltaSeconds / DayLengthSeconds);
            _phase = PhaseFromTime(_time01);
            RecomputeLighting();

            if (_phase == previous)
                return;

            _events.Add(new TimeOfDayEvent(TimeOfDayEventKind.PhaseChanged, _phase, _time01));
            switch (_phase)
            {
                case TimeOfDayPhase.Dawn:
                    _events.Add(new TimeOfDayEvent(TimeOfDayEventKind.Dawn, _phase, _time01));
                    break;
                case TimeOfDayPhase.Morning:
                    if (previous == TimeOfDayPhase.Dawn)
                        _events.Add(new TimeOfDayEvent(TimeOfDayEventKind.Day, _phase, _time01));
                    break;
                case TimeOfDayPhase.Dusk:
                    _events.Add(new TimeOfDayEvent(TimeOfDayEventKind.Dusk, _phase, _time01));
                    break;
                case TimeOfDayPhase.Night:
                    _events.Add(new TimeOfDayEvent(TimeOfDayEventKind.Night, _phase, _time01));
                    break;
            }
        }

        public void SetTime01(float time01)
        {
            ClearEvents();
            var previous = _phase;
            _time01 = Wrap01(time01);
            _phase = PhaseFromTime(_time01);
            RecomputeLighting();
            if (_phase != previous)
                _events.Add(new TimeOfDayEvent(TimeOfDayEventKind.PhaseChanged, _phase, _time01));
        }

        public static TimeOfDayPhase PhaseFromTime(float time01)
        {
            time01 = Wrap01(time01);
            if (time01 < DawnEnd)
                return TimeOfDayPhase.Dawn;
            if (time01 < MorningEnd)
                return TimeOfDayPhase.Morning;
            if (time01 < AfternoonEnd)
                return TimeOfDayPhase.Afternoon;
            if (time01 < EveningEnd)
                return TimeOfDayPhase.Evening;
            if (time01 < DuskEnd)
                return TimeOfDayPhase.Dusk;
            return TimeOfDayPhase.Night;
        }

        private void RecomputeLighting()
        {
            float dayAngle = _time01 * (float)(Math.PI * 2.0) - (float)(Math.PI * 0.5);
            SunDirX = (float)Math.Cos(dayAngle);
            SunDirZ = (float)Math.Sin(dayAngle) * 0.35f;
            float elev = _phase == TimeOfDayPhase.Night
                ? 0.12f
                : (_phase == TimeOfDayPhase.Dawn || _phase == TimeOfDayPhase.Dusk ? 0.35f : 0.85f);
            SunDirY = elev;
            NormalizeSun();

            switch (_phase)
            {
                case TimeOfDayPhase.Dawn:
                    SunIntensity = 0.45f;
                    AmbientIntensity = 0.28f;
                    ShadowStrength = 0.55f;
                    VisibilityModifier = 0.85f;
                    TemperatureBias = -0.05f;
                    break;
                case TimeOfDayPhase.Morning:
                    SunIntensity = 0.85f;
                    AmbientIntensity = 0.38f;
                    ShadowStrength = 0.8f;
                    VisibilityModifier = 1f;
                    TemperatureBias = 0.05f;
                    break;
                case TimeOfDayPhase.Afternoon:
                    SunIntensity = 1f;
                    AmbientIntensity = 0.42f;
                    ShadowStrength = 0.9f;
                    VisibilityModifier = 1.05f;
                    TemperatureBias = 0.12f;
                    break;
                case TimeOfDayPhase.Evening:
                    SunIntensity = 0.7f;
                    AmbientIntensity = 0.34f;
                    ShadowStrength = 0.75f;
                    VisibilityModifier = 0.95f;
                    TemperatureBias = 0f;
                    break;
                case TimeOfDayPhase.Dusk:
                    SunIntensity = 0.35f;
                    AmbientIntensity = 0.25f;
                    ShadowStrength = 0.45f;
                    VisibilityModifier = 0.7f;
                    TemperatureBias = -0.12f;
                    break;
                default:
                    SunIntensity = 0.08f;
                    AmbientIntensity = 0.16f;
                    ShadowStrength = 0.2f;
                    VisibilityModifier = 0.55f;
                    TemperatureBias = -0.25f;
                    break;
            }
        }

        private void NormalizeSun()
        {
            float len = MathF.Sqrt(SunDirX * SunDirX + SunDirY * SunDirY + SunDirZ * SunDirZ);
            if (len < 0.0001f)
                return;
            SunDirX /= len;
            SunDirY /= len;
            SunDirZ /= len;
        }

        private static float Wrap01(float t)
        {
            t %= 1f;
            if (t < 0f)
                t += 1f;
            return t;
        }
    }
}
