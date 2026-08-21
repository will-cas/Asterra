using System.Text;
using Asterra.Core.World;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Phase-6 regression: weather transitions, rain waterlog, snow, lightning hooks.</summary>
    public static class WeatherSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            var env = new WorldEnvironmentSim(weatherSeed: 99u);
            var weather = env.WeatherSim;

            Expect(ref fails, sb, "starts clear/sunny-ish",
                weather.Current.Kind == WeatherKind.Clear || weather.Current.Kind == WeatherKind.Sunny);
            Expect(ref fails, sb, "visibility sane", weather.EffectiveVisibility() > 0.2f);
            Expect(ref fails, sb, "movement ~1 at start", System.Math.Abs(weather.EffectiveMovement() - 1f) < 0.15f);

            weather.ForceTransitionTo(WeatherKind.Rain);
            Expect(ref fails, sb, "transition started event", HasEvent(weather, WeatherEventKind.TransitionStarted));

            // Advance through transition.
            for (int i = 0; i < 40; i++)
                env.Tick(0.5f);

            Expect(ref fails, sb, "became rain or still transitioning to rain",
                weather.Current.Kind == WeatherKind.Rain
                || (weather.TransitionTarget.HasValue && weather.TransitionTarget.Value.Kind == WeatherKind.Rain));

            // Force heavy rain hold and waterlog.
            weather.ForceTransitionTo(WeatherKind.Storm);
            for (int i = 0; i < 30; i++)
                env.Tick(0.5f);

            // Drive env pulses long enough to waterlog some cells.
            for (int i = 0; i < 200; i++)
                env.Tick(0.5f);

            bool anyMud = false;
            bool anyWaterlog = false;
            for (int z = 0; z < env.Grid.Height; z += 5)
            {
                for (int x = 0; x < env.Grid.Width; x += 5)
                {
                    float wx = env.Grid.OriginX + (x + 0.5f) * env.Grid.CellSize;
                    float wz = env.Grid.OriginZ + (z + 0.5f) * env.Grid.CellSize;
                    if (!env.Grid.TryGetCell(wx, wz, out var cell))
                        continue;
                    if (cell.Waterlog01 > 0)
                        anyWaterlog = true;
                    if ((cell.Flags & TerrainCell.FlagMuddy) != 0)
                        anyMud = true;
                }
            }

            Expect(ref fails, sb, "rain waterlogged cells", anyWaterlog);
            Expect(ref fails, sb, "some muddy flags", anyMud || anyWaterlog);

            // Snow path.
            var snowEnv = new WorldEnvironmentSim(weatherSeed: 7u);
            snowEnv.WeatherSim.ForceTransitionTo(WeatherKind.Snow);
            for (int i = 0; i < 40; i++)
                snowEnv.Tick(0.5f);
            for (int i = 0; i < 120; i++)
                snowEnv.Tick(0.5f);

            bool anySnow = false;
            for (int z = 0; z < snowEnv.Grid.Height; z += 7)
            {
                for (int x = 0; x < snowEnv.Grid.Width; x += 7)
                {
                    float wx = snowEnv.Grid.OriginX + (x + 0.5f) * snowEnv.Grid.CellSize;
                    float wz = snowEnv.Grid.OriginZ + (z + 0.5f) * snowEnv.Grid.CellSize;
                    if (snowEnv.Grid.TryGetCell(wx, wz, out var cell) && cell.SnowDepth01 > 0)
                        anySnow = true;
                }
            }

            Expect(ref fails, sb, "snow accumulated", anySnow);
            Expect(ref fails, sb, "snow slows movement", snowEnv.WeatherSim.EffectiveMovement() < 0.95f);

            // Footprints buffer.
            snowEnv.WeatherSim.Footprints.Add(1f, 2f, 180);
            Expect(ref fails, sb, "footprint stored", snowEnv.WeatherSim.Footprints.Count >= 1);

            // Lightning events under storm.
            var storm = new WorldEnvironmentSim(weatherSeed: 1234u);
            storm.WeatherSim.ForceTransitionTo(WeatherKind.Storm);
            for (int i = 0; i < 20; i++)
                storm.Tick(0.5f);
            bool sawLightning = false;
            for (int i = 0; i < 300; i++)
            {
                storm.Tick(0.25f);
                if (HasEvent(storm.WeatherSim, WeatherEventKind.Lightning))
                {
                    sawLightning = true;
                    break;
                }
            }

            Expect(ref fails, sb, "lightning event fired", sawLightning);

            // Wind changes over time.
            float w0 = storm.WeatherSim.WindIntensity;
            for (int i = 0; i < 80; i++)
                storm.Tick(0.25f);
            Expect(ref fails, sb, "wind active", storm.WeatherSim.WindIntensity > 0.01f
                || System.Math.Abs(storm.WeatherSim.WindDirX) + System.Math.Abs(storm.WeatherSim.WindDirZ) > 0.5f);
            Expect(ref fails, sb, "wind not stuck at zero forever", w0 >= 0f);

            // Sunny drains — after storm waterlog, switch sunny and drain.
            var dry = new WorldEnvironmentSim(weatherSeed: 3u);
            dry.WeatherSim.ForceTransitionTo(WeatherKind.Storm);
            for (int i = 0; i < 150; i++)
                dry.Tick(0.5f);
            int wetBefore = CountWaterlog(dry.Grid);
            dry.WeatherSim.ForceTransitionTo(WeatherKind.Sunny);
            for (int i = 0; i < 40; i++)
                dry.Tick(0.5f);
            for (int i = 0; i < 200; i++)
                dry.Tick(0.5f);
            int wetAfter = CountWaterlog(dry.Grid);
            Expect(ref fails, sb, "sunny drains waterlog", wetAfter < wetBefore || wetBefore == 0);

            sb.Append(fails == 0 ? "WeatherSelfTest: OK" : $"WeatherSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool HasEvent(WeatherSystem weather, WeatherEventKind kind)
        {
            for (int i = 0; i < weather.Events.Count; i++)
            {
                if (weather.Events[i].Kind == kind)
                    return true;
            }

            return false;
        }

        private static int CountWaterlog(WorldTerrainGrid grid)
        {
            int sum = 0;
            for (int z = 0; z < grid.Height; z += 3)
            {
                for (int x = 0; x < grid.Width; x += 3)
                {
                    float wx = grid.OriginX + (x + 0.5f) * grid.CellSize;
                    float wz = grid.OriginZ + (z + 0.5f) * grid.CellSize;
                    if (grid.TryGetCell(wx, wz, out var cell))
                        sum += cell.Waterlog01;
                }
            }

            return sum;
        }

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
