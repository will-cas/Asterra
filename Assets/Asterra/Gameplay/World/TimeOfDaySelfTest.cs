using System.Text;
using Asterra.Core.World;
using Asterra.Gameplay.World;

namespace Asterra.Gameplay
{
    /// <summary>Phase-7 regression: day/night phases, events, visibility coupling.</summary>
    public static class TimeOfDaySelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            var tod = new TimeOfDaySystem(dayLengthSeconds: 100f, startTime01: 0.05f);
            Expect(ref fails, sb, "starts dawn", tod.Phase == TimeOfDayPhase.Dawn);
            Expect(ref fails, sb, "dawn not night", !tod.IsNight);
            Expect(ref fails, sb, "dawn visibility reduced", tod.VisibilityModifier < 1f);

            for (int i = 0; i < 20; i++)
                tod.Tick(0.5f);
            Expect(ref fails, sb, "entered morning", tod.Phase == TimeOfDayPhase.Morning);
            Expect(ref fails, sb, "day event", HasEvent(tod, TimeOfDayEventKind.Day));
            Expect(ref fails, sb, "is day", tod.IsDay);

            tod.SetTime01(0.7f);
            Expect(ref fails, sb, "dusk", tod.Phase == TimeOfDayPhase.Dusk);

            tod.SetTime01(0.85f);
            Expect(ref fails, sb, "night", tod.Phase == TimeOfDayPhase.Night && tod.IsNight);
            Expect(ref fails, sb, "night sun low", tod.SunIntensity < 0.2f);
            Expect(ref fails, sb, "night vis low", tod.VisibilityModifier < 0.7f);
            Expect(ref fails, sb, "night colder", tod.TemperatureBias < 0f);

            tod = new TimeOfDaySystem(50f, 0.74f);
            bool sawNight = false;
            bool sawDawn = false;
            for (int i = 0; i < 80; i++)
            {
                tod.Tick(1f);
                if (HasEvent(tod, TimeOfDayEventKind.Night))
                    sawNight = true;
                if (HasEvent(tod, TimeOfDayEventKind.Dawn))
                    sawDawn = true;
            }

            Expect(ref fails, sb, "night event in cycle", sawNight);
            Expect(ref fails, sb, "dawn event in cycle", sawDawn);
            Expect(ref fails, sb, "phase table dawn", TimeOfDaySystem.PhaseFromTime(0.02f) == TimeOfDayPhase.Dawn);
            Expect(ref fails, sb, "phase table afternoon", TimeOfDaySystem.PhaseFromTime(0.4f) == TimeOfDayPhase.Afternoon);
            Expect(ref fails, sb, "phase table night", TimeOfDaySystem.PhaseFromTime(0.9f) == TimeOfDayPhase.Night);

            var env = new WorldEnvironmentSim(weatherSeed: 1u, dayLengthSeconds: 1000f, startTime01: 0.4f);
            float dayVis = env.CombinedVisibility();
            env.TimeOfDaySim.SetTime01(0.9f);
            float nightVis = env.CombinedVisibility();
            Expect(ref fails, sb, "night darker than day", nightVis < dayVis);

            sb.Append(fails == 0 ? "TimeOfDaySelfTest: OK" : $"TimeOfDaySelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool HasEvent(TimeOfDaySystem tod, TimeOfDayEventKind kind)
        {
            for (int i = 0; i < tod.Events.Count; i++)
            {
                if (tod.Events[i].Kind == kind)
                    return true;
            }

            return false;
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
