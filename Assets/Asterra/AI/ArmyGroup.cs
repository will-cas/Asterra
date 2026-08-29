using System;
using System.Collections.Generic;
using Asterra.Core;

namespace Asterra.AI
{
    public enum ArmyGroupRole : byte
    {
        Guard = 0,
        Main = 1,
        Harass = 2,
    }

    /// <summary>Named combat detachment with a scored role assignment.</summary>
    public sealed class ArmyGroup
    {
        public ArmyGroupRole Role;
        public readonly List<SimEntityId> UnitIds = new(32);
        public float UtilityScore;
        public string LastOrder = "idle";
    }

    /// <summary>
    /// Utility scoring for army-group roles (eco/attack/defend). Used by
    /// <see cref="SkirmishOpponentBrain"/> to allocate combat units.
    /// </summary>
    public static class ArmyGroupUtility
    {
        public readonly struct Sense
        {
            public readonly int CombatCount;
            public readonly int EnemyCombatNearby;
            public readonly bool UnderAttack;
            public readonly bool HasKeepTarget;
            public readonly float GoldRatio;
            public readonly int HomeGuardSize;
            public readonly int HarassSize;
            public readonly int AssaultArmySize;
            public readonly bool AllowHarass;

            public Sense(
                int combatCount,
                int enemyCombatNearby,
                bool underAttack,
                bool hasKeepTarget,
                float goldRatio,
                int homeGuardSize,
                int harassSize,
                int assaultArmySize,
                bool allowHarass)
            {
                CombatCount = combatCount;
                EnemyCombatNearby = enemyCombatNearby;
                UnderAttack = underAttack;
                HasKeepTarget = hasKeepTarget;
                GoldRatio = goldRatio;
                HomeGuardSize = homeGuardSize;
                HarassSize = harassSize;
                AssaultArmySize = assaultArmySize;
                AllowHarass = allowHarass;
            }
        }

        public static float ScoreGuard(in Sense s)
        {
            float score = 1.2f;
            if (s.UnderAttack)
                score += 3.5f;
            score += Math.Min(2f, s.EnemyCombatNearby * 0.35f);
            if (s.CombatCount < s.HomeGuardSize + 2)
                score += 1.5f;
            return score;
        }

        public static float ScoreMain(in Sense s)
        {
            float score = 1f;
            if (s.HasKeepTarget && s.CombatCount >= s.AssaultArmySize)
                score += 2.8f;
            if (!s.UnderAttack && s.CombatCount >= s.AssaultArmySize)
                score += 1.6f;
            if (s.GoldRatio > 1.2f)
                score += 0.4f;
            return score;
        }

        public static float ScoreHarass(in Sense s)
        {
            if (!s.AllowHarass || s.HarassSize <= 0)
                return -100f;
            float score = 0.4f;
            if (s.CombatCount >= s.AssaultArmySize + s.HarassSize)
                score += 2.2f;
            if (!s.UnderAttack && s.HasKeepTarget)
                score += 0.8f;
            if (s.GoldRatio < 0.85f)
                score += 0.5f; // behind on eco → raid
            return score;
        }

        /// <summary>
        /// Allocate combat ids into guard / main / harass using utility scores and difficulty caps.
        /// </summary>
        public static void Allocate(
            IReadOnlyList<SimEntityId> combat,
            IReadOnlyList<string> combatDefIds,
            string rangedDefId,
            string cavalryDefId,
            in Sense sense,
            out SimEntityId[] guard,
            out SimEntityId[] main,
            out SimEntityId[] harass)
        {
            int n = combat?.Count ?? 0;
            if (n == 0)
            {
                guard = Array.Empty<SimEntityId>();
                main = Array.Empty<SimEntityId>();
                harass = Array.Empty<SimEntityId>();
                return;
            }

            float gScore = ScoreGuard(in sense);
            float mScore = ScoreMain(in sense);
            float hScore = ScoreHarass(in sense);

            int guardNeed = Math.Min(sense.HomeGuardSize, Math.Max(0, n - 1));
            if (gScore >= mScore + 1.5f && sense.UnderAttack)
                guardNeed = Math.Min(n, Math.Max(guardNeed, sense.HomeGuardSize + 1));

            int harassNeed = 0;
            if (hScore > 1.5f && hScore >= mScore * 0.55f)
            {
                harassNeed = Math.Min(sense.HarassSize, n - guardNeed - 1);
                if (harassNeed < 0)
                    harassNeed = 0;
            }

            if (n <= guardNeed)
            {
                guard = ToArray(combat, 0, n);
                main = Array.Empty<SimEntityId>();
                harass = Array.Empty<SimEntityId>();
                return;
            }

            var used = new bool[n];
            guard = new SimEntityId[guardNeed];
            for (int i = 0; i < guardNeed; i++)
            {
                guard[i] = combat[i];
                used[i] = true;
            }

            harass = new SimEntityId[harassNeed];
            int hFilled = 0;
            for (int pass = 0; pass < 2 && hFilled < harassNeed; pass++)
            {
                for (int i = n - 1; i >= 0 && hFilled < harassNeed; i--)
                {
                    if (used[i])
                        continue;
                    string def = combatDefIds != null && i < combatDefIds.Count ? combatDefIds[i] : string.Empty;
                    bool preferred = def == rangedDefId || def == cavalryDefId;
                    if (pass == 0 && !preferred)
                        continue;
                    harass[hFilled++] = combat[i];
                    used[i] = true;
                }
            }

            if (hFilled < harassNeed)
            {
                var trimmed = new SimEntityId[hFilled];
                Array.Copy(harass, trimmed, hFilled);
                harass = trimmed;
            }

            int mainCount = 0;
            for (int i = 0; i < n; i++)
            {
                if (!used[i])
                    mainCount++;
            }

            main = new SimEntityId[mainCount];
            int m = 0;
            for (int i = 0; i < n; i++)
            {
                if (used[i])
                    continue;
                main[m++] = combat[i];
            }
        }

        private static SimEntityId[] ToArray(IReadOnlyList<SimEntityId> list, int start, int count)
        {
            var arr = new SimEntityId[count];
            for (int i = 0; i < count; i++)
                arr[i] = list[start + i];
            return arr;
        }
    }
}
