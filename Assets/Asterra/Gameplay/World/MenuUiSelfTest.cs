using System.Text;
using Asterra.AI;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Presentation;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>Lobby/map-preview and menu policy smoke without play mode.</summary>
    public static class MenuUiSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "profile not during match", !AsterraMenuPanels.IsProfileAllowedDuringMatch);

            Expect(ref fails, sb, "greenveil preview", PreviewOk(MapCatalog.LushForestId));
            Expect(ref fails, sb, "capital preview", PreviewOk(MapCatalog.MundorCapitalId));
            Expect(ref fails, sb, "river crossing preview", PreviewOk(MapCatalog.RiverCrossingId));

            Expect(ref fails, sb, "greenveil seats", SeatsOk(MapCatalog.LushForestId, 2));
            Expect(ref fails, sb, "capital seats", SeatsOk(MapCatalog.MundorCapitalId, 3));
            Expect(ref fails, sb, "outcast seats", SeatsOk(MapCatalog.OutcastCampId, 4));
            Expect(ref fails, sb, "keep count camp", MapCatalog.KeepCount(MapCatalog.OutcastCampId) == 4);
            Expect(ref fails, sb, "keep count capital", MapCatalog.KeepCount(MapCatalog.MundorCapitalId) == 3);
            Expect(ref fails, sb, "river seats", SeatsOk(MapCatalog.RiverCrossingId, 2));

            Expect(ref fails, sb, "seat hit west greenveil", SeatHitOk(MapCatalog.LushForestId));
            Expect(ref fails, sb, "seat hit east greenveil", SeatHitEastOk(MapCatalog.LushForestId));

            Expect(ref fails, sb, "difficulty cycle", AiDifficultyTuning.Cycle(AiDifficulty.Easy, 1) == AiDifficulty.Normal);
            Expect(ref fails, sb, "difficulty display", !string.IsNullOrEmpty(AiDifficultyTuning.DisplayName(AiDifficulty.Hard)));
            Expect(ref fails, sb, "easy blurb", !string.IsNullOrEmpty(AiDifficultyTuning.Blurb(AiDifficulty.Easy)));
            Expect(ref fails, sb, "insane blurb", !string.IsNullOrEmpty(AiDifficultyTuning.Blurb(AiDifficulty.Insane)));

            Expect(ref fails, sb, "campaign mission count", CampaignCatalog.MissionCount == 6);
            Expect(ref fails, sb, "campaign greenveil map", CampaignCatalog.Get(0).MapKey == MapCatalog.LushForestId);
            Expect(ref fails, sb, "campaign river map", CampaignCatalog.Get(1).MapKey == MapCatalog.RiverCrossingId);
            Expect(ref fails, sb, "campaign camp map", CampaignCatalog.Get(2).MapKey == MapCatalog.OutcastCampId);
            Expect(ref fails, sb, "campaign camp spawn", CampaignCatalog.Get(2).SpawnSeat == 1);
            Expect(ref fails, sb, "campaign mercy mission", CampaignCatalog.MercyMissionIndex == 2);
            Expect(ref fails, sb, "campaign opening talk", CampaignCatalog.Talk(0).Length >= 1);
            Expect(ref fails, sb, "campaign rival outcast", CampaignCatalog.RivalFactionIndex(1) == 2);
            Expect(ref fails, sb, "campaign rival crown", CampaignCatalog.RivalFactionIndex(0) == 1);
            Expect(ref fails, sb, "campaign rival others vs crown", CampaignCatalog.RivalFactionIndex(2) == 1);
            Expect(ref fails, sb, "campaign crown locked", CampaignCatalog.PlayerFactionIndex == 1);
            Expect(ref fails, sb, "campaign secret mission", CampaignCatalog.Get(6).IsSecret);
            Expect(ref fails, sb, "campaign secret capital", CampaignCatalog.Get(6).MapKey == MapCatalog.MundorCapitalId);
            Expect(ref fails, sb, "campaign progress", CampaignProgressRoundtripOk());

            Expect(ref fails, sb, "map catalog builtin greenveil", MapCatalog.BuiltinChoice(SkirmishMapId.LushForest).Id == MapCatalog.LushForestId);
            Expect(ref fails, sb, "overlay enum pause", (int)AsterraMenuPanels.Overlay.Pause == 3);
            Expect(ref fails, sb, "overlay enum profile", (int)AsterraMenuPanels.Overlay.Profile == 2);
            Expect(ref fails, sb, "ui scale default in range",
                AsterraSettings.UiScale >= AsterraSettings.UiScaleMin
                && AsterraSettings.UiScale <= AsterraSettings.UiScaleMax);
            Expect(ref fails, sb, "ui scale clamp", UiScaleClampOk());
            Expect(ref fails, sb, "profile name roundtrip", ProfileNameOk());
            Expect(ref fails, sb, "hud content right clears minimap",
                HudStyle.ContentRight < Screen.width - 8f || Screen.width < 100f);
            Expect(ref fails, sb, "infantry squad size 16",
                UnitSquadVisual.DefaultForRole(UnitRole.Infantry) == 16);
            Expect(ref fails, sb, "ranged squad size 12",
                UnitSquadVisual.DefaultForRole(UnitRole.Ranged) == 12);
            Expect(ref fails, sb, "cavalry squad size 6",
                UnitSquadVisual.DefaultForRole(UnitRole.Cavalry) == 6);
            Expect(ref fails, sb, "builder stays solo",
                UnitSquadVisual.ResolveSquadSize("unit_veiled_builder") == 1);
            Expect(ref fails, sb, "apprentice is company",
                UnitSquadVisual.ResolveSquadSize("unit_veiled_apprentice") == 16);

            sb.Append(fails == 0 ? "MenuUiSelfTest: OK" : $"MenuUiSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool UiScaleClampOk()
        {
            float prev = AsterraSettings.UiScale;
            AsterraSettings.UiScale = 99f;
            bool hi = Mathf.Approximately(AsterraSettings.UiScale, AsterraSettings.UiScaleMax);
            AsterraSettings.UiScale = 0.01f;
            bool lo = Mathf.Approximately(AsterraSettings.UiScale, AsterraSettings.UiScaleMin);
            AsterraSettings.UiScale = prev;
            return hi && lo;
        }

        private static bool ProfileNameOk()
        {
            string prev = AsterraLocalProfile.DisplayName;
            AsterraLocalProfile.DisplayName = "  Test Commander  ";
            bool ok = AsterraLocalProfile.DisplayName == "Test Commander";
            AsterraLocalProfile.DisplayName = prev;
            return ok;
        }

        private static bool CampaignProgressRoundtripOk()
        {
            bool had = CampaignProgress.HasSave;
            int prevMission = CampaignProgress.NextMissionIndex;
            int prevFaction = CampaignProgress.FactionIndex;
            var prevDiff = CampaignProgress.Difficulty;
            bool prevComplete = CampaignProgress.IsComplete;

            CampaignProgress.Clear();
            bool empty = !CampaignProgress.HasSave;
            CampaignProgress.StartNew(0, AiDifficulty.Hard);
            bool started = CampaignProgress.HasSave
                           && CampaignProgress.NextMissionIndex == 0
                           && CampaignProgress.Difficulty == AiDifficulty.Hard;
            CampaignProgress.OnMissionWon(0);
            bool advanced = CampaignProgress.NextMissionIndex == 1 && !CampaignProgress.IsComplete;
            for (int i = 1; i < CampaignCatalog.MissionCount; i++)
                CampaignProgress.OnMissionWon(i);
            bool done = CampaignProgress.IsComplete;

            if (had)
            {
                CampaignProgress.StartNew(prevFaction, prevDiff);
                for (int i = 0; i < prevMission; i++)
                    CampaignProgress.OnMissionWon(i);
                if (prevComplete)
                    CampaignProgress.OnMissionWon(CampaignCatalog.MissionCount - 1);
            }
            else
            {
                CampaignProgress.Clear();
            }

            return empty && started && advanced && done;
        }

        private static bool PreviewOk(string mapId)
        {
            var tex = MapPreviewBuilder.Build(mapId);
            bool ok = tex != null && tex.width == MapPreviewBuilder.Resolution && tex.height == MapPreviewBuilder.Resolution;
            if (tex != null)
                Object.DestroyImmediate(tex);
            return ok;
        }

        private static bool SeatsOk(string mapId, int min)
        {
            var markers = MapPreviewBuilder.GetKeepMarkers(mapId);
            return markers != null && markers.Count >= min;
        }

        private static bool SeatHitOk(string mapId)
        {
            var markers = MapPreviewBuilder.GetKeepMarkers(mapId);
            if (markers == null || markers.Count < 1)
                return false;
            var rect = new Rect(10f, 10f, 200f, 200f);
            MapPreviewBuilder.WorldToPreviewGui(rect, markers[0].X, markers[0].Z, out float gx, out float gy);
            return MapPreviewBuilder.TryHitSeat(rect, new Vector2(gx, gy), mapId, 30f, out int seat)
                   && seat == markers[0].SeatIndex;
        }

        private static bool SeatHitEastOk(string mapId)
        {
            var markers = MapPreviewBuilder.GetKeepMarkers(mapId);
            if (markers == null || markers.Count < 2)
                return false;
            var east = markers[markers.Count - 1];
            var rect = new Rect(10f, 10f, 200f, 200f);
            MapPreviewBuilder.WorldToPreviewGui(rect, east.X, east.Z, out float gx, out float gy);
            return MapPreviewBuilder.TryHitSeat(rect, new Vector2(gx, gy), mapId, 30f, out int seat)
                   && seat == east.SeatIndex;
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
