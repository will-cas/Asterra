using Asterra.Core;

namespace Asterra.Gameplay
{
    public sealed class LockstepClock : ILockstepClock
    {
        public Tick CurrentTick { get; private set; }
        public int CommandDelayTicks { get; }
        public float FixedDeltaSeconds { get; }

        public LockstepClock(float fixedDeltaSeconds = 0.05f, int commandDelayTicks = 2)
        {
            FixedDeltaSeconds = fixedDeltaSeconds;
            CommandDelayTicks = commandDelayTicks;
            CurrentTick = new Tick(0);
        }

        public void Advance() => CurrentTick = CurrentTick.Next();
    }
}
