using System.Text;
using Asterra.Core;
using Asterra.Core.World;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    public static class MapScriptSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            var map = new MapDefinition();
            map.conversations = new[]
            {
                new MapConversationLine { id = "open", speaker = "A", text = "First." },
                new MapConversationLine { id = "open", speaker = "B", text = "Second." },
            };
            map.talkTriggers = new[]
            {
                new MapTalkTrigger { conversationId = "open", when = "start" },
            };
            map.objectives = new[]
            {
                new MapObjective
                {
                    id = "go",
                    title = "Reach the stone",
                    kind = MapScriptRuntime.KindReach,
                    required = true,
                    x = 10f,
                    z = 0f,
                    radius = 20f,
                },
            };

            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var script = new MapScriptRuntime();
            script.Bind(map, new PlayerId(0));
            script.Tick(sim, null, 0.05f);
            Expect(ref fails, sb, "start talk", script.HasTalk && script.CurrentTalk.Speaker == "A");
            script.AdvanceTalk();
            Expect(ref fails, sb, "second line", script.HasTalk && script.CurrentTalk.Speaker == "B");
            script.AdvanceTalk();
            Expect(ref fails, sb, "talk done", !script.HasTalk);

            script.Tick(sim, null, 0.05f);
            Expect(ref fails, sb, "reach incomplete", !script.TryCustomVictory(out _));

            sim.SpawnUnit(
                ids.Next(),
                new PlayerId(0),
                new FactionId(0),
                FactionDefaultContent.VeiledApprenticeId,
                8f,
                0f);
            script.Tick(sim, null, 0.05f);
            Expect(ref fails, sb, "reach wins", script.TryCustomVictory(out var win) && win.Reason == MatchEndReason.ObjectivesComplete);

            var surviveMap = new MapDefinition();
            surviveMap.objectives = new[]
            {
                new MapObjective
                {
                    id = "live",
                    title = "Survive",
                    kind = MapScriptRuntime.KindSurvive,
                    required = true,
                    holdSeconds = 0.2f,
                },
            };
            var survive = new MapScriptRuntime();
            survive.Bind(surviveMap, new PlayerId(0));
            survive.Tick(sim, null, 0.05f);
            Expect(ref fails, sb, "survive not yet", !survive.TryCustomVictory(out _));
            survive.Tick(sim, null, 0.2f);
            Expect(ref fails, sb, "survive wins", survive.TryCustomVictory(out var sWin) && sWin.Reason == MatchEndReason.ObjectivesComplete);

            var protectMap = new MapDefinition();
            protectMap.objectives = new[]
            {
                new MapObjective
                {
                    id = "ward",
                    title = "Protect",
                    kind = MapScriptRuntime.KindProtect,
                    required = true,
                    x = 40f,
                    z = 0f,
                    radius = 30f,
                },
            };
            var keep = sim.SpawnBuilding(
                ids.Next(),
                new PlayerId(0),
                new FactionId(0),
                FactionDefaultContent.ArcaneumId,
                40f,
                0f,
                startActive: true);
            var protect = new MapScriptRuntime();
            protect.Bind(protectMap, new PlayerId(0));
            protect.Tick(sim, null, 0.05f);
            Expect(ref fails, sb, "protect holds", !protect.TryCustomDefeat(out _));
            sim.ApplyWorldDamage(keep.Id, 50000f, vsStructure: true);
            sim.Tick(0.05f);
            protect.Tick(sim, null, 0.05f);
            Expect(ref fails, sb, "protect fails", protect.TryCustomDefeat(out var pFail) && pFail.Reason == MatchEndReason.ObjectiveFailed);

            Expect(ref fails, sb, "greenveil objectives", HasRequiredKeepObj(BuiltinMaps.LushForest()));
            Expect(ref fails, sb, "ford objectives", HasRequiredKeepObj(BuiltinMaps.RiverCrossing()));
            Expect(ref fails, sb, "camp objectives", HasRequiredKeepObj(BuiltinMaps.OutcastCamp()));
            Expect(ref fails, sb, "twins objectives", HasRequiredKeepObj(BuiltinMaps.TwinCities()));
            Expect(ref fails, sb, "wastes objectives", HasRequiredKeepObj(BuiltinMaps.FrozenWastes()));
            Expect(ref fails, sb, "relic objectives", HasRequiredKeepObj(BuiltinMaps.AncientRelic()));
            Expect(ref fails, sb, "capital objectives", HasRequiredKeepObj(BuiltinMaps.MundorCapital()));
            Expect(ref fails, sb, "camp mercy talk", BuiltinMaps.OutcastCamp().conversations != null && BuiltinMaps.OutcastCamp().conversations.Length > 0);

            var skirmish = new MapScriptRuntime();
            skirmish.Bind(null, new PlayerId(0));
            Expect(ref fails, sb, "skirmish no hud rows", skirmish.CopyHudRows(new ObjectiveHudRow[8]) == 0);
            Expect(ref fails, sb, "skirmish no scripted win", !skirmish.TryCustomVictory(out _));

            sb.Append(fails == 0 ? "MapScriptSelfTest: OK" : $"MapScriptSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }

        private static bool HasRequiredKeepObj(MapDefinition map)
        {
            if (map?.objectives == null || map.objectives.Length == 0)
                return false;
            for (int i = 0; i < map.objectives.Length; i++)
            {
                var o = map.objectives[i];
                if (o.required && o.kind == MapScriptRuntime.KindDestroyKeeps)
                    return true;
            }

            return false;
        }
    }
}
