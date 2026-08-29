using System.Collections.Generic;
using System.Text;
using Asterra.AI;
using Asterra.Core;

namespace Asterra.Gameplay
{
    public static class ArmyGroupSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "guard scores high under attack", GuardHighUnderAttack());
            Expect(ref fails, sb, "harass disabled when not allowed", HarassDisabled());
            Expect(ref fails, sb, "allocate splits combat", AllocateSplits());

            sb.Append(fails == 0 ? "ArmyGroupSelfTest: OK" : $"ArmyGroupSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool GuardHighUnderAttack()
        {
            var calm = new ArmyGroupUtility.Sense(10, 0, false, true, 1f, 3, 2, 6, true);
            var hot = new ArmyGroupUtility.Sense(10, 5, true, true, 1f, 3, 2, 6, true);
            return ArmyGroupUtility.ScoreGuard(in hot) > ArmyGroupUtility.ScoreGuard(in calm) + 1f;
        }

        private static bool HarassDisabled()
        {
            var sense = new ArmyGroupUtility.Sense(20, 0, false, true, 1f, 3, 2, 6, false);
            return ArmyGroupUtility.ScoreHarass(in sense) < 0f;
        }

        private static bool AllocateSplits()
        {
            var combat = new List<SimEntityId>();
            var defs = new List<string>();
            for (uint i = 1; i <= 12; i++)
            {
                combat.Add(new SimEntityId(i));
                defs.Add(i % 3 == 0 ? "unit_iron_archer" : "unit_militia");
            }

            var sense = new ArmyGroupUtility.Sense(12, 0, false, true, 1f, 3, 2, 6, true);
            ArmyGroupUtility.Allocate(
                combat,
                defs,
                "unit_iron_archer",
                "unit_iron_knight",
                in sense,
                out var guard,
                out var main,
                out var harass);
            return guard.Length == 3
                   && harass.Length == 2
                   && main.Length == 7
                   && guard.Length + main.Length + harass.Length == 12;
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
