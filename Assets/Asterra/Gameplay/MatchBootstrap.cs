using System.Collections.Generic;
using Asterra.AI;
using Asterra.Core;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Player;
using Asterra.Gameplay.Presentation;
using Asterra.Gameplay.Sim;
using Asterra.Net;
using UnityEngine;

namespace Asterra.Gameplay
{
    public enum MatchPlayMode : byte
    {
        OfflineVsAi = 0,
        Online = 1,
    }

    /// <summary>
    /// Single composition root for offline AI skirmish and online lockstep matches.
    /// </summary>
    public sealed class MatchBootstrap : MonoBehaviour
    {
        [SerializeField] private MatchPlayMode playMode = MatchPlayMode.OfflineVsAi;
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
        [SerializeField] private LockstepMatchCoordinator coordinator;
        [SerializeField] private LockstepNetworkBridge networkBridge;
        [SerializeField] private bool autoStartOffline = true;
        [SerializeField] private float territoryHoldSecondsToWin = 90f;
        [SerializeField] private bool attachPresentation = true;
        [SerializeField] private bool attachCameraRig = true;

        public IMatchSession Session { get; private set; }
        public ILockstepClock Clock => coordinator != null ? coordinator.Clock : null;
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
        public MatchLobbyState Lobby { get; private set; }
        public MatchPlayMode PlayMode => playMode;
        public bool IsMatchRunning { get; private set; }
        public MatchResult Result { get; private set; } = MatchResult.None;
        public VictoryEvaluator Victory { get; private set; }

        private CommandBus _commandBus;
        private SkirmishWorldSim _sim;
        private LocalOrderController _orders;
        private readonly List<PlayerId> _participants = new();

        private void Awake()
        {
            if (coordinator == null)
                coordinator = GetComponent<LockstepMatchCoordinator>();
            if (coordinator == null)
                coordinator = gameObject.AddComponent<LockstepMatchCoordinator>();

            Lobby = new MatchLobbyState { MatchSeed = matchSeed, MaxPlayers = 8 };
            Definitions = SkirmishDefaultContent.CreateRegistry();
            Factions = new FactionCatalog(factions);
            Replay = new ReplayBuffer();
            _commandBus = new CommandBus();
            Commands = _commandBus;

            if (runSmokeOnAwake)
                Debug.Log(SkirmishSmokeTest.Run());

            if (playMode == MatchPlayMode.OfflineVsAi && autoStartOffline)
                StartOfflineVsAi();
        }

        public void SetPlayMode(MatchPlayMode mode) => playMode = mode;

        public void StartOfflineVsAi()
        {
            playMode = MatchPlayMode.OfflineVsAi;
            PlayerRoster = FactionDefaultContent.Get(new FactionId((byte)Mathf.Clamp(playerFactionIndex, 0, 2)));
            EnemyRoster = FactionDefaultContent.Get(new FactionId((byte)Mathf.Clamp(enemyFactionIndex, 0, 2)));
            if (EnemyRoster.Id == PlayerRoster.Id)
                EnemyRoster = FactionDefaultContent.Get(new FactionId((byte)((PlayerRoster.Id.Value + 1) % 3)));

            var local = new PlayerId(0);
            var enemy = new PlayerId(1);
            Lobby = new MatchLobbyState { MatchSeed = matchSeed, MaxPlayers = 8 };
            Lobby.ClaimSlot(local, "Player");
            Lobby.ClaimSlot(enemy, "Enemy AI");
            Lobby.SetFaction(local, PlayerRoster.Id.Value);
            Lobby.SetFaction(enemy, EnemyRoster.Id.Value);
            Lobby.SetReady(local, true);
            Lobby.SetReady(enemy, true);
            Lobby.TryStart(out _);

            BeginMatchFromLobby(local, includeAi: true);
        }

        /// <summary>Online path: call after lobby TryStart succeeds on all peers.</summary>
        public void StartOnlineFromLobby(PlayerId localPlayer, MatchStartInfo startInfo)
        {
            playMode = MatchPlayMode.Online;
            if (startInfo == null)
                throw new System.ArgumentNullException(nameof(startInfo));

            matchSeed = startInfo.Seed;
            PlayerSlotState localSlot = null;
            PlayerSlotState firstRemote = null;
            for (int i = 0; i < startInfo.Players.Length; i++)
            {
                var slot = startInfo.Players[i];
                if (slot.Player == localPlayer)
                    localSlot = slot;
                else if (firstRemote == null)
                    firstRemote = slot;
            }

            if (localSlot == null)
                throw new System.InvalidOperationException("Local player missing from MatchStartInfo.");

            PlayerRoster = FactionDefaultContent.Get(new FactionId(localSlot.FactionIndex));
            EnemyRoster = firstRemote != null
                ? FactionDefaultContent.Get(new FactionId(firstRemote.FactionIndex))
                : FactionDefaultContent.Get(new FactionId((byte)((localSlot.FactionIndex + 1) % 3)));

            BeginMatchFromLobby(localPlayer, includeAi: false, startInfo.Players);
        }

        private void BeginMatchFromLobby(PlayerId localPlayer, bool includeAi, PlayerSlotState[] onlinePlayers = null)
        {
            Ids = new SequentialIdFactory();
            Wallet = new ResourceWallet();
            Random = new DeterministicRandom(matchSeed);
            _sim = new SkirmishWorldSim(Wallet, Ids, Definitions);
            World = _sim;
            Session = new LocalMatchSession(localPlayer, includeAi ? 2 : (onlinePlayers?.Length ?? 1));

            _participants.Clear();
            if (onlinePlayers != null)
            {
                for (int i = 0; i < onlinePlayers.Length; i++)
                {
                    var slot = onlinePlayers[i];
                    _participants.Add(slot.Player);
                    Wallet.Seed(slot.Player, ResourceType.Gold, startingGold);
                    Wallet.Seed(slot.Player, ResourceType.Timber, startingTimber);
                }
            }
            else
            {
                _participants.Add(localPlayer);
                _participants.Add(new PlayerId(1));
                Wallet.Seed(localPlayer, ResourceType.Gold, startingGold);
                Wallet.Seed(localPlayer, ResourceType.Timber, startingTimber);
                Wallet.Seed(new PlayerId(1), ResourceType.Gold, enemyStartingGold);
                Wallet.Seed(new PlayerId(1), ResourceType.Timber, startingTimber);
            }

            // Deterministic seats: sorted player ids → west/east. Never local-centric.
            PlayerSlotState[] seats;
            if (onlinePlayers != null)
            {
                seats = onlinePlayers;
            }
            else
            {
                seats = new[]
                {
                    new PlayerSlotState
                    {
                        Player = localPlayer,
                        FactionIndex = PlayerRoster.Id.Value,
                        IsReady = true,
                        DisplayName = "Player",
                    },
                    new PlayerSlotState
                    {
                        Player = new PlayerId(1),
                        FactionIndex = EnemyRoster.Id.Value,
                        IsReady = true,
                        DisplayName = "Enemy AI",
                    },
                };
                System.Array.Sort(seats, (a, b) => a.Player.Value.CompareTo(b.Player.Value));
            }

            SkirmishDefaultContent.PopulateFromSlots(_sim, Ids, seats);

            coordinator.ConfigureTiming(tickHz, commandDelayTicks);
            coordinator.SetBridge(networkBridge);
            coordinator.Initialize(_sim, _commandBus, localPlayer, _participants, Replay, networkBridge, Wallet);

            if (includeAi)
            {
                var brain = new DummyEnemyCamp(
                    new PlayerId(1),
                    EnemyRoster.KeepBuildingId,
                    EnemyRoster.ProducerBuildingId,
                    EnemyRoster.BuilderUnitId,
                    EnemyRoster.BasicUnitId);
                coordinator.AddContributor(new ArmyBrainFrameContributor(brain, _sim, Wallet));
            }

            if (attachPresentation)
            {
                var presentation = FindFirstObjectByType<SimPresentationBridge>();
                if (presentation == null)
                    presentation = gameObject.AddComponent<SimPresentationBridge>();

                if (FindFirstObjectByType<FogOfWarPresenter>() == null)
                    gameObject.AddComponent<FogOfWarPresenter>();
            }

            if (attachLocalOrders)
            {
                _orders = gameObject.GetComponent<LocalOrderController>();
                if (_orders == null)
                    _orders = gameObject.AddComponent<LocalOrderController>();
                _orders.Bind(this);
            }

            if (GetComponent<MatchHud>() == null)
                gameObject.AddComponent<MatchHud>();

            if (attachCameraRig)
            {
                var camRig = FindFirstObjectByType<RtsCameraRig>();
                if (camRig == null)
                    camRig = gameObject.AddComponent<RtsCameraRig>();
                camRig.FocusOn(-320f, 0f, height: 240f, back: 42f);
            }
            var keepIds = new[]
            {
                FactionDefaultContent.IronKeepId,
                FactionDefaultContent.HeartwoodId,
                FactionDefaultContent.AshCitadelId,
            };
            Victory = new VictoryEvaluator(keepIds, territoryHoldSecondsToWin);
            Result = MatchResult.None;
            coordinator.TickAdvanced += OnTickAdvanced;

            IsMatchRunning = true;
            Debug.Log($"[Asterra] Match started mode={playMode} seed={matchSeed} players={_participants.Count}");
        }

        private void OnTickAdvanced(Tick tick, ulong hash)
        {
            if (reportHashEveryTick && tick.Value % 20 == 0)
                Debug.Log($"[Asterra] tick={tick.Value} hash={hash}");

            if (Result.IsOver || Victory == null || World == null || coordinator == null)
                return;

            float dt = coordinator.Clock.FixedDeltaSeconds;
            var result = Victory.Evaluate(World, dt, _participants);
            if (!result.IsOver)
                return;

            Result = result;
            IsMatchRunning = false;
            coordinator.Stop();
            Debug.Log($"[Asterra] MATCH OVER winner=P{result.Winner.Value} reason={result.Reason}");
        }

        private void OnDestroy()
        {
            if (coordinator != null)
                coordinator.TickAdvanced -= OnTickAdvanced;
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
