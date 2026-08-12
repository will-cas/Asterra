using Asterra.Core;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Composition root for a local skirmish. Wire Netcode session later without rewriting gameplay.
    /// </summary>
    public sealed class MatchBootstrap : MonoBehaviour
    {
        [SerializeField] private FactionDefinition[] factions = new FactionDefinition[3];
        [SerializeField] private float tickHz = 20f;
        [SerializeField] private int commandDelayTicks = 2;
        [SerializeField] private int startingGold = 500;
        [SerializeField] private int startingTimber = 300;

        public IMatchSession Session { get; private set; }
        public ILockstepClock Clock { get; private set; }
        public ICommandBus Commands { get; private set; }
        public IWorldSim World { get; private set; }
        public IResourceWallet Wallet { get; private set; }
        public IFactionCatalog Factions { get; private set; }
        public IIdFactory Ids { get; private set; }

        private CommandBus _commandBus;
        private float _accum;

        private void Awake()
        {
            Ids = new SequentialIdFactory();
            Wallet = new ResourceWallet();
            _commandBus = new CommandBus();
            Commands = _commandBus;
            Clock = new LockstepClock(1f / Mathf.Max(1f, tickHz), commandDelayTicks);
            World = new SkirmishWorldSim(Wallet);
            Factions = new FactionCatalog(factions);
            Session = new LocalMatchSession(new PlayerId(0), playerCount: 1);

            var local = Session.LocalPlayer;
            Wallet.Seed(local, ResourceType.Gold, startingGold);
            Wallet.Seed(local, ResourceType.Timber, startingTimber);
        }

        private void Update()
        {
            _accum += Time.deltaTime;
            float step = Clock.FixedDeltaSeconds;
            while (_accum >= step)
            {
                _accum -= step;
                SimulateOneTick();
            }
        }

        private void SimulateOneTick()
        {
            var target = new Tick(Clock.CurrentTick.Value + (uint)Clock.CommandDelayTicks);
            _commandBus.ScheduleLocal(target);

            var frameCommands = Commands.DrainForTick(Clock.CurrentTick);
            World.ApplyCommands(frameCommands);
            World.Tick(Clock.FixedDeltaSeconds);
            Clock.Advance();
        }

        private sealed class LocalMatchSession : IMatchSession
        {
            public LocalMatchSession(PlayerId local, int playerCount)
            {
                LocalPlayer = local;
                PlayerCount = playerCount;
                IsInMatch = true;
            }

            public bool IsInMatch { get; }
            public int PlayerCount { get; }
            public PlayerId LocalPlayer { get; }
        }
    }
}
