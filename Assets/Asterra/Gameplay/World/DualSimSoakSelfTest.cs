using System.Collections.Generic;
using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Long dual-sim soak: same command stream → identical world hash.</summary>
    public static class DualSimSoakSelfTest
    {
        public const int DefaultTicks = 800;

        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "short soak matches", Soak(120));
            Expect(ref fails, sb, "medium soak matches", Soak(DefaultTicks));
            Expect(ref fails, sb, "replay buffer length", ReplayLengthOk());
            Expect(ref fails, sb, "diverged command fails hash", DivergedFails());

            sb.Append(fails == 0 ? "DualSimSoakSelfTest: OK" : $"DualSimSoakSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool Soak(int ticks)
        {
            var frames = BuildScript(ticks);
            var a = Boot(out _);
            var b = Boot(out _);
            for (int i = 0; i < frames.Count; i++)
            {
                a.ApplyCommands(frames[i].Commands);
                b.ApplyCommands(frames[i].Commands);
                a.Tick(0.05f);
                b.Tick(0.05f);
            }

            return a.ComputeWorldHash() == b.ComputeWorldHash()
                   && a.Units.Count == b.Units.Count
                   && a.Buildings.Count == b.Buildings.Count;
        }

        private static bool ReplayLengthOk()
        {
            var frames = BuildScript(40);
            var replay = new ReplayBuffer();
            for (int i = 0; i < frames.Count; i++)
                replay.Record(frames[i]);
            return replay.Count == frames.Count && replay.GetFrame(0).Commands.Length > 0;
        }

        private static bool DivergedFails()
        {
            var frames = BuildScript(60);
            var a = Boot(out _);
            var b = Boot(out _);
            for (int i = 0; i < frames.Count; i++)
            {
                a.ApplyCommands(frames[i].Commands);
                if (i == frames.Count / 2)
                {
                    // Intentional divergence on B only.
                    b.ApplyCommands(new GameCommand[]
                    {
                        new StopCommand
                        {
                            Issuer = new PlayerId(0),
                            UnitIds = FirstUnits(b, new PlayerId(0), 1),
                        },
                    });
                }
                else
                {
                    b.ApplyCommands(frames[i].Commands);
                }

                a.Tick(0.05f);
                b.Tick(0.05f);
            }

            return a.ComputeWorldHash() != b.ComputeWorldHash();
        }

        private static List<CommandFrame> BuildScript(int ticks)
        {
            var list = new List<CommandFrame>(ticks);
            // Discover unit ids from a throwaway boot so both sims share the same script ids.
            var probe = Boot(out var probeWallet);
            _ = probeWallet;
            var p0 = new PlayerId(0);
            var units = FirstUnits(probe, p0, 4);
            SimEntityId keep = default;
            for (int i = 0; i < probe.Buildings.Count; i++)
            {
                if (probe.Buildings[i].Owner == p0 && probe.Buildings[i].CanProduce)
                {
                    keep = probe.Buildings[i].Id;
                    break;
                }
            }

            for (uint t = 1; t <= (uint)ticks; t++)
            {
                GameCommand[] cmds;
                if (t % 40 == 5 && keep.Value != 0)
                {
                    cmds = new GameCommand[]
                    {
                        new TrainUnitCommand
                        {
                            Issuer = p0,
                            IssueTick = new Tick(t),
                            BuildingId = keep,
                            UnitDefId = FactionDefaultContent.IronBuilderId,
                        },
                    };
                }
                else if (t % 17 == 0 && units.Length > 0)
                {
                    cmds = new GameCommand[]
                    {
                        new MoveCommand
                        {
                            Issuer = p0,
                            IssueTick = new Tick(t),
                            UnitIds = units,
                            TargetX = 20f + (t % 50),
                            TargetZ = (t % 7) * 3f,
                        },
                    };
                }
                else if (t % 23 == 0 && units.Length > 0)
                {
                    cmds = new GameCommand[]
                    {
                        new AttackMoveCommand
                        {
                            Issuer = p0,
                            IssueTick = new Tick(t),
                            UnitIds = units,
                            TargetX = -10f + (t % 30),
                            TargetZ = 15f,
                        },
                    };
                }
                else
                {
                    cmds = System.Array.Empty<GameCommand>();
                }

                list.Add(new CommandFrame
                {
                    TargetTick = new Tick(t),
                    Player = p0,
                    Commands = cmds,
                });
            }

            return list;
        }

        private static SkirmishWorldSim Boot(out ResourceWallet wallet)
        {
            var ids = new SequentialIdFactory();
            wallet = new ResourceWallet();
            var defs = SkirmishDefaultContent.CreateRegistry();
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p0 = new PlayerId(0);
            var p1 = new PlayerId(1);
            wallet.Seed(p0, ResourceType.Gold, 2000);
            wallet.Seed(p0, ResourceType.Timber, 800);
            wallet.Seed(p1, ResourceType.Gold, 500);
            wallet.Seed(p1, ResourceType.Timber, 300);
            SkirmishDefaultContent.PopulateInitialWorld(
                sim,
                ids,
                FactionDefaultContent.IronCovenant,
                FactionDefaultContent.VerdantCourt);
            return sim;
        }

        private static SimEntityId[] FirstUnits(SkirmishWorldSim sim, PlayerId owner, int max)
        {
            var list = new List<SimEntityId>(max);
            for (int i = 0; i < sim.Units.Count && list.Count < max; i++)
            {
                if (sim.Units[i].Owner == owner && sim.Units[i].IsAlive)
                    list.Add(sim.Units[i].Id);
            }

            return list.ToArray();
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
