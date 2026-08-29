using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>World-hash determinism, replay playback, and desync detector.</summary>
    public static class WorldHashSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "identical sims match hash", IdenticalSimsMatch());
            Expect(ref fails, sb, "idle ticks stay deterministic", IdleTicksDeterministic());
            Expect(ref fails, sb, "move diverges hash", MoveDivergesHash());
            Expect(ref fails, sb, "hash changes after place", PlaceChangesHash());
            Expect(ref fails, sb, "replay payloads reproduce hash", ReplayPayloadsMatch());
            Expect(ref fails, sb, "desync detector flags mismatch", DesyncDetectorFlags());
            Expect(ref fails, sb, "desync detector agrees when equal", DesyncDetectorAgrees());
            Expect(ref fails, sb, "forget before drops old ticks", DesyncForget());

            sb.Append(fails == 0 ? "WorldHashSelfTest: OK" : $"WorldHashSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool IdenticalSimsMatch()
        {
            var a = BuildTwin(out var cmds, out _);
            var b = BuildTwin(out _, out _);
            ApplyAll(a, cmds);
            ApplyAll(b, cmds);
            for (int i = 0; i < 40; i++)
            {
                a.Tick(0.25f);
                b.Tick(0.25f);
            }

            return a.ComputeWorldHash() == b.ComputeWorldHash();
        }

        private static bool IdleTicksDeterministic()
        {
            var a = BuildTwin(out _, out _);
            var b = BuildTwin(out _, out _);
            for (int i = 0; i < 30; i++)
            {
                a.Tick(0.25f);
                b.Tick(0.25f);
            }

            return a.ComputeWorldHash() == b.ComputeWorldHash();
        }

        private static bool MoveDivergesHash()
        {
            var a = BuildTwin(out _, out _);
            var b = BuildTwin(out _, out _);
            var p = new PlayerId(0);
            SimEntityId unitId = FirstOwnedUnit(a, p);
            a.ApplyCommands(new GameCommand[]
            {
                new MoveCommand { Issuer = p, UnitIds = new[] { unitId }, TargetX = 80f, TargetZ = 0f },
            });
            for (int i = 0; i < 20; i++)
            {
                a.Tick(0.25f);
                b.Tick(0.25f);
            }

            return a.ComputeWorldHash() != b.ComputeWorldHash();
        }

        private static bool PlaceChangesHash()
        {
            var sim = BuildTwin(out _, out var wallet);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 2000);
            wallet.Seed(p, ResourceType.Timber, 2000);
            ulong before = sim.ComputeWorldHash();
            sim.ApplyCommands(new GameCommand[]
            {
                new PlaceBuildingCommand
                {
                    Issuer = p,
                    BuildingDefId = FactionDefaultContent.ArcaneAcademyId,
                    X = 40f,
                    Z = 40f,
                },
            });
            return sim.ComputeWorldHash() != before;
        }

        private static bool ReplayPayloadsMatch()
        {
            var live = BuildTwin(out var seedCmds, out _);
            var replay = new ReplayBuffer();
            ApplyAll(live, seedCmds);
            var p = new PlayerId(0);
            SimEntityId unitId = FirstOwnedUnit(live, p);

            for (uint t = 1; t <= 8; t++)
            {
                var frame = new CommandFrame
                {
                    TargetTick = new Tick(t),
                    Player = p,
                    Commands = new GameCommand[]
                    {
                        new MoveCommand
                        {
                            Issuer = p,
                            IssueTick = new Tick(t),
                            UnitIds = new[] { unitId },
                            TargetX = 10f * t,
                            TargetZ = 5f,
                        },
                    },
                };
                replay.Record(frame);
                live.ApplyCommands(frame.Commands);
                live.Tick(0.25f);
            }

            var mirror = BuildTwin(out var seed2, out _);
            ApplyAll(mirror, seed2);
            for (int i = 0; i < replay.Count; i++)
            {
                var frame = replay.GetFrame(i);
                mirror.ApplyCommands(frame.Commands);
                mirror.Tick(0.25f);
            }

            return live.ComputeWorldHash() == mirror.ComputeWorldHash() && replay.Count == 8;
        }

        private static bool DesyncDetectorFlags()
        {
            var d = new DesyncDetector();
            d.Report(10, 0, 111ul);
            d.Report(10, 1, 222ul);
            return d.TryGetDesync(10, out var expected, out var actual)
                   && expected == 111ul
                   && actual == 222ul;
        }

        private static bool DesyncDetectorAgrees()
        {
            var d = new DesyncDetector();
            d.Report(3, 0, 999ul);
            d.Report(3, 1, 999ul);
            return !d.TryGetDesync(3, out _, out _);
        }

        private static bool DesyncForget()
        {
            var d = new DesyncDetector();
            d.Report(1, 0, 1ul);
            d.Report(1, 1, 2ul);
            d.ForgetBefore(2);
            return !d.TryGetDesync(1, out _, out _);
        }

        private static SkirmishWorldSim BuildTwin(out GameCommand[] seedCmds, out ResourceWallet wallet)
        {
            var ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            wallet.Seed(p, ResourceType.Gold, 500);
            wallet.Seed(p, ResourceType.Timber, 200);
            sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneumId, 0f, 0f, startActive: true);
            var u = sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 12f, 0f);
            sim.AddResourceNode(ids.Next(), ResourceType.Gold, 200, 30f, 0f);
            seedCmds = new GameCommand[]
            {
                new SetStanceCommand
                {
                    Issuer = p,
                    UnitIds = new[] { u.Id },
                    Stance = UnitStance.Defensive,
                },
            };
            return sim;
        }

        private static SimEntityId FirstOwnedUnit(SkirmishWorldSim sim, PlayerId p)
        {
            for (int i = 0; i < sim.Units.Count; i++)
            {
                if (sim.Units[i].Owner == p)
                    return sim.Units[i].Id;
            }

            return default;
        }

        private static void ApplyAll(SkirmishWorldSim sim, GameCommand[] cmds)
        {
            if (cmds == null || cmds.Length == 0)
                return;
            sim.ApplyCommands(cmds);
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
