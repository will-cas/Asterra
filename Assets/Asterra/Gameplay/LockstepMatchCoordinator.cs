using System;
using System.Collections.Generic;
using Asterra.Core;
using Asterra.Net;
using UnityEngine;

namespace Asterra.Gameplay
{
    /// <summary>
    /// Supplies additional command frames each tick (e.g. AI seats in offline skirmish).
    /// </summary>
    public interface IFrameContributor
    {
        PlayerId Player { get; }
        CommandFrame BuildFrame(Tick targetTick, IWorldQuery world, IResourceWallet wallet);
    }

    /// <summary>
    /// Networked or local lockstep driver: submits local + contributor frames, waits via
    /// <see cref="LockstepFrameGate"/>, then advances the shared sim.
    /// </summary>
    public sealed class LockstepMatchCoordinator : MonoBehaviour
    {
        [SerializeField] private LockstepNetworkBridge bridge;
        [SerializeField] private float tickHz = 20f;
        [SerializeField] private int commandDelayTicks = 2;
        [SerializeField] private bool reportHashEverySecond = true;

        private CommandBus _commandBus;
        private IWorldSim _world;
        private IResourceWallet _wallet;
        private ILockstepClock _clock;
        private LockstepFrameGate _gate;
        private ReplayBuffer _replay;
        private PlayerId _localPlayer;
        private readonly List<GameCommand> _consumeBuffer = new();
        private readonly List<IFrameContributor> _contributors = new();
        private float _accum;
        private float _hashTimer;
        private uint? _lastSubmittedTarget;

        public ILockstepClock Clock => _clock;
        public LockstepFrameGate Gate => _gate;
        public bool IsRunning { get; private set; }
        public event Action<Tick, ulong> TickAdvanced;

        public void ConfigureTiming(float hz, int delayTicks)
        {
            tickHz = hz;
            commandDelayTicks = delayTicks;
        }

        public void SetBridge(LockstepNetworkBridge networkBridge) => bridge = networkBridge;

        public void Initialize(
            IWorldSim world,
            CommandBus commandBus,
            PlayerId localPlayer,
            IEnumerable<PlayerId> participants,
            ReplayBuffer replay = null,
            LockstepNetworkBridge networkBridge = null,
            IResourceWallet wallet = null)
        {
            _world = world;
            _commandBus = commandBus;
            _wallet = wallet;
            _localPlayer = localPlayer;
            _replay = replay ?? new ReplayBuffer();
            _clock = new LockstepClock(1f / Mathf.Max(1f, tickHz), commandDelayTicks);
            _gate = new LockstepFrameGate();
            _gate.SetExpectedPlayers(participants);
            if (networkBridge != null)
                bridge = networkBridge;
            if (bridge != null)
                bridge.Bind(commandBus, localPlayer, _replay, _gate);

            foreach (var player in participants)
            {
                for (uint t = 0; t < (uint)commandDelayTicks; t++)
                    _gate.SubmitEmpty(new Tick(t), player);
            }

            _lastSubmittedTarget = null;
            _accum = 0f;
            IsRunning = true;
        }

        public void AddContributor(IFrameContributor contributor)
        {
            if (contributor == null)
                return;
            _contributors.Add(contributor);
        }

        public void Stop()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning || _world == null || _gate == null)
                return;

            _accum += Time.deltaTime;
            float step = _clock.FixedDeltaSeconds;
            while (_accum >= step)
            {
                _accum -= step;
                StepOnce();
            }

            if (!reportHashEverySecond)
                return;

            _hashTimer += Time.deltaTime;
            if (_hashTimer < 1f)
                return;
            _hashTimer = 0f;
            ulong hash = _world.ComputeWorldHash();
            if (bridge != null)
                bridge.BroadcastWorldHash(_clock.CurrentTick, hash);
            TickAdvanced?.Invoke(_clock.CurrentTick, hash);
        }

        private void StepOnce()
        {
            var target = new Tick(_clock.CurrentTick.Value + (uint)_clock.CommandDelayTicks);
            if (_lastSubmittedTarget != target.Value)
            {
                _commandBus.ScheduleLocal(target);
                var scheduled = _commandBus.DrainForTick(target);
                var localFrame = new CommandFrame
                {
                    TargetTick = target,
                    Player = _localPlayer,
                    Commands = ToArray(scheduled),
                };
                SubmitFrame(localFrame);

                for (int i = 0; i < _contributors.Count; i++)
                {
                    var frame = _contributors[i].BuildFrame(target, _world, _wallet);
                    if (frame != null)
                        SubmitFrame(frame);
                }

                _lastSubmittedTarget = target.Value;
            }

            if (!_gate.TryConsume(_clock.CurrentTick, _consumeBuffer))
                return;

            _world.ApplyCommands(_consumeBuffer);
            _world.Tick(_clock.FixedDeltaSeconds);
            _clock.Advance();
        }

        private void SubmitFrame(CommandFrame frame)
        {
            if (bridge != null)
                bridge.BroadcastFrame(frame);
            else
            {
                _replay.Record(frame);
                _gate.Submit(frame);
            }
        }

        private static GameCommand[] ToArray(IReadOnlyList<GameCommand> list)
        {
            if (list == null || list.Count == 0)
                return Array.Empty<GameCommand>();
            var arr = new GameCommand[list.Count];
            for (int i = 0; i < list.Count; i++)
                arr[i] = list[i];
            return arr;
        }
    }
}
