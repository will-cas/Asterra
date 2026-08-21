using Asterra.Core.World;
using Asterra.Gameplay.Audio;
using UnityEngine;

namespace Asterra.Gameplay.Presentation
{
    /// <summary>Drives ambience volume from weather + time-of-day.</summary>
    public sealed class AsterraAmbiencePresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrap match;

        private void Awake()
        {
            if (match == null)
                match = FindFirstObjectByType<MatchBootstrap>();
            _ = AsterraAudio.Instance;
        }

        private void LateUpdate()
        {
            if (match == null || match.World is not SkirmishWorldSim sim)
                return;

            float weather = 0.4f;
            switch (sim.Environment.WeatherSim.Current.Kind)
            {
                case WeatherKind.Rain:
                case WeatherKind.Storm:
                    weather = 0.85f;
                    break;
                case WeatherKind.Snow:
                    weather = 0.55f;
                    break;
                case WeatherKind.Fog:
                    weather = 0.65f;
                    break;
                case WeatherKind.Cloudy:
                    weather = 0.5f;
                    break;
                default:
                    weather = 0.4f;
                    break;
            }

            float tod = 0.55f;
            switch (sim.Environment.TimeOfDaySim.Phase)
            {
                case TimeOfDayPhase.Night:
                    tod = 0.32f;
                    break;
                case TimeOfDayPhase.Dawn:
                case TimeOfDayPhase.Dusk:
                case TimeOfDayPhase.Evening:
                    tod = 0.48f;
                    break;
                case TimeOfDayPhase.Morning:
                case TimeOfDayPhase.Afternoon:
                    tod = 0.7f;
                    break;
            }

            AsterraAudio.Instance.SetAmbienceIntensity(Mathf.Clamp01(weather * 0.65f + tod * 0.35f));
        }
    }
}
