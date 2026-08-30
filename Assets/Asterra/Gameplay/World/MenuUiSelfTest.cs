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

            Expect(ref fails, sb, "blackridge preview", PreviewOk(MapCatalog.BlackridgePassId));
            Expect(ref fails, sb, "twin keeps preview", PreviewOk(MapCatalog.TwinKeepsId));
            Expect(ref fails, sb, "river crossing preview", PreviewOk(MapCatalog.RiverCrossingId));

            Expect(ref fails, sb, "blackridge seats", SeatsOk(MapCatalog.BlackridgePassId, 2));
            Expect(ref fails, sb, "twin keeps seats", SeatsOk(MapCatalog.TwinKeepsId, 2));
            Expect(ref fails, sb, "river seats", SeatsOk(MapCatalog.RiverCrossingId, 2));

            Expect(ref fails, sb, "seat hit west blackridge", SeatHitOk(MapCatalog.BlackridgePassId));
            Expect(ref fails, sb, "seat hit east blackridge", SeatHitEastOk(MapCatalog.BlackridgePassId));

            Expect(ref fails, sb, "difficulty cycle", AiDifficultyTuning.Cycle(AiDifficulty.Easy, 1) == AiDifficulty.Normal);
            Expect(ref fails, sb, "difficulty display", !string.IsNullOrEmpty(AiDifficultyTuning.DisplayName(AiDifficulty.Hard)));
            Expect(ref fails, sb, "easy blurb", !string.IsNullOrEmpty(AiDifficultyTuning.Blurb(AiDifficulty.Easy)));
            Expect(ref fails, sb, "insane blurb", !string.IsNullOrEmpty(AiDifficultyTuning.Blurb(AiDifficulty.Insane)));

            Expect(ref fails, sb, "map catalog builtin blackridge", MapCatalog.BuiltinChoice(SkirmishMapId.BlackridgePass).Id == MapCatalog.BlackridgePassId);
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
            AsterraLocalProfile.DisplayName = "TestCmdr";
            bool ok = AsterraLocalProfile.DisplayName == "TestCmdr";
            AsterraLocalProfile.DisplayName = prev;
            return ok;
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
