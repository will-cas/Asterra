using System.Text;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Sim;

namespace Asterra.Gameplay
{
    /// <summary>Move / stop / patrol / attack-move / stance / rally command coverage.</summary>
    public static class OrdersSelfTest
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;

            Expect(ref fails, sb, "move sets path", MoveSetsPath());
            Expect(ref fails, sb, "stop clears orders", StopClearsOrders());
            Expect(ref fails, sb, "attack-move flags unit", AttackMoveFlags());
            Expect(ref fails, sb, "patrol flags unit", PatrolFlags());
            Expect(ref fails, sb, "stance hold applied", StanceHold());
            Expect(ref fails, sb, "rally stored on producer", RallyStored());
            Expect(ref fails, sb, "builder ignored by attack-move", BuilderIgnoredByAttackMove());
            Expect(ref fails, sb, "move advances position", MoveAdvances());
            Expect(ref fails, sb, "foreign issuer ignored", ForeignIssuerIgnored());

            sb.Append(fails == 0 ? "OrdersSelfTest: OK" : $"OrdersSelfTest: FAIL ({fails})");
            return sb.ToString();
        }

        private static bool MoveSetsPath()
        {
            Setup(out var sim, out var ids, out var p, out var unit);
            sim.ApplyCommands(new GameCommand[]
            {
                new MoveCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    TargetX = 80f,
                    TargetZ = 10f,
                },
            });
            return unit.PathCount > 0 || (unit.MoveTargetX.HasValue && unit.MoveTargetX.Value > 40f);
        }

        private static bool StopClearsOrders()
        {
            Setup(out var sim, out var ids, out var p, out var unit);
            sim.ApplyCommands(new GameCommand[]
            {
                new MoveCommand { Issuer = p, UnitIds = new[] { unit.Id }, TargetX = 90f, TargetZ = 0f },
            });
            sim.ApplyCommands(new GameCommand[]
            {
                new StopCommand { Issuer = p, UnitIds = new[] { unit.Id } },
            });
            return !unit.MoveTargetX.HasValue
                   && !unit.AttackTargetId.HasValue
                   && !unit.AttackMoving
                   && !unit.Patrolling;
        }

        private static bool AttackMoveFlags()
        {
            Setup(out var sim, out _, out var p, out var unit);
            sim.ApplyCommands(new GameCommand[]
            {
                new AttackMoveCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    TargetX = 50f,
                    TargetZ = -20f,
                },
            });
            return unit.AttackMoving;
        }

        private static bool PatrolFlags()
        {
            Setup(out var sim, out _, out var p, out var unit);
            sim.ApplyCommands(new GameCommand[]
            {
                new PatrolCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    TargetX = 40f,
                    TargetZ = 40f,
                },
            });
            return unit.Patrolling;
        }

        private static bool StanceHold()
        {
            Setup(out var sim, out _, out var p, out var unit);
            sim.ApplyCommands(new GameCommand[]
            {
                new SetStanceCommand
                {
                    Issuer = p,
                    UnitIds = new[] { unit.Id },
                    Stance = UnitStance.Hold,
                },
            });
            return unit.Stance == UnitStance.Hold;
        }

        private static bool RallyStored()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            var barracks = sim.SpawnBuilding(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.ArcaneAcademyId, 0f, 0f, startActive: true);
            sim.ApplyCommands(new GameCommand[]
            {
                new SetRallyCommand
                {
                    Issuer = p,
                    BuildingId = barracks.Id,
                    TargetX = 123f,
                    TargetZ = -45f,
                },
            });
            return barracks.RallyX.HasValue
                   && barracks.RallyZ.HasValue
                   && Near(barracks.RallyX.Value, 123f, 0.01f)
                   && Near(barracks.RallyZ.Value, -45f, 0.01f);
        }

        private static bool BuilderIgnoredByAttackMove()
        {
            var ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            var sim = new SkirmishWorldSim(wallet, ids, defs);
            var p = new PlayerId(0);
            var builder = sim.SpawnUnit(
                ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledBuilderId, 0f, 0f);
            sim.ApplyCommands(new GameCommand[]
            {
                new AttackMoveCommand
                {
                    Issuer = p,
                    UnitIds = new[] { builder.Id },
                    TargetX = 60f,
                    TargetZ = 0f,
                },
            });
            return !builder.AttackMoving;
        }

        private static bool MoveAdvances()
        {
            Setup(out var sim, out _, out var p, out var unit);
            float x0 = unit.X;
            sim.ApplyCommands(new GameCommand[]
            {
                new MoveCommand { Issuer = p, UnitIds = new[] { unit.Id }, TargetX = 100f, TargetZ = 0f },
            });
            for (int i = 0; i < 20; i++)
                sim.Tick(0.25f);
            return unit.X > x0 + 2f;
        }

        private static bool ForeignIssuerIgnored()
        {
            Setup(out var sim, out _, out var p, out var unit);
            float x0 = unit.X;
            sim.ApplyCommands(new GameCommand[]
            {
                new MoveCommand
                {
                    Issuer = new PlayerId(1),
                    UnitIds = new[] { unit.Id },
                    TargetX = 200f,
                    TargetZ = 0f,
                },
            });
            return !unit.MoveTargetX.HasValue && Near(unit.X, x0, 0.01f);
        }

        private static void Setup(
            out SkirmishWorldSim sim,
            out SequentialIdFactory ids,
            out PlayerId p,
            out SimUnit unit)
        {
            ids = new SequentialIdFactory();
            var wallet = new ResourceWallet();
            var defs = new DefinitionRegistry();
            FactionDefaultContent.RegisterAll(defs);
            sim = new SkirmishWorldSim(wallet, ids, defs);
            p = new PlayerId(0);
            unit = sim.SpawnUnit(ids.Next(), p, new FactionId(0), FactionDefaultContent.VeiledApprenticeId, 0f, 0f);
        }

        private static bool Near(float a, float b, float eps) => Abs(a - b) <= eps;
        private static float Abs(float v) => v < 0f ? -v : v;

        private static void Expect(ref int fails, StringBuilder sb, string name, bool ok)
        {
            if (ok)
                return;
            fails++;
            sb.AppendLine("  fail: " + name);
        }
    }
}
