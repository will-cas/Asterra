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
        }
    }

    /// <summary>Fixed mid-day clock retained for tests / offline tools.</summary>
    public sealed class StaticTimeOfDaySystem : ITimeOfDaySystem
    {
        public float DayLengthSeconds { get; }
        public float Time01 { get; private set; } = 0.5f;
        public TimeOfDayPhase Phase => TimeOfDayPhase.Afternoon;
        public bool IsDay => true;
        public bool IsNight => false;
        public float SunIntensity => 1f;
        public float AmbientIntensity => 0.4f;
        public float ShadowStrength => 0.85f;
        public float VisibilityModifier => 1f;
        public float TemperatureBias => 0.1f;
        public float SunDirX => 0.2f;
        public float SunDirY => 0.9f;
        public float SunDirZ => -0.35f;

        public StaticTimeOfDaySystem(float dayLengthSeconds = 1200f)
        {
            DayLengthSeconds = dayLengthSeconds > 0f ? dayLengthSeconds : 1200f;
        }

        public void Tick(float deltaSeconds)
        {
        }
    }
}
