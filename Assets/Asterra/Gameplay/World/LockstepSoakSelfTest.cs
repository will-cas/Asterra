using System.Collections.Generic;
using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Lockstep hardening: delayed frames, multi-player gate, desync detection over a shared script.
    /// Does not require NGO — exercises the same hash / gate / replay path online would use.
    /// </summary>
    public static class LockstepSoakSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "gate opens when all submit", GateOpens());
            Expect(ref fails, sb, "gate blocks partial submit", GateBlocksPartial());
            Expect(ref fails, sb, "delayed dual soak matches", DelayedDualSoak());
            Expect(ref fails, sb, "desync detector catches drift", DesyncCatchesDrift());
            Expect(ref fails, sb, "loopback lobby info", LoopbackLobbyInfo());

            sb.Append(fails == 0 ? "LockstepSoakSelfTest: OK" : $"LockstepSoakSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool GateOpens()
        {
            var gate = new LockstepFrameGate();
            gate.SetExpectedPlayers(new[] { new PlayerId(0), new PlayerId(1) });
            var tick = new Tick(10);
            gate.SubmitEmpty(tick, new PlayerId(0));
            gate.SubmitEmpty(tick, new PlayerId(1));
            var buffer = new List<GameCommand>();
            return gate.TryConsume(tick, buffer);
        }

        private static bool GateBlocksPartial()
        {
            var gate = new LockstepFrameGate();
            gate.SetExpectedPlayers(new[] { new PlayerId(0), new PlayerId(1) });
            var tick = new Tick(11);
            gate.SubmitEmpty(tick, new PlayerId(0));
            var buffer = new List<GameCommand>();
            return !gate.TryConsume(tick, buffer);
        }

        private static bool DelayedDualSoak()
        {
            var frames = DualSimSoakSelfTest.BuildPublicScript(120);
            var a = Boot();
            var b = Boot();
            var delay = new Queue<GameCommand[]>(2);
            delay.Enqueue(System.Array.Empty<GameCommand>());
            delay.Enqueue(System.Array.Empty<GameCommand>());

            for (int i = 0; i < frames.Count; i++)
            {
                delay.Enqueue(frames[i].Commands ?? System.Array.Empty<GameCommand>());
                var apply = delay.Dequeue();
                a.ApplyCommands(apply);
                b.ApplyCommands(apply);
                a.Tick(0.05f);
                b.Tick(0.05f);
            }

            while (delay.Count > 0)
            {
                var apply = delay.Dequeue();
                a.ApplyCommands(apply);
                b.ApplyCommands(apply);
                a.Tick(0.05f);
                b.Tick(0.05f);
            }

            return a.ComputeWorldHash() == b.ComputeWorldHash();
        }

        private static bool DesyncCatchesDrift()
        {
            var detector = new DesyncDetector();
            detector.Report(5, 0, 111ul);
            detector.Report(5, 1, 222ul);
            return detector.TryGetDesync(5, out _, out _);
        }

        private static bool LoopbackLobbyInfo()
        {
            var info = new MatchLobbyInfo
            {
                Role = SessionRole.Host,
                MaxPlayers = 8,
                MatchSeed = 42,
                CurrentPlayers = 1,
                LobbyCode = "LOOP",
                RelayJoinCode = "LOOP",
            };
            return info.Role == SessionRole.Host
                   && info.LobbyCode == "LOOP"
                   && info.MaxPlayers == 8;
        }

        private static SkirmishWorldSim Boot()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p0 = new PlayerId(0);
            var p1 = new PlayerId(1);
            wallet.Seed(p0, ResourceType.Gold, 2000);
            wallet.Seed(p1, ResourceType.Gold, 2000);
            sim.SpawnBuilding(ids.Next(), p0, new FactionId(0), FactionDefaultContent.IronKeepId, -80f, 0f, startActive: true);
            sim.SpawnBuilding(ids.Next(), p1, new FactionId(0), FactionDefaultContent.IronKeepId, 80f, 0f, startActive: true);
            sim.SpawnUnit(ids.Next(), p0, new FactionId(0), FactionDefaultContent.MilitiaId, -70f, 0f);
            sim.SpawnUnit(ids.Next(), p1, new FactionId(0), FactionDefaultContent.MilitiaId, 70f, 0f);
            return sim;
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
