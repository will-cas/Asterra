using Asterra.AI;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Player;
using Asterra.Gameplay.Sim;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Composition root for a local 1v1 skirmish sandbox (player 0 vs dummy enemy 1).
    /// </summary>
    public sealed class MatchBootstrap : MonoBehaviour
    {
        [SerializeField] private FactionDefinition[] factions = new FactionDefinition[3];
        [SerializeField] private int playerFactionIndex;
        [SerializeField] private int enemyFactionIndex = 1;
        [SerializeField] private float tickHz = 20f;
        [SerializeField] private int commandDelayTicks = 2;
        [SerializeField] private int startingGold = 500;
        [SerializeField] private int startingTimber = 300;
        [SerializeField] private int enemyStartingGold = 500;
        [SerializeField] private bool attachLocalOrders = true;
        [SerializeField] private bool runSmokeOnAwake;
        [SerializeField] private bool reportHashEveryTick;
        [SerializeField] private uint matchSeed = 42;

        public IMatchSession Session { get; private set; }
        public ILockstepClock Clock { get; private set; }
        public ICommandBus Commands { get; private set; }
        public IWorldSim World { get; private set; }
        public IResourceWallet Wallet { get; private set; }
        public IFactionCatalog Factions { get; private set; }
        public IIdFactory Ids { get; private set; }
        public DefinitionRegistry Definitions { get; private set; }
        public ReplayBuffer Replay { get; private set; }
        public DeterministicRandom Random { get; private set; }
        public FactionRoster PlayerRoster { get; private set; }
        public FactionRoster EnemyRoster { get; private set; }

        private CommandBus _commandBus;
        private SkirmishWorldSim _sim;
        private IArmyBrain _enemyBrain;
        private float _accum;
        private LocalOrderController _orders;

        private void Awake()
        {
            PlayerRoster = FactionDefaultContent.Get(new FactionId((byte)Mathf.Clamp(playerFactionIndex, 0, 2)));
            EnemyRoster = FactionDefaultContent.Get(new FactionId((byte)Mathf.Clamp(enemyFactionIndex, 0, 2)));
            if (EnemyRoster.Id == PlayerRoster.Id)
                EnemyRoster = FactionDefaultContent.Get(new FactionId((byte)((PlayerRoster.Id.Value + 1) % 3)));

            Ids = new SequentialIdFactory();
            Wallet = new ResourceWallet();
            _commandBus = new CommandBus();
            Commands = _commandBus;
            Clock = new LockstepClock(1f / Mathf.Max(1f, tickHz), commandDelayTicks);
            Definitions = SkirmishDefaultContent.CreateRegistry();
            Replay = new ReplayBuffer();
            Random = new DeterministicRandom(matchSeed);
            _sim = new SkirmishWorldSim(Wallet, Ids, Definitions);
            World = _sim;
            Factions = new FactionCatalog(factions);
            Session = new LocalMatchSession(new PlayerId(0), playerCount: 2);

            var local = Session.LocalPlayer;
            var enemy = new PlayerId(1);
            Wallet.Seed(local, ResourceType.Gold, startingGold);
            Wallet.Seed(local, ResourceType.Timber, startingTimber);
            Wallet.Seed(enemy, ResourceType.Gold, enemyStartingGold);
            Wallet.Seed(enemy, ResourceType.Timber, startingTimber);

            SkirmishDefaultContent.PopulateInitialWorld(_sim, Ids, PlayerRoster, EnemyRoster);
            _enemyBrain = new DummyEnemyCamp(
                enemy,
                EnemyRoster.KeepBuildingId,
                EnemyRoster.BasicUnitId);

            if (attachLocalOrders)
            {
                _orders = gameObject.GetComponent<LocalOrderController>();
                if (_orders == null)
                    _orders = gameObject.AddComponent<LocalOrderController>();
                _orders.Bind(this);
            }

            if (runSmokeOnAwake)
                Debug.Log(SkirmishSmokeTest.Run());
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

            var aiCommands = _enemyBrain.Think(new ArmyBrainContext(Clock.CurrentTick, World, Wallet));
            if (aiCommands.Count > 0)
            {
                var copy = new GameCommand[aiCommands.Count];
                for (int i = 0; i < aiCommands.Count; i++)
                {
                    copy[i] = aiCommands[i];
                    copy[i].IssueTick = target;
                }

                var frame = new CommandFrame
                {
                    TargetTick = target,
                    Player = _enemyBrain.Player,
                    Commands = copy,
                };
                Replay.Record(frame);
                _commandBus.EnqueueRemote(frame);
            }

            _commandBus.ScheduleLocal(target);

            var frameCommands = Commands.DrainForTick(Clock.CurrentTick);
            if (frameCommands.Count > 0)
            {
                Replay.Record(new CommandFrame
                {
                    TargetTick = Clock.CurrentTick,
                    Player = Session.LocalPlayer,
                    Commands = ToArray(frameCommands),
                });
            }

            World.ApplyCommands(frameCommands);
            World.Tick(Clock.FixedDeltaSeconds);

            if (reportHashEveryTick && Clock.CurrentTick.Value % 20 == 0)
                Debug.Log($"[Asterra] tick={Clock.CurrentTick.Value} hash={World.ComputeWorldHash()}");

            Clock.Advance();
        }

        private static GameCommand[] ToArray(System.Collections.Generic.IReadOnlyList<GameCommand> list)
        {
            var arr = new GameCommand[list.Count];
            for (int i = 0; i < list.Count; i++)
                arr[i] = list[i];
            return arr;
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
