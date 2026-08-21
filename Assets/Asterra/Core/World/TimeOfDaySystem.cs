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

            // Continuous solar elevation (peaks mid-afternoon, soft through dawn/dusk).
            float elev = SampleDayCurve(_time01,
                dawn: 0.28f, morning: 0.7f, afternoon: 0.95f, evening: 0.55f, dusk: 0.28f, night: 0.1f);
            SunDirY = elev;
            NormalizeSun();

            SunIntensity = SampleDayCurve(_time01,
                dawn: 0.4f, morning: 0.85f, afternoon: 1f, evening: 0.68f, dusk: 0.32f, night: 0.08f);
            AmbientIntensity = SampleDayCurve(_time01,
                dawn: 0.27f, morning: 0.38f, afternoon: 0.42f, evening: 0.34f, dusk: 0.24f, night: 0.16f);
            ShadowStrength = SampleDayCurve(_time01,
                dawn: 0.5f, morning: 0.8f, afternoon: 0.9f, evening: 0.72f, dusk: 0.42f, night: 0.2f);
            VisibilityModifier = SampleDayCurve(_time01,
                dawn: 0.85f, morning: 1f, afternoon: 1.05f, evening: 0.95f, dusk: 0.7f, night: 0.55f);
            TemperatureBias = SampleDayCurve(_time01,
                dawn: -0.05f, morning: 0.05f, afternoon: 0.12f, evening: 0f, dusk: -0.12f, night: -0.25f);
        }

        /// <summary>
        /// Smoothstep across phase boundaries so lighting never hard-snaps at dawn/dusk/etc.
        /// </summary>
        private static float SampleDayCurve(
            float time01,
            float dawn,
            float morning,
            float afternoon,
            float evening,
            float dusk,
            float night)
        {
            time01 = Wrap01(time01);
            // Keys match PhaseFromTime boundaries.
            if (time01 < DawnEnd)
                return Lerp(night, dawn, Smooth01(time01 / DawnEnd));
            if (time01 < MorningEnd)
                return Lerp(dawn, morning, Smooth01((time01 - DawnEnd) / (MorningEnd - DawnEnd)));
            if (time01 < AfternoonEnd)
                return Lerp(morning, afternoon, Smooth01((time01 - MorningEnd) / (AfternoonEnd - MorningEnd)));
            if (time01 < EveningEnd)
                return Lerp(afternoon, evening, Smooth01((time01 - AfternoonEnd) / (EveningEnd - AfternoonEnd)));
            if (time01 < DuskEnd)
                return Lerp(evening, dusk, Smooth01((time01 - EveningEnd) / (DuskEnd - EveningEnd)));
            // Night wraps softly into next dawn; reach night levels by mid-night segment.
            float nightT = (time01 - DuskEnd) / (1f - DuskEnd);
            return Lerp(dusk, night, Smooth01(Math.Min(1f, nightT * 2f)));
        }

        private static float Smooth01(float t)
        {
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;
            return t * t * (3f - 2f * t);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

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
