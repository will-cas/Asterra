namespace Asterra.Core.World
{
    /// <summary>Static clear weather until the weather state machine lands.</summary>
    public sealed class StaticWeatherSystem : IWeatherSystem
    {
        private WeatherState _current = new WeatherState(
            WeatherKind.Clear,
            "weather_clear",
            intensity: 0f,
            durationSeconds: float.MaxValue,
            transitionSeconds: 0f,
            remainingSeconds: float.MaxValue,
            visibilityModifier: 1f,
            movementModifier: 1f,
            soundModifier: 1f);

        public WeatherState Current => _current;
        public WeatherState? TransitionTarget => null;
        public float WindDirX { get; private set; } = 1f;
        public float WindDirZ { get; private set; }
        public float WindIntensity { get; private set; }

        public void Tick(float deltaSeconds)
        {
            // No transitions until weather phase.
        }
    }

    /// <summary>Fixed mid-day clock until day/night cycle is implemented.</summary>
    public sealed class StaticTimeOfDaySystem : ITimeOfDaySystem
    {
        public float DayLengthSeconds { get; }
        public float Time01 { get; private set; } = 0.5f;
        public TimeOfDayPhase Phase => TimeOfDayPhase.Afternoon;
        public bool IsDay => true;
        public bool IsNight => false;

        public StaticTimeOfDaySystem(float dayLengthSeconds = 1200f)
        {
            DayLengthSeconds = dayLengthSeconds > 0f ? dayLengthSeconds : 1200f;
        }

        public void Tick(float deltaSeconds)
        {
            // Frozen until Phase 7.
        }
    }
}
