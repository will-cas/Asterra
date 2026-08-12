using System.Collections.Generic;
using System.Text;
using Asterra.Core;

namespace Asterra.Core
{
    public static class LockstepFrameGateSelfTest
    {
        public static string Run()
        {
            var gate = new LockstepFrameGate();
            gate.SetExpectedPlayers(new[] { new PlayerId(0), new PlayerId(1) });

            var tick = new Tick(5);
            gate.Submit(new CommandFrame
            {
                TargetTick = tick,
                Player = new PlayerId(0),
                Commands = new GameCommand[]
                {
                    new MoveCommand
                    {
                        Issuer = new PlayerId(0),
                        UnitIds = new[] { new EntityId(1) },
                        TargetX = 1f,
                        TargetZ = 2f,
                    },
                },
            });

            var buffer = new List<GameCommand>();
            if (gate.TryConsume(tick, buffer))
                throw new System.InvalidOperationException("Gate opened before all players submitted.");

            gate.SubmitEmpty(tick, new PlayerId(1));
            if (!gate.TryConsume(tick, buffer))
                throw new System.InvalidOperationException("Gate failed to open after all players submitted.");
            if (buffer.Count != 1 || !(buffer[0] is MoveCommand))
                throw new System.InvalidOperationException("Unexpected consumed commands.");

            var sb = new StringBuilder();
            sb.AppendLine("[Asterra LockstepGate]");
            sb.AppendLine("status=ok");
            return sb.ToString();
        }
    }
}
