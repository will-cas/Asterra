namespace Asterra.AI
{
    /// <summary>Offline skirmish opponent difficulty presets.</summary>
    public enum AiDifficulty : byte
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Insane = 3,
    }

    /// <summary>Numeric knobs for <see cref="SkirmishOpponentBrain"/> (and future brains).</summary>
    public readonly struct AiDifficultyTuning
    {
        public readonly int TrainIntervalTicks;
        public readonly int BuildIntervalTicks;
        public readonly int CombatOrderIntervalTicks;
        public readonly int GatherIntervalTicks;
        public readonly int DefendIntervalTicks;
        public readonly int RallyIntervalTicks;
        public readonly int CheatIntervalTicks;
        public readonly int ScoutIntervalTicks;
        public readonly int ScoutAssaultTimeoutTicks;
        public readonly int ReactionDelayTicks;

        public readonly int TargetBuilders;
        public readonly int MaxBuilders;
        public readonly int TargetWorkersPerNode;
        public readonly int TargetProducers;
        public readonly int AssaultArmySize;
        public readonly int HomeGuardSize;
        public readonly int HarassSize;
        public readonly float DefendThreatRadius;
        public readonly float Aggression;

        public readonly int TargetTowers;
        public readonly int TargetOutposts;
        public readonly int TargetWalls;

        public readonly int StartingGoldBonus;
        public readonly int GoldCheatAmount;
        public readonly bool UseGoldCheat;
        public readonly bool PreferTech;
        public readonly bool RequireSightBeforeAssault;

        public AiDifficultyTuning(
            int trainIntervalTicks,
            int buildIntervalTicks,
            int combatOrderIntervalTicks,
            int gatherIntervalTicks,
            int defendIntervalTicks,
            int rallyIntervalTicks,
            int cheatIntervalTicks,
            int scoutIntervalTicks,
            int scoutAssaultTimeoutTicks,
            int reactionDelayTicks,
            int targetBuilders,
            int maxBuilders,
            int targetWorkersPerNode,
            int targetProducers,
            int assaultArmySize,
            int homeGuardSize,
            int harassSize,
            float defendThreatRadius,
            float aggression,
            int targetTowers,
            int targetOutposts,
            int targetWalls,
            int startingGoldBonus,
            int goldCheatAmount,
            bool useGoldCheat,
            bool preferTech,
            bool requireSightBeforeAssault)
        {
            TrainIntervalTicks = trainIntervalTicks;
            BuildIntervalTicks = buildIntervalTicks;
            CombatOrderIntervalTicks = combatOrderIntervalTicks;
            GatherIntervalTicks = gatherIntervalTicks;
            DefendIntervalTicks = defendIntervalTicks;
            RallyIntervalTicks = rallyIntervalTicks;
            CheatIntervalTicks = cheatIntervalTicks;
            ScoutIntervalTicks = scoutIntervalTicks;
            ScoutAssaultTimeoutTicks = scoutAssaultTimeoutTicks;
            ReactionDelayTicks = reactionDelayTicks;
            TargetBuilders = targetBuilders;
            MaxBuilders = maxBuilders;
            TargetWorkersPerNode = targetWorkersPerNode;
            TargetProducers = targetProducers;
            AssaultArmySize = assaultArmySize;
            HomeGuardSize = homeGuardSize;
            HarassSize = harassSize;
            DefendThreatRadius = defendThreatRadius;
            Aggression = aggression;
            TargetTowers = targetTowers;
            TargetOutposts = targetOutposts;
            TargetWalls = targetWalls;
            StartingGoldBonus = startingGoldBonus;
            GoldCheatAmount = goldCheatAmount;
            UseGoldCheat = useGoldCheat;
            PreferTech = preferTech;
            RequireSightBeforeAssault = requireSightBeforeAssault;
        }

        public static AiDifficultyTuning For(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy:
                    return new AiDifficultyTuning(
                        trainIntervalTicks: 90,
                        buildIntervalTicks: 110,
                        combatOrderIntervalTicks: 90,
                        gatherIntervalTicks: 45,
                        defendIntervalTicks: 55,
                        rallyIntervalTicks: 220,
                        cheatIntervalTicks: 0,
                        scoutIntervalTicks: 160,
                        scoutAssaultTimeoutTicks: 900,
                        reactionDelayTicks: 35,
                        targetBuilders: 2,
                        maxBuilders: 4,
                        targetWorkersPerNode: 1,
                        targetProducers: 1,
                        assaultArmySize: 8,
                        homeGuardSize: 1,
                        harassSize: 0,
                        defendThreatRadius: 85f,
                        aggression: 0.25f,
                        targetTowers: 0,
                        targetOutposts: 0,
                        targetWalls: 0,
                        startingGoldBonus: 0,
                        goldCheatAmount: 0,
                        useGoldCheat: false,
                        preferTech: false,
                        requireSightBeforeAssault: true);

                case AiDifficulty.Hard:
                    return new AiDifficultyTuning(
                        trainIntervalTicks: 32,
                        buildIntervalTicks: 55,
                        combatOrderIntervalTicks: 38,
                        gatherIntervalTicks: 22,
                        defendIntervalTicks: 28,
                        rallyIntervalTicks: 130,
                        cheatIntervalTicks: 0,
                        scoutIntervalTicks: 70,
                        scoutAssaultTimeoutTicks: 450,
                        reactionDelayTicks: 8,
                        targetBuilders: 4,
                        maxBuilders: 8,
                        targetWorkersPerNode: 2,
                        targetProducers: 2,
                        assaultArmySize: 5,
                        homeGuardSize: 2,
                        harassSize: 1,
                        defendThreatRadius: 115f,
                        aggression: 0.75f,
                        targetTowers: 3,
                        targetOutposts: 1,
                        targetWalls: 4,
                        startingGoldBonus: 0,
                        goldCheatAmount: 0,
                        useGoldCheat: false,
                        preferTech: true,
                        requireSightBeforeAssault: false);

                case AiDifficulty.Insane:
                    return new AiDifficultyTuning(
                        trainIntervalTicks: 22,
                        buildIntervalTicks: 40,
                        combatOrderIntervalTicks: 28,
                        gatherIntervalTicks: 16,
                        defendIntervalTicks: 20,
                        rallyIntervalTicks: 100,
                        cheatIntervalTicks: 0,
                        scoutIntervalTicks: 45,
                        scoutAssaultTimeoutTicks: 280,
                        reactionDelayTicks: 0,
                        targetBuilders: 5,
                        maxBuilders: 10,
                        targetWorkersPerNode: 2,
                        targetProducers: 2,
                        assaultArmySize: 4,
                        homeGuardSize: 2,
                        harassSize: 2,
                        defendThreatRadius: 130f,
                        aggression: 0.95f,
                        targetTowers: 4,
                        targetOutposts: 2,
                        targetWalls: 8,
                        startingGoldBonus: 80,
                        goldCheatAmount: 0,
                        useGoldCheat: false,
                        preferTech: true,
                        requireSightBeforeAssault: false);

                case AiDifficulty.Normal:
                default:
                    return new AiDifficultyTuning(
                        trainIntervalTicks: 45,
                        buildIntervalTicks: 70,
                        combatOrderIntervalTicks: 50,
                        gatherIntervalTicks: 28,
                        defendIntervalTicks: 35,
                        rallyIntervalTicks: 160,
                        cheatIntervalTicks: 0,
                        scoutIntervalTicks: 110,
                        scoutAssaultTimeoutTicks: 650,
                        reactionDelayTicks: 18,
                        targetBuilders: 3,
                        maxBuilders: 6,
                        targetWorkersPerNode: 2,
                        targetProducers: 1,
                        assaultArmySize: 5,
                        homeGuardSize: 1,
                        harassSize: 0,
                        defendThreatRadius: 95f,
                        aggression: 0.5f,
                        targetTowers: 2,
                        targetOutposts: 1,
                        targetWalls: 2,
                        startingGoldBonus: 0,
                        goldCheatAmount: 0,
                        useGoldCheat: false,
                        preferTech: true,
                        requireSightBeforeAssault: true);
            }
        }

        public static string DisplayName(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy: return "Easy";
                case AiDifficulty.Hard: return "Hard";
                case AiDifficulty.Insane: return "Insane";
                default: return "Normal";
            }
        }

        public static string Blurb(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy:
                    return "Slow macro and reaction, scouts before rushes — no income cheats.";
                case AiDifficulty.Hard:
                    return "Two barracks, second-gold expand, sharp reaction — no gold drip.";
                case AiDifficulty.Insane:
                    return "Max eco pressure, snappy orders, dual producers — tiny start bump only.";
                default:
                    return "Balanced macro and scouting — no income cheats.";
            }
        }

        public static AiDifficulty Cycle(AiDifficulty current, int delta)
        {
            int n = 4;
            int v = ((int)current + delta) % n;
            if (v < 0)
                v += n;
            return (AiDifficulty)v;
        }
    }
}
