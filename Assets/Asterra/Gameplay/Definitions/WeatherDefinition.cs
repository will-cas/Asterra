using UnityEngine;
using Asterra.Core.World;

namespace Asterra.Gameplay
{
    [CreateAssetMenu(menuName = "Asterra/Weather Definition", fileName = "Weather_")]
    public sealed class WeatherDefinition : ScriptableObject
    {
        public string Id = "weather_clear";
        public string DisplayName = "Clear";
        public WeatherKind Kind = WeatherKind.Clear;
        public float DefaultIntensity = 0.5f;
        public float MinDurationSeconds = 30f;
        public float MaxDurationSeconds = 120f;
        public float TransitionSeconds = 8f;
        public float VisibilityModifier = 1f;
        public float MovementModifier = 1f;
        public float SoundModifier = 1f;
        public float TemperatureDelta;
        public float PrecipitationRate;
        public float SnowfallRate;
        public bool EnablesLightning;
        public float LightningChancePerSecond;

        public WeatherDefData ToData()
        {
            return new WeatherDefData
            {
                Id = Id,
                DisplayName = DisplayName,
                Kind = Kind,
                DefaultIntensity = DefaultIntensity,
                MinDurationSeconds = MinDurationSeconds,
                MaxDurationSeconds = MaxDurationSeconds,
                TransitionSeconds = TransitionSeconds,
                VisibilityModifier = VisibilityModifier,
                MovementModifier = MovementModifier,
                SoundModifier = SoundModifier,
                TemperatureDelta = TemperatureDelta,
                PrecipitationRate = PrecipitationRate,
                SnowfallRate = SnowfallRate,
                EnablesLightning = EnablesLightning,
                LightningChancePerSecond = LightningChancePerSecond,
            };
        }
    }
}
