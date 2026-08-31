using System;
using Asterra.AI;

namespace Asterra.Gameplay.Content
{
    /// <summary>
    /// Mundor Crown campaign: hunt the Outcast Host. One faction story now; six later.
    /// </summary>
    public static class CampaignCatalog
    {
        public const int StoryMissionCount = 6;
        public const int SecretMissionIndex = 6;
        public const int MissionCount = StoryMissionCount;
        public const int PlayerFactionIndex = 1;
        public const int OutcastFactionIndex = 2;
        public const int MercyMissionIndex = 2;

        public readonly struct Mission
        {
            public readonly int Index;
            public readonly string Id;
            public readonly string Chapter;
            public readonly string DisplayName;
            public readonly string MapKey;
            public readonly string Look;
            public readonly string Aim;
            public readonly string SecretTease;
            public readonly string StoryBetween;
            public readonly string Afterword;
            public readonly bool IsSecret;
            public readonly int SpawnSeat;

            public Mission(
                int index,
                string id,
                string chapter,
                string displayName,
                string mapKey,
                string look,
                string aim,
                string secretTease,
                string storyBetween,
                string afterword,
                bool isSecret = false,
                int spawnSeat = 0)
            {
                Index = index;
                Id = id;
                Chapter = chapter;
                DisplayName = displayName;
                MapKey = mapKey;
                Look = look;
                Aim = aim;
                SecretTease = secretTease;
                StoryBetween = storyBetween;
                Afterword = afterword;
                IsSecret = isSecret;
                SpawnSeat = spawnSeat;
            }
        }

        public static readonly Mission[] Missions =
        {
            new Mission(
                0,
                "greenveil",
                "Mundor Crown — The Rising",
                "Greenveil First Blood",
                MapCatalog.LushForestId,
                "A wet green canopy. Roads like scars. Host pickets already in the trees.",
                "Break the Outcast vanguard before they vanish into the wood.",
                "Optional: push through the south swamp before they burn it behind them.",
                "The Host has risen in the green. Crown riders do not wait for a war map. You take the next road.",
                "The trees empty. Survivors run west toward water. You are ordered to cut the retreat."),
            new Mission(
                1,
                "riverlands",
                "Mundor Crown — The Rising",
                "Cut the Ford",
                MapCatalog.RiverCrossingId,
                "A north–south river, one timber span, boats at the mouths, mist on the current.",
                "Hold the crossing. Starve the Host of a road home.",
                "Optional: leave one bank unburned. Smugglers will owe the Crown a later word.",
                "After Greenveil the river is no longer scenery. Every boat is a vote. The Host has friends in the mist. You have the law.",
                "The river is Crown water. The Host’s camp is no longer a rumour."),
            new Mission(
                2,
                "outcast_camp",
                "Mundor Crown — The Rising",
                "Burn the Camp",
                MapCatalog.OutcastCampId,
                "A packed camp jammed into the south-west corner. Swamp and rock choke the approaches.",
                "Assault the Host camp from the north-east. Do not let them dig in.",
                "Optional: win by holding the approaches, not by razing the camp. Mercy is remembered.",
                "You do not ride as the defender. You ride as the hammer. Their banners are already in the mud.",
                "The camp is broken. Columns peel toward the twin cities. You follow.",
                spawnSeat: 1),
            new Mission(
                3,
                "twin_cities",
                "Mundor Crown — The Rising",
                "The Twin Cities",
                MapCatalog.TwinCitiesId,
                "Two stone towns facing across a canal. Four bridges. Towers on both banks.",
                "Take the far city. Contest the bridges; do not gift them a single span.",
                "Optional: leave two bridges standing. Trade will remember who did not burn the river.",
                "The Host thought walls would make them a people. Walls make a siege.",
                "One city flies Crown colours. The other smokes. The Host flees into the white."),
            new Mission(
                4,
                "frozen_wastes",
                "Mundor Crown — The Rising",
                "The White March",
                MapCatalog.FrozenWastesId,
                "Snow, a frozen meres, thin ice that sings under boots. Keeps on opposite corners.",
                "Run them down before the ice takes both of you.",
                "Optional: do not smash the centre ice. A later courier will need a road that is not a grave.",
                "No villages here. No grain. Only distance, and the Host trying to spend it.",
                "The snow holds their dead. A relic road opens in the south. The Host still has a priest."),
            new Mission(
                5,
                "reliquary",
                "Mundor Crown — The Rising",
                "The Reliquary",
                MapCatalog.AncientRelicId,
                "A broken ring of stone. A jump into the bowl. No-entry cliffs east and west.",
                "Take the relic ground. End the rising in the bowl, not in a rumour.",
                "Optional: do not raze the inner ring. The University will ask who smashed history.",
                "Whatever they stole from the old world is here. Finish it.",
                "The rising is broken. Five other banners still have their wars. The Crown has this one."),
            new Mission(
                6,
                "quiet_capital",
                "Mundor Crown — Secret",
                "The Quiet Capital",
                MapCatalog.MundorCapitalId,
                "The island citadel between two rivers. Host on both banks. Night on the water.",
                "Hold Mundor. Host remnants come from the west bank and the east bank at once.",
                "Finishing this after a merciful camp assault unlocks the secret ending: the Crown keeps its honour.",
                "A boy from the river villages finds you after the Reliquary. He says the Host did not all die in the bowl. They took the quiet road — the one you left open when you refused to burn the camp.",
                "Secret ending: the Crown holds the rivers, the camp, the cities, and its name. The Host survives as exiles, not as ash.",
                isSecret: true,
                spawnSeat: 0),
        };

        public static bool TryGet(int index, out Mission mission)
        {
            if (index < 0 || index >= Missions.Length)
            {
                mission = default;
                return false;
            }

            mission = Missions[index];
            return true;
        }

        public static Mission Get(int index)
        {
            if (!TryGet(index, out var mission))
                return Missions[0];
            return mission;
        }

        public static int RivalFactionIndex(int playerFactionIndex)
        {
            return playerFactionIndex == PlayerFactionIndex ? OutcastFactionIndex : PlayerFactionIndex;
        }

        public static string HubBlurb =>
            "The Mundor Crown campaign — first of six faction stories. Hunt the Outcast Host. Not a war map. Not skirmish with a briefing.";

        public static string WhatItIsnt =>
            "Not Total War. Not six campaigns at once. Not the old three-faction war map.";

        public readonly struct TalkLine
        {
            public readonly string Speaker;
            public readonly string Text;

            public TalkLine(string speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }
        }

        public static TalkLine[] Talk(int missionIndex)
        {
            switch (missionIndex)
            {
                case 0:
                    return new[]
                    {
                        new TalkLine("Marshal", "Greenveil first. Cut their pickets before they melt into the wood."),
                        new TalkLine("Scout", "Host banners in the canopy. They know we are coming."),
                    };
                case 1:
                    return new[]
                    {
                        new TalkLine("Marshal", "The river is their road home. Take the span. Sink what you cannot hold."),
                    };
                case 2:
                    return new[]
                    {
                        new TalkLine("Marshal", "That camp is the rising. Break it."),
                        new TalkLine("Chaplain", "Hold the approaches if you can. Fire is easy. Mercy is remembered."),
                    };
                case 3:
                    return new[]
                    {
                        new TalkLine("Marshal", "Two cities, four bridges. Do not gift them a single span."),
                    };
                case 4:
                    return new[]
                    {
                        new TalkLine("Scout", "White in every direction. Their tracks go south-east."),
                    };
                case 5:
                    return new[]
                    {
                        new TalkLine("Marshal", "The bowl. Jump down if you must. End it in the ring."),
                    };
                case 6:
                    return new[]
                    {
                        new TalkLine("Watch", "West bank and east bank. They mean to take Mundor in the dark."),
                        new TalkLine("Marshal", "Hold the island. The rivers are ours if the bridges stand."),
                    };
                default:
                    return Array.Empty<TalkLine>();
            }
        }

        public static AiDifficulty ClampDifficulty(AiDifficulty difficulty)
        {
            int v = (int)difficulty;
            if (v < 0)
                return AiDifficulty.Easy;
            if (v > (int)AiDifficulty.Insane)
                return AiDifficulty.Insane;
            return difficulty;
        }
    }
}
