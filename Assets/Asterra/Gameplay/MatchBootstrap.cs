using System.Collections.Generic;
using Asterra.AI;
using Asterra.Core;
using Asterra.Gameplay.Audio;
using Asterra.Gameplay.Content;
using Asterra.Gameplay.Player;
using Asterra.Gameplay.Presentation;
using Asterra.Gameplay.Sim;
using Asterra.Gameplay.World;
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
        [SerializeField] private SkirmishMapId mapId = SkirmishMapId.BlackridgePass;
        [SerializeField] private string mapKey = MapCatalog.BlackridgePassId;
        [SerializeField] private float tickHz = 20f;
        [SerializeField] private int commandDelayTicks = 2;
        [SerializeField] private int startingGold = 500;
        [SerializeField] private int startingTimber = 300;
        [SerializeField] private int enemyStartingGold = 500;
        [SerializeField] private AiDifficulty aiDifficulty = AiDifficulty.Normal;
        [SerializeField] [Range(0, 1)] private int localSpawnSeat;
        [SerializeField] private bool attachLocalOrders = true;
        [SerializeField] private bool runSmokeOnAwake;
        [SerializeField] private bool reportHashEveryTick;
        [SerializeField] private uint matchSeed = 42;
        [SerializeField] private LockstepMatchCoordinator coordinator;
        [SerializeField] private LockstepNetworkBridge networkBridge;
        [SerializeField] private bool autoStartOffline;
        [SerializeField] private float territoryHoldSecondsToWin = 90f;
        [SerializeField] private bool attachPresentation = true;
        [SerializeField] private bool attachCameraRig = true;

        public IMatchSession Session { get; private set; }
        public ILockstepClock Clock => coordinator != null ? coordinator.Clock : null;
        public uint MatchSeed => matchSeed;
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

        /// <summary>True while pause/options/profile overlay is open (blocks orders).</summary>
        public bool IsMenuOverlayOpen { get; set; }
        public MatchResult Result { get; private set; } = MatchResult.None;
        public VictoryEvaluator Victory { get; private set; }

        public int PlayerFactionIndex
        {
            get => playerFactionIndex;
            set => playerFactionIndex = Mathf.Clamp(value, 0, FactionDefaultContent.All.Length - 1);
        }

        public int EnemyFactionIndex
        {
            get => enemyFactionIndex;
            set => enemyFactionIndex = Mathf.Clamp(value, 0, FactionDefaultContent.All.Length - 1);
        }

        public SkirmishMapId MapId
        {
            get => mapId;
            set
            {
                mapId = value;
                mapKey = MapCatalog.BuiltinChoice(value).Id;
            }
        }

        /// <summary>Catalog id: built-in (twin_keeps, …) or custom designer map.</summary>
        public string MapKey
        {
            get => string.IsNullOrEmpty(mapKey) ? MapCatalog.BuiltinChoice(mapId).Id : mapKey;
            set
            {
                mapKey = value;
                if (MapCatalog.TryParseBuiltin(value, out var builtin))
                    mapId = builtin;
            }
        }

        public AiDifficulty AiDifficulty
        {
            get => aiDifficulty;
            set => aiDifficulty = value;
        }

        /// <summary>0 = west keep, 1 = east keep for the local offline player.</summary>
        public int LocalSpawnSeat
        {
            get => localSpawnSeat;
            set => localSpawnSeat = value <= 0 ? 0 : 1;
        }

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
            RegisterFactionScriptablePowers();
            Factions = new FactionCatalog(factions);
            Replay = new ReplayBuffer();
            _commandBus = new CommandBus();
            Commands = _commandBus;

            if (runSmokeOnAwake)
                Debug.Log(SkirmishSmokeTest.Run());

            if (playMode == MatchPlayMode.OfflineVsAi && autoStartOffline)
                StartOfflineVsAi();
            else if (playMode == MatchPlayMode.OfflineVsAi && !autoStartOffline && !IsMatchRunning)
            {
                if (GetComponent<OfflineMatchMenu>() == null
                    && FindFirstObjectByType<OfflineMatchMenu>() == null)
                    gameObject.AddComponent<OfflineMatchMenu>();
            }
        }

        public void SetPlayMode(MatchPlayMode mode) => playMode = mode;

        public void ConfigureAndStartOffline(int playerFaction, int enemyFaction, SkirmishMapId map)
        {
            ConfigureAndStartOffline(playerFaction, enemyFaction, MapCatalog.BuiltinChoice(map).Id, aiDifficulty);
        }

        public void ConfigureAndStartOffline(int playerFaction, int enemyFaction, string mapCatalogKey)
        {
            ConfigureAndStartOffline(playerFaction, enemyFaction, mapCatalogKey, aiDifficulty);
        }

        public void ConfigureAndStartOffline(
            int playerFaction,
            int enemyFaction,
            string mapCatalogKey,
            AiDifficulty difficulty)
        {
            ConfigureAndStartOffline(playerFaction, enemyFaction, mapCatalogKey, difficulty, localSpawnSeat);
        }

        public void ConfigureAndStartOffline(
            int playerFaction,
            int enemyFaction,
            string mapCatalogKey,
            AiDifficulty difficulty,
            int spawnSeat)
        {
            PlayerFactionIndex = playerFaction;
            EnemyFactionIndex = enemyFaction;
            MapKey = mapCatalogKey;
            aiDifficulty = difficulty;
            LocalSpawnSeat = spawnSeat;
            StartOfflineVsAi();
        }

        /// <summary>Tear down the finished match and show the offline lobby again.</summary>
        public void ReturnToMainMenu()
        {
            TeardownMatchRuntime();
            Result = MatchResult.None;
            IsMatchRunning = false;
            EnsureOfflineMenu(enabled: true);
            if (AsterraAudio.Instance != null)
                AsterraSettings.ApplyAudio();
        }

        /// <summary>Soft rematch with the same faction/map picks (no scene reload).</summary>
        public void RematchOffline()
        {
            TeardownMatchRuntime();
            Result = MatchResult.None;
            if (AsterraAudio.Instance != null)
                AsterraSettings.ApplyAudio();
            StartOfflineVsAi();
            var menu = FindFirstObjectByType<OfflineMatchMenu>();
            if (menu != null)
                menu.enabled = false;
        }

        /// <summary>Write quicksave for the running offline match.</summary>
        public bool SaveOfflineQuick()
        {
            if (!IsMatchRunning || _sim == null || Wallet == null)
            {
                MatchFeedback.Show("Nothing to save", AsterraSfx.Invalid);
                return false;
            }

            try
            {
                string path = Asterra.Gameplay.Save.OfflineMatchSaveService.SaveQuick(this, _sim, Wallet);
                MatchFeedback.Show("Game saved", AsterraSfx.OrderResearch);
                Debug.Log("[Asterra] Saved match to " + path);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Asterra] Save failed: " + e.Message);
                MatchFeedback.Show("Save failed", AsterraSfx.Invalid);
                return false;
            }
        }

        /// <summary>Load quicksave and resume offline skirmish.</summary>
        public bool LoadOfflineQuick()
        {
            if (!Asterra.Gameplay.Save.OfflineMatchSaveService.TryLoadQuick(out var data))
            {
                MatchFeedback.Show("No save found", AsterraSfx.Invalid);
                return false;
            }

            return LoadOfflineFromSave(data);
        }

        public bool LoadOfflineFromSave(Asterra.Gameplay.Save.MatchSaveData data)
        {
            if (data == null)
                return false;

            TeardownMatchRuntime();
            Result = MatchResult.None;
            if (AsterraAudio.Instance != null)
                AsterraSettings.ApplyAudio();

            playMode = MatchPlayMode.OfflineVsAi;
            matchSeed = data.matchSeed;
            PlayerFactionIndex = data.playerFaction;
            EnemyFactionIndex = data.enemyFaction;
            MapKey = string.IsNullOrEmpty(data.mapKey) ? MapCatalog.BlackridgePassId : data.mapKey;
            aiDifficulty = data.formatVersion >= 2
                ? (AiDifficulty)Mathf.Clamp(data.aiDifficulty, 0, 3)
                : AiDifficulty.Normal;
            PlayerRoster = ResolveRoster(PlayerFactionIndex);
            EnemyRoster = ResolveRoster(EnemyFactionIndex);

            BeginMatchFromLobby(new PlayerId(0), includeAi: true, onlinePlayers: null, restore: data);

            var menu = FindFirstObjectByType<OfflineMatchMenu>();
            if (menu != null)
                menu.enabled = false;
            MatchFeedback.Show("Game loaded", AsterraSfx.OrderTrain);
            return true;
        }

        private void TeardownMatchRuntime()
        {
            if (coordinator != null)
            {
                coordinator.TickAdvanced -= OnTickAdvanced;
                coordinator.Stop();
                coordinator.ClearContributors();
            }

            var presentation = FindFirstObjectByType<SimPresentationBridge>();
            if (presentation != null)
                presentation.ClearAllViews();

            var terrain = FindFirstObjectByType<TerrainGridPresenter>();
            if (terrain != null)
                terrain.ClearPaint();

            World = null;
            _sim = null;
            Session = null;
            Wallet = null;
            Victory = null;
            Replay = new ReplayBuffer();
            _commandBus = new CommandBus();
            Commands = _commandBus;
            _participants.Clear();

            if (_orders != null)
                _orders.enabled = false;
        }

        private void EnsureOfflineMenu(bool enabled)
        {
            var menu = GetComponent<OfflineMatchMenu>();
            if (menu == null)
                menu = FindFirstObjectByType<OfflineMatchMenu>();
            if (menu == null)
                menu = gameObject.AddComponent<OfflineMatchMenu>();
            menu.enabled = enabled;
        }

        public void StartOfflineVsAi()
        {
            playMode = MatchPlayMode.OfflineVsAi;
            PlayerRoster = ResolveRoster(playerFactionIndex);
            EnemyRoster = ResolveRoster(enemyFactionIndex);
            if (EnemyRoster.Id == PlayerRoster.Id)
                EnemyRoster = ResolveRoster((PlayerRoster.Id.Value + 1) % FactionDefaultContent.All.Length);

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

            PlayerRoster = ResolveRoster(localSlot.FactionIndex);
            EnemyRoster = firstRemote != null
                ? ResolveRoster(firstRemote.FactionIndex)
                : ResolveRoster((localSlot.FactionIndex + 1) % FactionDefaultContent.All.Length);

            BeginMatchFromLobby(localPlayer, includeAi: false, startInfo.Players);
        }

        private FactionRoster ResolveRoster(int factionIndex)
        {
            factionIndex = Mathf.Clamp(factionIndex, 0, FactionDefaultContent.All.Length - 1);
            if (factions != null && factionIndex < factions.Length && factions[factionIndex] != null)
                return factions[factionIndex].ToRoster();
            return FactionDefaultContent.Get(new FactionId((byte)factionIndex));
        }

        private void RegisterFactionScriptablePowers()
        {
            if (factions == null || Definitions == null)
                return;
            for (int i = 0; i < factions.Length; i++)
                factions[i]?.RegisterPowers(Definitions);
        }

        private void BeginMatchFromLobby(
            PlayerId localPlayer,
            bool includeAi,
            PlayerSlotState[] onlinePlayers = null,
            Asterra.Gameplay.Save.MatchSaveData restore = null)
        {
            Ids = new SequentialIdFactory();
            Wallet = new ResourceWallet();
            Random = new DeterministicRandom(matchSeed);
            var environment = new WorldEnvironmentSim(
                weatherSeed: matchSeed,
                dayLengthSeconds: 180f,
                randomizeStartingWeather: restore == null);
            _sim = new SkirmishWorldSim(Wallet, Ids, Definitions, environment);
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
                int enemyGold = enemyStartingGold;
                if (includeAi)
                    enemyGold += AiDifficultyTuning.For(aiDifficulty).StartingGoldBonus;
                Wallet.Seed(localPlayer, ResourceType.Gold, startingGold);
                Wallet.Seed(localPlayer, ResourceType.Timber, startingTimber);
                Wallet.Seed(new PlayerId(1), ResourceType.Gold, enemyGold);
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
                var local = new PlayerSlotState
                {
                    Player = localPlayer,
                    FactionIndex = PlayerRoster.Id.Value,
                    IsReady = true,
                    DisplayName = "Player",
                };
                var ai = new PlayerSlotState
                {
                    Player = new PlayerId(1),
                    FactionIndex = EnemyRoster.Id.Value,
                    IsReady = true,
                    DisplayName = "Enemy AI",
                };
                // seats[0]=west, seats[1]=east — honor lobby spawn pick (AI takes the other).
                seats = LocalSpawnSeat == 0
                    ? new[] { local, ai }
                    : new[] { ai, local };
            }

            if (restore != null)
            {
                SkirmishDefaultContent.ApplyMapEnvironmentOnly(_sim, MapKey);
                Asterra.Gameplay.Save.OfflineMatchSaveService.RestoreWallets(restore, Wallet);
                _sim.RestoreFrom(restore);
                if (Ids is SequentialIdFactory seq)
                    seq.Seek(restore.nextEntityId);
            }
            else
            {
                SkirmishDefaultContent.PopulateFromSlots(_sim, Ids, seats, MapKey);
            }

            coordinator.ConfigureTiming(tickHz, commandDelayTicks);
            coordinator.SetBridge(networkBridge);
            coordinator.Initialize(_sim, _commandBus, localPlayer, _participants, Replay, networkBridge, Wallet);

            if (restore != null)
                coordinator.SeekTick(new Tick(restore.tick));

            if (includeAi)
            {
                string powerId = EnemyRoster.PowerIds != null && EnemyRoster.PowerIds.Length > 0
                    ? EnemyRoster.PowerIds[0]
                    : EnemyRoster.PowerId;
                var brain = new SkirmishOpponentBrain(
                    new PlayerId(1),
                    EnemyRoster.KeepBuildingId,
                    EnemyRoster.ProducerBuildingId,
                    EnemyRoster.BuilderUnitId,
                    EnemyRoster.BasicUnitId,
                    EnemyRoster.RangedUnitId,
                    EnemyRoster.CavalryUnitId,
                    EnemyRoster.TowerBuildingId,
                    EnemyRoster.OutpostBuildingId,
                    EnemyRoster.WallBuildingId,
                    EnemyRoster.BasicUpgradeId,
                    powerId,
                    aiDifficulty,
                    FactionDefaultContent.KeepTurretId);
                coordinator.AddContributor(new ArmyBrainFrameContributor(brain, _sim, Wallet));
            }

            if (attachPresentation)
            {
                var presentation = FindFirstObjectByType<SimPresentationBridge>();
                if (presentation == null)
                    presentation = gameObject.AddComponent<SimPresentationBridge>();

                if (FindFirstObjectByType<FogOfWarPresenter>() == null)
                    gameObject.AddComponent<FogOfWarPresenter>();
                if (FindFirstObjectByType<CombatFeedbackPresenter>() == null)
                    gameObject.AddComponent<CombatFeedbackPresenter>();
                if (FindFirstObjectByType<OrderLinePresenter>() == null)
                    gameObject.AddComponent<OrderLinePresenter>();
                if (FindFirstObjectByType<ProjectilePresenter>() == null)
                    gameObject.AddComponent<ProjectilePresenter>();
                if (FindFirstObjectByType<MinimapPresenter>() == null)
                    gameObject.AddComponent<MinimapPresenter>();
                if (FindFirstObjectByType<DayNightLightingPresenter>() == null)
                    gameObject.AddComponent<DayNightLightingPresenter>();
                if (FindFirstObjectByType<WorldDebugOverlay>() == null)
                    gameObject.AddComponent<WorldDebugOverlay>();
                if (FindFirstObjectByType<WeatherAtmospherePresenter>() == null)
                    gameObject.AddComponent<WeatherAtmospherePresenter>();
                if (FindFirstObjectByType<TerrainGridPresenter>() == null)
                    gameObject.AddComponent<TerrainGridPresenter>();
            }

            if (attachLocalOrders)
            {
                _orders = gameObject.GetComponent<LocalOrderController>();
                if (_orders == null)
                    _orders = gameObject.AddComponent<LocalOrderController>();
                _orders.enabled = true;
                _orders.Bind(this);
            }

            if (GetComponent<MatchHud>() == null)
                gameObject.AddComponent<MatchHud>();
            if (GetComponent<MatchFeedback>() == null && FindFirstObjectByType<MatchFeedback>() == null)
                gameObject.AddComponent<MatchFeedback>();

            if (attachCameraRig)
            {
                var camRig = FindFirstObjectByType<RtsCameraRig>();
                if (camRig == null)
                    camRig = gameObject.AddComponent<RtsCameraRig>();
                ResolveCameraFocus(out float focusX, out float focusZ);
                camRig.FocusOn(focusX, focusZ, height: 240f, back: 42f);
            }
            var keepIds = new[]
            {
                FactionDefaultContent.ArcaneumId,
                FactionDefaultContent.RoyalCitadelId,
                FactionDefaultContent.OutcastGreatCampId,
                FactionDefaultContent.FreetownTavernId,
                FactionDefaultContent.UniversityCollegeId,
                FactionDefaultContent.ChurchGrandTempleId,
            };
            Victory = new VictoryEvaluator(keepIds, territoryHoldSecondsToWin);
            if (restore != null)
            {
                Victory.SetHoldSeconds(new PlayerId(0), restore.holdSecondsP0);
                Victory.SetHoldSeconds(new PlayerId(1), restore.holdSecondsP1);
            }

            Result = MatchResult.None;
            coordinator.TickAdvanced += OnTickAdvanced;

            IsMatchRunning = true;
            Debug.Log($"[Asterra] Match started mode={playMode} seed={matchSeed} map={MapKey} players={_participants.Count}");
        }

        private void ResolveCameraFocus(out float focusX, out float focusZ)
        {
            var keeps = MapPreviewBuilder.GetKeepMarkers(MapKey);
            if (keeps.Count > 0)
            {
                int seat = LocalSpawnSeat;
                for (int i = 0; i < keeps.Count; i++)
                {
                    if (keeps[i].SeatIndex == seat)
                    {
                        focusX = keeps[i].X;
                        focusZ = keeps[i].Z;
                        return;
                    }
                }

                focusX = keeps[0].X;
                focusZ = keeps[0].Z;
                return;
            }

            if (MapCatalog.TryLoad(MapKey, out var custom))
            {
                focusX = custom.cameraFocusX;
                focusZ = custom.cameraFocusZ;
                return;
            }

            focusX = LocalSpawnSeat == 0 ? -320f : 320f;
            focusZ = 0f;
            if (MapCatalog.TryParseBuiltin(MapKey, out var builtin))
            {
                if (builtin == SkirmishMapId.RiverCrossing)
                {
                    focusX = LocalSpawnSeat == 0 ? -280f : 280f;
                    focusZ = LocalSpawnSeat == 0 ? -200f : 200f;
                }
                else if (builtin == SkirmishMapId.BlackridgePass)
                {
                    focusX = LocalSpawnSeat == 0 ? -330f : 330f;
                    focusZ = 0f;
                }
            }
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
