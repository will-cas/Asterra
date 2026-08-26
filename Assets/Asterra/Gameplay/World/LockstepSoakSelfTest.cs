using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;
using Asterra.Net;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Lockstep hardening: delayed frames, multi-player gate (2→8), desync detection, loopback session.
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
            Expect(ref fails, sb, "gate scales to 4 players", GateScales(4));
            Expect(ref fails, sb, "gate scales to 8 players", GateScales(8));
            Expect(ref fails, sb, "delayed dual soak matches", DelayedDualSoak(120));
            Expect(ref fails, sb, "long delayed dual soak matches", DelayedDualSoak(400));
            Expect(ref fails, sb, "desync detector catches drift", DesyncCatchesDrift());
            Expect(ref fails, sb, "loopback host connects", Await(LoopbackHostConnects()));
            Expect(ref fails, sb, "loopback join + peer count", Await(LoopbackJoinPeers()));
            Expect(ref fails, sb, "8-seat empty frames consume", EightSeatEmptyFrames());

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

        private static bool GateScales(int players)
        {
            var gate = new LockstepFrameGate();
            var ids = new PlayerId[players];
            for (int i = 0; i < players; i++)
                ids[i] = new PlayerId((byte)i);
            gate.SetExpectedPlayers(ids);
            var tick = new Tick(20);
            var buffer = new List<GameCommand>();
            for (int i = 0; i < players - 1; i++)
            {
                gate.SubmitEmpty(tick, ids[i]);
                if (gate.TryConsume(tick, buffer))
                    return false;
            }

            gate.SubmitEmpty(tick, ids[players - 1]);
            return gate.TryConsume(tick, buffer) && gate.ExpectedCount == players;
        }

        private static bool DelayedDualSoak(int ticks)
        {
            var frames = DualSimSoakSelfTest.BuildPublicScript(ticks);
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

        private static async Task<bool> LoopbackHostConnects()
        {
            var session = new LoopbackSession(8);
            await session.HostAsync("soak", 8, 42);
            return session.IsConnected
                   && session.Info.Role == SessionRole.Host
                   && session.Info.LobbyCode == "LOOP"
                   && session.Info.CurrentPlayers == 1
                   && session.Info.MaxPlayers == 8;
        }

        private static async Task<bool> LoopbackJoinPeers()
        {
            var host = new LoopbackSession(8);
            await host.HostAsync("soak", 4, 7);
            host.AddLocalPeer();
            host.AddLocalPeer();
            host.AddLocalPeer(); // clamp at max 4
            var client = new LoopbackSession(8);
            await client.JoinAsync("LOOP");
            return host.Info.CurrentPlayers == 4
                   && client.IsConnected
                   && client.Info.Role == SessionRole.Client
                   && client.Info.CurrentPlayers == 2;
        }

        private static bool EightSeatEmptyFrames()
        {
            var gate = new LockstepFrameGate();
            var players = new PlayerId[8];
            for (int i = 0; i < 8; i++)
                players[i] = new PlayerId((byte)i);
            gate.SetExpectedPlayers(players);

            // Prime delay window like LockstepMatchCoordinator (2 ticks)
            for (uint t = 0; t < 2; t++)
            {
                for (int i = 0; i < 8; i++)
                    gate.SubmitEmpty(new Tick(t), players[i]);
            }

            var buffer = new List<GameCommand>();
            return gate.TryConsume(new Tick(0), buffer)
                   && gate.TryConsume(new Tick(1), buffer)
                   && !gate.TryConsume(new Tick(2), buffer);
        }

        private static bool Await(Task<bool> task) => task.GetAwaiter().GetResult();

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
