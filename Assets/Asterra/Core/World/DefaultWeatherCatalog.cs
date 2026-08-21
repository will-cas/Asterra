using Asterra.Core.World;

namespace Asterra.Core.World
{
    /// <summary>Built-in weather defs. Append-only indices for deterministic catalogs.</summary>
    public static class DefaultWeatherCatalog
    {
        public static WeatherDefData[] CreateDefs()
        {
            return new[]
            {
                new WeatherDefData
                {
                    Id = "weather_clear",
                    DisplayName = "Clear",
                    Kind = WeatherKind.Clear,
                    DefaultIntensity = 0.2f,
                    MinDurationSeconds = 40f,
                    MaxDurationSeconds = 90f,
                    TransitionSeconds = 6f,
                    VisibilityModifier = 1f,
                    MovementModifier = 1f,
                    SoundModifier = 1f,
                    TemperatureDelta = 0.1f,
                },
                new WeatherDefData
                {
                    Id = "weather_sunny",
                    DisplayName = "Sunny",
                    Kind = WeatherKind.Sunny,
                    DefaultIntensity = 0.85f,
                    MinDurationSeconds = 35f,
                    MaxDurationSeconds = 80f,
                    TransitionSeconds = 8f,
                    VisibilityModifier = 1.1f,
                    MovementModifier = 1f,
                    SoundModifier = 0.95f,
                    TemperatureDelta = 0.35f,
                },
                new WeatherDefData
                {
                    Id = "weather_cloudy",
                    DisplayName = "Cloudy",
                    Kind = WeatherKind.Cloudy,
                    DefaultIntensity = 0.45f,
                    MinDurationSeconds = 25f,
                    MaxDurationSeconds = 70f,
                    TransitionSeconds = 7f,
                    VisibilityModifier = 0.92f,
                    MovementModifier = 1f,
                    SoundModifier = 1f,
                    TemperatureDelta = -0.05f,
                },
                new WeatherDefData
                {
                    Id = "weather_rain_light",
                    DisplayName = "Light Rain",
                    Kind = WeatherKind.Rain,
                    DefaultIntensity = 0.35f,
                    MinDurationSeconds = 30f,
                    MaxDurationSeconds = 75f,
                    TransitionSeconds = 10f,
                    VisibilityModifier = 0.85f,
                    MovementModifier = 0.96f,
                    SoundModifier = 1.15f,
                    TemperatureDelta = -0.1f,
                    PrecipitationRate = 0.35f,
                },
                new WeatherDefData
                {
                    Id = "weather_rain_heavy",
                    DisplayName = "Heavy Rain",
                    Kind = WeatherKind.Rain,
                    DefaultIntensity = 0.85f,
                    MinDurationSeconds = 25f,
                    MaxDurationSeconds = 60f,
                    TransitionSeconds = 12f,
                    VisibilityModifier = 0.65f,
                    MovementModifier = 0.88f,
                    SoundModifier = 1.4f,
                    TemperatureDelta = -0.15f,
                    PrecipitationRate = 1f,
                },
                new WeatherDefData
                {
                    Id = "weather_snow",
                    DisplayName = "Snow",
                    Kind = WeatherKind.Snow,
                    DefaultIntensity = 0.6f,
                    MinDurationSeconds = 35f,
                    MaxDurationSeconds = 90f,
                    TransitionSeconds = 14f,
                    VisibilityModifier = 0.7f,
                    MovementModifier = 0.8f,
                    SoundModifier = 0.7f,
                    TemperatureDelta = -0.55f,
                    SnowfallRate = 0.8f,
                },
                new WeatherDefData
                {
                    Id = "weather_fog",
                    DisplayName = "Fog",
                    Kind = WeatherKind.Fog,
                    DefaultIntensity = 0.7f,
                    MinDurationSeconds = 20f,
                    MaxDurationSeconds = 55f,
                    TransitionSeconds = 9f,
                    VisibilityModifier = 0.45f,
                    MovementModifier = 0.98f,
                    SoundModifier = 0.85f,
                    TemperatureDelta = -0.05f,
                },
                new WeatherDefData
                {
                    Id = "weather_storm",
                    DisplayName = "Storm",
                    Kind = WeatherKind.Storm,
                    DefaultIntensity = 0.95f,
                    MinDurationSeconds = 20f,
                    MaxDurationSeconds = 50f,
                    TransitionSeconds = 10f,
                    VisibilityModifier = 0.5f,
                    MovementModifier = 0.82f,
                    SoundModifier = 1.6f,
                    TemperatureDelta = -0.2f,
                    PrecipitationRate = 1.2f,
                    EnablesLightning = true,
                    LightningChancePerSecond = 0.08f,
                },
            };
        }
    }
}
